using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using Microsoft.Xna.Framework;

namespace PurrplingCore.Toolkit.Hosting;

public class TitleContainerFileInfo(string subpath) : IFileInfo
{
    private readonly string _subpath = subpath.TrimStart('/', '\\');

    public bool Exists
    {
        get
        {
            try
            {
                using var stream = TitleContainer.OpenStream(_subpath);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    public long Length => -1;
    public string? PhysicalPath => null;
    public DateTimeOffset LastModified => DateTimeOffset.UnixEpoch;

    public string Name => Path.GetFileName(_subpath);
    public bool IsDirectory => false;

    public Stream CreateReadStream()
    {
        return TitleContainer.OpenStream(_subpath);
    }
}

public class TitleContainerFileProvider : IFileProvider
{
    public IFileInfo GetFileInfo(string subpath)
    {
        return new TitleContainerFileInfo(subpath);
    }

    public IDirectoryContents GetDirectoryContents(string subpath)
    {
        // TitleContainer neumí enumerovat složky. Konfigurace to naštěstí nevyžaduje.
        return NotFoundDirectoryContents.Singleton;
    }

    public IChangeToken Watch(string filter)
    {
        // Zde říkáme konfiguračnímu builderu: "Nic nesleduj, nikdy se to nezmění."
        // (Proto pak musíš v AddJsonFile() mít reloadOnChange: false)
        return NullChangeToken.Singleton;
    }
}