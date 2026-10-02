using Microsoft.Xna.Framework;

namespace PurrplingCore.Toolkit.Rendering;

/// <summary>
/// Default <see cref="ICamera2D"/> with position, zoom and rotation around a screen-space origin.
/// </summary>
/// <remarks>
/// The view matrix is rebuilt only when a property or the viewport actually changes, so layers
/// sharing the camera pay for the computation at most once per frame and parallax variants are
/// derived from the cached base transform.
/// </remarks>
public sealed class Camera2D : ICamera2D, IViewportCamera2D
{
    private Vector2 _position;
    private Vector2 _origin;
    private Vector2 _viewportSize = Vector2.One;
    private float _zoom = 1f;
    private float _rotation;
    private Matrix _rotationScale;
    private bool _dirty = true;

    /// <summary>
    /// Camera position in world space.
    /// </summary>
    public Vector2 Position
    {
        get => _position;
        set => Set(ref _position, value);
    }

    /// <summary>
    /// Screen-space point the camera is centred on. Usually half the viewport.
    /// </summary>
    public Vector2 Origin
    {
        get => _origin;
        set => Set(ref _origin, value);
    }

    /// <summary>
    /// Uniform zoom factor. Values above one magnify the world.
    /// </summary>
    public float Zoom
    {
        get => _zoom;
        set => Set(ref _zoom, value);
    }

    /// <summary>
    /// Camera rotation in radians.
    /// </summary>
    public float Rotation
    {
        get => _rotation;
        set => Set(ref _rotation, value);
    }

    /// <summary>
    /// Updates the viewport size used to compute visible bounds and the default origin.
    /// </summary>
    public void SetViewport(int width, int height)
    {
        var size = new Vector2(width, height);
        if (_viewportSize == size) return;

        _viewportSize = size;
        _dirty = true;
    }

    /// <summary>
    /// Centres the origin in the current viewport.
    /// </summary>
    public void CenterOrigin() => Origin = _viewportSize * 0.5f;

    public Matrix GetViewMatrix(Vector2 parallax)
    {
        EnsureTransform();

        // Parallax only scales how far the camera has travelled; zoom and rotation stay shared.
        return Matrix.CreateTranslation(new Vector3(-_position * parallax, 0f))
            * _rotationScale
            * Matrix.CreateTranslation(new Vector3(_origin, 0f));
    }

    public Rectangle GetVisibleBounds(Vector2 parallax)
    {
        GetVisibleOffsets(out var minimum, out var maximum);
        var position = _position * parallax;
        minimum += position;
        maximum += position;
        return new Rectangle(
            (int)MathF.Floor(minimum.X),
            (int)MathF.Floor(minimum.Y),
            (int)MathF.Ceiling(maximum.X - minimum.X),
            (int)MathF.Ceiling(maximum.Y - minimum.Y));
    }

    /// <summary>
    /// Keeps the visible world bounds inside <paramref name="bounds"/>. If the view is larger
    /// than the supplied bounds on an axis, it is centered on that axis.
    /// </summary>
    public void ClampPosition(Rectangle bounds)
    {
        GetVisibleOffsets(out var minimum, out var maximum);
        var position = _position;

        if (maximum.X - minimum.X >= bounds.Width)
        {
            position.X = bounds.Left + bounds.Width * 0.5f;
        }
        else
        {
            position.X = Math.Clamp(position.X, bounds.Left - minimum.X, bounds.Right - maximum.X);
        }

        if (maximum.Y - minimum.Y >= bounds.Height)
        {
            position.Y = bounds.Top + bounds.Height * 0.5f;
        }
        else
        {
            position.Y = Math.Clamp(position.Y, bounds.Top - minimum.Y, bounds.Bottom - maximum.Y);
        }

        Position = position;
    }

    private void GetVisibleOffsets(out Vector2 minimum, out Vector2 maximum)
    {
        EnsureTransform();
        var inverse = Matrix.Invert(_rotationScale);
        var topLeft = Vector2.Transform(-_origin, inverse);
        var bottomRight = Vector2.Transform(_viewportSize - _origin, inverse);
        var topRight = Vector2.Transform(new Vector2(_viewportSize.X, 0f) - _origin, inverse);
        var bottomLeft = Vector2.Transform(new Vector2(0f, _viewportSize.Y) - _origin, inverse);
        minimum = Vector2.Min(Vector2.Min(topLeft, bottomRight), Vector2.Min(topRight, bottomLeft));
        maximum = Vector2.Max(Vector2.Max(topLeft, bottomRight), Vector2.Max(topRight, bottomLeft));
    }

    private void EnsureTransform()
    {
        if (!_dirty) return;

        _dirty = false;
        _rotationScale = Matrix.CreateRotationZ(_rotation) * Matrix.CreateScale(_zoom, _zoom, 1f);
    }

    private void Set<T>(ref T field, T value) where T : struct, IEquatable<T>
    {
        if (field.Equals(value)) return;

        field = value;
        _dirty = true;
    }
}
