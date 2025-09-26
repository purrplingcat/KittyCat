using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PurrplingCore.Toolkit.Graphics;

public readonly struct TextureRegion2D
{
    private readonly string _name;
    private readonly Texture2D _texture;
    private readonly Rectangle _bounds;

    public TextureRegion2D(Texture2D texture, Rectangle bounds)
    {
        _texture = texture ?? throw new ArgumentNullException(nameof(texture));
        _bounds = bounds;
        _name = string.Empty;
    }

    public TextureRegion2D(Texture2D texture, Rectangle bounds, string name)
    {
        _texture = texture ?? throw new ArgumentNullException(nameof(texture));
        _bounds = bounds;
        _name = name ?? throw new ArgumentNullException(nameof(name));
    }

    public string Name => _name;
    public Texture2D Texture => _texture;
    public Rectangle Bounds => _bounds;

    public int Width => _bounds.Width;
    public int Height => _bounds.Height;
    public Point Location => _bounds.Location;

    public TextureRegion2D Transform(Rectangle transform)
    {
        var bounds = _bounds;

        bounds.X += transform.X;
        bounds.Y += transform.Y;
        bounds.Width += transform.Width;
        bounds.Height += transform.Height;

        return new TextureRegion2D(_texture, bounds, _name);
    }

    public TextureRegion2D Transform(Point location, int width, int height) => Transform(new Rectangle(location.X, location.Y, width, height));

    public TextureRegion2D Copy(string name) => new(_texture, _bounds, name);
    public TextureRegion2D Copy() => new(_texture, _bounds, _name);
}
