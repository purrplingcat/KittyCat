using Friflo.Engine.ECS;
using PurrplingCore.Toolkit.DI;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace KittyCat.Core.Services;

[Singleton]
public class PhysicsManager
{
    private readonly ConditionalWeakTable<EntityStore, PhysicsWorld> _worldMap = [];

    /// <summary>
    /// Vytvoří nový fyzikální svět pro daný EntityStore, pokud ještě neexistuje.
    /// </summary>
    public PhysicsWorld GetOrCreateWorldFor(EntityStore store)
    {
        return _worldMap.GetValue(store, _ => new PhysicsWorld());
    }

    /// <summary>
    /// Vrátí fyzikální svět pro daný EntityStore.
    /// </summary>
    public PhysicsWorld GetWorldFor(EntityStore store)
    {
        if (!_worldMap.TryGetValue(store, out var physicsWorld))
        {
            throw new KeyNotFoundException("No PhysicsWorld is registered for the given EntityStore.");
        }
        return physicsWorld;
    }

    /// <summary>
    /// Uklidí po sobě. Tato metoda je nyní technicky volitelná,
    /// protože se tabulka uklidí sama, ale je dobré ji mít pro explicitní kontrolu.
    /// </summary>
    public void RemoveWorldFor(EntityStore store)
    {
        _worldMap.Remove(store);
    }
}

// TODO: Temporary placeholder for PhysicsWorld class
public class PhysicsWorld
{
    internal void Step(float deltaTime)
    {
        throw new NotImplementedException();
    }
}
