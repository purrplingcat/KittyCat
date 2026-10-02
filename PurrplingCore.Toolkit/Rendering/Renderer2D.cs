using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PurrplingCore.Toolkit.Rendering;

/// <summary>
/// Collects draw commands per layer and replays them in one pass when the frame is flushed.
/// </summary>
/// <remarks>
/// A frame opens lazily on the first submission after a flush, so submitters never depend on
/// running in a particular order relative to each other or to the flush. Flushing runs the
/// Bevy-style sequence prepare, sort, execute, compose, with graphics state changing at most
/// once per layer.
/// </remarks>
public class Renderer2D : IRenderer
{
    private readonly RendererOptions _options;
    private readonly RenderLayer[] _layers;
    private readonly RenderLayer?[] _layersById;
    private readonly RenderTarget2D?[] _layerTargets;
    private readonly RenderTarget2D?[] _layerScratch;
    private readonly IPostProcessor[] _postProcessors;
    private readonly RenderResourceTable _resources = new();
    private readonly FrameTextBuffer _text = new();
    private ISpriteBatcher? _batcher;
    private GraphicsDevice _device = null!;
    private RenderTarget2D? _output;
    private RenderTarget2D? _outputScratch;
    private int _frameId;
    private int _openFrame = -1;
    private bool _initialized;
    private bool _disposed;

    public Renderer2D(string name, RendererOptions options)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(options);

        Name = name;
        _options = options;
        Enabled = options.Enabled;
        Context = new RenderContext(this);
        _postProcessors = [.. options.PostProcessors];

        _layers = new RenderLayer[options.Layers.Count];
        for (var index = 0; index < options.Layers.Count; index++)
        {
            var definition = options.Layers[index];
            _layers[index] = new RenderLayer(definition.Id, definition.Options, options.DefaultSamplerState);
        }

        Array.Sort(_layers, static (left, right) => left.Order.CompareTo(right.Order));

        var slots = 0;
        foreach (var layer in _layers)
        {
            slots = Math.Max(slots, layer.Id.Value + 1);
        }

        // Dense lookup by layer id keeps submission free of dictionary hashing.
        _layersById = new RenderLayer[slots];
        foreach (var layer in _layers)
        {
            _layersById[layer.Id.Value] = layer;
        }

