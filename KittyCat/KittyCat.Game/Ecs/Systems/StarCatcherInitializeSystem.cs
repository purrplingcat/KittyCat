using Friflo.Engine.ECS;
using KittyCat.Configuration;
using KittyCat.Ecs.Components;
using Microsoft.Extensions.Options;
using PurrplingCore.Ecs.Extensions;
using PurrplingCore.Ecs.Attributes;
using PurrplingCore.Ecs.Systems;

namespace KittyCat.Ecs.Systems;

/// <summary>
/// Creates the per-store spawn state and initial player from the configured settings.
/// </summary>
[System<InitializeSystemGroup>]
public sealed class StarCatcherInitializeSystem : StoreSystem
{
    private readonly StarCatcherOptions _options;

    public StarCatcherInitializeSystem(IOptions<StarCatcherOptions> options)
    {
        _options = options.Value;
    }

    protected override void OnUpdate()
    {
        Store.SetSingleton(StarSpawnState.EntityName, new StarSpawnState());
        var playerTransform = PlayerSetup.CreateTransform(_options);
        var player = Store.CreateEntity(
            playerTransform,
            PlayerSetup.CreateState(_options.Paddle),
            new ArcadeVisual { Kind = ArcadeVisualKind.Player, IsVisible = true });
        player.AddTag<CameraTarget>();

        var camera = Store.CreateEntity(
            new UniqueEntity(CameraState.EntityName),
            new CameraState
            {
                Position = playerTransform.Position + playerTransform.Size * 0.5f,
                Zoom = 1f,
                FollowSpeed = 10f,
            });
        camera.AddTag<ActiveCamera>();
    }
}