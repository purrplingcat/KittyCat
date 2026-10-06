using PurrplingCore.Ecs.DI;
using PurrplingCore.Ecs.Systems;
using PurrplingCore.Ecs.WorldBuilding;

namespace PurrplingCore.Ecs.Particles;

/// <summary>
/// Adds particle simulation and lifetime management without choosing a renderer.
/// </summary>
public sealed class ParticleModule : IWorldModule
{
    public int Order => 0;

    public void Setup(IWorldBlueprint world)
    {
        world.Systems
            .In<UpdateSystemGroup>()
            .Add<ParticleUpdateSystem>();
    }
}

/// <summary>
/// Opt-in registration of renderer-independent particle simulation.
/// </summary>
public static class ParticleWorldServicesBuilderExtensions
{
    /// <summary>
    /// Adds particle simulation to this world. Emission and rendering remain separate opt-ins.
    /// </summary>
    public static IWorldServicesBuilder AddParticleModule(this IWorldServicesBuilder builder)
    {
        return builder.AddModule<ParticleModule>();
    }
}
