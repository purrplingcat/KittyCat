using PurrplingCore.Toolkit.Metadata;

namespace PurrplingCore.Ecs;

public interface IWorldMarker;

[Name("Default")]
public struct DefaultWorld : IWorldMarker;

public sealed class WorldType : IEquatable<WorldType>
{
    private struct NoneWorld : IWorldMarker;

    private static class TypeCache<T> where T : IWorldMarker
    {
        public static readonly WorldType Instance = new(typeof(T), typeof(T).GetDisplayName());
    }

    public static WorldType Default { get; } = For<DefaultWorld>();
    public static WorldType None { get; } = new WorldType(typeof(NoneWorld), "None");

    public string Name { get; }
    public Type MarkerType { get; }
    public Guid GUID => MarkerType.GUID;

    private WorldType(Type markerType, string id)
    {        
        ArgumentNullException.ThrowIfNull(markerType);
        MarkerType = markerType;
        Name = id;
    }

    public static WorldType For<T>() where T : IWorldMarker
        => TypeCache<T>.Instance;

    public bool Is<T>() where T : IWorldMarker
    {
        return MarkerType == typeof(T);
    }

    public string GetQualifiedName()
    {
        return $"{Name}#{GUID}";
    }

    public bool Equals(WorldType? other) => ReferenceEquals(this, other);
    public override bool Equals(object? obj) => Equals(obj as WorldType);
    public override int GetHashCode() => MarkerType.GetHashCode();
    public override string ToString() => Name;

    public static bool operator ==(WorldType? left, WorldType? right) => Equals(left, right);
    public static bool operator !=(WorldType? left, WorldType? right) => !Equals(left, right);
}
