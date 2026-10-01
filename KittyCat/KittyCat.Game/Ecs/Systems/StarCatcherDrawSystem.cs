using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using KittyCat.Ecs.Components;
using KittyCat.Services;
using PurrplingCore.Ecs.Attributes;
using PurrplingCore.Ecs.Systems;

namespace KittyCat.Ecs.Systems;

/// <summary>
/// Queues visible entities for each store attached to the world.
/// </summary>
[System<DrawSystemGroup>]
public sealed class StarCatcherDrawSystem : QuerySystem<ArcadeTransform, ArcadeVisual>
{
    private readonly StarCatcherRenderer _renderer;

    public StarCatcherDrawSystem(StarCatcherRenderer renderer)
    {
        _renderer = renderer;
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
    }
}