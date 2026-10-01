using System;
using Friflo.Engine.ECS;
using KittyCat.Configuration;
using KittyCat.Ecs.Components;
using Microsoft.Extensions.Options;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using PurrplingCore.Ecs.Systems;

namespace KittyCat.Ecs.Systems;

/// <summary>Moves the player within the playfield using keyboard input and score-based speed.</summary>
public sealed partial class PlayerMovementSystem : StoreSystem
{
    private readonly PlayfieldOptions _playfield;
    private readonly PaddleOptions _paddle;
    private readonly DifficultyOptions _difficulty;

    public PlayerMovementSystem(IOptions<StarCatcherOptions> options)
    {
        _playfield = options.Value.Playfield;
        _paddle = options.Value.Paddle;
        _difficulty = options.Value.Difficulty;
    }

    protected override void OnUpdate(EntityStore store)
    {
        var keyboard = Keyboard.GetState();
        var direction = Vector2.Zero;

        if (keyboard.IsKeyDown(Keys.Left) || keyboard.IsKeyDown(Keys.A)) direction.X -= 1;
        if (keyboard.IsKeyDown(Keys.Right) || keyboard.IsKeyDown(Keys.D)) direction.X += 1;
        if (keyboard.IsKeyDown(Keys.Up) || keyboard.IsKeyDown(Keys.W)) direction.Y -= 1;
        if (keyboard.IsKeyDown(Keys.Down) || keyboard.IsKeyDown(Keys.S)) direction.Y += 1;

        if (direction.LengthSquared() > 1)
        {
            direction.Normalize();
        }

        MovePlayerQuery(
            store,
            direction,
            _playfield.Width,
            _playfield.Height,
            _paddle.StrikesForMaximumSpeed,
            _paddle.MaximumStrikeSpeedMultiplier,
            _paddle.HardSpeedMultiplier,
            _difficulty.EasyScoreLimit,
            _difficulty.HardScoreLimit,
            _difficulty.ScoreForMaximumDifficulty);
    }

    [Query]
    private void MovePlayer(
        ref ArcadeTransform transform,
        ref PlayerState player,
        Vector2 direction,
        int playfieldWidth,
        int playfieldHeight,
        int strikesForMaximumSpeed,
        float maximumStrikeSpeedMultiplier,
        float hardSpeedMultiplier,
        int easyScoreLimit,
        int hardScoreLimit,
        int maximumDifficultyScore)
    {
        if (player.Lives <= 0) return;

        // Hard difficulty raises the base; strike then scales that base up to its configured cap.
        var strikeProgress = Math.Clamp((float)player.Strike / strikesForMaximumSpeed, 0, 1);
        var difficulty = DifficultyOptions.GetProgress(
            player.Score,
            easyScoreLimit,
            hardScoreLimit,
            maximumDifficultyScore);
        var hardProgress = difficulty.Stage == DifficultyStage.Hard ? difficulty.Progress : 0;
        var baseSpeed = player.MoveSpeed * MathHelper.Lerp(1, hardSpeedMultiplier, hardProgress);
        var moveSpeed = baseSpeed * MathHelper.Lerp(1, maximumStrikeSpeedMultiplier, strikeProgress);
        transform.Position += direction * moveSpeed * Tick.deltaTime;
        transform.Position.X = Math.Clamp(transform.Position.X, 0, playfieldWidth - transform.Size.X);
        transform.Position.Y = Math.Clamp(transform.Position.Y, 0, playfieldHeight - transform.Size.Y);
    }
}