# KittyCat

KittyCat is a MonoGame project containing a reusable game framework, an engine, and a sample game. The repository targets .NET 10 and uses MonoGame 3.8.5. DesktopGL is the primary verified desktop run and publish path.

## Documentation

- [Repository guide](AGENTS.md): project layout, dependency direction, architecture, and development conventions.
- [Content path resolution](PurrplingCore.Toolkit/Content/README.md): package roots, path-prefix mappings, catalogs, and runtime content lookup.
- [2D rendering](PurrplingCore/Rendering/README.md): renderer setup, screens, scaling, layers, and draw order.
- [Particles](PurrplingCore/Ecs/Particles/README.md): simulation, ECS integration, and rendering options.
- [World feature packages](plans/world-feature-packages.md): current design notes for world feature composition.

## Repository Layout

| Project | Purpose |
| --- | --- |
| `PurrplingCore.Toolkit` | Hosting, dependency injection, content, VFS, rendering primitives, messaging, and reusable utilities. |
| `PurrplingCore.Ecs` | ECS integration and world composition. |
| `PurrplingCore` | Reusable engine services built on Toolkit and ECS. |
| `PurrplingCore.ContentBuilder` | MonoGame content compilation, binary asset catalogs, and PAK or loose-file publishing. |
| `KittyCat/KittyCat.Game` | Game-specific scenes, services, ECS systems, configuration, and content definitions. |
| `KittyCat/KittyCat.Content` | Content build tool and asset source files. |
| `KittyCat/KittyCat.DesktopGL` | Cross-platform desktop entry point using MonoGame DesktopGL. |
| `KittyCat/KittyCat.WindowsDX` | Windows-specific MonoGame entry point. |
| `PurrplingCore.Toolkit.Tests` | Toolkit and runtime tests. |
| `PurrplingCore.ContentBuilder.Tests` | Content pipeline and catalog generation tests. |

The root `KittyCat.slnx` includes the framework, content tool, game, desktop projects, and tests. The iOS project exists but is not included in the root solution; it requires Apple build and signing infrastructure.

## Prerequisites

- .NET 10 SDK.
- Internet access for initial NuGet and local .NET tool restore.
- A desktop environment supported by MonoGame DesktopGL to run the game.

The WindowsDX frontend additionally requires Windows. iOS builds require the .NET iOS workload, Apple tooling, and signing configuration.

## Build and Run

From the repository root, restore and build the DesktopGL app:

```powershell
dotnet restore .\KittyCat.slnx
dotnet build .\KittyCat\KittyCat.DesktopGL\KittyCat.DesktopGL.csproj --configuration Debug
```

Run the game:

```powershell
dotnet run --project .\KittyCat\KittyCat.DesktopGL\KittyCat.DesktopGL.csproj --configuration Debug
```

The DesktopGL build invokes the content build target. It compiles the configured source assets and writes generated packages under `Paks/` in the app output. The desktop entry point also discovers mods from a `Mods/` directory beside the executable; the directory may be absent when no mods are installed.

To build content separately, without building the game:

```powershell
dotnet run --project .\KittyCat\KittyCat.Content\KittyCat.Content.csproj --configuration Debug -- --ContentBuilder:CreatePak=true
```

Set `--ContentBuilder:CreatePak=false` to publish loose files instead of PAK archives. Content builder paths and options are configured through `KittyCat/KittyCat.Content/appsettings.json` and the `ContentBuilder` configuration section. Source assets and sidecar metadata live in `KittyCat/KittyCat.Content/Assets/`.

## Tests

Run both test projects from the repository root:

```powershell
dotnet test .\PurrplingCore.Toolkit.Tests\PurrplingCore.Toolkit.Tests.csproj --configuration Debug
dotnet test .\PurrplingCore.ContentBuilder.Tests\PurrplingCore.ContentBuilder.Tests.csproj --configuration Debug
```

## Production Publish

Publish the verified DesktopGL framework-dependent application:

```powershell
dotnet publish .\KittyCat\KittyCat.DesktopGL\KittyCat.DesktopGL.csproj --configuration Release --output .\artifacts\publish\desktopgl
```

Publishing rebuilds the application and generated content. The publish target copies generated packages to `Paks/` beside the app, where the runtime discovers them. Copy the **entire** publish directory to the destination machine; do not distribute only `KittyCat.exe` or `KittyCat.dll`. Install the .NET 10 runtime on the destination machine, then launch `KittyCat.exe` on Windows or the platform apphost on another supported DesktopGL platform.

For a Windows-only build, the `KittyCat.WindowsDX` project is available:

```powershell
dotnet build .\KittyCat\KittyCat.WindowsDX\KittyCat.WindowsDX.csproj --configuration Release
```

The WindowsDX frontend uses a separate MonoGame content pipeline configuration. Validate its content and packaging output for the target deployment before shipping it; the DesktopGL PAK publish instructions above do not automatically apply to WindowsDX. iOS deployment additionally requires its platform workload, signing identity, provisioning, and Apple toolchain.

## Configuration and Data

- Game settings for the DesktopGL app are in `KittyCat/KittyCat.DesktopGL/appsettings.json`.
- Content builder settings and source assets are in `KittyCat/KittyCat.Content/`.
- Compiled game content is produced as PAK files by default; packages are discovered from `Paks/` at runtime.
- Mod directories are discovered from `Mods/` beside the DesktopGL executable. Mods can provide data-only content; code mods use the mod assembly loading support.

Do not edit generated `bin/` and `obj/` outputs as source. Change the source assets, bundle definitions, or project configuration, then rebuild.