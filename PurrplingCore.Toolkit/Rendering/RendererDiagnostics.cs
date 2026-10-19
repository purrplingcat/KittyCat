using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace PurrplingCore.Toolkit.Rendering;

/// <summary>
/// Enables the renderer's rolling CPU performance watch.
/// </summary>
public sealed class RendererDiagnosticsOptions
{
    /// <summary>
    /// Number of recent frames retained for inspection.
    /// </summary>
    public int HistoryCapacity { get; set; } = 120;

    /// <summary>
    /// Frame duration at or above which a sample is marked as slow.
    /// </summary>
    public TimeSpan SlowFrameThreshold { get; set; } = TimeSpan.FromMilliseconds(16.67);
}

/// <summary>
/// CPU timings and submission totals recorded for one renderer flush.
/// </summary>
public readonly record struct RendererFrameSample(
    int Frame,
    double TotalMilliseconds,
    double PrepareMilliseconds,
    double ExecuteMilliseconds,
    double ComposeMilliseconds,
    int SubmissionCount,
    int EnabledLayerCount,
    bool IsSlowFrame);

/// <summary>
/// Submission count and CPU timings for one layer in the latest renderer frame.
/// </summary>
public readonly record struct RendererLayerSample(
    RenderLayerId Id,
    bool Enabled,
    int SubmissionCount,
    double PrepareMilliseconds,
    double ExecuteMilliseconds);

/// <summary>
/// Rolling performance summary over the samples currently retained by a diagnostics watch.
/// </summary>
public readonly record struct RendererPerformanceSummary(
    int SampleCount,
    double AverageMilliseconds,
    double MinimumMilliseconds,
    double P95Milliseconds,
    double MaximumMilliseconds,
    int SlowFrameCount);

/// <summary>
/// Stores a bounded history of renderer timings and the latest per-layer counters.
/// </summary>
/// <remarks>
/// The renderer updates diagnostics on its flush thread. Read and reset this object on that same
/// thread; it is not a synchronization boundary for graphics work.
/// </remarks>
public sealed class RendererDiagnostics
{
    private readonly string _rendererName;
    private readonly double _slowFrameThresholdMilliseconds;
    private readonly RendererFrameSample[] _history;
    private readonly RendererLayerSample[] _latestLayers;
    private readonly double[] _durationScratch;
    private int _historyCount;
    private int _nextHistoryIndex;

    internal RendererDiagnostics(string rendererName, RendererDiagnosticsOptions options, RenderLayer[] layers)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(options.HistoryCapacity, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(options.SlowFrameThreshold, TimeSpan.Zero);

        _rendererName = rendererName;
        _slowFrameThresholdMilliseconds = options.SlowFrameThreshold.TotalMilliseconds;
        _history = new RendererFrameSample[options.HistoryCapacity];
        _durationScratch = new double[options.HistoryCapacity];
        _latestLayers = new RendererLayerSample[layers.Length];

        for (var index = 0; index < layers.Length; index++)
        {
            _latestLayers[index] = new RendererLayerSample(layers[index].Id, false, 0, 0, 0);
        }
    }

    /// <summary>
    /// The most recently recorded frame, or the default value before the first flush.
    /// </summary>
    public RendererFrameSample LatestFrame { get; private set; }

    /// <summary>
    /// Per-layer counters from the most recently recorded frame.
    /// </summary>
    public ReadOnlySpan<RendererLayerSample> LatestLayers => _latestLayers;

    /// <summary>
    /// Number of frame samples currently retained.
    /// </summary>
    public int SampleCount => _historyCount;

