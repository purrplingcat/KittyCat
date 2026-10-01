using KittyCat.Core;
using KittyCat.Messages;
using PurrplingCore.Toolkit.DI;
using PurrplingCore.Toolkit.Messaging;
using PurrplingCore.Toolkit.States;
using System.Collections.Generic;

namespace KittyCat.Services;

/// <summary>Runs state entry and exit handlers and publishes game state transitions.</summary>
[Singleton]
public class GameStateMachine(IMessageBus messageBus, IEnumerable<IGameStateHandler> handlers)
    : StateMachine<GameState>(GameState.None, handlers)
{
    protected override void OnStateChanged(GameState previousState, GameState newState)
    {
        base.OnStateChanged(previousState, newState);
        messageBus.Publish(new GameStateChangedMessage(previousState, newState));
    }
}
