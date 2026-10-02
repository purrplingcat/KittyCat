# 2D rendering

The 2D renderer is optional. Draw systems, scenes, and ordinary objects submit commands to a renderer; the renderer sends them to MonoGame only when it is flushed. The core types live in `PurrplingCore.Toolkit.Rendering`; DI integration and the optional game component live in `PurrplingCore.Rendering`. Neither ECS nor `SceneManager` is required to use the renderer.

## Choose an execution mode

| Mode | Register | Who calls `Initialize` and `Flush`? |
| --- | --- | --- |
| Automatic renderer | `AddRenderer(key).AddLayer(...).AddDrawComponent()` | `RendererGameComponent`, via `Game.Components` |
| Manual renderer via DI | `AddRenderer(key)` | Your controller calls `Initialize(device)` and `Flush()` |
| No DI integration | `new Renderer2D(name, options)` | Your code owns initialization, flushing, and disposal |

Renderer identity is a keyed-service key of type `object`. Use the same key for `AddRenderer`, `.AddModule<T>()`, and `[FromKeyedServices(key)] IRenderer`. Strings, enums, and other stable objects are supported. A configuration class is only a DI module, never the renderer marker. There is no global renderer order or group: each renderer is initialized and flushed by its own controller, component, or manual draw path. `AddRenderer(key)` only registers the service; it does not schedule rendering. Add `.AddDrawComponent()` to register a component that initializes and flushes it; the default order is `DrawStage.Default` (`0`). Pass a custom `drawOrder` when it must run after a particular submitter. Each call adds another component; a different order does not replace an earlier registration. Without it, your own controller must initialize and flush the renderer. The lower-level `services.AddRendererGameComponent(key, drawOrder)` remains available for advanced service registration.

The flow is:

```text
producer.Draw() -> renderer.Context.Layer(id).Draw(...) -> per-layer command queue
RendererGameComponent.Draw() or your own Draw() -> renderer.Flush()
```

Submission must happen before the relevant flush **in the same frame**. `Flush` never initializes GPU resources: call `Initialize` first. `Initialize` binds the renderer to that device; every subsequent call throws. A `RendererGameComponent` does this during its `Initialize` lifecycle; a manually driven renderer is your responsibility. Flushing before initialization throws `InvalidOperationException`.

## Logical screens and split-screen

`PurrplingCore.Toolkit.Graphics.Screen` describes a logical size and a presentation area, not a GPU target. A root screen can follow `GraphicsManager.Resolution` or use an independent fixed size. Regions inherit a fraction of their parent's size and are placed inside the parent's presented image, including letterboxing. A region can use a fixed logical size without changing its position:

```csharp
using var main = new Screen(graphics.Resolution);
var left = main.CreateRegion(0f, 0f, 0.5f, 1f);
var right = main.CreateRegion(0.5f, 0f, 0.5f, 1f);
right.UseFixedSize(new Point(320, 360));

var options = new RendererOptions(left);
options.AddLayer(Layers.Sprites);
var renderer = new Renderer2D("Left", options);
```

Regions may be nested. Adjacent regions round shared boundaries consistently, including odd output sizes. `RendererOptions.ScaleMode` controls parent-image placement, child-region mapping, internal layer composition, and final image scaling, defaulting to `Fit`. The same mode is used throughout the screen hierarchy so regions align with the parent image's letterboxing. Renderer configuration does not change the shared screen. `Screen.ScaleMode` is used for standalone `GetPresentationBounds` calculations.

`CreateRegion` offsets are fractions of the parent's dimensions and may be negative or extend beyond the parent. Width and height remain positive fractions up to one, independent of position. For example, `CreateRegion(-0.25f, 0f, 1f, 1f)` keeps the full logical size and shifts the region left by a quarter of the parent image's width. Rendering outside the output or ancestor regions is clipped.

`RendererOptions.Alignment` independently positions the scaled image, defaulting to `RenderAlignment.Center`. It supports the center, four edges, and four corners. Alignment also applies to ancestor-image placement when resolving regions; internal layer composition stays centered. For an integer-scaled HUD at the physical top edge and horizontal center, keep the root screen and configure:

