using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;

namespace PurrplingCore.Ecs;

public interface IWorldContext
{
    string Name { get; }
    EntityStore Store { get; }
    WorldTag Tag { get; }
    UpdateTick Time { get; }

    IWorld GetActualWorld();
}

internal class WorldContext : IWorldContext
{
    private ManagedWorld? _world;

    public ManagedWorld World
    {
        get => _world ?? throw new InvalidOperationException("WorldContext is not initialized with a world.");
        internal set => _world = value;
    }

    public UpdateTick Time => World.Time;
    public WorldTag Tag => World.Tag;

    public string Name => World.Name;

    public EntityStore Store => World.Store;

    public IWorld GetActualWorld() => World;
}
