namespace KittyCat.Core;

public interface IGameStateHandler
{
    GameState HandledState { get; }

    void OnEnterState();
    void OnExitState();
}