using Friflo.Engine.ECS;
using PurrplingCore.Ecs.Components;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PurrplingCore.Ecs;

public static class EntityStoreExtensions
{
    internal static void AssignWorld(this EntityStore store, World world)
    {
        if (store.StoreRoot.IsNull)
        {
            throw new InvalidOperationException("EntityStore's StoreRoot is not initialized.");
        }

        store.StoreRoot.AddComponent(new EntityWorld(world));
    }

    public static World GetWorld(this EntityStore store)
    {
        if (store.StoreRoot.TryGetComponent<EntityWorld>(out var worldComp))
        {
            return worldComp.world;
        }
        throw new InvalidOperationException("EntityStore is not assigned to any World.");
    }

    public static bool TryGetWorld(this EntityStore store, [NotNullWhen(true)] out World? world)
    {
        if (store.StoreRoot.TryGetComponent<EntityWorld>(out var worldComp))
        {
            world = worldComp.world;
            return true;
        }

        world = null;
        return false;
    }

    public static bool IsCurrentStore(this EntityStore store)
    {
        if (store.TryGetWorld(out var world))
        {
            return world.HasCurrentStore && world.CurrentStore == store;
        }
        return false;
    }

    public static bool HasWorld(this EntityStore store)
    {
        return store.StoreRoot.HasComponent<EntityWorld>();
    }
}
