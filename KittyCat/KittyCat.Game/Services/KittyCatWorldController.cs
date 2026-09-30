using System;
using Friflo.Engine.ECS;
using PurrplingCore.Ecs;
using Microsoft.Xna.Framework;
using PurrplingCore.Toolkit.DI;
using PurrplingCore.Toolkit.Rendering;

namespace KittyCat.Services;

[Singleton]
public class WorldController : DrawableGameComponent
{
    private bool _disposed;
    private readonly World _world;

    public WorldController(Game game, IWorldFactory worldFactory) : base(game)
    {
        ArgumentNullException.ThrowIfNull(worldFactory);
        _world = worldFactory.CreateWorld("MainWorld", WorldSignature.Default);
    }

    public override void Initialize()
    {
        _world.Initialize();
        base.Initialize();
    }

    public override void Update(GameTime gameTime)
    {
        _world.Update(gameTime.ToUpdateTick());
        UpdateEntityScripts(_world.Store);
    }

    private static void UpdateEntityScripts(EntityStore store)
    {
        var scripts = store.EntityScripts;
        for (int i = 0; i < scripts.Length; i++)
        {
            foreach (var script in scripts[i])
            {
                script.Update();
            }
        }
    }

    public override void Draw(GameTime gameTime)
    {
        _world.Draw(gameTime.ToUpdateTick());
    }

    protected override void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            _disposed = true;
            _world.Dispose();
        }

        base.Dispose(disposing);
    }
}
