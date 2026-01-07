using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PurrplingCore.Toolkit.Rendering;

public class RenderLayer : IRenderPass, IPrepareRender, IInitializeRender
{
    private static readonly DefaultCamera DEFAULT_CAMERA = new();
    private readonly SpriteBatch _batch;
    private readonly ICamera _camera;

    private readonly IRenderFeature2D[] _features;
    private readonly IPrepareRender[] _preparables;
    private readonly IInitializeRender[] _initializables;
    private SpriteBatchConfig _config;
    private SpriteRenderBatch _renderContext;
    private bool _initialized;
    

    public SpriteBatchConfig Config
    {
        get => _config; 
        set
        {
            _config = value;
            _renderContext = new SpriteRenderBatch(_batch, _camera, _config);
        }
    }

    public ReadOnlySpan<IRenderFeature2D> Features => _features;

    public RenderLayer(IRenderFeature2D[] features, SpriteBatch batch, ICamera? camera)
    {
        _features = features;
        _batch = batch;
        _camera = camera ?? DEFAULT_CAMERA;
        _preparables = [.. features.OfType<IPrepareRender>()];
        _initializables = [.. features.OfType<IInitializeRender>()];
        _renderContext = new SpriteRenderBatch(_batch, _camera, _config);
    }

    private class DefaultCamera : ICamera
    {
        public Matrix ViewMatrix { get; } = Matrix.Identity;

        public Matrix ProjectionMatrix { get; } = Matrix.Identity;

        public BoundingFrustum Frustum { get; } = new(Matrix.Identity);

        public void UpdateState(Matrix view, Matrix projection)
        {
            // Internal default camera is not updateable,
            // cause it represents no-camera rendering.
            throw new NotImplementedException();
        }
    }

    public void Draw(GameTime gameTime)
    {
        if (_features.Length == 0) return;
        
        _renderContext.Begin();

        try
        {
            for (int i = 0; i < _features.Length; i++)
            {
                _features[i].Draw(_renderContext, gameTime);
            }
        }
        finally
        {
            _renderContext.End();
        }
    }

    public void Prepare(GameTime gameTime)
    {
        // Propagujeme update jen těm, kdo ho chtějí
        for (int i = 0; i < _preparables.Length; i++)
        {
            _preparables[i].Prepare(gameTime);
        }
    }

    public void Initialize()
    {
        if (_initialized) return;

        _initialized = true;
        for (int i = 0; i < _initializables.Length; i++)
        {
            _initializables[i].Initialize();
        }
        LoadContent();
    }

    protected virtual void LoadContent() { }
}
