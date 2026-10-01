using Friflo.Engine.ECS.Systems;
using KittyCat.Ecs.Components;
using KittyCat.Services;
using PurrplingCore.Ecs.Attributes;
using PurrplingCore.Ecs.Systems;

namespace KittyCat.Ecs.Systems;

/// <summary>
/// Queues the player's HUD after world entities have been submitted.
/// </summary>
[System<DrawSystemGroup>]
[PurrplingCore.Ecs.RunAfter<StarCatcherDrawSystem>]
public sealed class StarCatcherHudSystem(StarCatcherRenderer renderer) : QuerySystem<PlayerState>
{
    protected override void OnUpdate()
    {
        foreach (var (players, entities) in Query.Chunks)
        {
            for (var index = 0; index < entities.Length; index++)
            {
                ref var player = ref players[index];
                renderer.DrawHud(player.Score, player.Lives, player.Strike);
            }
        }
    }
}