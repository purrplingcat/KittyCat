# Particles

Particles are part of the engine. The reusable foundation lives in Toolkit; ECS ownership and lifecycle live in the engine. A game adds its own small wrapper for effect recipes and drawing. There is no separate package to install and no required renderer.

| Layer | Types | Responsibility |
| --- | --- | --- |
| `PurrplingCore.Toolkit.Particles` | `ParticleEmission`, `ParticleState` | Initial settings, motion, lifetime, tint, and scale; no engine, ECS, DI, or graphics device required. |
| `PurrplingCore.Toolkit.Particles` | `ParticleRenderer2D` | Optional static sprite helper for `LayerWriter`; independent of ECS. |
| `PurrplingCore.Ecs.Particles` | `Particle`, `ParticleModule`, `ParticleUpdateSystem` | Store the state in entities, advance simulation, and delete expired entities. |
| `PurrplingCore.Ecs.Particles` | `EmitParticle`, `ParticleDrawSystem` | Deferred emission and a world-owned query with an optional layered-rendering helper. |
| Game code | Your effects and draw wrappers | Choose when and what to emit, renderer keys, layers, cameras, assets, or an entirely different drawing backend. |

## Start With ECS

Register the simulation module and your draw wrapper in the game's existing composition:

```csharp
using Microsoft.Extensions.DependencyInjection;
using PurrplingCore.Ecs;
using PurrplingCore.Ecs.DI;
using PurrplingCore.Ecs.Particles;
using PurrplingCore.Rendering;
using PurrplingCore.Toolkit.Rendering;

services.AddWorld(ConfigureWorld).AddParticleModule();

// Only needed when the wrapper chooses the layered renderer.
services.AddRenderer("World")
    .AddLayer(GameParticleLayers.Effects)
    .AddDrawComponent(DrawStage.World);

static void ConfigureWorld(IWorldBlueprint world)
{
    world.AddSystem<GameParticleDrawSystem>();
    // Add your gameplay and emission systems here.
}
```

`AddParticleModule()` only installs simulation. It does not register a renderer, select a layer, load textures, or emit particles. The existing world controller or your own world owner calls `Initialize`, `Update`, and `Draw`; enable `AddWorldController()` when using the game-component integration. No particle-specific factory or world-composition changes are needed.

## Emit From Gameplay

Use the current ECS system's `CommandBuffer`. A game recipe can be this small:

```csharp
using Friflo.Engine.ECS;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PurrplingCore.Ecs.Particles;
using PurrplingCore.Toolkit.Particles;

public sealed class GameParticleEffects
{
    public void Spark(CommandBuffer commands, Texture2D texture, Vector2 position)
    {
        commands.EmitParticle(new ParticleEmission
        {
            Texture = texture,
            Position = position,
            Velocity = new Vector2(0f, -60f),
            Acceleration = new Vector2(0f, 120f),
            Lifetime = 0.6f,
            StartColor = Color.Gold,
            EndColor = new Color(255, 215, 0, 0),
            StartScale = 2f,
        });
    }
}
```

Call `effects.Spark(CommandBuffer, texture, position)` from your gameplay system. The entity appears at command-buffer playback, after the current system-group iteration. `EmitParticle` returns the queued entity ID when you need to attach additional components or tags using the same buffer. Do not look up that entity before playback.

Emission timing, bursts, randomness, capacity limits, and game-specific recipes belong in the game wrapper. The framework does not invent a global emitter, particle pool, or singleton store.

## Choose Your Drawing Backend

### Layered Renderer

The wrapper chooses its context and layer. This is the complete draw adapter:

```csharp
using Microsoft.Extensions.DependencyInjection;
using PurrplingCore.Ecs.Attributes;
using PurrplingCore.Ecs.Particles;
using PurrplingCore.Ecs.Systems;
using PurrplingCore.Toolkit.Rendering;

public static class GameParticleLayers
{
    public static readonly RenderLayerId Effects = RenderLayerId.Of("Particles");
}

[System<DrawSystemGroup>]
public sealed class GameParticleDrawSystem(
    [FromKeyedServices("World")] RenderContext context) : ParticleDrawSystem
{
    protected override void OnUpdate()
    {
        var writer = context.Batch(GameParticleLayers.Effects);
        Draw(in writer);
    }
}
```

The inherited `Draw` helper only reads the query and queues sprites. It skips expired particles, particles without a texture, and inactive layers. The renderer's existing camera, culling, sorting, blending, and flush path still apply. Particle rendering has no DI service registration of its own.

Configure a camera or blend mode on the effects layer in the game's renderer configuration, not in `ParticleModule`. Submit before that renderer's flush in the same frame. See [2D rendering](../../Rendering/README.md) for renderer setup and draw ordering.

