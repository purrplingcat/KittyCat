using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PurrplingCore.Toolkit.Rendering;

/// <summary>
/// Mutable configuration for a single render layer, applied once during composition.
/// </summary>
public sealed class RenderLayerOptions
{
    /// <summary>
    /// Position within the owning renderer. Lower values are drawn first.
    /// </summary>
    public int Order { get; set; }

    public bool Enabled { get; set; } = true;

    public RenderSortMode SortMode { get; set; } = RenderSortMode.Submission;

    /// <summary>
    /// Camera for world-space layers. Leave null to draw in screen space.
    /// </summary>
    public ICamera2D? Camera { get; set; }

    /// <summary>
    /// Synchronizes a viewport-capable camera to the layer's target and recenters its origin.
    /// </summary>
    public bool SyncCameraViewport { get; set; }

    /// <summary>
    /// Scroll rate relative to the camera. Ignored when <see cref="Camera"/> is null.
    /// </summary>
    public Vector2 Parallax { get; set; } = Vector2.One;

    /// <summary>
    /// Discards commands outside the camera's visible bounds before they are queued.
    /// </summary>
    public bool CullingEnabled { get; set; }

    /// <summary>
    /// Clears the layer's own target before drawing. Ignored without <see cref="Target"/>.
    /// </summary>
    public Color? ClearColor { get; set; }

    /// <summary>
    /// Draws into an offscreen target instead of the renderer's output.
    /// </summary>
    public RenderTargetSpec? Target { get; set; }

    public BlendState? BlendState { get; set; }

    /// <summary>
    /// Overrides the renderer's default sampler state.
    /// </summary>
    public SamplerState? SamplerState { get; set; }

    public DepthStencilState? DepthStencilState { get; set; }

    public RasterizerState? RasterizerState { get; set; }

    public Effect? Effect { get; set; }

    /// <summary>
    /// Processors applied to this layer's target, in order.
    /// </summary>
    public IList<IPostProcessor> PostProcessors { get; } = [];

    /// <summary>
    /// Custom pass. Defaults to <see cref="DrawCommandPass"/> when left null.
    /// </summary>
    public IRenderLayerPass? Pass { get; set; }

    /// <summary>
    /// Initial command capacity, sized to avoid growth during the first frames.
    /// </summary>
    public int InitialCapacity { get; set; } = 256;
}

/// <summary>
/// A composed render layer: its configuration, its command queue, and the pass that draws it.
/// </summary>
/// <remarks>
/// Built once from <see cref="RenderLayerOptions"/> during composition. Only <see cref="Enabled"/>
/// is mutable afterwards, so the flush path reads immutable fields and a pooled command list.
/// </remarks>
public sealed class RenderLayer : IDisposable
{
    private bool _disposed;

    public RenderLayer(RenderLayerId id, RenderLayerOptions options, SamplerState? defaultSamplerState = null)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!id.IsValid)
        {
            throw new ArgumentException("A render layer requires an identifier created by RenderLayerId.Of.", nameof(id));
        }

        IViewportCamera2D? viewportCamera = options.Camera as IViewportCamera2D;

        if (options.SyncCameraViewport && viewportCamera is null)
        {
            throw new ArgumentException("Camera viewport synchronization requires an IViewportCamera2D.", nameof(options));
        }

        Id = id;
        Order = options.Order;
        Enabled = options.Enabled;
        Camera = options.Camera;
        SyncCameraViewport = options.SyncCameraViewport;
        ViewportCamera = viewportCamera;
        Parallax = options.Parallax;
        CullingEnabled = options.CullingEnabled && options.Camera is not null;
        ClearColor = options.ClearColor;
        Target = options.Target;
        BlendState = options.BlendState;
        SamplerState = options.SamplerState ?? defaultSamplerState;
        DepthStencilState = options.DepthStencilState;
        RasterizerState = options.RasterizerState;
        Effect = options.Effect;
        PostProcessors = [.. options.PostProcessors];
        Pass = options.Pass ?? new DrawCommandPass();
        FrameSubmissionPass = Pass as IFrameSubmissionPass;
        Commands = new DrawList(options.SortMode, options.InitialCapacity);
    }

    public RenderLayerId Id { get; }

    public int Order { get; }

    /// <summary>
    /// Skips the layer entirely, including submission, while false.
    /// </summary>
    public bool Enabled { get; set; }

    public ICamera2D? Camera { get; }

    public bool SyncCameraViewport { get; }

    /// <summary>
    /// Viewport capability provided by the camera, when supported.
    /// </summary>
    public IViewportCamera2D? ViewportCamera { get; }

    public Vector2 Parallax { get; }

    public bool CullingEnabled { get; }

    public Color? ClearColor { get; }

    public RenderTargetSpec? Target { get; }

    public BlendState? BlendState { get; }

    public SamplerState? SamplerState { get; }

    public DepthStencilState? DepthStencilState { get; }

    public RasterizerState? RasterizerState { get; }

    public Effect? Effect { get; }

    public IPostProcessor[] PostProcessors { get; }

    public IRenderLayerPass Pass { get; }

    internal IFrameSubmissionPass? FrameSubmissionPass { get; }

    /// <summary>
    /// Commands queued for the current frame.
    /// </summary>
    public DrawList Commands { get; }

    /// <summary>
    /// Builds the graphics state for this layer.
    /// </summary>
    public BatchState CreateBatchState()
    {
        var transform = Camera?.GetViewMatrix(Parallax) ?? Matrix.Identity;

        return new BatchState(
            transform,
            BlendState,
            SamplerState,
            DepthStencilState,
            RasterizerState,
            Effect);
    }

    /// <summary>
    /// Returns the region visible to this layer, or null when nothing is culled.
    /// </summary>
    public Rectangle? GetCullBounds()
        => CullingEnabled ? Camera!.GetVisibleBounds(Parallax) : null;

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        Pass.Dispose();
        foreach (var processor in PostProcessors)
        {
            processor.Dispose();
        }
    }
}
