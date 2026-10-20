# Renderer usage examples

These are alternative setups, not registrations to apply all at once. They assume
the game uses `UsePurrplingCore()`, which supplies the main `Screen` and graphics
services. Put service registrations in an
`IServiceModule.ConfigureServices(IServiceCollection services)` implementation.
The shared imports and example types are defined at the end of this page.

| Setup | Renderer owner | Frame driver |
| --- | --- | --- |
| [Global renderer](#global-renderer) | Application DI container | One `RendererGameComponent` |
| [World-owned renderer](#world-owned-renderer) | Each world instance | That world's lifecycle |
| [Shared renderer in multiple worlds](#shared-renderer-in-multiple-worlds) | Application DI container | One global `RendererGameComponent` |
| [Manual renderer](#manual-renderer) | Application DI container | Your controller |

## Global renderer

Use this for a HUD, menu, scene, or other drawing that does not belong to one world:

```csharp
services.AddRenderer("Screen")
    .AddLayer(ExampleLayers.Hud)
    .AddDrawComponent(DrawStage.Screen);
```

Global producers can inject `[FromKeyedServices("Screen")] RenderContext context`.
During their draw callback, submit commands to the configured layer:

```csharp
context.Batch(ExampleLayers.Hud).Draw(texture, position, Color.White);
```

Here `texture` is an already loaded texture and `position` is a `Vector2`.
The producer must draw before the renderer component's `DrawStage.Screen` order
(`3000`). The component initializes the renderer once, calls `BeginFrame` on the
game's `BeginDraw` event, and calls `Flush` in its own `Draw` method. The application
container disposes the renderer. Do not also initialize, reset, or flush it from
each producer.

## World-owned renderer

Use this when each world instance needs independent queues, cameras, targets, and
renderer lifetime:

```csharp
services.AddWorld()
    .AddService<Camera2D>()
    .AddRenderer<WorldRendering>()
    .AddModule<ExampleWorldModule>();

services.AddWorldController();
```

`AddWorldController()` creates and drives this default world through a game
component. A world created by another controller can use the same registration
without adding this default controller.

The configuration module is created inside the world's scope, so it can receive
that world's camera:

```csharp
public sealed class WorldRendering(Camera2D camera) : IRendererConfiguration
{
    public void Configure(RendererOptions options)
    {
        options.AddLayer(ExampleLayers.Sprites, new RenderLayerOptions
        {
            Camera = camera,
            SyncCameraViewport = true
        });
    }
}
```

World draw systems inject an ordinary, unkeyed `RenderContext`, as in
`ExampleDrawSystem` below. `WorldRenderer` initializes during `World.Initialize`,
calls `BeginFrame` before the world's draw systems, and calls `Flush` after them.
The world scope disposes it. Each world instance gets its own renderer, even when
instances use the same marker.

Do not add `.AddDrawComponent()` for a world-owned renderer or call its frame
methods from individual systems. A separate global HUD renderer can coexist with
it and has its own frame driver.

## Shared renderer in multiple worlds

Register one global renderer and alias only its nondisposable `RenderContext` into
each world. Add the global frame driver exactly once:

```csharp
services.AddRenderer("SharedWorld")
    .AddLayer(ExampleLayers.Sprites)
    .AddDrawComponent(DrawStage.World);

services.AddWorld<FirstWorld>()
    .AddService<RenderContext>(ResolveSharedContext)
    .AddModule<ExampleWorldModule>();

services.AddWorld<SecondWorld>()
    .AddService<RenderContext>(ResolveSharedContext)
    .AddModule<ExampleWorldModule>();
```

Use this factory method on the service-module class:

```csharp
private static RenderContext ResolveSharedContext(IServiceProvider provider) =>
    provider.GetRequiredKeyedService<RenderContext>("SharedWorld");
```

`AddWorld<TMarker>()` registers factories; it does not create or schedule these
worlds. Your existing controller or scene must create and initialize both worlds,
then draw them before the renderer component's `DrawStage.World` order (`1000`).
For example, world-driving game components with the default `DrawOrder` (`0`)
draw before this renderer. `AddWorldController()` alone drives only the default
world, not both named markers.

The frame sequence is:

```text
Game.BeginDraw -> shared renderer.BeginFrame()
FirstWorld.Draw -> submit into the shared context
SecondWorld.Draw -> submit into the same context
RendererGameComponent.Draw -> shared renderer.Flush()
```

Both worlds use the same unkeyed `RenderContext` injection and draw-system code.
Neither world owns the renderer, resets its queue, or flushes it. Disposing one or
both worlds leaves the shared renderer alive; the application container owns its
disposal.

Do not add `.AddRenderer<WorldRendering>()` to these worlds, wrap the shared
renderer in `WorldRenderer`, or alias the disposable `IRenderer` through
`AddService<IRenderer>`. Share the context, not renderer ownership.

Sharing a renderer also shares its layer configuration, cameras, and output.
Configure every layer used by either world on that global renderer. Use separate
world-owned renderers when worlds need independent rendering configuration.

## Manual renderer

Omit `.AddDrawComponent()` only when your controller will drive the renderer:

```csharp
services.AddRenderer("Manual")
    .AddLayer(ExampleLayers.Sprites);
```

Resolve and initialize it once after the graphics device is available:

```csharp
var renderer = provider.GetRequiredKeyedService<IRenderer>("Manual");
renderer.Initialize(graphicsDevice);
```

For each frame, begin once before all producers and flush after all of them:

```csharp
renderer.BeginFrame();
renderer.Context.Batch(ExampleLayers.Sprites)
    .Draw(texture, position, Color.White);
renderer.Flush();
renderer.Flush();
```

Here `provider` is the application service provider, `graphicsDevice` is its live
graphics device, and `texture` is an already loaded texture. Both flushes replay
the same submissions. Neither `Batch` nor `Flush` clears the queue; only the next
`BeginFrame` discards it. DI still owns disposal of this registered renderer.

For an automatically driven renderer, submit during the draw phase, not during
`Update`: the subsequent game `BeginDraw` event clears earlier submissions. Do not
load assets inside update or draw loops.

## Shared imports and example types

Use these imports for the snippets above:

```csharp
using Friflo.Engine.ECS.Systems;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PurrplingCore.Ecs;
using PurrplingCore.Ecs.DI;
using PurrplingCore.Ecs.Systems;
using PurrplingCore.Ecs.WorldBuilding;
using PurrplingCore.Rendering;
using PurrplingCore.Toolkit.Graphics;
using PurrplingCore.Toolkit.Hosting;
using PurrplingCore.Toolkit.Rendering;
```

The world-owned and shared-world registrations use these example types. Replace
the module's draw-system registration with your game's systems as needed:

```csharp
public static class ExampleLayers
{
    public static readonly RenderLayerId Sprites = RenderLayerId.Of("Example.Sprites");
    public static readonly RenderLayerId Hud = RenderLayerId.Of("Example.Hud");
}

public sealed class FirstWorld : IWorldMarker;
public sealed class SecondWorld : IWorldMarker;

public sealed class ExampleWorldModule : IWorldModule
{
    public int Order => 0;

    public void Setup(IWorldBlueprint world) => world.AddSystem<ExampleDrawSystem>();
}

[PurrplingCore.Ecs.Attributes.System<DrawSystemGroup>]
public sealed class ExampleDrawSystem(
    RenderContext context,
    IGraphicsDeviceService graphics) : BaseSystem
{
    protected override void OnUpdateGroup()
    {
        context.Batch(ExampleLayers.Sprites).FillRect(
            graphics.GraphicsDevice, new Rectangle(8, 8, 32, 16), Color.White);
    }
}
```

The framework runs this system through `DrawSystemGroup` during `World.Draw`.
Its unkeyed context comes from the world-owned renderer in the second setup, or
from the global alias in the shared setup. Both shared worlds draw the same sample
rectangle; game-specific systems can submit different content through that same
context.

See the [rendering guide](README.md) for screen sizing, layers, passes, and cameras.