using System.Reflection;
using asn1;

namespace asn1.Tests;

public class PublicEnumTests
{
    private static readonly object[] EnumCases =
    [
        new object[] { typeof(AsnEncodingRules), new string[] { "Ber", "Der", "Cer", "Aper", "Uper", "CanonicalAlignedPer", "CanonicalUnalignedPer", "Oer", "Coer", "BasicXer", "CanonicalXer", "ExtendedXer", "Ecn" } },
        new object[] { typeof(AsnDiagnosticSeverity), new string[] { "Information", "Warning", "Error" } },
        new object[] { typeof(AsnDiagnosticCategory), new string[] { "General", "Source", "Lexical", "Syntax", "Binding", "Semantic", "Constraint", "Generation", "Encoding", "Decoding", "Canonicality", "Resource", "Ecn", "Xsd", "InputOutput" } },
        new object[] { typeof(AsnTokenKind), new string[] { "EndOfInput", "Invalid", "Whitespace", "Comment", "ReservedWord", "TypeReference", "ValueReference", "ModuleReference", "ObjectClassReference", "ObjectReference", "ObjectSetReference", "TypeFieldReference", "ValueFieldReference", "ValueSetFieldReference", "ObjectFieldReference", "ObjectSetFieldReference", "EncodingReference", "Identifier", "IntegerLiteral", "RealLiteral", "BinaryStringLiteral", "HexStringLiteral", "CharacterStringLiteral", "TupleLiteral", "QuadrupleLiteral", "XmlValue", "LeftBrace", "RightBrace", "LeftParenthesis", "RightParenthesis", "LeftBracket", "RightBracket", "LeftVersionBracket", "RightVersionBracket", "LessThan", "GreaterThan", "Comma", "Semicolon", "Colon", "Dot", "ExclamationMark", "AtSign", "VerticalBar", "Caret", "Ampersand", "Plus", "Minus", "Asterisk", "Slash", "Assignment", "Ellipsis", "Range", "Equals", "EncodingClassReference", "NonEcnText" } },
        new object[] { typeof(AsnAssignmentKind), new string[] { "Unknown", "Type", "Value", "ValueSet", "ObjectClass", "Object", "ObjectSet" } },
        new object[] { typeof(AsnDerivationKind), new string[] { "Alias", "ComponentsOf", "ParameterInstantiation", "AutomaticTag", "XsdMapping", "EncodingControl" } },
        new object[] { typeof(AsnTypeKind), new string[] { "Unknown", "Boolean", "Integer", "Enumerated", "Real", "BitString", "OctetString", "Null", "ObjectIdentifier", "RelativeObjectIdentifier", "ObjectIdentifierIri", "RelativeObjectIdentifierIri", "Choice", "Sequence", "Set", "SequenceOf", "SetOf", "Tagged", "Prefixed", "Selection", "InstanceOf", "External", "EmbeddedPdv", "CharacterString", "ObjectDescriptor", "Utf8String", "NumericString", "PrintableString", "TeletexString", "VideotexString", "Ia5String", "GraphicString", "VisibleString", "GeneralString", "UniversalString", "BmpString", "UtcTime", "GeneralizedTime", "Time", "Date", "TimeOfDay", "DateTime", "Duration", "Defined", "OpenType" } },
        new object[] { typeof(AsnValueKind), new string[] { "Unknown", "Boolean", "Integer", "Enumerated", "Real", "BitString", "OctetString", "Null", "CharacterString", "ObjectIdentifier", "RelativeObjectIdentifier", "ObjectIdentifierIri", "RelativeObjectIdentifierIri", "Sequence", "Set", "SequenceOf", "SetOf", "Choice", "OpenType", "External", "EmbeddedPdv", "InstanceOf", "Time", "Defined", "Named", "Xml" } },
        new object[] { typeof(AsnConstraintKind), new string[] { "Unknown", "SingleValue", "ContainedSubtype", "ValueRange", "Size", "PermittedAlphabet", "Pattern", "PropertySettings", "Type", "WithComponent", "WithComponents", "UserDefined", "SimpleTable", "ComponentRelation", "Contents", "Union", "Intersection", "Except", "AllExcept", "Extension", "Exception" } },
        new object[] { typeof(AsnTagClass), new string[] { "Universal", "Application", "ContextSpecific", "Private" } },
        new object[] { typeof(AsnTaggingMode), new string[] { "Unspecified", "Explicit", "Implicit", "Automatic" } },
        new object[] { typeof(AsnParameterKind), new string[] { "Unknown", "Type", "Value", "ValueSet", "ObjectClass", "Object", "ObjectSet" } },
        new object[] { typeof(AsnObjectFieldKind), new string[] { "Unknown", "Type", "FixedTypeValue", "VariableTypeValue", "FixedTypeValueSet", "VariableTypeValueSet", "Object", "ObjectSet" } },
        new object[] { typeof(AsnInstructionKind), new string[] { "Unknown", "AnyAttributes", "AnyElement", "Attribute", "Base64", "Decimal", "DefaultForEmpty", "Element", "EmbedValues", "GlobalDefaults", "List", "Name", "Namespace", "PiOrComment", "Text", "Untagged", "UseNil", "UseNumber", "UseOrder", "UseQName", "UseType", "UseUnion", "Whitespace", "PerApplication", "Ecn" } },
        new object[] { typeof(AsnPlanStepKind), new string[] { "Unknown", "Primitive", "Sequence", "Choice", "Repetition", "Optional", "Tag", "Length", "Alignment", "Padding", "Condition", "Determinant", "Mapping", "Replacement", "Transformation", "OpenType", "StandardEncoding" } },
        new object[] { typeof(AsnRuntimeValueKind), new string[] { "Unknown", "Boolean", "Integer", "Enumerated", "Real", "BitString", "OctetString", "Null", "CharacterString", "ObjectIdentifier", "RelativeObjectIdentifier", "ObjectIdentifierIri", "RelativeObjectIdentifierIri", "Sequence", "Set", "SequenceOf", "SetOf", "Choice", "OpenType", "External", "EmbeddedPdv", "InstanceOf", "Time" } },
        new object[] { typeof(AsnDecodeStatus), new string[] { "NeedMoreData", "Done", "Error" } },
        new object[] { typeof(AsnCanonicalityStatus), new string[] { "Canonical", "NonCanonical", "Invalid" } },
        new object[] { typeof(AsnTlvEventKind), new string[] { "EndOfInput", "Primitive", "StartConstructed", "EndConstructed" } },
        new object[] { typeof(AsnRealKind), new string[] { "Zero", "MinusZero", "FiniteDecimal", "FiniteBinary", "PositiveInfinity", "NegativeInfinity", "NotANumber" } },
        new object[] { typeof(AsnTimeKind), new string[] { "UtcTime", "GeneralizedTime", "Time", "Date", "TimeOfDay", "DateTime", "Duration" } },
        new object[] { typeof(AsnUnknownDataPolicy), new string[] { "Reject", "Preserve", "Skip" } },
        new object[] { typeof(AsnCodeGenerationTarget), new string[] { "ModelOnly", "DescriptorOnly", "EncodeOnly", "DecodeOnly", "Combined" } }
    ];

