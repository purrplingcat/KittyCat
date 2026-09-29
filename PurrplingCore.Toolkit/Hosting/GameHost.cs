using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PurrplingCore.Toolkit.DI;
using PurrplingCore.Toolkit.Hosting.Services;

namespace PurrplingCore.Toolkit.Hosting;

public sealed class GameHost : ApplicationHost
{
    private GameApplicationRunner? _gameRunner;

    /// <summary>
    /// Initializes a new GameHost instance with game-specific services and configuration.
    /// </summary>
    /// <remarks>
    /// This constructor is called internally by GameHostBuilder.Build() and should not be used directly.
    /// The GameHost automatically registers GameApplicationRunner and GameApplicationLifetime services.
    /// </remarks>
    internal GameHost(
        IServiceProvider hostProvider, 
        IServiceCollection gameServices, 
        IHostEnvironment environment,
        GameVersion version,
        IDictionary<object, object>? properties = null
    ) : base(hostProvider, gameServices, properties, "GameHost")
    {
        Logger.LogGameInformation(environment, version);
        Logger.LogDebug("Environment: {EnvironmentName}", environment.EnvironmentName);

        gameServices
            .AddSingleton<GameApplicationLifetime>()
            .AddSingleton<GameApplicationRunner>()
            .AddAlias<IGameApplicationLifetime, GameApplicationLifetime>()
            .AddAlias<IHostedApplication, GameApplicationRunner>();
    }

    /// <summary>
    /// Gets a value indicating whether the game is currently running.
    /// </summary>
    public bool IsRunning => _gameRunner?.IsRunning ?? false;

    /// <summary>
    /// Creates a new GameHostBuilder with default game configuration.
    /// </summary>
    /// <param name="args">Optional command-line arguments.</param>
    /// <returns>A configured IGameHostBuilder instance ready for game setup.</returns>
    public static new IGameHostBuilder CreateBuilder(string[]? args = null)
    {
        IGameHostBuilder builder = new GameHostBuilder(args);

        builder.UseDefaultConfiguration();
        builder.AddCoreServices();

        return builder;
    }

    /// <summary>
    /// Runs the game application loop and returns the exit code.
    /// </summary>
    /// <remarks>
    /// This method initializes the GameApplicationRunner on first call and then delegates
    /// to the base ApplicationHost.Run() method.
    /// </remarks>
    public override int Run()
    {
        _gameRunner ??= Services.GetRequiredService<GameApplicationRunner>();
        return base.Run();
    }

}

/// <summary>
/// Provides logging extension methods for game host information.
/// </summary>
internal static class LoggerExtensions
{
    /// <summary>
    /// Logs game version and platform information.
    /// </summary>
    public static void LogGameInformation(this ILogger logger, IHostEnvironment env, GameVersion version)
    {
        logger.LogInformation(
            "{GameVersion} on {OS} - {Platform} {PlatformType}",
            version,
            env.OperatingSystem,
            env.OperatingSystem.Platform,
            env.PlatformType
        );
    }
}
