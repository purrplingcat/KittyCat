using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PurrplingCore.Toolkit.DI;
using System.Diagnostics;

namespace PurrplingCore.Toolkit.Hosting;

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
        Logger = hostProvider.GetRequiredService<ILoggerFactory>().CreateLogger(loggerCategory);

        _applicationServices.AddSingleton<StartupServiceExecutor>();
    }

    public ILogger Logger { get; }

    public IDictionary<object, object> Properties { get; }

    public IServiceProvider Services
    {
        get
        {
            Debug.Assert(_applicationServiceProvider != null, "Initialize must be called before accessing services.");
            return _applicationServiceProvider;
        }
    }

    public static IApplicationHostBuilder CreateBuilder(string[]? args = null)
    {
        var builder = new ApplicationHostBuilder(args);
        GameHostBuilderExtensions.ConfigureApplicationDefaults(builder);
        return builder;
    }

    internal virtual void Initialize()
    {
        foreach (var module in _hostServiceProvider.GetServices<IServiceModule>())
        {
            module.ConfigureServices(_applicationServices);
        }

        var registrations = _applicationServices.Count(service => service.ServiceType == typeof(IHostedApplication));
        if (registrations != 1)
        {
            throw new InvalidOperationException("Exactly one IHostedApplication must be registered.");
        }

        var factory = _hostServiceProvider.GetRequiredService<IServiceFactoryAdapter>();
        _applicationServiceProvider = factory.CreateServiceProvider(_applicationServices);
    }

    public virtual int Run()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _startupServices ??= Services.GetRequiredService<StartupServiceExecutor>();
        Logger.LogDebug("Executing startup services");
        _startupServices.Startup();

        _application ??= Services.GetRequiredService<IHostedApplication>();
        return _application.Run();
    }

    public virtual void Exit()
    {
        if (_application is IStoppableApplication stoppableApplication)
        {
            stoppableApplication.Exit();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        try
        {
            if (_startupServices != null)
            {
                Logger.LogDebug("Executing cleanup services ...");
                _startupServices.CleanUp();
            }
        }
        finally
        {
            DisposeRegisteredServices();
            DisposeProvider(_applicationServiceProvider);
            DisposeProvider(_hostServiceProvider);
        }
    }

    private void DisposeRegisteredServices()
    {
        if (_applicationServiceProvider == null) return;

        foreach (var disposableService in _applicationServiceProvider.GetServices<IDisposable>())
        {
            disposableService.Dispose();
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