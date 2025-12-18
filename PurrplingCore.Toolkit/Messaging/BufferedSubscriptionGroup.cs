using System.ComponentModel;

namespace PurrplingCore.Toolkit.Messaging;

/// <summary>
/// A container for managing multiple <see cref="IBufferedSubscription"/> instances together.
/// Simplifies processing and disposing of multiple subscriptions.
/// </summary>
public sealed class BufferedSubscriptionGroup : IDisposable
{
    private readonly List<IBufferedSubscription> _subscriptions = [];
    private readonly object _lock = new();
    private bool _disposed;

    /// <summary>
    /// Adds an existing subscription to the group.
    /// </summary>
    public void Add(IBufferedSubscription subscription)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(BufferedSubscriptionGroup));

        lock (_lock)
        {
            ObjectDisposedException.ThrowIf(_disposed, nameof(BufferedSubscriptionGroup));
            _subscriptions.Add(subscription);
        }
    }

    /// <summary>
    /// Convenience method: Subscribes to the bus and immediately adds the subscription to this group.
    /// </summary>
    public IBufferedSubscription Subscribe<T>(IMessageBus bus, IMessageBus.Subscriber<T> handler) where T : notnull
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(BufferedSubscriptionGroup));

        var subscription = bus.SubscribeBuffered(handler);
        Add(subscription);

        return subscription;
    }

    /// <summary>
    /// Processes pending messages for ALL subscriptions in this group.
    /// </summary>
    /// <param name="maxBatchSizePerSub">Max messages to process per single subscription.</param>
    public void Process(int maxBatchSizePerSub = 0)
    {
        lock (_lock)
        {
            for (int i = 0; i < _subscriptions.Count; i++)
            {
                if (!_subscriptions[i].IsAlive)
                {
                    _subscriptions[i].Dispose();
                    _subscriptions.RemoveAt(i--);
                    continue;
                }

                _subscriptions[i].Process(maxBatchSizePerSub);
            }
        }
    }

    public void UnsubscribeAll()
    {
        lock (_lock)
        {
            foreach (var sub in _subscriptions)
            {
                sub.Unsubscribe();
            }

            _subscriptions.Clear();
        }
    }

    /// <summary>
    /// Unsubscribes and disposes all managed subscriptions.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        UnsubscribeAll();
        _disposed = true;
    }
}
