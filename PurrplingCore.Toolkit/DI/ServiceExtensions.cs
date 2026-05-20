using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PurrplingCore.Toolkit.DI.Configuration;
using PurrplingCore.Toolkit.Hosting;
using System.Runtime.CompilerServices;

namespace PurrplingCore.Toolkit.DI;

public static partial class ServiceExtensions
{
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

    public static void TryExpose<TExposed, TSource>(this IServiceCollection services, Func<TSource, TExposed> exposeDelegate)
        where TExposed : class
        where TSource : class
    {
        services.TryAddTransient(provider => exposeDelegate(provider.GetRequiredService<TSource>()));
    }

    public static IServiceCollection AddStartup<TStartup>(this IServiceCollection services) where TStartup : class, IStartupService
    {
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IStartupService, TStartup>());
        return services;
    }

    public static IServiceCollection ActivateSingleton<TService>(this IServiceCollection services) where TService : class
    {
        services.AddStartup<AutoActivator>()
                .AddOptions<AutoActivatorOptions>()
                .Configure(ao =>
                {
                    var constructed = typeof(IEnumerable<TService>);
                    if (ao.AutoActivators.Contains(constructed))
                    {
                        return;
                    }

                    if (ao.AutoActivators.Remove(typeof(TService)))
                    {
                        ao.AutoActivators.Add(constructed);
                        return;
                    }

                    ao.AutoActivators.Add(typeof(TService));
                });
        return services;
    }

    public static IServiceCollection AddActivatedSingleton<TService>(this IServiceCollection services) where TService : class
    {
        return services.AddSingleton<TService>()
                       .ActivateSingleton<TService>();
    }

    public static IServiceCollection AddActivatedSingleton<TService, TImplementation>(this IServiceCollection services)
        where TService : class
        where TImplementation : class, TService
    {
        return services.AddSingleton<TService, TImplementation>()
                       .ActivateSingleton<TService>();
    }

    /// <summary>
    /// [HACK] MongoGame services are registered in the Game.Services collection, so this method allows you to expose them to the DI container.
    /// </summary>
    /// <typeparam name="TService"></typeparam>
    /// <param name="services"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static IServiceCollection ExposeMonoGameService<TService>(this IServiceCollection services)
    where TService : class
    {
        services.AddTransient(static provider =>
        {
            var game = provider.GetRequiredService<Microsoft.Xna.Framework.Game>();
            var monoGameService = game.Services.GetService<TService>();

            return monoGameService
                ?? throw new InvalidOperationException(
                    $"Service type '{typeof(TService)}' not found in Game.Services");
        });

        return services;
    }

    public static GameServiceContainer GetGameServices(this IServiceProvider provider)
    {
        if (provider is GameServiceContainer gameServices) 
            return gameServices;

        return provider.GetRequiredService<GameServiceContainer>();
    }

    public static T GetGameService<T>(this IServiceProvider provider) where T : notnull
    {
        return provider.GetGameServices()
                       .GetRequiredService<T>();
    }
}

public static partial class ServiceExtensions
{
    public static IServiceCollection AddServiceConfiguration<T>(this IServiceCollection services, T instance) where T : class, IServicesConfiguration
    {
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IServicesConfiguration>(instance));
        return services;
    }

    public static IServiceCollection AddServiceConfiguration<T>(this IServiceCollection services) where T : class, IServicesConfiguration
    {
        services.TryAddEnumerable(ServiceDescriptor.Transient<IServicesConfiguration, T>());
        return services;
    }

    public static IServiceCollection AddServiceConfiguration<T>(this IServiceCollection services, Func<IServiceProvider, T> factory) where T : class, IServicesConfiguration
    {
        services.TryAddEnumerable(ServiceDescriptor.Transient<IServicesConfiguration, T>(factory));
        return services;
    }

    public static IServiceCollection AddServiceConfiguration(this IServiceCollection services, Func<IServiceProvider, IServicesConfiguration> factory)
    {
        services.AddTransient(factory);
        return services;
    }
}
