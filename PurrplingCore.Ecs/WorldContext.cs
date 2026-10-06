using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;

namespace PurrplingCore.Ecs;

/// <summary>
/// Exposes the current world and its update state to scoped services.
/// </summary>
public interface IWorldContext
{
    /// <summary>
    /// Gets the current world's display name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the entity store owned by the current world.
    /// </summary>
    EntityStore Store { get; }

    /// <summary>
    /// Gets the signature that identifies the current world.
    /// </summary>
    WorldSignature Signature { get; }

    /// <summary>
    /// Gets the current update tick.
    /// </summary>
    UpdateTick Time { get; }

    /// <summary>
    /// Gets the current world as an <see cref="IWorld"/> instance.
    /// </summary>
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
    public WorldSignature Signature => World.Signature;

    public string Name => World.Name;

    public EntityStore Store => World.Store;

    public IWorld GetActualWorld() => World;
}
