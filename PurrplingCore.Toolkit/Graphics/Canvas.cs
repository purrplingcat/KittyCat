using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Diagnostics.CodeAnalysis;

namespace PurrplingCore.Toolkit.Graphics;
public class Canvas
{
    private readonly GraphicsDevice _graphics;
    private readonly Resolution _resolution;
    private readonly float _pixelZoom;
    private RenderTarget2D _renderTarget;
    private Rectangle _bounds;

    public GraphicsDevice GraphicsDevice => _graphics;
    public RenderTarget2D RenderTarget => _renderTarget;
    public Rectangle Bounds => _bounds;

    public Canvas(GraphicsDevice graphics, Resolution resolution, float pixelZoom = 1f)
    {
        _graphics = graphics ?? throw new ArgumentNullException(nameof(graphics));
        _resolution = resolution ?? throw new ArgumentNullException(nameof(resolution));
        _pixelZoom = pixelZoom;
        _resolution.ResolutionChanged += OnResolutionChanged;
        OnResolutionChanged();
    }

    public Snapshot CreateSnapshot() => new(this);


    [MemberNotNull(nameof(_renderTarget))]
    private void OnResolutionChanged()
    {
        var screenSize = _graphics.PresentationParameters.Bounds;
        int canvasWidth = (int)(_resolution.Width / _pixelZoom);
        int canvasHeight = (int)(_resolution.Height / _pixelZoom);

        float scaleX = (float)screenSize.Width / canvasWidth;
        float scaleY = (float)screenSize.Height / canvasHeight;
        float scale = Math.Min(scaleX, scaleY);

        int newWidth = (int)(_resolution.Width * scale);
        int newHeight = (int)(_resolution.Height * scale);

        int posX = (screenSize.Width - newWidth) / 2;
        int posY = (screenSize.Height - newHeight) / 2;

        _renderTarget = new RenderTarget2D(_graphics, canvasWidth, canvasHeight);
        _bounds = new Rectangle(posX, posY, newWidth, newHeight);
    }

    public readonly struct Snapshot
    {
        private readonly Rectangle _bounds;
        private readonly Texture2D _frame;

        public Snapshot(Texture2D frame, Rectangle bounds)
        {
            _frame = frame;
            _bounds = bounds;
        }

        public Snapshot(Canvas canvas)
        {
            _frame = canvas.RenderTarget;
            _bounds = canvas.Bounds;
        }

        public Rectangle Bounds => _bounds;
        public Texture2D Frame => _frame;
    }
}
