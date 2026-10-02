using Microsoft.Xna.Framework;
using PurrplingCore.Toolkit.Graphics;

namespace PurrplingCore.Toolkit.Rendering;

/// <summary>
/// Describes a screen's logical size, presentation area, and inherited clip in output pixels.
/// </summary>
public readonly record struct ScreenLayout(Point Size, Rectangle Bounds, Rectangle Clip)
{
    /// <summary>
    /// Calculates presentation bounds and clipping using the renderer's scaling and alignment.
    /// </summary>
    public static ScreenLayout Calculate(Screen screen, Rectangle outputBounds, RenderScaleMode scaleMode, RenderAlignment alignment = RenderAlignment.Center)
    {
        ArgumentNullException.ThrowIfNull(screen);
        var size = screen.Size;
        if (outputBounds.Width <= 0 || outputBounds.Height <= 0)
            return new ScreenLayout(size, Rectangle.Empty, Rectangle.Empty);

        var parent = screen.Parent;
        if (parent is null)
            return new ScreenLayout(size, outputBounds, outputBounds);

        var parentLayout = Calculate(parent, outputBounds, scaleMode, alignment);
        var parentImage = GetImageBounds(parentLayout.Bounds, parentLayout.Size, scaleMode, alignment);
        var bounds = screen.GetRegion(parentImage);
        return new ScreenLayout(size, bounds, Rectangle.Intersect(bounds, parentLayout.Clip));
    }

    internal static Rectangle GetImageBounds(Rectangle area, Point size, RenderScaleMode scaleMode, RenderAlignment alignment = RenderAlignment.Center)
    {
        var scaleX = (float)area.Width / size.X;
        var scaleY = (float)area.Height / size.Y;
        var fit = MathF.Min(scaleX, scaleY);
        var scale = scaleMode switch
        {
            RenderScaleMode.Stretch => new Vector2(scaleX, scaleY),
            RenderScaleMode.Fill => new Vector2(MathF.Max(scaleX, scaleY)),
            RenderScaleMode.Native => Vector2.One,
            RenderScaleMode.IntegerFit => new Vector2(fit >= 1f ? MathF.Floor(fit) : fit),
            _ => new Vector2(fit),
        };

        var width = (int)MathF.Round(size.X * scale.X);
        var height = (int)MathF.Round(size.Y * scale.Y);
        var horizontalSpace = area.Width - width;
        var verticalSpace = area.Height - height;
        var offsetX = alignment switch
        {
            RenderAlignment.TopLeft or RenderAlignment.CenterLeft or RenderAlignment.BottomLeft => 0,
            RenderAlignment.TopRight or RenderAlignment.CenterRight or RenderAlignment.BottomRight => horizontalSpace,
            _ => (int)MathF.Floor(horizontalSpace * 0.5f),
        };
        var offsetY = alignment switch
        {
            RenderAlignment.TopLeft or RenderAlignment.TopCenter or RenderAlignment.TopRight => 0,
            RenderAlignment.BottomLeft or RenderAlignment.BottomCenter or RenderAlignment.BottomRight => verticalSpace,
            _ => (int)MathF.Floor(verticalSpace * 0.5f),
        };
        return new Rectangle(
            area.X + offsetX,
            area.Y + offsetY,
            width,
            height);
    }
}