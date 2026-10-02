using KittyCat.Configuration;
using PurrplingCore.Graphics;
using PurrplingCore.Hosting;
using KittyCat.Localization;
using KittyCat.Services;
using Microsoft.Extensions.Options;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PurrplingCore.Toolkit.DI;
using PurrplingCore.Toolkit.Graphics;
using PurrplingCore.Toolkit.Rendering;
using System;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace KittyCat;
/// <summary>
/// Configures the example game's resolution, content, input, and drawing lifecycle.
/// </summary>
[GameServices<KittyCatGameServices>]
public class KittyCatGame : PurrplingCore.Toolkit.Game
{
    private KeyboardState _previousKeyboardState;
    private Resolution? _resolution;

    public static Version Version { get; }
    public static string VersionInfo { get; }
    public static string GameName { get; }

    /// <summary>
    /// Creates the game with services supplied by the host.
    /// </summary>
    public KittyCatGame(IServiceProvider provider) : base(provider)
    {
        Title = "KittyCat Game";
    }

    static KittyCatGame()
    {
        GameName = "KittyCat";
        Version = typeof(KittyCatGame).Assembly
            .GetName()
            .Version ?? new Version();
        VersionInfo = typeof(KittyCatGame).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion ?? "";
    }

    /// <summary>
    /// Applies the playfield resolution and initializes localization.
    /// </summary>
    protected override void Initialize()
    {
        var resolution = GetRequiredService<Resolution>();
        var playfield = GetRequiredService<IOptions<StarCatcherOptions>>().Value.Playfield;

        _resolution = resolution;
        resolution.ResolutionChanged += OnResolutionChanged;
        resolution
            .SetResolution(playfield.Width, playfield.Height)
            .SetVirtualMode(VirtualResolutionMode.Fixed)
            .ApplyChanges();
        Window.AllowUserResizing = true;

        InitializeLocalization();
        base.Initialize();
    }

    private void OnResolutionChanged()
    {
        if (_resolution is null) return;

        var clientBounds = _resolution.WindowBounds;
        var presentation = _resolution.PresentationParameters;
        Logger.LogInformation(
            "Resolution changed: virtual {VirtualWidth}x{VirtualHeight}"
            + " | window {ClientWidth}x{ClientHeight}"
            + " | backbuffer {BackBufferWidth}x{BackBufferHeight} {Depth}",
            _resolution.Width, _resolution.Height,
            clientBounds.Width, clientBounds.Height,
            presentation.BackBufferWidth, presentation.BackBufferHeight,
            presentation.DepthStencilFormat);
    }

    private static void InitializeLocalization()
    {
        LocalizationManager.SetCulture(LocalizationManager.DEFAULT_CULTURE_CODE);
    }

    /// <summary>
    /// Updates the game's logic, called once per frame.
    /// </summary>
    /// <param name="gameTime">
    /// Provides a snapshot of timing values used for game updates.
    /// </param>
    protected override void Update(GameTime gameTime)
    {
        var keyboard = Keyboard.GetState();
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed
            || keyboard.IsKeyDown(Keys.Escape))
            Exit();

        if (keyboard.IsKeyDown(Keys.F11) && !_previousKeyboardState.IsKeyDown(Keys.F11))
        {
            var graphicsManager = GetRequiredService<GraphicsManager>();
            graphicsManager.ToggleFullScreen();
        }

        _previousKeyboardState = keyboard;
        base.Update(gameTime);
    }

    /// <summary>
    /// Draws the game's graphics, called once per frame.
    /// </summary>
    /// <param name="gameTime">
    /// Provides a snapshot of timing values used for rendering.
    /// </param>
    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        base.Draw(gameTime);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && _resolution is not null)
        {
            _resolution.ResolutionChanged -= OnResolutionChanged;
            _resolution = null;
        }

        base.Dispose(disposing);
    }
}
