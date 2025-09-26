

using System.Diagnostics;

namespace PurrplingCore.Toolkit.Animations;

[DebuggerDisplay("{Index} {Duration}")]
public readonly struct AnimationFrame(int index, TimeSpan duration)
{
    /// <summary>
    /// Gets the index of the frame in the overall sprite sheet.
    /// </summary>
    public int FrameIndex => index;

    /// <summary>
    /// Gets the total duration this frame should be displayed during an animation.
    /// </summary>
    public TimeSpan Duration => duration;

}
