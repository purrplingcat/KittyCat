using MonoGame.Framework.Content.Pipeline.Builder;
using PurrplingCore.Toolkit.Metadata;

namespace PurrplingCore.ContentBuilder;

public interface IAssetBundle
{
    public string Name { get; }
    public AssetBundleType Type { get; }
    public string Source { get; }
    public string Destination { get; }

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
    public AssetBundleType Type { get; } = type;
    public virtual string Source { get; protected set; } = ".";
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
