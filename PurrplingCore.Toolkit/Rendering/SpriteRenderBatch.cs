using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PurrplingCore.Toolkit.Rendering;

public class SpriteRenderBatch(SpriteBatch batch, ICamera camera, SpriteBatchConfig config)
{
    private readonly SpriteBatch _batch = batch;
    private readonly ICamera _camera = camera;
    private readonly SpriteBatchConfig _config = config;
    private bool _isBatchOpen;

    public Matrix Transform => _camera.ViewMatrix * _camera.ProjectionMatrix;
    public bool Active => _isBatchOpen;

    // --- 1. KRESLÍCÍ METODY (Forwarding) ---
    // Tady jen posíláme data dál. Můžeme přidat i vlastní logiku (např. automatický posun o 0.5px)

    public void Draw(Texture2D texture, Vector2 position, Color color)
    {
        _batch.Draw(texture, position, color);
    }

    public void Draw(Texture2D texture, Rectangle destination, Color color)
    {
        _batch.Draw(texture, destination, color);
    }

    public IDisposable With(Matrix? transform = null, BlendState? blend = null, SamplerState? sampler = null, Effect? effect = null)
    {
        return new BatchScope(this, transform, blend, sampler, effect);
    }

    private void StartBatch(Matrix? transform = null, BlendState? blend = null, SamplerState? sampler = null, Effect? effect = null)
    {
        if (_isBatchOpen)
        {
            _batch.End();
            _isBatchOpen = false;
        }

        _batch.Begin(
            sortMode: _config.SortMode,
            samplerState: sampler ?? _config.SamplerState,
            transformMatrix: transform ?? Transform,
            blendState: blend ?? _config.BlendState,
            effect: effect ?? _config.Effect
        );

        _isBatchOpen = true;
    }

    public void Begin() => StartBatch();

    public SpriteBatch AsSpriteBatch() => _batch;

    public void End()
    {
        if (_isBatchOpen)
        {
            _batch.End();
            _isBatchOpen = false;
        }
    }

    private readonly struct BatchScope : IDisposable
    {
        private readonly SpriteRenderBatch _parent;

        public BatchScope(SpriteRenderBatch parent, Matrix? transform = null, BlendState? blend = null, SamplerState? sampler = null, Effect? effect = null)
        {
            _parent = parent;
            _parent.StartBatch(transform, blend, sampler, effect);
        }

        public void Dispose()
        {
            if (_parent.Active)
            {
                _parent.End();
                _parent.Begin();
            }
        }
    }
}
