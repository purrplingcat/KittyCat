namespace PurrplingCore.Toolkit;

public interface IGame : IDisposable
{
    public bool IsRunning { get; }

    public void Run();
    void Exit();

    public event EventHandler<EventArgs>? Initializing;
    public event EventHandler<EventArgs>? Initialized;
    public event EventHandler<EventArgs>? Launched;
    public event EventHandler<EventArgs>? Exited;
    public event EventHandler<EventArgs>? Disposed;
}
