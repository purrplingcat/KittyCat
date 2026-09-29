namespace PurrplingCore.Toolkit.Hosting;

/// <summary>
/// Represents an application hosted within the ApplicationHost that can be run and controlled.
/// </summary>
public interface IHostedApplication
{
    /// <summary>
    /// Runs the hosted application.
    /// </summary>
    /// <returns>The application exit code (0 for success, non-zero for errors).</returns>
    int Run();
}

/// <summary>
/// Represents a stoppable application that can gracefully exit.
/// </summary>
/// <remarks>
/// Implement this interface in addition to IHostedApplication if your application
/// requires graceful shutdown handling (e.g., a game loop that needs to clean up resources).
/// </remarks>
public interface IStoppableApplication
{
    /// <summary>
    /// Requests the application to stop gracefully.
    /// </summary>
    void Exit();
}