using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PurrplingCore.Ecs.DI;
using PurrplingCore.Ecs.Systems;
using PurrplingCore.Ecs.Systems.Builder;
using PurrplingCore.Toolkit.DI;
using System.Reflection;

namespace PurrplingCore.Ecs;

public static class ServiceCollectionExtensions
{
    private class DelegateWorldBootstrap(int order, Action<ManagedWorld> setupAction) : IWorldBootstrap
    {
        public int Order { get; } = order;

        public void Setup(ManagedWorld world)
        {
            setupAction(world);
        }
    }

    public static IServiceCollection AddEcs(this IServiceCollection services)
    {
        services.TryAddScoped<IWorldContext, WorldContext>();
        services.TryAddSingleton<IWorldFactory, WorldFactory>();
        services.TryAddSingleton<SystemMetadataStore>();
        services.TryAddAlias<IStartupService, SystemMetadataStore>();
        services.TryAddSingleton(static provider =>
        {
            var options = provider.GetService<IOptions<EcsOptions>>();

            return new WorldManager(
                factory: provider.GetRequiredService<IWorldFactory>(),
                options: options?.Value ?? new EcsOptions(),
                logger: provider.GetRequiredService<ILogger<WorldManager>>()
            );
        });

        return services;
    }

    public static IServiceCollection AddEcs(this IServiceCollection services, Action<EcsOptions> configure)
    {
        services.Configure(configure);
        return services.AddEcs();
    }

    public static IWorldBuilder AddWorld<T>(this IServiceCollection services) where T : IWorldMarker
    {
        var worldType = WorldType.For<T>();
        var builder = new DefaultWorldBuilder(services, worldType);

        services.AddEcs();
        services.TryAddKeyedSingleton(worldType, BuildSystemRegistry<T>);

        return builder;
    }

    public static IWorldBuilder AddWorld<T>(this IServiceCollection services, Action<WorldInitOptions> configure)
        where T : IWorldMarker
    {
        var builder = services.AddWorld<T>();
        services.Configure<EcsOptions>(options =>
        {
            configure(options.GetWorldInitOptions(builder.WorldType));
        });

        return builder;
    }

    private static SystemRegistry BuildSystemRegistry<T>(IServiceProvider sp, object? key) where T : IWorldMarker
    {
        var worldType = WorldType.For<T>();
        var registry = SystemRegistry.CreateWithDefaults();
        var globalStore = sp.GetRequiredService<SystemMetadataStore>();
        var worldSystems = globalStore.GetSystemsForWorld(worldType);

        foreach (var info in worldSystems)
        {
            var entry = new SortedSystemSet.SystemEntry(info.SystemType, info.Order);

            entry.RunBefore.UnionWith(info.RunBefore);
            entry.RunAfter.UnionWith(info.RunAfter);

            registry.GetOrCreate(info.GroupType).Add(entry);
        }

        return registry;
    }
}
