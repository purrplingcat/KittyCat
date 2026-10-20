using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xna.Framework.Graphics;
using PurrplingCore.Ecs;
using PurrplingCore.Ecs.DI;
using PurrplingCore.Ecs.WorldBuilding;
using PurrplingCore.Toolkit.Graphics;
using PurrplingCore.Toolkit.Rendering;

namespace PurrplingCore.Rendering;

/// <summary>
/// Attaches an independently owned renderer to each instance of the configured world.
/// </summary>
public sealed class WorldRenderingFeature : IWorldFeature
{
    private readonly record struct ConfigurationKey(Type MarkerType);

    private sealed class RendererModule(WorldRenderer renderer) : IWorldModule
    {
        public int Order => 0;

        public void Setup(IWorldBlueprint builder) => renderer.Attach(builder);
    }

    /// <summary>
    /// Registers the world-owned renderer, its context, and its lifecycle integration.
    /// </summary>
    public void ConfigureServices(IWorldServicesBuilder builder)
    {
        builder.AddService<WorldRenderer>(CreateRenderer)
            .AddService<RenderContext>(provider => provider.GetRequiredService<WorldRenderer>().Context)
            .AddModule<RendererModule>();
    }

    internal static object GetConfigurationKey(WorldSignature signature) => new ConfigurationKey(signature.MarkerType);

    private static WorldRenderer CreateRenderer(IServiceProvider provider)
    {
        var context = provider.GetRequiredService<IWorldContext>();
        var graphics = provider.GetRequiredService<IGraphicsDeviceService>();
        var name = string.IsNullOrEmpty(context.Name) ? context.Signature.MarkerType.Name : context.Name;
        var options = new RendererOptions(provider.GetRequiredService<Screen>());
        foreach (var module in provider.GetKeyedServices<IRendererConfiguration>(GetConfigurationKey(context.Signature)))
        {
            module.Configure(options);
        }

        return new WorldRenderer(new Renderer2D(name, options), graphics);
    }
}