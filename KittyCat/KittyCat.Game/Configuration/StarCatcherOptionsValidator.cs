using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;

namespace KittyCat.Configuration;

/// <summary>
/// Validates Star Catcher gameplay settings and reports invalid sections by name.
/// </summary>
public sealed class StarCatcherOptionsValidator : IValidateOptions<StarCatcherOptions>
{
    public ValidateOptionsResult Validate(string? name, StarCatcherOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        List<string> failures = [];
        var playfield = options.Playfield;
        var paddle = options.Paddle;
        var stars = options.Stars;
        var hearts = options.Hearts;
        var difficulty = options.Difficulty;

        if (playfield.Width <= 0 || playfield.Height <= 0)
        {
            failures.Add("Playfield width and height must be positive.");
        }

        if (paddle.Width <= 0
            || paddle.Height <= 0
            || paddle.MoveSpeed <= 0
            || paddle.StrikesForMaximumSpeed <= 0
            || paddle.MaximumStrikeSpeedMultiplier < 1
            || paddle.HardSpeedMultiplier < 1
            || paddle.StartingLives is < 1 or > PaddleOptions.MaximumLives
            || paddle.EffectDurationSeconds <= 0
            || paddle.ExpandMultiplier <= 1
            || paddle.ShrinkMultiplier is <= 0 or >= 1)
        {
            failures.Add("Paddle dimensions, movement, lives, and effect values are invalid.");
        }

        if (stars.Size <= 0
            || stars.InitialSpawnIntervalSeconds <= 0
            || stars.InitialMaxActive <= 0
            || stars.SpecialSpawnChance is < 0 or > 1
            || stars.GreenSpeedMultiplier <= 0
            || stars.RedSpeedMultiplier <= 0
            || stars.FastSpeedMultiplier < 1)
        {
            failures.Add("Star size, spawn, chance, and movement values are invalid.");
        }

        if (hearts.Size <= 0
            || hearts.Size >= playfield.Width
            || hearts.Size >= playfield.Height
            || hearts.SpawnIntervalSeconds <= 0
            || hearts.Speed <= 0
            || hearts.LifetimeSeconds <= 0
            || hearts.PopDurationSeconds <= 0)
        {
            failures.Add("Heart size, spawn, movement, and lifetime values are invalid.");
        }

        if (difficulty.EasyScoreLimit <= 0
            || difficulty.HardScoreLimit <= difficulty.EasyScoreLimit
            || difficulty.ScoreForMaximumDifficulty <= difficulty.HardScoreLimit
            || difficulty.EasyMaximumActiveStars < stars.InitialMaxActive
            || difficulty.MaximumActiveStars < difficulty.EasyMaximumActiveStars
            || difficulty.EasyBonusStarChance is < 0 or > 1
            || difficulty.EasySpawnIntervalSeconds <= 0
            || difficulty.EasySpawnIntervalSeconds > stars.InitialSpawnIntervalSeconds
            || difficulty.HardSpawnIntervalSeconds <= 0
            || difficulty.HardSpawnIntervalSeconds > difficulty.EasySpawnIntervalSeconds
            || difficulty.EasyMinimumFallSpeed <= 0
            || difficulty.EasyMaximumFallSpeed < difficulty.EasyMinimumFallSpeed
            || difficulty.MediumMinimumFallSpeed < difficulty.EasyMinimumFallSpeed
            || difficulty.MediumMaximumFallSpeed < difficulty.MediumMinimumFallSpeed
            || difficulty.HardMinimumFallSpeed < difficulty.MediumMinimumFallSpeed
            || difficulty.HardMaximumFallSpeed < difficulty.HardMinimumFallSpeed
            || difficulty.EasyFastStarChance is < 0 or > 1
            || difficulty.MediumFastStarChance is < 0 or > 1
            || difficulty.HardFastStarChance is < 0 or > 1)
        {
            failures.Add("Difficulty thresholds, wave sizes, spawn intervals, speeds, or chances are invalid.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
