using Friflo.Engine.ECS;
using KittyCat.Configuration;
using KittyCat.Ecs.Components;
using Microsoft.Extensions.Options;
using Microsoft.Xna.Framework;
using PurrplingCore.Ecs.Attributes;
using PurrplingCore.Ecs.Systems;

namespace KittyCat.Ecs.Systems;

[System<InitializeSystemGroup>]
public sealed class StarCatcherInitializeSystem : StoreSystem
{
    private readonly StarCatcherOptions _options;

    public StarCatcherInitializeSystem(IOptions<StarCatcherOptions> options)
    {
        _options = options.Value;
    }

    protected override void OnUpdate(EntityStore store)
    {
        var paddle = _options.Paddle;
        var playfield = _options.Playfield;
        var playerSize = new Vector2(paddle.Width, paddle.Height);
        store.CreateEntity(
            new ArcadeTransform
            {
                Position = new Vector2((playfield.Width - playerSize.X) / 2, playfield.Height - playerSize.Y * 2),
                Size = playerSize
            },
            new PlayerState
            {
                MoveSpeed = paddle.MoveSpeed,
                BasePaddleWidth = playerSize.X,
                Lives = paddle.StartingLives
            },
            new ArcadeVisual { Kind = ArcadeVisualKind.Player, IsVisible = true });
    }
}