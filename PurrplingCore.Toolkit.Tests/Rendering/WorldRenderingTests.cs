using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PurrplingCore.Ecs;
using PurrplingCore.Ecs.DI;
using PurrplingCore.Ecs.Systems;
using PurrplingCore.Ecs.WorldBuilding;
using PurrplingCore.Rendering;
using PurrplingCore.Toolkit.Collections;
using PurrplingCore.Toolkit.Graphics;
using PurrplingCore.Toolkit.Rendering;
using Xunit;

namespace PurrplingCore.Toolkit.Tests.Rendering;

public class WorldRenderingTests
{
    private static readonly RenderLayerId Layer = RenderLayerId.Of("WorldRenderingTests.Main");

    [Fact]
    public void CallbacksRejectNullAndReportDuplicateDelegates()
    {
        var callbacks = new WorldBlueprint().Lifecycle.Drawing;
        var first = new FrameLog();
        var second = new FrameLog();
        var callback = new Action<WorldDrawingContext>(first.RecordLifecycle);
        var duplicate = new Action<WorldDrawingContext>(first.RecordLifecycle);

        Assert.NotSame(callback, duplicate);
        Assert.Throws<ArgumentNullException>(() => callbacks.Add(null!));
        Assert.True(callbacks.Add(callback));
        Assert.True(callbacks.Add(second.RecordLifecycle));
        Assert.False(callbacks.Add(duplicate));
        Assert.Throws<ArgumentNullException>(() => callbacks.Remove(null!));
        Assert.True(callbacks.Remove(duplicate));
        Assert.False(callbacks.Remove(callback));
        Assert.True(callbacks.Remove(second.RecordLifecycle));
        Assert.False(callbacks.Remove(second.RecordLifecycle));
    }

    [Fact]
    public void CallbackExecutorRunsOnlyRegisteredCallbacks()
    {
        var executor = new CallbackExecutor<int>();
        var values = new List<int>();
        executor.Execute(0);
        Assert.Empty(values);

        executor.Callbacks.Add(values.Add);
        executor.Execute(7);
        executor.Callbacks.Remove(values.Add);
        executor.Execute(8);

        Assert.Equal(new[] { 7 }, values);
        Assert.Equal(0, executor.Count);
    }

    [Fact]
    public void RegistrationChangesDuringExecutionAffectCurrentAndNextInvocation()
    {
        var executor = new CallbackExecutor<int>();
        var values = new List<int>();
        Action<int> added = value => values.Add(value * 10);
        executor.Callbacks.Add(context =>
        {
            executor.Callbacks.Remove(values.Add);
            executor.Callbacks.Add(added);
        });
        executor.Callbacks.Add(values.Add);

        executor.Execute(1);
        Assert.Equal(new[] { 10 }, values);
        executor.Execute(2);

        Assert.Equal(new[] { 10, 20 }, values);
    }

    [Fact]
    public void ComposerExecutesCallbacksInRegistrationOrderWithProvidedContext()
    {
        using var services = new ServiceCollection().BuildServiceProvider();
        using var world = new World();
        var blueprint = new WorldBlueprint();
        var events = new List<string>();
        Action<WorldCompositionContext> first = context =>
        {
            Assert.Same(world, context.World);
            events.Add("first");
        };
        Action<WorldCompositionContext> second = context =>
        {
            Assert.Same(services, context.Services);
            events.Add("second");
        };
        blueprint.OnComposed(first).OnComposed(second).OnComposed(first);

        blueprint.CreateComposer(world, services).Compose();

        Assert.Equal(new[] { "first", "second" }, events);
    }

