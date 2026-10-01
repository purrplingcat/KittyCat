using System;
using System.Collections.Generic;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using KittyCat.Configuration;
using KittyCat.Ecs.Components;
using Microsoft.Extensions.Options;
using PurrplingCore.Ecs;
using PurrplingCore.Ecs.Components;

namespace KittyCat.Ecs.Systems;

/// <summary>
/// Collects overlapping stars, applies their effects, and advances score and strike.
/// </summary>
public sealed class StarCollectionSystem : QuerySystem<ArcadeTransform, PlayerState>
{
    private readonly Dictionary<EntityStore, ArchetypeQuery<ArcadeTransform, FallingStar, ArcadeVisual>> _starQueries = [];
    private readonly PaddleOptions _paddleOptions;

    public StarCollectionSystem(IOptions<StarCatcherOptions> options)
    {
        _paddleOptions = options.Value.Paddle;
    }

    protected override void OnAddStore(EntityStore store)
    {
        _starQueries.Add(store, store.Query<ArcadeTransform, FallingStar, ArcadeVisual>());
        base.OnAddStore(store);
    }

    protected override void OnRemoveStore(EntityStore store)
    {
        _starQueries.Remove(store);
        base.OnRemoveStore(store);
    }

    protected override void OnUpdate()
    {
        var stars = _starQueries[Query.Store];
        foreach (var (playerTransforms, players, playerEntities) in Query.Chunks)
        {
            for (var playerIndex = 0; playerIndex < playerEntities.Length; playerIndex++)
            {
                ref var playerTransform = ref playerTransforms[playerIndex];
                ref var player = ref players[playerIndex];
                if (player.Lives <= 0) continue;

                var collectedStars = 0;

                foreach (var (starTransforms, starComponents, visuals, starEntities) in stars.Chunks)
                {
                    for (var starIndex = 0; starIndex < starEntities.Length; starIndex++)
                    {
                        ref var star = ref starComponents[starIndex];
                        if (!star.IsActive) continue;

                        ref var starTransform = ref starTransforms[starIndex];
                        if (!playerTransform.Intersects(in starTransform)) continue;

                        collectedStars++;
                        ApplyPaddleEffect(ref playerTransform, ref player, star.PaddleEffect);
                        star.IsActive = false;
                        visuals[starIndex].IsVisible = false;
                        var entityId = starEntities.EntityAt(starIndex).Id;
                        // Queue entity changes because this code is inside a query-chunk loop.
                        CommandBuffer.AddTag<PendingDestroy>(entityId);
                        CommandBuffer.AddTag<Disabled>(entityId);
                    }
                }

                player.Score += collectedStars;
                // Strike is the consecutive-star count and is reset when any star is missed.
                player.Strike += collectedStars;
            }
        }
    }

    private void ApplyPaddleEffect(
        ref ArcadeTransform transform,
        ref PlayerState player,
        PaddleSizeEffect effect)
    {
        if (effect == PaddleSizeEffect.None) return;

        player.PaddleEffect = effect;
        player.PaddleEffectTime = _paddleOptions.EffectDurationSeconds;
        var width = effect == PaddleSizeEffect.Expand
            ? transform.Size.X + 16
            : _paddleOptions.Width * _paddleOptions.ShrinkMultiplier;

        transform.Size.X = Math.Clamp(width, 16, 128);
    }
}