    /// <summary>
    /// Copies retained frame samples from oldest to newest into the destination.
    /// </summary>
    /// <remarks>If the destination is shorter than the history, it receives the newest samples.</remarks>
    public int CopyHistory(Span<RendererFrameSample> destination)
    {
        var copyCount = Math.Min(destination.Length, _historyCount);
        var skippedSamples = _historyCount - copyCount;
        var oldestIndex = (_nextHistoryIndex - _historyCount + _history.Length + skippedSamples) % _history.Length;

        for (var index = 0; index < copyCount; index++)
        {
            destination[index] = _history[(oldestIndex + index) % _history.Length];
        }

        return copyCount;
    }

    /// <summary>
    /// Calculates rolling min, average, p95, max, and slow-frame values.
    /// </summary>
    public RendererPerformanceSummary GetSummary()
    {
        if (_historyCount == 0) return default;

        var totalMilliseconds = 0d;
        var slowFrames = 0;
        var oldestIndex = (_nextHistoryIndex - _historyCount + _history.Length) % _history.Length;

        for (var index = 0; index < _historyCount; index++)
        {
            var sample = _history[(oldestIndex + index) % _history.Length];
            _durationScratch[index] = sample.TotalMilliseconds;
            totalMilliseconds += sample.TotalMilliseconds;
            if (sample.IsSlowFrame) slowFrames++;
        }

        Array.Sort(_durationScratch, 0, _historyCount);
        var percentileIndex = Math.Max(0, (int)Math.Ceiling(_historyCount * 0.95) - 1);

        return new RendererPerformanceSummary(
            _historyCount,
            totalMilliseconds / _historyCount,
            _durationScratch[0],
            _durationScratch[percentileIndex],
            _durationScratch[_historyCount - 1],
            slowFrames);
    }

    /// <summary>
    /// Builds a human-readable snapshot for logs and debugger output.
    /// </summary>
    public string CreateDebugReport()
    {
        var builder = new StringBuilder();
        var summary = GetSummary();
        builder.Append("Renderer ").AppendLine(_rendererName);
        builder.Append("Frame ").Append(LatestFrame.Frame)
            .Append(": ").Append(Format(LatestFrame.TotalMilliseconds)).Append(" ms, ")
            .Append(LatestFrame.SubmissionCount).Append(" submissions, ")
            .Append(LatestFrame.EnabledLayerCount).AppendLine(" enabled layers");
        builder.Append("CPU prepare/execute/compose: ")
            .Append(Format(LatestFrame.PrepareMilliseconds)).Append(" / ")
            .Append(Format(LatestFrame.ExecuteMilliseconds)).Append(" / ")
            .Append(Format(LatestFrame.ComposeMilliseconds)).AppendLine(" ms");
        builder.Append("Watch: ").Append(summary.SampleCount).Append(" frames, avg ")
            .Append(Format(summary.AverageMilliseconds)).Append(" ms, p95 ")
            .Append(Format(summary.P95Milliseconds)).Append(" ms, max ")
            .Append(Format(summary.MaximumMilliseconds)).Append(" ms, slow ")
            .Append(summary.SlowFrameCount).AppendLine();

        foreach (var layer in _latestLayers)
        {
            builder.Append("  ").Append(layer.Id.Name)
                .Append(layer.Enabled ? " enabled" : " disabled")
                .Append(": ").Append(layer.SubmissionCount).Append(" submissions, prepare ")
                .Append(Format(layer.PrepareMilliseconds)).Append(" ms, execute ")
                .Append(Format(layer.ExecuteMilliseconds)).AppendLine(" ms");
        }

        return builder.ToString();
    }

    /// <summary>
    /// Clears retained samples and resets the latest snapshot.
    /// </summary>
    public void Reset()
    {
        Array.Clear(_history);
        _historyCount = 0;
        _nextHistoryIndex = 0;
        LatestFrame = default;

        for (var index = 0; index < _latestLayers.Length; index++)
        {
            _latestLayers[index] = new RendererLayerSample(_latestLayers[index].Id, false, 0, 0, 0);
        }
    }

    internal void BeginFrame()
    {
        for (var index = 0; index < _latestLayers.Length; index++)
        {
            _latestLayers[index] = new RendererLayerSample(_latestLayers[index].Id, false, 0, 0, 0);
        }
    }

