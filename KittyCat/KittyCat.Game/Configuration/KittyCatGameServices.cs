using KittyCat.Ecs.Systems;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework.Content;
using PurrplingCore.Ecs;
using PurrplingCore.Ecs.DI;
using PurrplingCore.Ecs.Systems;
using PurrplingCore.Toolkit;
using PurrplingCore.Toolkit.Hosting;
using System;
using System.Reflection;

namespace KittyCat.Configuration;

public class KittyCatGameServices : IServiceModule
{
    public Assembly Assembly => GetType().Assembly;

    protected virtual ContentManager GetDefaultContentManager(IServiceProvider provider)
    {
        return provider.GetRequiredService<IContentManagerProvider>().ContentManager;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        // Register world and their systems
        static void ConfigureWorld(IWorldBlueprint world)
        {
            world.AddSystem<PaddleEffectSystem>();
            world.AddSystem<PlayerMovementSystem>();
            world.AddSystem<FallingStarSystem>();
            world.AddSystem<FallingStarSpawnerSystem>();
            world.AddSystem<StarCollectionSystem>();
            world.AddSystem<RestartGameSystem>();
            world.AddSystem<StarCatcherInitializeSystem>();
            world.AddSystem<StarCatcherDrawSystem>();
        }

        services.AddOptions<StarCatcherOptions>()
            .BindConfiguration(StarCatcherOptions.SectionName)
            .Validate(IsValid, "StarCatcher configuration contains invalid gameplay values.");

        services.AddWorld(ConfigureWorld);
    }

    private static bool IsValid(StarCatcherOptions options)
    {
        return options.Playfield.Width > 0
            && options.Playfield.Height > 0
            && options.Paddle.Width > 0
            && options.Paddle.Height > 0
            && options.Paddle.MoveSpeed > 0
            && options.Paddle.StartingLives > 0
            && options.Paddle.EffectDurationSeconds > 0
            && options.Paddle.ExpandMultiplier > 1
            && options.Paddle.ShrinkMultiplier is > 0 and < 1
            && options.Stars.Size > 0
            && options.Stars.InitialSpawnIntervalSeconds > 0
            && options.Stars.InitialMaxActive > 0
            && options.Stars.MaximumCreatedStars >= options.Stars.InitialMaxActive
            && options.Stars.SpecialSpawnChance is >= 0 and <= 1
            && options.Stars.GreenSpeedMultiplier > 0
            && options.Stars.RedSpeedMultiplier > 0
            && options.Stars.FastSpeedMultiplier >= 1
            && options.Difficulty.ScoreForMaximumDifficulty > 0
            && options.Difficulty.MaximumSpawnIntervalSeconds > 0
            && options.Difficulty.MaximumActiveStars >= options.Stars.InitialMaxActive
            && options.Difficulty.MaximumActiveStars <= options.Stars.MaximumCreatedStars
            && options.Difficulty.EasyMinimumFallSpeed > 0
            && options.Difficulty.EasyMaximumFallSpeed >= options.Difficulty.EasyMinimumFallSpeed
            && options.Difficulty.HardMinimumFallSpeed > 0
            && options.Difficulty.HardMaximumFallSpeed >= options.Difficulty.HardMinimumFallSpeed
            && options.Difficulty.EasyFastStarChance is >= 0 and <= 1
            && options.Difficulty.HardFastStarChance is >= 0 and <= 1;
    }
}
