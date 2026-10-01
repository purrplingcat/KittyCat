namespace PurrplingCore.Toolkit.States;

/// <summary>Handles entry and exit of a particular state.</summary>
public interface IStateHandler<TState>
{
    TState HandledState { get; }
    void OnEnterState();
    void OnExitState();
}

/// <summary>Coordinates state transitions and invokes the registered state handlers.</summary>
public class StateMachine<TState> where TState : notnull
{
    private readonly Dictionary<TState, List<IStateHandler<TState>>> _handlers = [];

    public TState CurrentState { get; private set; }
    public event Action<TState, TState>? StateChanged;

    public StateMachine(TState initialState, IEnumerable<IStateHandler<TState>> handlers)
    {
        CurrentState = initialState;
        foreach (var handler in handlers)
        {
            if (!_handlers.TryGetValue(handler.HandledState, out var group))
            {
                group = [];
                _handlers.Add(handler.HandledState, group);
            }

            group.Add(handler);
        }
    }

    /// <summary>Leaves the old state, enters the new state, then notifies subscribers.</summary>
    public void ChangeState(TState newState)
    {
        if (EqualityComparer<TState>.Default.Equals(CurrentState, newState)) return;

        var previousState = CurrentState;
        if (_handlers.TryGetValue(previousState, out var oldHandlers))
        {
            foreach (var handler in oldHandlers) handler.OnExitState();
        }

        CurrentState = newState;
        if (_handlers.TryGetValue(newState, out var newHandlers))
        {
            foreach (var handler in newHandlers) handler.OnEnterState();
        }

        OnStateChanged(previousState, newState);
    }

    protected virtual void OnStateChanged(TState previousState, TState newState)
    {
        StateChanged?.Invoke(previousState, newState);
    }
}