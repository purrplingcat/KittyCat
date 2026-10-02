using Microsoft.Xna.Framework;
using PurrplingCore.Toolkit.Rendering;

namespace PurrplingCore.Toolkit.Graphics;

/// <summary>
/// Describes a logical screen or a relative region of another screen without owning GPU resources.
/// </summary>
public class Screen : IDisposable
{
    private readonly Screen? _parent;
    private readonly float _x;
    private readonly float _y;
    private readonly float _width = 1f;
    private readonly float _height = 1f;
    private Resolution? _resolution;

    private Point? _fixedSize;
    private bool _disposed;

    /// <summary>
    /// Creates a screen that follows the supplied logical resolution.
    /// </summary>
    public Screen(Resolution resolution)
    {
        ArgumentNullException.ThrowIfNull(resolution);
        _resolution = resolution;
        _fixedSize = resolution.Size;
        resolution.ResolutionChanged += OnResolutionChanged;
    }

    /// <summary>
    /// Creates a screen with an independent logical size.
    /// </summary>
    public Screen(Point size)
    {
        UseFixedSize(size);
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
    /// Logical size, updated by the bound source's change event, inherited region, or explicit fixed size.
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
    /// How child regions map to this screen's logical image within its presentation area.
    /// Renderer image scaling is controlled separately by RendererOptions.ScaleMode.
    /// </summary>
    public RenderScaleMode ScaleMode { get; set; } = RenderScaleMode.Fit;

    /// <summary>
    /// Creates a child using fractions of this screen's width and height.
    /// Offsets may extend outside the parent; each size must be greater than zero and at most one.
    /// </summary>
    public Screen CreateRegion(float x, float y, float width, float height)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!float.IsFinite(x))
            throw new ArgumentOutOfRangeException(nameof(x));
        if (!float.IsFinite(y))
            throw new ArgumentOutOfRangeException(nameof(y));
        if (!float.IsFinite(width) || width <= 0f || width > 1f)
            throw new ArgumentOutOfRangeException(nameof(width));
        if (!float.IsFinite(height) || height <= 0f || height > 1f)
            throw new ArgumentOutOfRangeException(nameof(height));

        return new Screen(this, x, y, width, height);
    }

    /// <summary>
    /// Uses an independent logical size while retaining this screen's presentation area.
    /// </summary>
    public void UseFixedSize(Point size)
    {
        UpdateSize(size);
        DetachResolution();
    }

    private void UpdateSize(Point size)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentOutOfRangeException.ThrowIfLessThan(size.X, 1, nameof(size));
        ArgumentOutOfRangeException.ThrowIfLessThan(size.Y, 1, nameof(size));
        _fixedSize = size;
    }

    private void OnResolutionChanged()
    {
        if (_resolution is not null) UpdateSize(_resolution.Size);
    }

    private void DetachResolution()
    {
        var resolution = _resolution;
        if (resolution is null) return;

        resolution.ResolutionChanged -= OnResolutionChanged;
        _resolution = null;
    }

    /// <summary>
    /// Resolves this screen's presentation area in pixels of the supplied final output.
    /// </summary>
    public Rectangle GetPresentationBounds(Rectangle outputBounds)
    {
        if (outputBounds.Width <= 0 || outputBounds.Height <= 0) return Rectangle.Empty;
        if (_parent is null) return outputBounds;

        var parentArea = _parent.GetPresentationBounds(outputBounds);
        if (parentArea.Width <= 0 || parentArea.Height <= 0) return Rectangle.Empty;

        return GetRegion(_parent.GetImageBounds(parentArea));
    }

    internal ScreenLayout GetLayout(Rectangle outputBounds, RenderScaleMode scaleMode, RenderAlignment alignment = RenderAlignment.Center)
    {
        var size = Size;
        if (outputBounds.Width <= 0 || outputBounds.Height <= 0)
            return new ScreenLayout(size, Rectangle.Empty, Rectangle.Empty);

        if (_parent is null)
            return new ScreenLayout(size, outputBounds, outputBounds);

        var parentLayout = _parent.GetLayout(outputBounds, scaleMode, alignment);
        var parentImage = GetImageBounds(parentLayout.Bounds, parentLayout.Size, scaleMode, alignment);
        var bounds = GetRegion(parentImage);
        return new ScreenLayout(size, bounds, Rectangle.Intersect(bounds, parentLayout.Clip));
    }

    private Rectangle GetRegion(Rectangle bounds)
    {
        var left = bounds.X + (int)MathF.Round(bounds.Width * _x);
        var top = bounds.Y + (int)MathF.Round(bounds.Height * _y);
        var right = bounds.X + (int)MathF.Round(bounds.Width * (_x + _width));
        var bottom = bounds.Y + (int)MathF.Round(bounds.Height * (_y + _height));
        return new Rectangle(left, top, right - left, bottom - top);
    }

    private Rectangle GetImageBounds(Rectangle area)
        => GetImageBounds(area, Size, ScaleMode);

    internal static Rectangle GetImageBounds(Rectangle area, Point size, RenderScaleMode scaleMode, RenderAlignment alignment = RenderAlignment.Center)
    {
        var scaleX = (float)area.Width / size.X;
        var scaleY = (float)area.Height / size.Y;
        var fit = MathF.Min(scaleX, scaleY);
        var scale = scaleMode switch
        {
            RenderScaleMode.Stretch => new Vector2(scaleX, scaleY),
            RenderScaleMode.Fill => new Vector2(MathF.Max(scaleX, scaleY)),
            RenderScaleMode.Center => Vector2.One,
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

    /// <summary>
    /// Detaches this screen from resolution events without disposing its source or parent.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        DetachResolution();
        GC.SuppressFinalize(this);
    }
}

internal readonly record struct ScreenLayout(Point Size, Rectangle Bounds, Rectangle Clip);
