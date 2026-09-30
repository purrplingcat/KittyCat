using Microsoft.Xna.Framework;

namespace PurrplingCore.Toolkit;

/// <summary>
/// Read-only timing snapshots for the latest game update and draw ticks.
/// </summary>
public interface IGameClock
{
    GameTickSnapshot UpdateTick { get; }
    GameTickSnapshot DrawTick { get; }
}

/// <summary>
/// Timing data captured for one game-loop tick.
/// </summary>
public readonly record struct GameTickSnapshot(
    long Count,
    TimeSpan Elapsed,
    TimeSpan Total,
    bool IsRunningSlowly);

internal sealed class GameClock : IGameClock
{
    private long _updateCount;
    private long _drawCount;

    public GameTickSnapshot UpdateTick { get; private set; }
    public GameTickSnapshot DrawTick { get; private set; }

    public void RecordUpdate(GameTime gameTime)
    {
        UpdateTick = new GameTickSnapshot(
            ++_updateCount,
            gameTime.ElapsedGameTime,
            gameTime.TotalGameTime,
            gameTime.IsRunningSlowly);
    }

    public void RecordDraw(GameTime gameTime)
    {
        DrawTick = new GameTickSnapshot(
            ++_drawCount,
            gameTime.ElapsedGameTime,
            gameTime.TotalGameTime,
            gameTime.IsRunningSlowly);
    }
}