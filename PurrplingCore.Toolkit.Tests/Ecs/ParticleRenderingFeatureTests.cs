using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PurrplingCore.Ecs;
using PurrplingCore.Ecs.DI;
using PurrplingCore.Ecs.Particles;
using PurrplingCore.Ecs.Particles.Systems;
using PurrplingCore.Ecs.WorldBuilding;
using PurrplingCore.Rendering;
using PurrplingCore.Toolkit.Graphics;
using PurrplingCore.Toolkit.Rendering;
using Xunit;

namespace PurrplingCore.Toolkit.Tests.Ecs;

public class ParticleRenderingFeatureTests
{
    private static readonly RenderLayerId Layer = ParticleRenderingOptions.DefaultLayer;
    private static readonly RenderLayerId OtherLayer = RenderLayerId.Of("ParticleRenderingFeatureTests.OtherLayer");

    [Fact]
    public void RenderingFeatureAddsSimulationAndUsesWorldRendererContext()
    {
        var services = CreateServices();
        services.AddSingleton(new Screen(new Point(320, 180)));
        services.AddSingleton<IGraphicsDeviceService, DeviceService>();
        services.AddWorld<RenderedWorld>()
            .AddRenderer<TestRendererConfiguration>()
            .AddParticleModule()
            .AddFeature<ParticleRenderingFeature>();
        using var provider = BuildProvider(services);
        using var world = CreateWorld<RenderedWorld>(provider);

        Assert.IsType<WorldRenderer>(world.Services.GetRequiredService<WorldRenderer>());
        Assert.Single(world.Services.GetKeyedServices<IWorldModule>(typeof(RenderedWorld))
            .OfType<ParticleModule>());
        Assert.NotNull(world.Services.GetRequiredService<IWorldOptions<ParticleOptions>>().Value);
    }

    [Fact]
    public void RenderingLayerIsSelectedIndependentlyForEachWorldMarker()
    {
        var services = CreateServices();
        services.AddSingleton(new Screen(new Point(320, 180)));
        services.AddSingleton<IGraphicsDeviceService, DeviceService>();
        services.AddWorld<RenderedWorld>()
            .AddRenderer<TestRendererConfiguration>()
            .AddFeature<ParticleRenderingFeature>();
        services.AddWorld<SecondRenderedWorld>()
            .AddRenderer<TestRendererConfiguration>()
            .AddFeature<ParticleRenderingFeature>()
            .ConfigureOptions<ParticleRenderingOptions>(options => options.Layer = OtherLayer);
        using var provider = BuildProvider(services);
        using var first = CreateWorld<RenderedWorld>(provider);
        using var second = CreateWorld<SecondRenderedWorld>(provider);

        Assert.Equal(Layer, first.Services.GetRequiredService<IWorldOptions<ParticleRenderingOptions>>().Value.Layer);
        Assert.Equal(OtherLayer,
            second.Services.GetRequiredService<IWorldOptions<ParticleRenderingOptions>>().Value.Layer);
        Assert.Same(first.Services.GetRequiredService<RenderContext>(),
            first.Services.GetRequiredService<WorldRenderer>().Context);
        Assert.Same(second.Services.GetRequiredService<RenderContext>(),
            second.Services.GetRequiredService<WorldRenderer>().Context);
    }

    [Fact]
    public void RenderingFeatureCanBorrowASharedGlobalRendererContext()
    {
        const string rendererKey = "SharedParticleRenderer";
        var services = CreateServices();
        services.AddSingleton(new Screen(new Point(320, 180)));
        services.AddRenderer(rendererKey).AddLayer(Layer);
        services.AddWorld<SharedWorld>()
            .AddService<RenderContext>(provider =>
                provider.GetRequiredKeyedService<RenderContext>(rendererKey))
            .AddFeature<ParticleRenderingFeature>();
        using var provider = BuildProvider(services);
        using var world = CreateWorld<SharedWorld>(provider);
        var sharedContext = provider.GetRequiredKeyedService<RenderContext>(rendererKey);

        Assert.Same(sharedContext, world.Services.GetRequiredService<RenderContext>());
        Assert.Null(world.Services.GetService<WorldRenderer>());
    }

    [Fact]
    public void RenderingFeatureRequiresALayerDuringWorldComposition()
    {
        var services = CreateServices();
        services.AddSingleton(new Screen(new Point(320, 180)));
        services.AddSingleton<IGraphicsDeviceService, DeviceService>();
        services.AddWorld<RenderedWorld>()
            .AddRenderer<TestRendererConfiguration>()
            .AddFeature<ParticleRenderingFeature>()
            .ConfigureOptions<ParticleRenderingOptions>(options => options.Layer = default);
        using var provider = BuildProvider(services);

        Assert.Throws<OptionsValidationException>(() => CreateWorld<RenderedWorld>(provider));
    }

    private static IServiceCollection CreateServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        return services;
    }

    private static ServiceProvider BuildProvider(IServiceCollection services) =>
        services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });

    private static ManagedWorld CreateWorld<TMarker>(IServiceProvider provider) where TMarker : IWorldMarker
    {
        var signature = WorldSignature.For<TMarker>();
        return (ManagedWorld)provider.GetRequiredKeyedService<IWorldFactory>(signature.MarkerType)
            .CreateWorld(typeof(TMarker).Name, signature);
    }

    public sealed class RenderedWorld : IWorldMarker;
    public sealed class SecondRenderedWorld : IWorldMarker;
    public sealed class SharedWorld : IWorldMarker;

    private sealed class TestRendererConfiguration : IRendererConfiguration
    {
        private readonly RenderLayerId _layer;

        public TestRendererConfiguration(IWorldOptions<ParticleRenderingOptions> options)
        {
            _layer = options.Value.Layer;
        }

        public void Configure(RendererOptions options) => options.AddLayer(_layer);
    }

    private sealed class DeviceService : IGraphicsDeviceService
    {
        public GraphicsDevice GraphicsDevice => null!;
        public event EventHandler<EventArgs>? DeviceCreated { add { } remove { } }
        public event EventHandler<EventArgs>? DeviceDisposing { add { } remove { } }
        public event EventHandler<EventArgs>? DeviceReset { add { } remove { } }
        public event EventHandler<EventArgs>? DeviceResetting { add { } remove { } }
    }
}