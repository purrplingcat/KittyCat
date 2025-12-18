namespace PurrplingCore.Toolkit.Messaging;

public interface IMessageReplayer
{
    public bool IsCollecting { get; }
    public bool HasMessages { get; }
    public ISubscription Subscription { get; }

    void Clear();
    void Replay();
}
