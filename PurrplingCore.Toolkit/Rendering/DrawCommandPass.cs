using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PurrplingCore.Toolkit.Rendering;

/// <summary>
/// Executes a layer's ordered sprite, text, and primitive draw commands.
/// </summary>
public class DrawCommandPass : RenderLayerPass, ILayerSubmissionPass<DrawCommand>
{
    /// <summary>
    /// Creates the pass-owned command queue with the requested ordering and initial capacity.
    /// </summary>
    public DrawCommandPass(RenderSortMode sortMode = RenderSortMode.Submission, int initialCapacity = 256)
    {
        Commands = new DrawList(sortMode, initialCapacity);
    }

    /// <summary>
    /// Commands owned by this pass for the current frame.
    /// </summary>
    public DrawList Commands { get; }

    public virtual int SubmissionCount => Commands.Count;

    public void Submit(in DrawCommand submission) => Commands.Add(submission, submission.ResourceIndex);

    public virtual void ClearSubmissions() => Commands.Clear();

    public override void Prepare(in RenderFrameContext context) => Commands.Sort();

    public override void Execute(in RenderFrameContext context)
    {
        var commands = Commands;
        var count = commands.Count;
        if (count == 0) return;

        var batcher = context.Batcher;
        var resources = context.Resources;
        var text = context.Text;
        var batchOpen = false;

        for (var index = 0; index < count; index++)
        {
            ref readonly var command = ref commands.At(index);
            if (command.Kind == DrawCommandKind.Primitive)
            {
                if (batchOpen)
                {
                    batcher.End();
                    batchOpen = false;
                }

                DrawPrimitive(context, command, resources);
                continue;
            }

            if (!batchOpen)
            {
                batcher.Begin(context.State);
                batchOpen = true;
            }

            batcher.Submit(command, resources, text);
        }

        if (batchOpen) batcher.End();
    }

    private static void DrawPrimitive(
        in RenderFrameContext context,
        in DrawCommand command,
        RenderResourceTable resources)
    {
        var effect = context.State.Effect;
        if (effect is null)
        {
            throw new InvalidOperationException("Primitive draw commands require an effect on their render layer.");
        }

        var matrices = effect as IEffectMatrices;
        if (matrices is null)
        {
            throw new InvalidOperationException("Primitive draw commands require an effect that implements IEffectMatrices.");
        }

        matrices.World = CreateWorldTransform(command);
        matrices.View = context.State.Transform;

        var device = context.Device;
        device.BlendState = context.State.BlendState ?? BlendState.AlphaBlend;
        device.SamplerStates[0] = context.State.SamplerState ?? SamplerState.LinearClamp;
        device.DepthStencilState = context.State.DepthStencilState ?? DepthStencilState.None;
        device.RasterizerState = context.State.RasterizerState ?? RasterizerState.CullCounterClockwise;
        device.SetVertexBuffer(resources.GetVertexBuffer(command.ResourceIndex));

        foreach (var pass in effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            device.DrawPrimitives(command.PrimitiveType, command.VertexStart, command.PrimitiveCount);
        }
    }

    private static Matrix CreateWorldTransform(in DrawCommand command)
        => Matrix.CreateTranslation(new Vector3(-command.Origin, 0f))
            * Matrix.CreateScale(command.Scale.X, command.Scale.Y, 1f)
            * Matrix.CreateRotationZ(command.Rotation)
            * Matrix.CreateTranslation(new Vector3(command.Position, 0f));
}