using Friflo.Engine.ECS.Systems;
using PurrplingCore.Ecs.Attributes;
using PurrplingCore.Ecs.Systems;
using PurrplingCore.Toolkit.Particles;
using PurrplingCore.Toolkit.Rendering;

namespace PurrplingCore.Ecs.Particles.Systems;

/// <summary>
/// Provides a world-owned particle query. Game wrappers choose their drawing backend.
/// </summary>
[System<DrawSystemGroup>]
public abstract class ParticleDrawSystem : QuerySystem<Particle>
{
    /// <summary>
    /// Optionally queues this world's particles into the layer selected by the wrapper.
    /// </summary>
    protected void Draw(in LayerWriter<DrawCommand> writer)
    {
        if (!writer.IsActive) return;

        foreach (var (particles, entities) in Query.Chunks)
        {
            for (var index = 0; index < entities.Length; index++)
            {
                ParticleRenderer2D.Draw(in particles[index].State, in writer);
            }
        }
    }
}