using System.Reflection;

namespace PurrplingCore.Toolkit.Hosting;

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
