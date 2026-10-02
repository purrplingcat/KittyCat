namespace PurrplingCore.Toolkit.Rendering;

/// <summary>
/// Determines how an offscreen image is placed onto a target of a different size.
/// </summary>
public enum RenderScaleMode : byte
{
    /// <summary>
    /// Scales uniformly to fit inside the target and centres it, leaving bars on the free sides.
    /// </summary>
    Fit = 0,

    /// <summary>
    /// Like <see cref="Fit"/>, but only by whole multiples so pixel art stays crisp.
    /// </summary>
    IntegerFit,

    /// <summary>
    /// Scales uniformly to cover the whole target and centres it, cropping the overflow.
    /// </summary>
    Fill,

    /// <summary>
    /// Scales each axis independently to cover the target exactly, distorting the aspect ratio.
    /// </summary>
    Stretch,

    /// <summary>
    /// Keeps the native size
    /// </summary>
    Native,
}
