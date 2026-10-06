using PurrplingCore.Ecs.Systems;
using PurrplingCore.Ecs.WorldBuilding;

namespace PurrplingCore.Ecs.Modules;

/// <summary>
/// Adds periodic cleanup for entities marked with <see cref="Components.PendingDestroy"/>.
/// </summary>
public sealed class EntityCleanupModule : IWorldModule
{
    /// <summary>
    /// Gets the module's application order.
    /// </summary>
    public int Order => 0;

    /// <summary>
    /// Adds the entity cleanup system to the world's update group.
    /// </summary>
    public void Setup(IWorldBlueprint world)
    {
        world.Systems
            .In<UpdateSystemGroup>()
            .Add<EntityCleanupSystem>(SystemOrder.Last);
    }
}
