using KittyCat.Configuration;
using KittyCat.Ecs.Components;
using Microsoft.Xna.Framework;

namespace KittyCat.Ecs.Systems;

/// <summary>
/// Creates and restores the example game's player from its configured starting values.
/// </summary>
internal static class PlayerSetup
{
    /// <summary>
    /// Builds the player's transform at the configured starting position.
    /// </summary>
    public static ArcadeTransform CreateTransform(StarCatcherOptions options)
    {
        var size = new Vector2(options.Paddle.Width, options.Paddle.Height);
        return new ArcadeTransform
        {
            Position = GetStartingPosition(options.Playfield, size),
            Size = size,
        };
    }

    /// <summary>
    /// Creates fresh run state using the configured movement speed, width, and starting lives.
    /// </summary>
    public static PlayerState CreateState(PaddleOptions options)
    {
        return new PlayerState
        {
            MoveSpeed = options.MoveSpeed,
            BasePaddleWidth = options.Width,
            Lives = options.StartingLives,
        };
    }

    /// <summary>
    /// Restores position and all run-scoped values after game over.
    /// </summary>
    public static void Reset(ref ArcadeTransform transform, ref PlayerState player, StarCatcherOptions options)
    {
        transform.Size.X = player.BasePaddleWidth;
        transform.Position = GetStartingPosition(options.Playfield, transform.Size);
        player.Score = 0;
        player.Strike = 0;
        player.Lives = options.Paddle.StartingLives;
        player.PaddleEffect = PaddleSizeEffect.None;
        player.PaddleEffectTime = 0;
        player.InvincibilityTime = 0;
        player.InvincibilityBlinkTime = 0;
    }

    private static Vector2 GetStartingPosition(PlayfieldOptions playfield, Vector2 size)
    {
        // Keep one paddle-height of space below the starting position.
        return new Vector2((playfield.Width - size.X) / 2, playfield.Height - size.Y * 2);
    }
}