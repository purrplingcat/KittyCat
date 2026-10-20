# 2D rendering

The 2D renderer is optional. Draw systems, scenes, and ordinary objects submit data to layer passes; the renderer prepares and executes those passes only when it is flushed. Each pass owns its data and rendering strategy. The default `DrawCommandPass` accepts sprite, text, and primitive commands, while custom passes may accept any struct or render without queued data. The core types live in `PurrplingCore.Toolkit.Rendering`; DI integration and the optional game component live in `PurrplingCore.Rendering`. Neither ECS nor `SceneManager` is required to use the renderer.

See [renderer usage examples](EXAMPLES.md) for global, world-owned, shared-world, and manually driven renderers, including frame ownership and draw ordering.

The [particle system](../Ecs/Particles/README.md) can use this renderer through a small game-owned draw wrapper. Particle simulation itself does not require a renderer; the low-level Toolkit sprite helper can also draw particle states without ECS.

## Choose an execution mode

| Mode | Register | Who calls `Initialize` and `Flush`? |
| --- | --- | --- |
| World-owned renderer | `AddWorld(...).AddRenderer<WorldRendering>()` | `WorldRenderer` initializes it, begins each frame before draw systems, and flushes afterward |
| Automatic renderer | `AddRenderer(key).AddLayer(...).AddDrawComponent()` | `RendererGameComponent`, via `Game.Components` |
| Manual renderer via DI | `AddRenderer(key)` | Your controller calls `Initialize(device)` and `Flush()` |
| No DI integration | `new Renderer2D(name, options)` | Your code owns initialization, flushing, and disposal |

Renderer identity is a keyed-service key of type `object`. Use the same key for `AddRenderer`, `.AddModule<T>()`, and `[FromKeyedServices(key)] IRenderer`. Strings, enums, and other stable objects are supported. A configuration class is only a DI module, never the renderer marker. There is no global renderer order or group: each renderer is initialized and flushed by its own controller, component, or manual draw path. `AddRenderer(key)` only registers the service; it does not schedule rendering. Add `.AddDrawComponent()` to register a component that initializes and flushes it; the default order is `DrawStage.Default` (`0`). Pass a custom `drawOrder` when it must run after a particular submitter. Each call adds another component; a different order does not replace an earlier registration. Without it, your own controller must initialize and flush the renderer. The lower-level `services.AddRendererGameComponent(key, drawOrder)` remains available for advanced service registration.

The flow is:

```text
owner calls renderer.BeginFrame() -> previous submissions and text are cleared
producer.Draw() -> renderer.Context.Batch<T>(id).Submit(...) -> pass-owned submissions
RendererGameComponent.Draw() or your own Draw() -> renderer.Flush()
```

The owner calls `BeginFrame()` before producers submit a new frame. Neither `Batch` nor `Flush` begins a frame. A flush retains submissions and text, so the same frame can be replayed until the next `BeginFrame()`. For world-owned rendering, `WorldRenderer` pairs this reset with flushing through the world's drawing lifecycle. Other rendering paths choose their boundary in the code that owns their submissions, not in the main game class unless it actually draws through that renderer.

`Flush` never initializes GPU resources: call `Initialize` first. `Initialize` binds the renderer to that device; every subsequent call throws. A `RendererGameComponent` does this during its `Initialize` lifecycle; a manually driven renderer is your responsibility. Flushing before initialization throws `InvalidOperationException`.

```csharp
renderer.BeginFrame();
renderer.Context.Batch(Layers.Sprites).Draw(texture, position, Color.White);
renderer.Flush();
renderer.Flush();
```

Both flushes replay the same queue. Submitting more data between flushes appends to that queue; call `BeginFrame()` only when the existing data should be discarded. Each flush reruns preparation, passes, post-processing, and composition; it does not merely copy a cached final image. Custom passes must retain their submissions in `Prepare` and `Execute`, and discard them only in `ClearSubmissions`. Keep referenced textures, fonts, and buffers alive through the last replay. Replaying with another camera does not rerun submission-time culling.

## World-owned renderer and services

Use `AddRenderer<TConfiguration>()` on an `IWorldServicesBuilder` to attach a renderer to each instance of that world. `TConfiguration` implements `IRendererConfiguration` and is created through DI inside the world's scope. It can therefore depend on world-owned services, such as a camera. The `WorldRendering` module and `SpriteDrawSystem` used here are defined below.

```csharp
using Friflo.Engine.ECS.Systems;
using Microsoft.Extensions.DependencyInjection;
using PurrplingCore.Ecs;
using PurrplingCore.Ecs.Attributes;
using PurrplingCore.Ecs.DI;
using PurrplingCore.Ecs.Systems;
using PurrplingCore.Ecs.WorldBuilding;
using PurrplingCore.Rendering;
using PurrplingCore.Toolkit.Graphics;
using PurrplingCore.Toolkit.Hosting;
using PurrplingCore.Toolkit.Rendering;

public sealed class ExampleWorldServices : IServiceModule
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddWorld(ConfigureWorld)
            .AddService<Camera2D>()
            .AddRenderer<WorldRendering>();
        services.AddRenderer(RendererKey.Screen)
            .AddLayer(Layers.Hud)
            .AddDrawComponent(DrawStage.Screen);
        services.AddWorldController();
    }

    private static void ConfigureWorld(IWorldBlueprint world)
    {
        world.AddSystem<SpriteDrawSystem>();
    }
}
```

World draw systems inject an ordinary, unkeyed `RenderContext`. The scoped `WorldRenderer` owns its `IRenderer` and exposes the same context. It keeps lifecycle integration together: initialize during `World.Initialize`, begin a new frame through `OnDrawing` before all draw systems, flush through `OnDrawn` afterward, and dispose with the world scope. No game callback or begin-frame helper system is needed. Additional `Flush` calls replay the retained frame; the next `World.Draw` begins a new frame. The underlying renderer still supports explicit `BeginFrame` in manually driven paths. Do not also add a renderer game component or initialize it yourself. Repeated registration of the same configuration module is deduplicated per marker. One world owns one renderer; multiple distinct configuration modules contribute to that renderer.

The independent screen renderer has a separate frame boundary. The main game class does not manage that renderer. The world's `WorldRenderer` only begins and flushes its own renderer and does not reset the global screen renderer.

The globally keyed screen renderer remains independent, so UI or scenes can render without any world. World-owned renderers do not join the application-wide `IEnumerable<IRenderer>`. Two worlds, including two instances of the same marker, have separate render queues and GPU resources. They may share the engine's main `Screen` and `GraphicsDevice`; disposing one world does not dispose those shared services or another renderer. Worlds without `AddRenderer` do not require graphics services.

`AddService<TService>()`, `AddService<TService, TImplementation>()`, and the factory overload provide the same world lifetime for arbitrary services. Constructor injection selects the implementation registered for the current marker; another world instance gets its own value, and the world scope disposes disposable services once. The unkeyed resolver uses `TryAdd`, so an existing application registration is preserved instead of rejected. A later application registration can override it using normal DI precedence. Consumers and modules then receive that application service, whose lifetime and disposal remain controlled by its registration. Global renderers use keyed contracts and are unaffected.

Group registrations in an `IWorldFeature.ConfigureServices(IWorldServicesBuilder)` implementation and opt in with `AddFeature<TFeature>()` (see [world features and options](../../PurrplingCore.Ecs/DI/README.md)). Features run immediately during service registration and are deduplicated per feature type and marker; they are not runtime service modules. Register blueprint modules with `AddModule<TModule>()` or `AddModule(factory)`. All blueprint modules, including `Configure` callback wrappers, are scoped to the world and may inject world-owned services. Register a type or a per-world factory, not a shared module instance. `DeriveWorld<TMarker>()` inherits blueprint modules, not service registrations: register their required services/features on the derived marker too.

## Logical screens and split-screen

