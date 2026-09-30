# KittyCat AI Code Conduct

## Response Style

- Respond in the language used by the user.
- Keep responses short, direct, and useful.
- Optimize for information per token and avoid unnecessary verbosity.
- Do not add filler, repetition, or unsolicited explanations.
- Explain complex decisions or implementation details only when explicitly requested or needed to avoid a wrong change.
- Ask a question only when the requirement is genuinely ambiguous or blocks implementation.

## Language

- Use English in source code, identifiers, XML documentation, comments, commit-sized change descriptions, and repository documentation.

## Architecture and Composition

- The framework is DI-driven. Modules expose registrations and are composed through the host and engine.
- ECS is a core framework capability; worlds, systems, signatures, factories, and ECS modules belong to the ECS/engine composition model.
- Framework modules must remain modular and optional rather than being hard-wired together.
- The engine is the composition boundary for selecting and connecting framework modules.
- Modules that are not needed by most games may be disabled by default, but the engine must provide an explicit opt-in path.
- Keep lower-level Toolkit and ECS code independent from game-specific code.

## Engineering Style

- Keep code simple, readable, universal, modular, and reusable.
- Avoid enterprise patterns, ceremony, and abstractions unless they are clearly necessary for the game, engine, or framework.
- Prefer the smallest solution that has clear ownership and can be tested.
- Reuse existing project conventions and APIs before creating new infrastructure.
- Protect performance: consider allocations, frame time, startup time, I/O, update/draw frequency, and hot paths.
- Prefer measurable or reasoned optimizations over speculative complexity.
- Preserve dependency direction and public API stability unless the task explicitly changes them.
- Do not refactor unrelated code or overwrite user changes.

## Documentation

- Add XML summaries for public framework and engine APIs when they improve discoverability.
- Add concise English comments for non-obvious lifecycle, platform, performance, or file-format decisions.
- Keep comments accurate and update them when behavior changes.
- Prefer documentation that helps a future developer navigate the code over comments that restate syntax.

## Change Workflow

1. Inspect the local owning code path and its nearest usage or test.
2. Form a concrete hypothesis about the requested behavior.
3. Make the smallest focused edit.
4. Run the cheapest relevant validation, then broaden validation only when needed.
5. Report only the change, validation result, and any real remaining limitation.

## Personal Local Instructions

- Personal workspace-only instruction files may be added as `.github/instructions/*-local.instructions.md`.
- These files are ignored by Git and are not shared with other contributors.
- Each file should use `applyTo` to define the files or requests it affects.
