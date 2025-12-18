using Friflo.Engine.ECS;
using PurrplingCore.Toolkit.Messaging;

namespace PurrplingCore.Ecs.Systems;

public abstract class GlobalSystem(World world, IMessageBus? bus = null) : ManagedSystem(world, bus)
{
    private readonly List<EntityStore> _stores = [];

    protected override void OnAddStore(EntityStore store)
    {
        GetBufferFor(store);
        _stores.Add(store);

        base.OnAddStore(store);
    }

    protected override void OnRemoveStore(EntityStore store)
    {
        ReturnBufferFor(store);
        _stores.Remove(store);

        base.OnRemoveStore(store);
    }

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

    protected abstract void OnUpdate();
}

