namespace asn1;

internal static class EcnBuiltInClassCatalog
{
    internal static readonly EcnBuiltInClassDefinition[] Definitions =
        CreateDefinitions();

    internal static EcnBuiltInClassDefinition[] GetClasses()
    {
        EcnBuiltInClassDefinition[] result =
            new EcnBuiltInClassDefinition[Definitions.Length];
        int index = 0;
        while (index < Definitions.Length)
        {
            result[index] = Definitions[index];
            index = index + 1;
        }

        return result;
    }

    internal static EcnBuiltInClassDefinition? FindClass(string name)
    {
        if (name == null)
        {
            throw new System.ArgumentNullException("name");
        }

        int index = 0;
        while (index < Definitions.Length)
        {
            if (Definitions[index].Name == name)
            {
                return Definitions[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal static bool IsInGroup(
        EcnClassCategory category,
        EcnClassCategoryGroup group)
    {
        EcnClassCategoryGroup[] groups = GetGroupsForCategory(category);
        int index = 0;
        while (index < groups.Length)
        {
            if (groups[index] == group)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static EcnClassCategoryGroup[] GetGroupsForCategory(
        EcnClassCategory category)
    {
        switch (category)
        {
            case EcnClassCategory.BitString:
            case EcnClassCategory.Boolean:
            case EcnClassCategory.CharacterString:
            case EcnClassCategory.EncodingStructure:
            case EcnClassCategory.Integer:
            case EcnClassCategory.Null:
            case EcnClassCategory.ObjectIdentifier:
            case EcnClassCategory.OctetString:
            case EcnClassCategory.OpenType:
            case EcnClassCategory.Padding:
            case EcnClassCategory.Real:
            case EcnClassCategory.Tag:
            case EcnClassCategory.Time:
                return [EcnClassCategoryGroup.BitField];
            case EcnClassCategory.Alternatives:
            case EcnClassCategory.Concatenation:
            case EcnClassCategory.Repetition:
                return [EcnClassCategoryGroup.EncodingConstructor];
            case EcnClassCategory.EncodingProcedure:
                return [EcnClassCategoryGroup.EncodingProcedure];
            default:
                return [];
        }
    }

    internal static EcnBuiltInClassDefinition[] CreateDefinitions()
    {
        return
        [
            CreateDefinition("#ALTERNATIVES", "", "#ALTERNATIVES", EcnClassCategory.Alternatives, false),
            CreateDefinition("#CHOICE", "#ALTERNATIVES", "#ALTERNATIVES", EcnClassCategory.Alternatives, false),
            CreateDefinition("#BITS", "", "#BITS", EcnClassCategory.BitString, false),
            CreateDefinition("#BIT-STRING", "#BITS", "#BITS", EcnClassCategory.BitString, false),
            CreateDefinition("#BOOL", "", "#BOOL", EcnClassCategory.Boolean, false),
            CreateDefinition("#BOOLEAN", "#BOOL", "#BOOL", EcnClassCategory.Boolean, false),
            CreateDefinition("#CHARACTER-STRING", "", "#CHARACTER-STRING", EcnClassCategory.EncodingStructure, true),
            CreateDefinition("#EMBEDDED-PDV", "", "#EMBEDDED-PDV", EcnClassCategory.EncodingStructure, true),
            CreateDefinition("#EXTERNAL", "", "#EXTERNAL", EcnClassCategory.EncodingStructure, true),
            CreateDefinition("#CHARS", "", "#CHARS", EcnClassCategory.CharacterString, false),
            CreateDefinition("#GeneralizedTime", "#CHARS", "#CHARS", EcnClassCategory.CharacterString, false),
            CreateDefinition("#UTCTime", "#CHARS", "#CHARS", EcnClassCategory.CharacterString, false),
            CreateDefinition("#ObjectDescriptor", "#CHARS", "#CHARS", EcnClassCategory.CharacterString, false),
            CreateDefinition("#BMPString", "#CHARS", "#CHARS", EcnClassCategory.CharacterString, false),
            CreateDefinition("#GeneralString", "#CHARS", "#CHARS", EcnClassCategory.CharacterString, false),
            CreateDefinition("#GraphicString", "#CHARS", "#CHARS", EcnClassCategory.CharacterString, false),
            CreateDefinition("#IA5String", "#CHARS", "#CHARS", EcnClassCategory.CharacterString, false),
            CreateDefinition("#NumericString", "#CHARS", "#CHARS", EcnClassCategory.CharacterString, false),
            CreateDefinition("#PrintableString", "#CHARS", "#CHARS", EcnClassCategory.CharacterString, false),
            CreateDefinition("#TeletexString", "#CHARS", "#CHARS", EcnClassCategory.CharacterString, false),
            CreateDefinition("#UniversalString", "#CHARS", "#CHARS", EcnClassCategory.CharacterString, false),
            CreateDefinition("#UTF8String", "#CHARS", "#CHARS", EcnClassCategory.CharacterString, false),
            CreateDefinition("#VideotexString", "#CHARS", "#CHARS", EcnClassCategory.CharacterString, false),
            CreateDefinition("#VisibleString", "#CHARS", "#CHARS", EcnClassCategory.CharacterString, false),
            CreateDefinition("#CONCATENATION", "", "#CONCATENATION", EcnClassCategory.Concatenation, false),
            CreateDefinition("#SEQUENCE", "#CONCATENATION", "#CONCATENATION", EcnClassCategory.Concatenation, false),
            CreateDefinition("#SET", "#CONCATENATION", "#CONCATENATION", EcnClassCategory.Concatenation, false),
            CreateDefinition("#CONDITIONAL-INT", "", "#CONDITIONAL-INT", EcnClassCategory.EncodingProcedure, false),
            CreateDefinition("#CONDITIONAL-REPETITION", "", "#CONDITIONAL-REPETITION", EcnClassCategory.EncodingProcedure, false),
            CreateDefinition("#OUTER", "", "#OUTER", EcnClassCategory.EncodingProcedure, false),
            CreateDefinition("#TRANSFORM", "", "#TRANSFORM", EcnClassCategory.EncodingProcedure, false),
            CreateDefinition("#INT", "", "#INT", EcnClassCategory.Integer, false),
            CreateDefinition("#INTEGER", "#INT", "#INT", EcnClassCategory.Integer, false),
            CreateDefinition("#ENUMERATED", "#INT", "#INT", EcnClassCategory.Integer, false),
            CreateDefinition("#NUL", "", "#NUL", EcnClassCategory.Null, false),
            CreateDefinition("#NULL", "#NUL", "#NUL", EcnClassCategory.Null, false),
            CreateDefinition("#OBJECT-IDENTIFIER", "", "#OBJECT-IDENTIFIER", EcnClassCategory.ObjectIdentifier, false),
            CreateDefinition("#RELATIVE-OID", "#OBJECT-IDENTIFIER", "#OBJECT-IDENTIFIER", EcnClassCategory.ObjectIdentifier, false),
            CreateDefinition("#OCTETS", "", "#OCTETS", EcnClassCategory.OctetString, false),
            CreateDefinition("#OCTET-STRING", "#OCTETS", "#OCTETS", EcnClassCategory.OctetString, false),
            CreateDefinition("#OPEN-TYPE", "", "#OPEN-TYPE", EcnClassCategory.OpenType, false),
            CreateDefinition("#OPTIONAL", "", "#OPTIONAL", EcnClassCategory.Optionality, false),
            CreateDefinition("#PAD", "", "#PAD", EcnClassCategory.Padding, false),
            CreateDefinition("#REAL", "", "#REAL", EcnClassCategory.Real, false),
            CreateDefinition("#REPETITION", "", "#REPETITION", EcnClassCategory.Repetition, false),
            CreateDefinition("#SEQUENCE-OF", "#REPETITION", "#REPETITION", EcnClassCategory.Repetition, false),
            CreateDefinition("#SET-OF", "#REPETITION", "#REPETITION", EcnClassCategory.Repetition, false),
            CreateDefinition("#TAG", "", "#TAG", EcnClassCategory.Tag, false),
            CreateDefinition("#TIME", "", "#TIME", EcnClassCategory.Time, false),
            CreateDefinition("#DATE", "#TIME", "#TIME", EcnClassCategory.Time, false),
            CreateDefinition("#TIME-OF-DAY", "#TIME", "#TIME", EcnClassCategory.Time, false),
            CreateDefinition("#DATE-TIME", "#TIME", "#TIME", EcnClassCategory.Time, false),
            CreateDefinition("#DURATION", "#TIME", "#TIME", EcnClassCategory.Time, false)
        ];
    }

    internal static EcnBuiltInClassDefinition CreateDefinition(
        string name,
        string derivedFromName,
        string primitiveName,
        EcnClassCategory category,
        bool encodingStructure)
    {
        return new EcnBuiltInClassDefinition
        {
            StableId = "x692:class:" + name,
            Name = name,
            DerivedFromName = derivedFromName,
            PrimitiveName = primitiveName,
            Category = category,
            CategoryGroups = GetGroupsForCategory(category),
            Primitive = derivedFromName.Length == 0 && !encodingStructure,
            EncodingStructure = encodingStructure
        };
    }
}
