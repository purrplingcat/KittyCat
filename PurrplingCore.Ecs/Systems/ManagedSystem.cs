using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using PurrplingCore.Ecs.Queries;
using PurrplingCore.Toolkit.Messaging;
using System.Runtime.CompilerServices;

namespace PurrplingCore.Ecs.Systems;

/// <summary>
/// Base for systems that maintain per-store command buffers, queries, and message subscriptions.
/// </summary>
public abstract class ManagedSystem : BaseSystem, IDisposable
{
    private readonly List<IAutoQuery> _autoQueries = [];
    private readonly Dictionary<EntityStore, CommandBuffer> _commandBuffers = [];
    private readonly List<IMessageReplayer> _replayers = [];
    private readonly BufferedSubscriber? _subscriber;
    private bool _disposed;

    /// <summary>
    /// Gets the entity store currently being processed.
    /// </summary>
    protected EntityStore ContextStore { get; private set; } = null!;

    /// <summary>
    /// Gets the command buffer for the current store.
    /// </summary>
    protected CommandBuffer ContextBuffer { get; private set; } = null!;

    /// <summary>
    /// Gets the world that owns this system.
    /// </summary>
    protected World World { get; }

    /// <summary>
    /// Initializes the system with its owning world and an optional message bus.
    /// </summary>
    protected ManagedSystem(World world, IMessageBus? bus = null)
    {
        World = world;

        if (bus != null)
        {
            _subscriber = new BufferedSubscriber(bus);
        }
    }

    /// <summary>
    /// Gets or creates a reusable command buffer for the specified store.
    /// </summary>
    protected CommandBuffer GetBufferFor(EntityStore store)
    {
        if (!_commandBuffers.TryGetValue(store, out var buffer))
        {
            buffer = store.GetCommandBuffer();
            buffer.ReuseBuffer = true;
            _commandBuffers.Add(store, buffer);
        }

        return buffer;
    }

    /// <summary>
    /// Returns and removes the cached command buffer for the specified store.
    /// </summary>
    protected void ReturnBufferFor(EntityStore store)
    {
        if (_commandBuffers.TryGetValue(store, out var buffer))
        {
            buffer.ReturnBuffer();
            _commandBuffers.Remove(store);
        }
    }

    /// <summary>
    /// Sets the current store and updates registered automatic queries for it.
    /// </summary>
    protected void UpdateContext(EntityStore store)
    {
        if (store != ContextStore && store != null)
        {
            ContextBuffer = GetBufferFor(store);
            ContextStore = store;

            foreach (var autoQuery in _autoQueries)
            {
                autoQuery.UpdateStore(ContextStore);
            }
        }
    }

    /// <summary>
    /// Subscribes to messages and buffers them for replay during system updates.
    /// </summary>
    protected ISubscription Subscribe<T>(Action<T> handler) where T : notnull
    {
        if (_subscriber != null)
        {
            var replayer = new MessageReplayer<T>(_subscriber, handler);
            _replayers.Add(replayer);
            return replayer.Subscription;
        }

        return new VoidSubscription();
    }

    /// <summary>
    /// Replays collected messages to this system's active subscribers.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void ProcessMessages()
    {
        foreach (var replayer in _replayers)
        {
            replayer.Replay();
        }
    }

    /// <summary>
    /// Clears replayed messages and removes completed subscriptions.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void ClearMessages()
    {
        for (int i = _replayers.Count - 1; i >= 0; i--)
        {
            _replayers[i].Clear();

            if (!_replayers[i].IsCollecting)
            {
                _replayers.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Processes buffered messages before the update group begins.
    /// </summary>
    protected override void OnUpdateGroupBegin()
    {
        _subscriber?.Process();
        base.OnUpdateGroupBegin();
    }

    /// <summary>
    /// Clears buffered messages after the update group ends.
    /// </summary>
    protected override void OnUpdateGroupEnd()
    {
        ClearMessages();
        base.OnUpdateGroupEnd();
    }

    private void OnWorldCleared()
    {
        _autoQueries.ForEach(q => q.Cleanup());
        foreach (var buf in _commandBuffers.Values)
        {
            buf.ReturnBuffer();
        }
        _commandBuffers.Clear();
    }

    /// <summary>
    /// Releases managed resources when disposing the system.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                ContextBuffer = null!;
                ContextStore = null!;

                foreach (var buffer in _commandBuffers.Values)
                {
                    buffer.ReturnBuffer();
                }

                _commandBuffers.Clear();
                _autoQueries.Clear();
                _replayers.Clear();
                _subscriber?.Dispose();
            }

            _disposed = true;
        }
    }

    /// <summary>
    /// Releases the system's cached buffers, queries, and message subscriptions.
    /// </summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    #region Auto query registration

    /// <summary>
    /// Creates and registers an automatic query for one component type.
    /// </summary>
    protected AutoQuery<T1> CreateQuery<T1>()
        where T1 : struct, IComponent
    {
        var q = new AutoQuery<T1>();
        _autoQueries.Add(q);
        return q;
    }

    /// <summary>
    /// Creates and registers an automatic query for two component types.
    /// </summary>
    protected AutoQuery<T1, T2> CreateQuery<T1, T2>()
        where T1 : struct, IComponent
        where T2 : struct, IComponent
    {
        var q = new AutoQuery<T1, T2>();
        _autoQueries.Add(q);
        return q;
    }

    /// <summary>
    /// Creates and registers an automatic query for three component types.
    /// </summary>
    protected AutoQuery<T1, T2, T3> CreateQuery<T1, T2, T3>()
        where T1 : struct, IComponent
        where T2 : struct, IComponent
        where T3 : struct, IComponent
    {
        var q = new AutoQuery<T1, T2, T3>();
        _autoQueries.Add(q);
        return q;
    }

    /// <summary>
    /// Creates and registers an automatic query for four component types.
    /// </summary>
    protected AutoQuery<T1, T2, T3, T4> CreateQuery<T1, T2, T3, T4>()
        where T1 : struct, IComponent
        where T2 : struct, IComponent
        where T3 : struct, IComponent
        where T4 : struct, IComponent
    {
        var q = new AutoQuery<T1, T2, T3, T4>();
        _autoQueries.Add(q);
        return q;
    }

    /// <summary>
    /// Creates and registers an automatic query for five component types.
    /// </summary>
    protected AutoQuery<T1, T2, T3, T4, T5> CreateQuery<T1, T2, T3, T4, T5>()
        where T1 : struct, IComponent
        where T2 : struct, IComponent
        where T3 : struct, IComponent
        where T4 : struct, IComponent
        where T5 : struct, IComponent
    {
        var q = new AutoQuery<T1, T2, T3, T4, T5>();
        _autoQueries.Add(q);
        return q;
    }
    #endregion
}

