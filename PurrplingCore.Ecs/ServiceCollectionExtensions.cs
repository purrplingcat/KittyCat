using Friflo.Engine.ECS;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using PurrplingCore.Toolkit.DI;

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

    public static IServiceCollection UseEcs(this IServiceCollection services)
    {
        services.TryAddScoped<IWorldAccessor, WorldAccessor>();
        services.TryAddSingleton<IWorldFactory, WorldFactory>();
        services.TryAddSingleton(static provider =>
        {
            var worldFactory = provider.GetRequiredService<IWorldFactory>();
            var options = provider.GetService<IOptions<WorldOptions>>();
            var config = options?.Value ?? new WorldOptions();

            return new WorldManager(
                worldFactory,
                knownWorlds: config.AllowUnknownWorlds ? null : config.KnownWorlds
            );
        });

        return services;
    }

    public static IServiceCollection AddWorld(this IServiceCollection services, WorldTag tag, Action<WorldBuilder> configure)
    {
        services.UseEcs();
        services.Configure<WorldOptions>(options => options.KnownWorlds.Add(tag));
        services.AddWorldBootstrap(tag, (services, key) => new DelegateBuilderBootstrap(configure));
        return services;
    }

    public static IServiceCollection AddWorldBootstrap<TBootstrap>(this IServiceCollection services, WorldTag tag) where TBootstrap : class, IWorldBootstrap
    {
        services.UseEcs();
        return services.AddKeyedSingleton<IWorldBootstrap, TBootstrap>(tag);
    }

    public static IServiceCollection AddWorldBootstrap(this IServiceCollection services, WorldTag tag, Func<IServiceProvider, object?, IWorldBootstrap> factory)
    {
        services.UseEcs();
        return services.AddKeyedSingleton(tag, factory);
    }

    public static IServiceCollection AddWorldBootstrap(this IServiceCollection services, WorldTag tag, Action<ManagedWorld> setupAction, int order = 0)
    {
        services.UseEcs();

        var bootstrap = new DelegateWorldBootstrap(order, setupAction);
        return services.AddKeyedSingleton<IWorldBootstrap>(tag, bootstrap);
    }
}
