using Friflo.Engine.ECS.Systems;
using Microsoft.Extensions.DependencyInjection;

namespace PurrplingCore.Ecs.Extensions;

public static class ManagedWorldExtensions
{
    public static void Bootstrap(this ManagedWorld world, object? key = null)
    {
        var bootstraps = key == null
            ? world.Services.GetServices<IWorldBootstrap>()
            : world.Services.GetKeyedServices<IWorldBootstrap>(key);

        try
        {
            world.creating = true;
            foreach (var bootstrap in bootstraps.OrderBy(bs => bs.Order))
            {
                bootstrap.Setup(world);
            }
        }
        finally
        {
            world.creating = false;
        }
    }
}

public static class WorldExtensions
{
    public static void AddTopLevelSystem(this World world, BaseSystem system)
    {
        world.SystemRoot.Add(system);
    }

    public static T GetSystem<T>(this World world, bool recursive = true) where T : BaseSystem
    {
        return world.FindSystem<T>(recursive) 
            ?? throw new InvalidOperationException($"System of type {typeof(T).Name} not found in the world.");
    }
}
