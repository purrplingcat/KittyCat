namespace PurrplingCore.Toolkit.Messaging;

public interface ISubscription : IDisposable
{
    bool IsAlive { get; }
    void Unsubscribe();
}

public sealed class VoidSubscription : ISubscription
{
    private bool _alive = true;

    public bool IsAlive => _alive;

    public void Dispose()
    {
        _alive = false;
    }

    public void Unsubscribe() => Dispose();
}
