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

/// <summary>
/// Initializes a mod after its manifest is loaded, providing the directory path and logger.
/// </summary>
public interface IModInitializer
{
    /// <summary>
    /// Initializes the mod with its manifest and directory information.
    /// </summary>
    /// <param name="manifest">The mod's manifest containing metadata.</param>
    /// <param name="directoryPath">The full path to the mod's directory.</param>
    /// <param name="logger">A logger instance for the mod to use.</param>
    void Initialize(ModManifest manifest, string directoryPath, ILogger logger);
}

/// <summary>
/// Handles startup procedures for a mod when the host begins execution.
/// </summary>
public interface IModStartup
{
    /// <summary>
    /// Called during application startup to perform mod initialization logic.
    /// </summary>
    /// <param name="provider">The service provider for dependency resolution.</param>
    void Startup(IServiceProvider provider);
}

/// <summary>
/// Represents a loaded mod with its metadata, directory, and logger.
/// </summary>
public interface IMod
{
    /// <summary>
    /// Gets the mod's manifest containing metadata (id, name, version, author, etc.).
    /// </summary>
    ModManifest Manifest { get; }
    
    /// <summary>
    /// Gets the full path to the mod's directory.
    /// </summary>
    string DirectoryPath { get; }
    
    /// <summary>
    /// Gets the logger instance for this mod.
    /// </summary>
    ILogger Logger { get; }
}

/// <summary>
/// Attribute marking a class as the entry point for a mod.
/// </summary>
/// <remarks>
/// Apply this attribute to a class that implements the Mod abstract class to designate it
/// as the mod's entry point. Only one entry point per mod assembly is allowed.
/// </remarks>
[AttributeUsage(AttributeTargets.Class)]
public sealed class ModEntryAttribute : Attribute
{
}

/// <summary>
/// Abstract base class for implementing mods with service configuration and startup logic.
/// </summary>
/// <remarks>
/// Derive from this class and apply the ModEntryAttribute to create a mod.
/// The mod must implement ConfigureServices (for dependency injection) and Startup (for initialization).
/// </remarks>
public abstract class Mod : IMod, IModInitializer, IModStartup, IServiceModule
{
    private Assembly? _assembly;

    /// <summary>
    /// Gets the mod's manifest.
    /// </summary>
    public ModManifest Manifest { get; private set; } = null!;
    
    /// <summary>
    /// Gets the mod's directory path.
    /// </summary>
    public string DirectoryPath { get; private set; } = string.Empty;
    
    /// <summary>
    /// Gets the mod's logger instance.
    /// </summary>
    public ILogger Logger { get; private set; } = NullLogger.Instance;

    /// <summary>
    /// Gets the mod's assembly.
    /// </summary>
    public Assembly Assembly => _assembly ??= GetType().Assembly;

    /// <summary>
    /// Initializes the mod with its manifest and directory information.
    /// </summary>
    /// <remarks>
    /// This is called automatically by the mod loader. Direct calls will throw if already initialized.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown if the mod is already initialized.</exception>
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

    /// <summary>
    /// Called after the mod is initialized. Override to perform custom initialization logic.
    /// </summary>
    /// <remarks>
    /// The base implementation does nothing. Override to add custom initialization logic.
    /// </remarks>
    protected virtual void OnInitialize() { }
    
    /// <summary>
    /// Configures services for dependency injection in the mod.
    /// </summary>
    /// <remarks>
    /// This method is called when the mod is being added to the service collection.
    /// </remarks>
    /// <param name="services">The service collection to configure.</param>
    public abstract void ConfigureServices(IServiceCollection services);
    
    /// <summary>
    /// Performs startup initialization for the mod.
    /// </summary>
    /// <remarks>
    /// This is called during application startup after all services are configured.
    /// </remarks>
    /// <param name="provider">The service provider for dependency resolution.</param>
    public abstract void Startup(IServiceProvider provider);
}

/// <summary>
/// Represents a content pack as a mod (a mod without an entry point assembly).
/// </summary>
/// <remarks>
/// Content packs are mods that only contain assets and configuration, with no executable code.
/// </remarks>
internal sealed class ContentPack(ModManifest manifest, string directoryPath, ILogger? logger = null) : IMod
{
    /// <summary>
    /// Gets the content pack's manifest.
    /// </summary>
    public ModManifest Manifest { get; } = manifest;
    
    /// <summary>
    /// Gets the content pack's directory path.
    /// </summary>
    public string DirectoryPath { get; } = directoryPath;

    /// <summary>
    /// Gets the content pack's logger.
    /// </summary>
    public ILogger Logger => logger ?? NullLogger.Instance;
}

/// <summary>
/// Manifest describing a mod's metadata and configuration.
/// </summary>
/// <remarks>
/// The manifest is typically loaded from a JSON file (mod.json) in the mod's directory.
/// </remarks>
public record ModManifest
{
    /// <summary>
    /// Gets the unique identifier for the mod.
    /// </summary>
    public required string Id { get; init; }
    
