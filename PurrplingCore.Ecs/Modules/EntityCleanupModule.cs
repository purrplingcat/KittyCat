using PurrplingCore.Ecs.Systems;

namespace PurrplingCore.Ecs.Modules;

/// <summary>
/// Adds periodic cleanup for entities marked with <see cref="Components.PendingDestroy"/>.
/// </summary>
public sealed class EntityCleanupModule : IWorldModule
{
    public int Order => 0;

    public void Setup(IWorldBlueprint world)
    {
        world.Systems
            .In<UpdateSystemGroup>()
            .Add<EntityCleanupSystem>(SystemOrder.Last);
    }
}
