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
using PurrplingCore.Ecs.Components;
using PurrplingCore.Ecs.Systems;

namespace KittyCat.Ecs.Systems;

/// <summary>
/// Moves active stars and applies miss penalties when they leave the playfield.
/// </summary>
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
        // Any missed star breaks the strike; only ordinary stars cost a life.
        var missedLifeStar = false;
        var anyMissedStars = false;
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
                // Defer structural changes until after query iteration completes.
                CommandBuffer.AddTag<PendingDestroy>(entities.EntityAt(index).Id);
                CommandBuffer.AddTag<Disabled>(entities.EntityAt(index).Id);
                anyMissedStars = true;
                if (star.PaddleEffect == PaddleSizeEffect.None) missedLifeStar = true;
            }
        }

        if (!anyMissedStars) return;

        var playerQuery = _playerQueries[Query.Store];
        foreach (var (players, entities) in playerQuery.Chunks)
        {
            for (var index = 0; index < entities.Length; index++)
            {
                ref var player = ref players[index];
                player.Strike = 0;
                // Protect the player from follow-up misses without suppressing strike resets.
                if (player.Lives > 0 && missedLifeStar && player.InvincibilityTime <= 0)
                {
                    player.Lives--;
                    player.InvincibilityTime = PaddleOptions.InvincibilityDurationSeconds;
                    player.InvincibilityBlinkTime = 0;
                }
            }
        }
    }
}