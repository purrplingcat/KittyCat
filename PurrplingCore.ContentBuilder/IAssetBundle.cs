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

public abstract class AssetBundle(string name, AssetBundleType type) : IAssetBundle
{
    private IContentCollection? _content;

    protected AssetBundle(AssetBundleType type) : this(string.Empty, type)
    {
        Name = this.GetDisplayName();
    }

    public virtual string Name { get; } = name;
    public virtual string PackageName { get; protected set; } = name;
    public AssetBundleType Type { get; } = type;
    public virtual string Source { get; protected set; } = ".";
    public virtual bool GenerateAssetCatalog { get; protected set; } = true;
    public virtual string Destination { get; protected set; } = type switch
    {
        AssetBundleType.Loose => ".",
        AssetBundleType.Pak => "Paks",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
    };

    protected abstract IContentCollection CreateContentCollection();

    public IContentCollection GetContent()
    {
        return _content ??= CreateContentCollection();
    }
}
