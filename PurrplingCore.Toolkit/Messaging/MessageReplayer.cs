using System.Runtime.InteropServices;

namespace PurrplingCore.Toolkit.Messaging;

public sealed class MessageReplayer<TMessage> : IFetchableMessageReplayer
    where TMessage : notnull
{
    private readonly List<TMessage> _buffer = [];
    private readonly Action<TMessage> _userHandler;
    private readonly IBufferedSubscription _subscription;

    public bool IsCollecting => _subscription.IsAlive;
    public bool HasMessages => _buffer.Count > 0;
    public ISubscription Subscription => _subscription;

    public MessageReplayer(BufferedSubscriber subscriber, Action<TMessage> userHandler)
    {
        _userHandler = userHandler ?? throw new ArgumentNullException(nameof(userHandler));
        _subscription = subscriber.Subscribe<TMessage>(Collect);
    }

    public MessageReplayer(IMessageBus bus, Action<TMessage> userHandler)
    {
        _userHandler = userHandler ?? throw new ArgumentNullException(nameof(userHandler));
        _subscription = bus.SubscribeBuffered<TMessage>(Collect);
    }

    private void Collect(in TMessage message)
    {
        _buffer.Add(message);
    }

    public bool Fetch(int maxBatchSize = 0)
    {
        if (_subscription.IsEmpty)
        {
            return false;
        }

        _subscription.Process(maxBatchSize);
        return true;
    }

    public void Replay()
    {
        foreach (var msg in CollectionsMarshal.AsSpan(_buffer))
        {
            _userHandler(msg);
        }
    }

    public void Clear()
    {
        _buffer.Clear();
    }
}
