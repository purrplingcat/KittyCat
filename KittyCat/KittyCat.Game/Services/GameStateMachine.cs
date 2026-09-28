using KittyCat.Core;
using KittyCat.Messages;
using PurrplingCore.Toolkit.DI;
using PurrplingCore.Toolkit.Messaging;
using System;
using System.Collections.Generic;

namespace KittyCat.Services;

[Singleton]
public class GameStateMachine
{
    private readonly IMessageBus _messageBus;
    private readonly Dictionary<GameState, List<IGameStateHandler>> _handlerMap = [];

    public GameState CurrentState { get; private set; } = GameState.None;
    public event Action<GameState, GameState>? StateChanged;

    public GameStateMachine(IMessageBus messageBus, IEnumerable<IGameStateHandler> handlers)
    {
        _messageBus = messageBus;
        foreach (var handler in handlers)
        {
            if (!_handlerMap.TryGetValue(handler.HandledState, out List<IGameStateHandler>? handlerGroup))
            {
                handlerGroup = [];
                _handlerMap.Add(handler.HandledState, handlerGroup);
            }

            handlerGroup.Add(handler);
        }   
    }

    public void ChangeState(GameState newState)
    {
        if (CurrentState == newState) return;

        var previousState = CurrentState;

        if (_handlerMap.TryGetValue(previousState, out var oldHandlers))
        {
            oldHandlers.ForEach(static h => h.OnExitState());
        }
        CurrentState = newState;
        if (_handlerMap.TryGetValue(newState, out var newHandlers))
        {
            newHandlers.ForEach(static h => h.OnEnterState());
        }
        
        StateChanged?.Invoke(previousState, newState);
        _messageBus.Publish(new GameStateChangedMessage(previousState, newState));
    }
}
