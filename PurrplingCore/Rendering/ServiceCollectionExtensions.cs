using Microsoft.Extensions.DependencyInjection;
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

    /// <summary>
    /// Registers a keyed renderer without scheduling it in the game's draw loop.
    /// </summary>
    /// <param name="services">The service collection to add the renderer to.</param>
    /// <param name="key">The unique key identifying the renderer.</param>
    /// <returns>A builder for further configuring the renderer.</returns>
    /// <remarks>
    /// Call <see cref="AddDrawComponent(IRendererServicesBuilder, int)"/> to add a game-component driver;
    /// otherwise initialize and flush the renderer from your own controller.
    /// </remarks>
    public static IRendererServicesBuilder AddRenderer(
        this IServiceCollection services,
        object key)
    {
        ArgumentNullException.ThrowIfNull(key);

        var options = new RendererOptions();
        var name = key as string ?? key.ToString() ?? key.GetType().Name;
        services.AddKeyedSingleton<IRenderer>(key, (provider, _) =>
        {
            var modules = provider.GetKeyedServices<IRendererConfiguration>(key);
            foreach (var module in modules)
            {
                module.Configure(options);
            }

            return new Renderer2D(name, options);
        });

        services.AddSingleton<IRenderer>(provider => provider.GetRequiredKeyedService<IRenderer>(key));
        return new RendererServicesBuilder(services, name, key, options);
    }

    /// <summary>
    /// Registers a keyed renderer without scheduling it in the game's draw loop and configures its options.
    /// Call <see cref="AddDrawComponent(IRendererServicesBuilder, int)"/> to add a game-component driver;
    /// otherwise initialize and flush the renderer from your own controller.
    /// </summary>
    /// <param name="services">The service collection to add the renderer to.</param>
    /// <param name="key">The unique key identifying the renderer.</param>
    /// <param name="configure">An action to configure the renderer's options.</param>
    /// <returns>A builder for further configuring the renderer.</returns>
    public static IRendererServicesBuilder AddRenderer(
        this IServiceCollection services,
        object key,
        Action<RendererOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var builder = services.AddRenderer(key);
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

    /// <summary>
    /// Adds a draw-loop component that initializes and flushes this renderer.
    /// </summary>
    /// <param name="builder">The renderer services builder.</param>
    /// <param name="drawOrder">The draw order of the added component. Defaults to <see cref="DrawStage.Default"/>.</param>
    /// <returns>The same renderer services builder for further configuration.</returns>
    /// <remarks>
    /// Each call adds a separate component. Calling this more than once registers multiple components;
    /// a different draw order does not replace an earlier one.
    /// </remarks>
    public static IRendererServicesBuilder AddDrawComponent(
        this IRendererServicesBuilder builder,
        int drawOrder = DrawStage.Default)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.AddRendererGameComponent(builder.Key, drawOrder);
        return builder;
    }

    /// <summary>
    /// Adds a draw-loop driver for one keyed renderer.
    /// The renderer is initialized and flushed by this component at the supplied draw order.
    /// </summary>
    public static IServiceCollection AddRendererGameComponent(
        this IServiceCollection services,
        object key,
        int drawOrder = DrawStage.Default)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(key);

        services.AddSingleton<IGameComponent>(provider => new RendererGameComponent(
            provider.GetRequiredService<Toolkit.Game>(),
            provider.GetRequiredKeyedService<IRenderer>(key),
            provider.GetRequiredService<GraphicsManager>(),
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
