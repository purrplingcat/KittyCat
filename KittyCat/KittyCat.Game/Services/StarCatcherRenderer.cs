using System;
using Friflo.Engine.ECS;
using KittyCat.Configuration;
using KittyCat.Ecs.Components;
using Microsoft.Extensions.Options;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PurrplingCore.Graphics;
using PurrplingCore.Toolkit;
using PurrplingCore.Toolkit.DI;

namespace KittyCat.Services;

/// <summary>Draws the example game's entities and HUD using MonoGame's SpriteBatch.</summary>
[Singleton]
public sealed class StarCatcherRenderer : IDisposable
{
    private static readonly string[] HeartPixels = ["11011", "11111", "11111", "01110", "00100"];
    private const int StrikeMeterSegmentCount = 5;
    private readonly GraphicsManager _graphics;
    private readonly IContentManagerProvider _contentManagerProvider;
    private readonly StarCatcherOptions _options;
    private Texture2D? _pixel;
    private SpriteFont? _hudFont;
    private bool _disposed;

    public StarCatcherRenderer(
        GraphicsManager graphics,
        IContentManagerProvider contentManagerProvider,
        IOptions<StarCatcherOptions> options)
    {
        _graphics = graphics;
        _contentManagerProvider = contentManagerProvider;
        _options = options.Value;
    }

    public void BeginFrame()
    {
        _graphics.GraphicsDevice.Clear(new Color(14, 20, 31));
        _graphics.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
    }

    public void LoadContent()
    {
        // Load once during startup; DrawHud reuses the cached SpriteFont.
        _hudFont = _contentManagerProvider.ContentManager.Load<SpriteFont>("Fonts/Hud");
    }

    public void DrawEntity(in ArcadeTransform transform, in ArcadeVisual visual)
    {
        if (!visual.IsVisible) return;

        switch (visual.Kind)
        {
            case ArcadeVisualKind.Player:
                var paddleColor = transform.Size.X > _options.Paddle.Width
                    ? new Color(80, 220, 135)
                    : transform.Size.X < _options.Paddle.Width
                        ? new Color(240, 90, 100)
                        : new Color(70, 205, 220);
                _graphics.SpriteBatch.Draw(
                    Pixel,
                    new Rectangle((int)transform.Position.X, (int)transform.Position.Y,
                        (int)transform.Size.X, (int)transform.Size.Y),
                    paddleColor);
                break;
            case ArcadeVisualKind.Star:
                DrawStar((int)transform.Position.X, (int)transform.Position.Y,
                    (int)transform.Size.X, Color.Gold);
                break;
            case ArcadeVisualKind.GreenStar:
                DrawStar((int)transform.Position.X, (int)transform.Position.Y,
                    (int)transform.Size.X, new Color(80, 235, 125));
                break;
            case ArcadeVisualKind.RedStar:
                DrawStar((int)transform.Position.X, (int)transform.Position.Y,
                    (int)transform.Size.X, new Color(245, 75, 85));
                break;
            case ArcadeVisualKind.Heart:
                const int arenaHeartSize = 25;
                DrawHeart((int)(transform.Position.X + (transform.Size.X - arenaHeartSize) / 2),
                    (int)(transform.Position.Y + (transform.Size.Y - arenaHeartSize) / 2),
                    new Color(255, 90, 145), pixelSize: 5);
                break;
            case ArcadeVisualKind.HeartPop:
                DrawBurst((int)(transform.Position.X + transform.Size.X / 2),
                    (int)(transform.Position.Y + transform.Size.Y / 2),
                    (int)Math.Max(transform.Size.X, transform.Size.Y), new Color(255, 150, 190));
                break;
        }
    }

