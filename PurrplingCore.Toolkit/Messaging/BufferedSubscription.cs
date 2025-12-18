using System;
using System.Collections.Concurrent;

namespace PurrplingCore.Toolkit.Messaging;

internal sealed class BufferedSubscription<T> : IBufferedSubscription
    where T : notnull
{
    private readonly ConcurrentQueue<T> _queue = new();
    private readonly IMessageBus.Subscriber<T> _handler;
    private readonly ISubscription _subscription;

    public int Count => _queue.Count;
    public bool IsEmpty => _queue.IsEmpty;
    public bool IsAlive => _subscription.IsAlive;

    internal BufferedSubscription(IMessageBus bus, IMessageBus.Subscriber<T> handler)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        _subscription = bus.Subscribe<T>(OnMessageReceived);
    }

    private void OnMessageReceived(in T message)
    {
        _queue.Enqueue(message);
    }

    public void Process(int maxBatchSize = 0)
    {
        if (_queue.IsEmpty) return;

        int processedCount = 0;

        // "Vysajeme" frontu a aplikujeme uložený handler
        while (_queue.TryDequeue(out var message))
        {
            _handler(message);
            processedCount++;

            if (maxBatchSize > 0 && processedCount >= maxBatchSize)
            {
                break;
            }
        }
    }

    public void Unsubscribe() => Dispose();

    public void Dispose()
    {
        _subscription.Dispose();
        _queue.Clear();
    }
}
