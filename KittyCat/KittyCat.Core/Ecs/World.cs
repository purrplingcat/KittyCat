using DotTiled;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using KittyCat.Ecs.Components;
using KittyCat.Extensions;
using KittyCat.Services;
using Microsoft.Xna.Framework;
using PurrplingCore.Toolkit.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace KittyCat.Ecs;

/// <summary>
/// Represents the main ECS world, managing entities, systems, and update logic.
/// </summary>
public class World
{
    public delegate void StoreChangedHandler(string name, EntityStore store);

    private readonly object _lock = new();
    private readonly EntityStores _stores;
    private EntityStore? _currentStore;

    /// <summary>
    /// Gets the entity store for this world.
    /// </summary>
    public EntityStore CurrentStore => _currentStore
        ?? throw new InvalidOperationException("No current entity store is set. Ensure that at least one store is created and set as current.");

    public int StoreCount => _stores.Count;

    public event StoreChangedHandler? StoreAdded;
    public event StoreChangedHandler? StoreRemoved;
    public event StoreChangedHandler? CurrentStoreChanged;
    public event Action? WorldCleared;

    /// <summary>
    /// Initializes a new instance of the <see cref="World"/> class.
    /// </summary>
    public World()
    {
        _stores = new EntityStores(this);
        _currentStore = CreateStore(string.Empty);
    }

    public static EntityStore CreateNewStore(string name = "")
    {
        var store = new EntityStore(PidType.RandomPids);

        store.EventRecorder.Enabled = true;
        store.SetStoreRoot(
            store.CreateEntity(new EntityName(name), new MapContext())
        );

        return store;
    }

    public EntityStore CreateStore(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        lock (_lock)
        {
            if (_stores.Contains(name))
            {
                throw new InvalidOperationException($"An entity store with the name '{name}' already exists.");
            }

            var store = CreateNewStore(name);
            OnCreateStore(store);
            _stores.Add(name, store);

            return store;
        }
    }

    protected virtual void OnCreateStore(EntityStore store)
    {
    }

    private void OnRemoveStore(string name, EntityStore store)
    {
        if (_currentStore == store)
        {
            var defaultStore = _stores.GetDefaultStore();
            
            _currentStore = defaultStore.Value;
            CurrentStoreChanged?.Invoke(defaultStore.Key, defaultStore.Value);
        }

        StoreRemoved?.Invoke(name, store);
    }

    private void OnAddStore(string name, EntityStore store)
    {
        if (_currentStore == null)
        {
            _currentStore = store;
            CurrentStoreChanged?.Invoke(name, store);
        }

        StoreAdded?.Invoke(name, store);
    }

    public void RemoveStore(string name)
    {
        _stores.Remove(name);
    }

    public bool RemoveStore(EntityStore store)
    {
        lock (_lock)
        {
            if (store.StoreRoot.TryGetComponent<EntityName>(out var nameComp) && _stores.Contains(nameComp.value))
            {
                return _stores.Remove(nameComp.value);
            }

            return false;
        }
    }

    public bool ContainsStore(EntityStore store)
    {
        if (store.StoreRoot.TryGetComponent<EntityName>(out var nameComp))
        {
            return _stores.Contains(nameComp.value);
        }
        return false;
    }

    public bool StoreExists(string name) => _stores.Contains(name);
    public EntityStore GetStore(string name) => _stores.Get(name);
    public bool TryGetStore(string name, [MaybeNullWhen(false)] out EntityStore store) => _stores.TryGet(name, out store);
    public IEnumerable<EntityStore> GetAllStores() => _stores.GetAll();

    public bool SwitchToStore(string name)
    {
        lock (_lock)
        {
            if (_stores.TryGet(name, out var store))
            {
                _currentStore = store;
                CurrentStoreChanged?.Invoke(name, store);
                return true;
            }
            return false;
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _currentStore = null;
            _stores.Clear();
            WorldCleared?.Invoke();
        }
    }

    internal class EntityStores
    {
        private readonly Dictionary<string, EntityStore> _stores = [];
        private readonly World _world;

        public int Count => _stores.Count;

        internal EntityStores(World world)
        {
            _world = world;
        }

        public void Add(string name, EntityStore store)
        {
            lock (_world._lock)
            {
                _stores.Add(name, store);
                _world.OnAddStore(name, store);
            }
        }

        public bool Remove(string name)
        {
            lock (_world._lock)
            {
                if (_stores.TryGetValue(name, out var store))
                {
                    bool removed = _stores.Remove(name);
                    if (removed)
                    {
                        _world.OnRemoveStore(name, store);
                    }
                    return removed;
                }
                return false;
            }
        }

        public bool Contains(string name) => _stores.ContainsKey(name);
        public EntityStore Get(string name) => _stores[name];

        public bool TryGet(string name, [MaybeNullWhen(false)] out EntityStore store)
        {
            return _stores.TryGetValue(name, out store);
        }

        public IEnumerable<EntityStore> GetAll() => _stores.Values;

        public void Clear()
        {
            lock (_world._lock)
            {
                var removed = _stores.ToArray();

                _stores.Clear();

                if (_world.StoreRemoved != null)
                {
                    for (int i = 0; i < removed.Length; i++)
                    {
                        _world.StoreRemoved.Invoke(removed[i].Key, removed[i].Value);
                    }
                }
            }
        }

        public KeyValuePair<string, EntityStore> GetDefaultStore()
        {
            return _stores.FirstOrDefault();
        }
    }
}
