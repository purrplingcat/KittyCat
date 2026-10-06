using PurrplingCore.Ecs;

namespace PurrplingCore.Ecs.WorldBuilding;

/// <summary>
/// Mutable declaration of a world's systems and lifecycle callbacks.
/// This type intentionally contains no runtime service references.
/// </summary>
public sealed class WorldBlueprint : IWorldBlueprint
{
    /// <summary>
    /// Gets the systems declared for this world.
    /// </summary>
    public SystemRegistry Systems { get; } = new();

    /// <summary>
    /// Gets the callbacks registered for this world's lifecycle.
    /// </summary>
    public WorldLifecycleCallbacks Lifecycle { get; } = new();

    /// <summary>
    /// Creates a composer that materializes this blueprint in the specified world.
    /// </summary>
    public WorldComposer CreateComposer(World world, IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(services);
        return new WorldComposer(world, this, services);
    }
}
