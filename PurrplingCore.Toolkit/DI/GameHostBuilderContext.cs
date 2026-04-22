using Microsoft.Extensions.Logging;
using System.Reflection;

namespace PurrplingCore.Toolkit.DI;

public class GameHostBuilderContext : IDisposable
{
    private ILogger? _logger;
    public ILoggerFactory LoggerFactory { get; }
    public string Directory { get; }

    public ILogger Logger => _logger ??= LoggerFactory.CreateLogger("GameHostBuilder");

    public ILogger CreateLogger(string name) => LoggerFactory.CreateLogger(name);

    public ILogger CreateLogger<TLogger>() => LoggerFactory.CreateLogger<TLogger>();

    public Assembly HostAssembly { get; }

    public void Dispose()
    {
        if (_logger is IDisposable disposable)
        {
            disposable.Dispose();
        }

        LoggerFactory.Dispose();
    }

    internal GameHostBuilderContext(ILoggerFactory loggerFactory, Assembly executingAssembly)
    {
        LoggerFactory = loggerFactory;
        HostAssembly = executingAssembly;
        Directory = Path.GetDirectoryName(executingAssembly.Location) ?? string.Empty;
    }

    internal GameHostBuilderContext(ILogger logger, ILoggerFactory loggerFactory, Assembly executingAssembly) : this(loggerFactory, executingAssembly)
    {
        _logger = logger;
    }
}
