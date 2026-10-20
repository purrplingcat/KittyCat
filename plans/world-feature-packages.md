# Plan: World-Scoped Feature Packages

Date: 2026-10-05.

Status: Partially implemented (2026-10-20). `IWorldFeature`, `AddFeature<T>`, world-scoped `AddModule<T>`/`AddModule(factory)`, and per-world `AddService` registrations are available. All blueprint modules, including `Configure` callback wrappers, share the world-scoped lifetime; shared-instance module registration is not supported. World services and feature extensions have separate classes in `PurrplingCore.Ecs/DI`. Optional world-owned rendering is implemented in `PurrplingCore/Rendering`, with global UI rendering preserved. World options, particle `TimeScale`, and the particle-specific rendering package below remain planned, not implemented. The remaining work is not authorized by this status update.

## Goal and Confirmed Decisions

Provide real, composable game feature packages that register their services, configuration and world systems together. Avoid propagating settings and renderer selections through every method/system constructor factory.

User explicitly selected:
- Enable packages on an existing world builder: `AddWorld<TWorld>().AddFeature<ParticlesFeature>()`; default world equivalent supported.
- Shared defaults with configuration overrides per world marker, NOT per individual instance of the same marker.
- Particles as the first real package, NOT migration of all StarCatcher gameplay.
- Implement both simulation `TimeScale` (1 preserves behavior; 0 pauses) and separately optional renderer/layer integration.

Recommended composition uses standard `Microsoft.Extensions.Options`, ordinary constructor DI, existing `IWorldServicesBuilder`/`IWorldModule` and `IRendererConfiguration`/`RenderContextManager`. No universal shared-data dictionary, `IWorldContext` configuration service locator, `object[]` constructor arguments, temporary service-provider builds, new runtime feature registry, or nested consumer callbacks.

## Verified Local Findings

1. [ECS service registration](../PurrplingCore.Ecs/DI/ServiceCollectionExtensions.cs): `AddWorld`/`AddWorld<T>` use `TryAddKeyedSingleton` factories; multiple calls for the same marker accumulate modules. `AddModule<T>` and `AddModule(factory)` register keyed scoped `IWorldModule` using `TryAddEnumerable`. `Configure` adds a scoped delegate module per call.
2. [ManagedWorld](../PurrplingCore.Ecs/ManagedWorld.cs): `Create` creates a scope and initializes its `WorldContext` before modules and systems are resolved. Each `ManagedWorld` owns scope disposal.
3. [WorldFactory](../PurrplingCore.Ecs/WorldFactory.cs): `DefaultWorldFactory.CreateWorld` resolves keyed modules from `world.Services`, orders by `Order`, configures a fresh blueprint, composes via world-scoped services. Therefore keyed scoped modules fit without changing factory code.
4. [WorldComposer](../PurrplingCore.Ecs/WorldComposer.cs): `ResolveSystem` prefers marker-keyed registrations then `ActivatorUtilities.GetServiceOrCreateInstance`. Scoped constructor dependencies already supported; no resolver expansion needed. World systems must not be singletons.
5. [ApplicationHost](../PurrplingCore.Toolkit/Hosting/ApplicationHost.cs): `Initialize` enumerates `IServiceModule` from the bootstrap provider only, applies registrations to a cloned collection, and finally builds application provider. `AddServiceModule` inside another module's `ConfigureServices` does NOT run the added child module during that pass. A feature must perform its registration immediately while configuring `IServiceCollection`; it must not rely on recursive delayed `IServiceModule` processing.
6. [Renderer service registration](../PurrplingCore/Rendering/ServiceCollectionExtensions.cs): `AddRenderer` appends singleton/keyed registrations and each `AddDrawComponent` adds another draw-loop component. Do not call these from every attached particle rendering package; root game/renderer configuration remains the owner of actual renderer creation and draw scheduling. Renderer configuration `AddModule<TModule>` is keyed transient and DI-created when constructing renderer options.
7. [ParticleModule](../PurrplingCore/Ecs/Particles/ParticleModule.cs) adds only `ParticleUpdateSystem`. [ParticleUpdateSystem](../PurrplingCore/Ecs/Particles/ParticleUpdateSystem.cs) uses `Query.Chunks` and deferred `CommandBuffer.DeleteEntity`; there are no existing simulation options. [ParticleDrawSystem](../PurrplingCore/Ecs/Particles/ParticleDrawSystem.cs) is an abstract world-owned query system with protected `Draw(in LayerWriter)`. [StarCatcherParticleDrawSystem](../KittyCat/KittyCat.Game/Ecs/Systems/StarCatcherParticleDrawSystem.cs) selects a keyed context/effects layer; [RenderContextManager](../PurrplingCore/Rendering/RenderContextManager.cs) documents that `Get(key)` should be called once during composition.
8. `Friflo.Engine.ECS` package version is `3.6.0` in [PurrplingCore.Ecs.csproj](../PurrplingCore.Ecs/PurrplingCore.Ecs.csproj). Official systems documentation was consulted; `SystemRoot` owns hierarchy execution and command buffers defer structural changes. Preserve current query and playback semantics; no new Friflo APIs required.
9. Standard options: `IOptions<T>` is singleton/default-name only, does NOT select named options. `IOptionsSnapshot<T>` is scoped and supports `Get(name)`. Default-name configuration is NOT automatically inherited by named options. `IConfigureNamedOptions<T>` supporting all names supplies shared defaults; marker-specific `IPostConfigureOptions<T>` supplies final overrides independent of the order of base registrations.
10. No existing `*Test*.csproj` found. Reuse any relevant validation discovered at implementation start; otherwise use a temporary .NET harness OUTSIDE the workspace, to avoid VS Code adding it to the root solution.

