using System;
using System.Collections.Generic;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using KittyCat.Configuration;
using KittyCat.Ecs.Components;
using Microsoft.Extensions.Options;
using Microsoft.Xna.Framework;
using PurrplingCore.Ecs;
using PurrplingCore.Ecs.Attributes;
using PurrplingCore.Ecs.Systems;

namespace KittyCat.Ecs.Systems;

[System<UpdateSystemGroup>]
public sealed class FallingStarSystem : QuerySystem<ArcadeTransform, FallingStar, ArcadeVisual>
{
    private readonly Dictionary<EntityStore, ArchetypeQuery<PlayerState>> _playerQueries = [];
    private readonly PlayfieldOptions _playfield;

    public FallingStarSystem(IOptions<StarCatcherOptions> options)
    {
        _playfield = options.Value.Playfield;
    }

    protected override void OnAddStore(EntityStore store)
    {
        _playerQueries.Add(store, store.Query<PlayerState>());
        base.OnAddStore(store);
    }

    protected override void OnRemoveStore(EntityStore store)
    {
        _playerQueries.Remove(store);
        base.OnRemoveStore(store);
    }

    protected override void OnUpdate()
    {
        var missedStars = 0;
        foreach (var (transforms, stars, visuals, entities) in Query.Chunks)
        {
            for (var index = 0; index < entities.Length; index++)
            {
                ref var transform = ref transforms[index];
                ref var star = ref stars[index];
                ref var visual = ref visuals[index];
                if (!star.IsActive) continue;

                transform.Position.Y += star.FallSpeed * Tick.deltaTime;
                if (transform.Position.Y <= _playfield.Height) continue;

                star.IsActive = false;
                visual.IsVisible = false;
                if (star.PaddleEffect == PaddleSizeEffect.None) missedStars++;
            }
        }

        if (missedStars == 0) return;

        var playerQuery = _playerQueries[Query.Store];
        foreach (var (players, entities) in playerQuery.Chunks)
        {
            for (var index = 0; index < entities.Length; index++)
            {
                ref var player = ref players[index];
                if (player.Lives > 0)
                {
                    player.Lives = Math.Max(0, player.Lives - missedStars);
                }
            }
        }
    }
}