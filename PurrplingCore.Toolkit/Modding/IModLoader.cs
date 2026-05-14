using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PurrplingCore.Toolkit.DI;
using PurrplingCore.Toolkit.Hosting;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;

namespace PurrplingCore.Toolkit.Modding;

public interface IModInitializer
{
    void Initialize(ModManifest manifest, string directoryPath, ILogger logger);
}

public interface IModStartup
{
    void Startup(IServiceProvider provider);
}

public interface IMod
{
    ModManifest Manifest { get; }
    string DirectoryPath { get; }
    ILogger Logger { get; }
}

[AttributeUsage(AttributeTargets.Class)]
public sealed class ModEntryAttribute : Attribute
{
}

public abstract class Mod : IMod, IModInitializer, IModStartup, IServiceConfiguration
{
    private Assembly? _assembly;

    public ModManifest Manifest { get; private set; } = null!;
    public string DirectoryPath { get; private set; } = string.Empty;
    public ILogger Logger { get; private set; } = NullLogger.Instance;

    public Assembly Assembly => _assembly ??= GetType().Assembly;

    void IModInitializer.Initialize(ModManifest manifest, string directoryPath, ILogger logger)
    {
        if (Manifest != null) 
            throw new InvalidOperationException("Mod is already initialized.");

        Manifest = manifest;
        DirectoryPath = directoryPath;
        Logger = logger;
        OnInitialize();
    }

    protected virtual void OnInitialize() { }
    public abstract void ConfigureServices(IServiceCollection services);
    public abstract void Startup(IServiceProvider provider);
}

internal sealed class ContentPack(ModManifest manifest, string directoryPath) : IMod
{
    public ModManifest Manifest { get; } = manifest;
    public string DirectoryPath { get; } = directoryPath;

    public ILogger Logger => NullLogger.Instance;
}

public record ModManifest(
    string Id,
    string Name,
    string Version,
    string Author,
    string[] Dependencies,
    string? EntryPointAssembly = null
);

public interface IModRegistry
{
    IReadOnlyCollection<IMod> Mods { get; }
    IMod? Get(string modId);
    bool IsLoaded(string modId);
}

internal class ModRegistry : IModRegistry
{
    private readonly Dictionary<string, IMod> _mods = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<IMod> Mods => _mods.Values;

    public ModRegistry(IHostEnvironment env)
    {
        var manifest = new ModManifest(
            env.ApplicationName,
            env.ApplicationName,
            env.GameVersion.ToString(),
            env.GameVersion.Author,
            []
        );

        Add(new ContentPack(manifest, env.HostDirectory));
    }

    public void Add(IMod mod)
    {
        if (!_mods.TryAdd(mod.Manifest.Id, mod))
        {
            throw new InvalidOperationException($"Duplicate mod: {mod.Manifest.Id}");
        }
    }

    public void AddRange(IEnumerable<IMod> mods)
    {
        foreach (IMod mod in mods) 
            Add(mod);
    }

    public bool IsLoaded(string modId) => _mods.ContainsKey(modId);

    public IMod? Get(string modId) => _mods.GetValueOrDefault(modId);
}

internal record struct ModEntry(ModManifest Manifest, string Directory);

internal sealed class ModLoader
{
    private readonly ModRegistry _registry;
    private readonly string _modsDirectory;
    private readonly ILogger _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly JsonSerializerOptions _jsonOptions;

    public ModLoader(ModRegistry registry, ILoggerFactory loggerFactory, string modsDirectory)
    {
        _registry = registry;
        _modsDirectory = modsDirectory;
        _loggerFactory = loggerFactory;
        _logger = _loggerFactory.CreateLogger("ModLoader");
        _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    }

