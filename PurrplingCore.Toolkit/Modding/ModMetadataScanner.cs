using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace PurrplingCore.Toolkit.Modding;

internal class ModMetadataScanner(ILogger logger)
{
    public string? FindEntryPoint(string dllPath)
    {
        try
        {
            using var stream = File.OpenRead(dllPath);
            using var peReader = new PEReader(stream);

            if (!peReader.HasMetadata) return null;

            var reader = peReader.GetMetadataReader();

            foreach (var typeDefHandle in reader.TypeDefinitions)
            {
                var typeDef = reader.GetTypeDefinition(typeDefHandle);

                if (typeDef.Attributes.HasFlag(TypeAttributes.Abstract) ||
                    typeDef.Attributes.HasFlag(TypeAttributes.Interface))
                {
                    continue;
                }

                foreach (var customAttributeHandle in typeDef.GetCustomAttributes())
                {
                    var customAttribute = reader.GetCustomAttribute(customAttributeHandle);

                    if (IsModEntryAttribute(reader, customAttribute.Constructor))
                    {
                        string ns = reader.GetString(typeDef.Namespace);
                        string name = reader.GetString(typeDef.Name);

                        return string.IsNullOrEmpty(ns) ? name : $"{ns}.{name}";
                    }
                }
            }
        }
        catch (BadImageFormatException ex)
        {
            logger.LogError(ex, "Invalid DLL: {Dll}", dllPath);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while reading metadata: {Dll}", dllPath);
            return null;
        }

        return null;
    }

    private static bool IsModEntryAttribute(MetadataReader reader, EntityHandle constructorHandle)
    {
        // Referenced ModEntryAttribute (usually PurrplingCore definition)
        if (constructorHandle.Kind == HandleKind.MemberReference)
        {
            var memberRef = reader.GetMemberReference((MemberReferenceHandle)constructorHandle);
            var parentHandle = memberRef.Parent;

            if (parentHandle.Kind == HandleKind.TypeReference)
            {
                var typeRef = reader.GetTypeReference((TypeReferenceHandle)parentHandle);
                string attributeName = reader.GetString(typeRef.Name);

                return attributeName == nameof(ModEntryAttribute);
            }
        }

        // In-mod declared ModEntryAttribute
        else if (constructorHandle.Kind == HandleKind.MethodDefinition)
        {
            var methodDef = reader.GetMethodDefinition((MethodDefinitionHandle)constructorHandle);
            var declaringTypeHandle = methodDef.GetDeclaringType();
            var typeDef = reader.GetTypeDefinition(declaringTypeHandle);

            string attributeName = reader.GetString(typeDef.Name);
            return attributeName == nameof(ModEntryAttribute);
        }

        return false;
    }
}
