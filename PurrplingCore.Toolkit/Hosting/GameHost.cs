using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PurrplingCore.Toolkit.DI;
using PurrplingCore.Toolkit.Extensions;
using System.Diagnostics;

namespace PurrplingCore.Toolkit.Hosting;

public sealed class GameHost : IDisposable
{
    private readonly IServiceProvider _hostServiceProvider;
    private readonly IServiceCollection _gameServiceCollection;
    private readonly ILogger _logger;
    private readonly IHostEnvironment _environment;
    private StartupServiceExecutor? _startupServiceExecutor;
    private IGame? _game;
    
    private IServiceProvider? _gameServiceProvider;
    private bool _disposed;

    internal GameHost(
        IServiceProvider hostProvider, IServiceCollection gameServices, IHostEnvironment environment, ILogger logger)
    {
        _hostServiceProvider = hostProvider;
        _gameServiceCollection = gameServices;
        _logger = logger;
        _environment = environment;

        _gameServiceCollection.AddSingleton<StartupServiceExecutor>();
    }

    public ILogger Logger => _logger;

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

    internal void Initialize()
    {
        _logger.LogGameInformation(_environment);
        _logger.LogDebug("Environment: {EnvironmentName}", _environment.EnvironmentName);

        foreach (var serviceConfiguration in _hostServiceProvider.GetServices<IServicesConfiguration>())
        {
            serviceConfiguration.ConfigureServices(_gameServiceCollection);
        }

        IServiceFactoryAdapter factory = _hostServiceProvider.GetRequiredService<IServiceFactoryAdapter>();
        _gameServiceProvider = factory.CreateServiceProvider(_gameServiceCollection);
    }

    public void Run()
    {
        Debug.Assert(_gameServiceProvider != null, "Initialize must be called first.");
        ObjectDisposedException.ThrowIf(_disposed, this);

        _startupServiceExecutor = _gameServiceProvider.GetRequiredService<StartupServiceExecutor>();
        _game = ResolveGame();

        _logger.LogDebug("Executing startup services");
        _startupServiceExecutor.Startup();

        _logger.LogDebug("Executing {gameType}::Run()", _game.GetType());
        _game.Run();
    }

    private IGame ResolveGame()
    {
        var game = Services.GetRequiredService<IGame>();
        game.Exited += OnGameExited;
        game.Disposed += OnGameDisposed;

        return game;
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
            _game.Exited -= OnGameExited;
            _game.Disposed -= OnGameDisposed;
        }

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
