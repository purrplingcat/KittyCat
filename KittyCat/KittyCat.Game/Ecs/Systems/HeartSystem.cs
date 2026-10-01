using System;
using System.Collections.Generic;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using KittyCat.Configuration;
using KittyCat.Ecs.Components;
using Microsoft.Extensions.Options;
using Microsoft.Xna.Framework;
using PurrplingCore.Ecs.Attributes;
using PurrplingCore.Ecs.Components;
using PurrplingCore.Ecs.Systems;

namespace KittyCat.Ecs.Systems;

/// <summary>Spawns the life bonus, bounces it around the playfield, and handles collection or expiry.</summary>
[System<UpdateSystemGroup>]
public sealed partial class HeartSystem : QuerySystem<ArcadeTransform, BonusHeart, ArcadeVisual>
{
    private readonly HeartOptions _options;
    private readonly PlayfieldOptions _playfield;
    private readonly int _scoreForMaximumDifficulty;
    private readonly Dictionary<EntityStore, StoreState> _storeStates = [];
    private int _score;
    private bool _gameIsActive;
    private ArcadeTransform _collectingHeart;
    private bool _heartCollected;

    public HeartSystem(IOptions<StarCatcherOptions> options)
    {
        _options = options.Value.Hearts;
        _playfield = options.Value.Playfield;
        _scoreForMaximumDifficulty = options.Value.Difficulty.ScoreForMaximumDifficulty;
    }

    protected override void OnAddStore(EntityStore store)
    {
        _storeStates.Add(store, new StoreState(_options.SpawnIntervalSeconds));
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
        _gameIsActive = false;
        _score = 0;
        ReadPlayersQuery(Query.Store);

        if (!_gameIsActive)
        {
            DeactivateHearts();
            state.TimeUntilSpawn = _options.SpawnIntervalSeconds;
            return;
        }

        var deltaTime = Tick.deltaTime;
        // Heart speed scales with score independently of the star difficulty phases.
        var difficulty = Math.Clamp((float)_score / _scoreForMaximumDifficulty, 0, 1);
        var heartSpeed = _options.Speed * (1 + difficulty);
        var hasHeart = UpdateHearts(deltaTime, heartSpeed);

        state.TimeUntilSpawn -= deltaTime;
        if (hasHeart || state.TimeUntilSpawn > 0) return;

        foreach (var (transforms, hearts, visuals, entities) in Query.Chunks)
        {
            for (var index = 0; index < entities.Length; index++)
            {
                if (hearts[index].State != BonusHeartState.Inactive) continue;

                Spawn(ref transforms[index], ref hearts[index], ref visuals[index]);
                state.TimeUntilSpawn = _options.SpawnIntervalSeconds;
                return;
            }
        }

        var sizeForNewHeart = new Vector2(_options.Size, _options.Size);
        var entity = Query.Store.CreateEntity(
            new ArcadeTransform { Size = sizeForNewHeart },
            CreateHeart(),
            new ArcadeVisual { Kind = ArcadeVisualKind.Heart, IsVisible = true });
        ref var newTransform = ref entity.GetComponent<ArcadeTransform>();
        newTransform.Position = GetSpawnPosition();
        state.TimeUntilSpawn = _options.SpawnIntervalSeconds;
    }

    [Query]
    private void ReadPlayers(ref PlayerState player)
    {
        _score = Math.Max(_score, player.Score);
        _gameIsActive |= player.Lives > 0;
    }

