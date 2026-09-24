
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

public static class VfsServiceCollectionExtensions
{

    public static IServiceCollection AddVfs(this IServiceCollection services)
    {
        services.TryAddSingleton<VirtualFileSystemManager>();
        services.TryAddSingleton<IVirtualFileSystemManager>(sp => sp.GetRequiredService<VirtualFileSystemManager>());
        services.TryAddTransient(sp => 
            sp.GetRequiredService<IVirtualFileSystemManager>()
              .GetFileSystem()
        );
        
        return services;
    }
}
