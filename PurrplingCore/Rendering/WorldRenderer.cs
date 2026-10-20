using Microsoft.Xna.Framework.Graphics;
using PurrplingCore.Ecs.WorldBuilding;
using PurrplingCore.Toolkit.Rendering;

namespace PurrplingCore.Rendering;

/// <summary>
/// Owns a world's renderer, begins its frame before draw systems, and flushes afterward.
/// </summary>
public sealed class WorldRenderer : IDisposable
{
    private IGraphicsDeviceService _graphics;
    private bool _disposed;

    /// <summary>
    /// Takes ownership of a renderer that will be driven by the world lifecycle.
    /// </summary>
    public WorldRenderer(IRenderer renderer, IGraphicsDeviceService graphics)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        ArgumentNullException.ThrowIfNull(graphics);
        Renderer = renderer;
        _graphics = graphics;
    }

    /// <summary>
    /// Gets the renderer owned by this world, not an application-wide renderer.
    /// </summary>
    public IRenderer Renderer { get; }

    /// <summary>
    /// Gets the submission context used by the world's draw systems.
    /// </summary>
    public RenderContext Context => Renderer.Context;

    internal void Attach(IWorldBlueprint builder)
    {
        builder.OnInitialized(OnInitialized);
        builder.OnDrawing(OnDrawing);
        builder.OnDrawn(OnDrawn);
    }

    private void OnInitialized(WorldInitializedContext context)
        => Renderer.Initialize(_graphics.GraphicsDevice);

    private void OnDrawing(WorldDrawingContext context)
        => Renderer.BeginFrame();

    private void OnDrawn(WorldDrawnContext context)
        => Renderer.Flush();

    /// <summary>
    /// Releases only this world's renderer resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Renderer.Dispose();
    }
}