`PurrplingCore.Toolkit.Graphics.Screen` describes a logical size and relative regions, not a GPU target. Use `Screen(Point)` for an independent fixed size or `ResolutionScreen(Resolution)` to follow an applied logical resolution. Only `ResolutionScreen` owns a resolution-event subscription and needs disposal. Regions inherit a fraction of their parent's size and are placed inside the parent's presented image, including letterboxing:

```csharp
using var main = new ResolutionScreen(graphics.Resolution);
var left = main.CreateRegion(0f, 0f, 0.5f, 1f);
var right = main.CreateRegion(0.5f, 0f, 0.5f, 1f);

var options = new RendererOptions(left);
options.AddLayer(Layers.Sprites);
var renderer = new Renderer2D("Left", options);
```

Regions may be nested. Adjacent regions round shared boundaries consistently, including odd output sizes. `RendererOptions.ScaleMode` controls parent-image placement, child-region mapping, internal layer composition, and final image scaling, defaulting to `Fit`. The same mode is used throughout the screen hierarchy so regions align with the parent image's letterboxing. Renderer configuration does not change the shared screen. Presentation math lives in `PurrplingCore.Toolkit.Rendering.ScreenLayout`, not in `Screen`:

```csharp
var layout = ScreenLayout.Calculate(screen, outputBounds, options.ScaleMode, options.Alignment);
```

`layout.Size` is logical. `layout.Bounds` is the available presentation area, not necessarily the scaled image rectangle; `layout.Clip` is the output-pixel clip inherited from ancestors. For a root screen, bounds and clip equal `outputBounds`, even when those bounds are larger than the logical size. Scaling changes presentation, not the logical coordinate space.

`CreateRegion` offsets are fractions of the parent's dimensions and may be negative or extend beyond the parent. Width and height must be finite positive fractions and may exceed one. For example, `CreateRegion(-0.25f, 0f, 1f, 1f)` keeps the full logical size and shifts the region left by a quarter of the parent image's width. Rendering outside the output or ancestor regions is clipped.

`RendererOptions.Alignment` independently positions the scaled image, defaulting to `RenderAlignment.Center`. It supports the center, four edges, and four corners. Alignment also applies to ancestor-image placement when resolving regions; internal layer composition stays centered. For an integer-scaled HUD at the physical top edge and horizontal center, keep the root screen and configure:

```csharp
options.Output = RenderTargetSpec.MatchScreen();
options.ScaleMode = RenderScaleMode.IntegerFit;
options.Alignment = RenderAlignment.TopCenter;
```

The world renderer may independently use `Fit` and the default `Center`. A HUD using a region aligns within that region, not the entire window. When `RendererOptions.Output` is `MatchPresentation()`, the renderer's output already matches its presentation bounds, so final composition does not enlarge it. A layer's `Target = MatchPresentation()` does not guarantee 1:1 presentation: that layer is composed into the renderer output first, which may have a different resolution.

`UsePurrplingCore()` registers `Screen` with the `ResolutionScreen` implementation as an engine game service, bound to the engine's `Resolution`, even when no renderer is registered. A pre-registered main screen is preserved. `Screen` is required on `RendererOptions`, including when the output target uses `MatchPresentation()`. `AddRenderer(key)` creates options with the main screen only when its keyed renderer is first resolved and an `IServiceProvider` is available. It then applies registered configuration modules before constructing the renderer. Select a custom screen directly in configuration:

```csharp
services.AddRenderer("Left", ConfigureLeftRenderer)
    .AddDrawComponent();

static void ConfigureLeftRenderer(RendererOptions options)
{
    options.Screen = options.Screen.CreateRegion(0f, 0f, 0.5f, 1f);
    options.AddLayer(Layers.Sprites);
}
```

`AddRenderer(key, callback)` registers the callback through `DelegateRendererModule`, just like world configuration callbacks. It does not invoke it during service registration. `.Configure(callback)`, `.AddLayer(...)`, and `.AddModule<T>()` all contribute to the same deferred pipeline in registration order. The builder deliberately has no `Options` property: options do not exist until the renderer is resolved. Both callbacks and DI-created `IRendererConfiguration` modules receive initialized options and may explicitly assign `options.Screen`; there is no separate keyed-screen lookup or screen-selection property on modules. Outside engine composition, register a main `Screen` service before resolving registered renderers.

For direct construction, use `new RendererOptions(screen)` or an object initializer setting the required `Screen`, then `new Renderer2D(name, options)`. World and HUD renderers may share the same screen without sharing render targets. `RendererOptions.Output` defaults to `MatchScreen()`, so each renderer owns a screen-sized offscreen target; clears affect that private target and final presentation is clipped to the screen and its ancestors.

`ResolutionScreen` caches the logical size and updates it through `Resolution.ResolutionChanged`, raised after `ApplyChanges()` and handled client-size changes. `SetResolution()` alone stages the change. In the default fixed virtual mode, a window resize changes the physical backbuffer without changing the logical size. In responsive virtual mode, a changed output aspect ratio also changes the logical size. In `MatchClient` mode, logical size copies the current client dimensions in every display mode. A fixed `Screen` has no resolution binding, and child regions derive their size from their parent. Choose the screen source during configuration; there is no method that switches an existing screen between fixed and resolution-bound modes.

Screen layout is sampled at initialization and on every explicit `BeginFrame`, before camera synchronization. Applied changes affect targets and cameras on the next `BeginFrame`. Replaying without a reset keeps the sampled layout even if `OutputOverride` changes. Use `renderer.Screen.Size` for the screen's current logical size; `MatchScreen()` uses the frame's sampled logical size, and `MatchPresentation()` uses its sampled presentation bounds. The screen never changes global `Resolution` or allocates GPU resources. The renderer only reads the screen and output dimensions; it does not subscribe to device-reset events or manage the window. DI disposes its shared `ResolutionScreen`; callers dispose manually owned `ResolutionScreen` instances. Fixed screens and regions require no disposal, and the renderer never owns their lifetime. Automatic and scene-owned renderers use exactly the same sizing and presentation code.

### Virtual resolution modes

`Resolution.Mode` selects windowed, fullscreen, or borderless display behavior. `Resolution.VirtualMode` independently selects how the logical coordinate space follows the physical output or window client area:

| Virtual mode | Logical sizing |
| --- | --- |
| `VirtualResolutionMode.Fixed` | Keeps the configured reference width and height. This is the default. |
| `VirtualResolutionMode.Responsive` | Keeps the reference size as a minimum and expands one axis to follow the backbuffer aspect ratio. |
| `VirtualResolutionMode.MatchClient` | Copies `GameWindow.ClientBounds.Size`, including in exclusive fullscreen. |

Configure the reference size and mode through the existing fluent setup, before rendering starts:

```csharp
resolution
    .SetResolution(640, 480)
    .SetVirtualMode(VirtualResolutionMode.Responsive)
    .ApplyChanges();
```

The reference is stored independently of the calculated logical size. A resize is always calculated from the reference, not the previous responsive result. `SetResolution` retains its existing role in requesting the windowed or exclusive-fullscreen backbuffer dimensions; `ApplyChanges` uses the configured reference dimensions, not the expanded logical dimensions. Borderless mode uses the window bounds. The responsive calculation runs after applying the graphics settings and after a handled windowed resize, using the actual backbuffer dimensions. Minimized windows reporting an empty client area are ignored.

For a 640-by-480 reference:

| Backbuffer | Responsive logical size | Fixed logical size |
| --- | --- | --- |
| 640 by 480 | 640 by 480 | 640 by 480 |
| 1280 by 720 | 853 by 480 | 640 by 480 |
| 1920 by 1080 | 853 by 480 | 640 by 480 |
| 800 by 1000 | 640 by 800 | 640 by 480 |
| 320 by 240 | 640 by 480 | 640 by 480 |

