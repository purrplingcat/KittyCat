using Microsoft.Xna.Framework;

namespace PurrplingCore.Toolkit.Rendering;

/// <summary>
/// Queues frame-scoped values for a layer pass that accepts <typeparamref name="T"/>.
/// </summary>
/// <remarks>
/// The writer stays local to the submitting method and never accesses the graphics device.
/// Disabled layers and unavailable optional passes return an inactive writer that discards submissions.
/// </remarks>
public readonly ref struct LayerWriter<T> where T : struct
{
    private readonly ILayerSubmissionPass<T>? _pass;

    internal LayerWriter(
        RenderLayer layer,
        ILayerSubmissionPass<T> pass,
        RenderResourceTable resources,
        FrameTextBuffer text)
    {
        _pass = pass;
        Resources = resources;
        Text = text;
        CullBounds = layer.GetCullBounds();
    }

    /// <summary>
    /// Renderer-owned resource table, or null for an inactive writer.
    /// </summary>
    public RenderResourceTable? Resources { get; }

    /// <summary>
    /// Current frame's shared text buffer, or null for an inactive writer.
    /// </summary>
    public FrameTextBuffer? Text { get; }

    /// <summary>
    /// Visible world bounds sampled when the writer was obtained, or null when inactive or culling is disabled.
    /// </summary>
    public Rectangle? CullBounds { get; }

    /// <summary>
    /// Indicates whether the layer has a compatible active submission pass.
    /// </summary>
    public bool IsActive => _pass != null;

    /// <summary>
    /// Queues one value without modifying it. Referenced resources must remain valid through the last replay.
    /// </summary>
    public void Submit(in T submission) => _pass?.Submit(in submission);
}

/// <summary>
/// Per-renderer entry point for submitting data to layer passes.
/// </summary>
/// <remarks>
/// Resolving a layer never begins or clears a frame. The renderer owner calls BeginFrame
/// before producers submit the next frame's data.
/// </remarks>
public sealed class RenderContext
{
    private readonly Renderer2D _renderer;

    internal RenderContext(Renderer2D renderer) => _renderer = renderer;

    /// <summary>
    /// Gets the renderer-owned resource table for raw command construction.
    /// </summary>
    public RenderResourceTable Resources => _renderer.Resources;

    /// <summary>
    /// Gets the current frame text buffer for raw text command construction.
    /// </summary>
    public FrameTextBuffer TextBuffer => _renderer.TextBuffer;

    /// <summary>
    /// Returns a draw-command writer. Required access validates that the layer accepts draw commands.
    /// Disabled renderers and layers still return an inactive writer.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Required access cannot find the layer or a pass accepting draw commands.
    /// </exception>
    public LayerWriter<DrawCommand> Batch(RenderLayerId id, bool required = false)
        => Batch<DrawCommand>(id, required);

    /// <summary>
    /// Returns a writer for the layer's pass when it accepts <typeparamref name="T"/>.
    /// Optional access returns an inactive writer when the layer or submission type is unavailable.
    /// </summary>
    /// <remarks>
    /// Required access validates composition even when the renderer or layer is disabled.
    /// A compatible disabled layer returns an inactive writer in either mode.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Required access cannot find the layer or a pass accepting <typeparamref name="T"/>.
    /// </exception>
    public LayerWriter<T> Batch<T>(RenderLayerId id, bool required = true) where T : struct
        => _renderer.OpenBatch<T>(id, required);

    /// <summary>
    /// Determines whether the specified render layer is currently active.
    /// </summary>
    /// <param name="id">The ID of the render layer to check.</param>
    /// <returns><c>true</c> if the render layer is active; otherwise, <c>false</c>.</returns>
    public bool IsActive(RenderLayerId id) => _renderer.IsActiveLayer(id);
}
