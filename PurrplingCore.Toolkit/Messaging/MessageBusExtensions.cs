namespace PurrplingCore.Toolkit.Messaging;

public static class MessageBusExtensions
{
    public static IBufferedSubscription SubscribeBuffered<T>(this IMessageBus bus, IMessageBus.Subscriber<T> handler)
        where T : notnull
    {
        return new BufferedSubscription<T>(bus, handler);
    }

    public static BufferedSubscriber ToBufferedSubscriber(this IMessageBus bus)
    {
        return new BufferedSubscriber(bus);
    }
}
