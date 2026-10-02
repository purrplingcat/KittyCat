using System;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using KittyCat.Configuration;
using KittyCat.Ecs.Components;
using PurrplingCore.Ecs;
using PurrplingCore.Ecs.Attributes;
using PurrplingCore.Ecs.Systems;
using PurrplingCore.Toolkit.Rendering;
using Microsoft.Extensions.Options;
using Microsoft.Xna.Framework;

namespace KittyCat.Ecs.Systems;

/// <summary>
/// Clamps the ECS camera state after gameplay movement and restart updates.
/// </summary>
[System<UpdateSystemGroup>(Order = SystemOrder.Last)]
public sealed partial class CameraStateRenderSyncSystem : QuerySystem
{
    private readonly Camera2D _camera;
    private readonly Rectangle _playfieldBounds;
    private Vector2 _target;

    public CameraStateRenderSyncSystem(Camera2D camera, IOptions<StarCatcherOptions> options)
    {
        _camera = camera;
        var playfield = options.Value.Playfield;
        _playfieldBounds = new Rectangle(0, 0, playfield.Width, playfield.Height);
    }

    protected override void OnUpdate()
    {
        ApplyActiveCameraQuery(Query.Store);
    }

    [Query]
    [AllTags<ActiveCamera>]
    private void ApplyActiveCamera(ref CameraState state)
    {
        _target = state.Position;
        ReadCameraTargetQuery(Query.Store, ref _target);

        var deltaTime = MathF.Max(0f, Tick.deltaTime);
        var amount = state.FollowSpeed <= 0f
            ? 1f
            : 1f - MathF.Exp(-state.FollowSpeed * deltaTime);
        _camera.Position = Vector2.Lerp(_camera.Position, _target, amount);
        _camera.Zoom = state.Zoom;
        _camera.Rotation = state.Rotation;
        _camera.ClampPosition(_playfieldBounds);
    }

    [Query]
    [AllTags<CameraTarget>]
    private static void ReadCameraTarget(ref ArcadeTransform transform, ref Vector2 target)
    {
        target = transform.Position + transform.Size * 0.5f;
    }
}