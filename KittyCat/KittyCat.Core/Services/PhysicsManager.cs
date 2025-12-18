using Friflo.Engine.ECS;
using PurrplingCore.Ecs;
using PurrplingCore.Toolkit.DI;
using System;

namespace KittyCat.Services;

[Singleton, WorldExtension<PhysicsWorld>]
public class PhysicsManager(World world) : WorldExtension<PhysicsWorld>(world)
{
    protected override PhysicsWorld Create(EntityStore store)
    {
        return new PhysicsWorld();
    }
}

// TODO: Temporary placeholder for PhysicsWorld class
public class PhysicsWorld
{
    internal void Step(float deltaTime)
    {
        //throw new NotImplementedException();
    }
}
