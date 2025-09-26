using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;

namespace PurrplingCore.Toolkit.Content;

public static class ContentReaderExtensions
{
    private static readonly FieldInfo? _contentReaderGraphicsDeviceFieldInfo = typeof(ContentReader).GetTypeInfo().GetDeclaredField("graphicsDevice");

    public static GraphicsDevice GetGraphicsDevice(this ContentReader contentReader)
    {
        var graphics = _contentReaderGraphicsDeviceFieldInfo?.GetValue(contentReader);

        if (graphics is null)
            throw new InvalidOperationException("Unable to resolve graphics device");

        return (GraphicsDevice)graphics;
    }

    public static string RemoveExtension(string path)
    {
        return Path.ChangeExtension(path, null).TrimEnd('.');
    }

    public static string GetRelativeAssetName(this ContentReader contentReader, string relativeName)
    {
        var assetDirectory = Path.GetDirectoryName(contentReader.AssetName) ?? "";
        var assetName = RemoveExtension(Path.Combine(assetDirectory, relativeName).Replace('\\', '/'));

        return ShortenRelativePath(assetName);
    }

    public static string ShortenRelativePath(string relativePath)
    {
        var ellipseIndex = relativePath.IndexOf("/../", StringComparison.Ordinal);
        while (ellipseIndex != -1)
        {
            var lastDirectoryIndex = relativePath.LastIndexOf('/', ellipseIndex - 1) + 1;
            relativePath = relativePath.Remove(lastDirectoryIndex, ellipseIndex + 4 - lastDirectoryIndex);
            ellipseIndex = relativePath.IndexOf("/../", StringComparison.Ordinal);
        }

        return relativePath;
    }

    public static Texture2D ReadTexture2D(this ContentReader reader)
    {
        bool embedded = reader.ReadBoolean();
        string textureName = reader.ReadString();
        Texture2D texture = embedded 
            ? reader.ReadRawObject<Texture2D>() 
            : reader.ContentManager.Load<Texture2D>(reader.GetRelativeAssetName(textureName));

        texture.Name = string.IsNullOrEmpty(textureName)
            ? reader.AssetName
            : reader.GetRelativeAssetName(textureName);

        return texture;
    }

    public static Rectangle ReadRectangle(this ContentReader reader)
    {
        int x = reader.ReadInt32();
        int y = reader.ReadInt32();
        int width = reader.ReadInt32();
        int height = reader.ReadInt32();

        return new Rectangle(x, y, width, height);
    }
}