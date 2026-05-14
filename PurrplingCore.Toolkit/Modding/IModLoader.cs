using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PurrplingCore.Toolkit.Content;
using PurrplingCore.Toolkit.DI;
using PurrplingCore.Toolkit.Hosting;
using System.Reflection;
using System.Runtime.Loader;
using Zio;

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

public abstract class Mod : IMod, IModInitializer, IModStartup, IServicesConfiguration
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

        logger.LogTrace("Initializing mod ...");
        Manifest = manifest;
        DirectoryPath = directoryPath;
        Logger = logger;
        OnInitialize();
    }

    protected virtual void OnInitialize() { }
    public abstract void ConfigureServices(IServiceCollection services);
    public abstract void Startup(IServiceProvider provider);
}

internal sealed class ContentPack(ModManifest manifest, string directoryPath, ILogger? logger = null) : IMod
{
    public ModManifest Manifest { get; } = manifest;
    public string DirectoryPath { get; } = directoryPath;

    public ILogger Logger => logger ?? NullLogger.Instance;
}

public record ModManifest
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Version { get; init; }
    public required string Author { get; init; }
    public string[] Dependencies { get; init; } = [];
    public MountPoint[]? Mounts { get; init; }
    public string? EntryPointAssembly { get; init; } = null;
}

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
        var manifest = env.GameVersion.ToManifest();
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
        EnsureClear();

        foreach (var modId in _discoveredMods.Keys)
        {
            Visit(modId);
        }

        return _sorted;
    }

    private void EnsureClear()
    {
        _states.Clear();
        _sorted.Clear();
        _states.EnsureCapacity(_discoveredMods.Count);
        _sorted.EnsureCapacity(_discoveredMods.Count);
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
