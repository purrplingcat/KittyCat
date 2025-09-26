using Friflo.Engine.ECS.Systems;
using System;
using System.Collections.Generic;

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
}
