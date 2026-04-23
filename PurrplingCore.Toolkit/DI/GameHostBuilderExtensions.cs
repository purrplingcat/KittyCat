using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using PurrplingCore.Toolkit.DI.Configuration;
using System.Reflection;

namespace PurrplingCore.Toolkit.DI;

public static class GameHostBuilderExtensions
{
    public static IGameHostBuilder AddServiceConfiguration(this IGameHostBuilder builder, IServiceConfiguration configuration)
    {
        return builder.ConfigureServices((services, _) => services.AddConfiguration(configuration));
    }

    public static IGameHostBuilder AddServiceConfiguration(this IGameHostBuilder builder, Func<GameHostBuilderContext, IServiceConfiguration> createConfiguration)
    {
        return builder.ConfigureServices((services, context) => services.AddConfiguration(createConfiguration(context)));
    }

    public static IGameHostBuilder UseServiceProviderFactory<TContainerBuilder>(this IGameHostBuilder builder, IServiceProviderFactory<TContainerBuilder> factory) 
        where TContainerBuilder : notnull
    {
        return builder.UseServiceProviderFactory(new ServiceProviderFactoryAdapter<TContainerBuilder>(factory));
    }
}
