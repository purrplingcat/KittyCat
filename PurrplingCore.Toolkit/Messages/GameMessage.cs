using Microsoft.Xna.Framework;
using PurrplingCore.Toolkit.Messaging;

namespace PurrplingCore.Toolkit.Messages;

public enum GameMessages
{
    None,
    Initialized,
    Lanuched,
    Exit,
}

public readonly struct GameMessage
{
    public readonly Game Game;
    public readonly GameMessages Type;

    public bool IsEmpty => Game is null;

    internal GameMessage(Game game, GameMessages type)
    {
        Game = game;
        Type = type;
    }
}
