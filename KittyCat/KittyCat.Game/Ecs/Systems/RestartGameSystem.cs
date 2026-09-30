using System;
using System.Collections.Generic;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using KittyCat.Configuration;
using KittyCat.Ecs.Components;
using Microsoft.Extensions.Options;
using Microsoft.Xna.Framework.Input;
using PurrplingCore.Ecs;

namespace KittyCat.Ecs.Systems;

public sealed class RestartGameSystem : QuerySystem<ArcadeTransform, PlayerState>
{
    private readonly Dictionary<EntityStore, ArchetypeQuery<ArcadeTransform, FallingStar, ArcadeVisual>> _starQueries = [];
    private readonly StarCatcherOptions _options;

    public RestartGameSystem(IOptions<StarCatcherOptions> options)
    {
        _options = options.Value;
    }

    protected override void OnAddStore(EntityStore store)
    {
        _starQueries.Add(store, store.Query<ArcadeTransform, FallingStar, ArcadeVisual>());
        base.OnAddStore(store);
    }

    protected override void OnRemoveStore(EntityStore store)
    {
        _starQueries.Remove(store);
        base.OnRemoveStore(store);
    }

    protected override void OnUpdate()
    {
        if (!Keyboard.GetState().IsKeyDown(Keys.Space)) return;

        var restarted = false;
        foreach (var (transforms, players, entities) in Query.Chunks)
        {
            for (var index = 0; index < entities.Length; index++)
            {
                ref var transform = ref transforms[index];
                ref var player = ref players[index];
                if (player.Lives > 0) continue;

                transform.Position = new Microsoft.Xna.Framework.Vector2(
                    (_options.Playfield.Width - transform.Size.X) / 2,
                    _options.Playfield.Height - transform.Size.Y * 2);
                transform.Size.X = player.BasePaddleWidth;
                player.Score = 0;
                player.Lives = _options.Paddle.StartingLives;
                player.PaddleEffect = PaddleSizeEffect.None;
                player.PaddleEffectTime = 0;
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
            }
        }
    }
}