```csharp
options.Output = RenderTargetSpec.MatchScreen();
options.ScaleMode = RenderScaleMode.IntegerFit;
options.Alignment = RenderAlignment.TopCenter;
```

The world renderer may independently use `Fit` and the default `Center`. A HUD using a region aligns within that region, not the entire window. `MatchBackBuffer` targets already match the backbuffer's pixel dimensions, so they do not gain integer enlargement when composed onto the same backbuffer.

`UsePurrplingCore()` registers the shared main `Screen` as an engine game service, bound to the engine's `Resolution`, even when no renderer is registered. A pre-registered main screen is preserved. `Screen` is required on `RendererOptions`. `AddRenderer(key)` creates options with the main screen only when its keyed renderer is first resolved and an `IServiceProvider` is available. It then applies registered configuration modules before constructing the renderer. Select a custom screen directly in configuration:

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

`Screen` caches the logical size and updates it through `Resolution.ResolutionChanged`, raised by `ApplyChanges()`. `SetResolution()` alone stages the change; a normal window resize changes the physical backbuffer without changing the logical size. Child regions derive their size from their parent. `UseFixedSize()` unsubscribes from the source while retaining the screen's presentation area.

Screen layout is sampled at initialization and at the start of each frame, before camera synchronization. Applied changes affect targets and cameras on the next frame. Use `renderer.Screen.Size` for the screen's current logical size; `MatchScreen()` uses the frame's sampled screen size, and `MatchBackBuffer()` always follows the physical backbuffer. The screen never changes global `Resolution` or allocates GPU resources. The renderer only reads the screen and output dimensions; it does not subscribe to device-reset events or manage the window. DI disposes its shared screen; callers dispose explicitly supplied screens. Automatic and scene-owned renderers use exactly the same sizing and presentation code.

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

The game adds registered `IGameComponent` services to `Game.Components` during `Game.Initialize()`. Each renderer component initializes and flushes only its keyed renderer. `layer.Order` orders layers **inside a renderer**; `drawOrder` on `AddDrawComponent` orders the component relative to other MonoGame components and defaults to `0`. Calling `AddDrawComponent` again adds another component, even with a different `drawOrder`. `DrawStage` contains optional `int` anchors for components, not renderers; a game can use its own values.

At equal `DrawOrder`, MonoGame 3.8.5 draws components in registration order. Give each renderer component an explicit draw order after its producers. If two renderers need to be flushed at different points in the MonoGame draw loop, register two components with different `drawOrder` values.

### Quick path: no configuration classes

For a small game, use the same builder without a configuration class:

```csharp
services.AddRenderer("World")
    .AddLayer(Layers.Sprites)
    .AddDrawComponent();
```

The renderer also joins `IEnumerable<IRenderer>`. Inject it with `[FromKeyedServices(RendererKey.World)] IRenderer renderer`, then submit through `renderer.Context.Layer(Layers.Sprites)`. Omit `.AddDrawComponent()` for manual ownership; your controller must call `Initialize` and `Flush`. Add an `IRendererConfiguration` module under the same key when camera, effect, or other service dependencies must be injected; never build a second provider inside `ConfigureServices`.

### Submitting from ECS

Friflo draw systems only submit; they do not call `Flush` or `SpriteBatch.Begin/End`. Here is the same chunk-iteration style used by KittyCat (Friflo.Engine.ECS 3.6.0):

