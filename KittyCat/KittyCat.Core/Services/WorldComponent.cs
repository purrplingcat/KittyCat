using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using KittyCat.Extensions;
using Microsoft.Xna.Framework;
using PurrplingCore.Ecs;
using PurrplingCore.Ecs.Systems;
using PurrplingCore.Toolkit.DI;
using System;

namespace KittyCat.Services;

[Singleton]
public class WorldComponent(Game game, World world, SystemRoot systemRoot) : DrawableGameComponent(game)
{
    private readonly SystemRoot _systemRoot = systemRoot;
    private readonly World _world = world;
    private SystemWorldBinding? _binding;
    private bool _disposed;

    public override void Initialize()
    {
        _binding = _systemRoot.CreateBinding(_world);
        _systemRoot.Initialize();
        // TODO: Initialize world renderer
        
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _world.CreateStore("MainStore");
    }

    public override void Update(GameTime gameTime)
    {
        _world.CurrentStore.EventRecorder.ClearEvents();
        _systemRoot.Update(gameTime.ToUpdateTick());
        UpdateEntityScripts(_world.CurrentStore);
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
        // TODO: Draw world
    }

    protected override void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            _binding?.Dispose();
            _systemRoot.Destroy();
            _binding = null;
            _disposed = true;
        }

        base.Dispose(disposing);
    }
}
