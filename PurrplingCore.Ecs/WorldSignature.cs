using Friflo.Engine.ECS;
using PurrplingCore.Toolkit.Metadata;

namespace PurrplingCore.Ecs;

/// <summary>
/// Identifies a world category using a marker type.
/// </summary>
public interface IWorldMarker;

/// <summary>
/// Marker type for the default world.
/// </summary>
[Name("Default")]
public struct DefaultWorld : IWorldMarker;

/// <summary>
/// Describes capabilities or roles associated with a world.
/// </summary>
[Flags]
public enum WorldFlags
{
    /// <summary>
    /// No flags are assigned.
    /// </summary>
    None = 0,

    /// <summary>
    /// Combines client and server game worlds.
    /// </summary>
    Game = GameClient | GameServer,

    /// <summary>
    /// Identifies a game client world.
    /// </summary>
    GameClient = 1 << 0,

    /// <summary>
    /// Identifies a game server world.
    /// </summary>
    GameServer = 1 << 1,

    /// <summary>
    /// Identifies a simulation world.
    /// </summary>
    Simulation = 1 << 2,    
}

/// <summary>
/// Identifies a world by its marker type and associated flags.
/// </summary>
public readonly struct WorldSignature : IEquatable<WorldSignature>
{
    /// <summary>
    /// Gets the signature for the default world.
    /// </summary>
    public static WorldSignature Default { get; } = For<DefaultWorld>();

    /// <summary>
    /// Gets the empty world signature.
    /// </summary>
    public static WorldSignature None { get; } = default;

    private static class Cache<T> where T : IWorldMarker
    {
        public static readonly WorldSignature Signature = new(typeof(T), typeof(T).GetDisplayName());
    }

    /// <summary>
    /// Gets the display name of the world marker.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the marker type that identifies the world category.
    /// </summary>
    public Type MarkerType { get; }

    /// <summary>
    /// Gets the flags assigned to the world.
    /// </summary>
    public WorldFlags Flags { get; }

    /// <summary>
    /// Gets the GUID associated with the marker type.
    /// </summary>
    public Guid GUID => MarkerType?.GUID ?? Guid.Empty;

    private WorldSignature(Type markerType, string name, WorldFlags flags = WorldFlags.None)
    {        
        ArgumentNullException.ThrowIfNull(markerType);
        MarkerType = markerType;
        Name = name;
        Flags = flags;
    }

    /// <summary>
    /// Creates a signature for the specified world marker type.
    /// </summary>
    public static WorldSignature For<T>() where T : IWorldMarker
    {
        return Cache<T>.Signature;
    }

    /// <summary>
    /// Creates a signature for the specified world marker type with the given flags.
    /// </summary>
    public static WorldSignature For<T>(WorldFlags flags) where T : IWorldMarker
    {
        return For<T>().WithFlags(flags);
    }

    /// <summary>
    /// Determines whether this signature uses the specified marker type.
    /// </summary>
    public bool Is<T>() where T : IWorldMarker
    {
        return MarkerType == typeof(T);
    }

    /// <summary>
    /// Returns a copy of this signature with the specified flags added.
    /// </summary>
    public WorldSignature AddFlags(WorldFlags flags)
    {
        return new WorldSignature(MarkerType, Name, Flags | flags);
    }

    /// <summary>
    /// Returns a copy of this signature with the specified flags removed.
    /// </summary>
    public WorldSignature RemoveFlags(WorldFlags flags)
    {
        return new WorldSignature(MarkerType, Name, Flags & ~flags);
    }

    /// <summary>
    /// Returns a copy of this signature with exactly the specified flags.
    /// </summary>
    public WorldSignature WithFlags(WorldFlags flags)
    {
        return new WorldSignature(MarkerType, Name, flags);
    }

    /// <summary>
    /// Determines whether all specified flags are present.
    /// </summary>
    public bool HasFlags(WorldFlags flags)
    {
        return (Flags & flags) == flags;
    }

    /// <summary>
    /// Determines whether any specified flag is present.
    /// </summary>
    public bool HasAnyFlag(WorldFlags systemFlags)
    {
        if (systemFlags == WorldFlags.None)
            return true;

        return (Flags & systemFlags) != 0;
    }

    /// <summary>
    /// Determines whether this signature equals another signature.
    /// </summary>
    public bool Equals(WorldSignature other)
    {
        return Flags == other.Flags 
            && ReferenceEquals(MarkerType, other.MarkerType);
    }

    /// <summary>
    /// Returns a hash code for this signature.
    /// </summary>
    public override int GetHashCode() => HashCode.Combine(MarkerType, Flags);

    /// <summary>
    /// Returns the display name of the world marker.
    /// </summary>
    public override string ToString() => Name;

    /// <summary>
    /// Determines whether two nullable signatures are equal.
    /// </summary>
    public static bool operator ==(WorldSignature? left, WorldSignature? right) => Equals(left, right);

    /// <summary>
    /// Determines whether two nullable signatures are different.
    /// </summary>
    public static bool operator !=(WorldSignature? left, WorldSignature? right) => !Equals(left, right);

    /// <summary>
    /// Determines whether the specified object is an equal world signature.
    /// </summary>
    public override bool Equals(object? obj)
    {
        return obj is WorldSignature signature && Equals(signature);
    }
}