    public void LoadMods(IServiceCollection appServices, IServiceProvider hostProvider)
    {
        int count = 0;
        var watch = Stopwatch.StartNew();
        _logger.LogInformation("Mods go here: {@Directory}", _modsDirectory);

        if (Directory.Exists(_modsDirectory))
        {
            var resolver = new ModDependencyResolver(_registry, _logger);
            List<ModEntry> modEntries = resolver.Resolve(DiscoverMods());
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
        int count = mods.Count;
        for (int i = 0; i < mods.Count; i++)
        {
            ModEntry entry = mods[i];
            try
            {
                IMod? mod = LoadModInstance(
                    entry.Manifest, entry.Directory, hostProvider
                );

                if (mod == null) continue;

                if (mod is IModStartup startup)
                {
                    appServices.TryAddEnumerable(ServiceDescriptor.Singleton(startup));
                    mod.Logger.LogTrace("Recognised as startup mod");
                }

                if (mod is IServiceConfiguration serviceConfiguration)
                {
                    mod.Logger.LogTrace("Registering mod services ...");
                    serviceConfiguration.ConfigureServices(appServices);
                }

                _registry.Add(mod);
            }
            catch (Exception ex)
            {
                --count;
                _logger.LogError(ex, "Fatal error while instantiating mod '{Id}'", entry.Manifest.Id);
            }
        }

        return count;
    }

    private Dictionary<string, ModEntry> DiscoverMods()
    {
        var discovered = new Dictionary<string, ModEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var dir in Directory.GetDirectories(_modsDirectory))
        {
            string manifestPath = Path.Combine(dir, "manifest.json");
            if (!File.Exists(manifestPath)) continue;

            try
            {
                var manifest = JsonSerializer.Deserialize<ModManifest>(File.ReadAllText(manifestPath), _jsonOptions);
                if (manifest == null) continue;

                if (!discovered.TryAdd(manifest.Id, new ModEntry(manifest, dir)))
                {
                    _logger.LogWarning("Duplicate mod '{Id}' found in '{Dir}'. Skipping.", manifest.Id, dir);
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse manifest from '{Path}'", manifestPath);
            }
        }

        return discovered;
    }

    private IMod? LoadModInstance(ModManifest manifest, string directory, IServiceProvider hostProvider)
    {
        // CONTENT PACK
        if (string.IsNullOrWhiteSpace(manifest.EntryPointAssembly))
        {
            _logger.LogTrace("Loaded content pack: {Id}", manifest.Id);
            return new ContentPack(manifest, directory);
        }

        // ASSEMBLY MOD
        string dllPath = Path.GetFullPath(Path.Combine(directory, manifest.EntryPointAssembly));
        if (!File.Exists(dllPath))
        {
            _logger.LogError("Mod '{Id}' missing entry DLL: {DllPath}", manifest.Id, dllPath);
            return null;
        }

        var alc = new ModAssemblyLoadContext(dllPath);
        var assembly = alc.LoadFromAssemblyPath(dllPath);

        var entryType = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .Where(t => t.GetCustomAttribute<ModEntryAttribute>(inherit: false) != null)
            .SingleOrDefault(t => typeof(IMod).IsAssignableFrom(t));

        if (entryType == null)
        {
            _logger.LogError("Mod '{Id}' has no valid IMod entry point in {Dll}", manifest.Id, dllPath);
            return null;
        }

        var modInstance = (IMod)ActivatorUtilities.CreateInstance(hostProvider, entryType);

        if (modInstance is IModInitializer initializer)
        {
            var modLogger = _loggerFactory.CreateLogger($"Mod[{manifest.Id}]");
            initializer.Initialize(manifest, directory, modLogger);
        }

        _logger.LogTrace("Loaded mod assembly for '{Id}': {Assembly}", manifest.Id, assembly.FullName);
        return modInstance;
    }
}

internal sealed class ModDependencyResolver(IModRegistry registry, ILogger logger)
{
    private Dictionary<string, ModEntry> _discoveredMods = [];
    private readonly ILogger _logger = logger;
    private readonly IModRegistry _registry = registry;
    private readonly Dictionary<string, ResolveState> _states = new(0, StringComparer.OrdinalIgnoreCase);
    private readonly List<ModEntry> _sorted = [];

