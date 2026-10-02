using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PurrplingCore.Toolkit.Rendering;

/// <summary>
/// Graphics state shared by every command inside one batch.
/// </summary>
/// <remarks>
/// A layer resolves this once per frame, so a batch changes state at most once instead of once
/// per sprite. Null members fall back to the graphics defaults chosen by the batcher.
/// </remarks>
public readonly struct BatchState
{
    public BatchState(
        Matrix transform,
        BlendState? blendState = null,
        SamplerState? samplerState = null,
        DepthStencilState? depthStencilState = null,
        RasterizerState? rasterizerState = null,
        Effect? effect = null)
    {
        Transform = transform;
        BlendState = blendState;
        SamplerState = samplerState;
        DepthStencilState = depthStencilState;
        RasterizerState = rasterizerState;
        Effect = effect;
    }

    /// <summary>
    /// Combined camera, parallax, and resolution transform applied to the batch.
    /// </summary>
    public Matrix Transform { get; }

    public BlendState? BlendState { get; }
    public SamplerState? SamplerState { get; }
    public DepthStencilState? DepthStencilState { get; }
    public RasterizerState? RasterizerState { get; }
    public Effect? Effect { get; }
}

/// <summary>
/// Replays queued <see cref="DrawCommand"/> values onto the graphics device.
/// </summary>
/// <remarks>
/// The abstraction exists so the default <see cref="SpriteBatch"/> implementation can later be
/// replaced by a vertex-buffer batcher without touching layer or submission code.
/// </remarks>
public interface ISpriteBatcher
{
    /// <summary>
    /// Opens a batch using <paramref name="state"/>.
    /// </summary>
    void Begin(in BatchState state);

    /// <summary>
    /// Emits a single command into the open batch.
    /// </summary>
    void Submit(in DrawCommand command, RenderResourceTable resources, FrameTextBuffer text);

    /// <summary>
    /// Closes the batch and submits it to the device.
    /// </summary>
    void End();
}
