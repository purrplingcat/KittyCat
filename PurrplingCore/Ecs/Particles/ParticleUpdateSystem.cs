using Friflo.Engine.ECS.Systems;
using PurrplingCore.Ecs.Attributes;
using PurrplingCore.Ecs.Systems;

namespace PurrplingCore.Ecs.Particles;

/// <summary>
/// Advances particle motion and lifetime, deferring entity deletion until query iteration finishes.
/// </summary>
[System<UpdateSystemGroup>]
public sealed class ParticleUpdateSystem : QuerySystem<Particle>
{
    protected override void OnUpdate()
    {
        var deltaTime = Tick.deltaTime;
        if (!float.IsFinite(deltaTime) || deltaTime < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(deltaTime));
        }

        foreach (var (particles, entities) in Query.Chunks)
        {
            for (var index = 0; index < entities.Length; index++)
            {
                if (!particles[index].State.Update(deltaTime))
                {
                    CommandBuffer.DeleteEntity(entities.EntityAt(index).Id);
                }
            }
        }
    }
}