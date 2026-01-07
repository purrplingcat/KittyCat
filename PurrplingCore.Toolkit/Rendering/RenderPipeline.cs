using Microsoft.Xna.Framework;

namespace PurrplingCore.Toolkit.Rendering;

public sealed class RenderPipeline(params IRenderPass[] passes) : IRenderPass, IPrepareRender, IInitializeRender
{
    private readonly IRenderPass[] _passes = passes;
    private readonly IPrepareRender[] _preparables = [.. passes.OfType<IPrepareRender>()];
    private readonly IInitializeRender[] _initializables = [.. passes.OfType<IInitializeRender>()];
    private bool _initialized;

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
    }

    public void Draw(GameTime gameTime)
    {
        for (int i = 0; i < _passes.Length; i++)
        {
            _passes[i].Draw(gameTime);
        }
    }
}
