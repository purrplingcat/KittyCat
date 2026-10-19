using PurrplingCore.Content;
using PurrplingCore.Graphics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Xna.Framework.Content;
using PurrplingCore.Ecs.DI;
using PurrplingCore.Toolkit;
using PurrplingCore.Toolkit.Content;
using PurrplingCore.Toolkit.DI;
using PurrplingCore.Toolkit.Graphics;
using PurrplingCore.Toolkit.Hosting;
using PurrplingCore.Toolkit.Hosting.Services;
using PurrplingCore.Toolkit.Vfs;

namespace PurrplingCore.Hosting;

/// <summary>
/// Configures KittyCat engine host services explicitly.
/// </summary>
public static class PurrplingCoreHostBuilderExtensions
{

    /// <summary>
    /// Registers engine services and the platform source used to enumerate bundled packages.
    /// </summary>
    /// <param name="builder">The game host builder.</param>
    /// <param name="packageFiles">
    /// Optional platform provider. Desktop defaults to the application base directory; packaged
    /// platforms should supply a provider over their application assets.
    /// </param>
    /// <returns>The same builder for chained configuration.</returns>
    public static IGameHostBuilder UsePurrplingCore(
        this IGameHostBuilder builder,
        IFileProvider? packageFiles = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (packageFiles is not null)
        {
            builder.Services.AddKeyedSingleton(ContentPackageManager.FileProviderKey, packageFiles);
        }

        builder.Services.AddServiceModule<EngineGameServices>();
        builder.Services.AddServiceModule<EngineAssemblyServices>();
        builder.Services.AddVfs();
        builder.Services.TryExpose<ContentManager, IContentManagerProvider>(
            static provider => provider.ContentManager);
        builder.Services.TryExpose<Resolution, GraphicsManager>(
            static graphics => graphics.Resolution);

        return builder;
    }

    private sealed class EngineAssemblyServices()
        : AssemblyServices(typeof(EngineAssemblyServices).Assembly);

    private sealed class EngineGameServices(IHostEnvironment env) : IServiceModule
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.TryAddSingleton<Screen, ResolutionScreen>();
            services.AddAssetRegistry();
            services.AddWorld();
            services.TryAddSingleton<ContentPackageManager>();
            services.AddStartup<EngineStartup>();
            services.TryAddKeyedSingleton<IFileProvider>(
                ContentPackageManager.FileProviderKey,
                new PhysicalFileProvider(env.BaseDirectory));
        }
    }
}
