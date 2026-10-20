using PurrplingCore.Ecs.Attributes;
using PurrplingCore.Ecs.DI;
using PurrplingCore.Ecs.Systems;
using PurrplingCore.Toolkit.Rendering;

namespace PurrplingCore.Ecs.Particles.Systems;

[System<DrawSystemGroup>]
internal sealed class DefaultParticleDrawSystem : ParticleDrawSystem
{
    private readonly RenderContext _context;
    private readonly RenderLayerId _layer;

    public DefaultParticleDrawSystem(
        RenderContext context,
        IWorldOptions<ParticleRenderingOptions> options)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(options);
        _context = context;
        _layer = options.Value.Layer;
        _context.Batch(_layer, required: true);
    }

    protected override void OnUpdate()
    {
        var writer = _context.Batch(_layer, required: true);
        Draw(in writer);
    }
}