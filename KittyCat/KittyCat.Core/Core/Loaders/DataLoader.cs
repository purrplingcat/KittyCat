using KittyCat.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KittyCat.Core.Loaders;

public interface IDataLoader
{
    void OnComplete();
    void OnError(Exception error);
    IEnumerator Load();
}

public abstract class DataLoader : IDataLoader
{
    private readonly Queue<Func<IEnumerator>> _tasks = new();
    public event Action? Completed;
    public event Action<Exception>? Error;

    public DataLoader()
    {
        Initialize();
    }

    IEnumerator IDataLoader.Load()
    {
        while (_tasks.Count > 0)
        {
            var task = _tasks.Dequeue();
            var routine = task();

            while (routine.MoveNext())
            {
                yield return routine.Current;
            }
        }
    }

    protected abstract void Initialize();

    protected void AddTask(Func<IEnumerator> action)
    {
        _tasks.Enqueue(action);
    }

    protected void AddTask(Action action)
    {
        IEnumerator runAction()
        {
            action();
            yield return null;
        }

        _tasks.Enqueue(runAction);
    }

    protected void AddTask(Func<Task> action)
    {
        _tasks.Enqueue(() => action().AsCoroutine());
    }

    void IDataLoader.OnComplete() => Completed?.Invoke();

    void IDataLoader.OnError(Exception error) => Error?.Invoke(error);
}
