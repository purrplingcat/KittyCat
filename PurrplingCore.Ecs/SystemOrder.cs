namespace PurrplingCore.Ecs;

/// <summary>
/// Selects a system's position relative to the other ordering categories in its group.
/// </summary>
public enum SystemOrder
{
    /// <summary>
    /// Orders the system using its declared before and after dependencies.
    /// </summary>
    Default,

    /// <summary>
    /// Places the system before systems in the default and last categories.
    /// </summary>
    First,

    /// <summary>
    /// Places the system after systems in the default and first categories.
    /// </summary>
    Last,
}
