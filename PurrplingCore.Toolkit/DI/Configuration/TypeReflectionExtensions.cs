using System.Reflection;

namespace PurrplingCore.Toolkit.DI.Configuration;

public static class TypeReflectionExtensions
{
    public static HashSet<Type> GetNonGenericInterfaces(this Type type)
    {
        return [.. type.GetInterfaces().Where(x => !x.IsGenericType && !x.IsGenericTypeDefinition)];
    }

    public static Type[] GetServiceTypeAliases(this Type implementationType, bool withInterfaces)
    {
        var attrs = implementationType.GetCustomAttributes<AliasAttribute>();
        var aliases = new HashSet<Type>(attrs.SelectMany(attr => attr.Aliases));

        if (withInterfaces)
        {
            aliases.UnionWith(implementationType.GetNonGenericInterfaces());
        }

        return [.. aliases];
    }
}
