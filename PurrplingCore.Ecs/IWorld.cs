using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;

namespace PurrplingCore.Ecs;

/// <summary>
/// Exposes the entity store, systems, and lifecycle events of a world.
/// </summary>
public interface IWorld
{
    /// <summary>
    /// Gets or sets the world's display name.
    /// </summary>
    string Name { get; set; }

    /// <summary>
    /// Gets the entity store owned by the world.
    /// </summary>
    EntityStore Store { get; }

    /// <summary>
    /// Gets the systems attached to the world.
    /// </summary>
    IReadOnlyCollection<BaseSystem> Systems { get; }

    /// <summary>
    /// Occurs when the world is disposed.
    /// </summary>
    public event EventHandler Disposed;

    /// <summary>
    /// Occurs when the world is initialized.
    /// </summary>
    public event EventHandler Initialized;

    /// <summary>
    /// Occurs after the world's systems have been updated.
    /// </summary>
    public event Action<IWorld, UpdateTick> Updated;

    /// <summary>
    /// Occurs before the world's draw systems run.
    /// </summary>
    public event Action<IWorld, UpdateTick> Drawing;

    /// <summary>
    /// Occurs after the world's systems have been drawn.
    /// </summary>
    public event Action<IWorld, UpdateTick> Drawn;
}
