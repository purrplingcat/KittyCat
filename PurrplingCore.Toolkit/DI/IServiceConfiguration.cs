using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PurrplingCore.Toolkit.DI;

public interface IServiceConfiguration
{
    //object? Key { get; }
    public void Configure(IServiceCollection services);
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class ServiceConfiguration : Attribute
{
}
