using Friflo.Engine.ECS;
using Microsoft.Xna.Framework;

namespace KittyCat.Extensions;

public static class GameTimeExtensions
{
    public static UpdateTick ToUpdateTick(this GameTime time)
    {
        return new UpdateTick(
            (float)time.ElapsedGameTime.TotalSeconds,
            (float)time.TotalGameTime.TotalSeconds
        );
    }
}
