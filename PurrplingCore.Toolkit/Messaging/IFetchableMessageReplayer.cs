namespace PurrplingCore.Toolkit.Messaging;

public interface IFetchableMessageReplayer : IMessageReplayer
{
    bool Fetch(int maxBatchSize = 0);
}
