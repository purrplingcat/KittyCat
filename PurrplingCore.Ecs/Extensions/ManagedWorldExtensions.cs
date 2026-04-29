using Friflo.Engine.ECS.Systems;
using Microsoft.Extensions.DependencyInjection;

namespace PurrplingCore.Ecs.Extensions;

public static class WorldExtensions
{
    public static void AddTopLevelSystem(this World world, BaseSystem system)
    {
        if (!world.SystemRoot.ChildSystems.Contains(system))
        {
            world.SystemRoot.Add(system);
        }
    }

    public static T GetSystem<T>(this World world, bool recursive = true) where T : BaseSystem
    {
        return world.FindSystem<T>(recursive) 
            ?? throw new InvalidOperationException($"System of type {typeof(T).Name} not found in the world.");
    }
}