    public void DrawHud(int score, int lives, int strike)
    {
        var spriteBatch = _graphics.SpriteBatch;
        var playfield = _options.Playfield;
        spriteBatch.Draw(Pixel, new Rectangle(0, 0, playfield.Width, 54), new Color(24, 34, 49));
        DrawStar(20, 15, 24, Color.Gold);
        // Labels use the HUD font; numeric values keep the game's seven-segment style.
        spriteBatch.DrawString(HudFont, "SCORE", new Vector2(56, 17), Color.White);
        DrawNumber(score, 116, 12, Color.White);
        spriteBatch.DrawString(HudFont, "STRIKE", new Vector2(190, 17), new Color(120, 190, 255));
        DrawStrikeMeter(strike);
        DrawDifficultyIndicator(score);

        var firstHeartX = playfield.Width - PaddleOptions.MaximumLives * 26 - 16;
        for (var index = 0; index < PaddleOptions.MaximumLives; index++)
        {
            DrawHeart(firstHeartX + index * 26, 15,
                index < lives ? Color.Salmon : new Color(75, 82, 94),
                withShadow: index < _options.Paddle.StartingLives);
        }

        if (lives <= 0)
        {
            DrawGameOver(score);
        }
    }

    public void EndFrame() => _graphics.SpriteBatch.End();

    private void DrawGameOver(int score)
    {
        var spriteBatch = _graphics.SpriteBatch;
        var playfield = _options.Playfield;
        spriteBatch.Draw(Pixel, new Rectangle(playfield.Width / 4, playfield.Height * 7 / 18,
            playfield.Width / 2, playfield.Height / 4), new Color(64, 24, 38));
        const string gameOverText = "GAME OVER";
        var textSize = HudFont.MeasureString(gameOverText);
        spriteBatch.DrawString(HudFont, gameOverText,
            new Vector2((playfield.Width - textSize.X) / 2, playfield.Height / 2 - textSize.Y), Color.White);
        const string scoreLabel = "SCORE";
        textSize = HudFont.MeasureString(scoreLabel);
        var scoreY = playfield.Height / 2 + 8;
        var scoreGroupWidth = textSize.X + 8 + GetNumberWidth(score);
        var scoreX = (playfield.Width - scoreGroupWidth) / 2;
        spriteBatch.DrawString(HudFont, scoreLabel, new Vector2(scoreX, scoreY), Color.White);
        DrawNumber(score, (int)(scoreX + textSize.X + 8), scoreY, Color.White);
    }

    private Texture2D Pixel
    {
        get
        {
            if (_pixel is not null) return _pixel;

            _pixel = new Texture2D(_graphics.GraphicsDevice, 1, 1);
            _pixel.SetData([Color.White]);
            return _pixel;
        }
    }

    private SpriteFont HudFont
    {
        get => _hudFont ?? throw new InvalidOperationException(
            "The HUD font must be loaded before drawing. Call LoadContent() first.");
    }

    private void DrawNumber(int value, int x, int y, Color color)
    {
        ReadOnlySpan<byte> segments = [0b00111111, 0b00000110, 0b01011011, 0b01001111, 0b01100110,
            0b01101101, 0b01111101, 0b00000111, 0b01111111, 0b01101111];
        const int digitWidth = 20;
        const int digitSpacing = 8;
        value = Math.Max(0, value);
        Span<char> digits = stackalloc char[10];
        var digitCount = 0;
        do
        {
            digits[digitCount++] = (char)('0' + value % 10);
            value /= 10;
        }
        while (value > 0);

        for (var index = digitCount - 1; index >= 0; index--)
        {
            var digitX = x + (digitCount - index - 1) * (digitWidth + digitSpacing);
            var mask = segments[digits[index] - '0'];
            if ((mask & 1) != 0) DrawRect(digitX + 4, y, 12, 4, color);
            if ((mask & 2) != 0) DrawRect(digitX + 16, y + 4, 4, 10, color);
            if ((mask & 4) != 0) DrawRect(digitX + 16, y + 16, 4, 10, color);
            if ((mask & 8) != 0) DrawRect(digitX + 4, y + 26, 12, 4, color);
            if ((mask & 16) != 0) DrawRect(digitX, y + 16, 4, 10, color);
            if ((mask & 32) != 0) DrawRect(digitX, y + 4, 4, 10, color);
            if ((mask & 64) != 0) DrawRect(digitX + 4, y + 13, 12, 4, color);
        }
    }

