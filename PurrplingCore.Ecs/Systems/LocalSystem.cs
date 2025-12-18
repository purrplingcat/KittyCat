using PurrplingCore.Toolkit.Messaging;

namespace PurrplingCore.Ecs.Systems;

public abstract class LocalSystem(World world, IMessageBus? bus = null) : ManagedSystem(world, bus)
{
    protected override void OnUpdateGroup()
    {
        if (World.HasCurrentStore)
        {
            var store = World.CurrentStore;
            
            UpdateContext(store);
            ProcessMessages();
            OnUpdate();

            ContextBuffer.Playback();
        }
    }

    protected abstract void OnUpdate();
}

