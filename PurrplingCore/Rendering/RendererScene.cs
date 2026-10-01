using PurrplingCore.Graphics;
using PurrplingCore.Scenes;
using PurrplingCore.Toolkit;
using PurrplingCore.Toolkit.Rendering;

namespace PurrplingCore.Rendering;

/// <summary>
/// Scene that queues its drawing into a renderer instead of its own canvas.
/// </summary>
/// <remarks>
/// Lives in the rendering module rather than in Scenes so scene management keeps working for
/// games that never register a renderer. <see cref="RenderMode"/> is sealed because mixing
/// deferred submission with direct canvas drawing would draw outside an open frame.
/// </remarks>
public abstract class RendererScene : Scene
{
    protected RendererScene(GraphicsManager graphics, IContentManagerProvider contentProvider, IRenderer renderer)
        : base(graphics, contentProvider)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        Context = renderer.Context;
    }

    /// <summary>Entry point for queueing this scene's draw commands.</summary>
    protected RenderContext Context { get; }

    public sealed override SceneRenderMode RenderMode => SceneRenderMode.Deferred;
}
