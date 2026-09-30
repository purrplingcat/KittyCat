---
description: "Use whenever working with Friflo.Engine.ECS or ECS concepts, worlds, systems, components, queries, or performance in PurrplingCore.Ecs and KittyCat.Game. Consult relevant official documentation and verify APIs against the package version used by the project."
applyTo: "PurrplingCore.Ecs/**/*.cs, PurrplingCore/Ecs/**/*.cs, KittyCat/KittyCat.Game/Ecs/**/*.cs, KittyCat/KittyCat.Game/Configuration/**/*.cs"
---

# Friflo ECS Instructions

- For every task involving ECS/Friflo code, consult the relevant official [Friflo.Engine.ECS documentation](https://friflo.gitbook.io/friflo.engine.ecs), including conceptual and lifecycle guidance where relevant. Prefer documented behavior over assumptions from general ECS knowledge.
- Check the `Friflo.Engine.ECS` package version in the owning project before using a documented API. Documentation can describe features newer than this repository's package; confirm the API is available in the installed version.
- For query hot paths, prefer enumerating `query.Chunks` and indexing the component chunks over `query.Entities`. Friflo documents entity enumeration as significantly slower and suitable only for very small result sets (under 10 entities).
- `ForEachEntity` is concise, but its callback has per-result execution overhead. Replace it in a hot path only when appropriate, and preserve query semantics when using chunk loops.
- Do not perform structural entity/component changes while enumerating query chunks. Use the appropriate command-buffer approach for structural changes.
- Treat update, fixed-update, and draw queries as performance-sensitive. Avoid creating queries inside per-tick loops; retain and reuse queries when their store lifetime matches the owning system.