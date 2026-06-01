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

    public static byte[] GetEmbeddedKey(string name)
    {
        var assembly = Assembly.GetExecutingAssembly();
        string resourceName = $"{assembly.GetName().Name}.Resources.{name}.key";
        using Stream stream = assembly.GetManifestResourceStream(resourceName) 
            ?? throw new Exception("Key not found!");
        
        byte[] key = new byte[stream.Length];
        stream.Read(key, 0, (int)stream.Length);
        return key;
    }
}
