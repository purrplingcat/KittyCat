using Microsoft.Extensions.Logging;
using System.Reflection;

namespace PurrplingCore.Toolkit.DI;

public class GameHostBuilderContext : IDisposable
{
    private readonly Dictionary<object, ILogger> _loggers = [];
    private ILogger? _logger;

    public string Directory { get; }

    internal ILoggerFactory LoggerFactory { get; }

    internal ILogger Logger => _logger ??= CreateLogger("game");

    public ILogger CreateLogger(string name) => LoggerFactory.CreateLogger(name);

    public ILogger CreateLogger<TLogger>() => LoggerFactory.CreateLogger<TLogger>();

    public ILogger CreateLogger(Type loggerType) => LoggerFactory.CreateLogger(loggerType);

    public ILogger GetLogger(string name)
    {
        if (!_loggers.TryGetValue(name, out var logger))
        {
            logger = CreateLogger(name);
            _loggers.Add(name, logger);
        }
        
        return logger;
    }

    public ILogger GetLogger<TLogger>()
    {
        var type = typeof(TLogger);
        if (!_loggers.TryGetValue(type, out var logger))
        {
            logger = CreateLogger<TLogger>();
            _loggers.Add(type, logger);
        }

        return logger;
    }

    public Assembly HostAssembly { get; }
    public OperatingSystem OperatingSystem { get; }
    public PlatformType PlatformType { get; }
    public GameVersion GameVersion { get; }

    public void Dispose()
    {
        _loggers.Clear();
    }

    internal GameHostBuilderContext(ILoggerFactory loggerFactory, Assembly executingAssembly, GameVersion gameVersion)
    {
        LoggerFactory = loggerFactory;
        HostAssembly = executingAssembly;
        OperatingSystem = Environment.OSVersion;
        PlatformType = Game.PlatformType;
        Directory = Path.GetDirectoryName(executingAssembly.Location) ?? string.Empty;
        GameVersion = gameVersion;
    }
}
