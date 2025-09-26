using Friflo.Engine.ECS.Systems;
using PurrplingCore.Toolkit.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PurrplingCore.Toolkit.Messages;

public readonly struct SystemCreated(BaseSystem system) : IMessage
{
    public readonly BaseSystem System = system;

    public bool IsEmpty => System is null;
}
