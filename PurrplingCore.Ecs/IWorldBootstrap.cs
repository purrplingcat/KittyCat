using Friflo.Engine.ECS.Systems;
using PurrplingCore.Ecs.Extensions;
using PurrplingCore.Ecs.Systems;
using System.Runtime.CompilerServices;

namespace PurrplingCore.Ecs;

public interface IWorldBootstrap
{
    int Order { get; }
    public void Setup(ManagedWorld world);
}

public sealed class WorldBuilder
{
    private readonly ManagedWorld _world;

    internal WorldBuilder(ManagedWorld world)
    {
        _world = world;
    }

    public void InUpdate(Action<GroupBuilder> configure) =>
        configure(new GroupBuilder(_world, _world.UpdateSystems));

    public void InDraw(Action<GroupBuilder> configure) =>
        configure(new GroupBuilder(_world, _world.DrawSystems));

    public void InInitialize(Action<GroupBuilder> configure) =>
        configure(new GroupBuilder(_world, _world.InitializeSystems));

    public void InFixedUpdate(Action<GroupBuilder> configure) =>
        configure(new GroupBuilder(_world, _world.FixedUpdateSystems));

    public void InGroup(string groupName, Action<GroupBuilder> configure)
    {
        var rootBuilder = new GroupBuilder(_world, _world.SystemRoot);
        rootBuilder.InGroup(groupName, configure);
    }

    public void InGroup<TGroup>(Action<GroupBuilder> configure) where TGroup : SystemGroup
    {
        var rootBuilder = new GroupBuilder(_world, _world.SystemRoot);
        rootBuilder.InGroup<TGroup>(configure);
    }

    public void Configure(Action<ManagedWorld> configure)
    {
        configure(_world);
    }
}

public abstract class BootstrapBase : IWorldBootstrap
{
    public virtual int Order => 0;

    protected abstract void OnSetup(WorldBuilder builder);

    public void Setup(ManagedWorld world)
    {
        OnSetup(new WorldBuilder(world));
    }
}

internal sealed class DelegateBuilderBootstrap(Action<WorldBuilder> configure) : BootstrapBase
{
    protected override void OnSetup(WorldBuilder builder)
    {
        configure(builder);
    }
}

public readonly struct GroupBuilder
{
    private readonly ManagedWorld _world;
    private readonly SystemGroup _targetGroup;

    internal GroupBuilder(ManagedWorld world, SystemGroup targetGroup)
    {
        _world = world;
        _targetGroup = targetGroup;
    }

    public void Add<T>() where T : BaseSystem
    {
        _targetGroup.Add(_world.CreateSystem<T>());
    }

    public void Add<T>(Func<IServiceProvider, T> factory) where T : BaseSystem
    {
        _targetGroup.Add(factory(_world.Services));
    }

    public void Add(BaseSystem system)
    {
        _targetGroup.Add(system);
    }

    public void AddBefore<TTarget, TNew>() where TTarget : BaseSystem where TNew : BaseSystem
    {
        _targetGroup.AddBefore<TTarget>(_world.CreateSystem<TNew>());
    }

    public void AddBefore<TTarget>(Func<IServiceProvider, BaseSystem> factory) where TTarget : BaseSystem
    {
        _targetGroup.AddBefore<TTarget>(factory(_world.Services));
    }
    public void AddBefore<TTarget>(BaseSystem newSystem) where TTarget : BaseSystem
    {
        _targetGroup.AddBefore<TTarget>(newSystem);
    }

    public void AddAfter<TTarget, TNew>() where TTarget : BaseSystem where TNew : BaseSystem
    {
        _targetGroup.AddAfter<TTarget>(_world.CreateSystem<TNew>());
    }

    public void AddAfter<TTarget>(Func<IServiceProvider, BaseSystem> factory) where TTarget : BaseSystem
    {
        _targetGroup.AddAfter<TTarget>(factory(_world.Services));
    }

    public void AddAfter<TTarget>(BaseSystem newSystem) where TTarget : BaseSystem
    {
            _targetGroup.AddAfter<TTarget>(newSystem);
    }

    public void Insert(int index, BaseSystem system)
    {
        if (index < 0 || index > _targetGroup.ChildSystems.Count)
            throw new ArgumentOutOfRangeException(nameof(index));

        _targetGroup.Insert(index, system);
    }

    public void Insert<T>(int index) where T : BaseSystem
    {
        Insert(index, _world.CreateSystem<T>());
    }

    public void Insert<T>(int index, Func<IServiceProvider, BaseSystem> factory) where T : BaseSystem
    {
        Insert(index, factory(_world.Services));
    }

    public void Replace<TTarget, TNew>() where TTarget : BaseSystem where TNew : BaseSystem
    {
        Replace<TTarget>(_world.CreateSystem<TNew>());
    }

    public void Replace<TTarget>(Func<IServiceProvider, BaseSystem> factory) where TTarget : BaseSystem
    {
        Replace<TTarget>(factory(_world.Services));
    }

    public void Replace<TTarget>(BaseSystem newSystem) where TTarget : BaseSystem
    {
        var target = _targetGroup.FindSystem<TTarget>(recursive: false) 
            ?? throw new InvalidOperationException($"System {typeof(TTarget).Name} not found for replacement.");
        Replace(target, newSystem);
    }

    public void Replace(Type target, BaseSystem newSystem)
    {
        var targetSystem = _targetGroup.FindSystem(target, recursive: false) 
            ?? throw new InvalidOperationException($"System of type {target.Name} not found for replacement.");
        Replace(targetSystem, newSystem);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Replace(BaseSystem targetSystem, BaseSystem newSystem)
    {
        ArgumentNullException.ThrowIfNull(targetSystem);
        ArgumentNullException.ThrowIfNull(newSystem);

        int index = _targetGroup.ChildSystems.IndexOf(targetSystem);
        _targetGroup.Remove(targetSystem);
        _targetGroup.Insert(index, newSystem);
    }

    public void Remove<T>() where T : BaseSystem
    {
        var target = _targetGroup.FindSystem<T>(recursive: false);
        if (target is not null)
            _targetGroup.Remove(target);
    }

    public void InGroup(string name, Action<GroupBuilder> configureGroup)
    {
        var subGroup = _targetGroup.FindGroup(name, recursive: false);

        if (subGroup is null)
        {
            subGroup = new SystemGroup(name);
            _targetGroup.Add(subGroup);
        }

        configureGroup(new GroupBuilder(_world, subGroup));
    }

    public void InGroup<TGroup>(Action<GroupBuilder> configureGroup) where TGroup : SystemGroup
    {
        var subGroup = _targetGroup.FindSystem<TGroup>(recursive: false);

        if (subGroup is null)
        {
            subGroup = _world.CreateSystem<TGroup>();
            _targetGroup.Add(subGroup);
        }

        configureGroup(new GroupBuilder(_world, subGroup));
    }

    public void Configure<TSystem>(Action<TSystem, ManagedWorld> configure) where TSystem : BaseSystem
    {
        var system = _targetGroup.GetSystem<TSystem>(recursive: false);
        configure(system, _world);
    }

    public void Configure(Action<SystemGroup, ManagedWorld> configure)
    {
        configure(_targetGroup, _world);
    }
}
