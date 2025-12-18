using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using PurrplingCore.Ecs.Queries;
using PurrplingCore.Toolkit.Messaging;
using System.Runtime.CompilerServices;

namespace PurrplingCore.Ecs.Systems;

public abstract class ManagedSystem : BaseSystem, IDisposable
{
    private readonly List<IAutoQuery> _autoQueries = [];
    private readonly Dictionary<EntityStore, CommandBuffer> _commandBuffers = [];
    private readonly List<IMessageReplayer> _replayers = [];
    private readonly BufferedSubscriber? _subscriber;
    private bool _disposed;

    protected EntityStore ContextStore { get; private set; } = null!;
    protected CommandBuffer ContextBuffer { get; private set; } = null!;
    protected World World { get; }

    protected ManagedSystem(World world, IMessageBus? bus = null)
    {
        World = world;
        World.WorldCleared += OnWorldCleared;

        if (bus != null)
        {
            _subscriber = new BufferedSubscriber(bus);
        }
    }

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

    protected void ReturnBufferFor(EntityStore store)
    {
        if (_commandBuffers.TryGetValue(store, out var buffer))
        {
            buffer.ReturnBuffer();
            _commandBuffers.Remove(store);
        }
    }

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void ProcessMessages()
    {
        foreach (var replayer in _replayers)
        {
            replayer.Replay();
        }
    }

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

    protected override void OnUpdateGroupBegin()
    {
        _subscriber?.Process();
        base.OnUpdateGroupBegin();
    }

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

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                ContextBuffer = null!;
                ContextStore = null!;
                World.WorldCleared -= OnWorldCleared;

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

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    #region Auto query registration

    protected AutoQuery<T1> CreateQuery<T1>()
        where T1 : struct, IComponent
    {
        var q = new AutoQuery<T1>();
        _autoQueries.Add(q);
        return q;
    }

    protected AutoQuery<T1, T2> CreateQuery<T1, T2>()
        where T1 : struct, IComponent
        where T2 : struct, IComponent
    {
        var q = new AutoQuery<T1, T2>();
        _autoQueries.Add(q);
        return q;
    }

    protected AutoQuery<T1, T2, T3> CreateQuery<T1, T2, T3>()
        where T1 : struct, IComponent
        where T2 : struct, IComponent
        where T3 : struct, IComponent
    {
        var q = new AutoQuery<T1, T2, T3>();
        _autoQueries.Add(q);
        return q;
    }

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

