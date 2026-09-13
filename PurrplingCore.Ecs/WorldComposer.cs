using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.CompilerServices;

namespace PurrplingCore.Ecs;

public readonly record struct WorldCompositionContext(World World, SystemProvider Systems, IServiceProvider Services);
public readonly record struct WorldInitializedContext(World World, IServiceProvider Services);
public readonly record struct WorldUpdatedContext(World World, UpdateTick Tick, IServiceProvider Services);
public readonly record struct WorldDrawnContext(World World, UpdateTick Tick, IServiceProvider Services);
public readonly record struct WorldDisposedContext(World World, IServiceProvider Services);

public interface IWorldBlueprint
{
	SystemRegistry Systems { get; }
	WorldLifecycleCallbacks Lifecycle { get; }
}

/// <summary>
/// Stores callbacks for the runtime lifecycle of a world.
/// </summary>
public sealed class WorldLifecycleCallbacks
{
	public List<Action<WorldCompositionContext>> Composed { get; } = [];
	public List<Action<WorldInitializedContext>> Initialized { get; } = [];
	public List<Action<WorldUpdatedContext>> Updated { get; } = [];
	public List<Action<WorldDrawnContext>> Drawn { get; } = [];
	public List<Action<WorldDisposedContext>> Disposed { get; } = [];
}

public static class WorldBlueprintExtensions
{
	public static IWorldBlueprint OnComposed(this IWorldBlueprint blueprint, Action<WorldCompositionContext> callback)
	{
		ArgumentNullException.ThrowIfNull(blueprint);
		ArgumentNullException.ThrowIfNull(callback);
		blueprint.Lifecycle.Composed.Add(callback);
		return blueprint;
	}

	public static IWorldBlueprint OnInitialized(this IWorldBlueprint blueprint, Action<WorldInitializedContext> callback)
	{
		ArgumentNullException.ThrowIfNull(blueprint);
		ArgumentNullException.ThrowIfNull(callback);
		blueprint.Lifecycle.Initialized.Add(callback);
		return blueprint;
	}

	public static IWorldBlueprint OnUpdated(this IWorldBlueprint blueprint, Action<WorldUpdatedContext> callback)
	{
		ArgumentNullException.ThrowIfNull(blueprint);
		ArgumentNullException.ThrowIfNull(callback);
		blueprint.Lifecycle.Updated.Add(callback);
		return blueprint;
	}

	public static IWorldBlueprint OnDrawn(this IWorldBlueprint blueprint, Action<WorldDrawnContext> callback)
	{
		ArgumentNullException.ThrowIfNull(blueprint);
		ArgumentNullException.ThrowIfNull(callback);
		blueprint.Lifecycle.Drawn.Add(callback);
		return blueprint;
	}

	public static IWorldBlueprint OnDisposed(this IWorldBlueprint blueprint, Action<WorldDisposedContext> callback)
	{
		ArgumentNullException.ThrowIfNull(blueprint);
		ArgumentNullException.ThrowIfNull(callback);
		blueprint.Lifecycle.Disposed.Add(callback);
		return blueprint;
	}
}

/// <summary>
/// Mutable declaration of a world's systems and lifecycle callbacks.
/// This type intentionally contains no runtime service references.
/// </summary>
public sealed class WorldBlueprint : IWorldBlueprint
{
	public SystemRegistry Systems { get; } = new();
	public WorldLifecycleCallbacks Lifecycle { get; } = new();

    public WorldComposer CreateComposer(World world, IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(services);
        
        return new WorldComposer(world, this, services);
    }
}

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

	public void Compose()
	{
		if (_composed)
		{
			throw new InvalidOperationException("WorldComposer.Compose() can only be called once.");
		}

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
		{
			_world.Initialized += OnInitialized;
		}

		if (_updatedCallbacks.Length > 0)
		{
			_world.Updated += OnUpdated;
		}

		if (_drawnCallbacks.Length > 0)
		{
			_world.Drawn += OnDrawn;
		}

		if (_disposedCallbacks.Length > 0)
		{
			_world.Disposed += OnDisposed;
		}
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
		{
			callbacks[index](context);
		}
	}
}
