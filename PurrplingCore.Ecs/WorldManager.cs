using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PurrplingCore.Ecs.Diagnostics;
using PurrplingCore.Toolkit.Extensions;

namespace PurrplingCore.Ecs;

public class WorldManager(IServiceProvider provider, IWorldFactory defaultFactory, ILogger<WorldManager>? logger)
{
    private readonly Dictionary<string, ManagedWorld> _worldsByName = [];
    private readonly List<ManagedWorld> _worlds = [];
    private readonly ILogger<WorldManager> _logger = logger ?? NullLogger<WorldManager>.Instance;
    private readonly object _lock = new();

    public IReadOnlyCollection<ManagedWorld> Worlds => _worlds.AsReadOnly();

    public ManagedWorld? GetWorld(string name)
    {
        lock (_lock)
        {
            return _worldsByName.GetValueOrDefault(name);
        }
    }

    public IEnumerable<ManagedWorld> GetWorlds(WorldSignature tag)
    {
        lock (_lock)
        {
            return _worlds.Where(w => w.Signature == tag);
        }
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
            world.Destroyed += OnWorldDisposed;
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
