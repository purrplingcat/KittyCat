namespace PurrplingCore.Toolkit.Messaging;

/// <summary>
/// Buffered subscription interface for processing <see cref="IMessageBus"/> messages in batches.
/// Typically used in systems where messages need to be processed during update loops.
/// <example>
/// <code lang="csharp">
/// public class MySystem : BaseSystem 
/// {
///     private readonly IBufferedSubscription _subscription;
///     
///     public MySystem(IMessageBus messageBus) 
///     {
///         _subscription = messageBus.SubscribeBuffered&lt;MyMessage&gt;(OnMyMessage);
///     }
///     
///     void OnMyMessage(MyMessage message) 
///     {
///         Console.WriteLine($"Received message: {message.Content}");
///     }
/// 
///     void OnUpdateGroup() 
///     {
///         _subscription.Process();
///     }
/// }
/// </code>
/// </example>
/// </summary>
public interface IBufferedSubscription : ISubscription, IDisposable
{
    void Process(int maxBatchSize = 0);

    int Count { get; }
    bool IsEmpty { get; }
}
