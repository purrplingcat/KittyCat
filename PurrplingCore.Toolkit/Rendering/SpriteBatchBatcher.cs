using System.Text;
using Microsoft.Xna.Framework.Graphics;

namespace PurrplingCore.Toolkit.Rendering;

/// <summary>
/// Default <see cref="ISpriteBatcher"/> built on MonoGame's <see cref="SpriteBatch"/>.
/// </summary>
/// <remarks>
/// Always begins in <see cref="SpriteSortMode.Deferred"/>: the owning layer has already ordered
/// its commands, so letting SpriteBatch sort again would duplicate the work.
/// </remarks>
public sealed class SpriteBatchBatcher : ISpriteBatcher
{
    // MonoGame 3.8.5 has no ReadOnlySpan<char> DrawString overload; a reused builder avoids a string per command.
    private readonly StringBuilder _textScratch = new(256);
    private readonly SpriteBatch _spriteBatch;
    private bool _open;

    public SpriteBatchBatcher(SpriteBatch spriteBatch)
    {
        ArgumentNullException.ThrowIfNull(spriteBatch);
        _spriteBatch = spriteBatch;
    }

    public void Begin(in BatchState state)
    {
        _spriteBatch.Begin(
            SpriteSortMode.Deferred,
            state.BlendState,
            state.SamplerState,
            state.DepthStencilState,
            state.RasterizerState,
            state.Effect,
            state.Transform);

        _open = true;
    }

    public void Submit(in DrawCommand command, RenderResourceTable resources, FrameTextBuffer text)
    {
        if (command.Kind == DrawCommandKind.Text)
        {
            _textScratch.Clear();
            _textScratch.Append(text.Slice(command.TextOffset, command.TextLength));
            _spriteBatch.DrawString(
                resources.GetFont(command.ResourceIndex),
                _textScratch,
                command.Position,
                command.Color,
                command.Rotation,
                command.Origin,
                command.Scale,
                command.Effects,
                layerDepth: 0f);
            return;
        }

        _spriteBatch.Draw(
            resources.GetTexture(command.ResourceIndex),
            command.Position,
            command.Source,
            command.Color,
            command.Rotation,
            command.Origin,
            command.Scale,
            command.Effects,
            layerDepth: 0f);
    }

    public void End()
    {
        if (!_open) return;

        _open = false;
        _spriteBatch.End();
    }
}
