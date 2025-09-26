using Friflo.Engine.ECS.Systems;
using PurrplingCore.Toolkit.Extensions;
using System;
using System.Collections.Generic;

namespace KittyCat.Extensions;

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

    public static void RemoveAllStores(this SystemRoot systemRoot)
    {
        ArgumentNullException.ThrowIfNull(systemRoot, nameof(systemRoot));

        foreach (var store in systemRoot.Stores)
        {
            systemRoot.RemoveStore(store);
        }
    }
}
