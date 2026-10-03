using System;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using KittyCat.Configuration;
using KittyCat.Ecs.Components;
using KittyCat.Services;
using Microsoft.Extensions.Options;
using Microsoft.Xna.Framework;
using PurrplingCore.Ecs;
using PurrplingCore.Ecs.Components;

namespace KittyCat.Ecs.Systems;

/// <summary>
/// Collects overlapping stars, applies their effects, and advances score and strike.
/// </summary>
public sealed partial class StarCollectionSystem : QuerySystem<ArcadeTransform, PlayerState>
{
    private readonly PaddleOptions _paddleOptions;
    private readonly StarCatcherParticleEffects _particles;
    private ArcadeTransform _collectingPlayerTransform;
    private PlayerState _collectingPlayer;
    private int _collectedStars;

    public StarCollectionSystem(IOptions<StarCatcherOptions> options, StarCatcherParticleEffects particles)
    {
        _paddleOptions = options.Value.Paddle;
        _particles = particles;
    }

    protected override void OnUpdate()
    {
        foreach (var (playerTransforms, players, playerEntities) in Query.Chunks)
        {
            for (var playerIndex = 0; playerIndex < playerEntities.Length; playerIndex++)
            {
                ref var playerTransform = ref playerTransforms[playerIndex];
                ref var player = ref players[playerIndex];
                if (player.Lives <= 0) continue;

                // Keep mutations between star callbacks, then write the player back after query iteration.
                _collectingPlayerTransform = playerTransform;
                _collectingPlayer = player;
                _collectedStars = 0;
                CollectStarsQuery(Query.Store);

                _collectingPlayer.Score += _collectedStars;
                // A miss in FallingStarSystem resets this consecutive collection count.
                _collectingPlayer.Strike += _collectedStars;
                playerTransform = _collectingPlayerTransform;
                player = _collectingPlayer;
            }
        }
    }

    [Query]
    private void CollectStars(ref ArcadeTransform transform, ref FallingStar star, ref ArcadeVisual visual, Entity entity)
    {
        if (!star.IsActive || !_collectingPlayerTransform.Intersects(in transform)) return;

        var color = star.PaddleEffect switch
        {
            PaddleSizeEffect.Expand => new Color(80, 235, 125),
            PaddleSizeEffect.Shrink => new Color(245, 75, 85),
            _ => Color.Gold,
        };
        _particles.Burst(CommandBuffer, transform.Position + transform.Size * 0.5f, color);
        _collectedStars++;
        ApplyPaddleEffect(ref _collectingPlayerTransform, ref _collectingPlayer, star.PaddleEffect);
        star.IsActive = false;
        visual.IsVisible = false;
        // Queue entity changes because this code is inside a query-chunk loop.
        CommandBuffer.AddTag<PendingDestroy>(entity.Id);
        CommandBuffer.AddTag<Disabled>(entity.Id);
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