using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PurrplingCore.Toolkit.Graphics;
using PurrplingCore.Toolkit.Rendering;
using Xunit;

namespace PurrplingCore.Toolkit.Tests.Rendering;

public class RenderSubmissionTests
{
    private static readonly RenderLayerId Layer = RenderLayerId.Of("RenderSubmissionTests.Main");
    private static readonly RenderLayerId OtherLayer = RenderLayerId.Of("RenderSubmissionTests.Other");

    [Theory]
    [InlineData(RenderSortMode.Submission, 2, 3, 1)]
    [InlineData(RenderSortMode.FrontToBack, 3, 1, 2)]
    [InlineData(RenderSortMode.BackToFront, 2, 3, 1)]
    [InlineData(RenderSortMode.Texture, 1, 2, 3)]
    public void DrawPassOwnsAndOrdersItsSubmissions(RenderSortMode sortMode, int first, int second, int third)
    {
        using var pass = new DrawCommandPass(sortMode, initialCapacity: 1);
        pass.Submit(new DrawCommand { Kind = DrawCommandKind.Sprite, Depth = 0.75f, ResourceIndex = 2 });
        pass.Submit(new DrawCommand { Kind = DrawCommandKind.Sprite, Depth = 0.25f, ResourceIndex = 3 });
        pass.Submit(new DrawCommand { Kind = DrawCommandKind.Sprite, Depth = 0.25f, ResourceIndex = 1 });

        pass.Prepare(default);

        Assert.Equal(3, pass.SubmissionCount);
        Assert.Equal(first, pass.Commands.At(0).ResourceIndex);
        Assert.Equal(second, pass.Commands.At(1).ResourceIndex);
        Assert.Equal(third, pass.Commands.At(2).ResourceIndex);

        pass.ClearSubmissions();

        Assert.Equal(0, pass.SubmissionCount);
    }

    [Fact]
    public void DefaultAndTypedDrawBatchesShareThePassOwnedQueueAndReplayInPreparedOrder()
    {
        var pass = new DrawCommandPass(RenderSortMode.FrontToBack);
        using var renderer = CreateRenderer(pass);
        var first = new DrawCommand { Kind = DrawCommandKind.Sprite, Depth = 0.75f, ResourceIndex = 2 };
        var second = new DrawCommand { Kind = DrawCommandKind.Text, Depth = 0.25f, ResourceIndex = 1 };

        renderer.Context.Batch(Layer).Submit(in first);
        renderer.Context.Batch<DrawCommand>(Layer).Submit(in second);
        pass.Prepare(default);

        var batcher = new RecordingBatcher();
        using var layer = new RenderLayer(OtherLayer, new RenderLayerOptions());
        var context = new RenderFrameContext(
            null!, layer, batcher, renderer.Context.Resources, renderer.Context.TextBuffer, layer.CreateBatchState());

        pass.Execute(context);

        Assert.Equal(2, pass.SubmissionCount);
        Assert.Equal(new[] { second, first }, batcher.Commands);
        Assert.Equal(1, batcher.BeginCount);
        Assert.Equal(1, batcher.EndCount);
        Assert.False(batcher.IsBatchOpen);
    }

    [Fact]
    public void DrawWriterAcceptsAnyPassSupportingDrawCommands()
    {
        var pass = new RecordingPass<DrawCommand>();
        using var renderer = CreateRenderer(pass);
        var command = new DrawCommand { ResourceIndex = 7 };

        renderer.BeginFrame();
        var writer = renderer.Context.Batch(Layer);
        Assert.True(writer.IsActive);
        writer.Submit(in command);
        renderer.Context.Batch<DrawCommand>(Layer).Submit(in command);

        Assert.Equal(1, pass.ClearCount);
        Assert.Equal(new[] { command, command }, pass.Submissions);
    }

