using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PurrplingCore.Toolkit.DI;
using PurrplingCore.Toolkit.Hosting.Services;
using System.Diagnostics;

namespace PurrplingCore.Toolkit.Hosting;

public sealed class GameHost : IDisposable
{
    private readonly IServiceProvider _hostServiceProvider;
    private readonly IServiceCollection _gameServiceCollection;
    private readonly IDictionary<object, object> _properties;
    private readonly ILogger _logger;
    private readonly IHostEnvironment _environment;
    private IServiceProvider? _gameServiceProvider;
    private GameApplicationRunner? _gameRunner;
    private StartupServiceExecutor? _startupServices;
    private bool _disposed;

    internal GameHost(
        IServiceProvider hostProvider, 
        IServiceCollection gameServices, 
        IHostEnvironment environment, 
        ILogger logger,
        IDictionary<object, object>? properties = null
    )
    {
        _hostServiceProvider = hostProvider;
        _gameServiceCollection = gameServices;
        _logger = logger;
        _environment = environment;
        _properties = properties ?? new Dictionary<object, object>();

        // Register core game services
        _gameServiceCollection
            .AddSingleton<GameApplicationLifetime>()
            .AddSingleton<StartupServiceExecutor>()
            .AddSingleton<GameApplicationRunner>()
            .AddAlias<IGameApplicationLifetime, GameApplicationLifetime>();
    }

    public ILogger Logger => _logger;

    public bool IsRunning => _gameRunner?.IsRunning ?? false;

    public IDictionary<object, object> Properties => _properties;

    public IServiceProvider Services
    {
        get
        {
            Debug.Assert(_gameServiceProvider != null, "Initialize must be called before accessing services.");
            return _gameServiceProvider;
        }
    }

    public static IGameHostBuilder CreateBuilder(string[]? args = null)
    {
        var builder = new GameHostBuilder(args);

        builder.UseDefaultConfiguration();
        builder.AddCoreServices();

        return builder;
    }

    public static GameHost Create(
        IServiceProvider provider, 
        IServiceCollection gameServices,
        IDictionary<object, object>? properties = null,
        Action<GameHost>? onHostCreated = null
    )
    {
        var env = provider.GetRequiredService<IHostEnvironment>();
        var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("GameHost");
        var host = new GameHost(provider, gameServices, env, logger, properties);

        // Ensure that the startup service executor is resolved after the host is created
        host.Initialize();
        onHostCreated?.Invoke(host);
        
        return host;
    }

    internal void Initialize()
    {
        _logger.LogGameInformation(_environment);
        _logger.LogDebug("Environment: {EnvironmentName}", _environment.EnvironmentName);

        foreach (var module in _hostServiceProvider.GetServices<IServiceModule>())
        {
            module.ConfigureServices(_gameServiceCollection);
        }

        IServiceFactoryAdapter factory = _hostServiceProvider.GetRequiredService<IServiceFactoryAdapter>();
        _gameServiceProvider = factory.CreateServiceProvider(_gameServiceCollection);
    }

    public void Run()
    {
        Debug.Assert(_gameServiceProvider != null, "Initialize must be called first.");
        ObjectDisposedException.ThrowIf(_disposed, this);

        _startupServices ??= Services.GetRequiredService<StartupServiceExecutor>();
        _gameRunner ??= Services.GetRequiredService<GameApplicationRunner>();

        // Execute startup services before running the game application
        _logger.LogDebug("Executing startup services");
        _startupServices.Startup();

        // Run the game application
        _gameRunner.Run(Services.GetRequiredService<IGame>());
    }

    public void Exit()
    {
        _gameRunner?.Exit();
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        try
        {
            if (_startupServices != null)
            {
                // Clean up startup services before disposing the game host
                _logger.LogDebug("Executing cleanup services ...");
                _startupServices.CleanUp();
            }
        }
        finally
        {
            _gameRunner?.Dispose();
            DisposeGameServices();
            DisposeProvider(_gameServiceProvider);
            DisposeProvider(_hostServiceProvider);
        }
    }

    private void DisposeGameServices()
    {
        if (_gameServiceProvider == null) return;

        foreach (var disposableService in _gameServiceProvider.GetServices<IDisposable>())
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

internal static class LoggerExtensions
{
    public static void LogGameInformation(this ILogger logger, IHostEnvironment env)
    {
        logger.LogInformation(
            "{GameVersion} on {OS} - {Platform} {PlatformType}",
            env.GameVersion,
            env.OperatingSystem,
            env.OperatingSystem.Platform,
            env.PlatformType
        );
    }
}
