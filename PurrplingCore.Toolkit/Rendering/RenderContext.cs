using Friflo.Engine.ECS;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PurrplingCore.Toolkit.Rendering;

public class RenderContext(SpriteBatch batch, GraphicsDevice graphics) : IDisposable
{
    private static readonly EntityStore _store = new();
    private readonly SpriteBatch _batch = batch;
    private readonly GraphicsDevice _graphics = graphics;
    private readonly Entity _entity = _store.CreateEntity();
    private bool _isBatchOpen;
    private bool disposedValue;

    public Matrix Transform { get; set; } = Matrix.Identity;
    public SpriteBatch Batch => _batch;
    public GraphicsDevice GraphicsDevice => _graphics;

    public SpriteSortMode SortMode { get; set; } = SpriteSortMode.Deferred;
    public BlendState? BlendState { get; set; } = null;
    public SamplerState? SamplerState { get; set; } = null;
    public RasterizerState? RasterizerState { get; set; } = null;
    public Effect? Effect { get; set; } = null;

    public bool IsBatchOpen => _isBatchOpen;

    public void BeginBatch(Matrix? transform = null, BlendState? blend = null, SamplerState? sampler = null, Effect? effect = null)
    {
        if (_isBatchOpen)
        {
            _batch.End();
            _isBatchOpen = false;
        }

        _batch.Begin(
            sortMode: SortMode,
            samplerState: sampler ?? SamplerState,
            transformMatrix: transform ?? Transform,
            blendState: blend ?? BlendState,
            effect: effect ?? Effect
        );

        _isBatchOpen = true;
    }

    public void EndBatch()
    {
        if (_isBatchOpen)
        {
            _batch.End();
            _isBatchOpen = false;
        }
    }

    public void SetData<T>(T data = default) where T : struct, IComponent
    {
        _entity.AddComponent(data);
    }

    public ref T GetData<T>() where T : struct, IComponent
    {
        return ref _entity.GetComponent<T>();
    }

    public bool HasData<T>() where T : struct, IComponent
    {
        return _entity.HasComponent<T>();
    }

    public bool TryGetData<T>(out T component) where T : struct, IComponent
    {
        return _entity.TryGetComponent(out component);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                if (!_entity.IsNull)
                {
                    _entity.DeleteEntity();
                }
            }

            disposedValue = true;
        }
    }


    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
