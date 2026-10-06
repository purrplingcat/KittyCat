using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using PurrplingCore.Ecs.Attributes;

namespace PurrplingCore.Ecs.Systems;

/// <summary>
/// The root system hierarchy associated with a world.
/// </summary>
public class WorldSystemRoot(World world) : SystemRoot(world.Store)
{
    /// <summary>
    /// Gets the world that owns this system root.
    /// </summary>
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

/// <summary>
/// System group for variable-step world updates.
/// </summary>
[TopLevelGroup]
public sealed class UpdateSystemGroup() : SystemGroup("Update")
{
}

/// <summary>
/// System group for world drawing.
/// </summary>
[TopLevelGroup]
public sealed class DrawSystemGroup() : SystemGroup("Draw")
{
}

/// <summary>
/// System group for one-time world initialization.
/// </summary>
[TopLevelGroup]
public sealed class InitializeSystemGroup() : SystemGroup("Initialize")
{
    private bool _initialized;

    /// <summary>
    /// Runs the initialization group once, on its first update.
    /// </summary>
    protected override void OnUpdateGroup()
    {
        if (_initialized) return;

        _initialized = true;
        base.OnUpdateGroup();
    }
}

/// <summary>
/// System group that runs systems at a fixed timestep.
/// </summary>
[TopLevelGroup]
public sealed class FixedUpdateSystemGroup() : SystemGroup("FixedUpdate")
{
    private float _accumulator;

    /// <summary>
    /// Gets or sets the duration of each fixed update in seconds.
    /// </summary>
    public float Timestep { get; set; } = 1f / 50f;

    /// <summary>
    /// Gets or sets the maximum delta time accumulated during one call to <see cref="FixedUpdate(UpdateTick)"/>.
    /// </summary>
    public float MaxDeltaTime { get; set; } = 0.25f;
    
    /// <summary>
    /// Clears elapsed time accumulated toward the next fixed update.
    /// </summary>
    public void Reset()
    {
        _accumulator = 0f;
    }

    /// <summary>
    /// Runs fixed updates while the accumulated elapsed time reaches the configured timestep.
    /// </summary>
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
