using Friflo.Engine.ECS.Systems;
using System;

namespace KittyCat.Ecs;

public static class SystemRootExtensions
{
    public static SystemWorldBinding CreateBinding(this SystemRoot systemRoot, World world)
    {
        ArgumentException.ThrowIfNullOrEmpty(nameof(systemRoot));
        ArgumentException.ThrowIfNullOrEmpty(nameof(world));
        return new SystemWorldBinding(systemRoot, world);
    }
}
