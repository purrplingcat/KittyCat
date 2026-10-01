using Friflo.Engine.ECS;
using Microsoft.Xna.Framework;

namespace KittyCat.Ecs.Components;

/// <summary>Shared position and size for the player and visible game objects.</summary>
public struct ArcadeTransform : IComponent
{
    public Vector2 Position;
    public Vector2 Size;

    /// <summary>Returns whether this transform overlaps another axis-aligned transform.</summary>
    public readonly bool Intersects(in ArcadeTransform other)
    {
        return Position.X < other.Position.X + other.Size.X
            && Position.X + Size.X > other.Position.X
            && Position.Y < other.Position.Y + other.Size.Y
            && Position.Y + Size.Y > other.Position.Y;
    }
}

/// <summary>
/// Persistent player values plus timers for temporary paddle and invincibility states.
/// </summary>
public struct PlayerState : IComponent
{
    /// <summary>Unmodified movement speed before hard-stage and strike multipliers.</summary>
    public float MoveSpeed;
    public float BasePaddleWidth;
    public float PaddleEffectTime;
    public float InvincibilityTime;
    public float InvincibilityBlinkTime;
    public PaddleSizeEffect PaddleEffect;
    public int Score;
    /// <summary>Consecutive stars collected; a missed star resets this value.</summary>
    public int Strike;
    public int Lives;
}

public enum PaddleSizeEffect : byte
{
    None,
    Expand,
    Shrink,
}

/// <summary>Runtime movement and collection state for a spawned star.</summary>
public struct FallingStar : IComponent
{
    public float FallSpeed;
    public bool IsActive;
    public PaddleSizeEffect PaddleEffect;
}

/// <summary>Per-world timing and wave selection for the star spawner.</summary>
public struct StarSpawnState : IComponent
{
    public const string EntityName = "StarSpawner";

    public float TimeUntilSpawn;
    public int WaveTarget;
    public bool WaveWasActive;
}

public enum BonusHeartState : byte
{
    Inactive,
    Flying,
    Popping,
}

/// <summary>Velocity and lifetime state for the bouncing life bonus.</summary>
public struct BonusHeart : IComponent
{
    public Vector2 Velocity;
    public float RemainingSeconds;
    public float PopRemainingSeconds;
    public BonusHeartState State;
}

public enum ArcadeVisualKind : byte
{
    Player,
    Star,
    GreenStar,
    RedStar,
    Heart,
    HeartPop,
}

public struct ArcadeVisual : IComponent
{
    public ArcadeVisualKind Kind;
    public float Opacity;
    public bool IsVisible;
}
