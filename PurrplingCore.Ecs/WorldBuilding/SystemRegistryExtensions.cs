using System.Reflection;
using Friflo.Engine.ECS.Systems;
using PurrplingCore.Ecs;
using PurrplingCore.Ecs.Systems;

namespace PurrplingCore.Ecs.WorldBuilding;

/// <summary>
/// Provides helpers for registering systems in a world blueprint.
/// </summary>
public static class SystemRegistryExtensions
{
    /// <summary>
    /// Adds all concrete systems from an assembly using their declared system metadata.
    /// </summary>
    public static IWorldBlueprint AddSystems(this IWorldBlueprint world, Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(assembly);

        foreach (var typeInfo in assembly.DefinedTypes)
        {
            if (typeInfo.IsAbstract || typeInfo.ContainsGenericParameters || !typeof(BaseSystem).IsAssignableFrom(typeInfo))
                continue;

            world.AddSystem(typeInfo.AsType());
        }

        return world;
    }

    /// <summary>
    /// Adds each concrete system in the sequence using its declared metadata.
    /// </summary>
    public static IWorldBlueprint AddSystems(this IWorldBlueprint world, IEnumerable<Type> systemTypes)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(systemTypes);

        foreach (var type in systemTypes)
        {
            if (!typeof(BaseSystem).IsAssignableFrom(type))
                continue;

            world.AddSystem(type);
        }

        return world;
    }

    /// <summary>
    /// Gets the manual composition set for a Friflo system group.
    /// Systems added to the returned set are placed directly in that group.
    /// </summary>
    public static SortedSystemSet In<TGroup>(this SystemRegistry systems) where TGroup : SystemGroup
    {
        ArgumentNullException.ThrowIfNull(systems);
        return systems.GetOrCreate(typeof(TGroup));
    }

    /// <summary>
    /// Registers a system of type <typeparamref name="TSystem"/> in the group declared by its metadata.
    /// </summary>
    public static IWorldBlueprint AddSystem<TSystem>(this IWorldBlueprint world) where TSystem : BaseSystem
    {
        var metadata = SystemMetadata.For<TSystem>();
        world.Systems.GetOrCreate(metadata.GroupType).Add(metadata.ToSystemEntry());
        return world;
    }

    /// <summary>
    /// Registers a system using the supplied metadata.
    /// </summary>
    public static IWorldBlueprint AddSystem<TSystem>(this IWorldBlueprint world, SystemMetadata systemMetadata) where TSystem : BaseSystem
    {
        world.Systems.GetOrCreate(systemMetadata.GroupType).Add(systemMetadata.ToSystemEntry());
        return world;
    }

    /// <summary>
    /// Registers a system group as a child of the system root.
    /// </summary>
    public static IWorldBlueprint AddRootGroup<TSystemGroup>(this IWorldBlueprint world) where TSystemGroup : SystemGroup
    {
        var metadata = SystemMetadata.For<TSystemGroup>();
        world.Systems.GetOrCreate(typeof(SystemRoot)).Add(metadata.ToSystemEntry());
        return world;
    }

    /// <summary>
    /// Registers the specified system type in its metadata-defined system group.
    /// </summary>
    public static IWorldBlueprint AddSystem(this IWorldBlueprint world, Type systemType)
    {
        var metadata = SystemMetadata.For(systemType);
        world.Systems.GetOrCreate(metadata.GroupType).Add(metadata.ToSystemEntry());
        return world;
    }
}