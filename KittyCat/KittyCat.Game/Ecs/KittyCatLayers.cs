using PurrplingCore.Toolkit.Rendering;

namespace KittyCat.Ecs;

public enum KittyCatRendererKey
{
    World,
    Screen,
}

/// <summary>Layers declared by the game, grouped by the renderer that owns them.</summary>
public static class KittyCatLayers
{
    public static readonly RenderLayerId Entities = RenderLayerId.Of("World.Entities");
    public static readonly RenderLayerId Effects = RenderLayerId.Of("World.Effects");
    public static readonly RenderLayerId Hud = RenderLayerId.Of("Screen.Hud");
    public static readonly RenderLayerId Modal = RenderLayerId.Of("Screen.Modal");
}
