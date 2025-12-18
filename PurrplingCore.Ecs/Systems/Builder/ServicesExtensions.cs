using Friflo.Engine.ECS.Systems;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PurrplingCore.Toolkit.DI;

namespace PurrplingCore.Ecs.Systems.Builder;

public static class ServiceExtensions
{
    public static void AddSystemGroup<TGroup>(this IServiceCollection services)
        where TGroup : SystemGroup
    {
        services.TryAddSingleton(typeof(ISystemGroupFactory<>), typeof(SystemGroupFactory<>));
        services.TryAddSingleton<ISystemGroupFactory, SystemGroupFactory>();
        services.TryAddTransient(CreateSystemGroup<TGroup>);
    }

    public static IServiceCollection AddSystemGroup<TGroup>(this IServiceCollection services, Action<ISystemBuilder> configure)
        where TGroup : SystemGroup
    {
        services.AddSystemGroup<TGroup>();
        configure(new SystemBuilder<TGroup>(services));

        return services;
    }

    public static IServiceCollection AddSystem<TSystem>(this IServiceCollection services) where TSystem : BaseSystem
    {
        return services.AddSystemGroup<SystemRoot>(builder => builder.AddSystem<TSystem>());
    }

    public static IServiceCollection AddSystem<TSystem, TImplementation>(this IServiceCollection services)
        where TSystem : BaseSystem
        where TImplementation : TSystem
    {
        return services.AddSystemGroup<SystemRoot>(builder => builder.AddSystem<TSystem, TImplementation>());
    }

    public static IServiceCollection AddSystemFactory<TSystem, TFactory>(this IServiceCollection services)
        where TSystem : BaseSystem
        where TFactory : IServiceFactory<TSystem>
    {
        return services.AddSystemGroup<SystemRoot>(builder => builder.AddSystemFactory<TSystem, TFactory>());
    }

    public static IServiceCollection AddSystemRoot(this IServiceCollection services, Action<ISystemBuilder> configure)
    {
        return services.AddSystemGroup<SystemRoot>(configure);
    }

    private static TGroup CreateSystemGroup<TGroup>(IServiceProvider provider) where TGroup : SystemGroup
    {
        return provider
                .GetRequiredService<ISystemGroupFactory>()
                .CreateGroup<TGroup>();
    }
}
