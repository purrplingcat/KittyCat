using Friflo.Engine.ECS;
using PurrplingCore.Ecs;

namespace PurrplingCore.Ecs.WorldBuilding;

/// <summary>
/// Creates worlds from a name and world signature.
/// </summary>
public interface IWorldFactory
{
    /// <summary>
    /// Creates a world with the specified name and signature.
    /// </summary>
    World CreateWorld(string defaultWorldName, WorldSignature signature);
}