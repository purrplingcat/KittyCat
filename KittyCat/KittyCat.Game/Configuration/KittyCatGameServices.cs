using KittyCat.Ecs.Systems;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework.Content;
using PurrplingCore.Ecs;
using PurrplingCore.Ecs.DI;
using PurrplingCore.Ecs.Modules;
using PurrplingCore.Ecs.Systems;
using PurrplingCore.Scenes;
using PurrplingCore.Toolkit;
using PurrplingCore.Toolkit.Hosting;
using System;
using System.Reflection;

namespace KittyCat.Configuration;

/// <summary>Composes game-specific world systems and validates Star Catcher settings.</summary>
public class KittyCatGameServices : IServiceModule
{
    public Assembly Assembly => GetType().Assembly;

    protected virtual ContentManager GetDefaultContentManager(IServiceProvider provider)
    {
        return provider.GetRequiredService<IContentManagerProvider>().ContentManager;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddWorldController();
        services.AddSceneManager();

        // Cleanup is opt-in; the game explicitly selects it for its default world.
        services // Register the game world and its systems
            .AddWorld(ConfigureWorld)
            .AddModule<EntityCleanupModule>();

        services // Register the StarCatcher options
            .AddOptions<StarCatcherOptions>()
            .BindConfiguration(StarCatcherOptions.SectionName)
            .Validate(IsValid, "StarCatcher configuration contains invalid gameplay values.");
    }

    private static void ConfigureWorld(IWorldBlueprint world)
    {
        // Register the example game's systems in their update, initialize, and draw groups.
        world.AddSystem<PaddleEffectSystem>();
        world.AddSystem<PlayerMovementSystem>();
        world.AddSystem<FallingStarSystem>();
        world.AddSystem<FallingStarSpawnerSystem>();
        world.AddSystem<StarCollectionSystem>();
        world.AddSystem<HeartSystem>();
        world.AddSystem<RestartGameSystem>();
        world.AddSystem<StarCatcherDebugInfoSystem>();
        world.AddSystem<StarCatcherInitializeSystem>();
        world.AddSystem<StarCatcherDrawSystem>();
    }

    private static bool IsValid(StarCatcherOptions options)
    {
        return options.Playfield.Width > 0
            && options.Playfield.Height > 0
            && options.Paddle.Width > 0
            && options.Paddle.Height > 0
            && options.Paddle.MoveSpeed > 0
            && options.Paddle.StrikesForMaximumSpeed > 0
            && options.Paddle.MaximumStrikeSpeedMultiplier >= 1
            && options.Paddle.HardSpeedMultiplier >= 1
            && options.Paddle.StartingLives is > 0 and <= PaddleOptions.MaximumLives
            && options.Paddle.EffectDurationSeconds > 0
            && options.Paddle.ExpandMultiplier > 1
            && options.Paddle.ShrinkMultiplier is > 0 and < 1
            && options.Stars.Size > 0
            && options.Stars.InitialSpawnIntervalSeconds > 0
            && options.Stars.InitialMaxActive > 0
            && options.Stars.InitialMaxActive <= options.Difficulty.EasyMaximumActiveStars
            && options.Stars.SpecialSpawnChance is >= 0 and <= 1
            && options.Stars.GreenSpeedMultiplier > 0
            && options.Stars.RedSpeedMultiplier > 0
            && options.Stars.FastSpeedMultiplier >= 1
            && options.Hearts.Size > 0
            && options.Hearts.Size < options.Playfield.Width
            && options.Hearts.Size < options.Playfield.Height
            && options.Hearts.SpawnIntervalSeconds > 0
            && options.Hearts.Speed > 0
            && options.Hearts.LifetimeSeconds > 0
            && options.Hearts.PopDurationSeconds > 0
            && options.Difficulty.EasyScoreLimit > 0
            && options.Difficulty.HardScoreLimit > options.Difficulty.EasyScoreLimit
            && options.Difficulty.ScoreForMaximumDifficulty > options.Difficulty.HardScoreLimit
            && options.Difficulty.EasyMaximumActiveStars >= options.Stars.InitialMaxActive
            && options.Difficulty.MaximumActiveStars >= options.Difficulty.EasyMaximumActiveStars
            && options.Difficulty.EasyBonusStarChance is >= 0 and <= 1
            && options.Difficulty.EasySpawnIntervalSeconds > 0
            && options.Difficulty.EasySpawnIntervalSeconds <= options.Stars.InitialSpawnIntervalSeconds
            && options.Difficulty.HardSpawnIntervalSeconds > 0
            && options.Difficulty.HardSpawnIntervalSeconds <= options.Difficulty.EasySpawnIntervalSeconds
            && options.Difficulty.EasyMinimumFallSpeed > 0
            && options.Difficulty.EasyMaximumFallSpeed >= options.Difficulty.EasyMinimumFallSpeed
            && options.Difficulty.MediumMinimumFallSpeed >= options.Difficulty.EasyMinimumFallSpeed
            && options.Difficulty.MediumMaximumFallSpeed >= options.Difficulty.MediumMinimumFallSpeed
            && options.Difficulty.HardMinimumFallSpeed >= options.Difficulty.MediumMinimumFallSpeed
            && options.Difficulty.HardMaximumFallSpeed >= options.Difficulty.HardMinimumFallSpeed
            && options.Difficulty.EasyFastStarChance is >= 0 and <= 1
            && options.Difficulty.MediumFastStarChance is >= 0 and <= 1
            && options.Difficulty.HardFastStarChance is >= 0 and <= 1;
    }
}
