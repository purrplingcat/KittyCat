using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using KittyCat.Services;
using PurrplingCore.Toolkit;
using System.Collections.Generic;

namespace KittyCat.Ecs.Systems;

[RunAfter<PhysicsSystem>]
public class PhysicsCleanupSystem : BaseSystem
{
    private readonly World _world;
    private readonly PhysicsManager _physicsManager;
    //private readonly HashSet<B2BodyId> _pendingRemovals = new();

    public PhysicsCleanupSystem(World world, PhysicsManager physicsManager)
    {
        _world = world;
        _physicsManager = physicsManager;

        _world.CurrentStore.OnComponentRemoved += OnComponentRemoved;
        world.CurrentStore.OnEntityDelete += OnEntityDeleted;
    }

    private void OnEntityDeleted(EntityDelete obj)
    {
        /*if (obj.Entity.HasComponent<PhysicsBody>())
        {
            var body = obj.Entity.GetComponent<PhysicsBody>();
            _pendingRemovals.Add(body.BodyId);
        }*/
    }

    private void OnComponentRemoved(ComponentChanged obj)
    {
        /*if (obj.Type != typeof(PhysicsBody))
        {
            _pendingRemovals.Add(obj.OldComponent<PhysicsBody>().BodyId);
        }*/
    }

    protected override void OnUpdateGroup()
    {
        /*var physicsWorld = _physicsManager.GetWorldFor(world.Store);
        
        foreach (var bodyId in _pendingRemovals)
        {
            physicsWorld.RemoveBody(bodyId);
        }*/
    }
}
