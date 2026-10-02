using Microsoft.Xna.Framework;

namespace PurrplingCore.Toolkit.Rendering;

/// <summary>
/// Supplies the view transform and visible region for layers drawn in world space.
/// </summary>
/// <remarks>
/// Both members take a parallax factor so several layers can share one camera while scrolling at
/// different rates. A factor of <see cref="Vector2.One"/> follows the camera exactly, while
/// <see cref="Vector2.Zero"/> stays fixed on screen.
/// </remarks>
public interface ICamera2D
{
    /// <summary>
    /// Returns the transform passed to the batcher for a layer with this parallax.
    /// </summary>
    Matrix GetViewMatrix(Vector2 parallax);

    /// <summary>
    /// Returns the world-space region visible to a layer with this parallax.
    /// </summary>
    Rectangle GetVisibleBounds(Vector2 parallax);
}

/// <summary>
/// A camera whose viewport can be synchronized to its layer's render target.
/// </summary>
public interface IViewportCamera2D
{
    /// <summary>
    /// Sets the screen-space viewport dimensions.
    /// </summary>
    void SetViewport(int width, int height);

    /// <summary>
    /// Centers the camera origin within the current viewport.
    /// </summary>
    void CenterOrigin();
}