    [Fact]
    public void BatchExposesSharedResourcesAndTextForRawSubmissions()
    {
        var pass = new MixedPass();
        using var renderer = CreateRenderer(pass);
        var writer = renderer.Context.Batch(Layer);

        Assert.Same(renderer.Context.Resources, writer.Resources);
        Assert.Same(renderer.Context.TextBuffer, writer.Text);
        Assert.Null(writer.CullBounds);

        var command = new DrawCommand
        {
            Kind = DrawCommandKind.Text,
            TextOffset = writer.Text!.Append("Custom"),
            TextLength = 6,
        };
        writer.Submit(in command);
        var geometry = renderer.Context.Batch<GeometrySubmission>(Layer);

        Assert.Same(writer.Resources, geometry.Resources);
        Assert.Same(writer.Text, geometry.Text);
        Assert.Equal(command, pass.Commands.At(0));
        Assert.Equal("Custom", writer.Text.Slice(command.TextOffset, command.TextLength).ToString());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BatchExposesCullBoundsSampledWhenObtained(bool cullingEnabled)
    {
        var camera = new BoundsCamera { Bounds = new Rectangle(12, 24, 80, 50) };
        var options = new RendererOptions(new Screen(new Point(320, 180)))
        {
            Layers =
            {
                new(Layer, new RenderLayerOptions
                {
                    Pass = new RecordingPass<GeometrySubmission>(),
                    Camera = camera,
                    CullingEnabled = cullingEnabled,
                }),
            },
        };
        using var renderer = new Renderer2D("WriterCullBounds", options);
        var writer = renderer.Context.Batch<GeometrySubmission>(Layer);
        Rectangle? expected = cullingEnabled ? camera.Bounds : null;

        Assert.Equal(expected, writer.CullBounds);
        Assert.Equal(cullingEnabled, writer.IsCulled(new Rectangle(0, 0, 1, 1)));
        Assert.Equal(cullingEnabled, writer.IsCulled(Vector2.Zero, Vector2.Zero, Vector2.One, 1, 1));

        camera.Bounds = new Rectangle(100, 200, 80, 50);

        Assert.Equal(expected, writer.CullBounds);
    }

    [Theory]
    [InlineData(25, 25, 10, 10, false)]
    [InlineData(-11, 25, 10, 10, true)]
    [InlineData(-10, 25, 10, 10, false)]
    [InlineData(-5, 25, 10, 10, false)]
    [InlineData(101, 25, 10, 10, true)]
    [InlineData(100, 25, 10, 10, false)]
    [InlineData(25, -11, 10, 10, true)]
    [InlineData(25, -10, 10, 10, false)]
    [InlineData(25, 101, 10, 10, true)]
    [InlineData(25, 100, 10, 10, false)]
    [InlineData(95, 95, 10, 10, false)]
    [InlineData(50, 50, 0, 0, false)]
    public void PublicCullingKeepsIntersectingAndTouchingBounds(int x, int y, int width, int height, bool expected)
    {
        var visible = new Rectangle(0, 0, 100, 100);
        var bounds = new Rectangle(x, y, width, height);

        Assert.Equal(expected, Culling2D.IsCulled(visible, bounds));
    }

    [Theory]
    [InlineData(105f, 50f, 0f, 0f, -1f, 1f, false)]
    [InlineData(110f, 50f, 0f, 0f, -1f, 1f, false)]
    [InlineData(111f, 50f, 0f, 0f, -1f, 1f, true)]
    [InlineData(-1f, 50f, 0f, 0f, -1f, 1f, true)]
    [InlineData(50f, 105f, 0f, 0f, 1f, -1f, false)]
    [InlineData(50f, 111f, 0f, 0f, 1f, -1f, true)]
    [InlineData(105f, 105f, 0f, 0f, -1f, -1f, false)]
    [InlineData(105f, 50f, 5f, 5f, -2f, 2f, false)]
    public void PublicCullingHandlesOriginsAndMirroredScale(
        float x, float y, float originX, float originY, float scaleX, float scaleY, bool expected)
    {
        var visible = new Rectangle(0, 0, 100, 100);

        var actual = Culling2D.IsCulled(
            visible, new Vector2(x, y), new Vector2(originX, originY), new Vector2(scaleX, scaleY), 10, 10);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(false, 0f)]
    [InlineData(true, 0.5f)]
    [InlineData(true, -0.5f)]
    public void PublicCullingRemainsConservativeWithoutBoundsOrWithRotation(bool hasBounds, float rotation)
    {
        Rectangle? visible = hasBounds ? new Rectangle(0, 0, 100, 100) : null;

        Assert.False(Culling2D.IsCulled(visible, new Vector2(200), Vector2.Zero, Vector2.One, 10, 10, rotation));
        if (!hasBounds)
        {
            Assert.False(Culling2D.IsCulled(visible, new Rectangle(200, 200, 10, 10)));
        }
    }

    [Fact]
    public void CustomPassOwnsTypedSubmissionsAndClearsBeforeTheNextFrame()
    {
        var pass = new RecordingPass<GeometrySubmission>();
        using var renderer = CreateRenderer(pass);
        var first = new GeometrySubmission(3, 5);
        var second = new GeometrySubmission(9, 2);

        renderer.BeginFrame();
        renderer.Context.Batch<GeometrySubmission>(Layer).Submit(in first);
        renderer.Context.Batch<GeometrySubmission>(Layer).Submit(in second);

        Assert.Equal(1, pass.ClearCount);
        Assert.Equal(new[] { first, second }, pass.Submissions);

        renderer.BeginFrame();

        Assert.Equal(2, pass.ClearCount);
        Assert.Empty(pass.Submissions);
        renderer.Context.Batch<GeometrySubmission>(Layer).Submit(in second);
        Assert.Equal(2, pass.ClearCount);
        Assert.Equal(second, Assert.Single(pass.Submissions));
    }

    [Fact]
    public void OnePassAcceptsMultipleSubmissionTypesWithOneFrameReset()
    {
        var pass = new MixedPass();
        using var renderer = CreateRenderer(pass);
        var command = new DrawCommand { ResourceIndex = 7 };
        var geometry = new GeometrySubmission(3, 5);

        renderer.BeginFrame();
        var commands = renderer.Context.Batch(Layer, required: true);
        var geometries = renderer.Context.Batch<GeometrySubmission>(Layer, required: true);
        Assert.True(commands.IsActive);
        Assert.True(geometries.IsActive);
        commands.Submit(in command);
        geometries.Submit(in geometry);

        Assert.Equal(1, pass.ClearCount);
        Assert.Equal(1, pass.Commands.Count);
        Assert.Equal(command, pass.Commands.At(0));
        Assert.Equal(geometry, Assert.Single(pass.Geometry));
        Assert.Equal(2, ((IFrameSubmissionPass)pass).SubmissionCount);

        renderer.BeginFrame();

        Assert.Equal(2, pass.ClearCount);
        Assert.Equal(0, pass.SubmissionCount);
        Assert.Equal(0, pass.Commands.Count);
        Assert.Empty(pass.Geometry);
    }

    [Fact]
    public void CompatibleBatchesDoNotBeginOrClearAFrame()
    {
        var pass = new MixedPass();
        using var renderer = CreateRenderer(pass);
        var command = new DrawCommand { ResourceIndex = 7 };
        var geometry = new GeometrySubmission(3, 5);
        pass.Submit(in command);
        pass.Submit(in geometry);

        Assert.True(renderer.Context.Batch(Layer, required: true).IsActive);
        Assert.True(renderer.Context.Batch<GeometrySubmission>(Layer).IsActive);

        Assert.Equal(0, pass.ClearCount);
        Assert.Equal(2, pass.SubmissionCount);
        Assert.Equal(command, pass.Commands.At(0));
        Assert.Equal(geometry, Assert.Single(pass.Geometry));

        renderer.BeginFrame();

        Assert.Equal(1, pass.ClearCount);
        Assert.Equal(0, pass.SubmissionCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RequiredBatchRejectsUnsupportedTypesEvenWhenDisabled(bool enabled)
    {
        var pass = new RecordingPass<GeometrySubmission>();
        using var renderer = CreateRenderer(pass, enabled, enabled);
        var submission = new GeometrySubmission(3, 5);
        pass.Submit(in submission);

        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            renderer.Context.Batch<int>(Layer, required: true);
        });
        Assert.Contains(Layer.Name, exception.Message);
        Assert.Contains(typeof(int).FullName!, exception.Message);

        Assert.Throws<InvalidOperationException>(() =>
        {
            renderer.Context.Batch(Layer, required: true);
        });

        Assert.Equal(0, pass.ClearCount);
        Assert.Equal(submission, Assert.Single(pass.Submissions));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RequiredBatchRejectsMissingOrInvalidLayers(bool invalid)
    {
        var pass = new RecordingPass<DrawCommand>();
        using var renderer = CreateRenderer(pass);
        var id = invalid ? default : OtherLayer;

        Assert.Throws<InvalidOperationException>(() =>
        {
            renderer.Context.Batch(id, required: true);
        });

        Assert.False(renderer.Context.Batch(id, required: false).IsActive);
        Assert.False(renderer.Context.IsActive(id));
        Assert.Equal(0, pass.ClearCount);
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(false, false)]
    public void RequiredCompatibleBatchesRemainInactiveWhenDisabled(bool rendererEnabled, bool layerEnabled)
    {
        var pass = new MixedPass();
        using var renderer = CreateRenderer(pass, rendererEnabled, layerEnabled);

        var commands = renderer.Context.Batch(Layer, required: true);
        var geometries = renderer.Context.Batch<GeometrySubmission>(Layer, required: true);
        Assert.False(commands.IsActive);
        Assert.False(geometries.IsActive);
        commands.Submit(default);
        geometries.Submit(default);

        Assert.Equal(0, pass.ClearCount);
        Assert.Equal(0, pass.SubmissionCount);
    }

    [Fact]
    public void IncompatibleWritersDoNotOpenAFrameOrChangeCustomPassData()
    {
        var pass = new RecordingPass<GeometrySubmission>();
        using var renderer = CreateRenderer(pass);
        var submission = new GeometrySubmission(3, 5);
        pass.Submit(in submission);

        var batch = renderer.Context.Batch(Layer);
        var incompatible = renderer.Context.Batch<int>(Layer, required: false);
        Assert.True(renderer.Context.IsActive(Layer));
        Assert.False(batch.IsActive);
        Assert.False(incompatible.IsActive);
        batch.Submit(default);
        batch.Draw(null!, Vector2.Zero, Color.White);
        batch.DrawString(null!, "ignored", Vector2.Zero, Color.White);
        batch.DrawPrimitives(null!, PrimitiveType.TriangleList, 1, Vector2.Zero);
        incompatible.Submit(42);

        Assert.Equal(0, pass.ClearCount);
        Assert.Equal(submission, Assert.Single(pass.Submissions));
    }

    [Theory]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, true)]
    public void MissingOrDisabledLayersReturnInactiveWriters(bool rendererEnabled, bool layerEnabled, bool missing)
    {
        var pass = new RecordingPass<DrawCommand>();
        using var renderer = CreateRenderer(pass, rendererEnabled, layerEnabled);
        var id = missing ? OtherLayer : Layer;
        var batch = renderer.Context.Batch(id);
        var submissions = renderer.Context.Batch<DrawCommand>(id, required: false);

        Assert.False(renderer.Context.IsActive(id));
        Assert.False(batch.IsActive);
        Assert.False(submissions.IsActive);
        Assert.Null(batch.Resources);
        Assert.Null(batch.Text);
        Assert.Null(batch.CullBounds);
        batch.Submit(default);
        submissions.Submit(default);

        Assert.Equal(0, pass.ClearCount);
        Assert.Empty(pass.Submissions);
    }