### SpriteBatch Or Another Backend

The same base class also works without `RenderContext` or a registered layered renderer. Override `OnUpdate` and use the world-owned `Query` directly. For example, this alternative wrapper owns its immediate SpriteBatch pass:

```csharp
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PurrplingCore.Ecs.Attributes;
using PurrplingCore.Ecs.Particles;
using PurrplingCore.Ecs.Systems;

[System<DrawSystemGroup>]
public sealed class SpriteParticleDrawSystem(SpriteBatch sprites) : ParticleDrawSystem
{
    protected override void OnUpdate()
    {
        sprites.Begin(blendState: BlendState.AlphaBlend);
        foreach (var (particles, entities) in Query.Chunks)
        {
            for (var index = 0; index < entities.Length; index++)
            {
                ref readonly var state = ref particles[index].State;
                var texture = state.Texture;
                if (!state.IsAlive || texture is null) continue;

                var tint = state.Tint;
                sprites.Draw(texture, state.Position, state.Source,
                    Color.FromNonPremultiplied(tint.R, tint.G, tint.B, tint.A),
                    state.Rotation, new Vector2(state.Source.Width * 0.5f, state.Source.Height * 0.5f),
                    state.Scale, SpriteEffects.None, state.Depth);
            }
        }
        sprites.End();
    }
}
```

Register your existing `SpriteBatch` with DI and add this system instead of `GameParticleDrawSystem`. Do not register `AddRenderer` for this path. The game controls the graphics target, camera transform, pass ordering, and SpriteBatch lifetime; do not nest this pass inside another active SpriteBatch pass. If drawing is owned by another subsystem, forward state values to that subsystem instead. A primitive-only backend can leave `Texture` unset.

For simulation only, register `AddParticleModule()` and omit the draw wrapper altogether.

## Use Toolkit Without ECS

The same simulation is available directly:

```csharp
using Microsoft.Xna.Framework;
using PurrplingCore.Toolkit.Particles;

var particle = new ParticleState(new ParticleEmission
{
    Position = new Vector2(20f, 30f),
    Velocity = new Vector2(0f, -60f),
    Acceleration = new Vector2(0f, 120f),
    Lifetime = 0.6f,
});

bool alive = particle.Update(1f / 60f);
// The owner removes the particle when alive is false.
```

Your array, list, or pool owns storage and update timing. No `World`, `EntityStore`, host, DI container, or graphics device is required. A low-level owner that chooses the layered renderer may call `ParticleRenderer2D.Draw(in particle, in writer)` for each state. Other backends read `Position`, `Rotation`, `Tint`, `Scale`, and their own additional data.

## State And Lifetime

- `ParticleEmission` defaults to a one-second lifetime, white-to-transparent tint, and scale one-to-zero. `Texture` and `Source` are optional; an omitted source uses the texture bounds, or an empty rectangle without a texture.
- `ParticleState.Update` advances age, constant-acceleration movement, velocity, and rotation. It returns false at `Age >= Lifetime`, before moving an expired particle. Tint and scale are calculated on demand; expired states use their end values.
- Supply a finite positive lifetime and finite motion values. Pass finite, nonnegative elapsed seconds. Constructors and low-level update do not validate every particle; `ParticleUpdateSystem` validates elapsed time once per store update.
- ECS data lives in `Particle.State`. To expire an existing particle, set `particle.State.Age = particle.State.Lifetime`; the next particle update deletes the entity through its command buffer. Drawing skips it immediately.
- Textures are borrowed. Particle deletion and state updates never dispose textures. Load or obtain assets outside the per-particle hot path and keep them valid until their queued drawing has flushed. `Tint` is straight-alpha; the layered helper converts it to the premultiplied tint expected by `AlphaBlend`.

Queries belong to their world. Friflo provides `QuerySystem.Query` during `OnUpdate`; do not export it to a singleton renderer or use it after the active query iteration. Keep draw systems in normal world composition. `ParticleRenderer2D` is stateless and retains no world, query, texture, or state. Update and drawing use indexed chunk loops and create no per-particle delegates or steady-state allocations.

## KittyCat Example

[StarCatcherParticleEffects](../../../KittyCat/KittyCat.Game/Services/StarCatcherParticleEffects.cs) owns burst and trail recipes and calls `EmitParticle`. [StarCatcherParticleSystem](../../../KittyCat/KittyCat.Game/Ecs/Systems/StarCatcherParticleSystem.cs) supplies trail timing. [StarCatcherParticleDrawSystem](../../../KittyCat/KittyCat.Game/Ecs/Systems/StarCatcherParticleDrawSystem.cs) is the small wrapper selecting the world renderer and effects layer. The game's restart system expires existing particle states; the module handles deletion.