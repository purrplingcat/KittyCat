namespace KittyCat.Configuration;

public sealed class StarCatcherOptions
{
    public const string SectionName = "StarCatcher";

    public PlayfieldOptions Playfield { get; set; } = new();
    public PaddleOptions Paddle { get; set; } = new();
    public StarOptions Stars { get; set; } = new();
    public DifficultyOptions Difficulty { get; set; } = new();
}

public sealed class PlayfieldOptions
{
    public int Width { get; set; } = 1280;
    public int Height { get; set; } = 720;
}

public sealed class PaddleOptions
{
    public float Width { get; set; } = 64;
    public float Height { get; set; } = 28;
    public float MoveSpeed { get; set; } = 600;
    public int StartingLives { get; set; } = 3;
    public float EffectDurationSeconds { get; set; } = 8;
    public float ExpandMultiplier { get; set; } = 1.7f;
    public float ShrinkMultiplier { get; set; } = 0.6f;
}

public sealed class StarOptions
{
    public float Size { get; set; } = 24;
    public float InitialSpawnIntervalSeconds { get; set; } = 2;
    public int InitialMaxActive { get; set; } = 2;
    public int MaximumCreatedStars { get; set; } = 7;
    public float SpecialSpawnChance { get; set; } = 0.16f;
    public float GreenSpeedMultiplier { get; set; } = 1.25f;
    public float RedSpeedMultiplier { get; set; } = 0.75f;
    public float FastSpeedMultiplier { get; set; } = 1.8f;
}

public sealed class DifficultyOptions
{
    public int ScoreForMaximumDifficulty { get; set; } = 30;
    public float MaximumSpawnIntervalSeconds { get; set; } = 0.65f;
    public int MaximumActiveStars { get; set; } = 7;
    public float EasyMinimumFallSpeed { get; set; } = 90;
    public float EasyMaximumFallSpeed { get; set; } = 145;
    public float HardMinimumFallSpeed { get; set; } = 170;
    public float HardMaximumFallSpeed { get; set; } = 275;
    public float EasyFastStarChance { get; set; } = 0.04f;
    public float HardFastStarChance { get; set; } = 0.20f;
}