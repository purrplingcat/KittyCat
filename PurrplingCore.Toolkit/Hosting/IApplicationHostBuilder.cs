using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace PurrplingCore.Toolkit.Hosting;

public interface IApplicationHostBuilder
{
    /// <summary>
    /// Gets the service collection for immediate registration
    /// of infrastructure and bootstrap services.
    /// </summary>
    IServiceCollection Services { get; }

    IConfigurationManager Configuration { get; }

    IHostEnvironment Environment { get; }

    /// <summary>
    /// Logging builder
    /// </summary>
    ILoggingBuilder Logging { get; }

    IDictionary<object, object> Properties { get; }

    IApplicationHostBuilder UseServiceProviderFactory<TContainer>(IServiceProviderFactory<TContainer> factory)
        where TContainer : notnull;

    ApplicationHost Build();
}