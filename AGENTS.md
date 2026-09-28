# Repository Guide

## Stack

- C# with nullable reference types enabled.
- Runtime projects target .NET 8.
- `KittyCat.Content` is a .NET 9 command-line content builder.
- MonoGame 3.8.x provides graphics, platform backends, and the content pipeline.
- Microsoft.Extensions provides dependency injection, configuration, and logging.
- Friflo.Engine.ECS backs the ECS integration in `PurrplingCore.Ecs`.
- Zio provides virtual file-system primitives; package streams use LZ4 compression.
- DotTiled is used by `PurrplingCore` for tiled map content.

## Project Structure

```text
KittyCat.slnx
|-- PurrplingCore/                Main reusable engine; depends on Toolkit and ECS
|-- PurrplingCore.Toolkit/        Shared hosting, DI, content, VFS, messaging, and rendering toolkit
|-- PurrplingCore.Ecs/            ECS integration; depends on Toolkit
`-- KittyCat/
    |-- KittyCat.Game/            Game implementation and shared content; depends on PurrplingCore
    |-- KittyCat.Content/         Content and PAK builder; depends on Toolkit
    |-- KittyCat.DesktopGL/       DesktopGL executable; depends on Game
    |-- KittyCat.WindowsDX/       WindowsDX executable; depends on Game
    |-- KittyCat.Android/         Android executable; depends on Game
    `-- KittyCat.iOS/             iOS executable; depends on Game
```

The root `KittyCat.slnx` contains PurrplingCore, Toolkit, ECS, Game, Content, DesktopGL, and WindowsDX. Android and iOS projects exist in the repository but are not included in that solution. `KittyCat/KittyCat.slnx` is a smaller runtime-focused solution containing PurrplingCore, Toolkit, ECS, Game, DesktopGL, and WindowsDX.

`PurrplingCore.Toolkit.Analyzers/` currently contains analyzer-related directory scaffolding but no project files included in either solution.

## Dependency Direction

```text
PurrplingCore.Toolkit --> PurrplingCore.Ecs
PurrplingCore.Toolkit --> PurrplingCore
PurrplingCore.Ecs     --> PurrplingCore
PurrplingCore         --> KittyCat.Game --> platform executables
PurrplingCore.Toolkit --> KittyCat.Content
```

Keep dependencies flowing in this direction. PurrplingCore projects must not depend on KittyCat projects or platform frontends.

## Hosting And Services

- `GameHostBuilder.Services` is the bootstrap/host service collection.
- `IServiceModule` registrations added with `AddServiceModule` are resolved from the host provider and applied later to the cloned game service collection during `GameHost.Initialize()`.
- Use `ConfigureHostServices<TModule>()` for modules that must run immediately against bootstrap services.
- Prefer fluent/convenience APIs as extension methods; keep `IGameHostBuilder` limited to essential host capabilities.
- Platform entry points explicitly call `UsePurrplingCore()` before `AddGame<KittyCatGame>()`. Android supplies its asset-backed `IFileProvider` there.

## Build

Primary local validation:

```powershell
dotnet build .\KittyCat\KittyCat.DesktopGL\KittyCat.DesktopGL.csproj --configuration Debug
```

WindowsDX validation:

```powershell
dotnet build .\KittyCat\KittyCat.WindowsDX\KittyCat.WindowsDX.csproj --configuration Debug
```

Android and iOS builds require their corresponding .NET workloads; iOS also requires Apple signing/build infrastructure.
