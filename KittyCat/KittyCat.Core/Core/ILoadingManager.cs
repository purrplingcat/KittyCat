using System;
using System.Collections;
using System.Threading.Tasks;

namespace KittyCat.Core;

internal interface ILoadingManager
{
    bool IsBusy { get; }

    void Enqueue(Func<IEnumerator> coroutineToRun, Action? onComplete = null, Action<Exception>? onError = null);
    void Enqueue(Func<Task> task, Action? onComplete = null, Action<Exception>? onError = null);
    void Enqueue(Func<IProgress<float>, Task> taskWithProgress, Action? onComplete = null, Action<Exception>? onError = null);
}
