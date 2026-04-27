using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;

namespace PurrplingCore.Ecs;

public interface IWorld
{
    string Name { get; set; }
    EntityStore Store { get; }
    IReadOnlyCollection<BaseSystem> Systems { get; }

    public event EventHandler Destroyed;
    public event EventHandler Initialized;
    public event Action<IWorld, UpdateTick> Updated;
    public event Action<IWorld, UpdateTick> Drawn;
}
