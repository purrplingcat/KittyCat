using System;
using Friflo.Engine.ECS.Systems;
using KittyCat.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Xna.Framework;
using PurrplingCore.Ecs.Attributes;
using PurrplingCore.Ecs.Systems;
using PurrplingCore.Graphics;
using PurrplingCore.Toolkit.Rendering;

namespace KittyCat.Ecs.Systems;

/// <summary>
/// Draws a stable, gently twinkling night sky and the ground behind gameplay.
/// </summary>
[System<DrawSystemGroup>]
public sealed class StarCatcherBackgroundSystem : BaseSystem
{
    private readonly GraphicsManager _graphics;
    private readonly RenderContext _context;
    private readonly BackgroundStar[] _stars;
    private readonly Rectangle _ground;

    public StarCatcherBackgroundSystem(
        GraphicsManager graphics,
        IOptions<StarCatcherOptions> options,
        RenderContext context)
    {
        _graphics = graphics;
        _context = context;
        var playfield = options.Value.Playfield;
        // Bounds use world coordinates; the background layer applies the shared world camera.
        _ground = new Rectangle(0, playfield.GroundTop, playfield.Width, playfield.Height - playfield.GroundTop);
        // Allocate star positions once per world; drawing only animates their brightness.
        _stars = CreateStars(playfield.Width, _ground.Top);
    }

    protected override void OnUpdateGroup()
    {
        var writer = _context.Batch(KittyCatLayers.Background);
        if (!writer.IsActive) return;

        // Reuse the shared white pixel for every dot and terrain strip.
        var pixel = Primitives2D.GetPixel(_graphics.GraphicsDevice);
        var time = (float)Tick.time;
        var dim = new Color(38, 51, 65);
        foreach (ref readonly var star in _stars.AsSpan())
        {
            // Map sine to [0, 1]; individual speeds and phases avoid synchronized blinking.
            var brightness = 0.6f + 0.5f * MathF.Sin(time * star.Speed + star.Phase);
            writer.Draw(pixel, star.Bounds, Color.Lerp(dim, star.Color, brightness));
        }

        // Soil, a brighter grass surface, and its shaded underside form the ground profile.
        writer.Draw(pixel, _ground, new Color(21, 35, 31));
        writer.Draw(pixel, new Rectangle(0, _ground.Top, _ground.Width, 4), new Color(64, 99, 65));
        writer.Draw(pixel, new Rectangle(0, _ground.Top + 4, _ground.Width, 3), new Color(34, 57, 40));
        // Deterministic tufts break up the straight horizon without storing more scenery.
        for (var position = 7; position < _ground.Width; position += 23)
        {
            var height = 4 + position % 3;
            writer.Draw(pixel, new Rectangle(position, _ground.Top - height, 4, height), new Color(76, 114, 72));
        }
    }

    private static BackgroundStar[] CreateStars(int width, int skyHeight)
    {
        // A fixed seed recreates the same sky whenever a new world is composed.
        var random = new Random(173);
        var stars = new BackgroundStar[80];
        for (var index = 0; index < stars.Length; index++)
        {
            // Most stars are single pixels; occasional larger, warmer dots add variation.
            var size = index % 7 == 0 ? 4 : 2;
            // Leave side margins and keep the sky clear of the ground's grass tufts.
            var bounds = new Rectangle(
                16 + random.Next(Math.Max(1, width - 32)),
                16 + random.Next(Math.Max(1, skyHeight - 48)),
                size,
                size);
            stars[index] = new BackgroundStar(
                bounds,
                random.NextSingle() * MathHelper.TwoPi,
                0.7f + random.NextSingle() * 1.1f,
                index % 9 == 0 ? new Color(182, 168, 133) : new Color(137, 161, 184));
        }

        return stars;
    }

    // Phase is in radians, Speed in radians per second, and Color is the peak twinkle tint.
    private readonly record struct BackgroundStar(Rectangle Bounds, float Phase, float Speed, Color Color);
}