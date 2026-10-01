namespace PurrplingCore.Rendering;

/// <summary>
/// Common draw-order anchors for MonoGame components.
/// </summary>
/// <remarks>
/// Lower values draw first. These constants are suggested anchors, not required stages;
/// games can use their own values in the gaps or outside this range. They order components,
/// not renderers or commands.
/// </remarks>
public static class DrawStage
{
    /// <summary>
    /// MonoGame's default component draw order, used when no explicit ordering is needed.
    /// </summary>
    public const int Default = 0;

    /// <summary>
    /// Draw order for backgrounds, skies, and parallax backdrops, before main world content.
    /// </summary>
    public const int Background = 500;

    /// <summary>Draw order for the main camera-rendered world content.</summary>
    public const int World = 1000;

    /// <summary>Draw order for world-space overlays, after the main world content.</summary>
    public const int WorldOverlay = 2000;

    /// <summary>Draw order for screen-space content, such as HUD, menus, subtitles, and cursor.</summary>
    public const int Screen = 3000;

    /// <summary>Draw order for debug diagnostics above normal screen-space content.</summary>
    public const int Debug = 4000;

    /// <summary>Maximum draw order, reserved for final composition and presentation.</summary>
    public const int Final = int.MaxValue;
}