The expanded axis is rounded to the nearest logical pixel. A larger output with the same aspect ratio does not add logical coordinates; a wider or taller aspect ratio does. This is not native-pixel sizing and not a fixed-height policy: portrait outputs can expand the logical height. Renderer scaling remains controlled separately by `ScaleMode`; use `Fit` for proportional fractional scaling or `IntegerFit` when integer enlargement and possible borders are desired. Very extreme aspect ratios can produce large logical targets, which remain subject to GPU target-size limits.

`ResolutionScreen`, its child regions, `MatchScreen()` targets, and synchronized camera viewports follow the responsive logical size on the next frame. An independent `Screen(Point)` does not. In particular, constructing a new fixed screen from the main screen's current dimensions creates a snapshot, not a responsive binding. UI anchors and gameplay bounds must adapt explicitly to changing logical size; this mode does not modify entities, positions, or camera zoom.

For logical coordinates that follow client dimensions directly, use:

```csharp
resolution.SetVirtualMode(VirtualResolutionMode.MatchClient).ApplyChanges();
```

This mode has no reference-size minimum or aspect-ratio calculation. A client area of 1280 by 720 produces a logical size of 1280 by 720, while 320 by 240 produces 320 by 240. The source is specifically `GameWindow.ClientBounds.Size`, not `PresentationParameters` or a renderer's presentation bounds. If the platform reports client and backbuffer sizes differently, this mode follows the client size.

To select exclusive fullscreen explicitly:

```csharp
resolution
    .SetVirtualMode(VirtualResolutionMode.MatchClient)
    .SetMode(ResolutionMode.FullScreen)
    .ApplyChanges();
```

`ApplyChanges()` copies the client dimensions after applying the display mode. Later positive client-size events also update logical size in fullscreen and borderless modes without calling `GraphicsDeviceManager.ApplyChanges()` again. In windowed mode, the handler continues resizing the backbuffer as before. Events during an ongoing apply are ignored, and zero-sized minimized client areas keep the last valid logical size. `SetResolution` still controls the requested display size and fixed/responsive reference; `MatchClient` does not select a monitor display resolution itself. The standard `GraphicsManager.ToggleFullScreen()` switches to borderless, not exclusive fullscreen.

Keep the main `ResolutionScreen` for renderers that should follow these updates; independently constructed fixed screens remain unchanged. `MatchClient` changes logical coordinate dimensions. `RenderTargetSpec.MatchPresentation()` is a separate target-sizing choice that follows each renderer's presentation region, which can differ from the window client area.

Return to the configured reference size with:

```csharp
resolution.SetVirtualMode(VirtualResolutionMode.Fixed).ApplyChanges();
```

## Configuration recipes

