using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;

namespace PurrplingCore.Ecs.DI;

public static class WorldBuilderExtensions
{
    public static IWorldBuilder AddBootstrap<TBootstrap>(this IWorldBuilder builder)
        where TBootstrap : class, IWorldBootstrap
    {
        builder.Services.TryAddEnumerable(
           ServiceDescriptor.KeyedTransient<IWorldBootstrap, TBootstrap>(builder.WorldType)
        );
        return builder;
    }

    public static IWorldBuilder ConfigureRuntime(this IWorldBuilder builder, WorldBuildAction configure)
    {
        builder.AddBootstrap((_, _) => new DelegateRuntimeBootstrap(configure));
        return builder;
    }

    public static IWorldBuilder AddBootstrap(
        this IWorldBuilder builder, Func<IServiceProvider, object?, IWorldBootstrap> factory)
    {
        builder.Services.AddKeyedTransient(
            builder.WorldType, factory
        );
        return builder;
    }
}
