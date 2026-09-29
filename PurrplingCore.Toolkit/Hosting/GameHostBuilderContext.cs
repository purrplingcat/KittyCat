using System.Reflection;

namespace PurrplingCore.Toolkit.Hosting;

/// <summary>
/// Provides information about the host environment including application name, environment type,
/// and platform information.
/// </summary>
public interface IHostEnvironment
{
    /// <summary>
    /// Gets or sets the name of the application.
    /// </summary>
    string ApplicationName { get; set; }
    
    /// <summary>
    /// Gets the environment name (e.g., Development, Production).
    /// </summary>
    string EnvironmentName { get; }

    /// <summary>
    /// Gets the ID of the current managed thread.
    /// </summary>
    public int ThreadId { get; }
    
    /// <summary>
    /// Gets the full path to the host assembly.
    /// </summary>
    string HostAssemblyPath { get; }
    
    /// <summary>
    /// Gets the directory containing the host assembly.
    /// </summary>
    string HostDirectory { get; }
    
    /// <summary>
    /// Gets the base directory of the application.
    /// </summary>
    string BaseDirectory { get; }
    
    /// <summary>
    /// Gets the operating system information.
    /// </summary>
    OperatingSystem OperatingSystem { get; }
    
    /// <summary>
    /// Gets the platform type (Desktop, Mobile, Browser, TV, etc.).
    /// </summary>
    PlatformType PlatformType { get; }
}

/// <summary>
/// Default implementation of IHostEnvironment that automatically detects environment and platform information.
/// </summary>
internal sealed class HostEnvironment : IHostEnvironment
{
    private static readonly Assembly _hostAssembly = Assembly.GetEntryAssembly() 
        ?? Assembly.GetExecutingAssembly();

    /// <summary>
    /// Initializes a new instance of HostEnvironment with default values.
    /// </summary>
    /// <remarks>
    /// Environment name is determined from the assembly compilation mode:
    /// Debug builds default to Development, Release builds default to Production.
    /// </remarks>
    public HostEnvironment()
    {
        ApplicationName = _hostAssembly.GetName().Name ?? "Application";
        EnvironmentName = _hostAssembly.IsDebugBuild()
            ? Environments.Development
            : Environments.Production;
    }

    public string ApplicationName { get; set; }

    public int ThreadId => Environment.CurrentManagedThreadId;

    public string EnvironmentName { get; set; }

    public string HostAssemblyPath => _hostAssembly.Location;

    public string BaseDirectory => AppContext.BaseDirectory;

    public OperatingSystem OperatingSystem => Environment.OSVersion;

    /// <summary>
    /// Gets the platform type based on the operating system.
    /// </summary>
    /// <remarks>
    /// Classifies the operating system into platform categories: Desktop, Mobile, TV, Browser, or Unknown.
    /// </remarks>
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

    /// <summary>
    /// Gets a value indicating whether the host is running in Debug configuration.
    /// </summary>
    public static bool IsDebugBuild => _hostAssembly.IsDebugBuild();
    
    /// <summary>
    /// Gets a value indicating whether the host is running in Release configuration.
    /// </summary>
    public static bool IsReleaseBuild => !_hostAssembly.IsDebugBuild();
}

/// <summary>
/// Extension methods for checking host environment properties.
/// </summary>
public static class HostEnvironmentExtensions
{
    /// <summary>
    /// Determines if the host is running in Development environment.
    /// </summary>
    public static bool IsDevelopment(this IHostEnvironment env)
    {
        return env.EnvironmentName == Environments.Development;
    }

    /// <summary>
    /// Determines if the host is running in Production environment.
    /// </summary>
    public static bool IsProduction(this IHostEnvironment env)
    {
        return env.EnvironmentName == Environments.Production;
    }
}

/// <summary>
/// Constants for common environment names.
/// </summary>
public static class Environments
{
    /// <summary>
    /// The Development environment name.
    /// </summary>
    public static readonly string Development = "Development";
    
    /// <summary>
    /// The Production environment name.
    /// </summary>
    public static readonly string Production = "Production";
}
