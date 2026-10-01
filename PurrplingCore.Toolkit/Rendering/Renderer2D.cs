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

    /// <summary>Optional rolling timing and command-count diagnostics configured at construction.</summary>
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

        if (_initialized) return;

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

        _initialized = true;
    }

    /// <summary>Creates the batcher used to replay commands. Override to supply a custom one.</summary>
    protected virtual ISpriteBatcher CreateBatcher(GraphicsDevice device) => new SpriteBatchBatcher(new SpriteBatch(device));

    public void BeginFrame()
    {
        _openFrame = _frameId;
        _text.Clear();

        foreach (var layer in _layers)
        {
            layer.Commands.Clear();
        }
    }

    public void Flush(GraphicsDevice device)
    {
        ArgumentNullException.ThrowIfNull(device);
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!_initialized)
        {
            throw new InvalidOperationException("Initialize the renderer before flushing it.");
        }

        if (!Enabled || _openFrame != _frameId)
        {
            _frameId++;
            return;
        }

        var diagnostics = Diagnostics;
        var totalStart = diagnostics is null ? 0 : Stopwatch.GetTimestamp();
        diagnostics?.BeginFrame();
        var output = ResolveOutput(device);
        device.SetRenderTarget(output);
        if (_options.ClearColor is { } clearColor)
        {
            device.Clear(clearColor);
        }

        var stageStart = diagnostics is null ? 0 : Stopwatch.GetTimestamp();
        Prepare(device, diagnostics);
        var prepareMilliseconds = diagnostics is null
            ? 0
            : Stopwatch.GetElapsedTime(stageStart).TotalMilliseconds;

        stageStart = diagnostics is null ? 0 : Stopwatch.GetTimestamp();
        Execute(device, output, diagnostics);
        var executeMilliseconds = diagnostics is null
            ? 0
            : Stopwatch.GetElapsedTime(stageStart).TotalMilliseconds;

        var composeMilliseconds = 0d;
        if (_output is not null)
        {
            stageStart = diagnostics is null ? 0 : Stopwatch.GetTimestamp();
            device.SetRenderTarget(OutputOverride);
            Compose(ApplyChain(device, _postProcessors, _output, ref _outputScratch, OutputOverride));
            if (diagnostics is not null)
            {
                composeMilliseconds = Stopwatch.GetElapsedTime(stageStart).TotalMilliseconds;
            }
        }

        if (diagnostics is not null)
        {
            diagnostics.RecordFrame(
                _frameId,
                Stopwatch.GetElapsedTime(totalStart).TotalMilliseconds,
                prepareMilliseconds,
                executeMilliseconds,
                composeMilliseconds);
        }

        _frameId++;
    }

    private RenderTarget2D? ResolveOutput(GraphicsDevice device)
    {
        if (_options.Output is not { } spec) return OutputOverride;

        EnsureTarget(device, spec, ref _output);
        return _output;
    }

    private void Prepare(GraphicsDevice device, RendererDiagnostics? diagnostics)
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
            layer.Pass.Prepare(new RenderFrameContext(device, layer, _batcher!, _resources, _text, state));
            if (diagnostics is not null)
            {
                diagnostics.RecordLayerPrepared(
                    index,
                    true,
                    layer.Commands.Count,
                    sortMilliseconds,
                    Stopwatch.GetElapsedTime(prepareStart).TotalMilliseconds);
            }
        }
    }

    private void Execute(GraphicsDevice device, RenderTarget2D? output, RendererDiagnostics? diagnostics)
    {
        for (var index = 0; index < _layers.Length; index++)
        {
            var layer = _layers[index];
            if (!layer.Enabled) continue;

            var executeStart = diagnostics is null ? 0 : Stopwatch.GetTimestamp();
            var state = layer.CreateBatchState();
            var context = new RenderFrameContext(device, layer, _batcher!, _resources, _text, state);

            if (layer.Target is not { } spec)
            {
                layer.Pass.Execute(context);
                if (diagnostics is not null)
                {
                    diagnostics.RecordLayerExecuted(index, Stopwatch.GetElapsedTime(executeStart).TotalMilliseconds);
                }

                continue;
            }

            EnsureTarget(device, spec, ref _layerTargets[index]);
            var target = _layerTargets[index]!;
            device.SetRenderTarget(target);
            device.Clear(layer.ClearColor ?? Color.Transparent);
            layer.Pass.Execute(context);

            device.SetRenderTarget(output);
            Compose(ApplyChain(device, layer.PostProcessors, target, ref _layerScratch[index], output));
            if (diagnostics is not null)
            {
                diagnostics.RecordLayerExecuted(index, Stopwatch.GetElapsedTime(executeStart).TotalMilliseconds);
            }
        }
    }

    /// <summary>
    /// Runs a processor chain and returns the image holding the result. Chains longer than one
    /// step ping-pong between the source target and a scratch target of the same size.
    /// </summary>
    private RenderTarget2D ApplyChain(
        GraphicsDevice device,
        IPostProcessor[] processors,
        RenderTarget2D source,
        ref RenderTarget2D? scratch,
        RenderTarget2D? restoreTarget)
    {
        if (processors.Length == 0) return source;

        scratch ??= new RenderTarget2D(device, source.Width, source.Height, false, source.Format, DepthFormat.None);

        var current = source;
        var other = scratch;

        foreach (var processor in processors)
        {
            device.SetRenderTarget(other);
            device.Clear(Color.Transparent);
            processor.Process(new PostProcessContext(device, _batcher!, _resources, _text, current, other));
            (current, other) = (other, current);
        }

        device.SetRenderTarget(restoreTarget);
        return current;
    }

    /// <summary>Draws an offscreen image onto the currently bound target at its native size.</summary>
    private void Compose(Texture2D image)
    {
        var command = new DrawCommand
        {
            Source = new Rectangle(0, 0, image.Width, image.Height),
            Position = Vector2.Zero,
            Scale = Vector2.One,
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

    private void EnsureTarget(GraphicsDevice device, RenderTargetSpec spec, ref RenderTarget2D? target)
    {
        var (width, height) = ResolveSize(device, spec);

        if (target is not null && target.Width == width && target.Height == height) return;

        target?.Dispose();
        target = new RenderTarget2D(device, width, height, false, spec.Format, spec.DepthFormat);
    }

    private (int Width, int Height) ResolveSize(GraphicsDevice device, RenderTargetSpec spec)
    {
        if (spec.ResizeMode == RenderTargetResizeMode.Fixed) return (spec.Width, spec.Height);

        if (spec.ResizeMode == RenderTargetResizeMode.MatchResolution && VirtualResolution.X > 0 && VirtualResolution.Y > 0)
        {
            return (VirtualResolution.X, VirtualResolution.Y);
        }

        var parameters = device.PresentationParameters;
        return (parameters.BackBufferWidth, parameters.BackBufferHeight);
    }

    internal LayerWriter OpenLayer(RenderLayerId id)
    {
        if (!Enabled) return default;

        var slot = id.Value;
        if (slot >= _layersById.Length) return default;

        var layer = _layersById[slot];
        if (layer is null || !layer.Enabled) return default;

        if (_openFrame != _frameId)
        {
            BeginFrame();
        }

        return new LayerWriter(layer, _resources, _text);
    }

    /// <summary>Drops cached resource registrations. Call after unloading content.</summary>
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
