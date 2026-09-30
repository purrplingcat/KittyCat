using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using PurrplingCore.Ecs.Attributes;

namespace PurrplingCore.Ecs.Systems;

public class WorldSystemRoot(World world) : SystemRoot(world.Store)
{
    public World World => world;

    internal T GetOrAddGroup<T>()
        where T : SystemGroup, new()
    {
        foreach (var system in ChildSystems)
        {
            if (system is T group)
            {
                return group;
            }
        }

        var newGroup = new T();
        Add(newGroup);

        return newGroup;
    }
}

[TopLevelGroup]
public sealed class UpdateSystemGroup() : SystemGroup("Update")
{
}

[TopLevelGroup]
public sealed class DrawSystemGroup() : SystemGroup("Draw")
{
}

[TopLevelGroup]
public sealed class InitializeSystemGroup() : SystemGroup("Initialize")
{
    private bool _initialized;

    protected override void OnUpdateGroup()
    {
        if (_initialized) return;

        _initialized = true;
        base.OnUpdateGroup();
    }
}

[TopLevelGroup]
public sealed class FixedUpdateSystemGroup() : SystemGroup("FixedUpdate")
{
    private float _accumulator;

    public float Timestep { get; set; } = 1f / 50f;
    public float MaxDeltaTime { get; set; } = 0.25f;
    
    public void Reset()
    {
        _accumulator = 0f;
    }

    public void FixedUpdate(UpdateTick tick)
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
            Update(new UpdateTick(Timestep, totalTime));
            totalTime += Timestep;
            _accumulator -= Timestep;
        }
    }
}
