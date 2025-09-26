using Friflo.Engine.ECS.Systems;
using KittyCat.Core.Ecs;
using Microsoft.Xna.Framework;
using PurrplingCore.Toolkit.DI;

namespace KittyCat.Core.Services;

[Singleton]
public class WorldComponent(Game game, World world, SystemRoot systemRoot) : DrawableGameComponent(game)
{
    private readonly SystemRoot _systemRoot = systemRoot;
    private readonly World _world = world;

    public override void Initialize()
    {
        _world.SetSystemRoot(_systemRoot);
        // TODO: Initialize world renderer
    }

    public override void Update(GameTime gameTime)
    {
        // TODO: Handle different update contexts (Active, Paused, ...)
        _world.Update(gameTime, UpdateContext.Active);
    }

    public override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.MonoGameOrange);
        // TODO: Draw world
    }
}