    [Fact]
    public void BeginningAFrameClearsDisabledPassesToo()
    {
        var active = new RecordingPass<GeometrySubmission>();
        var disabled = new RecordingPass<GeometrySubmission>();
        var options = new RendererOptions(new Screen(new Point(320, 180)))
        {
            Layers =
            {
                new(Layer, new RenderLayerOptions { Pass = active }),
                new(OtherLayer, new RenderLayerOptions { Pass = disabled, Enabled = false }),
            },
        };
        using var renderer = new Renderer2D("SubmissionReset", options);
        active.Submit(new GeometrySubmission(0, 1));
        disabled.Submit(new GeometrySubmission(0, 1));

        renderer.BeginFrame();

        Assert.Equal(1, active.ClearCount);
        Assert.Equal(1, disabled.ClearCount);
        Assert.Empty(active.Submissions);
        Assert.Empty(disabled.Submissions);
    }

    [Fact]
    public void ProceduralPassDoesNotRequireSubmissionStorage()
    {
        var pass = new ProceduralPass();
        using var renderer = CreateRenderer(pass);

        renderer.BeginFrame();
        Assert.True(renderer.Context.IsActive(Layer));
        Assert.False(renderer.Context.Batch(Layer).IsActive);
        Assert.False(renderer.Context.Batch<GeometrySubmission>(Layer, required: false).IsActive);

        pass.Prepare(default);
        pass.Execute(default);

        Assert.Equal(1, pass.ExecuteCount);
    }

