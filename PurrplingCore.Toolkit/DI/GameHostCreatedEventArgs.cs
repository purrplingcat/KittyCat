namespace PurrplingCore.Toolkit.DI;

public class GameHostCreatedEventArgs(GameHost host) : EventArgs
{
    public GameHost GameHost { get; } = host;
}
