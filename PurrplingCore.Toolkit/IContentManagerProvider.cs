using Microsoft.Xna.Framework.Content;

namespace PurrplingCore.Toolkit;

public interface IContentManagerProvider
{
    public ContentManager Default { get; }
    public ContentManager CreateContentManager();
}
