using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using PurrplingCore.Ecs.DI;
using PurrplingCore.Ecs.Particles.Systems;
using PurrplingCore.Ecs.Systems;
using PurrplingCore.Ecs.WorldBuilding;

namespace PurrplingCore.Ecs.Particles;

/// <summary>
/// Configures particle simulation for one world marker.
/// </summary>
public sealed class ParticleOptions
{
    /// <summary>
    /// Gets or sets the simulation speed multiplier. Zero pauses particle updates.
    /// </summary>
    public float TimeScale { get; set; } = 1f;
}

internal sealed class ParticleOptionsValidator : IValidateOptions<ParticleOptions>
{
    public ValidateOptionsResult Validate(string? name, ParticleOptions options)
    {
        return float.IsFinite(options.TimeScale) && options.TimeScale >= 0f
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail("Particle TimeScale must be finite and nonnegative.");
    }
}

/// <summary>
/// Adds renderer-independent particle simulation and its world-specific options.
/// </summary>
public sealed class ParticlesFeature : IWorldFeature
{
    /// <summary>
    /// Registers particle simulation for the configured world marker.
    /// </summary>
    public void ConfigureServices(IWorldServicesBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddOptions<ParticleOptions>();
        builder.Services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<ParticleOptions>, ParticleOptionsValidator>());
        builder.AddModule<ParticleModule>();
    }

    private sealed class ParticleModule : IWorldModule
    {
        public int Order => 0;

        public void Setup(IWorldBlueprint world)
        {
            world.Systems
                .In<UpdateSystemGroup>()
                .Add<ParticleUpdateSystem>();
        }
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
        return builder.AddFeature<ParticlesFeature>();
    }
}
