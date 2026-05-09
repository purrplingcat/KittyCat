using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PurrplingCore.Toolkit.DI;
using PurrplingCore.Toolkit.Metadata;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;

namespace PurrplingCore.Toolkit.Modding;

public interface IModEntryPoint
{
    void OnLoad(IServiceCollection services, ModContext context);
    void OnStartup(IServiceProvider provider);
    void OnShutdown(IServiceProvider provider);
}

public record struct ModContext(
    ModManifest Manifest,
    ILogger Logger,
    string ModDirectoryPath,
    string GamePath,
    GameVersion GameVersion,
    OperatingSystem OperatingSystem,
    PlatformType PlatformType,
    IModRegistry Registry
);

public record ModManifest(
    string Id,
    string Name,
    string Version,
    string Author,
    string[] Dependencies,
    string? EntryPointAssembly = null
);

internal record ModPackage(ModManifest Manifest, string DirectoryPath);
internal record LoadedMod(ModPackage Package, Assembly? Assembly, List<IModEntryPoint> EntryPoints)
{
    public string Id => Manifest.Id;
    public string DirectoryPath => Package.DirectoryPath;
    public ModManifest Manifest => Package.Manifest;
}

public interface IModRegistry
{
    bool IsModLoaded(string modId);
    ModManifest? GetManifest(string modId);
    ModManifest? GetManifest(Assembly assembly);
}

internal class ModRegistry : IModRegistry
{
    private readonly IReadOnlyList<LoadedMod> _loadedMods = [];

    public ModRegistry(IReadOnlyList<LoadedMod> loadedMods)
    {
        _loadedMods = loadedMods;
    }

    public ModRegistry() { }

    public bool IsModLoaded(string modId)
        => _loadedMods.Any(m => EqualsIgnoreCase(m.Id, modId));

    public ModManifest? GetManifest(string modId)
    {
        ArgumentException.ThrowIfNullOrEmpty(modId, nameof(modId));
        return _loadedMods.FirstOrDefault(m => EqualsIgnoreCase(m.Id, modId))?.Manifest;
    }

    private static bool EqualsIgnoreCase(string left, string right)
    {
        return left.Equals(right, StringComparison.OrdinalIgnoreCase);
    }

    public ModManifest? GetManifest(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly, nameof(assembly));
        return _loadedMods.FirstOrDefault(m => m.Assembly == assembly)?.Manifest;
    }
}

public class ModLoader(string modsDirectory) : IGameHostPlugin
{
    public string Name { get; } = "PurrplingCore Mod Loader";

    public void OnAdd(IGameHostBuilder builder)
    {
    }

    private static LoadedMod GetGameAsMod(GameVersion version, string directory)
    {
        var gamePackage = new ModPackage(version.ToManifest(), directory);
        return new LoadedMod(gamePackage, version.Type?.Assembly, []);
    }

    public void OnBuild(IGameHostBuilder builder, GameHostBuilderContext context)
    {
        var logger = context.CreateLogger<ModLoader>();
        var discoveredManifests = DiscoverManifests(logger);
        var loadOrder = ResolveDependencies(discoveredManifests);
        var modDirectories = loadOrder.Select(m => m.DirectoryPath).ToList();
        var loadedMods = new List<LoadedMod>
        {
            GetGameAsMod(context.GameVersion, context.Directory)
        };

        loadedMods.AddRange(LoadModAssemblies(loadOrder, logger));

        builder.ConfigureServices((services, ctx) =>
        {
            var registry = new ModRegistry(loadedMods);

            services.AddSingleton<IModRegistry>(registry);

            foreach (var loaded in loadedMods)
            {
                var modContext = new ModContext(
                    Manifest: loaded.Package.Manifest,
                    Logger: ctx.CreateLogger($"ModEntry {loaded.Manifest.Name}"),
                    ModDirectoryPath: loaded.Package.DirectoryPath,
                    GamePath: ctx.Directory,
                    GameVersion: ctx.GameVersion,
                    OperatingSystem: ctx.OperatingSystem,
                    PlatformType: ctx.PlatformType,
                    Registry: registry
                );

                foreach (var entryPoint in loaded.EntryPoints)
                {
                    entryPoint.OnLoad(services, modContext);
                    services.TryAddEnumerable(ServiceDescriptor.Singleton<IModEntryPoint>(entryPoint));
                }
            }

            services.AddStartup<ModInitializerStartupService>();
        });
    }

