using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PurrplingCore.Toolkit.DI;
using System.Diagnostics;

namespace PurrplingCore.Toolkit.Hosting;

/// <summary>
/// Manages the lifecycle of a hosted application, orchestrating service initialization,
/// startup procedures, application execution, and cleanup.
/// </summary>
/// <remarks>
/// ApplicationHost is the core hosting container that manages dependency injection,
/// startup services, and the application lifecycle. It bridges the bootstrap host services
/// with the scoped application services.
/// </remarks>
public class ApplicationHost : IDisposable
{
    private readonly IServiceProvider _hostServiceProvider;
    private readonly IServiceCollection _applicationServices;
    private IServiceProvider? _applicationServiceProvider;
    private IHostedApplication? _application;
    private StartupServiceExecutor? _startupServices;
    private bool _disposed;

    protected internal ApplicationHost(
        IServiceProvider hostProvider,
        IServiceCollection applicationServices,
        IDictionary<object, object>? properties = null,
        string loggerCategory = "ApplicationHost")
    {
        _hostServiceProvider = hostProvider;
        _applicationServices = applicationServices;
        Properties = properties ?? new Dictionary<object, object>();
        
        var loggerFactory = hostProvider.GetRequiredService<ILoggerFactory>();
        Logger = loggerFactory.CreateLogger(loggerCategory);

        _applicationServices.AddSingleton<StartupServiceExecutor>();
    }

    public ILogger Logger { get; }

    public IDictionary<object, object> Properties { get; }

    /// <summary>
    /// Gets the application service provider for dependency resolution.
    /// </summary>
    /// <remarks>
    /// This property should only be accessed after Initialize() is called during Build().
    /// </remarks>
    public IServiceProvider Services
    {
        get
        {
            const string assertMsg = "Initialize must be called before accessing services.";
            Debug.Assert(_applicationServiceProvider != null, assertMsg);
            return _applicationServiceProvider;
        }
    }

    /// <summary>
    /// Creates a new ApplicationHostBuilder with default configuration.
    /// </summary>
    /// <param name="args">Optional command-line arguments.</param>
    /// <returns>A configured IApplicationHostBuilder instance.</returns>
    public static IApplicationHostBuilder CreateBuilder(string[]? args = null)
    {
        var builder = new ApplicationHostBuilder(args);
        GameHostBuilderExtensions.ConfigureApplicationDefaults(builder);
        return builder;
    }

    /// <summary>
    /// Initializes the application by configuring services and creating the service provider.
    /// </summary>
    /// <remarks>
    /// This method is called internally during Build() to apply service modules and finalize
    /// service registrations. It verifies that an IHostedApplication is available when
    /// the service provider supports <see cref="IServiceProviderIsService"/>.
    /// </remarks>
    internal virtual void Initialize()
    {
        // Apply all registered service modules to the application service collection
        var modules = _hostServiceProvider.GetServices<IServiceModule>();
        foreach (var module in modules)
        {
            module.ConfigureServices(_applicationServices);
        }

        // Create the application-scoped service provider
        var factory = _hostServiceProvider.GetRequiredService<IServiceFactoryAdapter>();
        _applicationServiceProvider = factory.CreateServiceProvider(_applicationServices);
        _application = ResolveHostedApplication(_applicationServiceProvider);
    }

    private static IHostedApplication ResolveHostedApplication(IServiceProvider provider)
    {
        var applications = provider.GetServices<IHostedApplication>()
                                   .ToArray();

        if (applications.Length != 1)
        {
            throw new InvalidOperationException("Exactly one IHostedApplication must be registered.");
        }

        return applications[0];
    }

    /// <summary>
    /// Executes startup procedures and runs the hosted application.
    /// </summary>
    /// <returns>The exit code from the hosted application.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the host has been disposed.</exception>
    public virtual int Run()
    {
        Debug.Assert(_application != null, "Initialize must be called before running the application.");
        ObjectDisposedException.ThrowIf(_disposed, this);
        
        // Execute startup services on first run
        _startupServices ??= Services.GetRequiredService<StartupServiceExecutor>();
        Logger.LogDebug("Executing startup services");
        _startupServices.Startup();

        // Run the hosted application and return its exit code
        return _application.Run();
    }

    /// <summary>
    /// Requests graceful shutdown of the hosted application.
    /// </summary>
    public virtual void Exit()
    {
        if (_application is IStoppableApplication stoppableApplication)
        {
            stoppableApplication.Exit();
        }
    }

    /// <summary>
    /// Disposes the application host, cleaning up services and providers.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        try
        {
            // Run cleanup services in reverse order of startup
            if (_startupServices != null)
            {
                Logger.LogDebug("Executing cleanup services ...");
                _startupServices.CleanUp();
            }
        }
        finally
        {
            // Dispose all registered services and providers
            DisposeRegisteredServices();
            DisposeProvider(_applicationServiceProvider);
            DisposeProvider(_hostServiceProvider);
        }
    }

    private void DisposeRegisteredServices()
    {
        if (_applicationServiceProvider == null)
            return;

        var disposables = _applicationServiceProvider.GetServices<IDisposable>();
        foreach (var service in disposables)
        {
            service.Dispose();
        }
    }

    private static void DisposeProvider(IServiceProvider? provider)
    {
        if (provider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}