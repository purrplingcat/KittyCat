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
    private StartupServiceExecutor? _startupServiceExecutor;
    private IGame? _game;

    private IServiceProvider? _gameServiceProvider;
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

        _gameServiceCollection.AddSingleton<StartupServiceExecutor>();
    }

    public ILogger Logger => _logger;

    public bool IsRunning => _game?.IsRunning ?? false;

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

        // Resolve the startup service executor and the game instance before running the game.
        _startupServiceExecutor = _gameServiceProvider.GetRequiredService<StartupServiceExecutor>();
        _game ??= ResolveGame();

        _logger.LogDebug("Executing startup services");
        _startupServiceExecutor.Startup();

        // Run the game after initializing the startup services.
        _logger.LogDebug("Executing {gameType}::Run()", _game.GetType());
        _game.Run();
    }

    private IGame ResolveGame()
    {
        var game = Services.GetRequiredService<IGame>();
        game.Launched += OnGameLaunched;
        game.Exited += OnGameExited;
        game.Disposed += OnGameDisposed;

        return game;
    }

    private void OnGameLaunched(object? sender, EventArgs e)
    {
        throw new NotImplementedException();
    }

    public void Exit()
    {
        if (_game != null && _game.IsRunning)
        {
            _game.Exit();
            _logger.LogTrace("Requested game application exit");
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        DisposeGameServices();
        DisposeProvider(_gameServiceProvider);
        DisposeProvider(_hostServiceProvider);
    }

    private static void DisposeProvider(IServiceProvider? provider)
    {
        if (provider is IDisposable disposable)
        {
            disposable.Dispose();
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

    private void OnGameExited(object? sender, EventArgs e)
    {
        _logger.LogInformation("Shutdown");
        _logger.LogDebug("Executing cleanup services ...");
        _startupServiceExecutor?.CleanUp();
    }

    private void OnGameDisposed(object? sender, EventArgs e)
    {
        if (_game != null)
        {
            _game.Launched -= OnGameLaunched;
            _game.Exited -= OnGameExited;
            _game.Disposed -= OnGameDisposed;
        }

        _game = null;
        _logger.LogDebug("Game disposed.");
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
