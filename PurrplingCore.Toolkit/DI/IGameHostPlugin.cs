namespace PurrplingCore.Toolkit.DI;

public interface IGameHostPlugin 
{
    string Name { get; }
    void Setup(IGameHostBuilder builder, GameHostBuilderContext context);
}