    private static int GetNumberWidth(int value)
    {
        var digitCount = 1;
        for (value = Math.Max(0, value) / 10; value > 0; value /= 10)
        {
            digitCount++;
        }

        return digitCount * 20 + (digitCount - 1) * 8;
    }

    private void DrawStrikeMeter(int strike)
    {
        const int x = 260;
        const int y = 18;
        const int segmentWidth = 8;
        const int segmentHeight = 18;
        const int segmentSpacing = 4;
        var maximumStrike = _options.Paddle.StrikesForMaximumSpeed;
        var progress = Math.Clamp((float)strike / maximumStrike, 0, 1);
        var filledSegments = (int)Math.Ceiling(progress * StrikeMeterSegmentCount);

        for (var index = 0; index < StrikeMeterSegmentCount; index++)
        {
            var color = index < filledSegments
                ? new Color(120, 190, 255)
                : new Color(55, 70, 90);
            DrawRect(x + index * (segmentWidth + segmentSpacing), y, segmentWidth, segmentHeight, color);
        }
    }

    private void DrawDifficultyIndicator(int score)
    {
        const int centerX = 340;
        const int centerY = 27;
        const int radius = 6;
        var difficulty = _options.Difficulty.GetProgress(score);
        var color = difficulty.Stage switch
        {
            DifficultyStage.Easy => new Color(80, 220, 120),
            DifficultyStage.Medium => Color.Gold,
            _ => new Color(245, 75, 85),
        };

        for (var offsetY = -radius; offsetY <= radius; offsetY++)
        {
            var halfWidth = (int)Math.Sqrt(radius * radius - offsetY * offsetY);
            DrawRect(centerX - halfWidth, centerY + offsetY, halfWidth * 2 + 1, 1, color);
        }
    }

    private void DrawStar(int x, int y, int size, Color color)
    {
        DrawRect(x + size / 2 - 3, y, 6, size, color);
        DrawRect(x, y + size / 2 - 3, size, 6, color);
    }

    private void DrawHeart(int x, int y, Color color, int pixelSize = 4, bool withShadow = false)
    {
        if (withShadow)
        {
            DrawHeartShape(x + 2, y + 2, pixelSize, new Color(0, 0, 0, 110));
        }

        DrawHeartShape(x, y, pixelSize, color);
    }

    private void DrawHeartShape(int x, int y, int pixelSize, Color color)
    {
        for (var row = 0; row < HeartPixels.Length; row++)
        {
            for (var column = 0; column < HeartPixels[row].Length; column++)
            {
                if (HeartPixels[row][column] == '1')
                {
                    DrawRect(x + column * pixelSize, y + row * pixelSize, pixelSize, pixelSize, color);
                }
            }
        }
    }

    private void DrawBurst(int centerX, int centerY, int size, Color color)
    {
        var radius = Math.Max(5, size / 2);
        var ray = Math.Max(2, size / 8);
        DrawRect(centerX - ray / 2, centerY - radius, ray, radius * 2, color);
        DrawRect(centerX - radius, centerY - ray / 2, radius * 2, ray, color);
        DrawRect(centerX - radius, centerY - radius, ray, ray, color);
        DrawRect(centerX + radius - ray, centerY - radius, ray, ray, color);
        DrawRect(centerX - radius, centerY + radius - ray, ray, ray, color);
        DrawRect(centerX + radius - ray, centerY + radius - ray, ray, ray, color);
    }

    private void DrawRect(int x, int y, int width, int height, Color color)
    {
        _graphics.SpriteBatch.Draw(Pixel, new Rectangle(x, y, width, height), color);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _pixel?.Dispose();
    }
}