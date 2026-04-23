namespace PurrplingCore.Ecs;

internal class WorldOptions
{
    public HashSet<WorldTag> KnownWorlds { get; } = [];
    public bool AllowUnknownWorlds { get; set; }
}
