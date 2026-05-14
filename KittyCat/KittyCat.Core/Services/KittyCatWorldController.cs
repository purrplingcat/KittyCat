using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using KittyCat.Extensions;
using Microsoft.Xna.Framework;
using PurrplingCore.Ecs;
using PurrplingCore.Ecs.Systems;
using PurrplingCore.Toolkit.DI;
using PurrplingCore.Toolkit.Rendering;
using System;

namespace KittyCat.Services;

[Singleton]
public class WorldController : DrawableGameComponent
{
    private bool _disposed;
    private bool _initialized;
    private World _world;

    public WorldController(Game game, IWorldFactory worldFactory) : base(game)
    {
        ArgumentNullException.ThrowIfNull(worldFactory);
        _world = worldFactory.CreateWorld("MainWorld", WorldSignature.Default);
    }

    public override void Initialize()
    {
        if (_initialized) return;

        _world.Initialize();
        _initialized = true;

        //Game.Content.Load<string>("test");
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
        GraphicsDevice.Clear(Color.MonoGameOrange);
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
