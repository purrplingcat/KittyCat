using System;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using KittyCat.Configuration;
using KittyCat.Ecs.Components;
using Microsoft.Extensions.Options;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PurrplingCore.Ecs.Attributes;
using PurrplingCore.Ecs.Systems;
using PurrplingCore.Graphics;
using PurrplingCore.Toolkit.Rendering;

namespace KittyCat.Ecs.Systems;

/// <summary>
/// Queues visible arcade entities into the world renderer.
/// </summary>
[System<DrawSystemGroup>]
public sealed class StarCatcherDrawSystem(
    GraphicsManager graphics,
    IOptions<StarCatcherOptions> options,
    RenderContext renderer)
    : QuerySystem<ArcadeTransform, ArcadeVisual>
{
    private const int ArenaHeartSize = 25;
    private readonly float _paddleWidth = options.Value.Paddle.Width;

    protected override void OnUpdate()
    {
        var device = graphics.GraphicsDevice;
        var entities = renderer.Batch(KittyCatLayers.Entities);
        var effects = renderer.Batch(KittyCatLayers.Effects);

        foreach (var (transforms, visuals, chunkEntities) in Query.Chunks)
        {
            for (var index = 0; index < chunkEntities.Length; index++)
            {
                var visual = visuals[index];
                if (!visual.IsVisible) continue;

                var transform = transforms[index];
                if (visual.Kind == ArcadeVisualKind.HeartPop)
                {
                    DrawHeartPop(in effects, device, in transform);
                    continue;
                }

                DrawEntity(in entities, device, in transform, visual.Kind);
            }
        }
    }

    private void DrawEntity(
        in LayerWriter<DrawCommand> writer,
        GraphicsDevice device,
        in ArcadeTransform transform,
        ArcadeVisualKind kind)
    {
        var x = (int)transform.Position.X;
        var y = (int)transform.Position.Y;
        var width = (int)transform.Size.X;
        var height = (int)transform.Size.Y;

        switch (kind)
        {
            case ArcadeVisualKind.Player:
                var paddleColor = transform.Size.X > _paddleWidth ? new Color(80, 220, 135)
                    : transform.Size.X < _paddleWidth ? new Color(240, 90, 100)
                    : new Color(70, 205, 220);
                writer.FillRect(device, new Rectangle(x, y, width, height), paddleColor);
                break;
            case ArcadeVisualKind.Star:
                writer.DrawStar(device, x, y, width, Color.Gold);
                break;
            case ArcadeVisualKind.GreenStar:
                writer.DrawStar(device, x, y, width, new Color(80, 235, 125));
                break;
            case ArcadeVisualKind.RedStar:
                writer.DrawStar(device, x, y, width, new Color(245, 75, 85));
                break;
            case ArcadeVisualKind.Heart:
                writer.DrawHeart(device,
                    x + (width - ArenaHeartSize) / 2,
                    y + (height - ArenaHeartSize) / 2,
                    pixelSize: 5,
                    new Color(255, 90, 145));
                break;
        }
    }

    /// <summary>
    /// Draws the heart-pop effect centered on its original floating-point transform.
    /// </summary>
    private static void DrawHeartPop(in LayerWriter<DrawCommand> writer, GraphicsDevice device, in ArcadeTransform transform)
    {
        var centerX = (int)(transform.Position.X + transform.Size.X / 2);
        var centerY = (int)(transform.Position.Y + transform.Size.Y / 2);
        var size = (int)Math.Max(transform.Size.X, transform.Size.Y);
        var radius = Math.Max(5, size / 2);
        var ray = Math.Max(2, size / 8);
        var color = new Color(255, 150, 190);

        writer.FillRect(device, new Rectangle(centerX - ray / 2, centerY - radius, ray, radius * 2), color);
        writer.FillRect(device, new Rectangle(centerX - radius, centerY - ray / 2, radius * 2, ray), color);
        writer.FillRect(device, new Rectangle(centerX - radius, centerY - radius, ray, ray), color);
        writer.FillRect(device, new Rectangle(centerX + radius - ray, centerY - radius, ray, ray), color);
        writer.FillRect(device, new Rectangle(centerX - radius, centerY + radius - ray, ray, ray), color);
        writer.FillRect(device, new Rectangle(centerX + radius - ray, centerY + radius - ray, ray, ray), color);
    }
}
