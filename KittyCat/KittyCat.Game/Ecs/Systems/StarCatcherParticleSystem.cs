using System;
using Friflo.Engine.ECS.Systems;
using KittyCat.Ecs.Components;
using KittyCat.Services;
using Microsoft.Xna.Framework;
using PurrplingCore.Ecs.Attributes;
using PurrplingCore.Ecs.Systems;

namespace KittyCat.Ecs.Systems;

/// <summary>
/// Emits collectible trails into each ECS store at a simulation-time-controlled rate.
/// </summary>
[System<UpdateSystemGroup>]
public sealed class StarCatcherParticleSystem(StarCatcherParticleEffects effects)
    : QuerySystem<ArcadeTransform, ArcadeVisual>
{
    private float _trailTime;
    private int _trailCount;

    protected override void OnUpdateGroupBegin()
    {
        _trailTime += Math.Min(Tick.deltaTime, 0.1f);
        _trailCount = (int)(_trailTime * 30f);
        _trailTime -= _trailCount / 30f;
    }

    protected override void OnUpdate()
    {
        if (_trailCount == 0) return;

        foreach (var (transforms, visuals, entities) in Query.Chunks)
        {
            for (var index = 0; index < entities.Length; index++)
            {
                ref readonly var visual = ref visuals[index];
                if (!visual.IsVisible) continue;

                var color = visual.Kind switch
                {
                    ArcadeVisualKind.Star => Color.Gold,
                    ArcadeVisualKind.GreenStar => new Color(80, 235, 125),
                    ArcadeVisualKind.RedStar => new Color(245, 75, 85),
                    ArcadeVisualKind.Heart => new Color(255, 90, 145),
                    _ => Color.Transparent,
                };
                if (color.A == 0) continue;

                var position = transforms[index].Position + transforms[index].Size * 0.5f;
                for (var trailIndex = 0; trailIndex < _trailCount; trailIndex++)
                {
                    effects.Trail(CommandBuffer, position, color);
                }
            }
        }
    }
}