using System;
using System.Linq;

namespace PurrplingCore.Toolkit.DI;

public static class TypeExtensions
{
    public static bool DirectlyImplements(this Type testType, Type interfaceType)
    {
        if (!interfaceType.IsInterface || !testType.IsClass || !interfaceType.IsAssignableFrom(testType)) { return false; }
        return !testType.GetInterfaces().Any(i => i != interfaceType && interfaceType.IsAssignableFrom(i));
    }

    public static string GetFriendlyName(this Type type)
    {
        if (type == typeof(int))
            return "int";
        else if (type == typeof(short))
            return "short";
        else if (type == typeof(byte))
            return "byte";
        else if (type == typeof(bool))
            return "bool";
        else if (type == typeof(long))
            return "long";
        else if (type == typeof(float))
            return "float";
        else if (type == typeof(double))
            return "double";
        else if (type == typeof(decimal))
            return "decimal";
        else if (type == typeof(string))
            return "string";
        else if (type.IsGenericType)
            return type.Name.Split('`')[0] + "<" + string.Join(", ", type.GetGenericArguments().Select(x => x.GetFriendlyName()).ToArray()) + ">";
        else
            return type.Name;
    }
}
