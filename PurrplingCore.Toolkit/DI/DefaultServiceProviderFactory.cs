using Microsoft.Extensions.DependencyInjection;

namespace PurrplingCore.Toolkit.DI;

internal sealed class ServiceProviderFactoryAdapter<TContainerBuilder> : IServiceFactoryAdapter where TContainerBuilder : notnull
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
