using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PurrplingCore.Toolkit.Rendering;

namespace KittyCat.Ecs.Systems;

/// <summary>
/// Pixel-art shapes shared by the world and HUD draw systems.
/// </summary>
internal static class StarCatcherShapes
{
    // 5x5 heart, one bit per column with the leftmost column in bit 4.
    private static ReadOnlySpan<byte> HeartRows => [0b11011, 0b11111, 0b11111, 0b01110, 0b00100];

    /// <summary>
    /// Draws the game's cross-shaped star as two filled rectangles.
    /// </summary>
    public static void DrawStar(this in LayerWriter<DrawCommand> writer, GraphicsDevice device, int x, int y, int size, Color color)
    {
        writer.FillRect(device, new Rectangle(x + size / 2 - 3, y, 6, size), color);
        writer.FillRect(device, new Rectangle(x, y + size / 2 - 3, size, 6), color);
    }

    /// <summary>
    /// Draws the five-row pixel heart at the supplied origin.
    /// </summary>
    public static void DrawHeart(this in LayerWriter<DrawCommand> writer, GraphicsDevice device, int x, int y, int pixelSize, Color color)
    {
        var rows = HeartRows;
        for (var row = 0; row < rows.Length; row++)
        {
            for (var column = 0; column < 5; column++)
            {
                if ((rows[row] & (0b10000 >> column)) == 0) continue;

                writer.FillRect(device,
                    new Rectangle(x + column * pixelSize, y + row * pixelSize, pixelSize, pixelSize), color);
            }
        }
    }
}
