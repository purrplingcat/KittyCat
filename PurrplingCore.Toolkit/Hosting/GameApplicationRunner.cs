using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace PurrplingCore.Toolkit.Hosting;

/// <summary>
/// Adapts MonoGame application events to the hosted game lifecycle and coordinates shutdown.
/// </summary>
internal sealed class GameApplicationRunner : IHostedApplication, IStoppableApplication, IDisposable
{
    private readonly IServiceProvider _services;
    private readonly GameApplicationLifetime _gameLifecycle;
    private readonly ILogger _logger;
    private IGame? _game;
    private CancellationTokenRegistration _stoppingRegistration;
    private bool _disposed;

    public GameApplicationRunner(IServiceProvider services, GameApplicationLifetime gameLifecycle, ILoggerFactory loggerFactory)
    {
        _services = services;
        _gameLifecycle = gameLifecycle;
        _logger = loggerFactory.CreateLogger("Game");
    }

    public bool IsRunning => _game?.IsRunning ?? false;

    public int Run()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_game != null)
        {
            throw new InvalidOperationException("The game application has already been started.");
        }

        _game = _services.GetRequiredService<IGame>();
        Subscribe(_game);

        _logger.LogDebug("Executing {GameType}::Run()", _game.GetType());
        try
        {
            _game.Run();
        }
        finally
        {
            StopApplication();
            _gameLifecycle.NotifyStopped();
            Unsubscribe(_game);
            _game = null;
        }

        return _gameLifecycle.Error is null ? 0 : 1;
    }

    public void Exit()
    {
        if (_game != null)
        {
            StopApplication();
            _logger.LogTrace("Requested game application exit");
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        StopApplication();
        _gameLifecycle.NotifyStopped();

        if (_game != null)
        {
            Unsubscribe(_game);
            _game = null;
        }
    }

    private void Subscribe(IGame game)
    {
        _stoppingRegistration = _gameLifecycle.GameStopping.Register(OnGameStopping);
        game.Initializing += OnGameInitializing;
        game.Initialized += OnGameInitialized;
        game.Launched += OnGameLaunched;
        game.Exited += OnGameExited;
        game.Disposed += OnGameDisposed;
    }

    private void Unsubscribe(IGame game)
    {
        if (game is null)
        {
            return;
        }

        _stoppingRegistration.Dispose();
        game.Initializing -= OnGameInitializing;
        game.Initialized -= OnGameInitialized;
        game.Launched -= OnGameLaunched;
        game.Exited -= OnGameExited;
        game.Disposed -= OnGameDisposed;
    }

    private void OnGameInitializing(object? sender, EventArgs e)
    {
        // Cancellation invokes every subsystem registered for the single initialization phase.
        if (!_gameLifecycle.NotifyInitializing())
        {
            throw new ApplicationException(
                "The game application was aborted.", 
                _gameLifecycle.Error
            );
        }
    }

    private void OnGameInitialized(object? sender, EventArgs e) => _gameLifecycle.NotifyInitialized();

    private void OnGameLaunched(object? sender, EventArgs e) => _gameLifecycle.NotifyStarted();

    private void OnGameExited(object? sender, EventArgs e)
    {
        _logger.LogInformation("Shutdown");
        StopApplication();
    }

    private void OnGameStopping() => _game?.Exit();

    private void OnGameDisposed(object? sender, EventArgs e)
    {
        if (sender is IGame game)
        {
            Unsubscribe(game);
        }
    }

    private void StopApplication()
    {
        if (!_gameLifecycle.IsStopping)
        {
            _gameLifecycle.StopApplication();
        }
    }

}