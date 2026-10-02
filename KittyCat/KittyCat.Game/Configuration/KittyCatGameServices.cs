using KittyCat.Ecs;
using KittyCat.Ecs.Systems;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Extensions.Options;
using PurrplingCore.Ecs;
using PurrplingCore.Ecs.DI;
using PurrplingCore.Ecs.Modules;
using PurrplingCore.Rendering;
using PurrplingCore.Scenes;
using PurrplingCore.Toolkit;
using PurrplingCore.Toolkit.Hosting;
using PurrplingCore.Toolkit.Rendering;
using System;
using System.Reflection;
using PurrplingCore.Toolkit.Graphics;

namespace KittyCat.Configuration;

/// <summary>
/// Composes game-specific world systems and validates Star Catcher settings.
/// </summary>
public class KittyCatGameServices : IServiceModule
{
    public Assembly Assembly => GetType().Assembly;

    /// <summary>
    /// Resolves the default content manager used by game scenes.
    /// </summary>
    protected virtual ContentManager GetDefaultContentManager(IServiceProvider provider)
    {
        return provider.GetRequiredService<IContentManagerProvider>().ContentManager;
    }

    /// <summary>
    /// Registers the game's settings, world, renderers, and draw-loop components.
    /// </summary>
    public void ConfigureServices(IServiceCollection services)
    {
        // Register shared services consumed by world and renderer modules here.
        services.AddSingleton<Camera2D>();

        // Bind and validate game-wide options before registering systems that consume them.
        services
            .AddOptions<StarCatcherOptions>()
            .BindConfiguration(StarCatcherOptions.SectionName)
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<StarCatcherOptions>, StarCatcherOptionsValidator>();

        // Declare the world's systems and world-specific modules here.
        services
            .AddWorld(ConfigureWorld)
            .AddModule<EntityCleanupModule>();

        // Declare renderer keys, layers, and renderer-specific configuration modules here.
        services
            .AddRenderer(KittyCatRendererKey.World)
            .AddModule<WorldRendererConfiguration>()
            .AddDrawComponent(DrawStage.World);
        services
            .AddRenderer(KittyCatRendererKey.Screen, ConfigureScreenRenderer)
            .AddDrawComponent(DrawStage.Screen);

        // Register game components and flush each renderer at its own draw stage.
        services.AddWorldController();
        services.AddSceneManager();
    }

    /// <summary>
    /// Adds gameplay and drawing systems to the default world.
    /// </summary>
    private static void ConfigureWorld(IWorldBlueprint world)
    {
        world.AddSystem<PaddleEffectSystem>();
        world.AddSystem<PlayerMovementSystem>();
        world.AddSystem<FallingStarSystem>();
        world.AddSystem<FallingStarSpawnerSystem>();
        world.AddSystem<StarCollectionSystem>();
        world.AddSystem<HeartSystem>();
        world.AddSystem<RestartGameSystem>();
        world.AddSystem<CameraStateRenderSyncSystem>();
        world.AddSystem<StarCatcherDebugInfoSystem>();
        world.AddSystem<StarCatcherInitializeSystem>();
        world.AddSystem<StarCatcherDrawSystem>();
        world.AddSystem<StarCatcherHudSystem>();
    }

    /// <summary>
    /// Declares the screen-space layers used by the HUD and modal overlay.
    /// </summary>
    private static void ConfigureScreenRenderer(RendererOptions options)
    {
        options.Output = RenderTargetSpec.MatchScreen();
        options.ScaleMode = RenderScaleMode.Native;
        options.AddLayer(KittyCatLayers.Hud);
        options.AddLayer(KittyCatLayers.Modal, new RenderLayerOptions { Order = 100 });
    }
}

public sealed class WorldRendererConfiguration(Camera2D camera) : IRendererConfiguration
{
    public void Configure(RendererOptions options)
    {
        //options.Alignment = RenderAlignment.TopLeft;
        options.Diagnostics = new RendererDiagnosticsOptions();
        options.ClearColor = new Color(14, 20, 31);
        options.Output = RenderTargetSpec.MatchScreen();
        options.ScaleMode = RenderScaleMode.Native;
        //options.Screen = options.Screen.CreateRegion(-0.25f, 0f, 1f, 1f);
        options.AddLayer(KittyCatLayers.Entities, new RenderLayerOptions { Camera = camera, SyncCameraViewport = true, CullingEnabled = true, });
        options.AddLayer(KittyCatLayers.Effects, new RenderLayerOptions { Order = 100, Camera = camera, SyncCameraViewport = true });
    }
}
