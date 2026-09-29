using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PurrplingCore.Toolkit.DI;
using PurrplingCore.Toolkit.Hosting.Services;

namespace PurrplingCore.Toolkit.Hosting;

public sealed class GameHost : ApplicationHost
{
    private GameApplicationRunner? _gameRunner;

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

    public bool IsRunning => _gameRunner?.IsRunning ?? false;

    public static new IGameHostBuilder CreateBuilder(string[]? args = null)
    {
        IGameHostBuilder builder = new GameHostBuilder(args);

        builder.UseDefaultConfiguration();
        builder.AddCoreServices();

        return builder;
    }

    public override int Run()
    {
        _gameRunner ??= Services.GetRequiredService<GameApplicationRunner>();
        return base.Run();
    }

}

internal static class LoggerExtensions
{
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
