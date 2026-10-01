using System.Collections.Generic;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using KittyCat.Ecs.Components;
using KittyCat.Services;
using PurrplingCore.Ecs;
using PurrplingCore.Ecs.Attributes;
using PurrplingCore.Ecs.Systems;

namespace KittyCat.Ecs.Systems;

/// <summary>Draws visible entities and the HUD for each store attached to the world.</summary>
[System<DrawSystemGroup>]
public sealed class StarCatcherDrawSystem : QuerySystem<ArcadeTransform, ArcadeVisual>
{
    private readonly Dictionary<EntityStore, ArchetypeQuery<PlayerState>> _playerQueries = [];
    private readonly StarCatcherRenderer _renderer;

    public StarCatcherDrawSystem(StarCatcherRenderer renderer)
    {
        _renderer = renderer;
    }

    protected override void OnAddStore(EntityStore store)
    {
        // Keep the HUD query bound to this store instead of creating it during each draw.
        _playerQueries.Add(store, store.Query<PlayerState>());
        base.OnAddStore(store);
    }

    protected override void OnRemoveStore(EntityStore store)
    {
        _playerQueries.Remove(store);
        base.OnRemoveStore(store);
    }

    protected override void OnUpdateGroupBegin()
    {
        _renderer.BeginFrame();
        base.OnUpdateGroupBegin();
    }

    protected override void OnUpdate()
    {
        foreach (var (transforms, visuals, entities) in Query.Chunks)
        {
            for (var index = 0; index < entities.Length; index++)
            {
                _renderer.DrawEntity(transforms[index], visuals[index]);
            }
        }

        var playerQuery = _playerQueries[Query.Store];
        foreach (var (players, entities) in playerQuery.Chunks)
        {
            for (var index = 0; index < entities.Length; index++)
            {
                _renderer.DrawHud(players[index].Score, players[index].Lives, players[index].Strike);
            }
        }
    }

    protected override void OnUpdateGroupEnd()
    {
        _renderer.EndFrame();
        base.OnUpdateGroupEnd();
    }
}