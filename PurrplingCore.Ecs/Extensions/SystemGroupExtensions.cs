using Friflo.Engine.ECS.Systems;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PurrplingCore.Ecs.Systems;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace PurrplingCore.Ecs.Extensions;

/// <summary>
/// Provides helpers for modifying and querying Friflo system groups.
/// </summary>
public static class SystemGroupExtensions
{
    /// <summary>
    /// Inserts a system immediately before the first matching direct child system.
    /// </summary>
    public static void AddBefore<TTarget>(this SystemGroup group, BaseSystem newSystem)
        where TTarget : BaseSystem
    {
        InsertRelative(group, group.FindSystem<TTarget>(recursive: false), newSystem, offset: 0);
    }

    /// <summary>
    /// Inserts a system immediately after the first matching direct child system.
    /// </summary>
    public static void AddAfter<TTarget>(this SystemGroup group, BaseSystem newSystem)
        where TTarget : BaseSystem
    {
        InsertRelative(group, group.FindSystem<TTarget>(recursive: false), newSystem, offset: 1);
    }

    /// <summary>
    /// Inserts a system immediately before a direct child with the specified type.
    /// </summary>
    public static void AddBefore(this SystemGroup group, Type targetType, BaseSystem newSystem)
    {
        InsertRelative(group, group.FindSystem(targetType, recursive: false)!, newSystem, offset: 0);
    }

    /// <summary>
    /// Inserts a system immediately after a direct child with the specified type.
    /// </summary>
    public static void AddAfter(this SystemGroup group, Type targetType, BaseSystem newSystem)
    {
        InsertRelative(group, group.FindSystem(targetType, recursive: false)!, newSystem, offset: 1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void InsertRelative(SystemGroup group, BaseSystem target, BaseSystem newSystem, int offset)
    {
        if (target is null)
        {
            throw new InvalidOperationException("Target system not found in the group.");
        }

        int index = group.ChildSystems.IndexOf(target);
        group.Insert(index + offset, newSystem);
    }

    /// <summary>
    /// Finds the first system assignable to the specified type.
    /// </summary>
    public static BaseSystem? FindSystem(this SystemGroup group, Type systemType, bool recursive = false)
    {
        foreach (var system in group.ChildSystems)
        {
            if (systemType.IsAssignableFrom(system.GetType()))
            {
                return system;
            }
            if (recursive && system is SystemGroup subgroup)
            {
                var found = FindSystem(subgroup, systemType, recursive);
                if (found != null) return found;
            }
        }
        return null;
    }

    /// <summary>
    /// Gets a system of the specified type or throws if none is found.
    /// </summary>
    public static T GetSystem<T>(this SystemGroup group, bool recursive = false) where T : BaseSystem
    {
        return group.FindSystem<T>(recursive)
            ?? throw new InvalidOperationException($"System of type {typeof(T).Name} not found in the group.");
    }

    /// <summary>
    /// Gets a direct child group by name or creates and adds it when absent.
    /// </summary>
    public static SystemGroup GetOrCreateGroup(this SystemGroup parent, string groupName)
    {
        var group = parent.FindGroup(groupName, recursive: false);
        if (group == null)
        {
            group = new SystemGroup(groupName);
            parent.Add(group);
        }
        return group;
    }

    /// <summary>
    /// Gets the world that owns this group.
    /// </summary>
    public static World GetWorld(this SystemGroup group)
    {
        if (group.SystemRoot is WorldSystemRoot worldRoot)
        {
            return worldRoot.World;
        }

        throw new InvalidOperationException($"SystemGroup {group.Name} is not attached to a WorldSystemRoot.");
    }

    /// <summary>
    /// Tries to get the world that owns this group.
    /// </summary>
    public static bool TryGetWorld(this SystemGroup group, [MaybeNullWhen(false)] out World world)
    {
        if (group.SystemRoot is WorldSystemRoot worldRoot)
        {
            world = worldRoot.World;
            return true;
        }

        world = null;
        return false;
    }

    /// <summary>
    /// Adds each system in the sequence to the group.
    /// </summary>
    public static void Add(this SystemGroup group, IEnumerable<BaseSystem> systems)
    {
        ArgumentNullException.ThrowIfNull(group, nameof(group));
        ArgumentNullException.ThrowIfNull(systems, nameof(systems));

        foreach (var system in systems)
        {
            group.Add(system);
        }
    }

    /// <summary>
    /// Adds each system in the array to the group.
    /// </summary>
    public static void Add(this SystemGroup group, params BaseSystem[] systems)
    {
        ArgumentNullException.ThrowIfNull(group, nameof(group));
        ArgumentNullException.ThrowIfNull(systems, nameof(systems));

        foreach (var system in systems)
        {
            group.Add(system);
        }
    }
    /// <summary>
    /// Disables the first matching system found recursively.
    /// </summary>
    public static void Disable<TSystem>(this SystemGroup group) where TSystem : BaseSystem
    {
        var system = group.FindSystem<TSystem>(recursive: true);
        if (system != null)
        {
            system.Enabled = false;
        }
    }

    /// <summary>
    /// Enables the first matching system found recursively.
    /// </summary>
    public static void Enable<TSystem>(this SystemGroup group) where TSystem : BaseSystem
    {
        var system = group.FindSystem<TSystem>(recursive: true);
        if (system != null)
        {
            system.Enabled = true;
        }
    }

    /// <summary>
    /// Determines whether the first matching system found recursively is enabled.
    /// </summary>
    public static bool IsEnabled<TSystem>(this SystemGroup group) where TSystem : BaseSystem
    {
        var system = group.FindSystem<TSystem>(recursive: true);
        return system != null && system.Enabled;
    }

    /// <summary>
    /// Counts the direct child systems and optionally all descendants.
    /// </summary>
    public static int Count(this SystemGroup group, bool recursive = false)
    {
        int count = group.ChildSystems.Count;

        if (recursive)
        {
            for (int i = 0; i < group.ChildSystems.Count; i++)
            {
                if (group.ChildSystems[i] is not SystemGroup subGroup)
                    continue;

                count += Count(subGroup, recursive);
            }
        }

        return count;
    }
}
