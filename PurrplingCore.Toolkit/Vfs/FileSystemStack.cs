using PurrplingCore.Toolkit.DI;
using Zio;
using Zio.FileSystems;

namespace PurrplingCore.Toolkit.Vfs;

public class FileSystemStack()
{
    private readonly AggregateFileSystem _aggregate = new();
    private readonly List<FileSystemLayer> _layers = [];
    private readonly object _syncRoot = new();

    public IFileSystem FileSystem => _aggregate;
    public IReadOnlyList<FileSystemLayer> Layers => _layers;

    public FileSystemStack(IEnumerable<FileSystemLayer> layers) : this()
    {
        _layers = [.. layers];
        Rebuild();
    }

    public void AddLayers(IEnumerable<FileSystemLayer> newLayers)
    {
        lock (_syncRoot)
        {
            _layers.AddRange(newLayers);
            Rebuild();
        }
    }

    public void AddLayer(IFileSystem fs, int order)
    {
        lock (_syncRoot)
        {
            _layers.Add(new FileSystemLayer(fs, order));
            Rebuild();
        }
    }

    internal IReadOnlyList<IFileSystem> GetFileSystems()
    {
        lock (_syncRoot)
        {
            return _layers.OrderBy(layer => layer.Order)
                          .Select(layer => layer.FileSystem)
                          .ToArray();
        }
    }

    public bool RemoveLayers(IEnumerable<FileSystemLayer> toRemove)
    {
        lock (_syncRoot)
        {
            bool removed = false;

            foreach (FileSystemLayer layer in toRemove)
            {
                removed |= _layers.Remove(layer);
            }

            if (removed) Rebuild();

            return removed;
        }
    }

    public bool RemoveLayer(IFileSystem fs)
    {
        lock (_syncRoot)
        {
            var layer = _layers.FirstOrDefault(l => l.FileSystem == fs);
            if (layer != null)
            {
                _layers.Remove(layer);
                Rebuild();
                return true;
            }

            return false;
        }
    }

    public void ApplyChanges(Action<List<FileSystemLayer>> action)
    {
        lock (_syncRoot)
        {
            action(_layers);
            Rebuild();
        }
    }

    public void Clear()
    {
        lock (_syncRoot)
        {
            _layers.Clear();
            _aggregate.ClearFileSystems();
        }
    }

    private void Rebuild()
    {
        var orderedLayers = _layers.OrderBy(layer => layer.Order).ToArray();

        _aggregate.ClearFileSystems();
        foreach (var layer in orderedLayers)
        {
            _aggregate.AddFileSystem(layer.FileSystem);
        }
    }
}
