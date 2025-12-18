using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using System;

namespace PurrplingCore.Ecs;

public sealed class SystemWorldBinding : IDisposable
{
    private readonly World _world;
    private readonly SystemRoot _systemRoot;
    private bool _disposed;

    public SystemWorldBinding(SystemRoot systemRoot, World world)
    {
        _systemRoot = systemRoot ?? throw new ArgumentNullException(nameof(systemRoot));
        _world = world ?? throw new ArgumentNullException(nameof(world));

        world.StoreAdded += OnStoreAdded;
        world.StoreRemoved += OnStoreRemoved;

        foreach (var store in world.GetAllStores())
        {
            _systemRoot.AddStore(store);
        }
    }

    private void OnStoreAdded(string name, EntityStore store) => _systemRoot.AddStore(store);
    private void OnStoreRemoved(string name, EntityStore store) => _systemRoot.RemoveStore(store);

    public void Dispose()
    {
        if (!_disposed)
        {
            _world.StoreAdded -= OnStoreAdded;
            _world.StoreRemoved -= OnStoreRemoved;
            _disposed = true;

            foreach (var store in _world.GetAllStores())
            {
                _systemRoot.RemoveStore(store);
            }
        }
    }
}
