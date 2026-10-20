using KittyCat.Configuration;
using PurrplingCore.Ecs.Attributes;
using PurrplingCore.Ecs.Particles;
using PurrplingCore.Ecs.Systems;
using PurrplingCore.Toolkit.Rendering;

namespace KittyCat.Ecs.Systems;

/// <summary>
/// Draws this world's particles on the game's effects layer.
/// </summary>
[System<DrawSystemGroup>]
public sealed class StarCatcherParticleDrawSystem(
    RenderContext context)
    : ParticleDrawSystem
{
    protected override void OnUpdate()
    {
        var writer = context.Batch(KittyCatLayers.Effects);
        Draw(in writer);
    }
}
