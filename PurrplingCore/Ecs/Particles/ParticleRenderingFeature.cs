using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using PurrplingCore.Ecs.DI;
using PurrplingCore.Ecs.Particles.Systems;
using PurrplingCore.Ecs.WorldBuilding;
using PurrplingCore.Rendering;
using PurrplingCore.Toolkit.Rendering;

namespace PurrplingCore.Ecs.Particles;

/// <summary>
/// Selects the configured layer used by the world's particle drawing system.
/// </summary>
public sealed class ParticleRenderingOptions
{
    /// <summary>
    /// Gets the default layer ID expected by particle rendering.
    /// </summary>
    public static RenderLayerId DefaultLayer { get; } = RenderLayerId.Of("World.Effects");

    /// <summary>
    /// Gets or sets the existing renderer layer that accepts draw commands.
    /// </summary>
    public RenderLayerId Layer { get; set; } = DefaultLayer;
}

internal sealed class ParticleRenderingOptionsValidator : IValidateOptions<ParticleRenderingOptions>
{
    public ValidateOptionsResult Validate(string? name, ParticleRenderingOptions options) =>
        options.Layer.IsValid
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail("Particle rendering requires a valid render layer.");
}

/// <summary>
/// Adds particle simulation and drawing through the renderer context already configured for the world.
/// </summary>
public sealed class ParticleRenderingFeature : IWorldFeature
{
    /// <summary>
    /// Registers the particle draw system and its marker-specific layer options.
    /// </summary>
    public void ConfigureServices(IWorldServicesBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddFeature<ParticlesFeature>();
        builder.Services.AddOptions<ParticleRenderingOptions>();
        builder.Services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<ParticleRenderingOptions>, ParticleRenderingOptionsValidator>());
        builder.Configure(w => w.AddSystem<DefaultParticleDrawSystem>());
    }
}
