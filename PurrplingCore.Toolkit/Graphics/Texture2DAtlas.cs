using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Runtime.InteropServices;

namespace PurrplingCore.Toolkit.Graphics;

public class Texture2DAtlas
{
    private readonly string _name;
    private readonly Texture2D _texture;
    private readonly List<TextureRegion2D> _regions = [];
    private readonly Dictionary<string, TextureRegion2D> _regionsByName = [];

    public string Name => _name;
    public int RegionCount => _regions.Count;

    public Texture2DAtlas(string name, Texture2D texture)
    {
        ArgumentNullException.ThrowIfNull(texture);
        if (texture.IsDisposed)
        {
            throw new ObjectDisposedException(nameof(texture), $"{nameof(texture)} was disposed prior");
        }

        if (string.IsNullOrEmpty(name))
        {
            name = $"{texture.Name}Atlas";
        }

        _name = name;
        _texture = texture;
    }

    public TextureRegion2D this[int index] => GetRegion(index);
    public TextureRegion2D this[string name] => GetRegion(name);

    public TextureRegion2D CreateRegion(int x, int y, int width, int height) => CreateRegion(new Rectangle(x, y, width, height));
    public TextureRegion2D CreateRegion(string name, int x, int y, int width, int height) => CreateRegion(name, new Rectangle(x, y, width, height));

    public TextureRegion2D CreateRegion(Rectangle bounds)
    {
        var region = new TextureRegion2D(_texture, bounds);

        AddRegion(region);
        return region;
    }

    public TextureRegion2D CreateRegion(string name, Rectangle bounds)
    {
        var region = new TextureRegion2D(_texture, bounds, name);

        AddRegion(region);
        return region;
    }

    private void AddRegion(in TextureRegion2D region)
    {

        if (!string.IsNullOrEmpty(region.Name))
        {
            if (_regionsByName.ContainsKey(region.Name))
            {
                throw new InvalidOperationException($"This atlas already contains a region with the name {region.Name}");
            }

            _regionsByName.Add(region.Name, region);
        }

        _regions.Add(region);
    }

    public TextureRegion2D GetRegion(int index) => _regions[index];

    public TextureRegion2D GetRegion(string name) => _regionsByName[name];

    public bool TryGetRegion(string name, out TextureRegion2D region) => _regionsByName.TryGetValue(name, out region);

    public bool TryGetRegion(int index, out TextureRegion2D region)
    {
        if (index >= 0 && index < _regions.Count)
        {
            region = _regions[index];
            return true;
        }

        region = default;
        return false;
    }

    public Span<TextureRegion2D> GetRegions(params int[] indexes)
    {
        var regions = new TextureRegion2D[indexes.Length];
        for (int i = 0; i < indexes.Length; i++)
        {
            regions[i] = GetRegion(indexes[i]);
        }

        return regions;
    }

    public Span<TextureRegion2D> GetRegions(params string[] names)
    {
        var regions = new TextureRegion2D[names.Length];
        for (int i = 0; i < names.Length; i++)
        {
            regions[i] = GetRegion(names[i]);
        }

        return regions;
    }

    public int GetIndexOfRegion(string name)
    {
        for (int i = 0; i < _regions.Count; i++)
        {
            if (_regions[i].Name == name)
            {
                return i;
            }
        }

        return -1;
    }

    private bool RemoveRegion(TextureRegion2D region) => _regions.Remove(region) || _regionsByName.Remove(region.Name);

    public bool RemoveRegion(int index)
    {
        if (TryGetRegion(index, out TextureRegion2D region))
        {
            return RemoveRegion(region);
        }

        return false;
    }

    public bool RemoveRegion(string name)
    {
        if (TryGetRegion(name, out TextureRegion2D region))
        {
            return RemoveRegion(region);
        }

        return false;
    }

    public void ClearRegions()
    {
        _regions.Clear();
        _regionsByName.Clear();
    }

    public static Texture2DAtlas Create(string name, Texture2D texture, int regionWidth, int regionHeight, int maxRegionCount = int.MaxValue, int margin = 0, int spacing = 0)
    {
        ReadOnlySpan<Rectangle> regions = CalculateRegions(texture.Width, texture.Height, regionWidth, regionHeight, maxRegionCount, margin, spacing);
        Texture2DAtlas atlas = new(name, texture);

        for (int i = 0; i < regions.Length; i++)
        {
            atlas.CreateRegion(regions[i]);
        }

        return atlas;
    }

    public ReadOnlySpan<TextureRegion2D> AsSpan() => CollectionsMarshal.AsSpan(_regions);

    public static Texture2DAtlas Create(Texture2D texture, int regionWidth, int regionHeight, int maxRegionCount = int.MaxValue, int margin = 0, int spacing = 0)
    {
        return Create($"{texture.Name}Atlas", texture, regionWidth, regionHeight, maxRegionCount, margin, spacing);
    }

    internal static ReadOnlySpan<Rectangle> CalculateRegions(int textureWidth, int textureHeight, int regionWidth, int regionHeight, int maxRegionCount, int margin, int spacing)
    {
        int width = textureWidth - margin;
        int height = textureHeight - margin;
        int xIncrement = regionWidth + spacing;
        int yIncrement = regionHeight + spacing;

        int columns = (width - margin + spacing) / xIncrement;
        int rows = (height - margin + spacing) / yIncrement;
        int totalRegions = columns * rows;

        int capacity = Math.Min(totalRegions, maxRegionCount);
        var regions = new List<Rectangle>(capacity);

        for (int i = 0; i < totalRegions; i++)
        {
            int x = margin + (i % columns) * xIncrement;
            int y = margin + (i / columns) * yIncrement;

            if (x >= width || y >= height)
            {
                break;
            }

            regions.Add(new Rectangle(x, y, regionWidth, regionHeight));

            if (regions.Count >= maxRegionCount)
            {
                break;
            }
        }

        return CollectionsMarshal.AsSpan(regions);
    }
}
