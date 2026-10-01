using PurrplingCore.Graphics;
using PurrplingCore.Toolkit.Rendering;

namespace PurrplingCore.Rendering;

/// <summary>Flushes renderers in one named group, ordered by <see cref="IRenderer.Order"/>.</summary>
public sealed class RendererGroup
{
    private readonly IRenderer[] _renderers;
    private readonly GraphicsManager _graphics;
    private bool _initialized;

    public RendererGroup(string name, IEnumerable<IRenderer> renderers, GraphicsManager graphics)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(renderers);
        ArgumentNullException.ThrowIfNull(graphics);

        Name = name;
        _renderers = [
            ..renderers
                .Where(renderer => renderer.RenderGroup == name)
                .OrderBy(renderer => renderer.Order)
        ];
        _graphics = graphics;
    }

    public string Name { get; }

    /// <summary>Initializes all renderers in the group before their first flush.</summary>
    public void Initialize()
    {
        if (_initialized) return;

        var device = _graphics.GraphicsDevice;
        for (var index = 0; index < _renderers.Length; index++)
        {
            _renderers[index].Initialize(device);
        }

        _initialized = true;
    }

    /// <summary>Renders queued commands in group order after <see cref="Initialize"/>.</summary>
    public void Flush()
    {
        if (!_initialized)
        {
            throw new InvalidOperationException("Initialize the renderer group before flushing it.");
        }

        var size = _graphics.Resolution.Size;
        var device = _graphics.GraphicsDevice;
        for (var index = 0; index < _renderers.Length; index++)
        {
            if (_renderers[index] is Renderer2D renderer)
            {
                renderer.VirtualResolution = size;
            }

            _renderers[index].Flush(device);
        }
    }
}