using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using PurrplingCore.Toolkit.Content;
using PurrplingCore.Toolkit.DI;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using Zio;
using Zio.FileSystems;

namespace PurrplingCore.Toolkit.Modding;

public record struct MountPoint(string Target, string[] Sources);

internal sealed class ModLoader
{
    private readonly ModRegistry _registry;
    private readonly string _modsDirectory;
    private readonly ILogger _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ModFactory _factory;

    public ModLoader(ModRegistry registry, ILoggerFactory loggerFactory, string modsDirectory)
    {
        _registry = registry;
        _modsDirectory = modsDirectory;
        _loggerFactory = loggerFactory;
        _logger = _loggerFactory.CreateLogger("ModLoader");
        _factory = new ModFactory(loggerFactory);
    }

    public void LoadMods(IServiceCollection appServices, IServiceProvider hostProvider)
    {
        LoadModsAsync(appServices, hostProvider)
            .GetAwaiter()
            .GetResult();
    }

    public async Task LoadModsAsync(IServiceCollection appServices, IServiceProvider hostProvider)
    {
        int count = 0;
        var watch = Stopwatch.StartNew();
        _logger.LogInformation("Mods go here: {@Directory}", _modsDirectory);

        if (Directory.Exists(_modsDirectory))
        {
            var discovery = new ModDiscovery(_logger);
            var resolver = new ModDependencyResolver(_registry, _logger);
            List<ModEntry> modEntries = resolver.Resolve(await discovery.Discover(_modsDirectory));
            count = LoadMods(appServices, hostProvider, modEntries);
        }

        watch.Stop();
        _logger.LogInformation(
            "Loaded {Count} mods in {Duration} ms", 
            count, watch.ElapsedMilliseconds
        );
    }