        _layerTargets = new RenderTarget2D[_layers.Length];
        _layerScratch = new RenderTarget2D[_layers.Length];
        Diagnostics = options.Diagnostics is { } diagnosticsOptions
            ? new RendererDiagnostics(name, diagnosticsOptions, _layers)
            : null;
    }

    public string Name { get; }

    public bool Enabled { get; set; }

    public RenderContext Context { get; }

    internal RenderResourceTable Resources => _resources;

    internal FrameTextBuffer TextBuffer => _text;

    /// <summary>
    /// Optional rolling timing and command-count diagnostics configured at construction.
    /// </summary>
    public RendererDiagnostics? Diagnostics { get; }

    /// <summary>
    /// Target the composed frame is written to. Null means the back buffer. Set by a host that
    /// chains renderers through a shared target.
    /// </summary>
    public RenderTarget2D? OutputOverride { get; set; }

    /// <summary>
    /// Virtual resolution used by <see cref="RenderTargetResizeMode.MatchResolution"/>.
    /// Falls back to the back buffer size while unset.
    /// </summary>
    public Point VirtualResolution { get; set; }

    public void Initialize(GraphicsDevice device)
    {
        ArgumentNullException.ThrowIfNull(device);
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_initialized)
        {
            throw new InvalidOperationException($"Renderer '{Name}' is already initialized.");
        }

        _batcher = CreateBatcher(device);

        foreach (var layer in _layers)
        {
            layer.Pass.Initialize(device);
            foreach (var processor in layer.PostProcessors)
            {
                processor.Initialize(device);
            }
        }

        foreach (var processor in _postProcessors)
        {
            processor.Initialize(device);
        }

        _device = device;
        _initialized = true;

        SynchronizeCameraViewports();
    }

    /// <summary>
    /// Creates the batcher used to replay commands. Override to supply a custom one.
    /// </summary>
    protected virtual ISpriteBatcher CreateBatcher(GraphicsDevice device) => new SpriteBatchBatcher(new SpriteBatch(device));

    public void BeginFrame()
    {
        var isNewFrame = _openFrame != _frameId;
        _openFrame = _frameId;
        _text.Clear();

        if (isNewFrame) SynchronizeCameraViewports();

        foreach (var layer in _layers)
        {
            layer.Commands.Clear();
            layer.FrameSubmissionPass?.ClearSubmissions();
        }
    }

    public void Flush()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!_initialized)
        {
            throw new InvalidOperationException("Initialize the renderer before flushing it.");
        }

        if (!Enabled) goto AdvanceFrame;

        var hasQueuedFrame = _openFrame == _frameId;

        if (!hasQueuedFrame) BeginFrame();

        var diagnostics = Diagnostics;
        var totalStart = diagnostics is null ? 0 : Stopwatch.GetTimestamp();
        diagnostics?.BeginFrame();

        // Resolve the output render target for this frame.
        var output = ResolveOutput();
        _device.SetRenderTarget(output); // Set the render target for this frame.
        if (_options.ClearColor is { } clearColor)
        {
            // Clear the render target with the specified clear color.
            _device.Clear(clearColor);
        }
        else if (_output is not null)
        {
            // The private output is composed over other renderers, so stale pixels must not leak through.
            _device.Clear(Color.Transparent);
        }

        // Prepare the frame for rendering.
        long stageStart = diagnostics is null ? 0 : Stopwatch.GetTimestamp();
        Prepare(diagnostics);
        var prepareMilliseconds = diagnostics is null
            ? 0
            : Stopwatch.GetElapsedTime(stageStart).TotalMilliseconds;

        // Execute the rendering commands for this frame.
        stageStart = diagnostics is null ? 0 : Stopwatch.GetTimestamp();
        Execute(output, diagnostics);
        double executeMilliseconds = diagnostics is null
            ? 0
            : Stopwatch.GetElapsedTime(stageStart).TotalMilliseconds;

        // Compose the final image for this frame.
        double composeMilliseconds = 0d;
        if (_output is not null)
        {
            stageStart = diagnostics is null ? 0 : Stopwatch.GetTimestamp();
            var image = ApplyChain(_postProcessors, _output, ref _outputScratch, OutputOverride);
            _device.SetRenderTarget(OutputOverride);
            Compose(image, OutputOverride);
            if (diagnostics is not null)
            {
                composeMilliseconds = Stopwatch.GetElapsedTime(stageStart).TotalMilliseconds;
            }
        }

        diagnostics?.RecordFrame(
            _frameId,
            Stopwatch.GetElapsedTime(totalStart).TotalMilliseconds,
            prepareMilliseconds,
            executeMilliseconds,
            composeMilliseconds
        );

    AdvanceFrame:
        // Keep frame advancement in one epilogue for skipped and rendered frames.
        _frameId++;
    }

    private RenderTarget2D? ResolveOutput()
    {
        if (!_options.Output.HasValue) return OutputOverride;

        var spec = _options.Output.Value;
        EnsureTarget(spec, ref _output);
        return _output;
    }

    private void Prepare(RendererDiagnostics? diagnostics)
    {
        for (var index = 0; index < _layers.Length; index++)
        {
            var layer = _layers[index];
            if (!layer.Enabled)
            {
                diagnostics?.RecordLayerPrepared(index, false, 0, 0, 0);
                continue;
            }

            var sortStart = diagnostics is null ? 0 : Stopwatch.GetTimestamp();
            layer.Commands.Sort();
            var sortMilliseconds = diagnostics is null
                ? 0
                : Stopwatch.GetElapsedTime(sortStart).TotalMilliseconds;
            var state = layer.CreateBatchState();
            var prepareStart = diagnostics is null ? 0 : Stopwatch.GetTimestamp();
            layer.Pass.Prepare(new RenderFrameContext(_device, layer, _batcher!, _resources, _text, state));
            diagnostics?.RecordLayerPrepared(
                index,
                true,
                layer.Commands.Count,
                sortMilliseconds,
                Stopwatch.GetElapsedTime(prepareStart).TotalMilliseconds
            );
        }
    }

    private void Execute(RenderTarget2D? output, RendererDiagnostics? diagnostics)
    {
        for (var index = 0; index < _layers.Length; index++)
        {
            var layer = _layers[index];
            if (!layer.Enabled) continue;

            var executeStart = diagnostics is null ? 0 : Stopwatch.GetTimestamp();
            var state = layer.CreateBatchState();
            var context = new RenderFrameContext(_device, layer, _batcher!, _resources, _text, state);

            var targetSpec = layer.Target;
            if (targetSpec is null)
            {
                layer.Pass.Execute(context);
                diagnostics?.RecordLayerExecuted(index, Stopwatch.GetElapsedTime(executeStart).TotalMilliseconds);

                continue;
            }

            EnsureTarget(targetSpec.Value, ref _layerTargets[index]);
            var target = _layerTargets[index]!;
            _device.SetRenderTarget(target);
            _device.Clear(layer.ClearColor ?? Color.Transparent);
            layer.Pass.Execute(context);

            var image = ApplyChain(layer.PostProcessors, target, ref _layerScratch[index], output);
            _device.SetRenderTarget(output);
            Compose(image, output);
            diagnostics?.RecordLayerExecuted(index, Stopwatch.GetElapsedTime(executeStart).TotalMilliseconds);
        }
    }

    /// <summary>
    /// Runs a processor chain and returns the image holding the result. Chains longer than one
    /// step ping-pong between the source target and a scratch target of the same size.
    /// </summary>
    private RenderTarget2D ApplyChain(
        IPostProcessor[] processors,
        RenderTarget2D source,
        ref RenderTarget2D? scratch,
        RenderTarget2D? restoreTarget)
    {
        if (processors.Length == 0) return source;

        if (scratch is null || scratch.Width != source.Width || scratch.Height != source.Height || scratch.Format != source.Format)
        {
            scratch?.Dispose();
            scratch = CreateTarget(source.Width, source.Height, source.Format, DepthFormat.None);
        }

        var current = source;
        var other = scratch;

        foreach (var processor in processors)
        {
            _device.SetRenderTarget(other);
            _device.Clear(Color.Transparent);
            processor.Process(new PostProcessContext(_device, _batcher!, _resources, _text, current, other));
            (current, other) = (other, current);
        }

        _device.SetRenderTarget(restoreTarget);
        return current;
    }

    /// <summary>
    /// Draws an offscreen image onto the bound target, placed by <see cref="RendererOptions.ScaleMode"/>.
    /// </summary>
    private void Compose(Texture2D image, RenderTarget2D? target)
    {
        var targetWidth = target?.Width ?? _device.PresentationParameters.BackBufferWidth;
        var targetHeight = target?.Height ?? _device.PresentationParameters.BackBufferHeight;
        var scale = GetScale(_options.ScaleMode, targetWidth, targetHeight, image.Width, image.Height);
        var position = new Vector2(
            MathF.Floor((targetWidth - image.Width * scale.X) * 0.5f),
            MathF.Floor((targetHeight - image.Height * scale.Y) * 0.5f));

        var command = new DrawCommand
        {
            Source = new Rectangle(0, 0, image.Width, image.Height),
            Position = position,
            Scale = scale,
            Origin = Vector2.Zero,
            Color = Color.White,
            ResourceIndex = _resources.GetTextureIndex(image),
            Kind = DrawCommandKind.Sprite,
        };

        var batcher = _batcher!;
        batcher.Begin(new BatchState(Matrix.Identity, BlendState.AlphaBlend, _options.DefaultSamplerState));
        batcher.Submit(command, _resources, _text);
        batcher.End();
    }

    private static Vector2 GetScale(RenderScaleMode mode, int targetWidth, int targetHeight, int imageWidth, int imageHeight)
    {
        var scaleX = (float)targetWidth / imageWidth;
        var scaleY = (float)targetHeight / imageHeight;
        var fit = MathF.Min(scaleX, scaleY);

        return mode switch
        {
            RenderScaleMode.Stretch => new Vector2(scaleX, scaleY),
            RenderScaleMode.Fill => new Vector2(MathF.Max(scaleX, scaleY)),
            RenderScaleMode.Center => Vector2.One,
            // Below 1x there is no whole multiple, so shrink smoothly rather than overflow.
            RenderScaleMode.IntegerFit => new Vector2(fit >= 1f ? MathF.Floor(fit) : fit),
            _ => new Vector2(fit),
        };
    }

    private void EnsureTarget(RenderTargetSpec spec, ref RenderTarget2D? target)
    {
        var (width, height) = ResolveSize(spec);

        if (target is not null && target.Width == width && target.Height == height) return;

        target?.Dispose();
        target = CreateTarget(width, height, spec.Format, spec.DepthFormat);
    }

    // MonoGame clears DiscardContents targets on every bind, which would wipe layers already
    // drawn into the output whenever a layer target or post-processor switches targets.
    private RenderTarget2D CreateTarget(
        int width,
        int height,
        SurfaceFormat format,
        DepthFormat depthFormat)
        => new(_device, width, height, false, format, depthFormat, 0, RenderTargetUsage.PreserveContents);

    private (int Width, int Height) ResolveSize(RenderTargetSpec spec)
    {
        if (spec.ResizeMode == RenderTargetResizeMode.Fixed) return (spec.Width, spec.Height);

        if (spec.ResizeMode == RenderTargetResizeMode.MatchResolution && VirtualResolution.X > 0 && VirtualResolution.Y > 0)
        {
            return (VirtualResolution.X, VirtualResolution.Y);
        }

        var parameters = _device.PresentationParameters;
        return (parameters.BackBufferWidth, parameters.BackBufferHeight);
    }

    internal LayerWriter OpenLayer(RenderLayerId id)
    {
        var layer = GetActiveLayer(id);
        if (layer is null) return default;

        OpenLayerFrame();

        return new LayerWriter(layer, _resources, _text);
    }

    internal LayerSubmissionWriter<T> OpenSubmissions<T>(RenderLayerId id) where T : struct
    {
        var layer = GetActiveLayer(id);
        if (layer is null) return default;

        var submissionPass = layer.Pass as ILayerSubmissionPass<T>;
        if (submissionPass is null)
        {
            throw new InvalidOperationException($"Layer '{layer.Id.Name}' does not accept {typeof(T).Name} submissions.");
        }

        OpenLayerFrame();
        return new LayerSubmissionWriter<T>(submissionPass);
    }

    private RenderLayer? GetActiveLayer(RenderLayerId id)
    {
        if (!Enabled) return null;

        var slot = id.Value;
        if (slot >= _layersById.Length) return null;

        var layer = _layersById[slot];
        return layer is { Enabled: true } ? layer : null;
    }

    private void OpenLayerFrame()
    {
        if (_openFrame != _frameId) BeginFrame();
    }

    private void SynchronizeCameraViewports()
    {
        if (!_initialized) return;

        foreach (var layer in _layers)
        {
            var camera = layer.ViewportCamera;
            if (!layer.SyncCameraViewport || camera is null) continue;

            var size = GetCameraViewportSize(layer);
            camera.SetViewport(size.X, size.Y);
            camera.CenterOrigin();
        }
    }

    private Point GetCameraViewportSize(RenderLayer layer)
    {
        var layerTarget = layer.Target;
        if (layerTarget.HasValue)
        {
            var size = ResolveSize(layerTarget.Value);
            return new Point(size.Width, size.Height);
        }

        var rendererOutput = _options.Output;
        if (rendererOutput.HasValue)
        {
            var size = ResolveSize(rendererOutput.Value);
            return new Point(size.Width, size.Height);
        }

        var output = OutputOverride;
        if (output is not null)
        {
            return new Point(output.Width, output.Height);
        }

        var parameters = _device.PresentationParameters;
        return new Point(parameters.BackBufferWidth, parameters.BackBufferHeight);
    }

    /// <summary>
    /// Drops cached resource registrations. Call after unloading content.
    /// </summary>
    public void ResetResources() => _resources.Reset();

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        _disposed = true;
        if (!disposing) return;

        foreach (var layer in _layers)
        {
            layer.Dispose();
        }

        foreach (var processor in _postProcessors)
        {
            processor.Dispose();
        }

        DisposeAll(_layerTargets);
        DisposeAll(_layerScratch);
        _output?.Dispose();
        _outputScratch?.Dispose();
    }

    private static void DisposeAll(RenderTarget2D?[] targets)
    {
        for (var index = 0; index < targets.Length; index++)
        {
            targets[index]?.Dispose();
            targets[index] = null;
        }
    }
}
