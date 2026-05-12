using Microsoft.Xna.Framework;
using Zio;
using Zio.FileSystems;

namespace PurrplingCore.Toolkit.Content;

public record FileSystemLayer(IFileSystem FileSystem, int Order);

public interface IAggregateFileSystem : IFileSystem
{
    IEnumerable<IFileSystem> GetFileSystems();
}

internal class FileSystemManager : AggregateFileSystem, IAggregateFileSystem
{
    public FileSystemManager(IEnumerable<FileSystemLayer> entries) : base(owned: true)
    {
        SetFileSystems(
            entries
                .OrderBy(entry => entry.Order)
                .Select(entry => entry.FileSystem)
        );
    }

    IEnumerable<IFileSystem> IAggregateFileSystem.GetFileSystems()
    {
        return GetFileSystems();
    }
}
