using Friflo.Engine.ECS.Systems;

namespace PurrplingCore.Ecs.Systems.Builder;

/// <summary>
/// Creates system groups by their requested type.
/// </summary>
public interface ISystemGroupFactory
{
    /// <summary>
    /// Creates a system group of the requested type.
    /// </summary>
    TGroup CreateGroup<TGroup>() where TGroup : SystemGroup;
}

/// <summary>
/// Creates system groups of a fixed type.
/// </summary>
public interface ISystemGroupFactory<out TGroup> where TGroup : SystemGroup
{
    /// <summary>
    /// Creates a system group.
    /// </summary>
    TGroup CreateGroup();
}
