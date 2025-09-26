using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PurrplingCore.Toolkit.Rendering;

public readonly struct RenderContext(SpriteBatch spriteBatch, GraphicsDevice device, ICamera camera, GameTime gameTime)
{
    public readonly SpriteBatch SpriteBatch = spriteBatch;
    public readonly GraphicsDevice Device = device;
    public readonly ICamera Camera = camera;
    public readonly GameTime GameTime = gameTime;
}
