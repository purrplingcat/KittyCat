using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using KittyCat.Extensions;
using Microsoft.Xna.Framework;
using PurrplingCore.Toolkit;
using PurrplingCore.Toolkit.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace KittyCat.Ecs;

/// <summary>
/// Represents the main ECS world, managing entities, systems, and update logic.
/// </summary>
public class World
{
    private readonly object _lock = new();
    private EntityStore _store;
    private SystemRoot _systems;
    private UpdateContext _currentContext = Ecs.UpdateContext.None;

    /// <summary>
    /// Gets the entity store for this world.
    /// </summary>
    public EntityStore Store => _store;

    /// <summary>
    /// Gets the root system group for this world.
    /// </summary>
    public SystemGroup Systems => _systems;

    /// <summary>
    /// Gets the current update context.
    /// </summary>
    public UpdateContext CurrentContext => _currentContext;

    public event Action<UpdateContext>? ContextChanged;
    public event Action? UpdateBegin;
    public event Action? UpdateEnd;

    /// <summary>
    /// Initializes a new instance of the <see cref="World"/> class.
    /// </summary>
    public World()
    {
        _store = CreateStore();
        _systems = new SystemRoot(_store);
    }

    /// <summary>
    /// Updates the world, including all systems and entity scripts.
    /// </summary>
    /// <param name="time">The current game time.</param>
    /// <param name="context">The update context (e.g., Active, Paused).</param>
    public virtual void Update(GameTime time, UpdateContext context)
    {
        lock (_lock)
        {
            // Context switch
            UpdateContext(context);

            // Pre-update
            _store.EventRecorder.ClearEvents();
            UpdateBegin?.Invoke();

            // Do main update
            _systems.Update(time.ToUpdateTick());
            if (_currentContext == Ecs.UpdateContext.Active)
            {
                UpdateEntityScripts();
            }

            // Post-update
            UpdateEnd?.Invoke();
        }
    }


    private void UpdateEntityScripts()
    {
        var scripts = _store.EntityScripts;
        for (int i = 0; i < scripts.Length; i++)
        {
            foreach (var script in scripts[i])
            {
                script.Update();
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void UpdateContext(UpdateContext context)
    {
        if (context != _currentContext)
        {
            _currentContext = context;
            ContextChanged?.Invoke(_currentContext);
        }
    }

    public void SetSystemRoot(SystemRoot systemRoot)
    {
        lock (_lock)
        {
            systemRoot.AddStore(_store);
            _systems.RemoveStore(_store);
            _systems = systemRoot;
        }
    }

    public static EntityStore CreateStore(string name = "")
    {
        var store = new EntityStore(PidType.RandomPids);

        store.EventRecorder.Enabled = true;
        store.SetStoreRoot(
            store.CreateEntity(new EntityName(name))
        );

        return store;
    }

    public void Clear()
    {
        lock (_lock)
        {
            _currentContext = Ecs.UpdateContext.None;
            _systems.RemoveAllStores();
            _store = CreateStore();
            _systems.AddStore(_store);
        }
    }
}

/// <summary>
/// Represents the context in which the world update is performed.
/// </summary>
public enum UpdateContext
{
    None,
    Active,
    Inactive,
}
