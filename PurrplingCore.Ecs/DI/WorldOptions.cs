using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace PurrplingCore.Ecs.DI;

/// <summary>
/// Provides the options selected for the current world's marker.
/// </summary>
public interface IWorldOptions<out TOptions> where TOptions : class
{
    /// <summary>
    /// Gets the cached options instance for this world scope.
    /// </summary>
    TOptions Value { get; }
}

/// <summary>
/// Maps world signatures to stable named-options identities.
/// </summary>
public static class WorldOptions
{
    /// <summary>
    /// Gets the named-options key for a world marker type.
    /// </summary>
    public static string GetName(WorldSignature signature)
    {
        var markerType = signature.MarkerType;
        if (markerType is null)
        {
            throw new ArgumentException("World signature must identify a marker type.", nameof(signature));
        }

        return markerType.AssemblyQualifiedName ?? markerType.FullName ?? markerType.Name;
    }

    /// <summary>
    /// Configures options for this world marker after shared defaults, overriding only the properties it sets.
    /// </summary>
    public static IWorldServicesBuilder ConfigureOptions<TOptions>(
        this IWorldServicesBuilder builder,
        Action<TOptions> configure)
        where TOptions : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        builder.Services.PostConfigure(GetName(builder.Signature), configure);
        return builder;
    }
}

internal sealed class WorldOptionsAdapter<TOptions> : IWorldOptions<TOptions> where TOptions : class
{
    public WorldOptionsAdapter(IOptionsSnapshot<TOptions> options, IWorldContext context)
    {
        Value = options.Get(WorldOptions.GetName(context.Signature));
    }

    public TOptions Value { get; }
}

internal static class WorldOptionsServiceCollectionExtensions
{
    public static IServiceCollection AddWorldOptions(this IServiceCollection services)
    {
        services.AddOptions();
        services.TryAddScoped(typeof(IWorldOptions<>), typeof(WorldOptionsAdapter<>));
        return services;
    }
}