    private List<ModPackage> DiscoverManifests(ILogger logger)
    {
        var packages = new List<ModPackage>();

        logger.LogInformation("Discovering mods in '{ModsDirectory}'...", modsDirectory);
        if (!Directory.Exists(modsDirectory)) return packages;

        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        foreach (var dir in Directory.GetDirectories(modsDirectory))
        {
            string manifestPath = Path.Combine(dir, "manifest.json");
            if (!File.Exists(manifestPath)) continue;

            try
            {
                string json = File.ReadAllText(manifestPath);
                var manifest = JsonSerializer.Deserialize<ModManifest>(json, jsonOptions);

                if (manifest != null)
                {
                    packages.Add(new ModPackage(manifest, dir));
                    logger.LogDebug(
                        "Found mod {@ModName} v{ModVersion} by {ModAuthor} (ID: {ModId})", 
                        manifest.Name, manifest.Version, manifest.Author, manifest.Id
                    );
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to load manifest from '{ManifestPath}'", manifestPath);
            }
        }

        return packages;
    }

    private List<ModPackage> ResolveDependencies(List<ModPackage> rawManifests)
    {
        // Zde napíšeš jednoduchý grafový algoritmus, který:
        // 1. Zjistí, jestli má Mod B načtený Mod A.
        // 2. Vrátí seřazený list (např. [Core, ModA, ModB]).
        // Pokud Mod B vyžaduje neexistující Mod C, rovnou Mod B z listu vyhodíš a zaloguješ Error.
        return rawManifests; // Zjednodušeno
    }

    private static IEnumerable<IModEntryPoint> ExtractEntryPoints(Assembly assembly)
    {
        var modTypes = assembly.GetTypes()
            .Where(t => typeof(IModEntryPoint).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        foreach (var type in modTypes)
        {
            yield return (IModEntryPoint)Activator.CreateInstance(type)!;
        }
    }

    private static IEnumerable<LoadedMod> LoadModAssemblies(List<ModPackage> loadOrder, ILogger logger)
    {
        foreach (var package in loadOrder)
        {
            string? targetDll = package.Manifest.EntryPointAssembly;
            if (string.IsNullOrWhiteSpace(targetDll))
            {
                // Mod bez kódu – přidáme ho taky, aby o něm registry věděly
                yield return new LoadedMod(package, null, []);
                continue;
            }

            string fullPath = Path.Combine(package.DirectoryPath, targetDll);
            if (!File.Exists(fullPath))
            {
                logger.LogError("Mod '{Id}' declares EntryDll '{Dll}', but it's missing.", package.Manifest.Id, targetDll);
                continue;
            }

            var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(fullPath));
            var entryPoints = ExtractEntryPoints(assembly).ToList();
 
            logger.LogTrace(
                "Loaded mod assembly {@ModAssembly} for '{Id}' with {Count} entry points.",
                targetDll, package.Manifest.Id, entryPoints.Count
            );

            yield return new LoadedMod(package, assembly, entryPoints);
        }
    }
}

internal class ModInitializerStartupService(IEnumerable<IModEntryPoint> loadedMods, IServiceProvider services) : IStartupService, ICleanupService
{
    public int Order => 100;

    public void OnCleanup()
    {
        foreach (var mod in loadedMods)
        {
            mod.OnShutdown(services);
        }
    }

    public void OnStartup()
    {
        foreach (var mod in loadedMods)
        {
            mod.OnStartup(services);
        }
    }
}
