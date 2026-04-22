using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;

namespace PurrplingCore.Ecs.Systems;

public class WorldSystemRoot(World world) : SystemRoot(world.Store)
{
    public World World => world;
}

public sealed class UpdateSystemGroup() : SystemGroup("Update")
{
}

public sealed class DrawSystemGroup() : SystemGroup("Draw")
{
}

public sealed class InitializeSystemGroup() : SystemGroup("Initialize")
{
}

public sealed class FixedUpdateSystemGroup() : SystemGroup("FixedUpdate")
{
    private float _accumulator;

    public float Timestep { get; set; } = 1f / 50f;
    public float MaxDeltaTime { get; set; } = 0.25f;
    

    public new void Update(UpdateTick tick)
    {
        var realDeltaTime = tick.deltaTime;
        var totalTime = tick.time;

        if (realDeltaTime > MaxDeltaTime)
        {
            realDeltaTime = MaxDeltaTime;
        }

        _accumulator += realDeltaTime;

        while (_accumulator >= Timestep)
        {
            base.Update(new UpdateTick(Timestep, totalTime));
            totalTime += Timestep;
            _accumulator -= Timestep;
        }
    }
}
