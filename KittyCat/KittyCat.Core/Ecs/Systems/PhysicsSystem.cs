using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using KittyCat.Services;
using PurrplingCore.Ecs;
using PurrplingCore.Toolkit;

namespace KittyCat.Ecs.Systems;

[Order(Order.Later + 100)]
public class PhysicsSystem(World world, IWorldExtension<PhysicsWorld> physicsExtension) : BaseSystem()
{
    protected override void OnUpdateGroup()
    {
        EntityStore store = world.CurrentStore;
        PhysicsWorld physicsWorld = physicsExtension.GetFor(store);

        // --- FÁZE 1: ECS -> Fyzika (Předání příkazů) ---
        // Projdeme entity, které mají nějaké příkazy k vykonání
        /*store.Query<PhysicsCommandBuffer, PhysicsBody>().ForEach(ref (ref var commandBuffer, ref var body) =>
        {
            // Projdeme všechny příkazy v bufferu
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
            // Vyčistíme buffer po zpracování
            commandBuffer.Commands.Clear();
        });*/

        // --- FÁZE 2: Simulace ---
        // Řekneme fyzikálnímu enginu, aby provedl svůj výpočet
        physicsWorld.Step(Tick.deltaTime);

        // --- FÁZE 3: Fyzika -> ECS (Synchronizace výsledků) ---
        // Projdeme všechny fyzikální entity a zapíšeme jejich novou pozici
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