    [TestCaseSource(nameof(EnumCases))]
    public void EnumNamesAndValuesAreStable(Type enumType, string[] expectedNames)
    {
        string[] actualNames = Enum.GetNames(enumType);
        int[] actualValues = Enum.GetValues(enumType).Cast<object>().Select(Convert.ToInt32).ToArray();

        Assert.That(Enum.GetUnderlyingType(enumType), Is.EqualTo(typeof(int)));
        Assert.That(actualNames, Is.EqualTo(expectedNames));
        Assert.That(actualValues, Is.EqualTo(Enumerable.Range(0, expectedNames.Length)));
    }

    [Test]
    public void EveryPublicEnumIsConsumedByPublicDataOrContractType()
    {
        Type[] apiTypes = typeof(AsnEncodingRules).Assembly.GetExportedTypes();
        Type[] enumTypes = apiTypes.Where(candidate => candidate.IsEnum).ToArray();
        Type[] consumers = apiTypes.Where(candidate => candidate.IsClass || candidate.IsInterface).ToArray();

        Assert.That(enumTypes, Has.Length.EqualTo(EnumCases.Length));

        foreach (Type enumType in enumTypes)
        {
            bool isConsumed = consumers.Any(consumer =>
                consumer.GetFields(BindingFlags.Instance | BindingFlags.Public)
                    .Any(field => field.FieldType == enumType)
                || consumer.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                    .Any(method => method.ReturnType == enumType
                        || method.GetParameters().Any(parameter => parameter.ParameterType == enumType)));

            Assert.That(isConsumed, Is.True, $"{enumType.Name} is not consumed by a public data or contract type.");
        }
    }
}
