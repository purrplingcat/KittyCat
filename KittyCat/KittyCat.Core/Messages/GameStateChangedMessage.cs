namespace KittyCat.Core.Messages;

public record struct GameStateChangedMessage(GameState PreviousState, GameState NewState);