```csharp
using Friflo.Engine.ECS.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PurrplingCore.Ecs.Attributes;
using PurrplingCore.Ecs.Systems;
using PurrplingCore.Toolkit.Rendering;

[System<DrawSystemGroup>]
public sealed class SpriteDrawSystem([FromKeyedServices(RendererKey.World)] IRenderer renderer)
    : QuerySystem<Position2D, Sprite2D>
{
    protected override void OnUpdate()
    {
        var writer = renderer.Context.Layer(Layers.Sprites);
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

`Position2D` and `Sprite2D` above are game-owned Friflo components; provide your own definitions (`IComponent`), with `Vector2 Value`, `Texture2D Texture`, and `bool IsVisible` respectively. Register the system in your world blueprint (KittyCat uses `world.AddSystem<StarCatcherDrawSystem>()`). Set the world renderer component's `drawOrder` after the world controller so the system submits before the renderer flushes. See `KittyCat/KittyCat.Game/Ecs/Systems/StarCatcherDrawSystem.cs` and `KittyCat/KittyCat.Game/Ecs/Systems/StarCatcherHudSystem.cs` for the in-repo direct-rendering examples.

### Submitting from an ordinary object

Nothing in `RenderContext` depends on ECS. Submit from a UI object, an animation manager, or a game-level draw method:

```csharp
public sealed class BadgeOverlay([FromKeyedServices(RendererKey.Screen)] IRenderer renderer)
{
    public void Draw(Texture2D badge)
    {
        renderer.Context.Layer(Layers.Hud)
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
        => Context.Layer(Layers.Menu)
            .DrawString(_font, "PAUSED", new Vector2(120, 80), Color.White);
}
```

Register `services.AddSceneManager(); services.AddScene<MenuScene>();` and register `RendererKey.Screen` as above. The scene submits; the renderer component presents later. A `RendererScene` must **not** use `SpriteBatch` in its `Draw`: no canvas or batch has been opened for it.

Canvas and deferred scenes can coexist, but they have different draw timing. If `SceneManager` blits a canvas before a later renderer clears the back buffer (`RendererOptions.ClearColor`), the canvas disappears. Place the canvas scene's draw after that renderer, or remove/relocate the clear when combining those paths. `RenderMode` is per scene, not a global switch.

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
        Context.Layer(Layers.Menu).Draw(_image, Vector2.Zero, Color.White);
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
        _renderer.Context.Layer(World).Draw(_hero, new Vector2(32, 32), Color.White);
        _renderer.Flush();
        base.Draw(gameTime);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _renderer.Dispose();
            _screen.Dispose();
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

With this setup, nothing invokes `Initialize` or `Flush` for you. `Initialize` creates GPU resources; `Flush` requires it and must run on the graphics thread after submission. This example owns a fixed 320-by-180 screen independently of the backbuffer. Pass a `Screen` bound to `Resolution` when logical size should follow that source instead. Dispose the renderer and caller-owned screen separately. No component is needed to synchronize screen size.

### Manual screen size and camera viewport

Use a fixed `Screen` for an independent logical resolution. Enable `SyncCameraViewport` on camera layers to size the camera viewport and center its origin from the resolved layer target. The renderer traverses configured layers to synchronize their cameras during initialization and when each new frame opens, before returning its first layer writer. Changing the screen size takes effect at the start of the next frame without reinitializing the renderer. A camera shared by several layers may receive the same viewport more than once; `Camera2D` skips unchanged viewport sizes:

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

renderer.Context.Layer(World).Draw(texture, position, Color.White);
renderer.Flush();

screen.UseFixedSize(new Point(640, 360));
```

If the camera viewport should remain independent of the render target, leave `SyncCameraViewport` false and call `camera.SetViewport(width, height)` followed by `camera.CenterOrigin()` when your chosen viewport changes. Screen size controls the `MatchScreen` render target; it does not automatically alter game-world coordinates or camera position.

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
        _renderer.Context.Layer(World).Draw(_hero, new Vector2(32, 32), Color.White);
        _renderer.Flush();
        base.Draw(gameTime);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _renderer.Dispose();
            _screen.Dispose();
        }
        base.Dispose(disposing);
    }
}
```

## Layers, camera, targets, and extensions

Each renderer has ordered layers (`RenderLayerOptions.Order`). A layer inherits the renderer's `DefaultSamplerState` (`PointClamp` by default) unless overridden. `SortMode` defaults to `Submission`, preserving the order in which calls were queued. Other modes are `FrontToBack`, `BackToFront`, and `Texture`; sorting is done separately within each layer. `FrontToBack` sorts the `depth` argument to `Draw`/`DrawString` ascending (lower values first); `BackToFront` sorts it descending (higher values first). Equal depths retain submission order. This is CPU-side ordering only: the built-in SpriteBatch batcher submits every command with GPU `layerDepth` zero, so `depth` does not by itself provide depth-buffer testing. Renderers are independent and do not share a global sort order.

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
            SortMode = RenderSortMode.FrontToBack,
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

For an offscreen layer or renderer, set `layer.Target` or `options.Output` to `RenderTargetSpec.MatchScreen()`, `MatchBackBuffer()`, or `OfSize(width, height)`. Layer targets are composed onto the renderer output. `Renderer2D.OutputOverride` can direct the final composed image to an externally owned target; leave it null to use the back buffer. `IPostProcessor` chains can be added to a layer's `PostProcessors` or a renderer's `PostProcessors`; processors are initialized with the renderer and disposed with it. The renderer-level chain currently runs only when `options.Output` is set. A layer's chain requires `layer.Target`. Do not assume a global post-processing stage across multiple renderer groups exists.

`RenderLayerOptions.Pass` replaces the default `DrawCommandPass` with your `IRenderLayerPass`; its `Initialize(GraphicsDevice)` is called before drawing and `Prepare/Execute` run during flush. A custom pass must explicitly replay `context.Layer.Commands` if it wants queued commands to appear. `Primitives2D` provides `FillRect`, `DrawRect`, `DrawLine`, and `FillCircle` extensions on `LayerWriter`; they currently take a `GraphicsDevice` to obtain the shared white pixel. That first pixel creation is a GPU operation, so warm it up after device initialization if draw systems must only enqueue during the frame.

### Frame-scoped pass submissions

A custom pass can implement `ILayerSubmissionPass<T>` to receive typed values from producers without putting them in the ordinary draw-command list. Producers call `renderer.Context.Submissions<T>(layerId).Submit(in value)`. The returned writer is a `ref struct` intended to stay local to the submitting method. The pass also implements `ClearSubmissions()`; `BeginFrame()` calls it when a new frame opens, before any new submissions are added. Requesting submissions for a layer whose pass does not accept `T` throws `InvalidOperationException`.

### Custom pass and post-processor

A pass can issue its own draw calls instead of consuming layer commands. This example draws one texture using the batcher; the texture is supplied by the caller and remains caller-owned:

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

The `backdrop` must be loaded before configuration; `RenderLayerPass` does not own or dispose it. `Prepare` is optional on the `RenderLayerPass` base class. To retain regular queued commands as well, either replay `context.Layer.Commands` inside your pass or use a separate layer for the backdrop and leave `DrawCommandPass` on the sprite layer.

#### Drawing vertex geometry

For geometry that is not a sprite or text, use `RenderFrameContext.Device` directly. The renderer has already bound the layer target. Raw `GraphicsDevice` calls do not apply the layer's camera transform or batch state automatically, so the pass must apply the relevant `context.State` values itself. This pass allocates its effect and vertex array once, then draws one colored triangle per frame:

```csharp
public sealed class TrianglePass : RenderLayerPass
{
    private readonly VertexPositionColor[] _vertices =
    [
        new(new Vector3(80, 40, 0), Color.Cyan),
        new(new Vector3(40, 120, 0), Color.Magenta),
        new(new Vector3(120, 120, 0), Color.Yellow),
    ];

