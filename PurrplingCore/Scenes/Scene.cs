using PurrplingCore.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using PurrplingCore.Toolkit;
using PurrplingCore.Toolkit.Graphics;
using System;

namespace PurrplingCore.Scenes;

/// <summary>
/// Determines how a scene's drawing reaches the screen.
/// </summary>
public enum SceneRenderMode
{
    /// <summary>
    /// The scene draws immediately into its own canvas, which the manager then blits.
    /// </summary>
    Canvas,

    /// <summary>
    /// The scene only queues commands into a renderer, which presents them later in the draw
    /// loop. The scene's canvas is never opened.
    /// </summary>
    Deferred,
}

public abstract class Scene : IDisposable
{
    private readonly ContentManager _content;
    private readonly Canvas _canvas;
    private readonly GraphicsManager _graphics;
    private readonly string _name;
    protected Color clearColor = Color.Black;

    #region flags
    private bool _initialized;
    private bool _active;
    private bool _disposed;
    #endregion

    protected GraphicsDevice GraphicsDevice => _graphics.GraphicsDevice;
    protected ContentManager Content => _content;
    protected SpriteBatch SpriteBatch => _graphics.SpriteBatch;

    public virtual string Name => _name;
    public string? Title { get; set; }

    /// <summary>
    /// How this scene reaches the screen. Override to return <see cref="SceneRenderMode.Deferred"/>
    /// when <see cref="Draw"/> queues into a renderer instead of drawing immediately.
    /// </summary>
    public virtual SceneRenderMode RenderMode => SceneRenderMode.Canvas;


    #region Events
    public event Action<Scene>? Activated;
    public event Action<Scene>? Deactivated;
    public event Action? Disposed;
    #endregion

    public Scene(GraphicsManager graphics, IContentManagerProvider contentProvider)
    {
        ArgumentNullException.ThrowIfNull(graphics, nameof(graphics));
        ArgumentNullException.ThrowIfNull(contentProvider, nameof(contentProvider));

        _graphics = graphics;
        _canvas = graphics.CreateCanvas();
        _content = contentProvider.CreateContentManager();
        _name = GetType().Name;
    }

    protected abstract void Update(GameTime time, bool isActive);
    protected abstract void Draw(GameTime time);

    protected virtual void LoadContent()
    {
    }

    protected virtual void UnloadContent()
    {
    }

    protected virtual void OnInitialize()
    {
    }

    protected virtual void OnActivate()
    {
        Activated?.Invoke(this);
    }

    protected virtual void OnDeactivate()
    {
        Deactivated?.Invoke(this);
    }

    public void Initialize()
    {
        if (!_initialized)
        {
            OnInitialize();
            LoadContent();
            _initialized = true;
        }
    }

    public void Activate()
    {
        _active = true;

        Initialize();
        OnActivate();
    }

    public void Deactivate()
    {
        _active = false;
        OnDeactivate();
    }


    public void Update(GameTime time)
    {
        if (_initialized)
        {
            Initialize();
        }

        Update(time, _active);
    }

    public Canvas Render(GameTime time)
    {
        if (_initialized)
        {
            using var _ = _canvas.Open();
            GraphicsDevice.Clear(clearColor);
            Draw(time);
        }

        return _canvas;
    }

    /// <summary>
    /// Runs <see cref="Draw"/> without binding the canvas, for scenes in
    /// <see cref="SceneRenderMode.Deferred"/> mode.
    /// </summary>
    public void Submit(GameTime time)
    {
        if (_initialized)
        {
            Draw(time);
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                Deactivate();
                UnloadContent();
            }

            _disposed = true;
            Disposed?.Invoke();
        }
    }

    ~Scene()
    {
        Dispose(disposing: false);
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
