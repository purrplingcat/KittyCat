using System;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using KittyCat.Configuration;
using KittyCat.Ecs.Components;
using KittyCat.Services;
using Microsoft.Extensions.Options;
using Microsoft.Xna.Framework;
using PurrplingCore.Ecs;
using PurrplingCore.Ecs.Attributes;
using PurrplingCore.Ecs.Components;
using PurrplingCore.Ecs.Systems;

namespace KittyCat.Ecs.Systems;

/// <summary>
/// Moves active stars and applies miss penalties when they reach the ground.
/// </summary>
[System<UpdateSystemGroup>]
public sealed partial class FallingStarSystem : QuerySystem<ArcadeTransform, FallingStar, ArcadeVisual>
{
    private readonly PlayfieldOptions _playfield;
    private readonly StarCatcherParticleEffects _particles;

    public FallingStarSystem(IOptions<StarCatcherOptions> options, StarCatcherParticleEffects particles)
    {
        _playfield = options.Value.Playfield;
        _particles = particles;
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
                if (transform.Position.Y + transform.Size.Y < _playfield.GroundTop) continue;

                var color = star.PaddleEffect switch
                {
                    PaddleSizeEffect.Expand => new Color(80, 235, 125),
                    PaddleSizeEffect.Shrink => new Color(245, 75, 85),
                    _ => Color.Gold,
                };
                _particles.Shatter(CommandBuffer,
                    new Vector2(transform.Position.X + transform.Size.X * 0.5f, _playfield.GroundTop), color);
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
        PenalizeMissedStarsQuery(Query.Store, missedLifeStar);
    }

    [Query]
    private static void PenalizeMissedStars(ref PlayerState player, bool missedLifeStar)
    {
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