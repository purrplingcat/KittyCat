using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PurrplingCore.Toolkit.Graphics;

public class Screen
{
    private readonly Resolution _resolution;
    private readonly float _pixelZoom;
    private readonly ILogger? _logger;
    private RenderTarget2D _renderTarget;

    public event EventHandler<Rectangle>? Resized;
    public Rectangle ClientBounds => new(Point.Zero, _resolution.Size);
    public Rectangle Bounds => _renderTarget.Bounds;
    public Viewport Viewport => _resolution.Viewport;
    public RenderTarget2D RenderTarget => _renderTarget;

    public Screen(Resolution resolution, float pixelZoom)
    {
        _resolution = resolution;
        _pixelZoom = pixelZoom;
        _renderTarget = CreateRenderTarget();
        resolution.ResolutionChanged += OnResolutionChanged;
    }

    public Screen(Resolution resolution, float pixelZoom, ILogger logger) : this(resolution, pixelZoom)
    {
        _logger = logger;
    }

    private RenderTarget2D CreateRenderTarget()
    {
        return new RenderTarget2D(
            graphicsDevice: _resolution.GraphicsDevice,
            width: (int)(_resolution.Width / _pixelZoom),
            height: (int)(_resolution.Height / _pixelZoom)
        );
    }

    private void OnResolutionChanged()
    {
        _renderTarget = CreateRenderTarget();
        _logger?.LogDebug("Resolution changed: {width}x{height} (virtual screen: {virtualWidth}x{virtualHeight})", 
            ClientBounds.Width, ClientBounds.Height, Bounds.Width, Bounds.Height
        );
    }

    public virtual void Draw(SpriteBatch spriteBatch, Color color)
    {
        spriteBatch.Draw(_renderTarget, ClientBounds, color);
    }

    public virtual void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Draw(_renderTarget, ClientBounds, Color.White);
    }
}
