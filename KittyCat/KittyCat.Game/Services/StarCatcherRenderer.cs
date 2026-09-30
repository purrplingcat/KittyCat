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
using PurrplingGame = PurrplingCore.Toolkit.Game;

namespace KittyCat.Services;

[Singleton]
public sealed class StarCatcherRenderer : IDisposable
{
    private static readonly string[] HeartPixels = ["11011", "11111", "11111", "01110", "00100"];
    private readonly GraphicsManager _graphics;
    private readonly PurrplingGame _game;
    private readonly StarCatcherOptions _options;
    private Texture2D? _pixel;
    private int _lastScore = -1;
    private int _lastLives = -1;
    private bool _disposed;

    public StarCatcherRenderer(
        GraphicsManager graphics,
        PurrplingGame game,
        IOptions<StarCatcherOptions> options)
    {
        _graphics = graphics;
        _game = game;
        _options = options.Value;
    }

    public void BeginFrame()
    {
        _graphics.GraphicsDevice.Clear(new Color(14, 20, 31));
        _graphics.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
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
        }
    }

    public void DrawHud(int score, int lives)
    {
        var spriteBatch = _graphics.SpriteBatch;
        var playfield = _options.Playfield;
        spriteBatch.Draw(Pixel, new Rectangle(0, 0, playfield.Width, 54), new Color(24, 34, 49));
        DrawStar(20, 15, 24, Color.Gold);
        DrawNumber(score, 56, 12, Color.White);

        for (var index = 0; index < 3; index++)
        {
            DrawHeart(playfield.Width - 108 + index * 30, 15,
                index < lives ? Color.Salmon : new Color(75, 82, 94));
        }

        if (lives <= 0)
        {
            spriteBatch.Draw(Pixel, new Rectangle(playfield.Width / 4, playfield.Height * 7 / 18,
                playfield.Width / 2, playfield.Height / 4), new Color(64, 24, 38));
            DrawNumber(score, playfield.Width / 2 - 10, playfield.Height / 2 - 15, Color.White);
        }

        if (score != _lastScore || lives != _lastLives)
        {
            var title = lives <= 0
                ? $"STAR CATCHER | GAME OVER - SCORE {score} - PRESS SPACE TO RESTART"
                : $"STAR CATCHER | WASD / ARROWS: MOVE | SCORE: {score} | LIVES: {lives}";
            _game.Window.Title = title;
            _lastScore = score;
            _lastLives = lives;
        }
    }

    public void EndFrame() => _graphics.SpriteBatch.End();

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

    private void DrawNumber(int value, int x, int y, Color color)
    {
        ReadOnlySpan<byte> segments = [0b00111111, 0b00000110, 0b01011011, 0b01001111, 0b01100110,
            0b01101101, 0b01111101, 0b00000111, 0b01111111, 0b01101111];
        const int digitWidth = 20;
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
            var digitX = x + (digitCount - index - 1) * (digitWidth + 8);
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

    private void DrawStar(int x, int y, int size, Color color)
    {
        DrawRect(x + size / 2 - 3, y, 6, size, color);
        DrawRect(x, y + size / 2 - 3, size, 6, color);
    }

    private void DrawHeart(int x, int y, Color color)
    {
        const int pixelSize = 4;
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