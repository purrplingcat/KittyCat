using System;
using Friflo.Engine.ECS.Systems;
using KittyCat.Ecs.Components;

namespace KittyCat.Ecs.Systems;

/// <summary>
/// Expires paddle-size effects and blinks the player during temporary invincibility.
/// </summary>
public sealed class PaddleEffectSystem : QuerySystem<ArcadeTransform, PlayerState, ArcadeVisual>
{
    private const float BlinkIntervalSeconds = 0.042f;

    protected override void OnUpdate()
    {
        foreach (var (transforms, players, visuals, entities) in Query.Chunks)
        {
            for (var index = 0; index < entities.Length; index++)
            {
                ref var transform = ref transforms[index];
                ref var player = ref players[index];
                ref var visual = ref visuals[index];

                if (player.InvincibilityTime > 0)
                {
                    player.InvincibilityTime = Math.Max(0, player.InvincibilityTime - Tick.deltaTime);
                    if (player.InvincibilityTime > 0)
                    {
                        // Carry fractional time so blinking stays stable across different frame rates.
                        player.InvincibilityBlinkTime += Tick.deltaTime;
                        var blinkCount = (int)(player.InvincibilityBlinkTime / BlinkIntervalSeconds);
                        if (blinkCount > 0)
                        {
                            player.InvincibilityBlinkTime -= blinkCount * BlinkIntervalSeconds;
                            // Skipped intervals only change visibility when their count is odd.
                            if ((blinkCount & 1) != 0) visual.IsVisible = !visual.IsVisible;
                        }
                    }
                    else
                    {
                        player.InvincibilityBlinkTime = 0;
                        // Always restore visibility when immunity ends, including after an odd blink count.
                        visual.IsVisible = true;
                    }
                }
                else if (player.InvincibilityBlinkTime > 0)
                {
                    player.InvincibilityBlinkTime = 0;
                    visual.IsVisible = true;
                }

                if (player.PaddleEffectTime <= 0) continue;

                player.PaddleEffectTime = Math.Max(0, player.PaddleEffectTime - Tick.deltaTime);
                if (player.PaddleEffectTime > 0) continue;

                player.PaddleEffect = PaddleSizeEffect.None;
                transform.Size.X = player.BasePaddleWidth;
            }
        }
    }
}