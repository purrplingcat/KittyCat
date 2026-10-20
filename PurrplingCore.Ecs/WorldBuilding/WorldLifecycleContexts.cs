using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using PurrplingCore.Ecs;
using PurrplingCore.Toolkit;

namespace PurrplingCore.Ecs.WorldBuilding;

/// <summary>
/// Provides the world, system provider, and services to composition callbacks.
/// </summary>
/// <param name="World">The world whose systems have been composed.</param>
/// <param name="Systems">The provider that resolved the world's system instances.</param>
/// <param name="Services">The service provider scoped to the world.</param>
public readonly record struct WorldCompositionContext(World World, SystemProvider Systems, IServiceProvider Services);

/// <summary>
/// Provides the world and services to initialization callbacks.
/// </summary>
/// <param name="World">The initialized world.</param>
/// <param name="Services">The service provider scoped to the world.</param>
public readonly record struct WorldInitializedContext(World World, IServiceProvider Services);

/// <summary>
/// Provides the world, update tick, and services to update callbacks.
/// </summary>
/// <param name="World">The updated world.</param>
/// <param name="Tick">The tick used for the update.</param>
/// <param name="Services">The service provider scoped to the world.</param>
public readonly record struct WorldUpdatedContext(World World, UpdateTick Tick, IServiceProvider Services);

/// <summary>
/// Provides the world, draw tick, and services before draw systems run.
/// </summary>
/// <param name="World">The world about to be drawn.</param>
/// <param name="Tick">The tick used for drawing.</param>
/// <param name="Services">The service provider scoped to the world.</param>
public readonly record struct WorldDrawingContext(World World, UpdateTick Tick, IServiceProvider Services);

/// <summary>
/// Provides the world, draw tick, and services to draw callbacks.
/// </summary>
/// <param name="World">The drawn world.</param>
/// <param name="Tick">The tick used for drawing.</param>
/// <param name="Services">The service provider scoped to the world.</param>
public readonly record struct WorldDrawnContext(World World, UpdateTick Tick, IServiceProvider Services);

/// <summary>
/// Provides the world and services to disposal callbacks.
/// </summary>
/// <param name="World">The world being disposed.</param>
/// <param name="Services">The service provider scoped to the world.</param>
public readonly record struct WorldDisposedContext(World World, IServiceProvider Services);