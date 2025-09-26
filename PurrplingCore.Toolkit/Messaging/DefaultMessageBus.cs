using PurrplingCore.Toolkit.Messages;

namespace PurrplingCore.Toolkit.Messaging;

internal sealed class DefaultMessageBus : IMessageBus
{
    private readonly Dictionary<Type, Delegate> _subscribers = [];

    public void Publish<T>(in T message)
    {
        var messageType = typeof(T);
        if (_subscribers.TryGetValue(messageType, out var subscribers))
        {
            ((IMessageBus.Subscriber<T>)subscribers).Invoke(message);
        }
    }

    public void Subscribe<T>(IMessageBus.Subscriber<T> subscriber)
    {
        var messageType = typeof(T);
        if (_subscribers.TryGetValue(messageType, out var existingSubscribers))
        {
            _subscribers[messageType] = Delegate.Combine(existingSubscribers, subscriber);
        }
        else
        {
            _subscribers[messageType] = subscriber;
        }
    }
}
