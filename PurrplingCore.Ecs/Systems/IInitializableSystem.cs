using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PurrplingCore.Ecs.Systems;

/// <summary>
/// Defines a system with an explicit initialization step.
/// </summary>
public interface IInitializableSystem
{
    /// <summary>
    /// Initializes the system before it begins normal updates.
    /// </summary>
    void Initialize();
}
