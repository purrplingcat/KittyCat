using Friflo.Engine.ECS.Systems;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PurrplingCore.Toolkit.DI;
using PurrplingCore.Ecs.WorldBuilding;

namespace PurrplingCore.Ecs.DI;

/// <summary>
/// Registers ECS world services and configures world factories and modules.
/// </summary>
public static class ServiceCollectionExtensions
{
    private sealed class WorldServicesBuilder(IServiceCollection services, WorldSignature signature) : IWorldServicesBuilder
    {
        public IServiceCollection Services { get; } = services;
        public WorldSignature Signature { get; } = signature;
        public object Key => Signature.MarkerType;
    }

    private sealed class DelegateWorldModule(int order, Action<IWorldBlueprint> setupAction) : IWorldModule
    {
        public int Order { get; } = order;

        public void Setup(IWorldBlueprint builder)
        {
            setupAction(builder);
        }
    }

    /// <summary>
    /// Registers the core scoped context and world factory services.
    /// </summary>
    public static IServiceCollection AddEcsCoreServices(this IServiceCollection services)
    {
        services.TryAddScoped<WorldContext>();
        services.TryAddAlias<IWorldContext, WorldContext>();
        services.TryAddSingleton<IWorldFactory, WorldFactory>();

        return services;
    }

    /// <summary>
    /// Registers a world identified by the specified marker type.
    /// </summary>
    public static IWorldServicesBuilder AddWorld<T>(this IServiceCollection services)
        where T : IWorldMarker
    {
        var signature = WorldSignature.For<T>();
        services.AddEcsCoreServices();
        services.TryAddKeyedSingleton<IWorldFactory, DefaultWorldFactory>(signature.MarkerType);

        return new WorldServicesBuilder(services, signature);
    }

    /// <summary>
    /// Registers the default world.
    /// </summary>
    public static IWorldServicesBuilder AddWorld(this IServiceCollection services)
    {
        var signature = WorldSignature.Default;
        services.AddEcsCoreServices();
        services.TryAddKeyedSingleton<IWorldFactory, DefaultWorldFactory>(signature.MarkerType);

        return new WorldServicesBuilder(services, signature);
    }

    /// <summary>
    /// Registers the default world and applies the specified blueprint configuration.
    /// </summary>
    public static IWorldServicesBuilder AddWorld(this IServiceCollection services, Action<IWorldBlueprint> setupAction)
    {
        return services.AddWorld().Configure(setupAction);
    }

    /// <summary>
    /// Replaces the factory registered for the configured world.
    /// </summary>
    public static IWorldServicesBuilder UseFactory<TFactory>(this IWorldServicesBuilder builder)
        where TFactory : class, IWorldFactory
    {
        builder.Services.RemoveAllKeyed<IWorldFactory>(builder.Key);
        builder.Services.AddKeyedSingleton<IWorldFactory, TFactory>(builder.Key);
        return builder;
    }

    /// <summary>
    /// Adds an instance of a world module to the configured world.
    /// </summary>
    public static IWorldServicesBuilder AddModule<TModule>(this IWorldServicesBuilder builder, TModule module)
        where TModule : class, IWorldModule
    {
        builder.Services.TryAddEnumerable(
            ServiceDescriptor.KeyedSingleton<IWorldModule>(builder.Key, module)
        );
        
        return builder;
    }

    /// <summary>
    /// Registers a world module type for the configured world.
    /// </summary>
    public static IWorldServicesBuilder AddModule<TModule>(this IWorldServicesBuilder builder)
        where TModule : class, IWorldModule
    {
        builder.Services.TryAddEnumerable(
            ServiceDescriptor.KeyedSingleton<IWorldModule, TModule>(builder.Key)
        );
        return builder;
    }  

    /// <summary>
    /// Registers a factory that creates a world module for the configured world.
    /// </summary>
    public static IWorldServicesBuilder AddModule<TModule>(
        this IWorldServicesBuilder builder,
        Func<IServiceProvider, TModule> factory)
        where TModule : class, IWorldModule
    {
        builder.Services.TryAddEnumerable(
            ServiceDescriptor.KeyedSingleton<IWorldModule, TModule>(
                serviceKey: builder.Key,
                implementationFactory: (provider, _) => factory(provider)
             )
        );

        return builder;
    }

    /// <summary>
    /// Applies modules registered for another world signature to the configured world.
    /// </summary>
    public static IWorldServicesBuilder DeriveWorld(this IWorldServicesBuilder builder, WorldSignature signature, int order = 0)
    {
        if (signature == WorldSignature.None)
            throw new ArgumentException("World signature cannot be None.", nameof(signature));

        return builder.AddModule(provider => new DeriveWorldModule(provider, signature, order));
    }

    /// <summary>
    /// Applies modules registered for the specified marker type to the configured world.
    /// </summary>
    public static IWorldServicesBuilder DeriveWorld<TMarker>(this IWorldServicesBuilder builder, int order = 0) where TMarker : IWorldMarker
    {
        var signature = WorldSignature.For<TMarker>();
        return builder.DeriveWorld(signature, order);
    }

    /// <summary>
    /// Registers an ordered callback that configures the world's blueprint.
    /// </summary>
    public static IWorldServicesBuilder Configure(this IWorldServicesBuilder builder, Action<IWorldBlueprint> setupAction, int order = 0)
    {
        builder.Services.AddKeyedSingleton<IWorldModule>(builder.Key, new DelegateWorldModule(order, setupAction));
        return builder;
    }

    /// <summary>
    /// Registers a keyed transient factory for a system type in the configured world.
    /// </summary>
    public static IWorldServicesBuilder AddSystemFactory<TSystem>(this IWorldServicesBuilder builder, Func<IServiceProvider, object?, TSystem> factory) where TSystem : BaseSystem
    {
        builder.Services.AddKeyedTransient(builder.Key, factory);
        return builder;
    }
}
