using Friflo.Engine.ECS;
using PurrplingCore.Toolkit.Metadata;

namespace PurrplingCore.Ecs;

public interface IWorldMarker;

[Name("Default")]
public struct DefaultWorld : IWorldMarker;

[Flags]
public enum WorldFlags
{
    None = 0,
    Game = GameClient | GameServer,
    GameClient = 1 << 0,
    GameServer = 1 << 1,
    Simulation = 1 << 2,    
}

public readonly struct WorldSignature : IEquatable<WorldSignature>
{
    public static WorldSignature Default { get; } = For<DefaultWorld>();
    public static WorldSignature None { get; } = default;

    private static class Cache<T> where T : IWorldMarker
    {
        public static readonly WorldSignature Signature = new(typeof(T), typeof(T).GetDisplayName());
    }

    public string Name { get; }
    public Type MarkerType { get; }
    public WorldFlags Flags { get; }
    public Guid GUID => MarkerType?.GUID ?? Guid.Empty;

    private WorldSignature(Type markerType, string name, WorldFlags flags = WorldFlags.None)
    {        
        ArgumentNullException.ThrowIfNull(markerType);
        MarkerType = markerType;
        Name = name;
        Flags = flags;
    }

    public static WorldSignature For<T>() where T : IWorldMarker
    {
        return Cache<T>.Signature;
    }

    public static WorldSignature For<T>(WorldFlags flags) where T : IWorldMarker
    {
        return For<T>().WithFlags(flags);
    }

    public bool Is<T>() where T : IWorldMarker
    {
        return MarkerType == typeof(T);
    }

    public WorldSignature AddFlags(WorldFlags flags)
    {
        return new WorldSignature(MarkerType, Name, Flags | flags);
    }

    public WorldSignature RemoveFlags(WorldFlags flags)
    {
        return new WorldSignature(MarkerType, Name, Flags & ~flags);
    }

    public WorldSignature WithFlags(WorldFlags flags)
    {
        return new WorldSignature(MarkerType, Name, flags);
    }

    public bool HasFlags(WorldFlags flags)
    {
        return (Flags & flags) == flags;
    }

    public bool HasAnyFlag(WorldFlags systemFlags)
    {
        if (systemFlags == WorldFlags.None)
            return true;

        return (Flags & systemFlags) != 0;
    }

    public bool Equals(WorldSignature other)
    {
        return Flags == other.Flags 
            && ReferenceEquals(MarkerType, other.MarkerType);
    }

    public override int GetHashCode() => MarkerType?.GetHashCode() ?? 0;
    public override string ToString() => Name;

    public static bool operator ==(WorldSignature? left, WorldSignature? right) => Equals(left, right);
    public static bool operator !=(WorldSignature? left, WorldSignature? right) => !Equals(left, right);

    public override bool Equals(object? obj)
    {
        return obj is WorldSignature signature && Equals(signature);
    }
}