These are alternative configurations, not blocks to apply one after another. Each options block belongs in `Configure(RendererOptions options)` of an `IRendererConfiguration` module. Unless reassigned, `options.Screen` is the main resolution-backed screen supplied by DI, and the examples assume the default fixed virtual mode. Enable responsive mode as described above when the logical space should adapt to the output aspect ratio. The `Layers` IDs are defined in [Automatic rendering with DI](#automatic-rendering-with-di); import `Microsoft.Xna.Framework.Graphics` for sampler settings. Where a camera is used, inject a `Camera2D` into the configuration module, as in `WorldRendering` below. Register and schedule each renderer using the execution modes above.

### Settings at a glance

| Setting | Responsibility |
| --- | --- |
| `Screen` | Logical size and relative regions; it does not allocate a render target. |
| `Output` | Resolution of the renderer's private render target. |
| Layer `Target` | Optional private target for that layer; `null` draws directly into the renderer output. |
| `ScaleMode` | Scaling during layer composition and final presentation. |
| `Alignment` | Placement of the final image and ancestor images; layer-target composition remains centered. |
| Layer `Camera` | World-to-target transform; omit it for UI coordinates. |

`MatchScreen()` and `MatchPresentation()` select target dimensions, not scaling behavior. They have the same sizing rule whether assigned to `RendererOptions.Output` or a layer's `Target`:

| Target specification | Dimensions |
| --- | --- |
| `MatchScreen()` | The renderer's sampled logical `ScreenLayout.Size`. |
| `MatchPresentation()` | The renderer's sampled `ScreenLayout.Bounds` width and height. |
| `OfSize(width, height)` | The supplied constant width and height. |

Layers do not have independent screens or presentation areas. In particular, a layer's `MatchPresentation()` follows the renderer's presentation bounds, not the dimensions of `RendererOptions.Output`. It does not position the layer inside those bounds; layer composition still happens inside the renderer output first.

The root output area comes from `OutputOverride.Bounds` when set, otherwise from the backbuffer dimensions. `ScreenLayout.Calculate` then resolves the screen hierarchy inside that area. A root screen uses the whole area; a child uses its mapped region. `OutputOverride` therefore changes the root output, but does not bypass child regions. `MatchPresentation()` uses these resulting bounds, not their visible `Clip` or the scaled rectangle of a logical image. Target dimensions are clamped to at least one pixel; an empty presentation area is not displayed.

For example, with a logical root screen of 320 by 180, `Stretch`, and an 800-by-600 backbuffer:

| Screen and final destination | `MatchScreen()` target | `MatchPresentation()` target |
| --- | --- | --- |
| Root screen, backbuffer | 320 by 180 | 800 by 600 |
| Left-half region, backbuffer | 160 by 180 | 400 by 600 |
| Left-half region, 400-by-300 `OutputOverride` | 160 by 180 | 200 by 300 |

Target coordinates start at `(0, 0)` even when the presentation bounds have a nonzero position. The renderer applies that position during final composition. `OfSize` changes resolution, not placement. `SyncCameraViewport` follows the resolved layer target, or the renderer output when the layer has no target, and always centers the camera origin. It does not adjust camera position or zoom.

### 1. Fixed-resolution pixel art

Keep a 320-by-180 coordinate space regardless of engine resolution or window size:

```csharp
options.Screen = new Screen(new Point(320, 180));
options.Output = RenderTargetSpec.MatchScreen();
options.ScaleMode = RenderScaleMode.IntegerFit;
options.Alignment = RenderAlignment.Center;
options.DefaultSamplerState = SamplerState.PointClamp;
options.AddLayer(Layers.Sprites, new RenderLayerOptions
{
    Camera = camera,
    SyncCameraViewport = true
});
```

A larger window enlarges the image by whole-number factors and leaves unused borders. If the output is smaller than the logical image, `IntegerFit` falls back to fractional downscaling. A `Resolution.SetResolution(...).ApplyChanges()` call does not change this fixed screen's logical size. The camera position remains a game-owned choice; viewport synchronization centers its origin, not its world position.

For the engine's configured logical resolution instead, retain the DI-supplied screen. `Fit` enlarges proportionally, `Fill` enlarges proportionally and clips overflow, and `Stretch` fills the area but may distort the aspect ratio. `LinearClamp` can smooth fractional scaling for non-pixel-art content, but cannot add detail to a small source image.

### 2. Logical-resolution UI with MatchScreen

Use this for a pixel-art HUD, health bars, inventory, or pause menu that should scale with the playfield rather than stay a fixed physical-pixel size. This is a separate UI renderer sharing the main logical screen with the world renderer:

```csharp
options.Output = RenderTargetSpec.MatchScreen();
options.ScaleMode = RenderScaleMode.IntegerFit;
options.Alignment = RenderAlignment.Center;
options.DefaultSamplerState = SamplerState.PointClamp;
options.AddLayer(Layers.Hud);
options.AddLayer(Layers.Menu, new RenderLayerOptions { Order = 100 });
```

Submit UI without a camera, using logical coordinates:

```csharp
renderer.Context.Batch(Layers.Hud)
    .Draw(badge, new Vector2(12, 12), Color.White);
```

Load `badge` once and submit before the UI renderer flushes. A badge at logical `(12, 12)` has a 24-pixel inset relative to the presented image when it is enlarged 2x. The submitted coordinates remain unchanged; in fixed virtual mode, resizing changes presentation rather than logical size. In responsive mode, resizing can also expand the logical UI area, so recompute anchors based on the current screen size. Applied logical resolution changes update the target size because the default screen is a `ResolutionScreen`.

For a HUD aligned with the playfield, both renderers must use the same screen, scale mode, and alignment. Flush the UI after the world, for example with `AddDrawComponent(DrawStage.Screen)` after the world stage. Do not assign a world camera to the HUD: panning and zooming the world should not move interface elements. A transparent private output allows the UI to overlay the world; avoid an opaque full-target clear. Pointer input must be mapped from window pixels back to the UI's presented logical coordinates; the renderer does not perform that conversion.

### 3. Native-resolution UI with MatchPresentation

Use this when interface elements should be drawn at their native pixel size rather than enlarged with the game:

```csharp
options.Output = RenderTargetSpec.MatchPresentation();
options.ScaleMode = RenderScaleMode.Stretch;
options.AddLayer(Layers.Hud);
options.AddLayer(Layers.Menu, new RenderLayerOptions { Order = 100 });
```

Keep the root screen and omit cameras for full-window UI. The renderer output follows its presentation area: the whole final destination for a root screen, or the mapped bounds for a child region. `OutputOverride` selects the final destination instead of the backbuffer; a child still occupies its region inside that destination. A larger presentation area offers more space but does not enlarge widgets. The UI must recalculate its own anchors from the presentation dimensions, using coordinates local to its target. The logical `Screen.Size` is not the size of this target. Because this example sets the renderer output to `MatchPresentation()`, final composition is 1:1 and clipped to ancestors.

`MatchPresentation()` is also useful for postprocessing at the presentation resolution. It is not a request to stretch logical game coordinates: a camera layer synchronized to this target gets a larger viewport, not automatic zoom. With unchanged zoom, the enlarged viewport can show more world around the camera.

### 4. Logical world layer with native UI

Use one renderer when the world should render at logical resolution but the HUD should draw in physical pixels:

```csharp
options.Output = RenderTargetSpec.MatchPresentation();
options.ScaleMode = RenderScaleMode.Fit;
options.AddLayer(Layers.Sprites, new RenderLayerOptions
{
    Target = RenderTargetSpec.MatchScreen(),
    Camera = camera,
    SyncCameraViewport = true
});
options.AddLayer(Layers.Hud, new RenderLayerOptions { Order = 100 });
```

The world layer renders into its own screen-sized target and is then fitted into the renderer's presentation-sized output. Its camera viewport follows the layer target. The HUD has no target or camera, so it draws directly into the presentation-pixel output after world composition. In fixed virtual mode, window resizing scales the world image, not the HUD. In responsive mode, an aspect-ratio change can also resize the world target and its camera viewport. Applied logical resolution changes resize the world target; changes to the presentation bounds resize the renderer output, including region and output-target changes.

Internal layer composition is always centered and uses the renderer's `ScaleMode`; there is no per-layer scaling or alignment option. `RendererOptions.Alignment` only changes final presentation and ancestor-image placement. A small layer target does not automatically become a minimap in a corner. Conversely, setting a layer target to `MatchPresentation()` while keeping the renderer output at `MatchScreen()` first scales that presentation-sized layer into the logical output, then scales the output for presentation. That round trip can lose detail; use a presentation-sized renderer output for native-resolution UI.

### 5. Two-player split-screen

Give each player a separate renderer and camera. Configure the left renderer with:

```csharp
options.Screen = options.Screen.CreateRegion(0f, 0f, 0.5f, 1f);
options.Output = RenderTargetSpec.MatchScreen();
options.ScaleMode = RenderScaleMode.Stretch;
options.AddLayer(Layers.Sprites, new RenderLayerOptions
{
    Camera = camera,
    SyncCameraViewport = true
});
```

Configure the right renderer independently, starting from the shared main screen, with `CreateRegion(0.5f, 0f, 0.5f, 1f)` and the same target and scaling settings. Use distinct camera instances for distinct player views. Each region has half the parent's logical width; with `Stretch`, it fills half the physical output. In fixed virtual mode, resizing changes those physical halves, not the parent logical resolution. In responsive mode, a changed aspect ratio also updates the parent and both regions' logical sizes. Applied logical resolution changes update both regions in either mode.

`Stretch` can distort the image. With `Fit`, the regions instead follow the parent's proportionally fitted image, including its letterboxing. Do not assume these regions are fractions of the entire physical window in every scale mode. Final presentation is clipped to the region and its ancestors, so renderer clears stay inside their private targets.

### 6. Fixed-size minimap in a corner

Use a separate renderer and camera for a 128-by-128-pixel minimap at the top-right corner:

```csharp
options.Screen = new Screen(new Point(128, 128));
options.Output = RenderTargetSpec.MatchScreen();
options.ScaleMode = RenderScaleMode.Native;
options.Alignment = RenderAlignment.TopRight;
options.AddLayer(Layers.Sprites, new RenderLayerOptions
{
    Camera = camera,
    SyncCameraViewport = true
});
```

`Center` means no image scaling; `TopRight` controls its placement. A larger window moves the minimap with the top-right corner without enlarging it. It remains independent of the engine's logical resolution. Give its camera the desired world position and zoom; target dimensions alone do not make the whole map visible. Schedule its renderer after the world and any UI it should cover. On an output smaller than the minimap, clipping removes the overflow.

For a minimap that scales with the main presentation instead, use a main-screen region such as `CreateRegion(0.75f, 0f, 0.25f, 0.25f)`, `MatchScreen()`, and the desired scale mode. That region inherits its logical dimensions from the parent; it is not an independent fixed 128-by-128 screen. A root fixed screen plus corner alignment needs no extra viewport API, but the current renderer has no direct arbitrary pixel-position or margin option.

### Larger output example

For a root screen of 320 by 180 presented into an 800-by-600 output with centered alignment:

| Scale mode | Presented image | Result |
| --- | --- | --- |
| `Fit` | 800 by 450 | Unused space above and below. |
| `Fill` | About 1067 by 600 | Horizontal overflow is clipped. |
| `Stretch` | 800 by 600 | Full coverage with aspect-ratio distortion. |
| `Center` | 320 by 180 | Original size, centered. |
| `IntegerFit` | 640 by 360 | Whole-number 2x enlargement. |

These image dimensions assume `RendererOptions.Output = MatchScreen()` and the root screen above. With `RendererOptions.Output = MatchPresentation()`, the output target is instead 800 by 600 and final composition has a scale factor of one in every listed mode. This does not enlarge logical drawing coordinates: without a camera transform, an item drawn at `(12, 12)` stays 12 presentation pixels from the output origin. A layer's own target can still be scaled during composition into that output.

## Automatic rendering with DI

Define layer IDs once. A configuration class is only needed for renderer-specific DI dependencies; simple renderers use the same builder directly by name.

```csharp
using PurrplingCore.Toolkit.Rendering;

public static class Layers
{
    public static readonly RenderLayerId Sprites = RenderLayerId.Of("World.Sprites");
    public static readonly RenderLayerId Effects = RenderLayerId.Of("World.Effects");
    public static readonly RenderLayerId Hud = RenderLayerId.Of("Screen.Hud");
    public static readonly RenderLayerId Menu = RenderLayerId.Of("Screen.Menu");
}

public enum RendererKey
{
    World,
    Screen,
    Cutscene,
}
```

Register them in an `IServiceModule` (for example, a module on your game class via `GameServicesAttribute`). The examples below assume the appropriate namespaces are imported: `Microsoft.Extensions.DependencyInjection`, `Microsoft.Xna.Framework`, `PurrplingCore.Graphics`, `PurrplingCore.Rendering`, `PurrplingCore.Toolkit.Graphics`, and `PurrplingCore.Toolkit.Rendering`. Renderer configuration is a DI-created class, not a service-configuration callback.

```csharp
public sealed class WorldRendering(Camera2D camera) : IRendererConfiguration
{
    public void Configure(RendererOptions options)
    {
        options.ClearColor = new Color(14, 20, 31);
        options.AddLayer(Layers.Sprites, new RenderLayerOptions { Camera = camera, SyncCameraViewport = true });
        options.AddLayer(Layers.Effects, new RenderLayerOptions { Order = 100, Camera = camera, SyncCameraViewport = true });
    }
}

public sealed class ExampleRenderingServices : IServiceModule
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddRenderer(RendererKey.World)
            .AddModule<WorldRendering>()
            .AddDrawComponent(DrawStage.World);
        services.AddRenderer(RendererKey.Screen)
            .AddLayer(Layers.Hud)
            .AddLayer(Layers.Menu, order: 100)
            .AddDrawComponent(DrawStage.Screen);
    }
}
```

The game adds registered `IGameComponent` services to `Game.Components` during `Game.Initialize()`. Each renderer component initializes and flushes only its keyed renderer. It subscribes to the game's `BeginDraw` event to call `BeginFrame` before the draw components run. `layer.Order` orders layers **inside a renderer**; `drawOrder` on `AddDrawComponent` orders the component relative to other MonoGame components and defaults to `0`. Calling `AddDrawComponent` again adds another component, even with a different `drawOrder`. `DrawStage` contains optional `int` anchors for components, not renderers; a game can use its own values.

At equal `DrawOrder`, MonoGame 3.8.5 draws components in registration order. Give each renderer component an explicit draw order after its producers. If two renderers need to be flushed at different points in the MonoGame draw loop, register two components with different `drawOrder` values.

### Quick path: no configuration classes

For a small game, use the same builder without a configuration class:

```csharp
services.AddRenderer("World")
    .AddLayer(Layers.Sprites)
    .AddDrawComponent();
```

The renderer also joins `IEnumerable<IRenderer>`. Inject it with `[FromKeyedServices(RendererKey.World)] IRenderer renderer`, then submit through `renderer.Context.Batch(Layers.Sprites)`. Begin the frame explicitly before any producer submits. Omit `.AddDrawComponent()` for manual ownership; your controller must call `Initialize`, `BeginFrame`, and `Flush`. Add an `IRendererConfiguration` module under the same key when camera, effect, or other service dependencies must be injected; never build a second provider inside `ConfigureServices`.

### Submitting from ECS

Friflo draw systems only submit; they do not call `Flush` or `SpriteBatch.Begin/End`. World-owned renderers provide an unkeyed context, as used by KittyCat below (Friflo.Engine.ECS 3.6.0). For a globally keyed renderer, use `[FromKeyedServices(RendererKey.World)] RenderContext context` instead.

```csharp
using Friflo.Engine.ECS.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PurrplingCore.Ecs.Attributes;
using PurrplingCore.Ecs.Systems;
using PurrplingCore.Toolkit.Rendering;

[System<DrawSystemGroup>]
public sealed class SpriteDrawSystem(RenderContext context)
    : QuerySystem<Position2D, Sprite2D>
{
    protected override void OnUpdate()
    {
        var writer = context.Batch(Layers.Sprites);
        foreach (var (positions, sprites, entities) in Query.Chunks)
        {
            for (var index = 0; index < entities.Length; index++)
            {
                ref var sprite = ref sprites[index];
                if (!sprite.IsVisible) continue;
                writer.Draw(sprite.Texture, positions[index].Value, Color.White);
            }
        }
    }
}
```

`Position2D` and `Sprite2D` above are game-owned Friflo components; provide your own definitions (`IComponent`), with `Vector2 Value`, `Texture2D Texture`, and `bool IsVisible` respectively. Register the system in your world blueprint (KittyCat uses `world.AddSystem<StarCatcherDrawSystem>()`). A world-owned renderer flushes after the draw systems automatically. For a globally keyed renderer, set its component's `drawOrder` after the world controller. See `KittyCat/KittyCat.Game/Ecs/Systems/StarCatcherDrawSystem.cs` and `KittyCat/KittyCat.Game/Ecs/Systems/StarCatcherHudSystem.cs` for world-owned rendering and independent global HUD rendering.

### Submitting from an ordinary object

Nothing in `RenderContext` depends on ECS. Submit from a UI object, an animation manager, or a game-level draw method:

```csharp
public sealed class BadgeOverlay([FromKeyedServices(RendererKey.Screen)] IRenderer renderer)
{
    public void Draw(Texture2D badge)
    {
        renderer.Context.Batch(Layers.Hud)
            .Draw(badge, new Vector2(12, 12), Color.White);
    }
}
```

Invoke `BadgeOverlay.Draw` before the component for the screen renderer draws. For example, in a game's `Draw` override, submit before `base.Draw(gameTime)`; the latter drives the registered components. Load `badge` once during content loading, not every frame.

## Scenes: with and without the renderer

`SceneManager` remains usable without any renderer registration. An ordinary `Scene` has `RenderMode = Canvas`: the manager opens its canvas, calls `Draw`, and blits the canvas immediately. No `AddRenderer` or `AddRendererGameComponent` is necessary for that mode.

For deferred rendering, inherit `RendererScene`. It exposes `Context`; `SceneManager` calls `Submit` rather than opening a canvas. It does not know which renderer the scene uses and does not flush it. Add `.AddDrawComponent(...)` after the scene's draw order, or flush the renderer in your own controller. Each call adds a separate component. Asset names in this example are placeholders for assets supplied by your game.

```csharp
public sealed class MenuScene(
    GraphicsManager graphics,
    IContentManagerProvider contentProvider,
    [FromKeyedServices(RendererKey.Screen)] IRenderer renderer)
    : RendererScene(graphics, contentProvider, renderer)
{
    private SpriteFont _font = null!;

    protected override void LoadContent()
        => _font = Content.Load<SpriteFont>("Fonts/Menu");

    protected override void Update(GameTime time, bool isActive) { }

    protected override void Draw(GameTime time)
        => Context.Batch(Layers.Menu)
            .DrawString(_font, "PAUSED", new Vector2(120, 80), Color.White);
}
```

Register `services.AddSceneManager(); services.AddScene<MenuScene>();` and register `RendererKey.Screen` as above. The scene submits; the renderer component presents later. A `RendererScene` must **not** use `SpriteBatch` in its `Draw`: no canvas or batch has been opened for it.

Canvas and deferred scenes can coexist, but they have different draw timing. `RendererOptions.ClearColor` clears the renderer's private output, not the backbuffer. If `SceneManager` blits a canvas before a later renderer presents an opaque image over that area, the canvas is covered. Draw the canvas scene later or use a transparent renderer output where appropriate. `RenderMode` is per scene, not a global switch.

## Manual renderer, including a scene

Each renderer can be owned by a specific controller or scene. Register it under its key, inject the same keyed `IRenderer`, and decide explicitly when it initializes and flushes:

```csharp
services.AddRenderer(RendererKey.Cutscene)
    .AddModule<CutsceneRendering>();
```

```csharp
public sealed class CutsceneScene(
    GraphicsManager graphics,
    IContentManagerProvider contentProvider,
    [FromKeyedServices(RendererKey.Cutscene)] IRenderer renderer)
    : RendererScene(graphics, contentProvider, renderer)
{
    private Texture2D _image = null!;

    protected override void OnInitialize()
        => renderer.Initialize(graphics.GraphicsDevice);

    protected override void LoadContent()
        => _image = Content.Load<Texture2D>("Scenes/Cutscene");

    protected override void Update(GameTime time, bool isActive) { }

    protected override void Draw(GameTime time)
    {
        renderer.BeginFrame();
        Context.Batch(Layers.Menu).Draw(_image, Vector2.Zero, Color.White);
        renderer.Flush();
    }
}
```

Use `AddRendererGameComponent(key, drawOrder)` when a renderer should be driven by MonoGame. Use a scene or another game controller instead when the renderer belongs to a special workflow. No renderer group or global renderer order is involved.

## Fully manual setup without renderer DI

Use the Toolkit renderer directly when you control the draw loop. This example is a `PurrplingCore.Toolkit.Game` subclass; its constructor gets the host service provider, but no renderer service, group, or renderer game component is registered. The texture path is supplied by your game's content pipeline.

```csharp
public sealed class StandaloneGame : PurrplingCore.Toolkit.Game
{
    private static readonly RenderLayerId World = RenderLayerId.Of("Standalone.World");
    private readonly Screen _screen = new(new Point(320, 180));
    private readonly Renderer2D _renderer;
    private Texture2D _hero = null!;

    public StandaloneGame(IServiceProvider provider) : base(provider)
    {
        var options = new RendererOptions(_screen) { ClearColor = Color.CornflowerBlue };
        options.AddLayer(World);
        _renderer = new Renderer2D("Standalone", options);
    }

    protected override void Initialize()
    {
        base.Initialize();
        _renderer.Initialize(GraphicsDevice);
    }

    protected override void LoadContent()
    {
        base.LoadContent();
        _hero = Content.Load<Texture2D>("Sprites/Hero");
    }

    protected override void Draw(GameTime gameTime)
    {
        _renderer.BeginFrame();
        _renderer.Context.Batch(World).Draw(_hero, new Vector2(32, 32), Color.White);
        _renderer.Flush();
        base.Draw(gameTime);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _renderer.Dispose();
        }
        base.Dispose(disposing);
    }
}
```

The usual host entry point still applies:

```csharp
var builder = GameHost.CreateBuilder(args);
builder.UsePurrplingCore();
builder.AddGame<StandaloneGame>();
using var host = builder.Build();
return host.Run();
```

With this setup, nothing invokes `Initialize`, `BeginFrame`, or `Flush` for you. `Initialize` creates GPU resources; `Flush` requires it and must run on the graphics thread after submission. This example owns a fixed 320-by-180 screen independently of the backbuffer. Pass a `ResolutionScreen` when logical size should follow that source instead. Dispose the renderer and any caller-owned `ResolutionScreen` separately; a fixed `Screen` requires no disposal. No component is needed to synchronize screen size.

### Manual screen size and camera viewport

Use a fixed `Screen` for an independent logical resolution. Enable `SyncCameraViewport` on camera layers to size the camera viewport and center its origin from the resolved layer target. The renderer traverses configured layers to synchronize their cameras during initialization and each explicit `BeginFrame`, before producers obtain layer writers. Applied changes to a `ResolutionScreen` take effect on the next `BeginFrame` without reinitializing the renderer. A camera shared by several layers may receive the same viewport more than once; `Camera2D` skips unchanged viewport sizes. Sharing a camera between layers with different target dimensions makes the last synchronized viewport win; use separate cameras instead:

```csharp
var camera = new Camera2D();
var screen = new Screen(new Point(320, 180));
var options = new RendererOptions(screen)
{
    Output = RenderTargetSpec.MatchScreen(),
    ScaleMode = RenderScaleMode.IntegerFit,
};
options.AddLayer(World, new RenderLayerOptions
{
    Camera = camera,
    SyncCameraViewport = true,
});

var renderer = new Renderer2D("World", options);
renderer.Initialize(graphicsDevice);

renderer.BeginFrame();
renderer.Context.Batch(World).Draw(texture, position, Color.White);
renderer.Flush();
```

Choose the size of a fixed screen before constructing the renderer. For changing logical resolution, use a `ResolutionScreen` and call `resolution.SetResolution(width, height).ApplyChanges()`. Reassigning `RendererOptions.Screen` after construction does not replace the renderer's captured screen. If the camera viewport should remain independent of the render target, leave `SyncCameraViewport` false and call `camera.SetViewport(width, height)` followed by `camera.CenterOrigin()` when your chosen viewport changes. Screen size controls the `MatchScreen` render target; it does not automatically alter game-world coordinates or camera position.

### Without the PurrplingCore host or game class

The Toolkit renderer also works with a plain MonoGame game. This version needs no DI, engine service module, world controller, scene manager, or renderer component. It still needs a MonoGame graphics device and your game's content asset.

```csharp
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PurrplingCore.Toolkit.Graphics;
using PurrplingCore.Toolkit.Rendering;

using var game = new RawMonoGame();
game.Run();

public sealed class RawMonoGame : Microsoft.Xna.Framework.Game
{
    private static readonly RenderLayerId World = RenderLayerId.Of("Raw.World");
    private readonly GraphicsDeviceManager _graphics;
    private readonly Screen _screen = new(new Point(320, 180));
    private readonly Renderer2D _renderer;
    private Texture2D _hero = null!;

    public RawMonoGame()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        var options = new RendererOptions(_screen) { ClearColor = Color.CornflowerBlue };
        options.AddLayer(World);
        _renderer = new Renderer2D("Raw", options);
    }

    protected override void LoadContent()
    {
        _renderer.Initialize(GraphicsDevice);
        _hero = Content.Load<Texture2D>("Sprites/Hero");
    }

    protected override void Draw(GameTime gameTime)
    {
        _renderer.BeginFrame();
        _renderer.Context.Batch(World).Draw(_hero, new Vector2(32, 32), Color.White);
        _renderer.Flush();
        base.Draw(gameTime);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _renderer.Dispose();
        }
        base.Dispose(disposing);
    }
}
```

## Layers, camera, targets, and extensions

Each renderer has ordered layers (`RenderLayerOptions.Order`). A layer inherits the renderer's `DefaultSamplerState` (`PointClamp` by default) unless overridden. Command ordering and initial capacity belong to `DrawCommandPass`, not the layer: configure them with `new DrawCommandPass(sortMode, initialCapacity)`. Its sort mode defaults to `Submission`, preserving the order in which calls were queued. Other modes are `FrontToBack`, `BackToFront`, and `Texture`; the pass sorts its own queue during `Prepare`. `FrontToBack` sorts the `depth` argument to `Draw`/`DrawString` ascending (lower values first); `BackToFront` sorts it descending (higher values first). Equal depths retain submission order. This is CPU-side ordering only: the built-in SpriteBatch batcher submits every command with GPU `layerDepth` zero, so `depth` does not by itself provide depth-buffer testing. Renderers are independent and do not share a global sort order.

World-space layers can share a DI-owned `Camera2D`. The renderer configuration is constructed by DI, so it receives the same camera instance as systems that update its position:

```csharp
public sealed class CameraWorldRendering(Camera2D camera, Resolution resolution) : IRendererConfiguration
{
    public void Configure(RendererOptions options)
    {
        options.AddLayer(Layers.Sprites, new RenderLayerOptions
        {
            Camera = camera,
            SyncCameraViewport = true,
            CullingEnabled = true,
            Pass = new DrawCommandPass(RenderSortMode.FrontToBack),
        });
        options.AddLayer(Layers.Effects, new RenderLayerOptions
        {
            Order = 100,
            Camera = camera,
            SyncCameraViewport = true,
            BlendState = BlendState.Additive,
        });
    }
}
```

Register both services in your `IServiceModule`:

```csharp
services.AddSingleton<Camera2D>();
services.AddRenderer(RendererKey.World)
    .AddModule<CameraWorldRendering>()
    .AddDrawComponent(DrawStage.World);
```

Update `camera.Position` from your update code. With `SyncCameraViewport = true`, the renderer derives viewport dimensions from the layer target and centers the camera origin; omit it when the camera viewport is intentionally independent. `layer.Parallax` defaults to `Vector2.One` (follow the camera); zero keeps the layer fixed. Camera-less layers render in screen coordinates. Culling is performed **at submission**, using an axis-aligned visible rectangle; rotated sprites deliberately skip that culling test.

For an offscreen layer or renderer, set `layer.Target` or `options.Output` to `RenderTargetSpec.MatchScreen()`, `MatchPresentation()`, or `OfSize(width, height)`. Layer targets are composed onto the renderer output. `Renderer2D.OutputOverride` can direct the final composed image to an externally owned target; leave it null to use the backbuffer. It does not replace the renderer's private output target or bypass its screen layout. `IPostProcessor` chains can be added to a layer's `PostProcessors` or a renderer's `PostProcessors`; processors are initialized with the renderer and disposed with it. The renderer-level chain processes the private output before final presentation; `options.Output` is non-nullable and defaults to `MatchScreen()`. A layer's chain requires `layer.Target`. Renderers do not share a global post-processing stage.

`RenderLayerOptions.Pass` replaces the default `DrawCommandPass` with your `IRenderLayerPass`; its `Initialize(GraphicsDevice)` is called before drawing and `Prepare/Execute` run during flush. The layer owns graphics state and target configuration, but no submission queue. Use a separate pass instance per layer; the renderer initializes and disposes it. A pass accepts draw commands only when it implements `ILayerSubmissionPass<DrawCommand>`. `Primitives2D` provides `FillRect`, `DrawRect`, `DrawLine`, and `FillCircle` extensions on `LayerWriter<DrawCommand>`; they currently take a `GraphicsDevice` to obtain the shared white pixel. That first pixel creation is a GPU operation, so warm it up after device initialization if draw systems must only enqueue during the frame.

### Frame-scoped pass submissions

A pass implements `ILayerSubmissionPass<T>` to own and receive typed values. Producers use the single `renderer.Context.Batch<T>(layerId).Submit(in value)` entry point. `LayerWriter<T>` is a `ref struct`; keep it local to the submitting method. Its public `Resources` and `Text` properties expose the renderer's shared resource table and current frame text buffer for custom submissions and extensions. `CullBounds` contains the visible bounds sampled when the writer was obtained, or null when culling is disabled. Missing, disabled, or incompatible optional layers return an inactive writer that discards data without opening a frame; all three properties are null on an inactive writer, so check `IsActive` before using them.

`Batch(layerId)` is shorthand for `Batch<DrawCommand>(layerId)`. `Draw`, `DrawString`, and `DrawPrimitives` are extensions from `DrawCommandExtensions` on `LayerWriter<DrawCommand>`, not operations on every writer. Custom submission types can have their own extensions over the same generic writer.

`Culling2D.IsCulled(...)` is a public, command-independent visibility test for world-space rectangles or local dimensions with position, origin, and scale. The same overloads are available as `writer.IsCulled(...)` for every `LayerWriter<T>`, including custom submissions:

```csharp
var writer = renderer.Context.Batch<MeshSubmission>(Layers.Mesh);
if (writer.IsActive && !writer.IsCulled(meshBounds))
{
    writer.Submit(in meshSubmission);
}
```

Culling preserves edge-touching bounds and handles mirrored scales. Missing cull bounds or nonzero rotation conservatively return false. `IsCulled` only tests geometry; check `IsActive` separately when inactive writers should skip submission preparation.

One pass can implement several closed interfaces, such as `ILayerSubmissionPass<DrawCommand>` and `ILayerSubmissionPass<MeshSubmission>`. Each `Batch<T>` resolves the corresponding interface on that same instance. Opening a second submission type within a frame does not clear earlier submissions. The pass supplies each `Submit(in T)` implementation and decides how different types are stored, ordered, and drawn.

The shared `SubmissionCount` totals all supported types. The shared `ClearSubmissions()` clears all of them and runs once per layer at frame reset, not once per type. `BeginFrame()` clears every submission pass, including disabled layers, before new data is added. `DrawCommandPass` implements this same contract and owns its `Commands` queue; custom passes choose their own storage and preparation without allocating an unused `DrawList`.

When extending `DrawCommandPass` with another submission type, override `SubmissionCount` to include the base count and override `ClearSubmissions()` to call the base implementation and clear the additional storage. Both members are virtual so the renderer still sees one complete frame lifecycle through every submission interface.

Required access belongs to the producer's request, not to the pass as a whole:

```csharp
var mesh = renderer.Context.Batch<MeshSubmission>(Layers.Mesh, required: true);
mesh.Submit(in meshSubmission);

var debug = renderer.Context.Batch<DebugSubmission>(Layers.Debug, required: false);
debug.Submit(in debugSubmission);
```

`Batch<T>(layerId)` is required by default (`required: true`), while the draw-command shorthand `Batch(layerId)` is optional by default (`required: false`). Required access throws `InvalidOperationException` if the layer is missing or its pass does not implement `ILayerSubmissionPass<T>`, including when the renderer or layer is disabled. A configured compatible layer that is disabled still returns an inactive writer: visibility is not a composition error. Failed and inactive requests never open or clear a frame. `Batch(layerId, required: true)` applies the same validation for `DrawCommand`. This checks composition, not whether at least one value of that type is submitted every frame.

### Custom pass and post-processor

A pass can issue its own draw calls without accepting submissions. This example draws one texture using the batcher; the texture is supplied by the caller and remains caller-owned:

```csharp
public sealed class BackdropPass(Texture2D backdrop) : RenderLayerPass
{
    public override void Execute(in RenderFrameContext context)
    {
        var resources = context.Resources;
        var command = new DrawCommand
        {
            Kind = DrawCommandKind.Sprite,
            ResourceIndex = resources.GetTextureIndex(backdrop),
            Source = new Rectangle(0, 0, backdrop.Width, backdrop.Height),
            Scale = Vector2.One,
            Color = Color.White,
        };

        context.Batcher.Begin(context.State);
        context.Batcher.Submit(command, resources, context.Text);
        context.Batcher.End();
    }
}

options.AddLayer(Layers.Sprites, new RenderLayerOptions { Pass = new BackdropPass(backdrop) });
```

The `backdrop` must be loaded before configuration; `RenderLayerPass` does not own or dispose it. `Prepare` is optional on the `RenderLayerPass` base class. `Batch(layerId)` is inactive for `BackdropPass` because it does not accept draw commands. To retain regular queued commands as well, use a separate sprite layer or derive from `DrawCommandPass` and extend its `Execute` implementation.

#### Drawing vertex geometry

For geometry that is not a sprite or text, use `RenderFrameContext.Device` directly. The renderer has already bound the layer target. Raw `GraphicsDevice` calls do not apply the layer's camera transform or batch state automatically, so the pass must apply the relevant `context.State` values itself. This pass accepts positions instead of draw commands, reuses its own list, and draws a colored triangle at each submitted offset:

```csharp
public sealed class TrianglePass : RenderLayerPass, ILayerSubmissionPass<Vector2>
{
    private readonly List<Vector2> _positions = new(256);
    private readonly VertexPositionColor[] _vertices =
    [
        new(new Vector3(80, 40, 0), Color.Cyan),
        new(new Vector3(40, 120, 0), Color.Magenta),
        new(new Vector3(120, 120, 0), Color.Yellow),
    ];

    private BasicEffect? _effect;

    public int SubmissionCount => _positions.Count;

    public void Submit(in Vector2 submission) => _positions.Add(submission);

    public void ClearSubmissions() => _positions.Clear();

    public override void Initialize(GraphicsDevice device)
    {
        _effect = new BasicEffect(device)
        {
            LightingEnabled = false,
            VertexColorEnabled = true,
        };
    }

    public override void Execute(in RenderFrameContext context)
    {
        var effect = _effect;
        if (effect is null)
        {
            throw new InvalidOperationException("Initialize the pass before drawing.");
        }

        var device = context.Device;
        var viewport = device.Viewport;
        effect.View = context.State.Transform;
        effect.Projection = Matrix.CreateOrthographicOffCenter(
            0, viewport.Width, viewport.Height, 0, 0, 1);

        device.BlendState = context.State.BlendState ?? BlendState.AlphaBlend;
        device.DepthStencilState = context.State.DepthStencilState ?? DepthStencilState.None;
        device.RasterizerState = context.State.RasterizerState ?? RasterizerState.CullNone;

        foreach (var position in _positions)
        {
            effect.World = Matrix.CreateTranslation(position.X, position.Y, 0f);
            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawUserPrimitives(PrimitiveType.TriangleList, _vertices, 0, 1);
            }
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _effect?.Dispose();
        base.Dispose(disposing);
    }
}

options.AddLayer(Layers.Mesh, new RenderLayerOptions { Pass = new TrianglePass() });
renderer.Context.Batch<Vector2>(Layers.Mesh).Submit(new Vector2(16, 32));
```

`context.State.Transform` contains the layer camera/parallax transform. The triangle pass never creates or reads `DrawCommand` values. `DrawUserPrimitives` is suitable for small, transient geometry; for larger or frequently changing meshes, use a custom mesh submission or submit a `VertexBuffer` through `LayerWriter<DrawCommand>` alongside sprites and text.

`DrawPrimitives` adds a primitive command to the accepting pass. With the default `DrawCommandPass`, this ECS-style loop interleaves geometry and sprites in its ordered `DrawList`:

```csharp
var writer = renderer.Context.Batch(Layers.World);
foreach (var tileChunk in visibleTileChunks)
{
    writer.DrawPrimitives(
        tileChunk.VertexBuffer,
        PrimitiveType.TriangleList,
        tileChunk.PrimitiveCount,
        tileChunk.Position,
        Vector2.One,
        depth: tileChunk.Depth);
}

foreach (var entity in visibleEntities)
{
    writer.Draw(entity.Texture, entity.Position, Color.White, depth: entity.Depth);
}
```

`DrawCommandPass.Prepare` sorts both kinds together using the pass's configured sort mode. `FrontToBack` and `BackToFront` use each command's `depth`; equal depths preserve submission order. Primitive commands require a layer `Effect` implementing `IEffectMatrices`; its `Projection` is caller-configured, while the command supplies `World` and the layer camera supplies `View`. Vertex buffers remain caller-owned and must stay valid through `Flush()`.

To control every command field directly, construct a `DrawCommand` and submit it with the resource it references:

```csharp
var writer = renderer.Context.Batch(Layers.Mesh);
var resourceIndex = renderer.Context.Resources.GetVertexBufferIndex(mesh.VertexBuffer);
var command = new DrawCommand
{
    Kind = DrawCommandKind.Primitive,
    ResourceIndex = resourceIndex,
    Position = position,
    Scale = Vector2.One,
    Color = Color.White,
    PrimitiveType = PrimitiveType.TriangleList,
    PrimitiveCount = mesh.PrimitiveCount,
};
writer.Submit(in command);
```

`LayerWriter<DrawCommand>.Submit` forwards the command unchanged to the pass: it does not resolve resources, validate the command kind, or alter its fields. Use `renderer.Context.Resources` to obtain texture, font, or vertex-buffer indices. For text, append characters through `renderer.Context.TextBuffer` and set `TextOffset`/`TextLength` yourself. Direct commands must contain valid indices and frame text offsets; resources must remain alive until `Flush()`. The `Draw` and `DrawString` extensions resolve these details for you.

A processor receives an already-bound destination target and a source texture. The supplied effect is owned by its caller:

```csharp
public sealed class TintProcessor(Effect effect) : IPostProcessor
{
    public void Initialize(GraphicsDevice device) { }

    public void Process(in PostProcessContext context)
    {
        var source = context.Source;
        var resources = context.Resources;
        var command = new DrawCommand
        {
            Kind = DrawCommandKind.Sprite,
            ResourceIndex = resources.GetTextureIndex(source),
            Source = new Rectangle(0, 0, source.Width, source.Height),
            Scale = Vector2.One,
            Color = Color.White,
        };

        context.Batcher.Begin(new BatchState(Matrix.Identity, effect: effect));
        context.Batcher.Submit(command, resources, context.Text);
        context.Batcher.End();
    }

    public void Dispose() { }
}

options.Output = RenderTargetSpec.OfSize(1280, 720);
options.PostProcessors.Add(new TintProcessor(effect));
```

For a layer-local effect, set `layer.Target` and add the processor to `layer.PostProcessors` instead. The target is required: without `options.Output`, the renderer-wide processor chain is currently skipped. The example's `backdrop` and `effect` must be loaded or constructed by game code; they are not registered as services automatically.

## Diagnostics and benchmarks

Diagnostics are opt-in. Configure them before the renderer is constructed; the default renderer path does not read the timer or retain samples:

```csharp
public sealed class WorldRendering : IRendererConfiguration
{
    public void Configure(RendererOptions options)
    {
        options.Diagnostics = new RendererDiagnosticsOptions
        {
            HistoryCapacity = 180,
            SlowFrameThreshold = TimeSpan.FromMilliseconds(16.67),
        };

        options.AddLayer(Layers.Sprites);
        options.AddLayer(Layers.Effects, new RenderLayerOptions { Order = 100 });
    }
}
```

`Renderer2D.Diagnostics` exposes the latest frame, per-layer `SubmissionCount`, prepare/execute timings, and a rolling summary (average, min, p95, max, slow-frame count). Submission counts include custom pass data; pass-specific sorting is included in `PrepareMilliseconds`. `CopyHistory(Span<RendererFrameSample>)` copies retained frames without allocating. `CreateDebugReport()` formats the current snapshot for a log or debugger; call it on demand, since creating a string allocates.

Use `GetSummary()` for a throttled development overlay. Call `CreateDebugReport()` on demand from a debug command rather than logging it every frame:

```csharp
var renderer2D = renderer as Renderer2D;
var diagnostics = renderer2D?.Diagnostics;
if (diagnostics is not null)
{
    var summary = diagnostics.GetSummary();
    logger.LogDebug("Renderer p95: {P95:F2} ms", summary.P95Milliseconds);
    logger.LogDebug("{Report}", diagnostics.CreateDebugReport());
}
```

Run the benchmark after renderer initialization from a development command or isolated test scene, not from the normal draw callback. Keep the same scene submissions for each measured frame, and do not also let `RendererGameComponent` flush the renderer during the benchmark:

```csharp
var result = RendererBenchmark.Run(() =>
{
    renderer.BeginFrame();
    renderer.Context.Batch(Layers.Sprites).Draw(sprite, position, Color.White);
    renderer.Flush();
}, warmupFrames: 60, measuredFrames: 300);

logger.LogInformation("Renderer avg {Average:F2} ms, p95 {P95:F2} ms",
    result.AverageMilliseconds,
    result.P95Milliseconds);
```

`RendererBenchmark` reports CPU wall time for the action, after warm-up. It does not wait for queued GPU work to finish; use a graphics profiler for GPU timings. Benchmark results are comparable only when the device, scene, resolution, and workload are held constant.

## Lifecycle and current limits

- Do not call `Flush` from a constructor, host service configuration, or before device initialization. An explicit `Initialize` must complete first. World-owned renderers initialize with the world; `RendererGameComponent.Initialize` handles globally scheduled renderers. Manually driven renderers must do it themselves.
- Only an explicit `BeginFrame()` clears pass submissions and text and samples screen layout. Call it before producers when starting new data. `Batch` and `Flush` never clear the queue; repeated flushes replay it, and additional submissions append until the next explicit reset.
- A renderer is only flushed by the controller or `RendererGameComponent` that owns it. Missing or incompatible optional layers return an inactive `LayerWriter<T>`; required access rejects those composition errors. Compatible disabled renderers and layers return inactive writers in either mode.
- The renderer currently has no built-in multithreaded submission synchronization. Submit and flush on the game/graphics thread. Pass queues and text buffers reuse capacity but may grow on their first large frame; layer and output targets are created when first used and recreated when their size changes. Post-process scratch targets are recreated when their source dimensions or format changes.
- `SpriteBatchBatcher` is the current default; `DrawCommandPass` batches consecutive sprite/text commands and closes the batch before drawing primitives. Text is submitted through a reused `StringBuilder` because MonoGame 3.8.5 has no `ReadOnlySpan<char>` `DrawString` overload. Measure allocation and draw-call targets on the actual workload rather than assuming they are always zero.