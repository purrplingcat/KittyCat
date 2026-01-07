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
public class KittyCatWorldController(Game game, World world, SystemRoot systemRoot, RenderPipeline renderer) : WorldController(game, world, systemRoot, renderer)
{
    private readonly World _world = world;

    protected override void LoadContent()
    {
        _world.CreateStore("MainStore");
    }
}

public class WorldController(Game game, World world, SystemRoot systemRoot, RenderPipeline renderer) : DrawableGameComponent(game)
{
    private readonly SystemRoot _systemRoot = systemRoot;
    private readonly RenderPipeline _renderer = renderer;
    private readonly World _world = world;
    private SystemWorldBinding? _binding;
    private bool _disposed;
    private bool _initialized;

    public override void Initialize()
    {
        if (_initialized) return; 
        
        _initialized = true;
        _binding = _systemRoot.CreateBinding(_world);
        _systemRoot.Initialize();
        // TODO: Initialize world renderer

        base.Initialize();
    }

    public override void Update(GameTime gameTime)
    {
        _world.CurrentStore.EventRecorder.ClearEvents();
        _systemRoot.Update(gameTime.ToUpdateTick());
        UpdateEntityScripts(_world.CurrentStore);

        _renderer.Prepare(gameTime);
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
        _renderer.Draw(gameTime);
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
