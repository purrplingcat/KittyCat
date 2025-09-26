using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Runtime.CompilerServices;

namespace PurrplingCore.Toolkit.Rendering;

public class RenderPipeline
{
    private readonly List<IRenderer> _renderers = [];
    private IRenderer[] _cache = [];
    private bool _initialized;
    private bool _dirty;
    private int _pointer;

    public object? Key { get; set; }
    public string Name { get; set; } = "RenderPipeline";

    public RenderPipeline()
    {
    }

    public RenderPipeline(IEnumerable<IRenderer> renderers)
    {
        ArgumentNullException.ThrowIfNull(renderers, nameof(renderers));

        _renderers = [.. renderers];
    }

    public virtual void Initialize()
    {
        if (!_initialized)
        {
            _initialized = true;
            _renderers.ForEach(InitializeRenderer);
            RebuildCache();
        }
    }

    private void InitializeRenderer(IRenderer renderer)
    {
        renderer.StateChanged += MarkDirty;
        renderer.LoadContent();
    }

    public void AddRenderer(IRenderer renderer)
    {
        if (_initialized)
        {
            InitializeRenderer(renderer);
        }

        _dirty = true;
        _renderers.Add(renderer);
    }

    public void AddRenderers(IEnumerable<IRenderer> renderers)
    {
        foreach (IRenderer renderer in renderers)
        {
            AddRenderer(renderer);
        }
    }

    public void RemoveRenderer(IRenderer renderer)
    {
        renderer.StateChanged -= MarkDirty;
        renderer.Unload();

        _dirty = true;
        _renderers.Remove(renderer);
    }

    public void Clear()
    {
        _cache = [];
        _renderers.Clear();
    }

    public IEnumerable<IRenderer> GetRenderers() => _renderers.AsEnumerable();

    public IEnumerable<TRenderer> GetRenderers<TRenderer>() where TRenderer : IRenderer
    {
        return _renderers.OfType<TRenderer>();
    }

    public TRenderer? GetRenderer<TRenderer>() where TRenderer : IRenderer
    {
        return GetRenderers<TRenderer>().FirstOrDefault();
    }

    public void MarkDirty() => _dirty = true;

    protected void RebuildCache()
    {
        _dirty = false;
        _cache = _renderers
            .Where(r => r.Enabled && !r.IsDisposed)
            .OrderBy(r => r.Order)
            .ToArray();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Render(SpriteBatch batch, ICamera camera, GameTime gameTime)
    {
        Render(new RenderContext(batch, batch.GraphicsDevice, camera, gameTime));
    }

    private RenderContext _context;
    public virtual void Render(in RenderContext context)
    {
        if (_initialized)
        {
            _pointer = 0;
            _context = context;

            if (_dirty)
            {
                RebuildCache();
            }

            Next(); // Start drawing pipeline
        }
    }

    private void Next()
    {
        if (_pointer < _cache.Length)
        {
            _cache[_pointer++].Render(_context, Next);
        }
    }
}