    private BasicEffect? _effect;

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
        effect.World = Matrix.Identity;
        effect.View = context.State.Transform;
        effect.Projection = Matrix.CreateOrthographicOffCenter(
            0, viewport.Width, viewport.Height, 0, 0, 1);

        device.BlendState = context.State.BlendState ?? BlendState.AlphaBlend;
        device.DepthStencilState = context.State.DepthStencilState ?? DepthStencilState.None;
        device.RasterizerState = context.State.RasterizerState ?? RasterizerState.CullNone;

        foreach (var pass in effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            device.DrawUserPrimitives(PrimitiveType.TriangleList, _vertices, 0, 1);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _effect?.Dispose();
        base.Dispose(disposing);
    }
}

options.AddLayer(Layers.Sprites, new RenderLayerOptions { Pass = new TrianglePass() });
```

`context.State.Transform` contains the layer camera/parallax transform. A custom pass replaces the default `DrawCommandPass`; it must handle every command kind it supports. `DrawUserPrimitives` is suitable for small, transient geometry; for larger or frequently changing meshes, create a `VertexBuffer` and submit it through the same `LayerWriter` as sprites and text.

`DrawPrimitives` adds a primitive command to the layer's ordered `DrawList`. This ECS-style loop interleaves geometry and sprites using one depth convention:

```csharp
var writer = renderer.Context.Layer(Layers.World);
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

