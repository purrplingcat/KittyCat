using KittyCat.Core;

namespace KittyCat.Messages;

public record struct GameStateChangedMessage(GameState PreviousState, GameState NewState);
