using Microsoft.Xna.Framework;

namespace PurrplingCore.Toolkit.Rendering;

public sealed class RenderPipeline(params IRenderPass[] passes) : IRenderPass, IPrepareRender, IInitializeRender, IDisposable
{
    private readonly IRenderPass[] _passes = passes;
    private readonly IPrepareRender[] _preparables = [.. passes.OfType<IPrepareRender>()];
    private readonly IInitializeRender[] _initializables = [.. passes.OfType<IInitializeRender>()];
    private bool _initialized;
    private bool _ready;

    public ReadOnlySpan<IRenderPass> Passes => _passes;

    public void Initialize()
    {
        if (_initialized) return;

        _initialized = true;
        for (int i = 0; i < _initializables.Length; i++)
        {
            _initializables[i].Initialize();
        }
    }

    public void Prepare(GameTime gameTime)
    {
        for (int i = 0; i < _preparables.Length; i++)
        {
            _preparables[i].Prepare(gameTime);
        }

        _ready = true;
    }

    public void Draw(GameTime gameTime)
    {
        if (!_ready) throw new InvalidOperationException("RenderPipeline is not prepared. Call Prepare() before Draw().");

        for (int i = 0; i < _passes.Length; i++)
        {
            _passes[i].Draw(gameTime);
        }

        _ready = false;
    }

    public void Uninitialize()
    {
        for (int i = 0; i < _initializables.Length; i++)
        {
            _initializables[i].Uninitialize();
        }
    }

    public void Dispose()
    {
        Uninitialize();
    }
}
