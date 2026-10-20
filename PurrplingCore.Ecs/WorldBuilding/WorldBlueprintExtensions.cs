namespace PurrplingCore.Ecs.WorldBuilding;

/// <summary>
/// Adds lifecycle callbacks to a world blueprint.
/// </summary>
public static class WorldBlueprintExtensions
{
    /// <summary>
    /// Registers a callback invoked after the world's systems have been composed.
    /// </summary>
    public static IWorldBlueprint OnComposed(this IWorldBlueprint blueprint, Action<WorldCompositionContext> callback)
    {
        ArgumentNullException.ThrowIfNull(blueprint);
        ArgumentNullException.ThrowIfNull(callback);
        blueprint.Lifecycle.Composed.Add(callback);
        return blueprint;
    }

    /// <summary>
    /// Registers a callback invoked when the world is initialized.
    /// </summary>
    public static IWorldBlueprint OnInitialized(this IWorldBlueprint blueprint, Action<WorldInitializedContext> callback)
    {
        ArgumentNullException.ThrowIfNull(blueprint);
        ArgumentNullException.ThrowIfNull(callback);
        blueprint.Lifecycle.Initialized.Add(callback);
        return blueprint;
    }

    /// <summary>
    /// Registers a callback invoked after the world is updated.
    /// </summary>
    public static IWorldBlueprint OnUpdated(this IWorldBlueprint blueprint, Action<WorldUpdatedContext> callback)
    {
        ArgumentNullException.ThrowIfNull(blueprint);
        ArgumentNullException.ThrowIfNull(callback);
        blueprint.Lifecycle.Updated.Add(callback);
        return blueprint;
    }

    /// <summary>
    /// Registers a callback invoked before the world's draw systems run.
    /// </summary>
    public static IWorldBlueprint OnDrawing(this IWorldBlueprint blueprint, Action<WorldDrawingContext> callback)
    {
        ArgumentNullException.ThrowIfNull(blueprint);
        ArgumentNullException.ThrowIfNull(callback);
        blueprint.Lifecycle.Drawing.Add(callback);
        return blueprint;
    }

    /// <summary>
    /// Registers a callback invoked after the world is drawn.
    /// </summary>
    public static IWorldBlueprint OnDrawn(this IWorldBlueprint blueprint, Action<WorldDrawnContext> callback)
    {
        ArgumentNullException.ThrowIfNull(blueprint);
        ArgumentNullException.ThrowIfNull(callback);
        blueprint.Lifecycle.Drawn.Add(callback);
        return blueprint;
    }

    /// <summary>
    /// Registers a callback invoked when the world is disposed.
    /// </summary>
    public static IWorldBlueprint OnDisposed(this IWorldBlueprint blueprint, Action<WorldDisposedContext> callback)
    {
        ArgumentNullException.ThrowIfNull(blueprint);
        ArgumentNullException.ThrowIfNull(callback);
        blueprint.Lifecycle.Disposed.Add(callback);
        return blueprint;
    }
}