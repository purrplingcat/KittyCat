using Microsoft.Xna.Framework.Graphics;

namespace PurrplingCore.Toolkit.Rendering;

/// <summary>
/// Everything a post-processor needs for one step of a chain.
/// </summary>
/// <remarks>
/// Post-processing is not bound to a layer, so this carries only the images and the batching
/// facilities. The destination is already bound when the processor runs; it is exposed so a
/// processor can rebind after drawing into an intermediate target of its own.
/// </remarks>
public readonly ref struct PostProcessContext
{
    public PostProcessContext(
        GraphicsDevice device,
        ISpriteBatcher batcher,
        RenderResourceTable resources,
        FrameTextBuffer text,
        RenderTarget2D source,
        RenderTarget2D? destination)
    {
        Device = device;
        Batcher = batcher;
        Resources = resources;
        Text = text;
        Source = source;
        Destination = destination;
    }

    public GraphicsDevice Device { get; }

    public ISpriteBatcher Batcher { get; }

    public RenderResourceTable Resources { get; }

    public FrameTextBuffer Text { get; }

    /// <summary>
    /// Image produced by the previous step, or by the layer or renderer being processed.
    /// </summary>
    public RenderTarget2D Source { get; }

    /// <summary>
    /// Target the result must be written to. Null means the back buffer.
    /// </summary>
    public RenderTarget2D? Destination { get; }
}

/// <summary>
/// Transforms an already rendered image. Processors form a chain where each step reads the
/// previous result.
/// </summary>
public interface IPostProcessor : IDisposable
{
    /// <summary>
    /// Creates GPU resources. Called once, never on the per-frame path.
    /// </summary>
    void Initialize(GraphicsDevice device);

    /// <summary>
    /// Reads <see cref="PostProcessContext.Source"/> and writes the processed image.
    /// </summary>
    void Process(in PostProcessContext context);
}
