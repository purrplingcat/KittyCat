using Microsoft.Xna.Framework.Graphics;

namespace PurrplingCore.Toolkit.Rendering;

/// <summary>
/// Executes one layer of a renderer. Implement this to draw a layer in a way the sprite
/// command queue cannot express, such as a tilemap mesh or a full-screen effect.
/// </summary>
/// <remarks>
/// Allocate in <see cref="Initialize"/> and release in <see cref="IDisposable.Dispose"/>.
/// <see cref="Prepare"/> and <see cref="Execute"/> run every frame and must not allocate.
/// </remarks>
public interface IRenderLayerPass : IDisposable
{
    /// <summary>
    /// Creates GPU resources once, before the first frame.
    /// </summary>
    void Initialize(GraphicsDevice device);

    /// <summary>
    /// Runs before any layer executes, for work that must precede all drawing.
    /// </summary>
    void Prepare(in RenderFrameContext context);

    /// <summary>
    /// Draws the layer.
    /// </summary>
    void Execute(in RenderFrameContext context);
}

/// <summary>
/// Clears data submitted to a pass when its renderer starts another frame.
/// </summary>
public interface IFrameSubmissionPass
{
    /// <summary>
    /// Discards pending submissions.
    /// </summary>
    void ClearSubmissions();
}

/// <summary>
/// Accepts frame-scoped data of type <typeparamref name="T"/> through a layer writer.
/// </summary>
public interface ILayerSubmissionPass<T> : IFrameSubmissionPass where T : struct
{
    /// <summary>
    /// Queues data for this pass to draw when the renderer flushes.
    /// </summary>
    void Submit(in T submission);
}

/// <summary>
/// Base class for passes that only need to override <see cref="Execute"/>.
/// </summary>
public abstract class RenderLayerPass : IRenderLayerPass
{
    public virtual void Initialize(GraphicsDevice device)
    {
    }

    public virtual void Prepare(in RenderFrameContext context)
    {
    }

    public abstract void Execute(in RenderFrameContext context);

    protected virtual void Dispose(bool disposing)
    {
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
