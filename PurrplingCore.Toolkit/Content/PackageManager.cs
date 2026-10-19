using System.Diagnostics.CodeAnalysis;

namespace PurrplingCore.Toolkit.Content;

/// <summary>
/// Owns logical package mount points without mounting archives or importing asset catalogs.
/// </summary>
public sealed class PackageManager
{
    private readonly Dictionary<Name, string> _mounts = [];
    private readonly object _syncRoot = new();

    /// <summary>
    /// Registers or replaces a package mount point. Content paths address the already mounted VFS.
    /// </summary>
    /// <remarks>
    /// Unload affected assets before changing a mount point used by loaded content.
    /// </remarks>
    public void RegisterMountPoint(Name packageName, string contentPath)
    {
        if (packageName.IsNone)
        {
            throw new ArgumentException("Package name cannot be empty or whitespace.", nameof(packageName));
        }

        var path = NormalizePath(contentPath);
        lock (_syncRoot)
        {
            _mounts[packageName] = path;
        }
    }

    /// <summary>
    /// Removes a logical package's root without unmounting any VFS containers.
    /// </summary>
    public bool UnregisterMountPoint(Name packageName)
    {
        lock (_syncRoot)
        {
            return _mounts.Remove(packageName);
        }
    }

    /// <summary>
    /// Gets the package-wide root, or null when no package-wide mount exists.
    /// </summary>
    public string? Resolve(Name packageName)
    {
        lock (_syncRoot)
        {
            return _mounts.TryGetValue(packageName, out var path) ? path : null;
        }
    }

    internal static string NormalizePath(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        var normalized = path.Replace('\\', '/').Trim('/');
        if (normalized.Length == 0 || normalized == ".")
        {
            return string.Empty;
        }

        if (normalized.Contains(':') || normalized.Split('/').Any(segment => segment is "" or "." or ".."))
        {
            throw new ArgumentException("Package paths must be non-traversing VFS paths.", nameof(path));
        }

        return normalized;
    }

}