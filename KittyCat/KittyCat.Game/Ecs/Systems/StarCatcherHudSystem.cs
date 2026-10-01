using Friflo.Engine.ECS.Systems;
using KittyCat.Ecs.Components;
using KittyCat.Services;
using PurrplingCore.Ecs.Attributes;
using PurrplingCore.Ecs.Systems;

namespace KittyCat.Ecs.Systems;

/// <summary>
/// Draws the player's HUD after world entities in the same SpriteBatch frame.
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