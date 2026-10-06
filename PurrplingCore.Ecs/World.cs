using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PurrplingCore.Ecs.Extensions;
using PurrplingCore.Ecs.Systems;
using PurrplingCore.Toolkit.Attributes;
using System.Runtime.CompilerServices;

namespace PurrplingCore.Ecs;

/// <summary>
/// Represents the main ECS world, managing entities, systems, and update logic.
/// </summary>
public class World : IWorld, IDisposable
{
    private readonly EntityStore _store;
    private InitializeSystemGroup _initializeSystems = null!;
    private FixedUpdateSystemGroup _fixedUpdateSystems = null!;
    private UpdateSystemGroup _updateSystems = null!;
    private DrawSystemGroup _drawSystems = null!;
    private readonly WorldSystemRoot _systemRoot;
    private readonly ILogger _logger;
    private UpdateTick _time;
    private bool _initialized;
    private bool _disposed;
    private bool _doFixedUpdate;

    /// <summary>
    /// Gets or sets the world's display name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets the signature that identifies this world.
    /// </summary>
    public WorldSignature Signature { get; }

    /// <summary>
    /// Gets the entity store owned by this world.
    /// </summary>
    public EntityStore Store => _store;
    internal UpdateSystemGroup UpdateSystems => _updateSystems;
    internal DrawSystemGroup DrawSystems => _drawSystems;
    internal InitializeSystemGroup InitializeSystems => _initializeSystems;
    internal FixedUpdateSystemGroup FixedUpdateSystems => _fixedUpdateSystems;

    /// <summary>
    /// Gets the current update tick.
    /// </summary>
    public ref UpdateTick Time => ref _time;

    internal SystemRoot SystemRoot => _systemRoot;
    internal ILogger Logger => _logger;

    /// <summary>
    /// Gets the systems attached to the world.
    /// </summary>
    public IReadOnlyCollection<BaseSystem> Systems => _systemRoot.ChildSystems;

    /// <summary>
    /// Occurs when the world is disposed.
    /// </summary>
    public event EventHandler? Disposed;

    /// <summary>
    /// Occurs when the world is initialized.
    /// </summary>
    public event EventHandler? Initialized;

    /// <summary>
    /// Occurs after the world's systems have been updated.
    /// </summary>
    public event Action<IWorld, UpdateTick>? Updated;

    /// <summary>
    /// Occurs after the world's systems have been drawn.
    /// </summary>
    public event Action<IWorld, UpdateTick>? Drawn;

    /// <summary>
    /// Initializes a world without a marker signature.
    /// </summary>
    public World() : this(PidType.UsePidAsId, WorldSignature.None) { }

    /// <summary>
    /// Initializes a world with the specified entity ID strategy and signature.
    /// </summary>
    public World(PidType pidType, WorldSignature signature, ILogger? logger = null)
    {
        _store = new EntityStore(pidType);
        _logger = logger ?? NullLogger<World>.Instance;
        _systemRoot = new WorldSystemRoot(this);

        Signature = signature;
        _store.EventRecorder.Enabled = true;
    }

    /// <summary>
    /// Runs custom initialization after the world's core system groups are created.
    /// </summary>
    protected virtual void OnInitialize()
    {
    }

    /// <summary>
    /// Runs custom cleanup while the world is being disposed.
    /// </summary>
    protected virtual void OnDispose()
    {
    }

    /// <summary>
    /// Initializes the world and its initialization systems once.
    /// </summary>
    public void Initialize()
    {
        if (_initialized) return;

        EnsureNotDisposed();
        EnsureCoreGroups();

        OnInitialize();
        _initializeSystems.Update(new UpdateTick());

        _doFixedUpdate = _fixedUpdateSystems.ChildSystems.Count > 0;
        _fixedUpdateSystems.Enabled = _doFixedUpdate;
        _initialized = true;
        Initialized?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Clears elapsed time accumulated toward the next fixed update.
    /// </summary>
    /// <remarks>
    /// Call after a discontinuity in simulation time, such as resuming after world updates were suspended.
    /// Call on the same thread as <see cref="Update(UpdateTick)"/>, outside an active update.
    /// Has no effect before initialization or when the fixed update group has no systems.
    /// </remarks>
    public void ResetFixedStep()
    {
        EnsureNotDisposed();
        if (!_initialized) return;

        _fixedUpdateSystems.Reset();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureCoreGroups()
    {
        _initializeSystems = _systemRoot.GetOrAddGroup<InitializeSystemGroup>();
        _fixedUpdateSystems = _systemRoot.GetOrAddGroup<FixedUpdateSystemGroup>();
        _updateSystems = _systemRoot.GetOrAddGroup<UpdateSystemGroup>();
        _drawSystems = _systemRoot.GetOrAddGroup<DrawSystemGroup>();
    }

    /// <summary>
    /// Throws if this world has already been disposed.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void EnsureNotDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    /// <summary>
    /// Advances the world's fixed-update and update systems for the specified tick.
    /// </summary>
    [Hot]
    public void Update(UpdateTick tick)
    {
        EnsureNotDisposed();
        if (!_initialized) { Initialize(); }

        _time = tick;
        _store.EventRecorder.ClearEvents();
        if (_doFixedUpdate)
        {
            _fixedUpdateSystems.FixedUpdate(tick);
        }
        _updateSystems.Update(tick);
        Updated?.Invoke(this, tick);
    }

    /// <summary>
    /// Runs the world's draw systems for the specified tick.
    /// </summary>
    [Hot]
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

    /// <summary>
    /// Finds a system group by name.
    /// </summary>
    public SystemGroup? FindGroup(string name, bool recursive = true)
    {
        EnsureNotDisposed();
        return _systemRoot.FindGroup(name, recursive);
    }

    /// <summary>
    /// Finds a system by its type.
    /// </summary>
    public T? FindSystem<T>(bool recursive = true) where T : BaseSystem
    {
        EnsureNotDisposed();
        return _systemRoot.FindSystem<T>(recursive);
    }

    internal void SetSystems(IEnumerable<BaseSystem> systems)
    {
        if (_initialized)
        {
            throw new InvalidOperationException(
                "Cannot set systems after the World has been initialized. " +
                "Call SetSystems() before calling Initialize()."
            );
        }

        lock (_systemRoot)
        {
            _systemRoot.Clear();
            _systemRoot.Add(systems);
        }
    }

    /// <summary>
    /// Releases managed resources owned by the world.
    /// </summary>
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

    /// <summary>
    /// Releases the world when it is finalized.
    /// </summary>
    ~World()
    {
        Dispose(disposing: false);
    }

    /// <summary>
    /// Releases the world and its owned resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
