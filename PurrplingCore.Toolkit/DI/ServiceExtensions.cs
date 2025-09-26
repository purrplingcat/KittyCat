using Friflo.Engine.ECS.Systems;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Xna.Framework;
using PurrplingCore.Toolkit.Systems;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace PurrplingCore.Toolkit.DI;

public static partial class ServiceExtensions
{
    public static IServiceCollection AddGame<TGame>(this IServiceCollection services) where TGame : Game, IGame 
    {
        return services.AddPureGame<TGame>()
                       .AddAlias<Game, TGame>();
    }

    public static IServiceCollection AddPureGame<TGame>(this IServiceCollection services) where TGame : class, IGame
    {
        return services.AddSingleton<TGame>()
                       .AddAlias<IGame, TGame>();
    }

    public static IServiceCollection AddConfiguration(this IServiceCollection services, IServiceConfiguration configuration)
    {
        configuration.Configure(services);
        return services;
    }

    public static IServiceCollection AddAlias<TService, TAliased>(this IServiceCollection services)
        where TService : class
        where TAliased : class, TService
    {
        return services.AddTransient<TService>(provider => provider.GetRequiredService<TAliased>());
    }

    public static IServiceCollection AddAliases(this IServiceCollection services, Type serviceType, params Type[] aliases)
    {
        return services.AddAliases(serviceType, (IEnumerable<Type>)aliases);
    }

    public static IServiceCollection AddAliases(this IServiceCollection services, Type serviceType, IEnumerable<Type> aliases)
    {
        foreach (var alias in aliases)
        {
            services.AddTransient(alias, provider => provider.GetRequiredService(serviceType));
        }

        return services;
    }

    public static IServiceCollection TryAddAlias<TService, TAliased>(this IServiceCollection services)
        where TService : class
        where TAliased : class, TService
    {
        services.TryAddTransient<TService>(provider => provider.GetRequiredService<TAliased>());
        return services;
    }

    public static IServiceCollection Expose<TExposed, TSource>(this IServiceCollection services, Func<TSource, TExposed> exposeDelegate) 
        where TExposed : class 
        where TSource : class
    {
        return services.AddTransient(provider => exposeDelegate(provider.GetRequiredService<TSource>()));
    }

    public static IServiceCollection AddSetup<T>(this IServiceCollection services, Action<T> setup)
    {
        services.TryAddSingleton(typeof(ISetup<T>), typeof(Setup<T>));
        services.AddSingleton(new Setup<T>.SetupAction(setup));
        return services;
    }

    public static IServiceCollection AddKeyedSetup<T>(this IServiceCollection services, Action<T> setup, object? key)
    {
        services.TryAddKeyedSingleton(typeof(ISetup<T>), key, typeof(Setup<T>));
        services.AddKeyedSingleton(key, new Setup<T>.SetupAction(setup));
        return services;
    }

    public static IServiceCollection AddStartup<TStartup>(this IServiceCollection services) where TStartup : class, IStartupService
    {
        return services.AddSingleton<IStartupService, TStartup>();
    }
}
