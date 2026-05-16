
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
    public static IServiceCollection AddVfs(this IServiceCollection services, VfsMountAction setup)
    {
        services.AddVfs();
        services.AddSingleton(setup);
        return services;
    }

    public static IServiceCollection AddVfs(this IServiceCollection services)
    {
        services.TryAddSingleton(sp =>
        {
            
            var env = sp.GetRequiredService<IHostEnvironment>();
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("VfsManager");
            var vfs = new VirtualFileSystemManager(env, logger);
            var actions = sp.GetServices<VfsMountAction>();

            foreach (var action in actions)
            {
                action(vfs, sp);
            }

            logger.LogVfsStructure(vfs.Root);
            return vfs;
        });
        services.TryAddAlias<IVirtualFileSystem, VirtualFileSystemManager>();
        services.TryAddAlias<IVirtualFileSystemManager, VirtualFileSystemManager>();

        return services;
    }

    public static IServiceCollection AddPhysicalVfs(this IServiceCollection services, string path, int order = 0)
    {
        return services.AddVfsLayer(order, sp =>
        {
            var rootFs = new PhysicalFileSystem();
            return new SubFileSystem(rootFs, rootFs.ConvertPathFromInternal(path));
        });
    }

    public static IServiceCollection AddVfsLayer(this IServiceCollection services, int order, Func<IServiceProvider, IFileSystem> factory)
    {
        return services.AddSingleton(sp => new FileSystemLayer(factory(sp), order));
    }
}
