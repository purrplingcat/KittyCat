using Microsoft.Extensions.DependencyInjection;
using PurrplingCore.Toolkit.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PurrplingCore.Toolkit.Hosting;

public interface IServiceModule
{
    public void ConfigureServices(IServiceCollection services);
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class GameModuleAttribute : Attribute
{
}
