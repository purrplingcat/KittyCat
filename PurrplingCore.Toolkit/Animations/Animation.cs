namespace PurrplingCore.Toolkit.Animations;

public sealed class Animation(string name, AnimationFrame[] frames, bool isLooping, bool isReversed, bool isPingPong) : IAnimation
{
    private readonly AnimationFrame[] _frames = frames;

    public string Name { get; } = name;
    public ReadOnlySpan<AnimationFrame> Frames => _frames;
    public int FrameCount => _frames.Length;
    public bool IsLooping { get; } = isLooping;
    public bool IsReversed { get; } = isReversed;
    public bool IsPingPong { get; } = isPingPong;
}
