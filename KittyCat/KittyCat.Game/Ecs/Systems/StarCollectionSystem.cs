using System;
using System.Collections.Generic;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using KittyCat.Configuration;
using KittyCat.Ecs.Components;
using Microsoft.Extensions.Options;
using Microsoft.Xna.Framework;
using PurrplingCore.Ecs;

namespace KittyCat.Ecs.Systems;

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

                var playerPosition = playerTransform.Position;
                var playerSize = playerTransform.Size;
                var collectedStars = 0;

                foreach (var (starTransforms, starComponents, visuals, starEntities) in stars.Chunks)
                {
                    for (var starIndex = 0; starIndex < starEntities.Length; starIndex++)
                    {
                        ref var star = ref starComponents[starIndex];
                        if (!star.IsActive) continue;

                        ref var starTransform = ref starTransforms[starIndex];
                        if (!Intersects(playerPosition, playerSize, starTransform)) continue;

                        collectedStars++;
                        ApplyPaddleEffect(ref playerTransform, ref player, star.PaddleEffect);
                        star.IsActive = false;
                        visuals[starIndex].IsVisible = false;
                    }
                }

                player.Score += collectedStars;
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
        var multiplier = effect == PaddleSizeEffect.Expand
            ? _paddleOptions.ExpandMultiplier
            : _paddleOptions.ShrinkMultiplier;
        transform.Size.X = player.BasePaddleWidth * multiplier;
    }

    private static bool Intersects(Vector2 firstPosition, Vector2 firstSize, ArcadeTransform second)
    {
        return firstPosition.X < second.Position.X + second.Size.X
            && firstPosition.X + firstSize.X > second.Position.X
            && firstPosition.Y < second.Position.Y + second.Size.Y
            && firstPosition.Y + firstSize.Y > second.Position.Y;
    }
}