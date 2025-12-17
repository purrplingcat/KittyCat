using Microsoft.Extensions.DependencyInjection;

namespace PurrplingCore.Toolkit.DI;

internal sealed class DefaultServiceProviderFactory : IServiceProviderFactory
{
    public IServiceProvider CreateServiceProvider(IServiceCollection services)
    {
        return services.BuildServiceProvider();
    }
}

internal sealed class ServiceProviderFactoryAdapter<TContainerBuilder> : IServiceProviderFactory where TContainerBuilder : notnull
{
    private readonly IServiceProviderFactory<TContainerBuilder> _serviceProviderFactory;

    public ServiceProviderFactoryAdapter(IServiceProviderFactory<TContainerBuilder> serviceProviderFactory)
    {
        _serviceProviderFactory = serviceProviderFactory;
    }

    public IServiceProvider CreateServiceProvider(IServiceCollection services)
    {
        var containerBuilder = _serviceProviderFactory.CreateBuilder(services);
        
        return _serviceProviderFactory.CreateServiceProvider(containerBuilder);
    }
}
