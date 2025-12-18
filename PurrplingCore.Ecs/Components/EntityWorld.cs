using Friflo.Engine.ECS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PurrplingCore.Ecs.Components;

internal readonly struct EntityWorld : IComponent
{
    public readonly World world;

    public EntityWorld(World world)
    {
        ArgumentNullException.ThrowIfNull(world);
        this.world = world;
    }
}
