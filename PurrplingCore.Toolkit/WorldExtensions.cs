using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using PurrplingCore.Toolkit.Extensions;
using System;
using System.Linq;

namespace PurrplingCore.Toolkit;

public static class WorldExtensions
{
    public static void AddSystems(this IWorld world, params IEnumerable<BaseSystem>[] systemSources)
    {
        ArgumentNullException.ThrowIfNull(world, nameof(world));
        ArgumentNullException.ThrowIfNull(systemSources, nameof(systemSources));
        
        world.Systems.Add(systemSources
            .SelectMany(systems => systems)
            .Sort());
    }
}
