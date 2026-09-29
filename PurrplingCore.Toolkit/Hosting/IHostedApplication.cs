namespace PurrplingCore.Toolkit.Hosting;

public interface IHostedApplication
{
    int Run();
}

public interface IStoppableApplication
{
    void Exit();
}