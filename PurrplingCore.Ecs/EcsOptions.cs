using PurrplingCore.Ecs.DI;
using System.Reflection;

namespace PurrplingCore.Ecs;

public class EcsOptions
{
    public HashSet<Assembly> Assemblies { get; } = [];
}
