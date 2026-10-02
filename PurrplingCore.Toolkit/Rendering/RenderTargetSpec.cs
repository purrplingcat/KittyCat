using Microsoft.Xna.Framework.Graphics;

namespace PurrplingCore.Toolkit.Rendering;

/// <summary>
/// Determines how a render target follows logical screen or presentation size changes.
/// </summary>
public enum RenderTargetResizeMode : byte
{
    /// <summary>
    /// Keeps the explicit <see cref="RenderTargetSpec.Width"/> and <see cref="RenderTargetSpec.Height"/>.
    /// </summary>
    Fixed = 0,

    /// <summary>
    /// Tracks the renderer's presentation area in output pixels.
    /// </summary>
    MatchPresentation,

    /// <summary>
    /// Tracks the renderer's logical screen size.
    /// </summary>
    MatchScreen,
}

/// <summary>
/// Describes an offscreen target a layer or renderer draws into before it is composed.
/// </summary>
/// <remarks>
/// Targets are created and recreated by the renderer, not by the submitting code, so a resolution
/// change only reallocates GPU memory and never touches the command path.
/// </remarks>
public readonly struct RenderTargetSpec
{
    private RenderTargetSpec(
        RenderTargetResizeMode resizeMode,
        int width,
        int height,
        SurfaceFormat format,
        DepthFormat depthFormat)
    {
        ResizeMode = resizeMode;
        Width = width;
        Height = height;
        Format = format;
        DepthFormat = depthFormat;
    }

    public RenderTargetResizeMode ResizeMode { get; }

    /// <summary>
    /// Width used by <see cref="RenderTargetResizeMode.Fixed"/>.
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// Height used by <see cref="RenderTargetResizeMode.Fixed"/>.
    /// </summary>
    public int Height { get; }

    public SurfaceFormat Format { get; }

    public DepthFormat DepthFormat { get; }

    /// <summary>
    /// Creates a target of a constant size, independent of the window.
    /// </summary>
    public static RenderTargetSpec OfSize(
        int width,
        int height,
        SurfaceFormat format = SurfaceFormat.Color,
        DepthFormat depthFormat = DepthFormat.None)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 1);

        return new RenderTargetSpec(RenderTargetResizeMode.Fixed, width, height, format, depthFormat);
    }

    /// <summary>
    /// Creates a target that tracks the renderer's presentation area in output pixels.
    /// </summary>
    public static RenderTargetSpec MatchPresentation(
        SurfaceFormat format = SurfaceFormat.Color,
        DepthFormat depthFormat = DepthFormat.None)
        => new(RenderTargetResizeMode.MatchPresentation, 0, 0, format, depthFormat);

    /// <summary>
    /// Creates a target that tracks the renderer's logical screen size.
    /// </summary>
    public static RenderTargetSpec MatchScreen(
        SurfaceFormat format = SurfaceFormat.Color,
        DepthFormat depthFormat = DepthFormat.None)
        => new(RenderTargetResizeMode.MatchScreen, 0, 0, format, depthFormat);
}
