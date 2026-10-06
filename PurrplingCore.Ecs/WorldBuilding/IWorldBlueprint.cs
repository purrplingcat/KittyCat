namespace PurrplingCore.Ecs.WorldBuilding;

/// <summary>
/// Declares the systems and lifecycle callbacks used to compose a world.
/// </summary>
public interface IWorldBlueprint
{
    /// <summary>
    /// Gets the registry of system groups and their systems.
    /// </summary>
    SystemRegistry Systems { get; }

    /// <summary>
    /// Gets the callbacks registered for world lifecycle events.
    /// </summary>
    WorldLifecycleCallbacks Lifecycle { get; }
}