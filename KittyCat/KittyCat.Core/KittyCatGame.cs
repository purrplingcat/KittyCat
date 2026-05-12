using KittyCat.Configuration;
using KittyCat.Localization;
using KittyCat.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PurrplingCore.Toolkit;
using PurrplingCore.Toolkit.DI;
using PurrplingCore.Toolkit.Graphics;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace KittyCat;
/// <summary>
/// The main class for the game, responsible for managing game components, settings, 
/// and platform-specific configurations.
/// </summary>
[GameServices<KittyCatGameServices>]
public class KittyCatGame : PurrplingCore.Toolkit.Game
{
    public static Version Version { get; }
    public static string VersionInfo { get; }
    public static string GameName { get; }

    /// <summary>
    /// Initializes a new instance of the game. Configures platform-specific settings, 
    /// initializes services like settings and leaderboard managers, and sets up the 
    /// screen manager for screen transitions.
    /// </summary>
    public KittyCatGame(IServiceProvider provider) : base(provider)
    {
        Title = "KittyCat Game";
    }

    static KittyCatGame()
    {
        // Set the game name and version information.
        GameName = "KittyCat";
        Version = typeof(KittyCatGame).Assembly
            .GetName()
            .Version ?? new Version();
        VersionInfo = typeof(KittyCatGame).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion ?? "";
    }

    /// <summary>
    /// Initializes the game, including setting up localization and adding the 
    /// initial screens to the ScreenManager.
    /// </summary>
    protected override void Initialize()
    {
        var resolution = GetRequiredService<Resolution>();

        resolution.SetResolution(1280, 720).ApplyChanges();
        //resolution.ApplyChanges(); 

        InitializeLocalization();
        base.Initialize();
    }

    private static void InitializeLocalization()
    {
        // Load supported languages and set the default language.
        List<CultureInfo> cultures = LocalizationManager.GetSupportedCultures();
        var languages = new List<CultureInfo>();
        for (int i = 0; i < cultures.Count; i++)
        {
            languages.Add(cultures[i]);
        }

        // TODO You should load this from a settings file or similar,
        // based on what the user or operating system selected.
        var selectedLanguage = LocalizationManager.DEFAULT_CULTURE_CODE;
        LocalizationManager.SetCulture(selectedLanguage);
    }

    /// <summary>
    /// Loads game content, such as textures and particle systems.
    /// </summary>
    protected override void LoadContent()
    {
        base.LoadContent();
    }

    /// <summary>
    /// Updates the game's logic, called once per frame.
    /// </summary>
    /// <param name="gameTime">
    /// Provides a snapshot of timing values used for game updates.
    /// </param>
    protected override void Update(GameTime gameTime)
    {
        // Exit the game if the Back button (GamePad) or Escape key (Keyboard) is pressed.
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed
            || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        if (Keyboard.GetState().IsKeyDown(Keys.F11))
        {
            // Toggle fullscreen mode when F11 is pressed.
            var graphicsManager = GetRequiredService<GraphicsManager>();
            graphicsManager.ToggleFullScreen();
        }

        // TODO: Add your update logic here

        base.Update(gameTime);
    }

    protected override void UnloadContent()
    {
        base.UnloadContent();
    }

    /// <summary>
    /// Draws the game's graphics, called once per frame.
    /// </summary>
    /// <param name="gameTime">
    /// Provides a snapshot of timing values used for rendering.
    /// </param>
    protected override void Draw(GameTime gameTime)
    {
        // Clears the screen with the MonoGame orange color before drawing.
        GraphicsDevice.Clear(Color.Black);

        base.Draw(gameTime);
    }

    public override string ToString() => $"{GameName} {VersionInfo} (v{Version} - {PlatformType})";
}
