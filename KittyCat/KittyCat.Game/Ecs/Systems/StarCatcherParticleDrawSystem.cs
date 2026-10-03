using KittyCat.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
    [FromKeyedServices(KittyCatRendererKey.World)] RenderContext context)
    : ParticleDrawSystem
{
    protected override void OnUpdate()
    {
        var writer = context.Batch(KittyCatLayers.Effects);
        Draw(in writer);
    }
}
