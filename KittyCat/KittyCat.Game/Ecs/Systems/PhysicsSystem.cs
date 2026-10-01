using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using KittyCat.Services;
using PurrplingCore.Ecs;
using System;

namespace KittyCat.Ecs.Systems;

/// <summary>
/// Placeholder for physics command processing, simulation, and ECS synchronization.
/// </summary>
[Order(Order.Later + 100)]
public class PhysicsSystem() : BaseSystem()
{
    protected override void OnUpdateGroup()
    {
        //EntityStore store = world.CurrentStore;
        //PhysicsWorld physicsWorld = physicsExtension.GetFor(store);

        // Phase 1: send queued ECS commands to the physics world.
        /*store.Query<PhysicsCommandBuffer, PhysicsBody>().ForEach(ref (ref var commandBuffer, ref var body) =>
        {
            // Execute all commands for this body.
            foreach (var command in commandBuffer.Commands)
            {
                switch (command.Type)
                {
                    case PhysicsCommandType.ApplyForce:
                        physicsWorld.ApplyForce(body.Id, command.Force);
                        break;
                    case PhysicsCommandType.SetVelocity:
                        physicsWorld.SetVelocity(body.Id, command.Velocity);
                        break;
                    case PhysicsCommandType.Teleport:
                        physicsWorld.Teleport(body.Id, command.Position);
                        break;
                }
            }
            // Clear commands after processing.
            commandBuffer.Commands.Clear();
        });*/

        // Phase 2: advance the physics simulation.
        //physicsWorld.Step(Tick.deltaTime);

        // Phase 3: synchronize physics positions back to ECS components.
        /*store.Query<PhysicsBody, Transform>().ForEach(ref (ref var body, ref var transform) =>
        {
            var physicsEntity = physicsWorld.GetEntity(body.Id);
            if (physicsEntity != null)
            {
                transform.Position = physicsEntity.Position;
                transform.Rotation = physicsEntity.Rotation;
            }
        });*/
    }
}
