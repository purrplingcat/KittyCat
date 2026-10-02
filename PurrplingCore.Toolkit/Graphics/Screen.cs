using Microsoft.Xna.Framework;

namespace PurrplingCore.Toolkit.Graphics;

/// <summary>
/// Describes a logical screen or a relative region of another screen without owning GPU resources.
/// </summary>
public class Screen
{
    private readonly Screen? _parent;
    private readonly float _x;
    private readonly float _y;
    private readonly float _width = 1f;
    private readonly float _height = 1f;

    private Point? _fixedSize;

    internal Screen? Parent => _parent;

    /// <summary>
    /// Creates a screen with an independent logical size.
    /// </summary>
    public Screen(Point size)
    {
        UpdateSize(size);
    }

    private Screen(Screen parent, float x, float y, float width, float height)
    {
        _parent = parent;
        _x = x;
        _y = y;
        _width = width;
        _height = height;
    }

    /// <summary>
    /// Logical size, defined by this screen or inherited from its parent region.
    /// </summary>
    public Point Size
    {
        get
        {
            if (_fixedSize.HasValue) return _fixedSize.Value;

            var parentSize = _parent!.Size;
            var region = GetRegion(new Rectangle(Point.Zero, parentSize));
            return new Point(Math.Max(1, region.Width), Math.Max(1, region.Height));
        }
    }

    /// <summary>
    /// Creates a child using fractions of this screen's width and height.
    /// Offsets may extend outside the parent; each size must be greater than zero and at most one.
    /// </summary>
    public Screen CreateRegion(float x, float y, float width, float height)
    {
        if (!float.IsFinite(x))
            throw new ArgumentOutOfRangeException(nameof(x));
        if (!float.IsFinite(y))
            throw new ArgumentOutOfRangeException(nameof(y));
        if (!float.IsFinite(width) || width <= 0f)
            throw new ArgumentOutOfRangeException(nameof(width));
        if (!float.IsFinite(height) || height <= 0f)
            throw new ArgumentOutOfRangeException(nameof(height));

        return new Screen(this, x, y, width, height);
    }

    /// <summary>
    /// Updates the logical size for a screen with a dynamic size source.
    /// </summary>
    protected void UpdateSize(Point size)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(size.X, 1, nameof(size));
        ArgumentOutOfRangeException.ThrowIfLessThan(size.Y, 1, nameof(size));
        _fixedSize = size;
    }

    internal Rectangle GetRegion(Rectangle bounds)
    {
        var left = bounds.X + (int)MathF.Round(bounds.Width * _x);
        var top = bounds.Y + (int)MathF.Round(bounds.Height * _y);
        var right = bounds.X + (int)MathF.Round(bounds.Width * (_x + _width));
        var bottom = bounds.Y + (int)MathF.Round(bounds.Height * (_y + _height));
        return new Rectangle(left, top, right - left, bottom - top);
    }
}
