namespace PurrplingCore.Toolkit.Graphics;

/// <summary>
/// Describes a logical screen that follows resolution changes and owns its event subscription.
/// </summary>
public sealed class ResolutionScreen : Screen, IDisposable
{
    private readonly Resolution _resolution;

    /// <summary>
    /// Creates a screen that follows the supplied logical resolution.
    /// </summary>
    public ResolutionScreen(Resolution resolution)
        : base(resolution?.Size ?? throw new ArgumentNullException(nameof(resolution)))
    {
        _resolution = resolution;
        resolution.ResolutionChanged += OnResolutionChanged;
    }

    private void OnResolutionChanged() => UpdateSize(_resolution.Size);

    /// <summary>
    /// Stops following resolution changes without disposing the resolution or child regions.
    /// </summary>
    public void Dispose()
    {
        _resolution.ResolutionChanged -= OnResolutionChanged;
        GC.SuppressFinalize(this);
    }
}
