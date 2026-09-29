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
    int Run(CancellationToken token);
}

public abstract class AsyncHostedApplication : IHostedApplication
{
    /// <summary>
    /// Runs the hosted application asynchronously.
    /// </summary>
    /// <returns>A task representing the asynchronous operation, with the application exit code as the result.</returns>
    public abstract Task<int> RunAsync(CancellationToken cancellationToken);

    /// <inheritdoc />
    public int Run(CancellationToken token) => RunAsync(token).GetAwaiter().GetResult();
}
