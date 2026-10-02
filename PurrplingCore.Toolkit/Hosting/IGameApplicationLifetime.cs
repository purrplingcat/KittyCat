using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace PurrplingCore.Toolkit.Hosting;

/// <summary>
/// Provides cancellation tokens that signal the major phases of a hosted game's lifecycle.
/// </summary>
public interface IGameApplicationLifetime
{
    /// <summary>
    /// Signals entry into MonoGame initialization.
    /// </summary>
    CancellationToken GameInitializing { get; }

    /// <summary>
    /// Signals that MonoGame initialization and its registered callbacks completed.
    /// </summary>
    CancellationToken GameInitialized { get; }

    /// <summary>
    /// Signals that the game loop has started.
    /// </summary>
    CancellationToken GameStarted { get; }

    /// <summary>
    /// Signals that application shutdown was requested.
    /// </summary>
    CancellationToken GameStopping { get; }

    /// <summary>
    /// Signals that the game loop and shutdown work completed.
    /// </summary>
    CancellationToken GameStopped { get; }

    /// <summary>
    /// Requests application shutdown. Repeated requests are ignored.
    /// </summary>
    void StopApplication();
}

/// <summary>
/// Owns one-shot cancellation sources that implement the hosted game lifecycle.
/// </summary>
internal sealed partial class GameApplicationLifetime(ILoggerFactory loggerFactory) : IGameApplicationLifetime, IDisposable
{
    private readonly ILogger _logger = loggerFactory.CreateLogger("Game");
    private readonly CancellationTokenSource _initializing = new();
    private readonly CancellationTokenSource _initialized = new();
    private readonly CancellationTokenSource _started = new();
    private readonly CancellationTokenSource _stopping = new();
    private readonly CancellationTokenSource _stopped = new();
    private int _initializingSignaled;
    private int _initializedSignaled;
    private int _startedSignaled;
    private int _stoppingSignaled;
    private int _stoppedSignaled;
    private Exception? _error;

    public CancellationToken GameInitializing => _initializing.Token;
    public CancellationToken GameInitialized => _initialized.Token;
    public CancellationToken GameStarted => _started.Token;
    public CancellationToken GameStopping => _stopping.Token;
    public CancellationToken GameStopped => _stopped.Token;

    internal bool IsStopping => Volatile.Read(ref _stoppingSignaled) != 0;
    internal Exception? Error => Volatile.Read(ref _error);

    public void StopApplication()
    {
        lock (_stopping) 
        {
            Notify(_stopping, ref _stoppingSignaled, Phase.Stopping);
        }
    
    }

    public bool NotifyInitializing() => Notify(_initializing, ref _initializingSignaled, Phase.Initializing);

    public bool NotifyInitialized() => Notify(_initialized, ref _initializedSignaled, Phase.Initialized);

    public bool NotifyStarted() => Notify(_started, ref _startedSignaled, Phase.Started);

    public bool NotifyStopped() => Notify(_stopped, ref _stoppedSignaled, Phase.Stopped);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool Notify(CancellationTokenSource source, ref int signaled, Phase phase)
    {
        // Every lifecycle phase is one-shot even when several shutdown paths converge here.
        if (Interlocked.Exchange(ref signaled, 1) != 0)
        {
            return false;
        }

        try 
        {
            LogPhase(_logger, phase);
            source.Cancel();
            return true;
        }
        catch (Exception ex)
        {
            Interlocked.CompareExchange(ref _error, ex, null);
            _logger.LogError(ex, "Error notifying phase {Phase}", phase);
            return false;
        }
    }

    [LoggerMessage(LogLevel.Trace, "Game lifecycle: {Phase}")]
    private static partial void LogPhase(ILogger logger, Phase phase);

    private enum Phase : byte
    {
        Initializing,
        Initialized,
        Started,
        Stopping,
        Stopped,
    }

    public void Dispose()
    {
        _initializing.Dispose();
        _initialized.Dispose();
        _started.Dispose();
        _stopping.Dispose();
        _stopped.Dispose();
    }
}