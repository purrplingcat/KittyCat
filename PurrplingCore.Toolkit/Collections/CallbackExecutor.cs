namespace PurrplingCore.Toolkit.Collections;

/// <summary>
/// Owns callback execution and exposes a separate registration-only collection.
/// </summary>
public sealed class CallbackExecutor<TContext>
{
    /// <summary>
    /// Creates an empty callback executor.
    /// </summary>
    public CallbackExecutor() : this([])
    {
    }

    private CallbackExecutor(IEnumerable<Action<TContext>> callbacks)
    {
        Callbacks = new Callbacks<TContext>(callbacks);
    }

    /// <summary>
    /// Gets the collection exposed to callers that may register or remove callbacks.
    /// </summary>
    public Callbacks<TContext> Callbacks { get; }

    /// <summary>
    /// Gets the number of registered callbacks.
    /// </summary>
    public int Count => Callbacks.Items.Count;

    /// <summary>
    /// Creates an executor whose registrations are independent of subsequent changes.
    /// </summary>
    public CallbackExecutor<TContext> Snapshot() => new(Callbacks.Items);

    /// <summary>
    /// Executes callbacks in registration order without allocating.
    /// Registration changes take effect on the next invocation.
    /// </summary>
    public void Execute(TContext context)
    {
        var callbacks = Callbacks.Items;
        for (var index = 0; index < callbacks.Count; index++)
            callbacks[index](context);
    }
}