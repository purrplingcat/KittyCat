using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using PurrplingCore.Ecs.Attributes;
using PurrplingCore.Ecs.Components;

namespace PurrplingCore.Ecs.Systems;

[System<UpdateSystemGroup>(Order = SystemOrder.Last)]
public sealed class EntityCleanupSystem : QuerySystem
{
    private const float CleanupIntervalSeconds = 1f;
    private float _timeUntilCleanup = CleanupIntervalSeconds;

    public EntityCleanupSystem()
    {
        Filter.AllTags(Tags.Get<PendingDestroy>());
        Filter.WithDisabled();
    }

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