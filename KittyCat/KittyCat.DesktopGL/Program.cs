using KittyCat;
using Microsoft.Extensions.Logging;
using PurrplingCore.Toolkit;
using PurrplingCore.Toolkit.DI;
using System;
using PurrplingCore.Toolkit.Extensions;
using Microsoft.Extensions.DependencyInjection;
using PurrplingCore.Ecs;

internal class Program
{
    /// <summary>
    /// The main entry point for the application. 
    /// This creates an instance of your game and calls it's Run() method 
    /// </summary>
    /// <param name="args">Command-line arguments passed to the application.</param>
    private static void Main(string[] args)
    {
        Console.WriteLine($"{KittyCatGame.GameName} {KittyCatGame.VersionInfo} ({KittyCatGame.Version})");
        Console.WriteLine($"Running on {Environment.OSVersion} ({Game.PlatformName})");
        Console.WriteLine();

        using var host = GameHost.CreateBuilder()
            .AddGame<KittyCatGame>()
            .ConfigureLogging(ConfigureLogger)
            .Build();

        host.Run();
    }

    private static void ConfigureLogger(ILoggingBuilder builder)
    {
        builder.SetMinimumLevel(LogLevel.Trace);
        builder.AddDebug();
        builder.AddSimpleConsole(o =>
        {
            o.SingleLine = true;
            o.TimestampFormat = "HH:mm:ss ";
            o.IncludeScopes = true;
        });
    }
}
