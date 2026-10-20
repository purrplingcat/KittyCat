using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace PurrplingCore.Ecs.DI;

/// <summary>
/// Registers services owned by individual instances of a configured world.
/// </summary>
public static class WorldServiceExtensions
{
    private sealed class WorldService<TService>(TService value) where TService : class
    {
        public TService Value { get; } = value;

        public static object Resolve(IServiceProvider provider)
        {
            var marker = provider.GetRequiredService<IWorldContext>().Signature.MarkerType;
            return provider.GetRequiredKeyedService<WorldService<TService>>(marker).Value;
        }
    }

    /// <summary>
    /// Registers a service owned by each instance of the configured world.
    /// </summary>
    public static IWorldServicesBuilder AddService<TService>(this IWorldServicesBuilder builder)
        where TService : class
    {
        return builder.AddService<TService, TService>();
    }

    /// <summary>
    /// Registers a world-specific implementation resolved through ordinary constructor injection.
    /// </summary>
    public static IWorldServicesBuilder AddService<TService, TImplementation>(this IWorldServicesBuilder builder)
        where TService : class
        where TImplementation : class, TService
    {
        return builder.AddService<TService>(provider => ActivatorUtilities.CreateInstance<TImplementation>(provider));
    }

    /// <summary>
    /// Registers a service factory executed once in each configured world's scope.
    /// </summary>
    /// <remarks>
    /// The world scope owns disposal of world-created instances. Existing unkeyed registrations are preserved.
    /// </remarks>
    public static IWorldServicesBuilder AddService<TService>(
        this IWorldServicesBuilder builder,
        Func<IServiceProvider, TService> factory)
        where TService : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(factory);

        builder.Services.TryAdd(ServiceDescriptor.Scoped(typeof(TService), WorldService<TService>.Resolve));
        builder.Services.AddKeyedScoped<WorldService<TService>>(
            builder.Key, (provider, _) => new WorldService<TService>(factory(provider)));
        return builder;
    }
}