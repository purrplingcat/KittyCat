using Microsoft.Xna.Framework;

namespace PurrplingCore.Rendering;

/// <summary>
/// Flushes one opt-in group of renderers in draw order.
/// </summary>
public sealed class RendererGameComponent : DrawableGameComponent
{
    private readonly RendererGroup _group;

    public RendererGameComponent(
        Toolkit.Game game,
        RendererGroup group,
        int drawOrder) : base(game)
    {
        ArgumentNullException.ThrowIfNull(group);
        _group = group;
        DrawOrder = drawOrder;
    }

    public override void Initialize()
    {
        base.Initialize();
        _group.Initialize();
    }

    public override void Draw(GameTime gameTime)
    {
        _group.Flush();

        base.Draw(gameTime);
    }
}