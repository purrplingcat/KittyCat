namespace PurrplingCore.Rendering;

/// <summary>
/// Conventional anchors for ordering draw components within the draw loop.
/// </summary>
/// <remarks>
/// These are plain constants rather than an enum so a game can define its own stages and
/// interleave them with the built-in ones. The gaps of 500 exist so custom stages fit between
/// anchors without renumbering. A group driver must order after everything that submits to
/// its renderers, which is why <see cref="Submit"/> sits at zero alongside their producers.
/// </remarks>
public static class RenderStage
{
    /// <summary>Components that queue commands, such as the world controller and scene manager.</summary>
    public const int Submit = 0;

    /// <summary>Skies, parallax backdrops, and anything behind the playable content.</summary>
    public const int Background = 500;

    /// <summary>Main world content drawn through a camera.</summary>
    public const int World = 1000;

    /// <summary>World-space overlays such as health bars and damage numbers.</summary>
    public const int WorldOverlay = 2000;

    /// <summary>Screen-space content: heads-up display, menus, subtitles, cursor.</summary>
    public const int Screen = 3000;

    /// <summary>Diagnostics drawn above everything else.</summary>
    public const int Debug = 4000;

    /// <summary>Global composition and presentation.</summary>
    public const int Final = int.MaxValue;
}
