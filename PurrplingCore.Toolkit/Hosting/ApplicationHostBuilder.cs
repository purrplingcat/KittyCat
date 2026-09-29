using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using PurrplingCore.Toolkit.DI;
using PurrplingCore.Toolkit.Messaging;
using System.Diagnostics.CodeAnalysis;

namespace PurrplingCore.Toolkit.Hosting;

/// <summary>
/// Builds an ApplicationHost by configuring services, logging, configuration, and environment.
/// </summary>
/// <remarks>
/// ApplicationHostBuilder provides a fluent API for setting up the host container with
/// dependency injection, configuration management, and logging. It supports custom service
/// provider factories and collects service modules that are applied during host initialization.
/// </remarks>
public class ApplicationHostBuilder : IApplicationHostBuilder
{
    private readonly ServiceCollection _services = [];
    private readonly ConfigurationManager _config = new();
    private readonly HostEnvironment _env = new();
    private IServiceFactoryAdapter _serviceProviderFactory;
    private bool _hostBuilt;

    public IServiceCollection Services => _services;
    public IConfigurationManager Configuration => _config;
    public IHostEnvironment Environment => _env;
    public ILoggingBuilder Logging { get; }
    public IDictionary<object, object> Properties { get; } = new Dictionary<object, object>();

    /// <summary>
    /// Initializes a new instance of ApplicationHostBuilder with optional command-line arguments.
    /// </summary>
    /// <param name="args">Command-line arguments to be parsed by the configuration system.</param>
    public ApplicationHostBuilder(string[]? args = null)
    {
        Logging = new LoggingBuilder(_services);
        _config.AddEnvironmentVariables(prefix: "DOTNET_");
        _config.AddCommandLine(args ?? []);
        _env.EnvironmentName = _config[HostDefaults.EnvironmentKey] ?? _env.EnvironmentName;

        var options = _env.IsDevelopment()
            ? new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true }
            : new ServiceProviderOptions();
        UseServiceProviderFactory(new DefaultServiceProviderFactory(options));
    }

    /// <summary>
    /// Specifies the service provider factory to use for creating the IServiceProvider.
    /// </summary>
    /// <typeparam name="TContainerBuilder">The type of the service container builder.</typeparam>
    /// <param name="factory">The factory to use for creating service providers.</param>
    /// <returns>The current builder instance for fluent configuration.</returns>
    [MemberNotNull(nameof(_serviceProviderFactory))]
    public IApplicationHostBuilder UseServiceProviderFactory<TContainerBuilder>(IServiceProviderFactory<TContainerBuilder> factory)
        where TContainerBuilder : notnull
    {
        ArgumentNullException.ThrowIfNull(factory);

        _serviceProviderFactory = new ServiceProviderFactoryAdapter<TContainerBuilder>(factory);
        return this;
    }

    /// <summary>
    /// Adds common services to the service collection that are required by all applications.
    /// </summary>
    /// <remarks>
    /// This is called internally during Build() to register logging, configuration,
    /// environment, message bus, and lazy service resolution.
    /// </remarks>
    protected virtual void AddCommonServices(IServiceCollection services)
    {
        services.AddLogging();
        services.TryAddSingleton(_serviceProviderFactory);
        services.TryAddSingleton<IConfiguration>(_config);
        services.TryAddSingleton<IHostEnvironment>(_env);
        services.TryAddSingleton<IMessageBus, NullMessageBus>();
        services.TryAddTransient(typeof(Lazy<>), typeof(LazyService<>));
    }

    /// <summary>
    /// Builds and initializes the ApplicationHost with all configured services.
    /// </summary>
    /// <returns>A fully configured and initialized ApplicationHost instance.</returns>
    /// <remarks>
    /// This method can only be called once. Subsequent calls will throw InvalidOperationException.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown if Build() is called more than once.</exception>
    public virtual ApplicationHost Build()
    {
        if (_hostBuilt)
        {
            throw new InvalidOperationException($"{GetType().Name} can only build once");
        }
        _hostBuilt = true;

        // Add common infrastructure services
        AddCommonServices(_services);
        _services.MakeReadOnly();

        // Create application-scoped service collection
        var applicationServices = _services.Clone();
        var provider = _serviceProviderFactory.CreateServiceProvider(_services);
        var host = CreateHost(provider, applicationServices);

        // Initialize host and invoke callback
        host.Initialize();
        OnHostCreated(host);

        return host;
    }

    /// <summary>
    /// Creates the ApplicationHost instance with the given services.
    /// </summary>
    /// <remarks>
    /// This method can be overridden in derived classes to create specialized host types.
    /// </remarks>
    protected virtual ApplicationHost CreateHost(IServiceProvider provider, IServiceCollection applicationServices)
    {
        return new ApplicationHost(provider, applicationServices, Properties);
    }

    /// <summary>
    /// Called after the host is created, allowing derived classes to perform additional setup.
    /// </summary>
    /// <remarks>
    /// The default implementation stores the host in the Properties dictionary.
    /// </remarks>
    protected virtual void OnHostCreated(ApplicationHost host)
    {
        Properties[typeof(ApplicationHost)] = host;
    }

    private sealed class LoggingBuilder(IServiceCollection services) : ILoggingBuilder
    {
        public IServiceCollection Services => services;
    }
}