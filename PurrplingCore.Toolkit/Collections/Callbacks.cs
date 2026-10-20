namespace PurrplingCore.Toolkit.Collections;

/// <summary>
/// Adds and removes unique callbacks without exposing their execution.
/// </summary>
public sealed class Callbacks<TContext>
{
    private List<Action<TContext>> _callbacks;

    internal Callbacks(IEnumerable<Action<TContext>> callbacks)
    {
        _callbacks = [.. callbacks];
    }

    internal List<Action<TContext>> Items => _callbacks;

    /// <summary>
    /// Appends a callback unless an equal delegate is already registered.
    /// </summary>
    public bool Add(Action<TContext> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        if (_callbacks.Contains(callback)) return false;

        _callbacks = new List<Action<TContext>>(_callbacks) { callback };
        return true;
    }

    /// <summary>
    /// Removes an equal delegate if it is registered.
    /// </summary>
    public bool Remove(Action<TContext> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        if (!_callbacks.Contains(callback)) return false;

        _callbacks = new List<Action<TContext>>(_callbacks);
        return _callbacks.Remove(callback);
    }
}