    internal void RecordLayerPrepared(
        int index,
        bool enabled,
        int submissionCount,
        double prepareMilliseconds)
    {
        _latestLayers[index] = _latestLayers[index] with
        {
            Enabled = enabled,
            SubmissionCount = submissionCount,
            PrepareMilliseconds = prepareMilliseconds,
        };
    }

    internal void RecordLayerExecuted(int index, double executeMilliseconds)
        => _latestLayers[index] = _latestLayers[index] with { ExecuteMilliseconds = executeMilliseconds };

    internal void RecordFrame(
        int frame,
        double totalMilliseconds,
        double prepareMilliseconds,
        double executeMilliseconds,
        double composeMilliseconds)
    {
        var submissionCount = 0;
        var enabledLayerCount = 0;
        foreach (var layer in _latestLayers)
        {
            if (!layer.Enabled) continue;

            enabledLayerCount++;
            submissionCount += layer.SubmissionCount;
        }

        var sample = new RendererFrameSample(
            frame,
            totalMilliseconds,
            prepareMilliseconds,
            executeMilliseconds,
            composeMilliseconds,
            submissionCount,
            enabledLayerCount,
            totalMilliseconds >= _slowFrameThresholdMilliseconds);

        LatestFrame = sample;
        _history[_nextHistoryIndex] = sample;
        _nextHistoryIndex = (_nextHistoryIndex + 1) % _history.Length;
        _historyCount = Math.Min(_historyCount + 1, _history.Length);
    }

    private static string Format(double milliseconds)
        => milliseconds.ToString("F2", CultureInfo.InvariantCulture);
}

/// <summary>
/// Result of a warm-up and repeated renderer action benchmark.
/// </summary>
public readonly record struct RendererBenchmarkResult(
    int WarmupFrames,
    int MeasuredFrames,
    double MinimumMilliseconds,
    double AverageMilliseconds,
    double MedianMilliseconds,
    double P95Milliseconds,
    double MaximumMilliseconds);

/// <summary>
/// Runs repeatable CPU-side measurements for a renderer frame action.
/// </summary>
public static class RendererBenchmark
{
    /// <summary>
    /// Runs warm-up frames, then measures each invocation of <paramref name="renderFrame"/>.
    /// </summary>
    /// <remarks>
    /// Invoke this on the graphics thread when the action flushes a renderer. Measurements cover
    /// CPU wall time for the action; they do not wait for queued GPU work to finish.
    /// </remarks>
    public static RendererBenchmarkResult Run(Action renderFrame, int warmupFrames = 30, int measuredFrames = 120)
    {
        ArgumentNullException.ThrowIfNull(renderFrame);
        ArgumentOutOfRangeException.ThrowIfNegative(warmupFrames);
        ArgumentOutOfRangeException.ThrowIfLessThan(measuredFrames, 1);

        for (var index = 0; index < warmupFrames; index++)
        {
            renderFrame();
        }

        var samples = new double[measuredFrames];
        var totalMilliseconds = 0d;
        for (var index = 0; index < measuredFrames; index++)
        {
            var start = Stopwatch.GetTimestamp();
            renderFrame();
            var elapsed = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            samples[index] = elapsed;
            totalMilliseconds += elapsed;
        }

        Array.Sort(samples);
        var middle = measuredFrames / 2;
        var median = measuredFrames % 2 == 0
            ? (samples[middle - 1] + samples[middle]) / 2d
            : samples[middle];
        var percentileIndex = Math.Max(0, (int)Math.Ceiling(measuredFrames * 0.95) - 1);

        return new RendererBenchmarkResult(
            warmupFrames,
            measuredFrames,
            samples[0],
            totalMilliseconds / measuredFrames,
            median,
            samples[percentileIndex],
            samples[^1]);
    }
}