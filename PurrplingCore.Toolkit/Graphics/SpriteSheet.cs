using PurrplingCore.Toolkit.Animations;
using System.Diagnostics;
using System;

namespace PurrplingCore.Toolkit.Graphics;

public class SpriteSheet
{
    private readonly Dictionary<string, Animation> _animations = [];

    public int AnimationCount => _animations.Count;
    public string Name { get; }
    public Texture2DAtlas TextureAtlas { get; }

    public SpriteSheet(string name, Texture2DAtlas atlas)
    {
        ArgumentNullException.ThrowIfNull(atlas, nameof(atlas));

        Name = name;
        TextureAtlas = atlas;
    }

    public Animation GetAnimation(string name) => _animations[name];
    public bool RemoveAnimation(string name) => _animations.Remove(name);

    public void DefineAnimation(string name, Action<SpriteSheetAnimationBuilder> buildAction)
    {
        var builder = new SpriteSheetAnimationBuilder(name, this);
        buildAction(builder);
        _animations.Add(name, builder.Build());
    }
}

public sealed class SpriteSheetAnimationBuilder
{
    private readonly string _name;
    private readonly SpriteSheet _spriteSheet;
    private readonly List<AnimationFrame> _frames = new List<AnimationFrame>();
    private bool _isLooping;
    private bool _isReversed;
    private bool _isPingPong;

    internal SpriteSheetAnimationBuilder(string name, SpriteSheet spriteSheet)
    {
        _name = name;
        _spriteSheet = spriteSheet;
    }

    /// <summary>
    /// Adds a frame to the animation using the region index and duration.
    /// </summary>
    /// <param name="regionIndex">The index of the region in the sprite sheet.</param>
    /// <param name="duration">The duration of the frame.</param>
    /// <returns>The <see cref="SpriteSheetAnimationBuilder"/> instance for chaining.</returns>
    public SpriteSheetAnimationBuilder AddFrame(int regionIndex, TimeSpan duration)
    {
        var frame = new AnimationFrame(regionIndex, duration);
        _frames.Add(frame);
        return this;
    }

    /// <summary>
    /// Adds a frame to the animation using the region name and duration.
    /// </summary>
    /// <param name="regionName">The name of the region in the sprite sheet.</param>
    /// <param name="duration">The duration of the frame.</param>
    /// <returns>The <see cref="SpriteSheetAnimationBuilder"/> instance for chaining.</returns>
    public SpriteSheetAnimationBuilder AddFrame(string regionName, TimeSpan duration)
    {
        int index = _spriteSheet.TextureAtlas.GetIndexOfRegion(regionName);
        return AddFrame(index, duration);
    }

    /// <summary>
    /// Sets whether the animation should loop.
    /// </summary>
    /// <param name="isLooping">If set to <c>true</c>, the animation will loop.</param>
    /// <returns>The <see cref="SpriteSheetAnimationBuilder"/> instance for chaining.</returns>
    public SpriteSheetAnimationBuilder IsLooping(bool isLooping)
    {
        _isLooping = isLooping;
        return this;
    }

    /// <summary>
    /// Sets whether the animation should be reversed.
    /// </summary>
    /// <param name="isReversed">If set to <c>true</c>, the animation will play in reverse.</param>
    /// <returns>The <see cref="SpriteSheetAnimationBuilder"/> instance for chaining.</returns>
    public SpriteSheetAnimationBuilder IsReversed(bool isReversed)
    {
        _isReversed = isReversed;
        return this;
    }

    /// <summary>
    /// Sets whether the animation should ping-pong (reverse direction at the ends).
    /// </summary>
    /// <param name="isPingPong">If set to <c>true</c>, the animation will ping-pong.</param>
    /// <returns>The <see cref="SpriteSheetAnimationBuilder"/> instance for chaining.</returns>
    public SpriteSheetAnimationBuilder IsPingPong(bool isPingPong)
    {
        _isPingPong = isPingPong;
        return this;
    }

    internal Animation Build() =>
        new(_name, _frames.ToArray(), _isLooping, _isReversed, _isPingPong);
}
