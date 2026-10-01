using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Xna.Framework;
using PurrplingCore.Graphics;
using PurrplingCore.Toolkit.Rendering;

namespace PurrplingCore.Rendering;

/// <summary>
/// Opt-in registration for layered 2D renderers.
/// </summary>
/// <remarks>Renderer identity and configuration-module lookup both use the same keyed-service key.</remarks>
public static class ServiceCollectionExtensions
{
    private sealed class RendererServicesBuilder(
        IServiceCollection services,
        string name,
        object key,
        RendererOptions options) : IRendererServicesBuilder
    {
        public IServiceCollection Services { get; } = services;
        public string Name { get; } = name;
        public object Key { get; } = key;
        public RendererOptions Options { get; } = options;
    }

    /// <summary>Begins a renderer declaration identified by an arbitrary keyed-service key.</summary>
    public static IRendererServicesBuilder AddRenderer(
        this IServiceCollection services,
        object key,
        int order = 1000,
        string? group = "Default")
    {
        ArgumentNullException.ThrowIfNull(key);
        if (group is not null) ArgumentException.ThrowIfNullOrEmpty(group);

        var options = new RendererOptions { Order = order, RenderGroup = group };
        var name = key as string ?? key.ToString() ?? key.GetType().Name;
        services.AddKeyedSingleton<IRenderer>(key, (provider, _) =>
        {
            var modules = provider.GetKeyedServices<IRendererConfiguration>(key);
            foreach (var module in modules)
            {
                module.Configure(options);
            }

            if (options.RenderGroup != group)
            {
                throw new InvalidOperationException("Set the render group in AddRenderer, not in the configuration type.");
            }

            return new Renderer2D(name, options);
        });

        services.AddSingleton<IRenderer>(provider => provider.GetRequiredKeyedService<IRenderer>(key));

        RegisterGroup(services, group);
        return new RendererServicesBuilder(services, name, key, options);
    }

    /// <summary>Begins a renderer declaration using a named setup method, like <c>AddWorld(setup)</c>.</summary>
    public static IRendererServicesBuilder AddRenderer(
        this IServiceCollection services,
        object key,
        Action<RendererOptions> configure,
        int order = 1000,
        string? group = "Default")
    {
        ArgumentNullException.ThrowIfNull(configure);

        var builder = services.AddRenderer(key, order, group);
        configure(builder.Options);
        return builder;
    }

    /// <summary>Registers a DI-created configuration module for the renderer.</summary>
    public static IRendererServicesBuilder AddModule<TModule>(this IRendererServicesBuilder builder)
        where TModule : class, IRendererConfiguration
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.AddKeyedTransient<IRendererConfiguration, TModule>(builder.Key);
        return builder;
    }

    /// <summary>Adds a screen-space or world-space layer to a named renderer.</summary>
    public static IRendererServicesBuilder AddLayer(
        this IRendererServicesBuilder builder,
        RenderLayerId layer,
        int order = 0)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Options.AddLayer(layer, new RenderLayerOptions { Order = order });
        return builder;
    }

    private static void RegisterGroup(IServiceCollection services, string? group)
    {
        if (group is not null)
        {
            services.TryAddKeyedSingleton(
                serviceKey: group,
                static (provider, key) => new RendererGroup(
                    (string)key!,
                    provider.GetServices<IRenderer>(),
                    provider.GetRequiredService<GraphicsManager>())
            );
        }
    }

    /// <summary>
    /// Adds a draw-loop driver for one group. Renderers with a null group remain manually flushed.
    /// At the default draw order of zero, register it after the components that submit to this group.
    /// Use a higher draw order for producers that draw later.
    /// </summary>
    public static IServiceCollection AddRendererGameComponent(
        this IServiceCollection services,
        string group = "Default",
        int drawOrder = 0)
    {
        ArgumentException.ThrowIfNullOrEmpty(group);

        services.AddSingleton<IGameComponent>(provider => new RendererGameComponent(
            provider.GetRequiredService<Toolkit.Game>(),
            provider.GetRequiredKeyedService<RendererGroup>(group),
            drawOrder));

        return services;
    }
}

/// <summary>Fluent declaration of a named renderer and its layers.</summary>
public interface IRendererServicesBuilder
{
    IServiceCollection Services { get; }
    string Name { get; }
    object Key { get; }
    RendererOptions Options { get; }
}

/// <summary>Configures renderer options from explicitly registered DI services.</summary>
public interface IRendererConfiguration
{
    void Configure(RendererOptions options);
}