## Architecture and API Contract

### Feature Registration

- Introduce `IWorldFeature` in `PurrplingCore.Ecs.DI` with a single registration operation `ConfigureServices(IWorldServicesBuilder builder)`.
- Add `AddFeature<TFeature>()` as an extension on `IWorldServicesBuilder`, where `TFeature` implements `IWorldFeature` and has a parameterless constructor.
- Features are registration recipes, applied synchronously before application provider build, not runtime DI services or a second module-resolution phase. They must not consume application options at registration time. Their world/configuration/system classes ARE DI-created and receive typed settings later.
- Deduplicate registration by (feature type, world marker) using minimal internal registration metadata in `IServiceCollection`. No public feature catalog or runtime lookup API. Mark before applying dependencies so repeated/dependent registrations cannot duplicate services/systems. A registration exception aborts composition; do not promise transactional retry of a partially modified service collection.
- Shared services/configurators use `TryAdd`/`TryAddEnumerable`. World contributions are keyed by `builder.Signature.MarkerType`. Two different markers can independently enable the same feature.
- Existing `AddParticleModule` stays supported and delegates to the simulation feature registration. Preserve fluent return type and current no-render behavior.

### Shared and World Configuration

- Add scoped `IWorldOptions<TOptions>` exposing `Value`; implementation wraps `IOptionsSnapshot<TOptions>` and `IWorldContext.Signature` only. It does not add members to `WorldContext`.
- Compute and cache the selected options instance on first access/construction in the world's scope. Consumers cache immutable configuration values during composition, never call DI/options factories from `Update`/`Draw`.
- One small `WorldOptions.GetName(WorldSignature)` helper maps marker types to deterministic, collision-resistant named-options identity (e.g. `MarkerType.AssemblyQualifiedName`). Use the same mapping for default and custom markers; do NOT use world instance `Name` or mutable `Flags`. Keep `Options.DefaultName` reserved for ordinary application-wide `IOptions<T>`.
- Register `AddOptions` infrastructure and the open generic scoped adapter through `AddEcsCoreServices`. Check the existing transitive Options package reference/version before editing project dependencies; add no version churn if already available.
- Shared defaults are applied to all names by a typed `IConfigureNamedOptions` class. World-specific overrides are DI-created `IPostConfigureOptions` classes selecting `WorldOptions.GetName(signature)`. Use standard `services.ConfigureOptions<TConfiguration>()` registration, not custom callbacks.
- Effective precedence: options property defaults -> common typed configuration/config section -> marker-specific post-configuration -> validation. Missing override inherits shared defaults; unspecified fields in a bound override retain shared values. Never mutate an options instance from gameplay/render code.
- Same marker instances have the same values but their own scoped options instances/adapters; different markers cannot leak settings. No hot reload guarantee: changes do not mutate existing world systems; new worlds get a new snapshot.
- Use scoped `AddModule<TModule>()` or `AddModule(factory)` for all blueprint composition modules. Resolve their dependencies from the actual world's scope, including `IWorldOptions`/`IWorldContext`. `Configure` wrappers have the same scoped lifetime. Do not add a separate lifetime-specific module API or shared-instance overload.

