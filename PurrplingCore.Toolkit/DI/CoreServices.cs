using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PurrplingCore.Toolkit.Content;
using PurrplingCore.Toolkit.Hosting;
using PurrplingCore.Toolkit.Messaging;
using System.Runtime.CompilerServices;

namespace PurrplingCore.Toolkit.DI;

public static class CoreServices
{
    public sealed class GameCoreServices : IServiceConfiguration
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
        services.TryAddEnumerable(ServiceDescriptor.Transient<IServiceConfiguration, GameCoreServices>());

        return services;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IGameHostBuilder AddCoreServices(this IGameHostBuilder builder)
    {
        builder.Services.AddCoreServices();
        return builder;
    }
}