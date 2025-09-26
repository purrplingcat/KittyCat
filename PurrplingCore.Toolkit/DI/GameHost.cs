using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PurrplingCore.Toolkit.Extensions;
using PurrplingCore.Toolkit.Rendering;

namespace PurrplingCore.Toolkit.DI;

public sealed class GameHost : IDisposable
{
    private readonly IServiceProvider _provider;
    private readonly IGame _game;
    private readonly ILogger<GameHost> _logger;
    private readonly IStartupService[] _startupServices;
    private readonly ICleanupService[] _cleanupServices;

    internal GameHost(IServiceProvider provider, IGame game, ILogger<GameHost> logger, IStartupService[] startupServices, ICleanupService[] cleanupServices)
    {
        _provider = provider;
        _game = game;
        _logger = logger;
        _startupServices = startupServices;
        _cleanupServices = cleanupServices;
        _game.Exited += OnGameExited;
        _game.Disposed += OnGameDisposed;
        logger.LogTrace("GameHost instance for {gameType}", Type);
    }

    public IServiceProvider Services => _provider;
    public Type Type => _game.GetType();

    public static IGameHostBuilder CreateBuilder()
    {
        return new GameHostBuilder();
    }

    public void Run()
    {
        _logger.LogInformation("Starting game: {gameName}", _game.ToString());
        _logger.LogDebug("Executing startup services");

        foreach (var startupService in _startupServices)
        {
            _logger.LogTrace("  Startup: {serviceType}", startupService.GetType());
            startupService.OnStartup();
        }

        _logger.LogDebug("Executing {gameType}::Run()", Type);
        _game.Run();
    }

    public void Exit() => _game.Exit();

    public void Dispose()
    {
        if (_provider is IDisposable disposableProvider)
        {
            disposableProvider.Dispose();
        }
    }

    private void OnGameExited(object? sender, EventArgs e)
    {
        _logger.LogInformation("Shutdown");

        _logger.LogDebug("Executing cleanup services ...");
        foreach (var cleanupService in _cleanupServices)
        {
            _logger.LogTrace("Cleanup: {serviceType}", cleanupService.GetType());
            cleanupService.OnCleanup();
        }
    }

    private void OnGameDisposed(object? sender, EventArgs e)
    {
        _game.Exited -= OnGameExited;
        _game.Disposed -= OnGameDisposed;
        _logger.LogDebug("Game disposed.");
    }
}