### Particle Simulation Package

- Define `ParticlesFeature`, `ParticleOptions` and its typed defaults/configuration/validator next to the existing particle module/extension where appropriate, minimizing file churn.
- `ParticleOptions` has only `TimeScale`, default 1. Validate finite, nonnegative values once when options are materialized. Do not add budgets, emission registries or unused switches.
- `ParticlesFeature` registers its options/configuration infrastructure and existing stateless `ParticleModule`; it must not reference rendering options/keys/services.
- `ParticleUpdateSystem` obtains `IWorldOptions<ParticleOptions>` by ordinary constructor DI and caches `TimeScale`. Retain the existing public parameterless constructor for direct standalone construction; it uses scale 1. DI selects the configuration-aware constructor in managed worlds.
- Validate incoming `deltaTime` as today. `TimeScale` 0 skips simulation/deletion, preserving state and lifetime; otherwise use scaled delta for `ParticleState.Update`. Ensure multiplied delta remains finite (explicit failure, not silent overflow) before the chunk loop. At scale 1 preserve existing results and allocation profile.
- Preserve `Query.Chunks` enumeration and deferred deletion; do not introduce per-particle guards, option reads or allocations.

### Optional Particle Rendering Package

- Separate `ParticleRenderingFeature` and `ParticleRenderingOptions` from simulation feature code. Rendering package depends explicitly on `ParticlesFeature`, deduplicated automatically. Simulation never installs it.
- Rendering options contain only `RendererKey` (same legitimate object-key contract as existing renderer APIs) and `Layer` (existing `RenderLayerId`). Texture/emission recipes remain where they are. Key/layer references are configuration, not mutable world state.
- Typed C# configuration is the guaranteed route for enum/object renderer keys and `RenderLayerId`; do not promise JSON binding for arbitrary object keys. JSON-safe simulation settings can bind a global `Particles` section; per-marker overrides use a selected section in typed post-configurators.
- Add a concrete engine-owned configured wrapper over existing `ParticleDrawSystem`. Constructor receives `IWorldOptions<ParticleRenderingOptions>` and `RenderContextManager`, validates required configuration, resolves the renderer context once and caches key/layer-derived state. `OnUpdate` creates/borrows `LayerWriter` and calls existing `Draw` helper. Queries remain owned by the world; renderer/context managers never capture worlds/stores/queries.
- Register the wrapper only for worlds that enable `ParticleRenderingFeature`. If only simulation is enabled, graphics/render services are not required. Invalid/missing renderer selection must fail during composition, not be resolved every frame.
- Do not create renderer instances/layers/draw components implicitly in this first rendering package: existing `AddRenderer(...).AddModule<WorldRendererConfiguration>().AddDrawComponent(...)` remains sole owner. The package connects its draw system to the configured existing renderer/layer.
- Do not create a generic particle submission DI service or change `Renderer2D`/`WorldController`.

## Implementation Steps (Sequential Dependencies)