The renderer sorts both kinds together according to the layer's `SortMode`. `FrontToBack` and `BackToFront` use each command's `depth`; equal depths preserve submission order. Primitive commands require a layer `Effect` implementing `IEffectMatrices`; its `Projection` is caller-configured, while the command supplies `World` and the layer camera supplies `View`. Vertex buffers remain caller-owned and must stay valid through `Flush()`.

To control every command field directly, construct a `DrawCommand` and submit it with the resource it references:

```csharp
var writer = renderer.Context.Layer(Layers.Mesh);
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

`LayerWriter.Submit` only appends the command: it does not resolve resources, validate the command kind, or alter its fields. Use `renderer.Context.Resources` to obtain texture, font, or vertex-buffer indices. For text, append characters through `renderer.Context.TextBuffer` and set `TextOffset`/`TextLength` yourself. Direct commands must contain valid indices and frame text offsets; resources must remain alive until `Flush()`. The regular `Draw` and `DrawString` helpers continue to resolve these details for you.

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

`Renderer2D.Diagnostics` exposes the latest frame, per-layer command/sort/prepare/execute timings, and a rolling summary (average, min, p95, max, slow-frame count). `CopyHistory(Span<RendererFrameSample>)` copies retained frames without allocating. `CreateDebugReport()` formats the current snapshot for a log or debugger; call it on demand, since creating a string allocates.

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
    renderer.Context.Layer(Layers.Sprites).Draw(sprite, position, Color.White);
    renderer.Flush();
}, warmupFrames: 60, measuredFrames: 300);

logger.LogInformation("Renderer avg {Average:F2} ms, p95 {P95:F2} ms",
    result.AverageMilliseconds,
    result.P95Milliseconds);
```

`RendererBenchmark` reports CPU wall time for the action, after warm-up. It does not wait for queued GPU work to finish; use a graphics profiler for GPU timings. Benchmark results are comparable only when the device, scene, resolution, and workload are held constant.

## Lifecycle and current limits

- Do not call `Flush` from a constructor, host service configuration, or before device initialization. An explicit `Initialize` must complete first. `RendererGameComponent.Initialize` handles this for its group; manually driven groups/renderers must do it themselves.
- The first submission after a flush opens a fresh frame and clears the previous command lists and frame-scoped pass submissions. `BeginFrame()` is available for explicit clearing but is not required each frame. Calling it **after** submitting commands discards those commands. Submitting again after an early flush starts a new queue.
- A renderer is only flushed by the controller or `RendererGameComponent` that owns it. Disabled renderers discard submissions; disabled layers return an inactive `LayerWriter` that also discards submissions.
- The renderer currently has no built-in multithreaded submission synchronization. Submit and flush on the game/graphics thread. Pool-backed command lists and text buffers reuse capacity but may grow on their first large frame; layer and output targets are created when first used and recreated when their size changes. Post-process scratch targets are recreated when their source dimensions or format changes.
- `SpriteBatchBatcher` is the current default; one `Begin/End` encloses each non-empty sprite layer. Text is submitted through a reused `StringBuilder` because MonoGame 3.8.5 has no `ReadOnlySpan<char>` `DrawString` overload. Measure allocation and draw-call targets on the actual workload rather than assuming they are always zero.