using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Xna.Framework.Content;
using PurrplingCore.Toolkit.Content;

namespace PurrplingCore.Toolkit;

/// <summary>
/// Provides access to the default content manager
/// and allows creating new content managers with specified roots.
/// </summary>
public interface IContentManagerProvider
{
    /// <summary>
    /// Gets the default content manager.
    /// </summary>
    public ContentManager ContentManager { get; }

    /// <summary>
    /// Creates a new content manager with the specified root.
    /// </summary>
    /// <param name="root">The root directory for the content manager. If null, the default root is used.</param>
    /// <returns>A new instance of <see cref="ContentManager"/>.</returns>
    public ContentManager CreateContentManager(string? root = null);
}

/// <summary>
/// Maps package names and object-path prefixes to content roots without depending on catalogs.
/// </summary>
/// <remarks>
/// Package names are case-insensitive; object-path prefixes use ordinal, case-sensitive matching.
/// Paths and prefixes must already use the caller's normalized path format.
/// </remarks>
public class ContentPathResolver
{
    /// <summary>
    /// Stores a prefix and its root as a value within the package's mapping list.
    /// </summary>
    private record struct PathMapping(string ObjectPathPrefix, string Path);

    private readonly Dictionary<string, string> _roots = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<PathMapping>> _mappings = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Sets or replaces the fallback root for a package.
    /// </summary>
    /// <param name="packageName">The case-insensitive package name.</param>
    /// <param name="path">The root to prepend to the asset's complete object path.</param>
    public void Map(string packageName, string path) => _roots[packageName] = path;

    /// <summary>
    /// Maps a package's object-path prefix to a root independently of the mapping's source.
    /// </summary>
    /// <param name="packageName">The case-insensitive package name.</param>
    /// <param name="path">The root to prepend to the asset's complete object path; may be empty.</param>
    /// <param name="objectPathPrefix">An ordinal prefix using forward slashes, or empty to match every path.</param>
    /// <remarks>
    /// Prefixes match complete path segments and are not removed from the asset's object path.
    /// Registering an existing prefix leaves its original root unchanged, even if the new root differs.
    /// </remarks>
    public void Map(string packageName, string path, string objectPathPrefix)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packageName);
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(objectPathPrefix);

        if (!_mappings.TryGetValue(packageName, out var mappings))
        {
            mappings = [];
            _mappings.Add(packageName, mappings);
        }

        // Keep the first registration for a prefix rather than retaining duplicate mappings.
        foreach (var mapping in mappings)
        {
            if (mapping.ObjectPathPrefix == objectPathPrefix)
            {
                return;
            }
        }

        mappings.Add(new PathMapping(objectPathPrefix, path));
    }

    /// <summary>
    /// Removes both the fallback root and every prefix mapping for a package.
    /// </summary>
    /// <param name="packageName">The case-insensitive package name.</param>
    /// <returns>True when at least one mapping was removed.</returns>
    public bool Unmap(string packageName)
    {
        var removed = _roots.Remove(packageName);
        return _mappings.Remove(packageName) || removed;
    }

    /// <summary>
    /// Gets the package's fallback root without considering prefix mappings.
    /// </summary>
    /// <param name="packageName">The case-insensitive package name.</param>
    /// <returns>The fallback root, or null when it is not registered.</returns>
    public string? Resolve(string packageName) => _roots.TryGetValue(packageName, out var path) ? path : null;

    /// <summary>
    /// Resolves the most specific path-prefix mapping before falling back to the package root.
    /// </summary>
    /// <param name="packageName">The case-insensitive package name.</param>
    /// <param name="objectPath">The normalized, forward-slash object path to match.</param>
    /// <returns>The matching root only, or null when neither a prefix nor a fallback root matches.</returns>
    /// <remarks>
    /// An empty root is a valid result and must not be treated as a missing mapping.
    /// </remarks>
    public string? Resolve(string packageName, string objectPath)
    {
        string? matchedPath = null;
        // An empty prefix has length zero and must still be eligible as the first match.
        var matchedPrefixLength = -1;
        if (_mappings.TryGetValue(packageName, out var mappings))
        {
            foreach (var mapping in mappings)
            {
                var prefix = mapping.ObjectPathPrefix;
                if (prefix.Length <= matchedPrefixLength)
                {
                    continue;
                }

                if (!objectPath.StartsWith(prefix, StringComparison.Ordinal))
                {
                    continue;
                }

                // Match "Textures" and "Textures/Fur", but not "TexturesExtra/Fur".
                if (prefix.Length > 0 &&
                    objectPath.Length > prefix.Length &&
                    objectPath[prefix.Length] != '/')
                {
                    continue;
                }

                matchedPath = mapping.Path;
                matchedPrefixLength = prefix.Length;
            }
        }

        return matchedPath ?? Resolve(packageName);
    }
    
    /// <summary>
    /// Tries to get the package's fallback root without considering prefix mappings.
    /// </summary>
    /// <param name="packageName">The case-insensitive package name.</param>
    /// <param name="path">The registered root when found; otherwise null.</param>
    /// <returns>True when a fallback root is registered, including an empty root.</returns>
    public bool TryResolve(string packageName, [MaybeNullWhen(false)] out string path) => _roots.TryGetValue(packageName, out path);
}

/// <summary>
/// Creates content managers using the configured root and optional package-root aliases.
/// </summary>
public sealed class DefaultContentManagerProvider : IContentManagerProvider
{
    private readonly IServiceProvider _services;
    private readonly ContentPathResolver _resolver;
    private readonly ContentManagerOptions _options;

    /// <summary>
    /// Gets the content manager created with the configured default root.
    /// </summary>
    public ContentManager ContentManager { get; }

    /// <summary>
    /// Creates the provider and its default content manager from the configured services and root.
    /// </summary>
    public DefaultContentManagerProvider(IServiceProvider services, ContentPathResolver resolver, IOptions<ContentManagerOptions> options)
    {
        _services = services;
        _options = options.Value;
        _resolver = resolver;
        ContentManager = CreateContentManager();
    }

    /// <summary>
    /// Creates a content manager, resolving a package-root alias when one matches the requested root.
    /// </summary>
    /// <param name="root">The requested root or package alias, or null to use the configured default.</param>
    /// <returns>A new content manager; object-path prefix mappings are not consulted.</returns>
    public ContentManager CreateContentManager(string? root = null)
    {
        var loggerFactory = _services.GetService<ILoggerFactory>();
        var logger = loggerFactory?.CreateLogger("ContentManager") ?? NullLogger.Instance;
        root ??= _options.ContentRoot;

        if (_resolver.TryResolve(root, out var resolvedRoot))
        {
            root = resolvedRoot;
        }

        return new DefaultContentManager(_services, root, logger);
    }
}
