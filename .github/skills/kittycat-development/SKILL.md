---
name: kittycat-development
description: "Use when modifying the KittyCat game, PurrplingCore framework, ECS, hosting, VFS, content pipeline, PAKs, mods, or platform projects."
---

# KittyCat Development

## Scope

KittyCat contains a reusable toolkit, ECS integration, engine, content builder, game, and
platform frontends. Keep changes aligned with the current dependency direction:

```text
PurrplingCore.Toolkit <- PurrplingCore.Ecs <- PurrplingCore <- KittyCat.Game <- platform
PurrplingCore.Toolkit <- KittyCat.Content
```

## Implementation

- Locate the code that directly owns the behavior before editing callers or wiring.
- Keep the implementation simple, readable, modular, and reusable.
- Avoid enterprise abstractions and speculative infrastructure.
- Treat the framework as DI-driven: modules expose registrations and are composed through the engine.
- Keep framework modules modular and optional. Modules that are not generally needed may be disabled
	by default, but the engine must provide an explicit opt-in path for games that need them.
- Keep ECS worlds, systems, signatures, factories, and ECS modules within the ECS/engine composition model.
- Treat update, draw, asset loading, startup, and file-system code as performance-sensitive.
- Use existing hosting, DI, ECS, VFS, and content abstractions where they already fit.
- Keep runtime file access behind the VFS and `IFileProvider`; build-time tools may use File I/O.
- Write code and documentation in English.
- Add or update concise XML summaries and comments for non-obvious behavior.

## Validation

Use the narrowest useful check first. For runtime changes, the default fallback is:

```powershell
dotnet build .\KittyCat\KittyCat.DesktopGL\KittyCat.DesktopGL.csproj --configuration Debug
```

Use the WindowsDX build when platform-specific code is affected. Android and iOS require
platform workloads and, for iOS, Apple build/signing infrastructure.
