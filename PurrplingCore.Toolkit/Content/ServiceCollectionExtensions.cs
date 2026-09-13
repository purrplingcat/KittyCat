
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using PurrplingCore.Toolkit.DI;
using PurrplingCore.Toolkit.Hosting;
using PurrplingCore.Toolkit.Vfs;
using Zio;
using Zio.FileSystems;

namespace PurrplingCore.Toolkit.Content;

public delegate void VfsMountAction(IVirtualFileSystemManager vfs, IServiceProvider provider);

public static class ServiceCollectionExtensions
{

    public static IServiceCollection AddVfs(this IServiceCollection services)
    {
        return services.AddVfsCore();
    }

    public static IServiceCollection AddVfsCore(
        this IServiceCollection services)
    {
        services.TryAddSingleton<VirtualFileSystemManager>(sp =>
        {
            var env = sp.GetRequiredService<IHostEnvironment>();
            return new VirtualFileSystemManager(env);
        });
        services.TryAddSingleton<IVirtualFileSystemManager>(sp => sp.GetRequiredService<VirtualFileSystemManager>());
        services.TryAddSingleton<IVfsMountManager, VfsMountManager>();
        services.TryAddTransient(sp => 
            sp.GetRequiredService<IVirtualFileSystemManager>()
              .GetFileSystem()
        );
        
        return services;
    }
}
