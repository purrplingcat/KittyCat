using Friflo.Engine.ECS;
using Microsoft.Extensions.DependencyInjection;
using PurrplingCore.Toolkit.DI;
using System;
using System.Runtime.CompilerServices;

namespace KittyCat.Ecs;

public abstract class WorldExtension<TExtension> : IWorldExtension<TExtension> where TExtension : class
{
    private readonly ConditionalWeakTable<EntityStore, TExtension> _worldMap = [];
    private readonly World _world;

    public WorldExtension(World world)
    {
        _world = world;
        world.StoreAdded += OnStoreAdded;
        world.StoreRemoved += OnStoreRemoved;
    }

    private void OnStoreAdded(string name, EntityStore store) => GetFor(store);
    private void OnStoreRemoved(string name, EntityStore store) => Destroy(store);

    public TExtension GetFor(EntityStore store)
    {
        return _worldMap.GetValue(store, Create);
    }

    public TExtension GetFor(string storeName)
    {
        var store = _world.GetStore(storeName);
        return GetFor(store);
    }

    public void Destroy(EntityStore store)
    {
        if (_worldMap.TryGetValue(store, out TExtension? extension))
        {
            if (extension is IDisposable disposable)
            {
                disposable.Dispose();
            }
            _worldMap.Remove(store);
        }
    }

    protected abstract TExtension Create(EntityStore store);
}

public sealed class WorldExtensionAttribute<TExtension> : AliasAttribute where TExtension : class
{
    public WorldExtensionAttribute() : base(typeof(IWorldExtension<TExtension>))
    {
    }
}
