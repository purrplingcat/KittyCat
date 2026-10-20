using System;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using KittyCat.Configuration;
using KittyCat.Ecs.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using PurrplingCore.Ecs.Attributes;
using PurrplingCore.Ecs.Systems;
using PurrplingCore.Graphics;
using PurrplingCore.Toolkit.Content;
using PurrplingCore.Toolkit.Rendering;

namespace KittyCat.Ecs.Systems;

/// <summary>
/// Queues the player's HUD and game-over panel into the screen renderer.
/// </summary>
[System<DrawSystemGroup>]
[PurrplingCore.Ecs.RunAfter<StarCatcherDrawSystem>]
public sealed class StarCatcherHudSystem(
    GraphicsManager graphics,
    AssetManager content,
    IOptions<StarCatcherOptions> options,
    [FromKeyedServices(KittyCatRendererKey.Screen)] IRenderer render)
    : QuerySystem<PlayerState>
{
    private const int LivesSpacing = 26;
    private const int LivesRightMargin = 16;
    private const int HeartPixelSize = 4;
    private const int StrikeMeterSegmentCount = 5;
    private const int DigitWidth = 20;
    private const int DigitSpacing = 8;
    private readonly StarCatcherOptions _options = options.Value;
    private readonly AssetHandle<SpriteFont> _font = content.Get<SpriteFont>(new AssetId("game:Font/Hud"));


    protected override void OnUpdate()
    {
        var device = graphics.GraphicsDevice;
        var hud = render.Context.Batch(KittyCatLayers.Hud);
        LayerWriter<DrawCommand> modal = default;
        var modalOpened = false;

        foreach (var (players, entities) in Query.Chunks)
        {
            for (var index = 0; index < entities.Length; index++)
            {
                var player = players[index];
                DrawHud(hud, device, _font.Asset, player);

                if (player.Lives <= 0)
                {
                    if (!modalOpened)
                    {
                        modal = render.Context.Batch(KittyCatLayers.Modal);
                        modalOpened = true;
                    }

                    DrawGameOver(in modal, device, _font.Asset, player.Score);
                }
            }
        }
    }

    private void DrawHud(in LayerWriter<DrawCommand> hud, GraphicsDevice device, SpriteFont font, in PlayerState player)
    {
        hud.FillRect(device, new Rectangle(0, 0, _options.Playfield.Width, PlayfieldOptions.HudHeight), new Color(24, 34, 49));
        hud.DrawStar(device, 20, 15, 24, Color.Gold);
        hud.DrawString(font, "SCORE", new Vector2(56, 17), Color.White);
        DrawNumber(in hud, device, player.Score, 116, 12, Color.White);
        hud.DrawString(font, "STRIKE", new Vector2(190, 17), new Color(120, 190, 255));
        DrawStrikeMeter(in hud, device, player.Strike);
        DrawDifficultyIndicator(in hud, device, player.Score);

        var firstHeartX = _options.Playfield.Width - PaddleOptions.MaximumLives * LivesSpacing - LivesRightMargin;
        for (var index = 0; index < PaddleOptions.MaximumLives; index++)
        {
            var heartX = firstHeartX + index * LivesSpacing;
            if (index < _options.Paddle.StartingLives)
            {
                hud.DrawHeart(device, heartX + 2, 17, HeartPixelSize, new Color(0, 0, 0, 110));
            }

            var color = index < player.Lives ? Color.Salmon : new Color(75, 82, 94);
            hud.DrawHeart(device, heartX, 15, HeartPixelSize, color);
        }
    }

    private void DrawGameOver(in LayerWriter<DrawCommand> modal, GraphicsDevice device, SpriteFont font, int score)
    {
        var playfield = _options.Playfield;
        modal.FillRect(device,
            new Rectangle(playfield.Width / 4, playfield.Height * 7 / 18, playfield.Width / 2, playfield.Height / 4),
            new Color(64, 24, 38));

        const string gameOverText = "GAME OVER";
        var textSize = font.MeasureString(gameOverText);
        modal.DrawString(font, gameOverText,
            new Vector2((playfield.Width - textSize.X) / 2, playfield.Height / 2 - textSize.Y), Color.White);

        const string scoreLabel = "SCORE";
        textSize = font.MeasureString(scoreLabel);
        var scoreY = playfield.Height / 2 + 8;
        var scoreX = (playfield.Width - (textSize.X + 8 + GetNumberWidth(score))) / 2;
        modal.DrawString(font, scoreLabel, new Vector2(scoreX, scoreY), Color.White);
        DrawNumber(in modal, device, score, (int)(scoreX + textSize.X + 8), scoreY, Color.White);
    }

    /// <summary>
    /// Draws a non-negative integer using the HUD's seven-segment style.
    /// </summary>
    private static void DrawNumber(in LayerWriter<DrawCommand> writer, GraphicsDevice device, int value, int x, int y, Color color)
    {
        ReadOnlySpan<byte> segments = [0b00111111, 0b00000110, 0b01011011, 0b01001111, 0b01100110,
            0b01101101, 0b01111101, 0b00000111, 0b01111111, 0b01101111];
        Span<byte> digits = stackalloc byte[10];
        var digitCount = 0;
        value = Math.Max(0, value);
        do
        {
            digits[digitCount++] = (byte)(value % 10);
            value /= 10;
        }
        while (value > 0);

        for (var index = digitCount - 1; index >= 0; index--)
        {
            var dx = x + (digitCount - index - 1) * (DigitWidth + DigitSpacing);
            var mask = segments[digits[index]];
            // Segment bits run clockwise from the top; bit 6 is the middle bar.
            if ((mask & 1) != 0) writer.FillRect(device, new Rectangle(dx + 4, y, 12, 4), color);
            if ((mask & 2) != 0) writer.FillRect(device, new Rectangle(dx + 16, y + 4, 4, 10), color);
            if ((mask & 4) != 0) writer.FillRect(device, new Rectangle(dx + 16, y + 16, 4, 10), color);
            if ((mask & 8) != 0) writer.FillRect(device, new Rectangle(dx + 4, y + 26, 12, 4), color);
            if ((mask & 16) != 0) writer.FillRect(device, new Rectangle(dx, y + 16, 4, 10), color);
            if ((mask & 32) != 0) writer.FillRect(device, new Rectangle(dx, y + 4, 4, 10), color);
            if ((mask & 64) != 0) writer.FillRect(device, new Rectangle(dx + 4, y + 13, 12, 4), color);
            }
    }

    /// <summary>
    /// Returns the width occupied by a non-negative seven-segment number.
    /// </summary>
    private static int GetNumberWidth(int value)
    {
        var digitCount = 1;
        for (value = Math.Max(0, value) / 10; value > 0; value /= 10)
        {
            digitCount++;
        }

        return digitCount * DigitWidth + (digitCount - 1) * DigitSpacing;
    }

    private void DrawStrikeMeter(in LayerWriter<DrawCommand> writer, GraphicsDevice device, int strike)
    {
        const int x = 260;
        const int y = 18;
        const int segmentWidth = 8;
        const int segmentHeight = 18;
        const int segmentSpacing = 4;
        var progress = Math.Clamp((float)strike / _options.Paddle.StrikesForMaximumSpeed, 0, 1);
        var filledSegments = (int)Math.Ceiling(progress * StrikeMeterSegmentCount);

        for (var index = 0; index < StrikeMeterSegmentCount; index++)
        {
            var color = index < filledSegments ? new Color(120, 190, 255) : new Color(55, 70, 90);
            writer.FillRect(device,
                new Rectangle(x + index * (segmentWidth + segmentSpacing), y, segmentWidth, segmentHeight), color);
        }
    }

    private void DrawDifficultyIndicator(in LayerWriter<DrawCommand> writer, GraphicsDevice device, int score)
    {
        var color = _options.Difficulty.GetProgress(score).Stage switch
        {
            DifficultyStage.Easy => new Color(80, 220, 120),
            DifficultyStage.Medium => Color.Gold,
            _ => new Color(245, 75, 85),
        };

        writer.FillCircle(device, new Vector2(340, 27), 6, color);
    }
}
