using Friflo.Engine.ECS.Systems;
using PurrplingCore.Ecs.Attributes;
using PurrplingCore.Ecs.DI;
using PurrplingCore.Ecs.Systems;

namespace PurrplingCore.Ecs.Particles.Systems;

/// <summary>
/// Advances particle motion and lifetime, deferring entity deletion until query iteration finishes.
/// </summary>
[System<UpdateSystemGroup>]
public sealed class ParticleUpdateSystem : QuerySystem<Particle>
{
    private readonly float _timeScale;

    /// <summary>
    /// Creates a standalone particle update system using the default simulation speed.
    /// </summary>
    public ParticleUpdateSystem() : this(1f)
    {
    }

    /// <summary>
    /// Creates a particle update system configured for its current world.
    /// </summary>
    public ParticleUpdateSystem(IWorldOptions<ParticleOptions> options)
        : this(options?.Value.TimeScale ?? throw new ArgumentNullException(nameof(options)))
    {
    }

    private ParticleUpdateSystem(float timeScale)
    {
        _timeScale = timeScale;
    }

    protected override void OnUpdate()
    {
        var deltaTime = Tick.deltaTime;
        if (!float.IsFinite(deltaTime) || deltaTime < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(deltaTime));
        }

        var scaledDeltaTime = deltaTime * _timeScale;
        if (!float.IsFinite(scaledDeltaTime))
        {
            throw new ArgumentOutOfRangeException(nameof(deltaTime), "Scaled particle delta time must remain finite.");
        }

        if (_timeScale == 0f) return;

        foreach (var (particles, entities) in Query.Chunks)
        {
            for (var index = 0; index < entities.Length; index++)
            {
                if (!particles[index].State.Update(scaledDeltaTime))
                {
                    CommandBuffer.DeleteEntity(entities.EntityAt(index).Id);
                }
            }
        }
    }
}