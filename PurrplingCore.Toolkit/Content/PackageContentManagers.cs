using Microsoft.Xna.Framework.Content;

namespace PurrplingCore.Toolkit.Content;

internal sealed class PackageContentManagers(IContentManagerProvider provider, PackageManager packages) : IDisposable
{
    private readonly Dictionary<string, ContentManager> _managers = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _syncRoot = new();

    public ContentManager Get(string packageName)
    {
        lock (_syncRoot)
        {
            if (_managers.TryGetValue(packageName, out var manager))
            {
                return manager;
            }

            var root = packages.Resolve(packageName);
            manager = provider.CreateContentManager(root);
            _managers[packageName] = manager;

            return manager;
        }
    }

    public void Release(string packageName)
    {
        lock (_syncRoot)
        {
            if (_managers.TryGetValue(packageName, out var manager))
            {
                _managers.Remove(packageName);
                manager.Dispose();
            }
        }
    }

    public void Dispose()
    {
        lock (_syncRoot)
        {
            foreach (var manager in _managers.Values)
            {
                manager.Dispose();
            }

            _managers.Clear();
        }
    }
}
