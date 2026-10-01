namespace PurrplingCore.Toolkit.Rendering;

/// <summary>
/// Default pass that replays a layer's queued sprite and text commands through the batcher.
/// </summary>
/// <remarks>
/// The whole layer is drawn inside a single batch, so graphics state changes once per layer
/// rather than once per command.
/// </remarks>
public sealed class SpriteLayerPass : RenderLayerPass
{
    public override void Execute(in RenderFrameContext context)
    {
        var commands = context.Layer.Commands;
        var count = commands.Count;
        if (count == 0) return;

        var batcher = context.Batcher;
        var resources = context.Resources;
        var text = context.Text;

        batcher.Begin(context.State);
        for (var index = 0; index < count; index++)
        {
            batcher.Submit(commands.At(index), resources, text);
        }

        batcher.End();
    }
}
