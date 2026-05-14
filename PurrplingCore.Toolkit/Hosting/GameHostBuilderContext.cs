using Microsoft.Extensions.Logging;
using System.Reflection;

namespace PurrplingCore.Toolkit.Hosting;

public class GameHostBuilderContext : IDisposable
{
    private readonly Dictionary<object, ILogger> _loggers = [];
    private ILogger? _logger;

    public string ApplicationName { get; }
    public string HostDirectory { get; }
    public string BaseDirectory { get; } = AppContext.BaseDirectory;

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
        ApplicationName = executingAssembly.GetName().Name ?? string.Empty;
        HostDirectory = Path.GetDirectoryName(executingAssembly.Location) ?? AppContext.BaseDirectory;
        GameVersion = gameVersion;
    }
}

public interface IHostEnvironment
{
    GameVersion GameVersion { get; }
    string ApplicationName { get; }
    string EnvironmentName { get; }
    string HostAssemblyPath { get; }
    string HostDirectory { get; }
    string BaseDirectory { get; }
    OperatingSystem OperatingSystem { get; }
    PlatformType PlatformType { get; }
}

internal sealed class HostEnvironment : IHostEnvironment
{
    private readonly Assembly _hostAssembly = Assembly.GetEntryAssembly() 
        ?? Assembly.GetExecutingAssembly();

    public HostEnvironment()
    {
        EnvironmentName = _hostAssembly.IsDebugBuild()
            ? Environments.Development
            : Environments.Production;
    }

    public GameVersion GameVersion { get; set; } = GameVersion.Empty;

    public string ApplicationName => GameVersion.Name;

    public string EnvironmentName { get; set; }

    public string HostAssemblyPath => _hostAssembly.Location;

    public string BaseDirectory => AppContext.BaseDirectory;

    public OperatingSystem OperatingSystem => Environment.OSVersion;

    public PlatformType PlatformType => Game.PlatformType;

    public string HostDirectory => Path.GetDirectoryName(_hostAssembly.Location) ?? string.Empty;
}

public static class HostEnvironmentExtensions
{
    public static bool IsDevelopment(this IHostEnvironment env)
    {
        return env.EnvironmentName == Environments.Development;
    }

    public static bool IsProduction(this IHostEnvironment env)
    {
        return env.EnvironmentName == Environments.Production;
    }
}

public static class Environments
{
    public static readonly string Development = "Development";
    public static readonly string Production = "Production";
}
