using Friflo.Engine.ECS;
using PurrplingCore.Toolkit.Particles;

namespace PurrplingCore.Ecs.Particles;

/// <summary>
/// Adapts low-level particle state to a world-owned ECS component.
/// </summary>
public struct Particle : IComponent
{
    public ParticleState State;

    public Particle(in ParticleEmission emission)
    {
        State = new ParticleState(emission);
    }
}

/// <summary>
/// Emits particles without modifying an ECS store during query iteration.
/// </summary>
public static class ParticleCommandBufferExtensions
{
    /// <summary>
    /// Queues one particle entity and returns its ID. The entity exists only after playback.
    /// </summary>
    public static int EmitParticle(this CommandBuffer commands, in ParticleEmission emission)
    {
        var entityId = commands.CreateEntity();
        commands.AddComponent(entityId, new Particle(emission));
        return entityId;
    }
}