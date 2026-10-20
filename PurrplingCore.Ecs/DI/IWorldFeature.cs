namespace PurrplingCore.Ecs.DI;

/// <summary>
/// Registers a composable set of services and modules for a world marker.
/// </summary>
/// <remarks>
/// Features are applied immediately during registration. Their services and scoped modules are
/// created later through dependency injection, separately for each world instance.
/// </remarks>
public interface IWorldFeature
{
    /// <summary>
    /// Adds this feature's registrations to the configured world.
    /// </summary>
    void ConfigureServices(IWorldServicesBuilder builder);
}