using Microsoft.Extensions.DependencyInjection;

namespace PurrplingCore.Toolkit.DI;

internal interface IServiceFactoryAdapter
{
    IServiceProvider CreateServiceProvider(IServiceCollection services);
}
