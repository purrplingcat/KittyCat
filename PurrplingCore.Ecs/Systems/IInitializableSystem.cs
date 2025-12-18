using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PurrplingCore.Ecs.Systems;

public interface IInitializableSystem
{
    void Initialize();
}