1. Core packaging and scoped configuration (first independent verification gate).
   Add `IWorldFeature`, `AddFeature<T>`, the marker-name helper, and `IWorldOptions<T>` adapter registration. Use the unified world-scoped `AddModule` API. Preserve `AddWorld`, `WorldFactory`, `WorldComposer`, and `SystemProvider`. Validate immediately with focused tests: immediate feature registration from inside `IServiceModule`, two features composing the same world, repeat deduplication, two markers/options isolation, same marker two scopes, and a module selecting a system from world options. No changes to hosting execution are necessary.

2. Simulation package (depends on step 1).
   Adapt `AddParticleModule` to `ParticlesFeature`, add `TimeScale` options/default configuration/validation, and consume options in `ParticleUpdateSystem` with standalone constructor compatibility. Validate immediately: 1x unchanged, 0x paused, 0.5x and 2x expected position/lifetime, invalid scale and scaled overflow failures, deletion playback unchanged, and headless/textureless use without renderer services.

3. Optional rendering package (depends on steps 1 and 2).
   Add `ParticleRenderingFeature`/Options and the configured world-owned draw-system wrapper. Resolve context once using `RenderContextManager`; keep existing render ownership and independent backends. Validate registration with scoped configuration for two markers and distinct renderer/layer selections; disabled rendering requires no graphics services; no duplicate update/draw systems when simulation and rendering dependencies are both registered. Concrete GPU drawing checks occur after game wiring, not by attempting an uninitialized renderer in the headless harness.

4. Game wiring and documentation (depends on steps 2 and 3).
   In `KittyCatGameServices` use one particle rendering feature opt-in (its simulation dependency is automatic), register a DI-created typed marker-specific post-configuration class selecting `KittyCatRendererKey.World` and `KittyCatLayers.Effects`, remove explicit `StarCatcherParticleDrawSystem` world registration to prevent duplicate rendering. Keep emission recipes, `StarCatcherParticleSystem` and renderer setup as-is. The existing custom `StarCatcherParticleDrawSystem` may remain unregistered as an existing supported custom-backend wrapper; do not delete unrelated behavior just to clean up. Extend particle README with package setup, defaults/marker overrides, global vs world options lifetimes, optional render composition and standalone construction.

5. Integration gates (depends on step 4).
   Build DesktopGL Debug and WindowsDX Debug (shared engine integration touched). Run the game: visible particle effects draw once on Effects layer, collection/restart/expiry still work, initial scale 1 unchanged. Manually exercise a second marker with altered `TimeScale`/render selection in the harness/test setup without persisting an extra game world. Check steady-state allocation profile for touched simulation/draw paths and disposal/no renderer reference retaining a world. Review actual diff for scope and preserved user changes.

## Critical Files

Paths below are relative to the repository root so this plan remains portable.

Existing:
- [PurrplingCore.Ecs/DI/ServiceCollectionExtensions.cs](../PurrplingCore.Ecs/DI/ServiceCollectionExtensions.cs): `AddEcsCoreServices`, scoped `AddModule` and `Configure`; retain the existing builder and factory composition.
- [PurrplingCore/Ecs/Particles/ParticleModule.cs](../PurrplingCore/Ecs/Particles/ParticleModule.cs): retain stateless `ParticleModule`, add package registration/configuration types or adjacent narrowly scoped types, keep `AddParticleModule` compatibility.
- [PurrplingCore/Ecs/Particles/ParticleUpdateSystem.cs](../PurrplingCore/Ecs/Particles/ParticleUpdateSystem.cs): constructor/configuration handling and scaled time only.
- [PurrplingCore/Ecs/Particles/README.md](../PurrplingCore/Ecs/Particles/README.md): feature setup, options precedence/scopes, rendering opt-in.
- [KittyCat/KittyCat.Game/Configuration/KittyCatGameServices.cs](../KittyCat/KittyCat.Game/Configuration/KittyCatGameServices.cs): use feature, add typed particle render post-configurator, stop registering old draw wrapper.

Necessary new homes (keep related small types together; these files are proposed, not created):
- `PurrplingCore.Ecs/DI/IWorldFeature.cs`: registration recipe contract.
- `PurrplingCore.Ecs/DI/WorldOptions.cs`: `IWorldOptions` adapter and marker-name mapping.
- `PurrplingCore/Ecs/Particles/ParticleRenderingFeature.cs`: optional render feature, options and concrete configured draw wrapper.

