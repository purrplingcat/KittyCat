using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using PurrplingCore.Ecs.Attributes;
using PurrplingCore.Ecs.Components;

namespace PurrplingCore.Ecs.Systems;

/// <summary>
/// Deletes entities marked for destruction at a periodic interval.
/// </summary>
[System<UpdateSystemGroup>(Order = SystemOrder.Last)]
public sealed class EntityCleanupSystem : QuerySystem
{
    private const float CleanupIntervalSeconds = 1f;
    private float _timeUntilCleanup = CleanupIntervalSeconds;

    /// <summary>
    /// Creates a cleanup system that selects entities marked as pending destruction.
    /// </summary>
    public EntityCleanupSystem()
    {
        Filter.AllTags(Tags.Get<PendingDestroy>());
        Filter.WithDisabled();
    }

    /// <summary>
    /// Advances the cleanup timer and deletes matching entities when it expires.
    /// </summary>
    protected override void OnUpdate()
    {
        _timeUntilCleanup -= Tick.deltaTime;
        if (_timeUntilCleanup > 0) return;

        _timeUntilCleanup = CleanupIntervalSeconds;
        foreach (var entity in Query.Entities)
        {
            CommandBuffer.DeleteEntity(entity.Id);
        }
    }
}