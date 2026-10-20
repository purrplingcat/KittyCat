using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PurrplingCore.Toolkit.Graphics;
using System.Diagnostics.CodeAnalysis;

namespace PurrplingCore.Toolkit.Rendering;

/// <summary>
/// Identifies a layer and its options in a renderer declaration.
/// </summary>
public readonly record struct RenderLayerDefinition(RenderLayerId Id, RenderLayerOptions Options);

/// <summary>
/// Routes frame submissions to layer passes and composes their output when flushed.
/// </summary>
/// <remarks>
/// Submission never touches the graphics device. A game composes several renderers, typically
/// one per output target or post-processing chain. The owner explicitly begins a frame before
/// submission and may flush the retained submissions more than once.
/// </remarks>
public interface IRenderer : IDisposable
{
    /// <summary>
    /// Name used for diagnostics.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Skips both submission and flushing while false.
    /// </summary>
    bool Enabled { get; set; }

    /// <summary>
    /// Entry point used by draw systems, scenes, and overlays to submit pass data.
    /// </summary>
    RenderContext Context { get; }

    /// <summary>
    /// Creates batcher, targets, and pass resources and binds the renderer to <paramref name="device"/>.
    /// Called once before the first frame; every subsequent call throws.
    /// </summary>
    void Initialize(GraphicsDevice device);

    /// <summary>
    /// Discards pass submissions and text, samples the current layout, and starts a new frame.
    /// Call explicitly before producers submit; neither Batch nor Flush calls this method.
    /// </summary>
    void BeginFrame();

    /// <summary>
    /// Prepares, draws, post-processes, and composes the retained frame on the bound device.
    /// Does not clear submissions or text; repeated calls replay them until BeginFrame.
    /// </summary>
    void Flush();
}

/// <summary>
/// Configuration for a single renderer, applied once during composition.
/// </summary>
public sealed class RendererOptions()
{
    /// <summary>
    /// Creates options with the required logical screen.
    /// </summary>
    [SetsRequiredMembers]
    public RendererOptions(Screen screen) : this()
    {
        ArgumentNullException.ThrowIfNull(screen);
        Screen = screen;
    }

    /// <summary>
    /// Required logical screen and final presentation area.
    /// </summary>
    public required Screen Screen { get; set; }

    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Sampler inherited by layers that do not override it.
    /// </summary>
    public SamplerState DefaultSamplerState { get; set; } = SamplerState.PointClamp;

    /// <summary>
    /// Enables bounded CPU timing and submission-count diagnostics when non-null.
    /// </summary>
    public RendererDiagnosticsOptions? Diagnostics { get; set; }

    /// <summary>
    /// Clears the renderer's own target before any layer draws.
    /// </summary>
    public Color? ClearColor { get; set; }

    /// <summary>
    /// Describes the renderer's private output target. Defaults to the screen's logical size.
    /// </summary>
    public RenderTargetSpec Output { get; set; } = RenderTargetSpec.MatchScreen();

    /// <summary>
    /// How offscreen images are scaled during layer composition and final presentation.
    /// </summary>
    public RenderScaleMode ScaleMode { get; set; } = RenderScaleMode.Fit;

    /// <summary>
    /// Aligns the final image and its screen hierarchy within the output area.
    /// Internal layer composition remains centered.
    /// </summary>
    public RenderAlignment Alignment { get; set; } = RenderAlignment.Center;

    /// <summary>
    /// Processors applied to <see cref="Output"/> after all layers have drawn.
    /// </summary>
    public IList<IPostProcessor> PostProcessors { get; } = [];

    /// <summary>
    /// Layers declared for this renderer. Collection initializers are supported.
    /// </summary>
    public IList<RenderLayerDefinition> Layers { get; } = [];

    /// <summary>
    /// Declares a layer. Call order does not matter; <see cref="RenderLayerOptions.Order"/> does.
    /// </summary>
    public RendererOptions AddLayer(RenderLayerId id, RenderLayerOptions? options = null)
    {
        Layers.Add(new RenderLayerDefinition(id, options ?? new RenderLayerOptions()));
        return this;
    }
}
