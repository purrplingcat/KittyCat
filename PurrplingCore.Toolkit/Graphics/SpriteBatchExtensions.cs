using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PurrplingCore.Toolkit.Graphics;

public static class SpriteBatchExtensions
{
    public static void Draw(this SpriteBatch spriteBatch, Canvas canvas, Color color)
    {
        spriteBatch.Draw(canvas.RenderTarget, canvas.Bounds, color);
    }

    public static void Draw(this SpriteBatch spriteBatch, Canvas.Snapshot snapshot, Color color)
    {
        spriteBatch.Draw(snapshot.Frame, snapshot.Bounds, color);
    }
}
