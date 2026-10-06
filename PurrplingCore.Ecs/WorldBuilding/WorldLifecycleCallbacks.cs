namespace PurrplingCore.Ecs.WorldBuilding;

/// <summary>
/// Stores callbacks for the runtime lifecycle of a world.
/// </summary>
public sealed class WorldLifecycleCallbacks
{
    /// <summary>
    /// Gets callbacks invoked after world composition completes.
    /// </summary>
    public List<Action<WorldCompositionContext>> Composed { get; } = [];

    /// <summary>
    /// Gets callbacks invoked when the world is initialized.
    /// </summary>
    public List<Action<WorldInitializedContext>> Initialized { get; } = [];

    /// <summary>
    /// Gets callbacks invoked after the world is updated.
    /// </summary>
    public List<Action<WorldUpdatedContext>> Updated { get; } = [];

    /// <summary>
    /// Gets callbacks invoked after the world is drawn.
    /// </summary>
    public List<Action<WorldDrawnContext>> Drawn { get; } = [];

    /// <summary>
    /// Gets callbacks invoked when the world is disposed.
    /// </summary>
    public List<Action<WorldDisposedContext>> Disposed { get; } = [];
}