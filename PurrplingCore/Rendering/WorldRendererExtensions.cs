using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PurrplingCore.Ecs.DI;

namespace PurrplingCore.Rendering;

/// <summary>
/// Attaches renderer configuration and lifecycle integration to a world.
/// </summary>
public static class WorldRendererExtensions
{
    /// <summary>
    /// Adds a world-owned renderer configured through a DI-created module.
    /// </summary>
    /// <remarks>
    /// The world initializes and flushes this renderer. Global renderers and their draw components
    /// remain independent. World draw systems receive its unkeyed RenderContext through DI.
    /// </remarks>
    public static IWorldServicesBuilder AddRenderer<TConfiguration>(this IWorldServicesBuilder builder)
        where TConfiguration : class, IRendererConfiguration
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddFeature<WorldRenderingFeature>();
        builder.Services.TryAddEnumerable(
            ServiceDescriptor.KeyedTransient<IRendererConfiguration, TConfiguration>(
                WorldRenderingFeature.GetConfigurationKey(builder.Signature)));
        return builder;
    }
}