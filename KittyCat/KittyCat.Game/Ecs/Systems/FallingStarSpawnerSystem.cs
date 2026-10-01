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

/// <summary>Maintains score-driven star waves and their speed for each world store.</summary>
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

        // Difficulty tuning uses the highest score in this store; the player query is cached per store.
        var difficulty = _options.Difficulty.GetProgress(score);
        var tuning = _options.Difficulty;
        // Easy cadence tightens with score. Medium keeps the easy interval; hard uses its faster interval.
        var spawnInterval = difficulty.Stage == DifficultyStage.Hard
            ? tuning.HardSpawnIntervalSeconds
            : difficulty.Stage == DifficultyStage.Medium
                ? tuning.EasySpawnIntervalSeconds
                : MathHelper.Lerp(_options.Stars.InitialSpawnIntervalSeconds, tuning.EasySpawnIntervalSeconds, difficulty.Progress);
        var activeCount = CountActiveStars();
        var desiredActive = GetDesiredActiveStars(score, difficulty, state, activeCount, tuning);

        state.TimeUntilSpawn -= Tick.deltaTime;
        if (activeCount >= desiredActive || state.TimeUntilSpawn > 0) return;

        var size = _options.Stars.Size;
        var entity = Query.Store.CreateEntity(
            new ArcadeTransform { Position = new Vector2(0, -size), Size = new Vector2(size, size) },
            new FallingStar(),
            new ArcadeVisual { Kind = ArcadeVisualKind.Star });

        ref var transform = ref entity.GetComponent<ArcadeTransform>();
        ref var newStar = ref entity.GetComponent<FallingStar>();
        ref var visual = ref entity.GetComponent<ArcadeVisual>();
        SpawnStar(ref transform, ref newStar, ref visual, difficulty);
        state.TimeUntilSpawn = spawnInterval;
    }

    private int GetDesiredActiveStars(
        int score,
        DifficultyProgress difficulty,
        StoreState state,
        int activeCount,
        DifficultyOptions tuning)
    {
        if (difficulty.Stage == DifficultyStage.Easy)
        {
            // Lock one target per wave so its random size does not change while stars are spawning.
            if (activeCount == 0 || state.WaveTarget == 0)
            {
                state.WaveTarget = SelectEasyWaveSize(score, tuning);
            }

            state.WaveWasActive |= activeCount > 0;
            return state.WaveTarget;
        }

        if (state.WaveTarget == 0 || (activeCount == 0 && state.WaveWasActive))
        {
            // Medium/hard select once at wave start; the target is reset only after a completed wave.
            state.WaveTarget = difficulty.Stage == DifficultyStage.Medium
                ? SelectMediumWaveSize(tuning.EasyMaximumActiveStars)
                : Random.Shared.Next(1, tuning.MaximumActiveStars + 1);
            state.WaveWasActive = false;
        }

        state.WaveWasActive |= activeCount > 0;
        return state.WaveTarget;
    }

    private int SelectEasyWaveSize(int score, DifficultyOptions tuning)
    {
        var firstBonusScore = 5;
        if (score < firstBonusScore) return _options.Stars.InitialMaxActive;

        // From score 5, the chance of a maximum-size base wave rises with easy progress; each wave also rolls a bonus star.
        var progress = Math.Clamp(
            (float)(score - firstBonusScore) / (tuning.EasyScoreLimit - firstBonusScore),
            0,
            1);
        var minimum = _options.Stars.InitialMaxActive;
        var maximum = tuning.EasyMaximumActiveStars;
        var maximumWaveChance = progress / (maximum - minimum + 1);
        var baseWaveSize = Random.Shared.NextSingle() < maximumWaveChance
            ? maximum
            : Random.Shared.Next(minimum, maximum);

        if (Random.Shared.NextSingle() < tuning.EasyBonusStarChance)
        {
            baseWaveSize++;
        }

        return Math.Min(baseWaveSize, tuning.MaximumActiveStars);
    }

    private static int SelectMediumWaveSize(int maximumStars)
    {
        // 80% of waves target 1-3 stars; the remaining 20% target 4-5.
        var roll = Random.Shared.Next(100);
        var count = roll switch
        {
            < 30 => 1,
            < 55 => 2,
            < 80 => 3,
            < 92 => 4,
            _ => 5,
        };

        return Math.Min(count, maximumStars);
    }

    private int CountActiveStars()
    {
        // Disabled/pending-destroy stars are excluded from the default query and do not occupy a wave slot.
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
        DifficultyProgress difficulty)
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

    private float GetFallSpeed(DifficultyProgress difficulty, PaddleSizeEffect paddleEffect)
    {
        var tuning = _options.Difficulty;
        // Easy only changes density; medium and hard interpolate speed and fast-star chance within their phase.
        var (minimum, maximum, fastChance) = difficulty.Stage switch
        {
            DifficultyStage.Easy => (
                tuning.EasyMinimumFallSpeed,
                tuning.EasyMaximumFallSpeed,
                tuning.EasyFastStarChance),
            DifficultyStage.Medium => (
                MathHelper.Lerp(tuning.EasyMinimumFallSpeed, tuning.MediumMinimumFallSpeed, difficulty.Progress),
                MathHelper.Lerp(tuning.EasyMaximumFallSpeed, tuning.MediumMaximumFallSpeed, difficulty.Progress),
                MathHelper.Lerp(tuning.EasyFastStarChance, tuning.MediumFastStarChance, difficulty.Progress)),
            _ => (
                MathHelper.Lerp(tuning.MediumMinimumFallSpeed, tuning.HardMinimumFallSpeed, difficulty.Progress),
                MathHelper.Lerp(tuning.MediumMaximumFallSpeed, tuning.HardMaximumFallSpeed, difficulty.Progress),
                MathHelper.Lerp(tuning.MediumFastStarChance, tuning.HardFastStarChance, difficulty.Progress)),
        };
        var speed = minimum + Random.Shared.NextSingle() * (maximum - minimum);
        speed *= paddleEffect switch
        {
            PaddleSizeEffect.Expand => _options.Stars.GreenSpeedMultiplier,
            PaddleSizeEffect.Shrink => _options.Stars.RedSpeedMultiplier,
            _ => 1f
        };

        return paddleEffect == PaddleSizeEffect.None && Random.Shared.NextSingle() < fastChance
            ? speed * _options.Stars.FastSpeedMultiplier
            : speed;
    }

    private sealed class StoreState(EntityStore store)
    {
        public ArchetypeQuery<PlayerState> Players { get; } = store.Query<PlayerState>();
        public float TimeUntilSpawn { get; set; }
        public int WaveTarget { get; set; }
        public bool WaveWasActive { get; set; }
    }
}