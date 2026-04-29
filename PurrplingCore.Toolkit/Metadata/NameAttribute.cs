using System.Reflection;
using System.Runtime.CompilerServices;

namespace PurrplingCore.Toolkit.Metadata;

[AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
public sealed class NameAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}

public static class MetadataExtensions
{
    public static string GetDisplayName(this Type type)
    {
        return type.GetCustomAttribute<NameAttribute>()?.Name ?? type.Name;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetDisplayName(this object obj)
    {
        return obj.GetType().GetDisplayName();
    }
}
