using System;
using System.Collections.Generic;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using KittyCat.Configuration;
using KittyCat.Ecs.Components;
using Microsoft.Extensions.Options;
using Microsoft.Xna.Framework.Input;
using PurrplingCore.Ecs;
using PurrplingCore.Ecs.Components;

namespace KittyCat.Ecs.Systems;

/// <summary>Restores the player and clears active bonuses when restarting after game over.</summary>
public sealed class RestartGameSystem : QuerySystem<ArcadeTransform, PlayerState, ArcadeVisual>
{
    private readonly Dictionary<EntityStore, ArchetypeQuery<ArcadeTransform, FallingStar, ArcadeVisual>> _starQueries = [];
    private readonly Dictionary<EntityStore, ArchetypeQuery<BonusHeart, ArcadeVisual>> _heartQueries = [];
    private readonly StarCatcherOptions _options;

    public RestartGameSystem(IOptions<StarCatcherOptions> options)
    {
        _options = options.Value;
    }

    protected override void OnAddStore(EntityStore store)
    {
        _starQueries.Add(store, store.Query<ArcadeTransform, FallingStar, ArcadeVisual>());
        _heartQueries.Add(store, store.Query<BonusHeart, ArcadeVisual>());
        base.OnAddStore(store);
    }

    protected override void OnRemoveStore(EntityStore store)
    {
        _starQueries.Remove(store);
        _heartQueries.Remove(store);
        base.OnRemoveStore(store);
    }

    protected override void OnUpdate()
    {
        // Space only restarts a game-over world; holding it during play has no effect.
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

        foreach (var (transforms, stars, visuals, entities) in _starQueries[Query.Store].Chunks)
        {
            for (var index = 0; index < entities.Length; index++)
            {
                ref var transform = ref transforms[index];
                transform.Position = new Microsoft.Xna.Framework.Vector2(0, -transform.Size.Y);
                stars[index].IsActive = false;
                stars[index].PaddleEffect = PaddleSizeEffect.None;
                visuals[index].IsVisible = false;
                var entityId = entities.EntityAt(index).Id;
                // Let the periodic cleanup module delete these after the current query pass.
                CommandBuffer.AddTag<PendingDestroy>(entityId);
                CommandBuffer.AddTag<Disabled>(entityId);
            }
        }

        foreach (var (hearts, visuals, entities) in _heartQueries[Query.Store].Chunks)
        {
            for (var index = 0; index < entities.Length; index++)
            {
                hearts[index].State = BonusHeartState.Inactive;
                visuals[index].IsVisible = false;
            }
        }
    }
}
