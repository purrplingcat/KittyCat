using PurrplingCore.Toolkit.Modding;
using Zio;
using Zio.FileSystems;

namespace PurrplingCore.Toolkit.Vfs.FileSystems;

public class PrefixedFileSystem : ComposeFileSystem
{
    private readonly UPath _prefix;
    private readonly IFileSystem _inner;

    public UPath Prefix => _prefix;
    public IFileSystem Inner => _inner;

    public PrefixedFileSystem(IFileSystem innerFileSystem, UPath prefix, bool owned = true)
        : base(CreateInternalMount(innerFileSystem, prefix), owned)
    {
        _inner = innerFileSystem;
        _prefix = prefix.ToAbsolute();
    }

    private static IFileSystem CreateInternalMount(IFileSystem fs, UPath prefix)
    {
        ArgumentNullException.ThrowIfNull(fs);

        var mountFs = new MountFileSystem();
        mountFs.Mount(prefix.ToAbsolute(), fs);
        return mountFs;
    }

    protected override UPath ConvertPathToDelegate(UPath path) => path;

    protected override UPath ConvertPathFromDelegate(UPath path) => path;
}
