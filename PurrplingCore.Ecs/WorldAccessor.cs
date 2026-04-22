namespace PurrplingCore.Ecs;

public interface IWorldAccessor
{
    ManagedWorld World { get; }
}
internal class WorldAccessor : IWorldAccessor
{
    private ManagedWorld? _world;
    public ManagedWorld World
    {
        get => _world ?? throw new InvalidOperationException("WorldContext is not initialized with a world.");
        internal set => _world = value;
    }
}
