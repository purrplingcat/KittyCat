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
    private Point _referenceSize;
    private bool _applying;

    public int Width { get; private set; }
    public int Height { get; private set; }
    public Point Size => new(Width, Height);
    public DepthFormat Depth => _manager.PreferredDepthStencilFormat;
    public Rectangle WindowBounds => _window.ClientBounds;
    public Viewport Viewport => new(0, 0, Width, Height);
    public float AspectRatio => Width / (float)Height;
    public PresentationParameters PresentationParameters => _manager.GraphicsDevice.PresentationParameters;
    public GraphicsDevice GraphicsDevice => _manager.GraphicsDevice;

    /// <summary>
    /// Controls whether the logical resolution stays fixed, expands with aspect ratio, or follows the client area.
    /// Changes take effect when ApplyChanges is called.
    /// </summary>
    public VirtualResolutionMode VirtualMode { get; private set; }

    public Resolution(GraphicsDeviceManager manager, GameWindow window)
    {
        _manager = manager;
        _window = window;
        _manager.PreparingDeviceSettings += OnPreparingDeviceSettings;
        _window.ClientSizeChanged += OnClientSizeChanged;

        SetResolution(_window.ClientBounds.Size);
    }

    // MonoGame clears a DiscardContents back buffer whenever it is re-bound, which erases earlier
    // renderers as soon as a later one switches to an offscreen target and back.
    private static void OnPreparingDeviceSettings(object? sender, PreparingDeviceSettingsEventArgs args)
        => args.GraphicsDeviceInformation.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PreserveContents;

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

    /// <summary>
    /// Sets the requested windowed backbuffer size and the reference logical resolution.
    /// Responsive mode treats this logical size as the minimum visible coordinate space.
    /// </summary>
    public Resolution SetResolution(int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 1, nameof(width));
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 1, nameof(height));

        _referenceSize = new Point(width, height);
        Width = width;
        Height = height;
        return this;
    }

    /// <summary>
    /// Selects logical sizing independently of the window display mode.
    /// Call ApplyChanges to apply the selected mode.
    /// </summary>
    public Resolution SetVirtualMode(VirtualResolutionMode mode)
    {
        if (!Enum.IsDefined(typeof(VirtualResolutionMode), mode))
            throw new ArgumentOutOfRangeException(nameof(mode), mode, "Invalid virtual resolution mode.");

        VirtualMode = mode;
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
        _applying = true;
        try
        {
            // 🐷This for-cycle is Windows DX hack, because on DirectX the resolution changes aren't applied for the first time
            for (int i = 0; i < 2; i++)
            {
                _manager.ApplyChanges(); // Preapply changes to calculate presentation resolution
                _manager.PreferredBackBufferWidth = Mode == ResolutionMode.BorderlessWindowed ? _window.ClientBounds.Width : _referenceSize.X;
                _manager.PreferredBackBufferHeight = Mode == ResolutionMode.BorderlessWindowed ? _window.ClientBounds.Height : _referenceSize.Y;
                _manager.GraphicsProfile = GraphicsProfile.HiDef;
                _manager.ApplyChanges();
            }

            UpdateVirtualSize();
        }
        finally
        {
            _applying = false;
        }

        ResolutionChanged?.Invoke();
    }

    /// <summary>
    /// Updates logical size after client changes and resizes the backbuffer only in windowed mode.
    /// </summary>
    private void OnClientSizeChanged(object? sender, EventArgs args)
    {
        if (_applying) return;

        var size = _window.ClientBounds.Size;

        // Minimizing reports an empty client area on some platforms.
        if (size.X <= 0 || size.Y <= 0) return;

        if (Mode != ResolutionMode.Windowed)
        {
            if (VirtualMode != VirtualResolutionMode.MatchClient || Size == size) return;

            UpdateVirtualSize();
            ResolutionChanged?.Invoke();
            return;
        }

        if (size.X == _manager.PreferredBackBufferWidth && size.Y == _manager.PreferredBackBufferHeight
            && Size == CalculateVirtualSize(size)) return;

        _applying = true;
        try
        {
            _manager.PreferredBackBufferWidth = size.X;
            _manager.PreferredBackBufferHeight = size.Y;
            _manager.ApplyChanges();
            UpdateVirtualSize();
        }
        finally
        {
            _applying = false;
        }

        ResolutionChanged?.Invoke();
    }

    private void UpdateVirtualSize()
    {
        Point outputSize;
        if (VirtualMode == VirtualResolutionMode.MatchClient)
        {
            outputSize = _window.ClientBounds.Size;
            if (outputSize.X <= 0 || outputSize.Y <= 0) return;
        }
        else
        {
            var parameters = PresentationParameters;
            outputSize = new Point(parameters.BackBufferWidth, parameters.BackBufferHeight);
        }

        var size = CalculateVirtualSize(outputSize);
        Width = size.X;
        Height = size.Y;
    }

    private Point CalculateVirtualSize(Point outputSize)
    {
        if (VirtualMode == VirtualResolutionMode.Fixed || outputSize.X <= 0 || outputSize.Y <= 0)
            return _referenceSize;

        if (VirtualMode == VirtualResolutionMode.MatchClient) return outputSize;

        if ((long)outputSize.X * _referenceSize.Y >= (long)outputSize.Y * _referenceSize.X)
        {
            var width = (int)Math.Round((double)_referenceSize.Y * outputSize.X / outputSize.Y);
            return new Point(Math.Max(_referenceSize.X, width), _referenceSize.Y);
        }

        var height = (int)Math.Round((double)_referenceSize.X * outputSize.Y / outputSize.X);
        return new Point(_referenceSize.X, Math.Max(_referenceSize.Y, height));
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

/// <summary>
/// Determines how the logical resolution follows the physical output or client area.
/// </summary>
public enum VirtualResolutionMode
{
    /// <summary>
    /// Keeps the configured reference size regardless of output dimensions.
    /// </summary>
    Fixed = 0,

    /// <summary>
    /// Keeps the reference size as a minimum and expands one axis to match the output aspect ratio.
    /// </summary>
    Responsive,

    /// <summary>
    /// Copies client-area dimensions in windowed, borderless, and exclusive-fullscreen modes.
    /// </summary>
    MatchClient,
}

