using Android.Content.Res;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using Microsoft.Xna.Framework;
using System.Collections;

namespace KittyCat.Android;

/// <summary>
/// Exposes files bundled in an Android APK through the standard .NET file-provider abstraction.
/// </summary>
/// <remarks>
/// Android assets are not operating-system files. Directory enumeration uses
/// <see cref="AssetManager.List(string)"/>, while reads use MonoGame's
/// <see cref="TitleContainer"/> so callers receive the same content stream semantics as the game.
/// </remarks>
internal sealed class AndroidAssetFileProvider(AssetManager assets) : IFileProvider
{
    public IDirectoryContents GetDirectoryContents(string subpath)
    {
        var path = Normalize(subpath);
        var names = assets.List(path) ?? [];
        return names.Length == 0
            ? NotFoundDirectoryContents.Singleton
            : new AssetDirectoryContents(path, names);
    }

    public IFileInfo GetFileInfo(string subpath)
    {
        var path = Normalize(subpath);
        try
        {
            using var stream = TitleContainer.OpenStream(path);
            return new AssetFileInfo(path, exists: true);
        }
        catch (FileNotFoundException)
        {
            return new NotFoundFileInfo(path);
        }
    }

    public IChangeToken Watch(string filter) => NullChangeToken.Singleton;

    private static string Normalize(string path) => path.Replace('\\', '/').Trim('/');

    private sealed class AssetDirectoryContents(string path, string[] names) : IDirectoryContents
    {
        public bool Exists => true;

        public IEnumerator<IFileInfo> GetEnumerator() => names
            .Select(name => (IFileInfo)new AssetFileInfo($"{path}/{name}", exists: true))
            .GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class AssetFileInfo(string path, bool exists) : IFileInfo
    {
        public bool Exists => exists;
        public long Length => -1;
        public string? PhysicalPath => null;
        public string Name => path[(path.LastIndexOf('/') + 1)..];
        public DateTimeOffset LastModified => DateTimeOffset.UnixEpoch;
        public bool IsDirectory => false;

        public Stream CreateReadStream() => TitleContainer.OpenStream(path);
    }
}