    private int LoadMods(IServiceCollection appServices, IServiceProvider hostProvider, List<ModEntry> mods)
    {
        int count = 0;
        var physicalFs = new PhysicalFileSystem();

        for (int i = 0; i < mods.Count; i++)
        {
            ModEntry entry = mods[i];
            try
            {
                var modPath = physicalFs.ConvertPathFromInternal(entry.Directory);
                var mounts = _factory.ResolveMountPoints(physicalFs, modPath, entry.Manifest.Mounts);
                var modVfs = _factory.CreateBaseFileSystem(physicalFs, entry, mounts);
                IMod? mod = _factory.CreateMod(entry, modVfs, hostProvider);

                if (mod == null) continue;

                _registry.Add(mod);
                ++count;

                ProcessMod(appServices, mod);
                appServices.RegisterModVfs(entry, modVfs, 1000 + count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error while instantiating mod '{Id}'", entry.Manifest.Id);
            }
        }

        return count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ProcessMod(IServiceCollection appServices, IMod mod)
    {
        if (mod is IServicesConfiguration serviceConfiguration)
        {
            mod.Logger.LogTrace("Registering mod services ...");
            serviceConfiguration.ConfigureServices(appServices);
        }

        if (mod is IModStartup startup)
        {
            appServices.TryAddEnumerable(ServiceDescriptor.Singleton(startup));
            mod.Logger.LogTrace("Recognized as startup mod");
        }
    }
}

internal sealed class ModFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger _logger;
    private readonly ModMetadataScanner _scanner;

    public ModFactory(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
        _logger = _loggerFactory.CreateLogger("ModFactory");
        _scanner = new ModMetadataScanner(_logger);
    }

    public IMod? CreateMod(ModEntry entry, IFileSystem vfs, IServiceProvider hostProvider)
    {
        using var scope = _logger.BeginScope("Mod: {ModId}", entry.Manifest.Id);
        
        if (string.IsNullOrWhiteSpace(entry.Manifest.EntryPointAssembly))
        {
            _logger.LogTrace("Loaded content pack: {Name}", entry.Manifest.Name);
            return new ContentPack(entry.Manifest, entry.Directory);
        }

        return LoadAssemblyMod(entry.Manifest, entry.Directory, vfs, hostProvider);
    }

    public IEnumerable<MountPoint> ResolveMountPoints(IFileSystem physicalFs, UPath modPath, IEnumerable<MountPoint>? manifestMounts)
    {
        if (manifestMounts != null)
        {
            return manifestMounts;
        }

        var autoMounts = new List<MountPoint>();

        var paksPath = UPath.Combine(modPath, "Paks");
        if (physicalFs.DirectoryExists(paksPath))
        {
            var pakSources = new List<string>();

            foreach (var path in physicalFs.EnumerateFiles(paksPath))
            {
                pakSources.Add($"Paks/{path.GetName()}");
            }

            if (pakSources.Count > 0)
            {
                autoMounts.Add(new MountPoint
                {
                    Target = "/",
                    Sources = [.. pakSources]
                });
            }
        }

        string[] standardFolders = ["Content", "assets", "i18n", "data", "private"];
        foreach (var folder in standardFolders)
        {
            if (physicalFs.DirectoryExists(UPath.Combine(modPath, folder)))
            {
                autoMounts.Add(
                    new MountPoint
                    { 
                        Target = (string)UPath.Combine(UPath.Root, folder), 
                        Sources = [folder] 
                    }
                );
            }
        }

        return autoMounts;
    }

    public IFileSystem CreateBaseFileSystem(IFileSystem physicalFs, ModEntry entry, IEnumerable<MountPoint> mountEntries)
    {
        var modPath = physicalFs.ConvertPathFromInternal(entry.Directory);
        var virtualRoot = new MountFileSystem();

        foreach (var group in mountEntries)
        {
            var target = group.Target;
            var aggregateFs = new AggregateFileSystem();
            bool hasAnySource = false;

            foreach (var source in group.Sources)
            {
                var path = UPath.Combine(modPath, source);

                if (physicalFs.DirectoryExists(path))
                {
                    aggregateFs.AddFileSystem(new SubFileSystem(physicalFs, path));
                    hasAnySource = true;
                }
                else if (physicalFs.FileExists(path))
                {
                    var ext = path.GetExtensionWithDot();
                    hasAnySource = true;

                    switch (ext)
                    {
                        case ".zip":
                            var zipStream = physicalFs.OpenFile(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                            aggregateFs.AddFileSystem(new ZipArchiveFileSystem(zipStream));
                            break;
                        default:
                            throw new IOException($"Unsupported VFS type: {ext}");
                    }
                }
                else
                {
                    _logger.LogError(
                        "Mount failed: {sourceName}, Target: {target}",
                        source, target
                    );
                }
            }

            if (hasAnySource)
            {
                virtualRoot.Mount(target, aggregateFs);
            }
        }

        return new ReadOnlyFileSystem(virtualRoot); 
    }

    private IMod? LoadAssemblyMod(ModManifest manifest, string directory, IFileSystem vfs, IServiceProvider hostProvider)
    {
        string dllPath = Path.GetFullPath(Path.Combine(directory, manifest.EntryPointAssembly!));

        if (!File.Exists(dllPath))
        {
            _logger.LogError("Missing entry DLL: {DllPath}", dllPath);
            return null;
        }

        string? entryTypeName = _scanner.FindEntryPoint(dllPath);
        if (entryTypeName == null)
        {
            _logger.LogError("Mod has no valid [ModEntry] point in {Dll}", dllPath);
            return null;
        }

        var mod = InstantiateMod(manifest, directory, dllPath, entryTypeName, hostProvider);
        if (mod is IModInitializer initializer)
        {
            var modLogger = _loggerFactory.CreateLogger($"Mod[{manifest.Name}]");
            initializer.Initialize(manifest, directory, modLogger);
        }

        return mod;
    }

    private IMod? InstantiateMod(ModManifest manifest, string directory, string dllPath, string entryTypeName, IServiceProvider hostProvider)
    {
        var alc = new ModAssemblyLoadContext(dllPath);
        var assembly = alc.LoadFromAssemblyPath(dllPath);
        var entryType = assembly.GetType(entryTypeName);

        if (entryType == null || !typeof(IMod).IsAssignableFrom(entryType))
        {
            _logger.LogError("Entry type '{Type}' does not implement IMod.", entryTypeName);
            return null;
        }

        var mod = (IMod)ActivatorUtilities.CreateInstance(hostProvider, entryType);
        _logger.LogTrace("Loaded mod assembly: {Assembly}", assembly.FullName);
        
        return mod;
    }
}

internal static class ServiceCollectionExtensions
{
    public static void RegisterModVfs(this IServiceCollection services, ModEntry entry, IFileSystem baseFs, int loadOrder)
    {
        services.AddVfsLayer(loadOrder, sp =>
        {
            var mountFs = new MountFileSystem();

            if (baseFs.DirectoryExists("/Content"))
            {
                mountFs.Mount("/Content", new SubFileSystem(baseFs, "/Content"));
            }

            // Assets: /assets -> /Mods/Id
            if (baseFs.DirectoryExists("/assets"))
            {
                mountFs.Mount($"/Content/Mods/{entry.Manifest.Id}", new SubFileSystem(baseFs, "/assets"));
            }

            return mountFs;
        });
    }
}