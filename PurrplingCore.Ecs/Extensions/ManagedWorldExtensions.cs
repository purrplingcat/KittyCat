using Friflo.Engine.ECS.Systems;
using Microsoft.Extensions.DependencyInjection;
using PurrplingCore.Ecs.Systems;

namespace PurrplingCore.Ecs.Extensions;

/// <summary>
/// Provides helpers for configuring and accessing systems in a world.
/// </summary>
public static class WorldExtensions
{
    /// <summary>
    /// Adds a system to the world unless an instance of its type is already present.
    /// </summary>
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

    /// <summary>
    /// Replaces the systems configured on a world before it is initialized.
    /// </summary>
    public static void SetSystems(this World world, IEnumerable<BaseSystem> systems)
    {
        lock (world)
        {
            world.SetSystems(systems);
        }
    }

    /// <summary>
    /// Gets a system of the specified type or throws if none is found.
    /// </summary>
    public static T GetSystem<T>(this World world, bool recursive = true) where T : BaseSystem
    {
        return world.FindSystem<T>(recursive) 
            ?? throw new InvalidOperationException($"System of type {typeof(T).Name} not found in the world.");
    }

    /// <summary>
    /// Gets a system group by name or throws if none is found.
    /// </summary>
    public static SystemGroup GetGroup(this World world, string name, bool recursive = true)
    {
        return world.FindGroup(name, recursive) 
            ?? throw new InvalidOperationException($"System group with name '{name}' not found in the world.");
    }
}
