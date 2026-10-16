using System.Reflection;
using MonoGame.Framework.Content.Pipeline.Builder;
using PurrplingCore.Toolkit.Metadata;

namespace PurrplingCore.ContentBuilder;

public interface IAssetBundle
{
    public string Name { get; }
    public string PackageName { get; }
    public AssetBundleType Type { get; }
    public string Source { get; }
    public string Destination { get; }

    /// <summary>
    /// Gets whether the builder emits a binary catalog from this bundle's source metadata.
    /// Disable this for content-only overlays.
    /// </summary>
    public bool GenerateAssetCatalog { get; }

    public IContentCollection GetContent();
}

public abstract class AssetBundle : IAssetBundle
{
    private IContentCollection? _content;

    public AssetBundle()
    {
        var attr = GetAssetBundleAttribute();
        Name = attr.Name;
        PackageName = attr.PackageName;
        Type = attr.Type;
        Destination = ResolveDefaultDestination(attr.Type);
    }

    protected AssetBundle(string name, AssetBundleType type)
    {
        Name = name;
        Type = type;
        PackageName = name;
        Destination = ResolveDefaultDestination(type);
    }

    private string ResolveDefaultDestination(AssetBundleType type)
    {
        return type switch
        {
            AssetBundleType.Loose => ".",
            AssetBundleType.Pak => "Paks",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
        };
    }

    private AssetBundleAttribute GetAssetBundleAttribute()
    {
        var type = GetType();
        var attr = type.GetCustomAttribute<AssetBundleAttribute>();

        return attr ?? new AssetBundleAttribute(type.Name);
    }

    public virtual string Name { get; }
    public virtual string PackageName { get; protected set; }
    public AssetBundleType Type { get; }
    public virtual string Source { get; protected set; } = ".";
    public virtual bool GenerateAssetCatalog { get; protected set; } = true;
    public virtual string Destination { get; protected set; }

    protected abstract IContentCollection CreateContentCollection();

    public IContentCollection GetContent()
    {
        return _content ??= CreateContentCollection();
    }
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class AssetBundleAttribute : Attribute
{
    public AssetBundleAttribute(string name)
    {
        Name = name;
        PackageName = name;
    }

    public string Name { get; }

    public string PackageName { get; set; }

    public AssetBundleType Type { get; set; }
}
