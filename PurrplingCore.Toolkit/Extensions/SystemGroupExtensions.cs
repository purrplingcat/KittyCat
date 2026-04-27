using Friflo.Engine.ECS.Systems;

namespace PurrplingCore.Toolkit.Extensions;

public static class SystemGroupExtensions
{
    public static void Add(this SystemGroup group, IEnumerable<BaseSystem> systems)
    {
        ArgumentNullException.ThrowIfNull(group, nameof(group));
        ArgumentNullException.ThrowIfNull(systems, nameof(systems));

        foreach (var system in systems)
        {
            group.Add(system);
        }
    }

    public static void Add(this SystemGroup group, params BaseSystem[] systems)
    {
        ArgumentNullException.ThrowIfNull(group, nameof(group));
        ArgumentNullException.ThrowIfNull(systems, nameof(systems));

        foreach (var system in systems)
        {
            group.Add(system);
        }
    }
    public static void Disable<TSystem>(this SystemGroup group) where TSystem : BaseSystem
    {
        var system = group.FindSystem<TSystem>(recursive: true);
        if (system != null)
        {
            system.Enabled = false;
        }
    }

    public static void Enable<TSystem>(this SystemGroup group) where TSystem : BaseSystem
    {
        var system = group.FindSystem<TSystem>(recursive: true);
        if (system != null)
        {
            system.Enabled = true;
        }
    }

    public static bool IsEnabled<TSystem>(this SystemGroup group) where TSystem : BaseSystem
    {
        var system = group.FindSystem<TSystem>(recursive: true);
        return system != null && system.Enabled;
    }

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
