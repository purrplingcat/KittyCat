namespace PurrplingCore.Toolkit.Messaging;

public interface IMessageBus
{
    delegate void Subscriber<T>(in T message);
    void Publish<T>(in T message) where T : notnull;
    ISubscription Subscribe<T>(Subscriber<T> subscriber);
}
