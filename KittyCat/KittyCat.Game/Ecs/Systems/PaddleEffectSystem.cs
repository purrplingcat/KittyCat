using System;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using KittyCat.Ecs.Components;

namespace KittyCat.Ecs.Systems;

public sealed class PaddleEffectSystem : QuerySystem<ArcadeTransform, PlayerState>
{
    protected override void OnUpdate()
    {
        foreach (var (transforms, players, entities) in Query.Chunks)
        {
            for (var index = 0; index < entities.Length; index++)
            {
                ref var transform = ref transforms[index];
                ref var player = ref players[index];
                if (player.PaddleEffectTime <= 0) continue;

                player.PaddleEffectTime = Math.Max(0, player.PaddleEffectTime - Tick.deltaTime);
                if (player.PaddleEffectTime > 0) continue;

                player.PaddleEffect = PaddleSizeEffect.None;
                transform.Size.X = player.BasePaddleWidth;
            }
        }
    }
}