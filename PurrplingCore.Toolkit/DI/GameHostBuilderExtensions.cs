using Microsoft.Extensions.DependencyInjection;
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

    public static IGameHostBuilder AddGame<TGame>(this IGameHostBuilder builder) where TGame : GameCore
    {
        return builder.ConfigureServices(static (services, _) => {
            var gameType = typeof(TGame);
            var servicesAttrs = gameType.GetCustomAttributes<GameServicesAttribute>();

            foreach (var attr in servicesAttrs)
            {
                services.AddConfiguration(attr.CreateConfiguration());
            }

            // Add services from the assembly containing the game type
            services.AddConfiguration(new AssemblyServices(gameType.Assembly))
                    .AddGame<TGame>()
                    .AddAlias<GameCore, TGame>();
        });
    }
}
