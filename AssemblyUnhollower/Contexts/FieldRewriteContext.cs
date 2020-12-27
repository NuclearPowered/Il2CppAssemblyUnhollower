using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using AssemblyUnhollower.Extensions;
using Mono.Cecil;

namespace AssemblyUnhollower.Contexts
{
    public class FieldRewriteContext
    {
        public readonly TypeRewriteContext DeclaringType;
        public readonly FieldDefinition OriginalField;
        public readonly string UnmangledName;

        public readonly FieldReference PointerField;

        public FieldRewriteContext(TypeRewriteContext declaringType, FieldDefinition originalField)
        {
            DeclaringType = declaringType;
            OriginalField = originalField;

            UnmangledName = UnmangleFieldName(originalField);
            var pointerField = new FieldDefinition("NativeFieldInfoPtr_" + UnmangledName, FieldAttributes.Private | FieldAttributes.Static | FieldAttributes.InitOnly, declaringType.AssemblyContext.Imports.IntPtr);
            
            declaringType.NewType.Fields.Add(pointerField);
            
            PointerField = new FieldReference(pointerField.Name, pointerField.FieldType, DeclaringType.SelfSubstitutedRef);
        }

        private static readonly string[] MethodAccessTypeLabels = { "CompilerControlled", "Private", "FamAndAssem", "Internal", "Protected", "FamOrAssem", "Public" };

        private static readonly Regex LambdaFieldRegex = new Regex(@"^<([\w\d]+)>5__\d$", RegexOptions.Compiled);

        private string UnmangleFieldNameBase(FieldDefinition field)
        {
            if (!field.Name.IsInvalidInSource()) return field.Name;

            var match = LambdaFieldRegex.Match(field.Name);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }

            var accessModString = MethodAccessTypeLabels[(int) (field.Attributes & FieldAttributes.FieldAccessMask)];
            var staticString = field.IsStatic ? "_Static" : "";
            return "field_" + accessModString + staticString + "_" + DeclaringType.AssemblyContext.RewriteTypeRef(field.FieldType).GetUnmangledName();
        }

        private string UnmangleFieldName(FieldDefinition field)
        {
            if (!field.Name.IsInvalidInSource()) return field.Name;

            if (field.Name == "<>1__state")
            {
                return "__state";
            }

            if (field.Name == "<>2__current")
            {
                return "__current";
            }

            if (field.Name == "<>4__this")
            {
                return "__this";
            }

            return UnmangleFieldNameBase(field) + "_" +
                   field.DeclaringType.Fields.Where(it => FieldsHaveSameSignature(field, it)).TakeWhile(it => it != field).Count();
        }

        private static bool FieldsHaveSameSignature(FieldDefinition fieldA, FieldDefinition fieldB)
        {
            if ((fieldA.Attributes & FieldAttributes.FieldAccessMask) !=
                (fieldB.Attributes & FieldAttributes.FieldAccessMask))
                return false;

            if (fieldA.IsStatic != fieldB.IsStatic) return false;

            return fieldA.FieldType.UnmangledNamesMatch(fieldB.FieldType);
        }
    }
}