using Microsoft.Xna.Framework.Graphics;

namespace PurrplingCore.Toolkit.Rendering;

/// <summary>
/// Everything a layer pass needs for the frame being flushed.
/// </summary>
/// <remarks>
/// Declared as a ref struct so it cannot be captured or stored, which keeps passes stateless
/// with respect to the frame and prevents accidental heap promotion on the flush path.
/// </remarks>
public readonly ref struct RenderFrameContext
{
    public RenderFrameContext(
        GraphicsDevice device,
        RenderLayer layer,
        ISpriteBatcher batcher,
        RenderResourceTable resources,
        FrameTextBuffer text,
        in BatchState state)
    {
        Device = device;
        Layer = layer;
        Batcher = batcher;
        Resources = resources;
        Text = text;
        State = state;
    }

    public GraphicsDevice Device { get; }

    /// <summary>
    /// The layer currently being executed.
    /// </summary>
    public RenderLayer Layer { get; }

    public ISpriteBatcher Batcher { get; }

    public RenderResourceTable Resources { get; }

    public FrameTextBuffer Text { get; }

    /// <summary>
    /// Graphics state resolved for this layer, including camera and parallax transform.
    /// </summary>
    public BatchState State { get; }
}
