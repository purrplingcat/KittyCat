namespace PurrplingCore.Toolkit.Rendering;

/// <summary>
/// Aligns the scaled renderer image within its screen's presentation area.
/// </summary>
public enum RenderAlignment : byte
{
    /// <summary>
    /// Centers the image on both axes.
    /// </summary>
    Center,

    /// <summary>
    /// Aligns the image to the top-left corner.
    /// </summary>
    TopLeft,

    /// <summary>
    /// Centers the image horizontally along the top edge.
    /// </summary>
    TopCenter,

    /// <summary>
    /// Aligns the image to the top-right corner.
    /// </summary>
    TopRight,

    /// <summary>
    /// Centers the image vertically along the left edge.
    /// </summary>
    CenterLeft,

    /// <summary>
    /// Centers the image vertically along the right edge.
    /// </summary>
    CenterRight,

    /// <summary>
    /// Aligns the image to the bottom-left corner.
    /// </summary>
    BottomLeft,

    /// <summary>
    /// Centers the image horizontally along the bottom edge.
    /// </summary>
    BottomCenter,

    /// <summary>
    /// Aligns the image to the bottom-right corner.
    /// </summary>
    BottomRight,
}