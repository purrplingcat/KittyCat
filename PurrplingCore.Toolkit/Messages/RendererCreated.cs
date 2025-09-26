using PurrplingCore.Toolkit.Messaging;
using PurrplingCore.Toolkit.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PurrplingCore.Toolkit.Messages;

public readonly struct RendererCreated(IRenderer renderer) : IMessage
{
    public readonly IRenderer Renderer = renderer;
    public bool IsEmpty => Renderer is null;
}
