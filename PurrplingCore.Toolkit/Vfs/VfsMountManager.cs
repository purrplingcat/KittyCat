using Zio;
using Zio.FileSystems;

namespace PurrplingCore.Toolkit.Vfs;

/// <summary>
/// Adds exclusive virtual mounts and ordered overlay layers to the fixed VFS root.
/// </summary>
public interface IVfsMountManager
{
    event EventHandler<VfsChangedEventArgs>? Changed;

    void Mount(UPath target, IFileSystem fileSystem);
    void AddLayer(UPath target, IFileSystem fileSystem, int order);
    IReadOnlyList<IFileSystem> GetLayers(UPath target);
}

public sealed class VfsChangedEventArgs(UPath target, VfsChangeKind kind) : EventArgs
{
    public UPath Target { get; } = target;
    public VfsChangeKind Kind { get; } = kind;
}

public enum VfsChangeKind
{
    MountAdded,
    LayerAdded
}

internal sealed class VfsMountManager : IVfsMountManager
{
    private readonly VirtualFileSystemManager _manager;
    private readonly Dictionary<UPath, FileSystemStack> _overlays = [];
    private readonly HashSet<UPath> _exclusiveMounts = [];
    private readonly object _syncRoot = new();

    public event EventHandler<VfsChangedEventArgs>? Changed;

    public VfsMountManager(VirtualFileSystemManager manager)
    {
        _manager = manager;
    }

    public void Mount(UPath target, IFileSystem fileSystem)
    {
        ArgumentNullException.ThrowIfNull(fileSystem);
        var absoluteTarget = NormalizeTarget(target);

        lock (_syncRoot)
        {
            EnsureTargetIsAvailable(absoluteTarget);
            _manager.Root.Mount(absoluteTarget, fileSystem);
            _exclusiveMounts.Add(absoluteTarget);
        }

        Changed?.Invoke(this, new VfsChangedEventArgs(absoluteTarget, VfsChangeKind.MountAdded));
    }

    public void AddLayer(UPath target, IFileSystem fileSystem, int order)
    {
        ArgumentNullException.ThrowIfNull(fileSystem);
        var absoluteTarget = NormalizeTarget(target);

        lock (_syncRoot)
        {
            if (!_overlays.TryGetValue(absoluteTarget, out var stack))
            {
                if (_exclusiveMounts.Contains(absoluteTarget))
                {
                    throw new InvalidOperationException($"Mount target '{absoluteTarget}' is already exclusive.");
                }

                stack = new FileSystemStack();
                _manager.Root.Mount(absoluteTarget, stack.FileSystem);
                _overlays.Add(absoluteTarget, stack);
            }

            stack.AddLayer(fileSystem, order);
        }

        Changed?.Invoke(this, new VfsChangedEventArgs(absoluteTarget, VfsChangeKind.LayerAdded));
    }

    public IReadOnlyList<IFileSystem> GetLayers(UPath target)
    {
        var absoluteTarget = NormalizeTarget(target);

        lock (_syncRoot)
        {
            return _overlays.TryGetValue(absoluteTarget, out var stack)
                ? stack.GetFileSystems()
                : [];
        }
    }

    private void EnsureTargetIsAvailable(UPath target)
    {
        if (_exclusiveMounts.Contains(target) || _overlays.ContainsKey(target))
        {
            throw new InvalidOperationException($"Mount target '{target}' is already in use.");
        }
    }

    private static UPath NormalizeTarget(UPath target)
    {
        if (target.IsNull || target == UPath.Root)
        {
            throw new ArgumentException("A mount target must be a non-root path.", nameof(target));
        }

        return target.ToAbsolute();
    }
}