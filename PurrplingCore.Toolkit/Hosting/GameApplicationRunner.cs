using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace PurrplingCore.Toolkit.Hosting;

/// <summary>
/// Adapts MonoGame application events to the hosted game lifecycle and coordinates shutdown.
/// </summary>
internal sealed class GameApplicationRunner : IHostedApplication, IDisposable
{
    private readonly IServiceProvider _services;
    private readonly GameApplicationLifetime _gameLifecycle;
    private readonly IHostEnvironment _env;
    private readonly ILogger _logger;
    private IGame? _game;
    private bool _disposed;

    public GameApplicationRunner(
        IServiceProvider services, 
        GameApplicationLifetime gameLifecycle, 
        IHostEnvironment environment, 
        ILoggerFactory loggerFactory
    )
    {
        _services = services;
        _gameLifecycle = gameLifecycle;
        _env = environment;
        _logger = loggerFactory.CreateLogger("Game");
    }

    public bool IsRunning => _game?.IsRunning ?? false;

    public int Run(CancellationToken token)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_game != null)
        {
            throw new InvalidOperationException("The game application has already been started.");
        }

        using var registration = token.Register(RequestGameExit);
        _game = _services.GetRequiredService<IGame>();

        try
        {
            Subscribe(_game);
            _logger.LogDebug("Executing {GameType}::Run() ThreadId: {ThreadId}", _game.GetType(), _env.ThreadId);
            _game.Run();
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Error while running the game application.");
            throw;
        }
        finally
        {
            Unsubscribe(_game);
            _gameLifecycle.NotifyStopped();
            _game = null;
            _logger.LogInformation("Game application has exited.");
        }

        return _gameLifecycle.Error is null ? 0 : 1;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_game != null)
        {
            Unsubscribe(_game);
            _game = null;
        }
    }

    private void Subscribe(IGame game)
    {
        game.Initializing += OnGameInitializing;
        game.Initialized += OnGameInitialized;
        game.Launched += OnGameLaunched;
        game.Exiting += OnGameExiting;
        game.Disposed += OnGameDisposed;
    }

    private void Unsubscribe(IGame game)
    {
        game.Initializing -= OnGameInitializing;
        game.Initialized -= OnGameInitialized;
        game.Launched -= OnGameLaunched;
        game.Exiting -= OnGameExiting;
        game.Disposed -= OnGameDisposed;
    }

    private void OnGameExiting(object? sender, EventArgs e)
    {
        if (!_gameLifecycle.GameStopping.IsCancellationRequested)
        {
            _gameLifecycle.StopApplication();
        }
    }

    private void OnGameInitializing(object? sender, EventArgs e) => _gameLifecycle.NotifyInitializing();

    private void OnGameInitialized(object? sender, EventArgs e) => _gameLifecycle.NotifyInitialized();

    private void OnGameLaunched(object? sender, EventArgs e) => _gameLifecycle.NotifyStarted();

    private void RequestGameExit() => _game?.Exit();

    private void OnGameDisposed(object? sender, EventArgs e)
    {
        if (sender is IGame game)
        {
            Unsubscribe(game);
        }
    }
}
