using Microsoft.Xna.Framework;
using PurrplingCore.Graphics;
using PurrplingCore.Toolkit.Rendering;

namespace PurrplingCore.Rendering;

/// <summary>
/// Initializes and flushes one keyed renderer at a chosen draw order.
/// </summary>
public sealed class RendererGameComponent : DrawableGameComponent
{
    private readonly IRenderer _renderer;
    private readonly GraphicsManager _graphics;

    public RendererGameComponent(
        Toolkit.Game game,
        IRenderer renderer,
        GraphicsManager graphics,
        int drawOrder) : base(game)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        ArgumentNullException.ThrowIfNull(graphics);
        _renderer = renderer;
        _graphics = graphics;
        DrawOrder = drawOrder;
    }

    public override void Initialize()
    {
        base.Initialize();
        _renderer.Initialize(_graphics.GraphicsDevice);
    }

    public override void Draw(GameTime gameTime)
    {
        if (_renderer is Renderer2D renderer)
        {
            renderer.VirtualResolution = _graphics.Resolution.Size;
        }

        _renderer.Flush();

        base.Draw(gameTime);
    }
}