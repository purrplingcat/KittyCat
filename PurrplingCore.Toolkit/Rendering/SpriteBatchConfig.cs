using Microsoft.Xna.Framework.Graphics;

namespace PurrplingCore.Toolkit.Rendering;

public readonly record struct SpriteBatchConfig(
    SpriteSortMode SortMode = SpriteSortMode.Deferred, 
    BlendState? BlendState = null, 
    SamplerState? SamplerState = null, 
    RasterizerState? RasterizerState = null,
    Effect? Effect = null
);
