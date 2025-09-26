using KittyCat.Core.Extensions;
using KittyCat.Core.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using PurrplingCore.Toolkit;
using PurrplingCore.Toolkit.Graphics;
using System;

namespace KittyCat.Core.Scenes;

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
            using (GraphicsDevice.UseCanvas(_canvas))
            {
                GraphicsDevice.Clear(clearColor);
                Draw(time);
            }
        }

        return _canvas;
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
