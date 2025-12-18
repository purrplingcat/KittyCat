namespace PurrplingCore.Toolkit.Messaging;

public sealed class BufferedSubscriber(IMessageBus bus) : IDisposable
{
    private readonly IMessageBus _bus = bus ?? throw new ArgumentNullException(nameof(bus));
    private readonly BufferedSubscriptionGroup _group = new();
    private bool _disposed;

    /// <summary>
    /// Subscribes to a message type on the bound bus and adds it to the internal buffer group.
    /// </summary>
    public IBufferedSubscription Subscribe<T>(IMessageBus.Subscriber<T> handler) where T : notnull
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(BufferedSubscriber));
        return _group.Subscribe(_bus, handler);
    }

    /// <summary>
    /// Processes pending messages for all managed subscriptions.
    /// </summary>
    public void Process(int maxBatchSizePerSub = 0)
    {
        _group.Process(maxBatchSizePerSub);
    }

    public void UnsubscribeAll()
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(BufferedSubscriber));
        _group.UnsubscribeAll();
    }

    public void Dispose()
    {
        if (_disposed) return;

        _group.Dispose();
        _disposed = true;
    }
}
