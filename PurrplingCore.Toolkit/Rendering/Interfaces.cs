using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PurrplingCore.Toolkit.Rendering;

public interface IRenderPass
{
    void Draw(GameTime gameTime);
}

public interface IPrepareRender
{
    void Prepare(GameTime gameTime);
}

public interface IRenderFeature2D
{
    void Draw(SpriteRenderBatch batch, GameTime gameTime);
}

public interface IInitializeRender
{
    void Initialize();
}