    private bool UpdateHearts(float deltaTime, float heartSpeed)
    {
        var hasHeart = false;
        foreach (var (transforms, hearts, visuals, entities) in Query.Chunks)
        {
            for (var index = 0; index < entities.Length; index++)
            {
                ref var transform = ref transforms[index];
                ref var heart = ref hearts[index];
                ref var visual = ref visuals[index];
                if (heart.State == BonusHeartState.Inactive) continue;

                if (heart.State == BonusHeartState.Popping)
                {
                    heart.PopRemainingSeconds -= deltaTime;
                    if (heart.PopRemainingSeconds <= 0)
                    {
                        heart.State = BonusHeartState.Inactive;
                        visual.IsVisible = false;
                        MarkForDestruction(entities.EntityAt(index).Id);
                        continue;
                    }

                    var center = transform.Position + transform.Size / 2;
                    var progress = 1 - heart.PopRemainingSeconds / _options.PopDurationSeconds;
                    var size = _options.Size * (1 + 0.6f * progress);
                    transform.Size = new Vector2(size, size);
                    transform.Position = center - transform.Size / 2;
                    hasHeart = true;
                    continue;
                }

                var velocityLengthSquared = heart.Velocity.LengthSquared();
                if (velocityLengthSquared > 0)
                {
                    // Preserve the bounce direction while adjusting only velocity magnitude.
                    heart.Velocity *= heartSpeed / MathF.Sqrt(velocityLengthSquared);
                }

                MoveAndBounce(ref transform, ref heart, deltaTime);
                if (TryCollect(transform))
                {
                    heart.State = BonusHeartState.Inactive;
                    visual.IsVisible = false;
                    MarkForDestruction(entities.EntityAt(index).Id);
                    continue;
                }

                heart.RemainingSeconds -= deltaTime;
                if (heart.RemainingSeconds <= 0)
                {
                    heart.State = BonusHeartState.Popping;
                    heart.PopRemainingSeconds = _options.PopDurationSeconds;
                    visual.Kind = ArcadeVisualKind.HeartPop;
                }

                hasHeart = true;
            }
        }

        return hasHeart;
    }

    private void DeactivateHearts()
    {
        foreach (var (transforms, hearts, visuals, entities) in Query.Chunks)
        {
            for (var index = 0; index < entities.Length; index++)
            {
                if (hearts[index].State != BonusHeartState.Inactive)
                {
                    MarkForDestruction(entities.EntityAt(index).Id);
                }

                hearts[index].State = BonusHeartState.Inactive;
                visuals[index].IsVisible = false;
            }
        }
    }

    private void MarkForDestruction(int entityId)
    {
        // Cleanup removes only disabled entities carrying PendingDestroy after query iteration.
        CommandBuffer.AddTag<PendingDestroy>(entityId);
        CommandBuffer.AddTag<Disabled>(entityId);
    }

    private void Spawn(ref ArcadeTransform transform, ref BonusHeart heart, ref ArcadeVisual visual)
    {
        transform.Size = new Vector2(_options.Size, _options.Size);
        transform.Position = GetSpawnPosition();
        heart = CreateHeart();
        visual.Kind = ArcadeVisualKind.Heart;
        visual.IsVisible = true;
    }

    private BonusHeart CreateHeart()
    {
        var angle = MathHelper.ToRadians(20 + Random.Shared.NextSingle() * 50);
        var direction = Random.Shared.Next(2) == 0 ? -1 : 1;
        return new BonusHeart
        {
            Velocity = new Vector2(
                direction * MathF.Sin(angle) * _options.Speed,
                MathF.Cos(angle) * _options.Speed),
            RemainingSeconds = _options.LifetimeSeconds,
            State = BonusHeartState.Flying,
        };
    }

    private Vector2 GetSpawnPosition()
    {
        var maximumX = _playfield.Width - _options.Size;
        return new Vector2(Random.Shared.NextSingle() * maximumX, 0);
    }

    private void MoveAndBounce(ref ArcadeTransform transform, ref BonusHeart heart, float deltaTime)
    {
        var position = transform.Position;
        MoveAxis(ref position.X, ref heart.Velocity.X, _playfield.Width - transform.Size.X, deltaTime);
        MoveAxis(ref position.Y, ref heart.Velocity.Y, _playfield.Height - transform.Size.Y, deltaTime);
        transform.Position = position;
    }

    private static void MoveAxis(ref float position, ref float velocity, float maximum, float deltaTime)
    {
        position += velocity * deltaTime;
        while (position < 0 || position > maximum)
        {
            if (position < 0)
            {
                position = -position;
                velocity = MathF.Abs(velocity);
            }
            else
            {
                position = 2 * maximum - position;
                velocity = -MathF.Abs(velocity);
            }
        }
    }

    private bool TryCollect(ArcadeTransform heartTransform)
    {
        _collectingHeart = heartTransform;
        _heartCollected = false;
        CollectHeartQuery(Query.Store);
        return _heartCollected;
    }

    [Query]
    private void CollectHeart(ref ArcadeTransform transform, ref PlayerState player)
    {
        if (_heartCollected || player.Lives <= 0 || !transform.Intersects(in _collectingHeart)) return;

        // A heart grants only a capped life; it does not add score or strike.
        player.Lives = Math.Min(PaddleOptions.MaximumLives, player.Lives + 1);
        _heartCollected = true;
    }

    private sealed class StoreState(float spawnInterval)
    {
        public float TimeUntilSpawn { get; set; } = spawnInterval;
    }
}