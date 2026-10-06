using Friflo.Engine.ECS;
using PurrplingCore.Ecs;

namespace PurrplingCore.Ecs.WorldBuilding;

/// <summary>
/// Provides convenience overloads for creating worlds from signatures and marker types.
/// </summary>
public static class WorldFactoryExtensions
{
    /// <summary>
    /// Creates a world with a generated name and the specified signature.
    /// </summary>
    public static World CreateWorld(this IWorldFactory factory, WorldSignature signature)
    {
        string randomName = $"World_{Guid.NewGuid().ToString("N")[..12]}";
        return factory.CreateWorld(randomName, signature);
    }

    /// <summary>
    /// Creates a world with the specified name and marker type.
    /// </summary>
    public static World CreateWorld<T>(this IWorldFactory factory, string name) where T : struct, IWorldMarker
    {
        return factory.CreateWorld(name, WorldSignature.For<T>());
    }

    /// <summary>
    /// Creates a world with a generated name and the specified marker type.
    /// </summary>
    public static World CreateWorld<T>(this IWorldFactory factory) where T : struct, IWorldMarker
    {
        return factory.CreateWorld(WorldSignature.For<T>());
    }

    /// <summary>
    /// Creates a world with the specified marker type and flags.
    /// </summary>
    public static World CreateWorld<T>(this IWorldFactory factory, WorldFlags flags) where T : struct, IWorldMarker
    {
        return factory.CreateWorld(WorldSignature.For<T>().WithFlags(flags));
    }

    /// <summary>
    /// Creates a world with the specified name, marker type, and flags.
    /// </summary>
    public static World CreateWorld<T>(this IWorldFactory factory, string name, WorldFlags flags) where T : struct, IWorldMarker
    {
        return factory.CreateWorld(name, WorldSignature.For<T>().WithFlags(flags));
    }
}