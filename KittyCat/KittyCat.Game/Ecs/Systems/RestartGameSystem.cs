using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using KittyCat.Configuration;
using KittyCat.Ecs.Components;
using Microsoft.Extensions.Options;
using Microsoft.Xna.Framework.Input;
using PurrplingCore.Ecs;
using PurrplingCore.Ecs.Components;
using PurrplingCore.Ecs.Particles;

namespace KittyCat.Ecs.Systems;

/// <summary>
/// Restores the player and clears active bonuses when restarting after game over.
/// </summary>
public sealed partial class RestartGameSystem : QuerySystem<ArcadeTransform, PlayerState, ArcadeVisual>
{
    private readonly StarCatcherOptions _options;

    public RestartGameSystem(IOptions<StarCatcherOptions> options)
    {
        _options = options.Value;
    }

    protected override void OnUpdate()
    {
        // A held Space key cannot reset an active run; only game-over players are restored.
        if (!Keyboard.GetState().IsKeyDown(Keys.Space)) return;

        var restarted = false;
        foreach (var (transforms, players, visuals, entities) in Query.Chunks)
        {
            for (var index = 0; index < entities.Length; index++)
            {
                ref var transform = ref transforms[index];
                ref var player = ref players[index];
                if (player.Lives > 0) continue;

                PlayerSetup.Reset(ref transform, ref player, _options);
                visuals[index].IsVisible = true;
                restarted = true;
            }
        }

        if (!restarted) return;
        ResetParticlesQuery(Query.Store);
        // Reset collected bonuses only after restoring the player, and defer star deletion.
        ResetStarsQuery(Query.Store);
        ResetHeartsQuery(Query.Store);
    }

    [Query]
    private static void ResetParticles(ref Particle particle)
    {
        particle.State.Age = particle.State.Lifetime;
    }

    [Query]
    private void ResetStars(ref ArcadeTransform transform, ref FallingStar star, ref ArcadeVisual visual, Entity entity)
    {
        transform.Position = new Microsoft.Xna.Framework.Vector2(0, -transform.Size.Y);
        star.IsActive = false;
        star.PaddleEffect = PaddleSizeEffect.None;
        visual.IsVisible = false;
        // Let the periodic cleanup module delete these after the current query pass.
        CommandBuffer.AddTag<PendingDestroy>(entity.Id);
        CommandBuffer.AddTag<Disabled>(entity.Id);
    }

    [Query]
    private static void ResetHearts(ref BonusHeart heart, ref ArcadeVisual visual)
    {
        heart.State = BonusHeartState.Inactive;
        visual.IsVisible = false;
    }
}
