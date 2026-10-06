namespace PurrplingCore.Ecs.WorldBuilding;

/// <summary>
/// Applies configuration to a world blueprint.
/// </summary>
public interface IWorldModule
{
    /// <summary>
    /// Gets the order in which this module is applied.
    /// </summary>
    int Order { get; }

    /// <summary>
    /// Adds this module's configuration to a world blueprint.
    /// </summary>
    void Setup(IWorldBlueprint world);
}