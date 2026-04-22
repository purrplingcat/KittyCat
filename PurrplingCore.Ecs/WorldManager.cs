using PurrplingCore.Ecs.Extensions;

namespace PurrplingCore.Ecs;

public class WorldManager(IWorldFactory factory, IEnumerable<WorldTag> knownWorlds)
{
    private readonly Dictionary<string, ManagedWorld> _worldsByName = [];
    private readonly List<ManagedWorld> _worlds = [];
    private readonly HashSet<WorldTag> _knownWorlds = [.. knownWorlds];
    private readonly object _lock = new();

    public IReadOnlyCollection<ManagedWorld> Worlds => _worlds.AsReadOnly();

    public ManagedWorld? GetWorld(string name) => _worldsByName.GetValueOrDefault(name);

    public IEnumerable<ManagedWorld> GetWorlds(WorldTag tag)
    {
        return _worlds.Where(w => w.Tag == tag);
    }

    public ManagedWorld CreateWorld(string? name = null)
    {
        return CreateWorld(WorldTag.Default, name);
    }

    public ManagedWorld CreateWorld(WorldTag tag, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(tag, nameof(tag));
        name ??= $"World_{Guid.NewGuid():N}";

        if (!_knownWorlds.Contains(tag))
        {
            throw new InvalidOperationException($"World tag '{tag.DebugName}' is not recognized.");
        }

        if (ContainsWorld(name))
        {
            throw new InvalidOperationException($"A world with the name '{name}' already exists.");
        }

        var world = factory.CreateWorld(name, tag);

        world.Bootstrap(world.Tag);
        AddWorld(world);

        return world;
    }

    public void AddWorld(ManagedWorld world)
    {
        lock (_lock)
        {
            if (!string.IsNullOrEmpty(world.Name))
            {
                if (_worldsByName.ContainsKey(world.Name))
                {
                    throw new InvalidOperationException($"World '{world.Name}' already exists.");
                }
                _worldsByName.Add(world.Name, world);
            }
            _worlds.Add(world);
            world.Disposed += OnWorldDisposed;
        }
    }

    public bool ContainsWorld(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;

        lock (_lock)
        {
            return _worldsByName.ContainsKey(name);
        }
    }

    public bool ContainsWorld(IWorld world)
    {
        if (world is not ManagedWorld managedWorld) return false;
        lock (_lock)
        {
            return _worlds.Contains(managedWorld);
        }
    }

    private void OnWorldDisposed(object? sender, EventArgs e)
    {
        if (sender is ManagedWorld world)
        {
            lock (_lock)
            {

                _worlds.Remove(world);
                if (!string.IsNullOrEmpty(world.Name))
                {
                    _worldsByName.Remove(world.Name);
                }
            }
        }
    }

    public bool DestroyWorld(string name)
    {
        lock (_lock)
        {
            if (_worldsByName.TryGetValue(name, out var world))
            {
                world.Dispose();
                return true;
            }

            return false;
        }
    }
}
