using System.Reflection;

namespace PurrplingCore.Toolkit.Hosting;

public interface IHostEnvironment
{
    string ApplicationName { get; set; }
    string EnvironmentName { get; }
    string HostAssemblyPath { get; }
    string HostDirectory { get; }
    string BaseDirectory { get; }
    OperatingSystem OperatingSystem { get; }
    PlatformType PlatformType { get; }
}

internal sealed class HostEnvironment : IHostEnvironment
{
    private static readonly Assembly _hostAssembly = Assembly.GetEntryAssembly() 
        ?? Assembly.GetExecutingAssembly();

    public HostEnvironment()
    {
        ApplicationName = _hostAssembly.GetName().Name ?? "Application";
        EnvironmentName = _hostAssembly.IsDebugBuild()
            ? Environments.Development
            : Environments.Production;
    }

    public string ApplicationName { get; set; }

    public string EnvironmentName { get; set; }

    public string HostAssemblyPath => _hostAssembly.Location;

    public string BaseDirectory => AppContext.BaseDirectory;

    public OperatingSystem OperatingSystem => Environment.OSVersion;

    public PlatformType PlatformType
    {
        get
        {
            if (OperatingSystem.IsTvOS()) return PlatformType.Tv;
            if (OperatingSystem.IsBrowser()) return PlatformType.Browser;
            if (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS()) return PlatformType.Mobile;
            if (OperatingSystem.IsMacOS() || OperatingSystem.IsLinux() || OperatingSystem.IsWindows()) return PlatformType.Desktop;

            return PlatformType.Unknown;
        }
    }

    public string HostDirectory => Path.GetDirectoryName(_hostAssembly.Location) ?? string.Empty;

    public static bool IsDebugBuild => _hostAssembly.IsDebugBuild();
    public static bool IsReleaseBuild => !_hostAssembly.IsDebugBuild();
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
