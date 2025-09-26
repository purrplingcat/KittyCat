using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using System;

namespace PurrplingCore.Toolkit;

public interface IWorld
{
    EntityStore Store { get; }
    SystemGroup Systems { get; }

}
