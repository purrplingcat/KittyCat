using Friflo.Engine.ECS;
using Microsoft.Xna.Framework;

namespace KittyCat.Ecs.Components;

/// <summary>
/// World-owned state for KittyCat's 2D camera.
/// </summary>
public struct CameraState : IComponent
{
    public const string EntityName = "MainCamera";

    /// <summary>
    /// Camera position used when no entity has the <see cref="CameraTarget"/> tag.
    /// </summary>
    public Vector2 Position;
    public float Zoom;
    public float Rotation;

    /// <summary>
    /// Exponential follow rate. Zero or a negative value snaps to the desired position.
    /// </summary>
    public float FollowSpeed;
}

/// <summary>
/// Marks the camera currently used by the world renderer.
/// Only one camera should have this tag in a store.
/// </summary>
public struct ActiveCamera : ITag
{
}

/// <summary>
/// Marks an entity whose <see cref="ArcadeTransform"/> may be followed by a camera.
/// Only one target should have this tag in a store.
/// </summary>
public struct CameraTarget : ITag
{
}