using Friflo.Engine.ECS.Systems;
using Microsoft.Extensions.DependencyInjection;
using PurrplingCore.Ecs.Systems;

namespace PurrplingCore.Ecs.Extensions;

public static class WorldExtensions
{
    public static void AddSystem(this World world, BaseSystem system)
    {
        var type = system.GetType();

        lock (world)
        {
            foreach (var sys in world.SystemRoot)
            {
                if (ReferenceEquals(sys, system))
                    return;

                if (sys.GetType() == type)
                {
                    throw new InvalidOperationException($"System of type {type.Name} already exists in the world.");
                }
            }

            world.SystemRoot.Add(system);
        }
    }

    public static void SetSystems(this World world, IEnumerable<BaseSystem> systems)
    {
        lock (world)
        {
            world.SetSystems(systems);
        }
    }

    public static T GetSystem<T>(this World world, bool recursive = true) where T : BaseSystem
    {
        return world.FindSystem<T>(recursive) 
            ?? throw new InvalidOperationException($"System of type {typeof(T).Name} not found in the world.");
    }

    public static SystemGroup GetGroup(this World world, string name, bool recursive = true)
    {
        return world.FindGroup(name, recursive) 
            ?? throw new InvalidOperationException($"System group with name '{name}' not found in the world.");
    }
}
