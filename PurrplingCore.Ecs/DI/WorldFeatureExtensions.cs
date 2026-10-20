using Microsoft.Extensions.DependencyInjection;

namespace PurrplingCore.Ecs.DI;

/// <summary>
/// Applies composable feature registrations to configured world markers.
/// </summary>
public static class WorldFeatureExtensions
{
    private sealed class FeatureRegistration<TFeature> where TFeature : IWorldFeature;

    /// <summary>
    /// Applies a feature's registrations once per world marker before the service provider is built.
    /// </summary>
    public static IWorldServicesBuilder AddFeature<TFeature>(this IWorldServicesBuilder builder)
        where TFeature : class, IWorldFeature, new()
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (builder.Services.Any(descriptor =>
            descriptor.IsKeyedService && descriptor.ServiceType == typeof(FeatureRegistration<TFeature>) &&
            Equals(descriptor.ServiceKey, builder.Key)))
        {
            return builder;
        }

        builder.Services.AddKeyedSingleton(builder.Key, new FeatureRegistration<TFeature>());
        new TFeature().ConfigureServices(builder);
        return builder;
    }
}