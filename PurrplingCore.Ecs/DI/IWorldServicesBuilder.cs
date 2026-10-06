using Microsoft.Extensions.DependencyInjection;

namespace PurrplingCore.Ecs.DI;

/// <summary>
/// Exposes services and the world identity used by world registration extensions.
/// </summary>
public interface IWorldServicesBuilder
{
    /// <summary>
    /// Gets the service collection being configured.
    /// </summary>
    IServiceCollection Services { get; }

    /// <summary>
    /// Gets the signature of the world being configured.
    /// </summary>
    WorldSignature Signature { get; }

    /// <summary>
    /// Gets the service registration key for this world.
    /// </summary>
    object Key { get; }
}
