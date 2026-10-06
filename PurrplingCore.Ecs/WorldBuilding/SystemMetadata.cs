using System.Collections.Concurrent;
using System.Reflection;
using Friflo.Engine.ECS.Systems;
using PurrplingCore.Ecs;
using PurrplingCore.Ecs.Attributes;
using PurrplingCore.Ecs.Systems;

namespace PurrplingCore.Ecs.WorldBuilding;

/// <summary>
/// Stores the system group, order, flags, and ordering attributes declared on a system type.
/// </summary>
public readonly record struct SystemMetadata
{
    private static readonly ConcurrentDictionary<Type, SystemMetadata> _metadataCache = new();

    /// <summary>
    /// Gets the system type described by this metadata.
    /// </summary>
    public required Type SystemType { get; init; }

    /// <summary>
    /// Gets the system group that contains the system.
    /// </summary>
    public required Type GroupType { get; init; }

    /// <summary>
    /// Gets the system types that must run after this system.
    /// </summary>
    public Type[] RunBefore { get; init; } = [];

    /// <summary>
    /// Gets the system types that must run before this system.
    /// </summary>
    public Type[] RunAfter { get; init; } = [];

    /// <summary>
    /// Gets the world marker types that target this system.
    /// </summary>
    public Type[] TargetWorlds { get; init; } = [];

    /// <summary>
    /// Gets the world flags required for this system to run.
    /// </summary>
    public WorldFlags Flags { get; init; }

    /// <summary>
    /// Gets the ordering category of the system.
    /// </summary>
    public SystemOrder Order { get; init; }

    /// <summary>
    /// Creates metadata with default system group and ordering values.
    /// </summary>
    public SystemMetadata(Type systemType)
    {
        SystemType = systemType;
        GroupType = typeof(UpdateSystemGroup);
        RunBefore = [];
        RunAfter = [];
        TargetWorlds = [];
        Flags = WorldFlags.None;
        Order = SystemOrder.Default;
    }

    private static SystemMetadata Create(Type type)
    {
        var groupAttr = type.GetCustomAttribute<SystemAttribute>();

        return new SystemMetadata
        {
            SystemType = type,
            GroupType = groupAttr?.GroupType ?? typeof(UpdateSystemGroup),
            Order = groupAttr?.Order ?? SystemOrder.Default,
            Flags = groupAttr?.Flags ?? WorldFlags.None,
            RunBefore = [.. type.GetCustomAttributes<RunBeforeAttribute>().Select(attribute => attribute.TargetType)],
            RunAfter = [.. type.GetCustomAttributes<RunAfterAttribute>().Select(attribute => attribute.TargetType)],
            TargetWorlds = [.. type.GetCustomAttributes<TargetWorldAttribute>().Select(attribute => attribute.WorldMarkerType)],
        };
    }

    /// <summary>
    /// Converts this metadata to a system registry entry.
    /// </summary>
    public SortedSystemSet.SystemEntry ToSystemEntry()
    {
        return new SortedSystemSet.SystemEntry(SystemType, Order)
        {
            RunBefore = [.. RunBefore],
            RunAfter = [.. RunAfter],
            Flags = Flags
        };
    }

    /// <summary>
    /// Gets cached metadata for the specified system type, creating it when needed.
    /// </summary>
    public static SystemMetadata For(Type systemType)
    {
        if (!_metadataCache.TryGetValue(systemType, out var metadata))
        {
            metadata = Create(systemType);
            _metadataCache[systemType] = metadata;
        }

        return metadata;
    }

    /// <summary>
    /// Gets cached metadata for the specified system type, creating it when needed.
    /// </summary>
    public static SystemMetadata For<TSystem>() where TSystem : BaseSystem
    {
        return For(typeof(TSystem));
    }
}