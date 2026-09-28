using DotTiled;
using Friflo.Engine.ECS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KittyCat.Ecs.Components;

public struct MapContext : IComponent
{
    public Map Tilemap;
}
