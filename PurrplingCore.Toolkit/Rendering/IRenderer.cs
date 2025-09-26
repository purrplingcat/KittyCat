namespace PurrplingCore.Toolkit.Rendering;

public interface IRenderer : IDisposable
{
    int Order { get; }
    bool Enabled { get; }
    bool IsDisposed { get; }

    event Action StateChanged;

    void LoadContent();
    void Render(in RenderContext context, Action next);
    void Unload();
}
