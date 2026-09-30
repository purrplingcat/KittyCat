using System;
using System.Collections.Generic;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using KittyCat.Configuration;
using KittyCat.Ecs.Components;
using Microsoft.Extensions.Options;
using Microsoft.Xna.Framework;
using PurrplingCore.Ecs;
using PurrplingCore.Ecs.Attributes;
using PurrplingCore.Ecs.Systems;

namespace KittyCat.Ecs.Systems;

[System<UpdateSystemGroup>]
public sealed class FallingStarSpawnerSystem : QuerySystem<ArcadeTransform, FallingStar, ArcadeVisual>
{
    private readonly StarCatcherOptions _options;
    private readonly Dictionary<EntityStore, StoreState> _storeStates = [];

    public FallingStarSpawnerSystem(IOptions<StarCatcherOptions> options)
    {
        _options = options.Value;
    }

    protected override void OnAddStore(EntityStore store)
    {
        _storeStates.Add(store, new StoreState(store));
        base.OnAddStore(store);
    }

    protected override void OnRemoveStore(EntityStore store)
    {
        _storeStates.Remove(store);
        base.OnRemoveStore(store);
    }

    protected override void OnUpdate()
    {
        var state = _storeStates[Query.Store];
        var score = 0;
        var gameIsActive = false;
        foreach (var (players, entities) in state.Players.Chunks)
        {
            for (var index = 0; index < entities.Length; index++)
            {
                ref var player = ref players[index];
                score = Math.Max(score, player.Score);
                gameIsActive |= player.Lives > 0;
            }
        }

        if (!gameIsActive) return;

        var difficulty = Math.Clamp((float)score / _options.Difficulty.ScoreForMaximumDifficulty, 0, 1);
        var spawnInterval = MathHelper.Lerp(
            _options.Stars.InitialSpawnIntervalSeconds,
            _options.Difficulty.MaximumSpawnIntervalSeconds,
            difficulty);
        var maximumActive = _options.Stars.InitialMaxActive
            + (int)((_options.Difficulty.MaximumActiveStars - _options.Stars.InitialMaxActive) * difficulty);
        var activeCount = CountActiveStars();

        state.TimeUntilSpawn -= Tick.deltaTime;
        if (activeCount >= maximumActive || state.TimeUntilSpawn > 0) return;

        foreach (var (transforms, stars, visuals, entities) in Query.Chunks)
        {
            for (var index = 0; index < entities.Length; index++)
            {
                ref var star = ref stars[index];
                if (star.IsActive) continue;

                SpawnStar(ref transforms[index], ref star, ref visuals[index], difficulty);
                state.TimeUntilSpawn = spawnInterval;
                return;
            }
        }

        if (state.CreatedStarCount >= _options.Stars.MaximumCreatedStars) return;

        var size = _options.Stars.Size;
        var entity = Query.Store.CreateEntity(
            new ArcadeTransform { Position = new Vector2(0, -size), Size = new Vector2(size, size) },
            new FallingStar(),
            new ArcadeVisual { Kind = ArcadeVisualKind.Star });
        state.CreatedStarCount++;

        ref var transform = ref entity.GetComponent<ArcadeTransform>();
        ref var newStar = ref entity.GetComponent<FallingStar>();
        ref var visual = ref entity.GetComponent<ArcadeVisual>();
        SpawnStar(ref transform, ref newStar, ref visual, difficulty);
        state.TimeUntilSpawn = spawnInterval;
    }

    private int CountActiveStars()
    {
        var count = 0;
        foreach (var (transforms, stars, visuals, entities) in Query.Chunks)
        {
            for (var index = 0; index < entities.Length; index++)
            {
                if (stars[index].IsActive) count++;
            }
        }

        return count;
    }

    private void SpawnStar(
        ref ArcadeTransform transform,
        ref FallingStar star,
        ref ArcadeVisual visual,
        float difficulty)
    {
        star.PaddleEffect = GetRandomPaddleEffect();
        star.FallSpeed = GetFallSpeed(difficulty, star.PaddleEffect);
        star.IsActive = true;
        visual.Kind = star.PaddleEffect switch
        {
            PaddleSizeEffect.Expand => ArcadeVisualKind.GreenStar,
            PaddleSizeEffect.Shrink => ArcadeVisualKind.RedStar,
            _ => ArcadeVisualKind.Star
        };
        visual.IsVisible = true;
        transform.Position = new Vector2(
            Random.Shared.Next(24, _options.Playfield.Width - 24),
            -transform.Size.Y);
    }

    private PaddleSizeEffect GetRandomPaddleEffect()
    {
        if (Random.Shared.NextSingle() >= _options.Stars.SpecialSpawnChance)
        {
            return PaddleSizeEffect.None;
        }

        return Random.Shared.Next(2) == 0 ? PaddleSizeEffect.Expand : PaddleSizeEffect.Shrink;
    }

    private float GetFallSpeed(float difficulty, PaddleSizeEffect paddleEffect)
    {
        var minimum = MathHelper.Lerp(
            _options.Difficulty.EasyMinimumFallSpeed,
            _options.Difficulty.HardMinimumFallSpeed,
            difficulty);
        var maximum = MathHelper.Lerp(
            _options.Difficulty.EasyMaximumFallSpeed,
            _options.Difficulty.HardMaximumFallSpeed,
            difficulty);
        var speed = minimum + Random.Shared.NextSingle() * (maximum - minimum);
        speed *= paddleEffect switch
        {
            PaddleSizeEffect.Expand => _options.Stars.GreenSpeedMultiplier,
            PaddleSizeEffect.Shrink => _options.Stars.RedSpeedMultiplier,
            _ => 1f
        };

        var fastChance = MathHelper.Lerp(
            _options.Difficulty.EasyFastStarChance,
            _options.Difficulty.HardFastStarChance,
            difficulty);
        return paddleEffect == PaddleSizeEffect.None && Random.Shared.NextSingle() < fastChance
            ? speed * _options.Stars.FastSpeedMultiplier
            : speed;
    }

    private sealed class StoreState(EntityStore store)
    {
        public ArchetypeQuery<PlayerState> Players { get; } = store.Query<PlayerState>();
        public float TimeUntilSpawn { get; set; }
        public int CreatedStarCount { get; set; }
    }
}