    enum ResolveState : byte
    {
        NotVisited = 0,
        Visiting = 1,
        Resolved = 2,
        Failed = 3
    }

    public List<ModEntry> Resolve(Dictionary<string, ModEntry> discoveredMods)
    {
        _discoveredMods = discoveredMods;   
        _states.EnsureCapacity(discoveredMods.Count);
        _sorted.EnsureCapacity(discoveredMods.Count);
        _states.Clear();
        _sorted.Clear();

        foreach (var modId in _discoveredMods.Keys)
        {
            Visit(modId);
        }

        return _sorted;
    }

    private bool Visit(string modId)
    {
        if (_states.TryGetValue(modId, out var state))
        {
            if (state == ResolveState.Resolved) return true;
            if (state == ResolveState.Failed) return false;
            if (state == ResolveState.Visiting)
            {
                _logger.LogError("Mod '{Id}' failed to load: Involved in a CIRCULAR dependency.", modId);
                return false;
            }
        }

        if (!_discoveredMods.TryGetValue(modId, out var package))
        {
            if (_registry.IsLoaded(modId))
            {
                _states[modId] = ResolveState.Resolved;
                return true;
            }

            _logger.LogError("Mod failed to load: MISSING dependency '{Id}'.", modId);
            _states[modId] = ResolveState.Failed;
            return false;
        }

        _states[modId] = ResolveState.Visiting;

        bool dependenciesValid = true;
        if (package.Manifest.Dependencies != null)
        {
            foreach (var depId in package.Manifest.Dependencies)
            {
                if (!Visit(depId))
                {
                    _logger.LogError("Mod '{Id}' failed to load: CASCADE failure due to '{DepId}'.", package.Manifest.Id, depId);
                    dependenciesValid = false;
                }
            }
        }

        if (!dependenciesValid)
        {
            _states[modId] = ResolveState.Failed;
            return false;
        }

        _states[modId] = ResolveState.Resolved;
        _sorted.Add(package);
        return true;
    }
}

public class ModAssemblyLoadContext(string modAssemblyDll) : AssemblyLoadContext(isCollectible: true)
{
    private readonly AssemblyDependencyResolver _resolver = new(modAssemblyDll);

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        try
        {
            var defaultAssembly = Default.LoadFromAssemblyName(assemblyName);
            if (defaultAssembly != null) return defaultAssembly;
        }
        catch 
        {
        }

        string? assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath != null)
        {
            return LoadFromAssemblyPath(assemblyPath);
        }

        return null;
    }
}

internal sealed class ModStartupService(IEnumerable<IModStartup> startups, IServiceProvider services) 
    : IStartupService, ICleanupService
{
    public int Order => 1000;

    public void OnCleanup()
    {
        foreach(var disposable in startups.OfType<IDisposable>())
            disposable.Dispose();
    }

    public void OnStartup()
    {
        foreach(var mod in startups)
            mod.Startup(services);
    }
}

public static class ModdingExtensions
{
    public static IGameHostBuilder AddMods(this IGameHostBuilder builder, string modsDirectory)
    {
        // Add mod-related common services
        builder.Services.TryAddSingleton<ModRegistry>();
        builder.Services.TryAddAlias<IModRegistry, ModRegistry>();

        // Apply mod loader & app services
        builder.AddServiceConfiguration((appServices, hostProvider) =>
        {   
            // Add necessary mod-related app services
            appServices.AddStartup<ModStartupService>();

            // Create mod loader
            var registry = hostProvider.GetRequiredService<ModRegistry>();
            var loggerFactory = hostProvider.GetRequiredService<ILoggerFactory>();
            var modLoader = new ModLoader(registry, loggerFactory, modsDirectory);

            // Load mods
            modLoader.LoadMods(appServices, hostProvider);
        });

        return builder;
    }
}
