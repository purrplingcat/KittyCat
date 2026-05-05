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
    private static readonly ConditionalWeakTable<Type, string> _displayNameCache = [];

    public static string GetDisplayName(this Type type)
    {
        if (!_displayNameCache.TryGetValue(type, out var displayName))
        {
            displayName = type.GetCustomAttribute<NameAttribute>()?.Name ?? type.Name;
            _displayNameCache.Add(type, displayName);
        }

        return displayName;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetDisplayName(this object obj)
    {
        return obj.GetType().GetDisplayName();
    }
}
