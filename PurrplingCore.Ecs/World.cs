using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using PurrplingCore.Ecs.Systems;
using System.Runtime.CompilerServices;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PurrplingCore.Toolkit;

namespace PurrplingCore.Ecs;

/// <summary>
/// Represents the main ECS world, managing entities, systems, and update logic.
/// </summary>
public class World : IWorld, IDisposable
{
    private readonly EntityStore _store;
    private readonly UpdateSystemGroup _updateSystems = [];
    private readonly DrawSystemGroup _drawSystems = [];
    private readonly InitializeSystemGroup _initializeSystems = [];
    private readonly FixedUpdateSystemGroup _fixedUpdateSystems = [];
    private readonly SystemRoot _systemRoot;
    private readonly ILogger _logger;
    private UpdateTick _time;
    private bool _initialized;
    private bool _disposed;

    public string Name { get; set; } = string.Empty;
    public EntityStore Store => _store;
    public UpdateSystemGroup UpdateSystems => _updateSystems;
    public DrawSystemGroup DrawSystems => _drawSystems;
    public InitializeSystemGroup InitializeSystems => _initializeSystems;
    public FixedUpdateSystemGroup FixedUpdateSystems => _fixedUpdateSystems;

    public ref UpdateTick Time => ref _time;

    internal SystemRoot SystemRoot => _systemRoot;
    internal ILogger Logger => _logger;

    public IReadOnlyCollection<BaseSystem> Systems => _systemRoot.ChildSystems;

    public event EventHandler? Disposed;
    public event EventHandler? Initialized;
    public event Action<IWorld, UpdateTick>? Updated;
    public event Action<IWorld, UpdateTick>? Drawn;

    public World() : this(PidType.UsePidAsId) { }

    public World(PidType pidType, ILogger? logger = null)
    {
        _store = new EntityStore(pidType);
        _logger = logger ?? NullLogger<World>.Instance;
        _systemRoot = new WorldSystemRoot(this) {
            _initializeSystems,
            _fixedUpdateSystems,
            _updateSystems,
            _drawSystems, 
        };
        
        _store.EventRecorder.Enabled = true;
    }

    protected virtual void OnInitialize()
    {
    }

    protected virtual void OnDispose()
    {
    }

    public void Initialize()
    {
        if (_initialized) return;

        OnInitialize();
        _initializeSystems.Update(new UpdateTick());
        _initialized = true;

        Initialized?.Invoke(this, EventArgs.Empty);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void EnsureNotDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    public void Update(UpdateTick tick)
    {
        EnsureNotDisposed();
        if (!_initialized) { Initialize(); }

        _time = tick;
        _store.EventRecorder.ClearEvents();
        _fixedUpdateSystems.Update(tick);
        _updateSystems.Update(tick);
        Updated?.Invoke(this, tick);
    }

    public void Draw(UpdateTick tick)
    {
        EnsureNotDisposed();
        if (!_initialized) { Initialize(); }

        _drawSystems.Update(tick);
        Drawn?.Invoke(this, tick);
    }

    public SystemGroup? FindGroup(string name, bool recursive = true)
    {
        EnsureNotDisposed();
        return _systemRoot.FindGroup(name, recursive);
    }

    public T? FindSystem<T>(bool recursive = true) where T : BaseSystem
    {
        EnsureNotDisposed();
        return _systemRoot.FindSystem<T>(recursive);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                Initialized = null;
                Updated = null;
                Drawn = null;
                OnDispose();
            }

            Disposed?.Invoke(this, EventArgs.Empty);
            Disposed = null;
            _disposed = true;
        }
    }

    ~World()
    {
        Dispose(disposing: false);
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
