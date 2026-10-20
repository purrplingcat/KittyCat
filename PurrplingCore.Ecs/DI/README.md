# World features, services and options

A **world marker** (`IWorldMarker`) identifies a kind of world. Every world instance gets its own DI scope. You compose a world through the `IWorldServicesBuilder` returned by `services.AddWorld<TMarker>()` or `services.AddWorld()`. Complete, tested code lives in [EXAMPLES.md](EXAMPLES.md).

```csharp
services.AddWorld<ArenaWorld>()
    .AddFeature<ParticlesFeature>()
    .ConfigureOptions<ParticleOptions>(options => options.TimeScale = 0.5f);
```

## Building blocks

| API | Purpose | Scope |
| --- | --- | --- |
| `AddFeature<TFeature>()` | Applies a reusable registration recipe (`IWorldFeature`) immediately. | Once per feature type and marker |
| `AddService<T>()`, `AddService<T, TImpl>()`, `AddService<T>(factory)` | Registers a service owned by each world instance. | One instance per world scope, disposed with it |
| `AddModule<TModule>()`, `AddModule(factory)`, `Configure(callback)` | Adds systems and lifecycle callbacks to the world blueprint. | One module instance per world scope |
| `ConfigureOptions<TOptions>(...)` and `IWorldOptions<TOptions>` | Per-marker options with defaults. | Cached per world scope |
| `AddSystemFactory<TSystem>(factory)` | Custom construction of one system type. | Marker-specific |
| `DeriveWorld<TMarker>()` | Reuses the modules registered for another marker. | Modules only |
| `AddRenderer<TConfiguration>()` | Gives the world its own renderer and `RenderContext`. | One renderer per world scope |

## Precedence rules

| Concern | Rule |
| --- | --- |
| Options | Property defaults, then shared `ConfigureAll`, then the marker's `ConfigureOptions`, then validation. Values merge per property, and the marker wins for the properties it sets regardless of registration order. Plain `services.Configure<TOptions>(...)` targets unnamed options and does not apply to worlds. |
| Services | An application-level registration of the same service type always wins, in either registration order. `AddService` is used only when no such registration exists. A world without its own `AddService` and without an application registration fails with `InvalidOperationException`; there is no per-world fallback. |
| Renderer | `AddRenderer<T>()` gives the world its own unkeyed `RenderContext`. A world never falls back to a global renderer automatically; alias one explicitly with `AddService<RenderContext>(...)`. |
| Modules | Every module registered for the marker applies, ordered by `Order`; ties keep registration order. `AddModule<T>()` registers a type once per marker, while each `Configure(callback)` call adds another module. |
| Features | A feature applies once per feature type and marker. Features requested by other features are deduplicated the same way. |
| Systems | A marker-specific `AddSystemFactory` wins; otherwise systems are created through ordinary constructor injection in the world scope. |

## Options

`IWorldOptions<TOptions>` returns the options for the current world's marker. Two instances of the same marker see equal settings but separate instances; different markers cannot affect each other. Values are read once when the world scope first resolves them, so consumers should cache what they need during composition and never look up options in `Update` or `Draw`.

```csharp
// Defaults shared by every world.
services.ConfigureAll<ParticleOptions>(options => options.TimeScale = 0.8f);

// Overrides only the properties it sets, for one marker.
services.AddWorld<SlowMotionWorld>()
    .AddFeature<ParticlesFeature>()
    .ConfigureOptions<ParticleOptions>(options => options.TimeScale = 0.25f);
```

Options types need a public parameterless constructor and valid defaults so that a feature works without any configuration. Register an `IValidateOptions<TOptions>` for checks that must fail world composition. Options are not inherited through `DeriveWorld`; configure the derived marker separately.

## Services

`AddService` creates the instance when the world scope first resolves it and disposes it with that scope. Another instance of the same marker gets its own object. Register a service globally instead when all worlds should share one object.

An application-level registration always overrides a world-owned one. Use one approach per service type: global for shared state, `AddService` for state that belongs to a world.

## Renderers

`AddRenderer<TConfiguration>()` creates a renderer per world instance. The world initializes it, begins its frame before draw systems, flushes it afterward, and disposes it with the scope. To render several worlds through one global renderer, register the renderer globally, add its draw component once, and alias only its context into each world:

```csharp
services.AddWorld<FirstWorld>()
    .AddService<RenderContext>(provider => provider.GetRequiredKeyedService<RenderContext>("Shared"));
```

See [renderer examples](../../PurrplingCore/Rendering/EXAMPLES.md) for the complete setups and frame ownership.

## Writing a feature

A feature is a small class with a parameterless constructor:

```csharp
public sealed class RegenerationFeature : IWorldFeature
{
    public void ConfigureServices(IWorldServicesBuilder builder) => builder.AddModule<RegenerationModule>();
}
```

1. **Options:** create a class with defaults. A feature then needs no configuration.
2. **System:** annotate it with `[System<TGroup>]` and inject `IWorldOptions<TOptions>` through its constructor.
3. **Module:** an `IWorldModule` that calls `world.AddSystem<TSystem>()`.
4. **Feature:** registers the module, services and validators, and depends on other features through `builder.AddFeature<Other>()`.
5. **Extension:** add `AddMyFeature(this IWorldServicesBuilder)` for discoverability.

Rules:

- Features run immediately during service registration. Do not read options or configuration there; read them in constructors when the world is composed.
- Keep simulation features independent of renderers. Put optional drawing in a separate feature that depends on the simulation feature, like `ParticleRenderingFeature`.
- Fail early: validate options with `IValidateOptions` or throw in a constructor. A world that fails to compose is disposed.
- Never create queries or look up services inside `Update` and `Draw`.
- Use `TryAdd` or `TryAddEnumerable` for application-wide registrations so several worlds can enable the same feature.
- Lifecycle callbacks (`OnInitialized`, `OnUpdated`, `OnDrawing`, `OnDrawn`, `OnDisposed`) belong in a module's `Setup`, as `WorldRenderer` uses them.

## Existing features

| Feature | Adds | Options |
| --- | --- | --- |
| `ParticlesFeature` (also `AddParticleModule()`) | Particle simulation without a renderer | `ParticleOptions.TimeScale` |
| `ParticleRenderingFeature` | Simulation plus drawing through the world's `RenderContext` | `ParticleRenderingOptions.Layer` |

The [particle guide](../../PurrplingCore/Ecs/Particles/README.md) covers emission, drawing backends and examples.
