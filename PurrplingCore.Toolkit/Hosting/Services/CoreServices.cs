using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PurrplingCore.Toolkit.Content;
using PurrplingCore.Toolkit.DI;
using PurrplingCore.Toolkit.Hosting;
using PurrplingCore.Toolkit.Messaging;
using System.Runtime.CompilerServices;

namespace PurrplingCore.Toolkit.Hosting.Services;

public static class CoreServices
{
    /// <summary>
    /// Default services added to the game service collection. Game configurations run afterwards and may replace them.
    /// </summary>
    private sealed class GameCoreServices : IServiceModule
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions<ContentManagerOptions>();
            services.TryAddSingleton<IContentManagerProvider, DefaultContentManagerProvider>();
        }
    }

    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        services.TryAddSingleton<IMessageBus, DefaultMessageBus>();
        services.AddServiceModule<GameCoreServices>();

        return services;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IGameHostBuilder AddCoreServices(this IGameHostBuilder builder)
    {
        builder.Services.AddCoreServices();
        return builder;
    }
}
