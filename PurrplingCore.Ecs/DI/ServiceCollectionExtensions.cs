using Friflo.Engine.ECS.Systems;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PurrplingCore.Toolkit.DI;

namespace PurrplingCore.Ecs.DI;

public static class ServiceCollectionExtensions
{
    private sealed class WorldServicesBuilder(IServiceCollection services, WorldSignature signature) : IWorldServicesBuilder
    {
        public IServiceCollection Services { get; } = services;
        public WorldSignature Signature { get; } = signature;
        public object Key => Signature.MarkerType;
    }

    private sealed class DelegadeWorldModule(int order, Action<IWorldBuilder> setupAction) : IWorldModule
    {
        public int Order { get; } = order;

        public void Setup(IWorldBuilder builder)
        {
            setupAction(builder);
        }
    }

    public static IServiceCollection AddEcsCoreServices(this IServiceCollection services)
    {
        services.TryAddScoped<WorldContext>();
        services.TryAddAlias<IWorldContext, WorldContext>();
        services.TryAddSingleton<IWorldFactory, WorldFactory>();
        services.TryAddSingleton(static provider =>
        {
            var options = provider.GetService<IOptions<EcsOptions>>();

            return new WorldManager(
                provider,
                defaultFactory: provider.GetRequiredService<IWorldFactory>(),
                logger: provider.GetRequiredService<ILogger<WorldManager>>()
            );
        });

        return services;
    }

    public static IWorldServicesBuilder AddWorld<T>(this IServiceCollection services)
        where T : IWorldMarker
    {
        var signature = WorldSignature.For<T>();
        services.AddEcsCoreServices();
        services.TryAddKeyedSingleton<IWorldFactory, DefaultWorldFactory>(signature.MarkerType);

        return new WorldServicesBuilder(services, signature);
    }

    public static IWorldServicesBuilder AddWorld(this IServiceCollection services)
    {
        var signature = WorldSignature.Default;
        services.AddEcsCoreServices();
        services.TryAddKeyedSingleton<IWorldFactory, DefaultWorldFactory>(signature.MarkerType);

        return new WorldServicesBuilder(services, signature);
    }

    public static IWorldServicesBuilder UseFactory<TFactory>(this IWorldServicesBuilder builder)
        where TFactory : class, IWorldFactory
    {
        builder.Services.RemoveAllKeyed<IWorldFactory>(builder.Key);
        builder.Services.AddKeyedSingleton<IWorldFactory, TFactory>(builder.Key);
        return builder;
    }

    public static IWorldServicesBuilder AddModule<TModule>(this IWorldServicesBuilder builder)
        where TModule : class, IWorldModule
    {
        // Tady je schovaný ten nehezký zápis
        builder.Services.AddKeyedSingleton<IWorldModule, TModule>(builder.Key);
        return builder;
    }  

    public static IWorldServicesBuilder AddModule<TModule>(
        this IWorldServicesBuilder builder,
        Func<IServiceProvider, TModule> factory)
        where TModule : class, IWorldModule
    {
        builder.Services.AddKeyedSingleton<IWorldModule, TModule>(
            builder.Key, (provider, _) => factory(provider)
        );

        return builder;
    }

    public static IWorldServicesBuilder AddModule(this IWorldServicesBuilder builder, Action<IWorldBuilder> setupAction, int order = 0)
    {
        builder.Services.AddKeyedSingleton<IWorldModule>(builder.Key, new DelegadeWorldModule(order, setupAction));
        return builder;
    }

    public static IWorldServicesBuilder AddSystemFactory<TSystem>(this IWorldServicesBuilder builder, Func<IServiceProvider, object?, TSystem> factory) where TSystem : BaseSystem
    {
        builder.Services.AddKeyedTransient(builder.Key, factory);
        return builder;
    }
}
