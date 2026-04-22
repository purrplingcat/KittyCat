using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PurrplingCore.Toolkit;
using PurrplingCore.Toolkit.DI;
using PurrplingCore.Toolkit.Graphics;
using System;

namespace KittyCat.Services;

[Singleton]
public class GraphicsManager : IStartupService
{
    private readonly PurrplingCore.Toolkit.Game _game;
    private readonly Resolution _resolution;
    private SpriteBatch? _spriteBatch;
    int IStartupService.Order => 0;

    public GraphicsDevice GraphicsDevice => _game.GraphicsDevice 
        ?? throw new InvalidOperationException("Graphics device is not ready yet!");

    public Viewport Viewport => _resolution.Viewport;
    public GraphicsDeviceManager GraphicsDeviceManager => _game.GraphicsDeviceManager;
    public SpriteBatch SpriteBatch => _spriteBatch ??= CreateSpriteBatch();
    public Resolution Resolution => _resolution;
    public bool IsReady => _game.IsInitialized && _game.GraphicsDevice != null;

    public GraphicsManager(PurrplingCore.Toolkit.Game game)
    {
        _game = game ?? throw new ArgumentNullException(nameof(game));
        _resolution = new Resolution(_game.GraphicsDeviceManager, game.Window);
    }

    public SpriteBatch CreateSpriteBatch()
    {
        return new SpriteBatch(GraphicsDevice);
    }

    public RenderTarget2D CreateRenderTarget()
    {
        return new RenderTarget2D(_game.GraphicsDevice, Viewport.Width, Viewport.Height);
    }

    public Canvas CreateCanvas()
    {
        return new Canvas(GraphicsDevice, _resolution);
    }

    public RenderTarget2D CreateRenderTarget(int width, int height)
    {
        return new RenderTarget2D(_game.GraphicsDevice, width, height);
    }

    public void OnStartup()
    {
        // Configure screen orientations.
        GraphicsDeviceManager.SupportedOrientations = DisplayOrientation.LandscapeLeft | DisplayOrientation.LandscapeRight;
    }

    public void ToggleFullScreen()
    {
        Resolution.SetMode(Resolution.Mode == ResolutionMode.Windowed 
            ? ResolutionMode.BorderlessWindowed
            : ResolutionMode.Windowed);
        Resolution.ApplyChanges();
    }
}
