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

    protected override void OnUpdate(EntityStore store)
    {
        store.SetSingleton(StarSpawnState.EntityName, new StarSpawnState());
        store.CreateEntity(
            PlayerSetup.CreateTransform(_options),
            PlayerSetup.CreateState(_options.Paddle),
            new ArcadeVisual { Kind = ArcadeVisualKind.Player, IsVisible = true });
    }
}