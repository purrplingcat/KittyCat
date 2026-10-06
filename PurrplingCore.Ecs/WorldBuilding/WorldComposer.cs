using System.Runtime.CompilerServices;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using Microsoft.Extensions.DependencyInjection;
using PurrplingCore.Ecs;
using PurrplingCore.Ecs.Systems;

namespace PurrplingCore.Ecs.WorldBuilding;

/// <summary>
/// Materializes a <see cref="WorldBlueprint"/> into a runtime world.
/// </summary>
public sealed class WorldComposer
{
    private readonly World _world;
    private readonly SystemRegistry _systems;
    private readonly IServiceProvider _services;
    private readonly Action<WorldCompositionContext>[] _composedCallbacks;
    private readonly Action<WorldInitializedContext>[] _initializedCallbacks;
    private readonly Action<WorldUpdatedContext>[] _updatedCallbacks;
    private readonly Action<WorldDrawnContext>[] _drawnCallbacks;
    private readonly Action<WorldDisposedContext>[] _disposedCallbacks;
    private bool _composed;

    /// <summary>
    /// Creates a composer for a world, blueprint, and scoped service provider.
    /// </summary>
    public WorldComposer(World world, IWorldBlueprint blueprint, IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(blueprint);
        ArgumentNullException.ThrowIfNull(services);

        _world = world;
        _systems = blueprint.Systems;
        _services = services;
        _composedCallbacks = [.. blueprint.Lifecycle.Composed];
        _initializedCallbacks = [.. blueprint.Lifecycle.Initialized];
        _updatedCallbacks = [.. blueprint.Lifecycle.Updated];
        _drawnCallbacks = [.. blueprint.Lifecycle.Drawn];
        _disposedCallbacks = [.. blueprint.Lifecycle.Disposed];
    }

    /// <summary>
    /// Builds the system hierarchy and attaches the configured lifecycle callbacks.
    /// </summary>
    public void Compose()
    {
        if (_composed)
            throw new InvalidOperationException("WorldComposer.Compose() can only be called once.");

        _composed = true;
        var provider = new SystemProvider(_systems, ResolveSystem, _world.Signature);
        provider.Populate(_world.SystemRoot, typeof(SystemRoot));
        AttachLifecycleCallbacks();
        ExecuteCallbacks(_composedCallbacks, new WorldCompositionContext(_world, provider, _services));
    }

    private BaseSystem ResolveSystem(Type type)
    {
        if (_services is IKeyedServiceProvider keyedProvider &&
            keyedProvider.GetKeyedService(type, _world.Signature.MarkerType) is BaseSystem keyedSystem)
        {
            return keyedSystem;
        }

        return (BaseSystem)ActivatorUtilities.GetServiceOrCreateInstance(_services, type);
    }

    private void AttachLifecycleCallbacks()
    {
        if (_initializedCallbacks.Length > 0)
            _world.Initialized += OnInitialized;
        if (_updatedCallbacks.Length > 0)
            _world.Updated += OnUpdated;
        if (_drawnCallbacks.Length > 0)
            _world.Drawn += OnDrawn;
        if (_disposedCallbacks.Length > 0)
            _world.Disposed += OnDisposed;
    }

    private void OnInitialized(object? sender, EventArgs args) =>
        ExecuteCallbacks(_initializedCallbacks, new WorldInitializedContext(_world, _services));

    private void OnUpdated(IWorld sender, UpdateTick tick) =>
        ExecuteCallbacks(_updatedCallbacks, new WorldUpdatedContext(_world, tick, _services));

    private void OnDrawn(IWorld sender, UpdateTick tick) =>
        ExecuteCallbacks(_drawnCallbacks, new WorldDrawnContext(_world, tick, _services));

    private void OnDisposed(object? sender, EventArgs args) =>
        ExecuteCallbacks(_disposedCallbacks, new WorldDisposedContext(_world, _services));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ExecuteCallbacks<TContext>(ReadOnlySpan<Action<TContext>> callbacks, TContext context)
    {
        for (var index = 0; index < callbacks.Length; index++)
            callbacks[index](context);
    }
}