using System.Diagnostics.CodeAnalysis;

namespace PurrplingCore.Ecs.WorldBuilding;

/// <summary>
/// Maps system group types to the entries registered for each group.
/// </summary>
public class SystemRegistry
{
    private readonly Dictionary<Type, SortedSystemSet> _groups = [];

    /// <summary>
    /// Gets or replaces the system set registered for a group type.
    /// </summary>
    public SortedSystemSet this[Type key]
    {
        get => _groups[key];
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _groups[key] = value;
        }
    }

    /// <summary>
    /// Gets the system set for a group type, creating it when absent.
    /// </summary>
    public SortedSystemSet GetOrCreate(Type key)
    {
        if (!_groups.TryGetValue(key, out var systemSet))
        {
            systemSet = new SortedSystemSet(key);
            _groups[key] = systemSet;
        }

        return systemSet;
    }

    /// <summary>
    /// Tries to get the system set registered for a group type.
    /// </summary>
    public bool TryGet(Type key, [MaybeNullWhen(false)] out SortedSystemSet systemSet)
    {
        return _groups.TryGetValue(key, out systemSet);
    }

    /// <summary>
    /// Creates a registry from system metadata entries.
    /// </summary>
    public static SystemRegistry Create(IEnumerable<SystemMetadata> systems)
    {
        var registry = new SystemRegistry();

        foreach (var metadata in systems)
        {
            var entry = new SortedSystemSet.SystemEntry(metadata.SystemType, metadata.Order)
            {
                Flags = metadata.Flags,
                RunBefore = [.. metadata.RunBefore],
                RunAfter = [.. metadata.RunAfter]
            };

            registry.GetOrCreate(metadata.GroupType).Add(entry);
        }

        return registry;
    }
}