using Friflo.Engine.ECS;
using Microsoft.Extensions.DependencyInjection;

namespace PurrplingCore.Ecs;
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWorld<TWorld>(this IServiceCollection services)
        where TWorld : World
    {
        return services.AddSingleton<World, TWorld>();
    }

    public static IServiceCollection AddWorld(this IServiceCollection services, Func<IServiceProvider, World> factory)
    {
        return services.AddSingleton(factory);
    }

    public static IServiceCollection AddWorldExtension<TExtension, TImplementation>(this IServiceCollection services)
        where TExtension : class
        where TImplementation : class, IWorldExtension<TExtension>
    {
        return services.AddSingleton<IWorldExtension<TExtension>, TImplementation>();
    }

    public static IServiceCollection AddWorldExtension<TExtension>(this IServiceCollection services, Func<EntityStore, IServiceProvider, TExtension> factory)
        where TExtension : class
    {
        return services.AddSingleton<IWorldExtension<TExtension>>(
            provider => new GenericWorldExtension<TExtension>(
                provider.GetRequiredService<World>(), provider, factory
            )
        );
    }
}
