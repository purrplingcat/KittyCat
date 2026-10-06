using Friflo.Engine.ECS;
using PurrplingCore.Toolkit.Messaging;

namespace PurrplingCore.Ecs.Systems;

/// <summary>
/// Base for systems that process every store attached to their system root.
/// </summary>
public abstract class GlobalSystem(World world, IMessageBus? bus = null) : ManagedSystem(world, bus)
{
    private readonly List<EntityStore> _stores = [];

    /// <summary>
    /// Tracks a store added to the system root and prepares its command buffer.
    /// </summary>
    protected override void OnAddStore(EntityStore store)
    {
        GetBufferFor(store);
        _stores.Add(store);

        base.OnAddStore(store);
    }

    /// <summary>
    /// Removes a store from the system and returns its command buffer.
    /// </summary>
    protected override void OnRemoveStore(EntityStore store)
    {
        ReturnBufferFor(store);
        _stores.Remove(store);

        base.OnRemoveStore(store);
    }

    /// <summary>
    /// Updates the system once for each store attached to its root.
    /// </summary>
    protected override void OnUpdateGroup()
    {
        for (int i = 0; i < _stores.Count; i++)
        {
            UpdateContext(_stores[i]);
            ProcessMessages();
            OnUpdate();

            ContextBuffer.Playback();
        }
    }

    /// <summary>
    /// Processes the current store after its context and pending messages are prepared.
    /// </summary>
    protected abstract void OnUpdate();
}

