using System;
using Friflo.Engine.ECS;
using KittyCat.Configuration;
using KittyCat.Ecs.Components;
using Microsoft.Extensions.Options;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using PurrplingCore.Ecs.Systems;

namespace KittyCat.Ecs.Systems;

public sealed partial class PlayerMovementSystem : StoreSystem
{
    private readonly PlayfieldOptions _playfield;

    public PlayerMovementSystem(IOptions<StarCatcherOptions> options)
    {
        _playfield = options.Value.Playfield;
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

        MovePlayerQuery(store, direction, _playfield.Width, _playfield.Height);
    }

    [Query]
    private void MovePlayer(
        ref ArcadeTransform transform,
        ref PlayerState player,
        Vector2 direction,
        int playfieldWidth,
        int playfieldHeight)
    {
        if (player.Lives <= 0) return;

        transform.Position += direction * player.MoveSpeed * Tick.deltaTime;
        transform.Position.X = Math.Clamp(transform.Position.X, 0, playfieldWidth - transform.Size.X);
        transform.Position.Y = Math.Clamp(transform.Position.Y, 0, playfieldHeight - transform.Size.Y);
    }
}