    /// <summary>
    /// Gets the human-readable name of the mod.
    /// </summary>
    public required string Name { get; init; }
    
    /// <summary>
    /// Gets the version string for the mod (e.g., "1.0.0").
    /// </summary>
    public required string Version { get; init; }
    
    /// <summary>
    /// Gets the author name(s).
    /// </summary>
    public required string Author { get; init; }
    
    /// <summary>
    /// Gets the list of mod IDs this mod depends on.
    /// </summary>
    public string[] Dependencies { get; init; } = [];
    
    /// <summary>
    /// Gets the mount points for virtual file system integration.
    /// </summary>
    public MountPoint[]? Mounts { get; init; }
    
    /// <summary>
    /// Gets the name of the assembly containing the mod's entry point class.
    /// </summary>
    public string? EntryPointAssembly { get; init; } = null;
}

/// <summary>
/// Provides read-only access to loaded mods.
/// </summary>
public interface IModRegistry
{
    /// <summary>
    /// Gets the collection of all loaded mods.
    /// </summary>
    IReadOnlyCollection<IMod> Mods { get; }
    
    /// <summary>
    /// Gets a loaded mod by its ID.
    /// </summary>
    /// <param name="modId">The mod's unique identifier.</param>
    /// <returns>The mod instance, or null if not found.</returns>
    IMod? Get(string modId);
    
    /// <summary>
    /// Determines if a mod has been loaded.
    /// </summary>
    /// <param name="modId">The mod's unique identifier.</param>
    /// <returns>True if the mod is loaded; false otherwise.</returns>
    bool IsLoaded(string modId);
}

/// <summary>
/// Maintains a registry of loaded mods and provides lookup by ID.
/// </summary>
internal class ModRegistry : IModRegistry
{
    private readonly Dictionary<string, IMod> _mods = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the collection of all loaded mods.
    /// </summary>
    public IReadOnlyCollection<IMod> Mods => _mods.Values;

    /// <summary>
    /// Initializes a new instance of ModRegistry with the base game mod.
    /// </summary>
    /// <remarks>
    /// The registry is pre-populated with a ContentPack representing the game itself.
    /// </remarks>
    public ModRegistry(IHostEnvironment env, GameVersion version)
    {
        var manifest = version.ToManifest();
        Add(new ContentPack(manifest, env.HostDirectory));
    }

    /// <summary>
    /// Adds a mod to the registry.
    /// </summary>
    /// <param name="mod">The mod to add.</param>
    /// <exception cref="InvalidOperationException">Thrown if a mod with the same ID is already registered.</exception>
    public void Add(IMod mod)
    {
        if (!_mods.TryAdd(mod.Manifest.Id, mod))
        {
            throw new InvalidOperationException($"Duplicate mod: {mod.Manifest.Id}");
        }
    }

    /// <summary>
    /// Adds multiple mods to the registry.
    /// </summary>
    /// <param name="mods">The mods to add.</param>
    public void AddRange(IEnumerable<IMod> mods)
    {
        foreach (IMod mod in mods) 
            Add(mod);
    }

    /// <summary>
    /// Determines if a mod has been loaded.
    /// </summary>
    /// <param name="modId">The mod's unique identifier.</param>
    /// <returns>True if the mod is loaded; false otherwise.</returns>
    public bool IsLoaded(string modId) => _mods.ContainsKey(modId);

    /// <summary>
    /// Gets a loaded mod by its ID.
    /// </summary>
    /// <param name="modId">The mod's unique identifier.</param>
    /// <returns>The mod instance, or null if not found.</returns>
    public IMod? Get(string modId) => _mods.GetValueOrDefault(modId);
}

/// <summary>
/// Represents a discovered mod awaiting resolution and loading.
/// </summary>
internal record struct ModEntry(ModManifest Manifest, string Directory);

/// <summary>
/// Resolves mod dependencies in topological order, detecting cycles and missing dependencies.
/// </summary>
/// <remarks>
/// Uses depth-first search with color-based cycle detection to sort mods and validate the dependency graph.
/// </remarks>
internal sealed class ModDependencyResolver(IModRegistry registry, ILogger logger)
{
    private Dictionary<string, ModEntry> _discoveredMods = [];
    private readonly ILogger _logger = logger;
    private readonly IModRegistry _registry = registry;
    private readonly Dictionary<string, ResolveState> _states = new(0, StringComparer.OrdinalIgnoreCase);
    private readonly List<ModEntry> _sorted = [];

    /// <summary>
    /// Enumeration for tracking the resolution state of a mod during dependency resolution.
    /// </summary>
    enum ResolveState : byte
    {
        /// <summary>
        /// Mod has not been visited yet.
        /// </summary>
        NotVisited = 0,
        /// <summary>
        /// Mod is currently being visited (cycle detection).
        /// </summary>
        Visiting = 1,
        /// <summary>
        /// Mod and its dependencies have been successfully resolved.
        /// </summary>
        Resolved = 2,
        /// <summary>
        /// Mod failed to resolve due to missing or circular dependencies.
        /// </summary>
        Failed = 3
    }