Reference only, no changes planned:
- [PurrplingCore.Ecs/ManagedWorld.cs](../PurrplingCore.Ecs/ManagedWorld.cs): scope/context lifetime.
- [PurrplingCore.Ecs/WorldFactory.cs](../PurrplingCore.Ecs/WorldFactory.cs): keyed module composition.
- [PurrplingCore.Ecs/WorldComposer.cs](../PurrplingCore.Ecs/WorldComposer.cs): ordinary DI system construction.
- [PurrplingCore/Ecs/Particles/ParticleDrawSystem.cs](../PurrplingCore/Ecs/Particles/ParticleDrawSystem.cs): chunk draw helper.
- [PurrplingCore/Rendering/RenderContextManager.cs](../PurrplingCore/Rendering/RenderContextManager.cs): resolve once at system composition.
- [PurrplingCore/Rendering/ServiceCollectionExtensions.cs](../PurrplingCore/Rendering/ServiceCollectionExtensions.cs): retain renderer creation/configuration ownership.

## Focused Verification Matrix

- Composition: package invoked inside `KittyCatGameServices.ConfigureServices` immediately contributes registrations; no child service-module recursion assumption; two packages retain both contributions; repeat opt-in no duplicate systems/configurators; two markers each get correct modules. Rendering dependency on simulation is registered once.
- Configuration: missing marker overrides inherits shared defaults; post-override precedes validation; `IOptions<T>` default remains global; `IWorldOptions<T>` selects marker named snapshot; systems and scoped modules see same `Value` in one scope; instances cannot mutate another world; flags/name irrelevant to configuration identity.
- Lifetimes: `ValidateScopes` and `ValidateOnBuild` enabled in test provider; all blueprint modules are scoped and can depend on world options; verify type, factory and `Configure` registrations. Disposing one world disposes its adapter/modules exactly once without invalidating another. Do not resolve scoped adapters or modules at root.
- Simulation: exact default behavior and parameterless API; scaling/pause; validation exceptions are outside per-particle loops; command playback deletes expired entities; do not read component refs after deletion; no texture/device/renderer requirements.
- Rendering: missing configured renderer fails at composition; expected key/layer selection; no double render flush/game components; draw calls no DI lookup; GPU manual check and world lifetime retention check. Do not claim automatic renderer/layer creation.
- Project builds: use the commands below. Prefer cheapest harness checks after first substantive edit. If temporary project needed, outside workspace; no solution churn.

```powershell
dotnet build .\KittyCat\KittyCat.DesktopGL\KittyCat.DesktopGL.csproj --configuration Debug
dotnet build .\KittyCat\KittyCat.WindowsDX\KittyCat.WindowsDX.csproj --configuration Debug
```

## Scope Exclusions

No NuGet packaging/new project, runtime plugin loading, hot enabling/disabling feature systems, per-instance world configs, generic feature dependency graph, recursive `IServiceModule` host overhaul, renderer auto-creation, game-wide StarCatcher migration, new particle budgets/emission APIs, or changes to Friflo queries/factories/resolvers.

## Sources

- [Friflo systems documentation](https://friflo.gitbook.io/friflo.engine.ecs/documentation/systems.md): system hierarchy/store/update and deferred command playback, checked against package 3.6.0.
- [Microsoft options documentation](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options?view=aspnetcore-8.0): named options, singleton/scoped lifetimes, typed configuration, post-configuration and validation.

## Estimate and Resume Point

Implementation estimate: approximately 90-120 minutes including the external focused harness and desktop manual check, dependent on graphics launch/build environment.

This document records the complete collected design, not completed implementation. When implementation is explicitly requested, start with step 1 and recheck the local owning code and applicable instructions against the then-current repository. Preserve user changes and do not expand the agreed scope. Run the first focused validation immediately after the first substantive code edit.