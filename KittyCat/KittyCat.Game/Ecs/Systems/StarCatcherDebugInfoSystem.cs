using System;
using Friflo.Engine.ECS.Systems;
using PurrplingCore.Ecs.Attributes;
using PurrplingCore.Ecs.Systems;
using PurrplingCore.Toolkit;
using PurrplingGame = PurrplingCore.Toolkit.Game;

namespace KittyCat.Ecs.Systems;

/// <summary>Writes FPS and ECS entity count to the window title for development diagnostics.</summary>
[System<UpdateSystemGroup>]
public sealed class StarCatcherDebugInfoSystem : QuerySystem
{
    private readonly PurrplingGame _game;
    private readonly IGameClock _gameClock;
    private int _lastFps = -1;
    private int _lastEntityCount = -1;

    public StarCatcherDebugInfoSystem(PurrplingGame game, IGameClock gameClock)
    {
        _game = game;
        _gameClock = gameClock;
    }

    protected override void OnUpdate()
    {
        var fps = (int)Math.Round(_gameClock.Fps);
        var entityCount = Query.Store.Count;
        if (fps == _lastFps && entityCount == _lastEntityCount) return;

        _game.Window.Title = $"FPS: {fps} | ECS entities: {entityCount}";
        _lastFps = fps;
        _lastEntityCount = entityCount;
    }
}
