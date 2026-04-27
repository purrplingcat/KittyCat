namespace PurrplingCore.Ecs;

public class WorldOptions
{
    public HashSet<WorldTag> KnownWorlds { get; } = [];
    public bool AllowUnknownWorlds { get; set; }
}
