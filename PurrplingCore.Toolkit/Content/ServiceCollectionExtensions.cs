
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
        services.AddVfsCore((vfs, _) =>
        {
            vfs.Chain(fs => new AggregateFileSystem(fs));
            vfs.Chain(fs => new MountFileSystem(fs));
        });

        return services;
    }

    public static IServiceCollection AddVfsCore(
        this IServiceCollection services,
        Action<IVirtualFileSystemManager, IServiceProvider>? configureChain = null)
    {
        services.TryAddSingleton<IVirtualFileSystemManager>(sp =>
        {
            var env = sp.GetRequiredService<IHostEnvironment>();
            var logger = sp.GetRequiredService<ILogger<VirtualFileSystemManager>>();
            var vfs = new VirtualFileSystemManager(env);

            configureChain?.Invoke(vfs, sp);

            return vfs;
        });
        services.TryAddTransient(sp => 
            sp.GetRequiredService<IVirtualFileSystemManager>()
              .GetFileSystem()
        );
        
        return services;
    }
}
