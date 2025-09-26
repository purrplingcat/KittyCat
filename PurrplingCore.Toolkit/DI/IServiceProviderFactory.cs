using Microsoft.Extensions.DependencyInjection;

namespace PurrplingCore.Toolkit.DI;

public interface IServiceProviderFactory
{
    IServiceProvider CreateServiceProvider(IServiceCollection services);
}
