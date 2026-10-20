using PurrplingCore.Toolkit.Collections;

namespace PurrplingCore.Ecs.WorldBuilding;

/// <summary>
/// Stores unique callbacks for the runtime lifecycle of a world in registration order.
/// </summary>
public sealed class WorldLifecycleCallbacks
{
    internal CallbackExecutor<WorldCompositionContext> ComposedExecutor { get; } = new();
    internal CallbackExecutor<WorldInitializedContext> InitializedExecutor { get; } = new();
    internal CallbackExecutor<WorldUpdatedContext> UpdatedExecutor { get; } = new();
    internal CallbackExecutor<WorldDrawingContext> DrawingExecutor { get; } = new();
    internal CallbackExecutor<WorldDrawnContext> DrawnExecutor { get; } = new();
    internal CallbackExecutor<WorldDisposedContext> DisposedExecutor { get; } = new();

    /// <summary>
    /// Gets callbacks invoked after world composition completes.
    /// </summary>
    public Callbacks<WorldCompositionContext> Composed => ComposedExecutor.Callbacks;

    /// <summary>
    /// Gets callbacks invoked when the world is initialized.
    /// </summary>
    public Callbacks<WorldInitializedContext> Initialized => InitializedExecutor.Callbacks;

    /// <summary>
    /// Gets callbacks invoked after the world is updated.
    /// </summary>
    public Callbacks<WorldUpdatedContext> Updated => UpdatedExecutor.Callbacks;

    /// <summary>
    /// Gets callbacks invoked before the world's draw systems run.
    /// </summary>
    public Callbacks<WorldDrawingContext> Drawing => DrawingExecutor.Callbacks;

    /// <summary>
    /// Gets callbacks invoked after the world is drawn.
    /// </summary>
    public Callbacks<WorldDrawnContext> Drawn => DrawnExecutor.Callbacks;

    /// <summary>
    /// Gets callbacks invoked when the world is disposed.
    /// </summary>
    public Callbacks<WorldDisposedContext> Disposed => DisposedExecutor.Callbacks;
}