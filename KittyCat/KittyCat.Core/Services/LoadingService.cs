using KittyCat.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KittyCat.Services;

/// <summary>
/// Univerzální správce pro spouštění dlouhotrvajících operací (jak asynchronních, tak vícesnímkových).
/// Pracuje jako fronta, která spouští vždy jen jednu operaci najednou.
/// </summary>
public class LoadingService : GameComponent, ILoadingManager
{
    private record QueuedTask(Func<IEnumerator> Corutine, Action? OnComplete, Action<Exception>? OnError);

    private readonly Queue<QueuedTask> _taskQueue = new();
    private readonly ILogger<LoadingService> _logger;

    private IEnumerator? _activeJob;
    private Action? _onCompleteCallback;
    private Action<Exception>? _onErrorCallback;

    /// <summary>
    /// Vrací true, pokud právě běží nějaká úloha nebo nějaká čeká ve frontě.
    /// </summary>
    public bool IsBusy => _activeJob != null || _taskQueue.Count > 0;

    /// <summary>
    /// Progress aktuálně běžící úlohy (0.0 až 1.0).
    /// </summary>
    public float Progress { get; private set; }

    public int QueueLength => _taskQueue.Count;

    public LoadingService(Game game, ILogger<LoadingService> logger) : base(game)
    {
        _logger = logger;
        Enabled = true;
    }

    /// <summary>
    /// Run a coroutine as a loading operation.
    /// </summary>
    /// <param name="coroutineToRun">Funkce, která vrací IEnumerator (např. samotná korutina nebo Task.AsCoroutine()).</param>
    /// <param name="onComplete">Akce, která se zavolá po úspěšném dokončení.</param>
    /// <param name="onError">Akce, která se zavolá, pokud úloha selže s výjimkou.</param>
    public void Enqueue(Func<IEnumerator> coroutineToRun, Action? onComplete = null, Action<Exception>? onError = null)
    {
        _taskQueue.Enqueue(new QueuedTask(coroutineToRun, onComplete, onError));
    }

    /// <summary>
    /// Run asynchronous <see cref="Task"/> as a loading operation.
    /// </summary>
    /// <param name="task">A function returns <see cref="Task"/> to run as loading operation</param>
    /// <param name="onComplete">Action executed when task is completed</param>
    /// <param name="onError">Action executed when task fails.</param>
    public void Enqueue(Func<Task> task, Action? onComplete = null, Action<Exception>? onError = null)
    {
        Enqueue(() => task().AsCoroutine(), onComplete, onError);
    }

    /// <summary>
    /// Run asynchronous <see cref="Task"/> with progress reporting as a loading operation.
    /// </summary>
    /// <param name="taskWithProgress">
    /// A function that returns <see cref="Task"/> to run as loading operation.
    /// </param>
    /// <param name="onComplete">Action to be executed when task is completed.</param>
    /// <param name="onError">Action to be executed when task fails.</param>
    public void Enqueue(
        Func<IProgress<float>, Task> taskWithProgress,
        Action? onComplete = null,
        Action<Exception>? onError = null)
    {
        var progressReporter = new Progress<float>(p => Progress = p);
        IEnumerator coroutineWrapper() => taskWithProgress(progressReporter).AsCoroutine();

        Enqueue(coroutineWrapper, onComplete, onError);
    }

    public override void Update(GameTime gameTime)
    {
        // Start new task if none is active and there are tasks in the queue.
        if (_activeJob == null && _taskQueue.Count > 0)
        {
            var next = _taskQueue.Dequeue();
            _onCompleteCallback = next.OnComplete;
            _onErrorCallback = next.OnError;
            Progress = 0f;

            _logger.LogDebug("Starting new loading task from queue. {Count} remaining.", _taskQueue.Count);
            _activeJob = next.Corutine.Invoke();
        }

        if (_activeJob == null) return; // No active task to process

        try
        {
            if (!_activeJob.MoveNext())
            {
                // Loading task is complete.
                Progress = 1f;
                _activeJob = null; // Uvolníme místo pro další.
                _logger.LogInformation("Loading task finished successfully.");
                _onCompleteCallback?.Invoke();
            }
            else
            {
                // Report progress if the coroutine yields a float value.
                if (_activeJob.Current is float progress)
                {
                    Progress = MathHelper.Clamp(progress, 0f, 1f);
                }
            }
        }
        catch (Exception ex)
        {
            // Zde se zachytí výjimka (např. z Task.AsCoroutine() nebo z logiky korutiny).
            _logger.LogError(ex, "loading task failed with an exception.");
            _onErrorCallback?.Invoke(ex);
            _activeJob = null; // Uvolníme místo pro další.
        }
    }
}

public static class TaskExtensions
{
    /// <summary>
    /// Převede asynchronní Task na korutinu (IEnumerator),
    /// kterou může zpracovávat LoadingManager.
    /// </summary>
    public static IEnumerator AsCoroutine(this Task task)
    {
        while (!task.IsCompleted)
        {
            yield return null;
        }

        if (task.IsFaulted)
        {
            throw task.Exception!;
        }
    }
}