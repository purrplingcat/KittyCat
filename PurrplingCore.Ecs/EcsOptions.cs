using PurrplingCore.Ecs.DI;
using System.Reflection;

namespace PurrplingCore.Ecs;

public class EcsOptions
{
    public HashSet<Assembly> Assemblies { get; } = [];
    public Dictionary<WorldType, WorldInitOptions> WorldInitOptions { get; } = [];

    public WorldInitOptions GetWorldInitOptions(WorldType worldType)
    {
        if (!WorldInitOptions.TryGetValue(worldType, out var options))
        {
            options = new WorldInitOptions();
            WorldInitOptions[worldType] = options;
        }

        return options;
    }
}
