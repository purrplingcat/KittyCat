using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using Microsoft.Extensions.DependencyInjection;
using PurrplingCore.Ecs;
using PurrplingCore.Ecs.Systems;
using PurrplingCore.Toolkit.Collections;

namespace PurrplingCore.Ecs.WorldBuilding;

/// <summary>
/// Materializes a <see cref="WorldBlueprint"/> into a runtime world.
/// </summary>
public sealed class WorldComposer
{
    private readonly World _world;
    private readonly SystemRegistry _systems;
    private readonly IServiceProvider _services;
    private readonly CallbackExecutor<WorldCompositionContext> _composedCallbacks;
    private readonly CallbackExecutor<WorldInitializedContext> _initializedCallbacks;
    private readonly CallbackExecutor<WorldUpdatedContext> _updatedCallbacks;
    private readonly CallbackExecutor<WorldDrawingContext> _drawingCallbacks;
    private readonly CallbackExecutor<WorldDrawnContext> _drawnCallbacks;
    private readonly CallbackExecutor<WorldDisposedContext> _disposedCallbacks;
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
        _composedCallbacks = blueprint.Lifecycle.ComposedExecutor.Snapshot();
        _initializedCallbacks = blueprint.Lifecycle.InitializedExecutor.Snapshot();
        _updatedCallbacks = blueprint.Lifecycle.UpdatedExecutor.Snapshot();
        _drawingCallbacks = blueprint.Lifecycle.DrawingExecutor.Snapshot();
        _drawnCallbacks = blueprint.Lifecycle.DrawnExecutor.Snapshot();
        _disposedCallbacks = blueprint.Lifecycle.DisposedExecutor.Snapshot();
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
        _composedCallbacks.Execute(new WorldCompositionContext(_world, provider, _services));
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
        if (_initializedCallbacks.Count > 0)
            _world.Initialized += OnInitialized;
        if (_updatedCallbacks.Count > 0)
            _world.Updated += OnUpdated;
        if (_drawingCallbacks.Count > 0)
            _world.Drawing += OnDrawing;
        if (_drawnCallbacks.Count > 0)
            _world.Drawn += OnDrawn;
        if (_disposedCallbacks.Count > 0)
            _world.Disposed += OnDisposed;
    }

    private void OnInitialized(object? sender, EventArgs args) =>
        _initializedCallbacks.Execute(new WorldInitializedContext(_world, _services));

    private void OnUpdated(IWorld sender, UpdateTick tick) =>
        _updatedCallbacks.Execute(new WorldUpdatedContext(_world, tick, _services));

    private void OnDrawing(IWorld sender, UpdateTick tick) =>
        _drawingCallbacks.Execute(new WorldDrawingContext(_world, tick, _services));

    private void OnDrawn(IWorld sender, UpdateTick tick) =>
        _drawnCallbacks.Execute(new WorldDrawnContext(_world, tick, _services));

    private void OnDisposed(object? sender, EventArgs args) =>
        _disposedCallbacks.Execute(new WorldDisposedContext(_world, _services));
}