    [Fact]
    public void BlueprintCallbacksDoNotExposeExecutionOrDelegates()
    {
        var type = typeof(Callbacks<WorldCompositionContext>);

        Assert.Null(type.GetMethod("Execute"));
        Assert.Null(type.GetMethod("Snapshot"));
        Assert.Null(type.GetProperty("Item"));
        Assert.NotNull(type.GetProperty("Count"));
        Assert.Empty(type.GetConstructors());
        Assert.False(typeof(System.Collections.IEnumerable).IsAssignableFrom(type));
        Assert.Equal(new[] { "Add", "get_Count", "Remove" }, type
            .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.DeclaredOnly)
            .Select(method => method.Name).OrderBy(name => name));
        Assert.DoesNotContain(typeof(WorldLifecycleCallbacks).GetProperties(), property =>
            property.PropertyType.IsGenericType &&
            property.PropertyType.GetGenericTypeDefinition() == typeof(CallbackExecutor<>));
    }

    [Fact]
    public void ComposerKeepsCallbackSnapshotTakenAtConstruction()
    {
        using var services = new ServiceCollection().BuildServiceProvider();
        using var world = new World();
        var blueprint = new WorldBlueprint();
        var log = new FrameLog();
        blueprint.OnComposed(log.RecordLifecycle);
        var composer = blueprint.CreateComposer(world, services);
        blueprint.Lifecycle.Composed.Remove(log.RecordLifecycle);
        blueprint.OnComposed(context => log.Events.Add("late"));

        composer.Compose();

        Assert.Equal(new[] { nameof(WorldCompositionContext) }, log.Events);
    }

    [Fact]
    public void BlueprintLifecycleCallbacksAreUniqueAndKeepRegistrationOrder()
    {
        using var services = new ServiceCollection().BuildServiceProvider();
        using var world = new World();
        var blueprint = new WorldBlueprint();
        var first = new FrameLog();
        var second = new FrameLog();
        blueprint
            .OnComposed(first.RecordLifecycle)
            .OnComposed(second.RecordLifecycle)
            .OnComposed(first.RecordLifecycle)
            .OnInitialized(first.RecordLifecycle)
            .OnInitialized(second.RecordLifecycle)
            .OnInitialized(first.RecordLifecycle)
            .OnUpdated(first.RecordLifecycle)
            .OnUpdated(second.RecordLifecycle)
            .OnUpdated(first.RecordLifecycle)
            .OnDrawing(first.RecordLifecycle)
            .OnDrawing(second.RecordLifecycle)
            .OnDrawing(first.RecordLifecycle)
            .OnDrawn(first.RecordLifecycle)
            .OnDrawn(second.RecordLifecycle)
            .OnDrawn(first.RecordLifecycle)
            .OnDisposed(first.RecordLifecycle)
            .OnDisposed(second.RecordLifecycle)
            .OnDisposed(first.RecordLifecycle);
        blueprint.Lifecycle.Composed.Add(first.RecordLifecycle);
        blueprint.Lifecycle.Initialized.Add(first.RecordLifecycle);
        blueprint.Lifecycle.Updated.Add(first.RecordLifecycle);
        blueprint.Lifecycle.Drawing.Add(first.RecordLifecycle);
        blueprint.Lifecycle.Drawn.Add(first.RecordLifecycle);
        blueprint.Lifecycle.Disposed.Add(first.RecordLifecycle);

        blueprint.CreateComposer(world, services).Compose();
        world.Initialize();
        world.Update(default);
        world.Draw(default);
        world.Dispose();

        var expected = new[]
        {
            nameof(WorldCompositionContext), nameof(WorldInitializedContext), nameof(WorldUpdatedContext),
            nameof(WorldDrawingContext), nameof(WorldDrawnContext), nameof(WorldDisposedContext),
        };
        Assert.Equal(expected, first.Events);
        Assert.Equal(expected, second.Events);
    }

    [Fact]
    public void WorldRenderersHaveIndependentQueuesAndDoNotReplaceGlobalUi()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(new Screen(new Point(320, 180)));
        services.AddSingleton<IGraphicsDeviceService, DeviceService>();
        services.AddRenderer("UI").AddLayer(Layer).AddDrawComponent(DrawStage.Screen);
        services.AddWorld<RenderedWorld>()
            .AddService<RenderProbe>()
            .AddRenderer<Configuration>()
            .AddRenderer<Configuration>();
        using var provider = BuildProvider(services);
        using var first = CreateWorld<RenderedWorld>(provider);
        using var second = CreateWorld<RenderedWorld>(provider);
        var firstRenderer = first.Services.GetRequiredService<WorldRenderer>();
        var secondRenderer = second.Services.GetRequiredService<WorldRenderer>();
        var ui = provider.GetRequiredKeyedService<IRenderer>("UI");

        first.Services.GetRequiredService<RenderContext>().Batch(Layer).Submit(new DrawCommand());

        Assert.NotSame(firstRenderer.Renderer, secondRenderer.Renderer);
        Assert.Same(firstRenderer.Context, first.Services.GetRequiredService<RenderContext>());
        Assert.Equal(1, first.Services.GetRequiredService<RenderProbe>().Pass!.SubmissionCount);
        Assert.Equal(0, second.Services.GetRequiredService<RenderProbe>().Pass!.SubmissionCount);
        Assert.Same(ui, Assert.Single(provider.GetServices<IRenderer>()));
        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IGameComponent));
        Assert.Single(first.Services.GetKeyedServices<IWorldModule>(typeof(RenderedWorld)));
        first.Dispose();
        Assert.Same(ui.Context, provider.GetRequiredKeyedService<RenderContext>("UI"));
        Assert.Equal(0, second.Services.GetRequiredService<RenderProbe>().Pass!.SubmissionCount);
    }

    [Fact]
    public void RendererInitializesOnceAndFlushesAfterWorldDrawSystems()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IGraphicsDeviceService, DeviceService>();
        services.AddWorld<RenderedWorld>()
            .AddFeature<WorldRenderingFeature>()
            .AddService<FrameLog>()
            .AddService<WorldRenderer>(provider =>
                new WorldRenderer(new RecordingRenderer(provider.GetRequiredService<FrameLog>()),
                    provider.GetRequiredService<IGraphicsDeviceService>()))
            .AddModule<DrawModule>();
        services.AddWorld<HeadlessWorld>();
        using var provider = BuildProvider(services);
        using var world = CreateWorld<RenderedWorld>(provider);
        using var headless = CreateWorld<HeadlessWorld>(provider);
        var renderer = Assert.IsType<RecordingRenderer>(world.Services.GetRequiredService<WorldRenderer>().Renderer);
        var log = world.Services.GetRequiredService<FrameLog>();

        world.Initialize();
        world.Initialize();
        world.Draw(default);
        headless.Initialize();
        headless.Draw(default);

        Assert.Equal(new[] { "initialize", "begin", "draw", "flush" }, log.Events);
        world.Dispose();
        world.Dispose();
        Assert.Equal(1, renderer.DisposeCount);
    }

    [Fact]
    public void DerivedWorldAttachesItsRendererOnlyOnce()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IGraphicsDeviceService, DeviceService>();
        services.AddWorld<RenderedWorld>()
            .AddFeature<WorldRenderingFeature>()
            .AddService<FrameLog>()
            .AddService<WorldRenderer>(CreateRecordingRenderer);
        services.AddWorld<HeadlessWorld>()
            .AddFeature<WorldRenderingFeature>()
            .AddService<FrameLog>()
            .AddService<WorldRenderer>(CreateRecordingRenderer)
            .DeriveWorld<RenderedWorld>();
        using var provider = BuildProvider(services);
        using var world = CreateWorld<HeadlessWorld>(provider);

        world.Initialize();
        world.Draw(default);

        Assert.Equal(new[] { "initialize", "begin", "flush" }, world.Services.GetRequiredService<FrameLog>().Events);
    }

    [Fact]
    public void WorldRendererBeginsOncePerDrawAndRetainsDataAcrossFlushes()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IGraphicsDeviceService, DeviceService>();
        services.AddWorld<RenderedWorld>()
            .AddFeature<WorldRenderingFeature>()
            .AddService<FrameLog>()
            .AddService<WorldRenderer>(CreateRecordingRenderer)
            .AddModule<DrawModule>();
        using var provider = BuildProvider(services);
        using var world = CreateWorld<RenderedWorld>(provider);
        var renderer = world.Services.GetRequiredService<WorldRenderer>().Renderer;
        world.Initialize();

        world.Draw(default);
        Assert.Equal(0, renderer.Context.TextBuffer.Append("queued"));

        renderer.Flush();
        renderer.Flush();

        Assert.Equal(6, renderer.Context.TextBuffer.Append("retained"));

        world.Draw(default);

        Assert.Equal(0, renderer.Context.TextBuffer.Append("next"));
        Assert.Equal(new[]
        {
            "initialize", "begin", "draw", "flush", "flush", "flush",
            "begin", "draw", "flush",
        }, world.Services.GetRequiredService<FrameLog>().Events);
    }

    [Fact]
    public void DrawingCallbacksResetOnlyTheirWorldBeforeSystems()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IGraphicsDeviceService, DeviceService>();
        services.AddWorld<RenderedWorld>()
            .AddFeature<WorldRenderingFeature>()
            .AddService<FrameLog>()
            .AddService<WorldRenderer>(CreateRecordingRenderer)
            .AddModule<DrawingModule>();
        using var provider = BuildProvider(services);
        using var first = CreateWorld<RenderedWorld>(provider);
        using var second = CreateWorld<RenderedWorld>(provider);
        var firstRenderer = first.Services.GetRequiredService<WorldRenderer>().Renderer;
        var secondRenderer = second.Services.GetRequiredService<WorldRenderer>().Renderer;
        var firstLog = first.Services.GetRequiredService<FrameLog>();
        var secondLog = second.Services.GetRequiredService<FrameLog>();
        first.Initialize();
        second.Initialize();

        first.Draw(default);
        Assert.Equal(0, firstRenderer.Context.TextBuffer.Append("queued"));
        firstRenderer.Flush();
        Assert.Equal(6, firstRenderer.Context.TextBuffer.Append("retained"));

        second.Draw(default);
        Assert.Equal(0, secondRenderer.Context.TextBuffer.Append("queued"));
        Assert.Equal(14, firstRenderer.Context.TextBuffer.Append("still queued"));

        first.Draw(default);
        Assert.Equal(0, firstRenderer.Context.TextBuffer.Append("next"));
        Assert.Equal(6, secondRenderer.Context.TextBuffer.Append("retained"));
        Assert.Equal(new[]
        {
            "initialize", "begin", "drawing", "draw", "flush", "drawn", "flush",
            "begin", "drawing", "draw", "flush", "drawn",
        }, firstLog.Events);
        Assert.Equal(new[] { "initialize", "begin", "drawing", "draw", "flush", "drawn" }, secondLog.Events);
    }

    [Fact]
    public void DrawingHookDoesNotRunBeforeInitializationOrAfterDisposal()
    {
        using var world = new World();
        var log = new FrameLog();
        world.Drawing += log.RecordDrawing;

        Assert.Throws<InvalidOperationException>(() => world.Draw(default));
        Assert.Empty(log.Events);

        world.Initialize();
        world.Draw(default);
        world.Dispose();

        Assert.Throws<ObjectDisposedException>(() => world.Draw(default));
        Assert.Equal(new[] { "drawing" }, log.Events);
    }

    [Fact]
    public void HeadlessWorldDoesNotRequireGraphicsServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddWorld<HeadlessWorld>();
        using var provider = BuildProvider(services);
        using var world = CreateWorld<HeadlessWorld>(provider);

        world.Initialize();
        world.Update(default);
        world.Draw(default);

        Assert.Null(world.Services.GetService<IGraphicsDeviceService>());
        Assert.Null(world.Services.GetService<WorldRenderer>());
    }

    private static WorldRenderer CreateRecordingRenderer(IServiceProvider provider) =>
        new(new RecordingRenderer(provider.GetRequiredService<FrameLog>()),
            provider.GetRequiredService<IGraphicsDeviceService>());

    private static ServiceProvider BuildProvider(IServiceCollection services) =>
        services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });

    private static ManagedWorld CreateWorld<TMarker>(IServiceProvider provider) where TMarker : IWorldMarker
    {
        var signature = WorldSignature.For<TMarker>();
        return (ManagedWorld)provider.GetRequiredKeyedService<IWorldFactory>(signature.MarkerType)
            .CreateWorld(typeof(TMarker).Name, signature);
    }

    public sealed class RenderedWorld : IWorldMarker;
    public sealed class HeadlessWorld : IWorldMarker;

    public sealed class RenderProbe
    {
        public DrawCommandPass? Pass { get; set; }
    }

    public sealed class Configuration(RenderProbe probe) : IRendererConfiguration
    {
        public void Configure(RendererOptions options)
        {
            var pass = new DrawCommandPass();
            probe.Pass = pass;
            options.AddLayer(Layer, new RenderLayerOptions { Pass = pass });
        }
    }

    public sealed class FrameLog
    {
        public List<string> Events { get; } = [];

        public void RecordLifecycle<TContext>(TContext context) => Events.Add(typeof(TContext).Name);

        public void RecordDrawing(IWorld world, UpdateTick tick) => Events.Add("drawing");
    }

    public sealed class DrawModule : IWorldModule
    {
        public int Order => 0;
        public void Setup(IWorldBlueprint builder) => builder.AddSystem<DrawRecorder>();
    }

    public sealed class DrawingModule(FrameLog log) : IWorldModule
    {
        public int Order => 0;

        public void Setup(IWorldBlueprint builder)
        {
            builder.OnDrawing(OnDrawing);
            builder.AddSystem<DrawRecorder>();
            builder.OnDrawn(OnDrawn);
        }

        private void OnDrawing(WorldDrawingContext context)
        {
            Assert.Same(log, context.Services.GetRequiredService<FrameLog>());
            log.Events.Add("drawing");
        }

        private void OnDrawn(WorldDrawnContext context) => log.Events.Add("drawn");
    }

    [PurrplingCore.Ecs.Attributes.System<DrawSystemGroup>]
    public sealed class DrawRecorder(RenderContext context, FrameLog log) : BaseSystem
    {
        protected override void OnUpdateGroup()
        {
            context.Batch(Layer).Submit(new DrawCommand());
            log.Events.Add("draw");
        }
    }

    private sealed class RecordingRenderer(FrameLog log) : IRenderer
    {
        private readonly Renderer2D _renderer = new("RecordingWorld", new RendererOptions(new Screen(new Point(320, 180)))
        {
            Layers = { new(Layer, new RenderLayerOptions()) },
        });

        public string Name => _renderer.Name;
        public bool Enabled { get => _renderer.Enabled; set => _renderer.Enabled = value; }
        public RenderContext Context => _renderer.Context;
        public int DisposeCount { get; private set; }
        public void Initialize(GraphicsDevice device) => log.Events.Add("initialize");
        public void BeginFrame()
        {
            log.Events.Add("begin");
            _renderer.BeginFrame();
        }
        public void Flush() => log.Events.Add("flush");
        public void Dispose()
        {
            DisposeCount++;
            _renderer.Dispose();
        }
    }

    public sealed class DeviceService : IGraphicsDeviceService
    {
        public GraphicsDevice GraphicsDevice => null!;
        public event EventHandler<EventArgs>? DeviceCreated { add { } remove { } }
        public event EventHandler<EventArgs>? DeviceDisposing { add { } remove { } }
        public event EventHandler<EventArgs>? DeviceReset { add { } remove { } }
        public event EventHandler<EventArgs>? DeviceResetting { add { } remove { } }
    }
}