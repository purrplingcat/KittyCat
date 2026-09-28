using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PurrplingCore.Toolkit.Hosting;
using Zio;

namespace PurrplingCore.Toolkit.Vfs;

public delegate void VfsMountAction(IVirtualFileSystemManager vfs, IServiceProvider provider);

public static class VfsServiceCollectionExtensions
{
    /// <summary>
    /// Registers the virtual filesystem and exposes its aggregate namespace through the standard
    /// <see cref="IFileProvider"/> abstraction.
    /// </summary>
    /// <remarks>
    /// The provider is the only integration point consumed by content catalog indexing; VFS does
    /// not reference the asset registry or catalog types.
    /// </remarks>
    public static IServiceCollection AddVfs(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<VirtualFileSystemManager>();
        services.TryAddSingleton<IVirtualFileSystemManager>(sp => sp.GetRequiredService<VirtualFileSystemManager>());
        services.TryAddTransient(sp => sp.GetRequiredService<IVirtualFileSystemManager>().GetFileSystem());
        services.TryAddSingleton<IFileProvider>(sp =>
            new VirtualFileProvider(sp.GetRequiredService<IVirtualFileSystemManager>().GetFileSystem()));

        return services;
    }
}