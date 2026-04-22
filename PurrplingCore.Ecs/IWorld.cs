using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;

namespace PurrplingCore.Ecs;

public interface IWorld
{
    string Name { get; set; }
    EntityStore Store { get; }
    IReadOnlyCollection<BaseSystem> Systems { get; }
}
