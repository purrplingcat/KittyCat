namespace PurrplingCore.Toolkit.DI;

public interface IGameFeatureCollection
{
    uint Revision { get; }

    bool IsReadOnly { get; }

    public int Count { get; }

    IGameFeature? this[Type featureType] { get; set; }

    void Set<TFeature>(TFeature? feature) where TFeature : IGameFeature;
    TFeature? Get<TFeature>() where TFeature : IGameFeature;
    bool Has<TFeature>() where TFeature : IGameFeature;
}

public interface IGameFeature { }

public class GameFeatureCollection : IGameFeatureCollection
{
    private volatile uint _revision;
    private readonly IDictionary<Type, IGameFeature> _features;

    public uint Revision => _revision;

    public bool IsReadOnly { get; private set; }
    public int Count => _features.Count;

    public GameFeatureCollection(IEnumerable<IGameFeature> features)
    {
        _features = new Dictionary<Type, IGameFeature>();

        foreach (var feature in features)
        {
            _features[feature.GetType()] = feature;
        }

        _revision++;
    }

    public GameFeatureCollection()
    {
        _features = new Dictionary<Type, IGameFeature>();
    }

    public IGameFeature? this[Type featureType] 
    {
        get
        {
            _features.TryGetValue(featureType, out var feature);
            return feature;
        }
        set
        {
            if (IsReadOnly)
            {
                throw new InvalidOperationException("Cannot modify a read-only GameFeatureCollection.");
            }

            if (value is null)
            {
                _features.Remove(featureType);
            }
            else
            {
                _features[featureType] = value;
            }
            _revision++;
        }
    }

    public void Set<TFeature>(TFeature? feature) where TFeature : IGameFeature
    {
        this[typeof(TFeature)] = feature;
    }

    public TFeature? Get<TFeature>() where TFeature : IGameFeature
    {
        return (TFeature?)this[typeof(TFeature)];
    }

    public bool Has<TFeature>() where TFeature : IGameFeature
    {
        var type = typeof(TFeature);
        return _features.ContainsKey(type);
    }

    public void Freeze()
    {
        IsReadOnly = true;
    }
}
