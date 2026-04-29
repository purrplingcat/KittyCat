using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using PurrplingCore.Ecs.Systems;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

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
    private bool _doFixedUpdate;

    public string Name { get; set; } = string.Empty;
    public EntityStore Store => _store;
    internal UpdateSystemGroup UpdateSystems => _updateSystems;
    internal DrawSystemGroup DrawSystems => _drawSystems;
    internal InitializeSystemGroup InitializeSystems => _initializeSystems;
    internal FixedUpdateSystemGroup FixedUpdateSystems => _fixedUpdateSystems;

    public ref UpdateTick Time => ref _time;

    internal SystemRoot SystemRoot => _systemRoot;
    internal ILogger Logger => _logger;

    public IReadOnlyCollection<BaseSystem> Systems => _systemRoot.ChildSystems;

    public event EventHandler? Destroyed;
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

        _systemRoot.OnSystemChanged += OnSystemRootChanged;
        _store.EventRecorder.Enabled = true;
    }

    private void OnSystemRootChanged(SystemChanged changed)
    {
        if (!_initialized) return;

        string systemName = changed.system.Name;
        string actionName = changed.action.ToString().ToLower();

        if (changed.field != null)
        {
            actionName += $" field ${changed.field} in";
        }

        throw new InvalidOperationException(
            $"World topology is locked. Cannot {actionName} system '{systemName}' after the World has been initialized."
        );
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
        _doFixedUpdate = _fixedUpdateSystems.ChildSystems.Count > 0;
        _fixedUpdateSystems.Enabled = _doFixedUpdate;
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
        if (_doFixedUpdate)
        {
            _fixedUpdateSystems.Update(tick);
        }
        _updateSystems.Update(tick);
        Updated?.Invoke(this, tick);
    }

    public void Draw(UpdateTick tick)
    {
        EnsureNotDisposed();
        if (!_initialized) 
        {
            throw new InvalidOperationException(
                "Cannot call Draw before the World has been initialized. " +
                "Call Initialize() or ensure that Update() is called at least once before Draw()."
            );
        }

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

            Destroyed?.Invoke(this, EventArgs.Empty);
            Destroyed = null;
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
