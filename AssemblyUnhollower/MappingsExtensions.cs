using System.Linq;
using AssemblyUnhollower.Contexts;
using Mono.Cecil;

namespace AssemblyUnhollower
{
    public static class MappingsExtensions
    {
        public static CustomAttribute? GetCustomAttribute(this ICustomAttributeProvider cap, string attribute)
        {
            if (!cap.HasCustomAttributes)
            {
                return null;
            }

            return cap.CustomAttributes.FirstOrDefault(attrib => attrib.AttributeType.FullName == attribute);
        }

        public static string? GetMapped(this ICustomAttributeProvider cap)
        {
            var mappedAttribute = cap.GetCustomAttribute("MappedAttribute");
            return mappedAttribute?.ConstructorArguments.Single().Value as string;
        }

        public static void AddObfuscatedName(this ICustomAttributeProvider cap, AssemblyRewriteContext assemblyContext, string originalName)
        {
            cap.CustomAttributes.Add(new CustomAttribute(assemblyContext.Imports.ObfuscatedNameAttributeCtor)
            {
                ConstructorArguments = { new CustomAttributeArgument(assemblyContext.Imports.String, originalName) }
            });
        }

        public static void AddObfuscatedName(this ICustomAttributeProvider cap, AssemblyRewriteContext assemblyContext, string originalName, string? newName)
        {
            if (newName != null && originalName != newName)
            {
                cap.AddObfuscatedName(assemblyContext, originalName);
            }
        }
    }
}
