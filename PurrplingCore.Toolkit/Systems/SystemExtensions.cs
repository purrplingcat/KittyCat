using Friflo.Engine.ECS.Systems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PurrplingCore.Toolkit.Systems;

public static class SystemExtensions
{
    public static void Initialize(this SystemGroup root)
    {
        foreach (var child in root.ChildSystems)
        {
            if (child is SystemGroup subgroup)
            {
                subgroup.Initialize();
            }

            if (child is IInitializableSystem initializable)
            {
                initializable.Initialize();
            }
        }
    }
}
