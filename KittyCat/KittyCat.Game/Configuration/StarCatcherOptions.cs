using System;

namespace KittyCat.Configuration;

/// <summary>Configuration root for the Star Catcher example game.</summary>
public sealed class StarCatcherOptions
{
    public const string SectionName = "StarCatcher";

    public PlayfieldOptions Playfield { get; set; } = new();
    public PaddleOptions Paddle { get; set; } = new();
    public StarOptions Stars { get; set; } = new();
    public HeartOptions Hearts { get; set; } = new();
    public DifficultyOptions Difficulty { get; set; } = new();
}

public sealed class PlayfieldOptions
{
    public int Width { get; set; } = 1280;
    public int Height { get; set; } = 720;
}

/// <summary>Paddle dimensions, movement limits, lives, and temporary effects.</summary>
public sealed class PaddleOptions
{
    public const int MaximumLives = 5;
    public const float InvincibilityDurationSeconds = 2f;

    /// <summary>Consecutive stars needed to fill the strike meter.</summary>
    public int StrikesForMaximumSpeed { get; set; } = 30;
    public float Width { get; set; } = 64;
    public float Height { get; set; } = 28;
    public float MoveSpeed { get; set; } = 600;
    public int StartingLives { get; set; } = 3;
    /// <summary>Maximum strike multiplier applied on top of the current difficulty-adjusted base speed.</summary>
    public float MaximumStrikeSpeedMultiplier { get; set; } = 1.6f;
    /// <summary>Base paddle-speed multiplier reached as hard-stage progress approaches its cap.</summary>
    public float HardSpeedMultiplier { get; set; } = 1.1f;
    public float EffectDurationSeconds { get; set; } = 8;
    public float ExpandMultiplier { get; set; } = 1.7f;
    public float ShrinkMultiplier { get; set; } = 0.6f;
}

public sealed class StarOptions
{
    public float Size { get; set; } = 24;
    public float InitialSpawnIntervalSeconds { get; set; } = 1.2f;
    /// <summary>Number of stars active when a new run begins.</summary>
    public int InitialMaxActive { get; set; } = 1;
    public float SpecialSpawnChance { get; set; } = 0.16f;
    public float GreenSpeedMultiplier { get; set; } = 1.25f;
    public float RedSpeedMultiplier { get; set; } = 0.75f;
    public float FastSpeedMultiplier { get; set; } = 1.8f;
}

public sealed class HeartOptions
{
    public float Size { get; set; } = 28;
    public float SpawnIntervalSeconds { get; set; } = 18;
    public float Speed { get; set; } = 460;
    public float LifetimeSeconds { get; set; } = 6;
    public float PopDurationSeconds { get; set; } = 0.25f;
}

/// <summary>Score thresholds and tuning values for the easy, medium, and hard stages.</summary>
public sealed class DifficultyOptions
{
    /// <summary>Scores below this value remain easy; medium starts at this boundary.</summary>
    public int EasyScoreLimit { get; set; } = 40;
    /// <summary>Hard starts at this score.</summary>
    public int HardScoreLimit { get; set; } = 100;
    /// <summary>Score at which hard-stage speed and spawn pressure reach their configured maximum.</summary>
    public int ScoreForMaximumDifficulty { get; set; } = 180;
    /// <summary>Largest base wave size during easy; the easy bonus roll may add one more.</summary>
    public int EasyMaximumActiveStars { get; set; } = 5;
    /// <summary>Global active-star cap used by medium and hard waves.</summary>
    public int MaximumActiveStars { get; set; } = 5;
    /// <summary>Independent chance to add one star to an easy wave.</summary>
    public float EasyBonusStarChance { get; set; } = 0.1f;
    public float EasySpawnIntervalSeconds { get; set; } = 0.8f;
    public float HardSpawnIntervalSeconds { get; set; } = 0.65f;
    public float EasyMinimumFallSpeed { get; set; } = 90;
    public float EasyMaximumFallSpeed { get; set; } = 145;
    public float MediumMinimumFallSpeed { get; set; } = 100;
    public float MediumMaximumFallSpeed { get; set; } = 155;
    public float HardMinimumFallSpeed { get; set; } = 120;
    public float HardMaximumFallSpeed { get; set; } = 185;
    public float EasyFastStarChance { get; set; } = 0.04f;
    public float MediumFastStarChance { get; set; } = 0.08f;
    public float HardFastStarChance { get; set; } = 0.14f;

    /// <summary>Maps a score to its difficulty stage and normalized progress within that stage.</summary>
    public DifficultyProgress GetProgress(int score)
    {
        return GetProgress(score, EasyScoreLimit, HardScoreLimit, ScoreForMaximumDifficulty);
    }

    public static DifficultyProgress GetProgress(
        int score,
        int easyScoreLimit,
        int hardScoreLimit,
        int maximumDifficultyScore)
    {
        if (score < easyScoreLimit)
        {
            return new DifficultyProgress(DifficultyStage.Easy, Math.Clamp((float)score / easyScoreLimit, 0, 1));
        }

        if (score < hardScoreLimit)
        {
            return new DifficultyProgress(
                DifficultyStage.Medium,
                Math.Clamp((float)(score - easyScoreLimit) / (hardScoreLimit - easyScoreLimit), 0, 1));
        }

        return new DifficultyProgress(
            DifficultyStage.Hard,
            Math.Clamp((float)(score - hardScoreLimit) / (maximumDifficultyScore - hardScoreLimit), 0, 1));
    }
}

/// <summary>The current score-based gameplay phase.</summary>
public enum DifficultyStage : byte
{
    Easy,
    Medium,
    Hard,
}

/// <summary>Difficulty phase and normalized progress within that phase.</summary>
public readonly record struct DifficultyProgress(DifficultyStage Stage, float Progress);