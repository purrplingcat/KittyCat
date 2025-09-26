using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace PurrplingCore.Toolkit.DI;

public interface IServiceFactory<TService> where TService : class
{
    public TService Create();
}

public static partial class ServiceExtensions
{
    private static TService CreateService<TService, TFactory>(IServiceProvider provider)
        where TService : class
        where TFactory : class, IServiceFactory<TService>
    {
        var factory = ActivatorUtilities.CreateInstance<TFactory>(provider);
        return factory.Create();
    }

    public static IServiceCollection AddTransientFactory<TService, TFactory>(this IServiceCollection services)
        where TService : class
        where TFactory : class, IServiceFactory<TService>
    {
        return services.AddTransient(CreateService<TService, TFactory>);
    }

    public static IServiceCollection AddSingletonFactory<TService, TFactory>(this IServiceCollection services)
        where TService : class
        where TFactory : class, IServiceFactory<TService>
    {
        return services.AddSingleton(CreateService<TService, TFactory>);
    }

    public static IServiceCollection AddScopedFactory<TService, TFactory>(this IServiceCollection services)
        where TService : class
        where TFactory : class, IServiceFactory<TService>
    {
        return services.AddScoped(CreateService<TService, TFactory>);
    }
}
