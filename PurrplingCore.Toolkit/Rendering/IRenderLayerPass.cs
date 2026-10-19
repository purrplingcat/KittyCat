using Microsoft.Xna.Framework.Graphics;

namespace PurrplingCore.Toolkit.Rendering;

/// <summary>
/// Executes one render layer using its own drawing strategy and optional submission data.
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
/// Manages frame-scoped data owned by a render pass.
/// </summary>
public interface IFrameSubmissionPass
{
    /// <summary>
    /// Total values queued for the current frame across every supported submission type.
    /// </summary>
    int SubmissionCount { get; }

    /// <summary>
    /// Discards pending submissions of every supported type. Called once per layer at frame reset.
    /// </summary>
    void ClearSubmissions();
}

/// <summary>
/// A render pass that owns and accepts frame-scoped data of type <typeparamref name="T"/>.
/// </summary>
/// <remarks>
/// A pass may implement this interface for multiple types, sharing one frame reset and total count.
/// </remarks>
public interface ILayerSubmissionPass<T> : IRenderLayerPass, IFrameSubmissionPass where T : struct
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
