namespace PurrplingCore.Ecs;

public sealed class WorldTag(string debugName)
{
    public string DebugName { get; } = debugName;

    public static readonly WorldTag Default = new("Default");
}