    [Fact]
    public void DrawSubmissionsAndPreparationDoNotAllocateAfterWarmup()
    {
        var pass = new DrawCommandPass(RenderSortMode.FrontToBack);
        using var renderer = CreateRenderer(pass);
        var command = new DrawCommand { Kind = DrawCommandKind.Sprite, Depth = 0.5f };

        for (var index = 0; index < 32; index++)
        {
            SubmitDrawFrame(renderer, pass, in command);
        }

        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < 1000; index++)
        {
            SubmitDrawFrame(renderer, pass, in command);
        }

        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        Assert.Equal(2, pass.SubmissionCount);
        Assert.Equal(0, allocatedBytes);
    }

    [Fact]
    public void CustomSubmissionsDoNotAllocateAfterWarmup()
    {
        var pass = new RecordingPass<GeometrySubmission>();
        using var renderer = CreateRenderer(pass);
        var submission = new GeometrySubmission(3, 5);

        for (var index = 0; index < 32; index++)
        {
            renderer.BeginFrame();
            renderer.Context.Batch<GeometrySubmission>(Layer).Submit(in submission);
        }

        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < 1000; index++)
        {
            renderer.BeginFrame();
            renderer.Context.Batch<GeometrySubmission>(Layer).Submit(in submission);
        }

        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        Assert.Equal(submission, Assert.Single(pass.Submissions));
        Assert.Equal(0, allocatedBytes);
    }

    private static Renderer2D CreateRenderer(IRenderLayerPass pass, bool enabled = true, bool layerEnabled = true)
        => new("SubmissionTests", new RendererOptions(new Screen(new Point(320, 180)))
        {
            Enabled = enabled,
            Layers = { new(Layer, new RenderLayerOptions { Pass = pass, Enabled = layerEnabled }) },
        });

    private static void SubmitDrawFrame(Renderer2D renderer, DrawCommandPass pass, in DrawCommand command)
    {
        renderer.BeginFrame();
        renderer.Context.Batch(Layer).Submit(in command);
        renderer.Context.Batch<DrawCommand>(Layer).Submit(in command);
        pass.Prepare(default);
    }

    private readonly record struct GeometrySubmission(int VertexStart, int PrimitiveCount);

    private sealed class BoundsCamera : ICamera2D
    {
        public Rectangle Bounds { get; set; }

        public Matrix GetViewMatrix(Vector2 parallax) => Matrix.Identity;

        public Rectangle GetVisibleBounds(Vector2 parallax) => Bounds;
    }

    private sealed class MixedPass : DrawCommandPass, ILayerSubmissionPass<GeometrySubmission>
    {
        public List<GeometrySubmission> Geometry { get; } = [];

        public override int SubmissionCount => base.SubmissionCount + Geometry.Count;

        public int ClearCount { get; private set; }

        public void Submit(in GeometrySubmission submission) => Geometry.Add(submission);

        public override void ClearSubmissions()
        {
            ClearCount++;
            base.ClearSubmissions();
            Geometry.Clear();
        }

        public override void Execute(in RenderFrameContext context)
        {
        }
    }

    private sealed class RecordingPass<T> : RenderLayerPass, ILayerSubmissionPass<T> where T : struct
    {
        public List<T> Submissions { get; } = [];

        public int SubmissionCount => Submissions.Count;

        public int ClearCount { get; private set; }

        public void Submit(in T submission) => Submissions.Add(submission);

        public void ClearSubmissions()
        {
            ClearCount++;
            Submissions.Clear();
        }

        public override void Execute(in RenderFrameContext context)
        {
        }
    }

    private sealed class ProceduralPass : RenderLayerPass
    {
        public int ExecuteCount { get; private set; }

        public override void Execute(in RenderFrameContext context) => ExecuteCount++;
    }

    private sealed class RecordingBatcher : ISpriteBatcher
    {
        public List<DrawCommand> Commands { get; } = [];

        public int BeginCount { get; private set; }

        public int EndCount { get; private set; }

        public bool IsBatchOpen { get; private set; }

        public void Begin(in BatchState state)
        {
            BeginCount++;
            IsBatchOpen = true;
        }

        public void Submit(in DrawCommand command, RenderResourceTable resources, FrameTextBuffer text)
            => Commands.Add(command);

        public void End()
        {
            EndCount++;
            IsBatchOpen = false;
        }
    }
}