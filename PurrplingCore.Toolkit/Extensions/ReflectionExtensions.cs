using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace PurrplingCore.Toolkit.Extensions;
public static class ReflectionExtensions
{
    public static bool IsGenerated(this Type type)
    {
        return type.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false)
            || type.Name.Contains("<>") // Common pattern for generated types
            || type.Name.Contains("AnonymousType"); // Anonymous types
    }
}
