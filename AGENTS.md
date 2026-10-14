# KittyCat Repository Guide

## Project

KittyCat is a reusable game framework, engine, and game built with C# and MonoGame.
The code is modular and performance-sensitive, but should remain simple, readable, and
portable. Avoid enterprise-style abstractions unless they solve a real game or framework
problem.

## Stack

- C# with nullable reference types and implicit usings enabled.
- Runtime projects and `KittyCat.Content` target `.NET 10`.
- MonoGame `3.8.5` provides the game loop, graphics, platform backends, and content pipeline.
- `Microsoft.Extensions` provides dependency injection, configuration, and logging.
- `Friflo.Engine.ECS` provides the ECS implementation in `PurrplingCore.Ecs`.
- `Zio` provides virtual file-system primitives; PAK streams use LZ4 compression.
- `DotTiled` provides tiled-map support in `PurrplingCore`.

## Structure

```text
KittyCat.slnx
|-- PurrplingCore.Toolkit/        Shared hosting, DI, content, VFS, messaging, and rendering
|-- PurrplingCore.Ecs/             ECS integration and world composition
|-- PurrplingCore/                 Reusable engine built on Toolkit and ECS
|-- PurrplingCore.ContentBuilder/  Reusable content compiler, catalog builder, and PAK/loose publisher
|-- KittyCat/
        |-- KittyCat.Game/             Game code, services, scenes, ECS content, and localization
        |-- KittyCat.Content/          KittyCat content tool: entry point, assets, and appsettings.json
        |-- KittyCat.DesktopGL/        DesktopGL entry point
        |-- KittyCat.WindowsDX/        WindowsDX entry point
        `-- KittyCat.iOS/              iOS entry point
```

The root solution includes Toolkit, ECS, PurrplingCore, ContentBuilder, Game, Content, DesktopGL, and
WindowsDX. The iOS project is present but is not included in the root solution.
`KittyCat/KittyCat.slnx` is the smaller runtime-focused solution and excludes Content.
`PurrplingCore.Toolkit.Analyzers/` currently contains directory scaffolding only.

## Dependency Direction

```text
PurrplingCore.Toolkit <- PurrplingCore.Ecs <- PurrplingCore <- KittyCat.Game
                    ^                    ^                 ^              ^
                    |                    |                 |              +-- platform projects
                    +--------------------+-----------------+-- KittyCat.Content

PurrplingCore.Toolkit <- PurrplingCore.ContentBuilder <- KittyCat.Content
```

The arrows mean "is referenced by". Keep dependencies flowing from lower-level reusable
code toward higher-level game code. Core projects must not reference `KittyCat.Game` or
platform frontends.

## Main Systems

### Hosting and DI

- `GameHost.CreateBuilder()` creates the host and adds core services.
- Platform entry points call `UsePurrplingCore()` and then `AddGame<KittyCatGame>()`.
- `GameHostBuilder.Services` is the bootstrap service collection.
- `GameHostServicesAttribute` applies modules immediately to host services.
- `GameServicesAttribute` registers modules for the cloned game-service collection.
- The framework is DI-driven. Modules expose registrations and are composed through the host
    and engine instead of being hard-wired together.
- Prefer extension methods for fluent APIs. Keep `IGameHostBuilder` small and focused.

Framework modules are modular and optional. The engine is the composition boundary that brings
the selected framework modules together. A module may be disabled by default when it is not
needed by most games, but the engine must provide an explicit opt-in path for games that need it.
Game-specific composition belongs in the engine or game layer, not in lower-level Toolkit code.

### ECS

`PurrplingCore.Ecs` builds worlds from signatures, keyed factories, modules, system metadata,
and ordered system groups. `WorldFactory`, `WorldManager`, and `SystemRegistry` are the
main composition points. Game-specific ECS systems and components belong in `KittyCat.Game`.
Whenever working with ECS/Friflo code, follow `.github/instructions/friflo-ecs.instructions.md`
and consult the relevant official [Friflo.Engine.ECS documentation](https://friflo.gitbook.io/friflo.engine.ecs),
verifying APIs against the package version used by the project.

### VFS, content, and PAKs

- `VirtualFileSystemManager` provides the stable aggregate file-system root.
- Desktop uses the physical game directory as a fallback; mobile uses an asset-backed provider.
- `VirtualFileSystemStartup` mounts memory, user, cache, and temporary file systems.
- `ContentPackageManager` discovers and mounts PAK/ZIP files from `Content/Paks`.
- PAK files use the `PCAT` format, path hashing, optional compression, and AES-256 CBC with
    HMAC-SHA256 key verification.
- `AssetRegistry` stores GUID-keyed metadata; `AssetManager` loads and reference-counts assets.
- Asset catalogs are binary at runtime, with JSON support for development and legacy data.
- Runtime file access goes through `IFileProvider`/VFS; direct `File` and `Directory` access
    belongs in build-time tools, not runtime code.

### Modding

`ModDiscovery` finds `manifest.json` files. `ModFactory` builds mounted file systems for
directories, ZIPs, and PAKs. Mods can be data-only content packs or code mods loaded through
an isolated `ModAssemblyLoadContext`. `ModRegistry` owns loaded-mod identity and de-duplication.

### Content pipeline

`PurrplingCore.ContentBuilder` wraps the MonoGame content builder, compiles source assets to XNB,
copies metadata, and publishes directory-grouped PAKs with asset catalogs or loose XNB files.
`UseContentBuilder()` binds `ContentBuilderOptions` from the host `ContentBuilder` configuration
section; pass args only to `ApplicationHost.CreateBuilder(args)`. `KittyCat.Content` is the thin
game tool that supplies assets and game-specific settings in `appsettings.json`. Build-time
File I/O is intentional; runtime asset access must use the VFS rules above.

## Coding Rules

- Keep code simple, readable, and broadly reusable.
- Prefer small focused types and clear ownership over large frameworks or enterprise patterns.
- Build modularly and reuse existing abstractions before adding new ones.
- Treat frame time, allocations, I/O, startup cost, and update/draw work as important.
- Measure or reason about performance at hot paths; do not add speculative micro-optimizations.
- Preserve public APIs and dependency direction unless the task requires a deliberate change.
- Use existing language and framework features before introducing custom infrastructure.
- Add XML summaries and short comments where they improve navigation or explain non-obvious
    decisions, lifecycle rules, performance constraints, or platform behavior.
- All code, XML documentation, comments, and repository guidance are written in English.

## Working Rules

- Start from the smallest relevant file, symbol, failing behavior, or test.
- State a local hypothesis, make the smallest useful edit, and run a focused validation.
- Do not refactor unrelated code or revert changes made by the user.
- Prefer a project build for validation when no narrower test exists.

## Validation

```powershell
dotnet build .\KittyCat\KittyCat.DesktopGL\KittyCat.DesktopGL.csproj --configuration Debug
dotnet build .\KittyCat\KittyCat.WindowsDX\KittyCat.WindowsDX.csproj --configuration Debug
```

iOS builds require the iOS .NET workload, Apple signing, and build infrastructure.
