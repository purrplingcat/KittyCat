using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace PurrplingCore.Toolkit.Hosting;

/// <summary>
/// Provides a fluent API for configuring and building an ApplicationHost.
/// </summary>
/// <remarks>
/// The application host builder is responsible for collecting service registrations,
/// configuration, logging setup, and environment information before building the host.
/// </remarks>
public interface IApplicationHostBuilder
{
    /// <summary>
    /// Gets the service collection for immediate registration
    /// of infrastructure and bootstrap services.
    /// </summary>
    IServiceCollection Services { get; }

    /// <summary>
    /// Gets the configuration manager for adding configuration sources.
    /// </summary>
    IConfigurationManager Configuration { get; }

    /// <summary>
    /// Gets the host environment information.
    /// </summary>
    IHostEnvironment Environment { get; }

    /// <summary>
    /// Gets the logging builder for configuring log providers and filters.
    /// </summary>
    ILoggingBuilder Logging { get; }

    /// <summary>
    /// Gets the properties dictionary for storing arbitrary values during configuration.
    /// </summary>
    IDictionary<object, object> Properties { get; }

    /// <summary>
    /// Sets the factory to use for creating the IServiceProvider.
    /// </summary>
    /// <typeparam name="TContainer">The type of the service container builder.</typeparam>
    /// <param name="factory">The service provider factory to use.</param>
    /// <returns>The builder instance for fluent configuration.</returns>
    IApplicationHostBuilder UseServiceProviderFactory<TContainer>(IServiceProviderFactory<TContainer> factory)
        where TContainer : notnull;

    /// <summary>
    /// Builds and initializes the ApplicationHost with all configured services.
    /// </summary>
    /// <returns>A fully configured and initialized ApplicationHost instance.</returns>
    ApplicationHost Build();
}