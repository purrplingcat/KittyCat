using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PurrplingCore.Ecs.DI;

public interface IWorldBuilder
{
    IServiceCollection Services { get; }
    WorldType WorldType { get; }
}
