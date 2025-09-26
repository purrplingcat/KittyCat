using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PurrplingCore.Toolkit.Graphics;

public class Resolution
{
    private readonly GraphicsDeviceManager _manager;
    private readonly GameWindow _window;

    public int Width { get; private set; }
    public int Height { get; private set; }
    public Point Size => new(Width, Height);
    public DepthFormat Depth => _manager.PreferredDepthStencilFormat;
    public Rectangle WindowBounds => _window.ClientBounds;
    public Viewport Viewport => new(0, 0, Width, Height);
    public float AspectRatio => Width / (float)Height;
    public PresentationParameters PresentationParameters => _manager.GraphicsDevice.PresentationParameters;
    public GraphicsDevice GraphicsDevice => _manager.GraphicsDevice;

    public Resolution(GraphicsDeviceManager manager, GameWindow window)
    {
        _manager = manager;
        _window = window;

        SetResolution(_window.ClientBounds.Size);
    }

    public ResolutionMode Mode
    {
        get
        {
            if (_manager.IsFullScreen)
            {
                return _manager.HardwareModeSwitch
                    ? ResolutionMode.FullScreen
                    : ResolutionMode.BorderlessWindowed;
            }

            return ResolutionMode.Windowed;
        }
    }

    public event Action? ResolutionChanged;

    public Resolution SetResolution(int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 1, nameof(width));
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 1, nameof(height));

        Width = width;
        Height = height;
        return this;
    }

    public Resolution SetDepth(DepthFormat depth)
    {
        if (_manager.PreferredDepthStencilFormat != depth)
        {
            if (!Enum.IsDefined(typeof(DepthFormat), depth))
            {
                _manager.PreferredDepthStencilFormat = depth;
            }
        }

        return this;
    }

    public Resolution SetMode(ResolutionMode mode)
    {
        switch (mode)
        {
            case ResolutionMode.FullScreen:
                _manager.IsFullScreen = true;
                _manager.HardwareModeSwitch = true;
                break;
            case ResolutionMode.Windowed:
                _manager.IsFullScreen = false;
                break;
            case ResolutionMode.BorderlessWindowed:
                _manager.IsFullScreen = true;
                _manager.HardwareModeSwitch = false;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(mode), mode, "Invalid resolution mode.");
        }

        return this;
    }

    public void ApplyChanges()
    {
        // 🐷This for-cycle is Windows DX hack, because on DirectX the resolution changes aren't applied for the first time
        for (int i = 0; i < 2; i++) 
        {
            _manager.ApplyChanges(); // Preapply changes to calculate presentation resolution
            _manager.PreferredBackBufferWidth = Mode == ResolutionMode.BorderlessWindowed ? _window.ClientBounds.Width : Width;
            _manager.PreferredBackBufferHeight = Mode == ResolutionMode.BorderlessWindowed ? _window.ClientBounds.Height : Height;
            _manager.GraphicsProfile = GraphicsProfile.HiDef;
            _manager.ApplyChanges();
        }

        ResolutionChanged?.Invoke();
    }

    public Resolution SetResolution(Point resolution) => SetResolution(resolution.X, resolution.Y);

    public override string ToString() => $"{Width}x{Height}";
}

public enum ResolutionMode
{
    FullScreen,
    Windowed,
    BorderlessWindowed
}

