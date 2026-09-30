using Friflo.Engine.ECS;
using Microsoft.Xna.Framework;

namespace KittyCat.Ecs.Components;

public struct ArcadeTransform : IComponent
{
    public Vector2 Position;
    public Vector2 Size;
}

public struct PlayerState : IComponent
{
    public float MoveSpeed;
    public float BasePaddleWidth;
    public float PaddleEffectTime;
    public PaddleSizeEffect PaddleEffect;
    public int Score;
    public int Lives;
}

public enum PaddleSizeEffect : byte
{
    None,
    Expand,
    Shrink,
}

public struct FallingStar : IComponent
{
    public float FallSpeed;
    public bool IsActive;
    public PaddleSizeEffect PaddleEffect;
}

public enum ArcadeVisualKind : byte
{
    Player,
    Star,
    GreenStar,
    RedStar,
}

public struct ArcadeVisual : IComponent
{
    public ArcadeVisualKind Kind;
    public bool IsVisible;
}