    /// <summary>
    /// Resolves mods and their dependencies in topological order.
    /// </summary>
    /// <remarks>
    /// Performs depth-first search to detect circular dependencies and validate the dependency graph.
    /// Logs errors for each mod that fails to resolve.
    /// </remarks>
    /// <param name="discoveredMods">Dictionary of discovered mods keyed by ID.</param>
    /// <returns>A list of mods sorted in topological order, ready for loading.</returns>
    public List<ModEntry> Resolve(Dictionary<string, ModEntry> discoveredMods)
    {
        _discoveredMods = discoveredMods;
        EnsureClear();

        // Visit all discovered mods
        foreach (var modId in _discoveredMods.Keys)
        {
            Visit(modId);
        }

        return _sorted;
    }

    /// <summary>
    /// Clears internal state for a fresh resolution pass.
    /// </summary>
    private void EnsureClear()
    {
        _states.Clear();
        _sorted.Clear();
        _states.EnsureCapacity(_discoveredMods.Count);
        _sorted.EnsureCapacity(_discoveredMods.Count);
    }

    /// <summary>
    /// Performs a depth-first visit of a mod, resolving its dependencies.
    /// </summary>
    /// <remarks>
    /// Returns true if the mod and all its dependencies are successfully resolved.
    /// Logs specific error messages for different failure modes (circular, missing, cascade).
    /// </remarks>
    /// <param name="modId">The ID of the mod to visit.</param>
    /// <returns>True if the mod is resolved; false if it failed.</returns>
    private bool Visit(string modId)
    {
        // Check current state
        if (_states.TryGetValue(modId, out var state))
        {
            if (state == ResolveState.Resolved) return true;  // Already resolved
            if (state == ResolveState.Failed) return false;    // Already failed
            if (state == ResolveState.Visiting)                // Circular dependency detected
            {
                _logger.LogError("Mod '{Id}' failed to load: Involved in a CIRCULAR dependency.", modId);
                return false;
            }
        }

        // Check if mod exists in discovered mods
        if (!_discoveredMods.TryGetValue(modId, out var package))
        {
            // Check if it's already loaded (e.g., from another source)
            if (_registry.IsLoaded(modId))
            {
                _states[modId] = ResolveState.Resolved;
                return true;
            }

            // Mod not found anywhere
            _logger.LogError("Mod failed to load: MISSING dependency '{Id}'.", modId);
            _states[modId] = ResolveState.Failed;
            return false;
        }

        // Mark as visiting to detect cycles
        _states[modId] = ResolveState.Visiting;

        // Recursively resolve dependencies
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

        // Mark as resolved and add to sorted list
        _states[modId] = ResolveState.Resolved;
        _sorted.Add(package);
        return true;
    }
}

/// <summary>
/// Custom AssemblyLoadContext for loading mod assemblies in isolation.
/// </summary>
/// <remarks>
/// Allows mods to have their own dependency versions without conflicting with the game's versions.
/// </remarks>
public class ModAssemblyLoadContext(string modAssemblyDll) : AssemblyLoadContext(isCollectible: true)
{
    private readonly AssemblyDependencyResolver _resolver = new(modAssemblyDll);

    /// <summary>
    /// Loads an assembly by name, preferring the default context's assemblies when available.
    /// </summary>
    /// <remarks>
    /// First attempts to load from the default AssemblyLoadContext to ensure framework and
    /// shared assemblies are used consistently. Falls back to resolver if needed.
    /// </remarks>
    protected override Assembly? Load(AssemblyName assemblyName)
    {
        try
        {
            // Prefer loading from default context (framework, shared assemblies)
            var defaultAssembly = Default.LoadFromAssemblyName(assemblyName);
            if (defaultAssembly != null) return defaultAssembly;
        }
        catch 
        {
            // Fall through to resolver
        }

        // Resolve using the mod's dependency resolver
        string? assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath != null)
        {
            return LoadFromAssemblyPath(assemblyPath);
        }

        return null;
    }
}

/// <summary>
/// Startup service that initializes all registered mods.
/// </summary>
internal sealed class ModStartupService(IEnumerable<IModStartup> startups, IServiceProvider services) 
    : IStartupService, ICleanupService
{
    /// <summary>
    /// Gets the order in which this startup service runs (later in the sequence).
    /// </summary>
    public int Order => 1000;

    /// <summary>
    /// Performs cleanup for all mods when the application is shutting down.
    /// </summary>
    public void Cleanup()
    {
        // Dispose mods that implement IDisposable
        foreach(var disposable in startups.OfType<IDisposable>())
            disposable.Dispose();
    }

    /// <summary>
    /// Calls Startup on all registered mods.
    /// </summary>
    public void Startup()
    {
        // Run startup for each mod
        foreach(var mod in startups)
            mod.Startup(services);
    }
}
