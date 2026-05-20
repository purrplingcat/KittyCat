using PurrplingCore.Toolkit.DI;
using Zio;
using Zio.FileSystems;

namespace PurrplingCore.Toolkit.Vfs;

public class FileSystemStack()
{
    private readonly AggregateFileSystem _aggregate = new();
    private readonly List<FileSystemLayer> _layers = [];

    public IFileSystem FileSystem => _aggregate;
    public IReadOnlyList<FileSystemLayer> Layers => _layers;

    public FileSystemStack(IEnumerable<FileSystemLayer> layers) : this()
    {
        _layers = [.. layers];
        Rebuild();
    }

    public void AddLayers(IEnumerable<FileSystemLayer> newLayers)
    {
        _layers.AddRange(newLayers);
        Rebuild();
    }

    public void AddLayer(IFileSystem fs, int order)
    {
        _layers.Add(new FileSystemLayer(fs, order));
        Rebuild();
    }

    public bool RemoveLayers(IEnumerable<FileSystemLayer> toRemove)
    {
        bool removed = false;

        foreach (FileSystemLayer layer in toRemove)
        {
            removed |= _layers.Remove(layer);
        }

        if (removed) Rebuild();

        return removed;
    }

    public bool RemoveLayer(IFileSystem fs)
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

    public void ApplyChanges(Action<List<FileSystemLayer>> action)
    {
        action(_layers);
        Rebuild();
    }

    public void Clear()
    {
        _layers.Clear();
        _aggregate.ClearFileSystems();
    }

    private void Rebuild()
    {
        _layers.Sort((a, b) => a.Order.CompareTo(b.Order));

        _aggregate.ClearFileSystems();
        foreach (var layer in _layers)
        {
            _aggregate.AddFileSystem(layer.FileSystem);
        }
    }
}


