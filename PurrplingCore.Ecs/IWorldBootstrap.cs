using Friflo.Engine.ECS;
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

public delegate void InGroupAction<T>(GroupBuilder<T> builder) where T : SystemGroup;

public sealed class WorldBuilder
{
    private readonly ManagedWorld _world;

    internal WorldBuilder(ManagedWorld world)
    {
        _world = world;
    }

    public void InUpdate(InGroupAction<UpdateSystemGroup> configure) =>
        configure(new GroupBuilder<UpdateSystemGroup>(_world, _world.UpdateSystems));

    public void InDraw(InGroupAction<DrawSystemGroup> configure) =>
        configure(new GroupBuilder<DrawSystemGroup>(_world, _world.DrawSystems));

    public void InInitialize(InGroupAction<InitializeSystemGroup> configure) =>
        configure(new GroupBuilder<InitializeSystemGroup>(_world, _world.InitializeSystems));

    public void InFixedUpdate(InGroupAction<FixedUpdateSystemGroup> configure) =>
        configure(new GroupBuilder<FixedUpdateSystemGroup>(_world, _world.FixedUpdateSystems));

    public void InGroup(string groupName, InGroupAction<SystemGroup> configure)
    {
        var rootBuilder = new GroupBuilder<SystemRoot>(_world, _world.SystemRoot);
        rootBuilder.InGroup(groupName, configure);
    }

    public void InGroup<TGroup>(InGroupAction<TGroup> configure) where TGroup : SystemGroup
    {
        var rootBuilder = new GroupBuilder<SystemRoot>(_world, _world.SystemRoot);
        rootBuilder.InGroup<TGroup>(configure);
    }

    public void InStore(Action<EntityStore> configure)
    {
        configure(_world.Store);
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

public readonly struct GroupBuilder<TGroup> where TGroup : SystemGroup
{
    private readonly ManagedWorld _world;
    private readonly SystemGroup _targetGroup;

    internal GroupBuilder(ManagedWorld world, SystemGroup targetGroup)
    {
        _world = world;
        _targetGroup = targetGroup;
    }

    public void Add<TSystem>() where TSystem : BaseSystem
    {
        _targetGroup.Add(_world.CreateSystem<TSystem>());
    }

    public void Add<TSystem>(Func<IServiceProvider, TSystem> factory) where TSystem : BaseSystem
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

    public void Remove<TSystem>() where TSystem : BaseSystem
    {
        var target = _targetGroup.FindSystem<TSystem>(recursive: false);
        if (target is not null)
            _targetGroup.Remove(target);
    }

    public bool Has<TSystem>() where TSystem : BaseSystem
    {
        return _targetGroup.FindSystem<BaseSystem>(recursive: false) != null;
    }

    public bool HasGroup(string name)
    {
        return _targetGroup.FindGroup(name, recursive: false) != null;
    }

    public void AddGroup(string name)
    {
        _targetGroup.Add(new SystemGroup(name));
    }

    /// <summary>
    /// Enters a specified sub-group and provides a builder to configure it.
    /// </summary>
    /// <param name="name">The name of the target sub-group.</param>
    /// <param name="configureGroup">The action used to configure the sub-group (e.g., adding systems or nested groups).</param>
    /// <param name="createIfMissing">
    /// If set to <c>true</c>, automatically creates the sub-group if it does not already exist. 
    /// The default value is <c>false</c>.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the specified sub-group is not found and the <paramref name="createIfMissing"/> parameter is <c>false</c>.
    /// </exception>
    public void InGroup(string name, InGroupAction<SystemGroup> configureGroup, bool createIfMissing = false)
    {
        var subGroup = _targetGroup.FindGroup(name, recursive: false);

        if (subGroup is null)
        {
            if (!createIfMissing) 
                throw new InvalidOperationException($"Group '{name}' not found in '{_targetGroup.Name}'");
            
            subGroup = new SystemGroup(name);
            _targetGroup.Add(subGroup);
        }

        configureGroup(new GroupBuilder<SystemGroup>(_world, subGroup));
    }

    public void InGroup<TSubGroup>(InGroupAction<TSubGroup> configureGroup) where TSubGroup : SystemGroup
    {
        var subGroup = _targetGroup.FindSystem<TSubGroup>(recursive: false);

        if (subGroup is null)
        {
            subGroup = _world.CreateSystem<TSubGroup>();
            _targetGroup.Add(subGroup);
        }

        configureGroup(new GroupBuilder<TSubGroup>(_world, subGroup));
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

    public void ConfigureGroup(string name, Action<SystemGroup, ManagedWorld> configure)
    {

    }
}
