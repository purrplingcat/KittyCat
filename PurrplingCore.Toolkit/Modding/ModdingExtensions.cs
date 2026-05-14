using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using PurrplingCore.Toolkit.Content;
using PurrplingCore.Toolkit.DI;
using PurrplingCore.Toolkit.Hosting;
using System.Runtime.CompilerServices;

namespace PurrplingCore.Toolkit.Modding;

public static class ModdingExtensions
{
    public static IGameHostBuilder AddMods(this IGameHostBuilder builder, string modsDirectory)
    {
        // Add mod-related common services
        builder.Services.TryAddSingleton<ModRegistry>();
        builder.Services.TryAddAlias<IModRegistry, ModRegistry>();
        builder.Services.AddVfs();

        // Apply mod loader & app services
        builder.AddServiceConfiguration((appServices, hostProvider) =>
        {   
            // Add necessary mod-related app services
            appServices.AddStartup<ModStartupService>();

            // Create mod loader
            var registry = hostProvider.GetRequiredService<ModRegistry>();
            var loggerFactory = hostProvider.GetRequiredService<ILoggerFactory>();
            var modLoader = new ModLoader(registry, loggerFactory, modsDirectory);

            // Load mods
            modLoader.LoadMods(appServices, hostProvider);
        });

        return builder;
    }
}
