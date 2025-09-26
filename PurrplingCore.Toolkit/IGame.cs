namespace PurrplingCore.Toolkit;

public interface IGame : IDisposable
{
    public void Run();
    void Exit();

    public event EventHandler<EventArgs>? Exited;
    public event EventHandler<EventArgs>? Disposed;
}
