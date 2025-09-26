namespace PurrplingCore.Toolkit.DI;

public interface IStartupService
{
    int Order { get; }
    void OnStartup();
}

public interface ICleanupService
{
    int Order { get; }
    void OnCleanup();
}

public interface IGameService
{
    void Initialize();
    void LoadContent();
    void UnloadContent();
}
