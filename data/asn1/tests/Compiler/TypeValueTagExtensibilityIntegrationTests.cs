using asn1;

namespace asn1.Tests;

public class TypeValueTagExtensibilityIntegrationTests
{
    [Test]
    public void CompilerAcceptsEveryBuiltinAndUsefulType()
    {
        string source = "AllTypes DEFINITIONS ::= BEGIN "
                + "BooleanType ::= BOOLEAN; IntegerType ::= INTEGER; "
                + "EnumeratedType ::= ENUMERATED { first, second }; RealType ::= REAL; "
                + "BitStringType ::= BIT STRING; OctetStringType ::= OCTET STRING; "
                + "NullType ::= NULL; ObjectIdentifierType ::= OBJECT IDENTIFIER; "
                + "RelativeOidType ::= RELATIVE-OID; OidIriType ::= OID-IRI; "
                + "RelativeOidIriType ::= RELATIVE-OID-IRI; "
                + "ChoiceType ::= CHOICE { number INTEGER, flag BOOLEAN }; "
                + "SequenceType ::= SEQUENCE { number INTEGER }; "
                + "SetType ::= SET { flag BOOLEAN }; "
                + "SequenceOfType ::= SEQUENCE OF INTEGER; SetOfType ::= SET OF BOOLEAN; "
                + "InstanceOfType ::= INSTANCE OF TYPE-IDENTIFIER; "
                + "ExternalType ::= EXTERNAL; EmbeddedPdvType ::= EMBEDDED PDV; "
                + "CharacterStringType ::= CHARACTER STRING; "
                + "ObjectDescriptorType ::= ObjectDescriptor; Utf8StringType ::= UTF8String; "
                + "NumericStringType ::= NumericString; PrintableStringType ::= PrintableString; "
                + "TeletexStringType ::= TeletexString; VideotexStringType ::= VideotexString; "
                + "Ia5StringType ::= IA5String; GraphicStringType ::= GraphicString; "
                + "VisibleStringType ::= VisibleString; GeneralStringType ::= GeneralString; "
                + "UniversalStringType ::= UniversalString; BmpStringType ::= BMPString; "
                + "UtcTimeType ::= UTCTime; GeneralizedTimeType ::= GeneralizedTime; "
                + "TimeType ::= TIME; DateType ::= DATE; TimeOfDayType ::= TIME-OF-DAY; "
                + "DateTimeType ::= DATE-TIME; DurationType ::= DURATION; "
                + "SelectedType ::= number < ChoiceType; "
                + "TaggedType ::= [APPLICATION 3] EXPLICIT INTEGER; "
                + "DefinedType ::= IntegerType; OpenType ::= TYPE-IDENTIFIER.&Type END";
        AsnCompilationResult result = Compile(source);

        Assert.That(result.Success, Is.True, Format(result));
        AssertType(result, "BooleanType", AsnTypeKind.Boolean);
        AssertType(result, "IntegerType", AsnTypeKind.Integer);
        AssertType(result, "EnumeratedType", AsnTypeKind.Enumerated);
        AssertType(result, "RealType", AsnTypeKind.Real);
        AssertType(result, "BitStringType", AsnTypeKind.BitString);
        AssertType(result, "OctetStringType", AsnTypeKind.OctetString);
        AssertType(result, "NullType", AsnTypeKind.Null);
        AssertType(result, "ObjectIdentifierType", AsnTypeKind.ObjectIdentifier);
        AssertType(result, "RelativeOidType", AsnTypeKind.RelativeObjectIdentifier);
        AssertType(result, "OidIriType", AsnTypeKind.ObjectIdentifierIri);
        AssertType(result, "RelativeOidIriType", AsnTypeKind.RelativeObjectIdentifierIri);
        AssertType(result, "ChoiceType", AsnTypeKind.Choice);
        AssertType(result, "SequenceType", AsnTypeKind.Sequence);
        AssertType(result, "SetType", AsnTypeKind.Set);
        AssertType(result, "SequenceOfType", AsnTypeKind.SequenceOf);
        AssertType(result, "SetOfType", AsnTypeKind.SetOf);
        AssertType(result, "InstanceOfType", AsnTypeKind.InstanceOf);
        AssertType(result, "ExternalType", AsnTypeKind.External);
        AssertType(result, "EmbeddedPdvType", AsnTypeKind.EmbeddedPdv);
        AssertType(result, "CharacterStringType", AsnTypeKind.CharacterString);
        AssertType(result, "ObjectDescriptorType", AsnTypeKind.ObjectDescriptor);
        AssertType(result, "Utf8StringType", AsnTypeKind.Utf8String);
        AssertType(result, "NumericStringType", AsnTypeKind.NumericString);
        AssertType(result, "PrintableStringType", AsnTypeKind.PrintableString);
        AssertType(result, "TeletexStringType", AsnTypeKind.TeletexString);
        AssertType(result, "VideotexStringType", AsnTypeKind.VideotexString);
        AssertType(result, "Ia5StringType", AsnTypeKind.Ia5String);
        AssertType(result, "GraphicStringType", AsnTypeKind.GraphicString);
        AssertType(result, "VisibleStringType", AsnTypeKind.VisibleString);
        AssertType(result, "GeneralStringType", AsnTypeKind.GeneralString);
        AssertType(result, "UniversalStringType", AsnTypeKind.UniversalString);
        AssertType(result, "BmpStringType", AsnTypeKind.BmpString);
        AssertType(result, "UtcTimeType", AsnTypeKind.UtcTime);
        AssertType(result, "GeneralizedTimeType", AsnTypeKind.GeneralizedTime);
        AssertType(result, "TimeType", AsnTypeKind.Time);
        AssertType(result, "DateType", AsnTypeKind.Date);
        AssertType(result, "TimeOfDayType", AsnTypeKind.TimeOfDay);
        AssertType(result, "DateTimeType", AsnTypeKind.DateTime);
        AssertType(result, "DurationType", AsnTypeKind.Duration);
        AssertType(result, "SelectedType", AsnTypeKind.Integer);
        AssertType(result, "TaggedType", AsnTypeKind.Integer);
        AssertType(result, "DefinedType", AsnTypeKind.Integer);
        AssertType(result, "OpenType", AsnTypeKind.OpenType);
    }

    [Test]
    public void CompilerAcceptsEveryValueNotationCategory()
    {
        AsnCompilationResult result = Compile(
            "AllValues DEFINITIONS ::= BEGIN "
                + "booleanValue BOOLEAN ::= TRUE; integerValue INTEGER ::= -42; "
                + "enumeratedValue ENUMERATED { red, blue } ::= blue; "
                + "realValue REAL ::= 1.25E-3; bitsValue BIT STRING ::= 'A'H; "
                + "octetsValue OCTET STRING ::= 'AB'H; nullValue NULL ::= NULL; "
                + "textValue UTF8String ::= \"hello\"; "
                + "descriptorValue ObjectDescriptor ::= \"descriptor\"; "
                + "oidValue OBJECT IDENTIFIER ::= { iso(1) member-body(2) 840 }; "
                + "relativeOidValue RELATIVE-OID ::= { 7 8 9 }; "
                + "oidIriValue OID-IRI ::= \"/iso/1/member-body/2\"; "
                + "relativeOidIriValue RELATIVE-OID-IRI ::= \"member/2\"; "
                + "sequenceValue SEQUENCE { id INTEGER, enabled BOOLEAN } "
                + "::= { id 7, enabled TRUE }; "
                + "setValue SET { id INTEGER, enabled BOOLEAN } "
                + "::= { enabled FALSE, id 8 }; "
                + "sequenceOfValue SEQUENCE OF INTEGER ::= { 1, 2, 3 }; "
                + "setOfValue SET OF BOOLEAN ::= { TRUE, FALSE }; "
                + "choiceValue CHOICE { number INTEGER, text UTF8String } ::= number: 9; "
                + "syntaxRef OBJECT IDENTIFIER ::= { 1 2 3 }; "
                + "payload OCTET STRING ::= 'CAFE'H; "
                + "externalValue EXTERNAL ::= { identification syntaxRef, data-value payload }; "
                + "embeddedValue EMBEDDED PDV ::= { identification syntaxRef, "
                + "data-value payload }; "
                + "instanceValue INSTANCE OF TYPE-IDENTIFIER ::= { "
                + "type-id { 1 2 3 }, value INTEGER: 5 }; "
                + "OpenValueType ::= TYPE-IDENTIFIER.&Type; "
                + "openValue OpenValueType ::= INTEGER: 11; "
                + "utcValue UTCTime ::= \"240229235959Z\"; "
                + "generalizedValue GeneralizedTime ::= \"20240229235959.5Z\"; "
                + "timeValue TIME ::= \"23:59:59\"; "
                + "dateValue DATE ::= \"2024-02-29\"; "
                + "timeOfDayValue TIME-OF-DAY ::= \"23:59:59\"; "
                + "dateTimeValue DATE-TIME ::= \"2024-02-29T23:59:59Z\"; "
                + "durationValue DURATION ::= \"P2DT3H\"; "
                + "definedValue INTEGER ::= integerValue; "
                + "xmlValue BOOLEAN ::= <true/> END");

        Assert.That(result.Success, Is.True, Format(result));
        AssertValue(result, "booleanValue", AsnValueKind.Boolean);
        AssertValue(result, "integerValue", AsnValueKind.Integer);
        AssertValue(result, "enumeratedValue", AsnValueKind.Enumerated);
        AssertValue(result, "realValue", AsnValueKind.Real);
        AssertValue(result, "bitsValue", AsnValueKind.BitString);
        AssertValue(result, "octetsValue", AsnValueKind.OctetString);
        AssertValue(result, "nullValue", AsnValueKind.Null);
        AssertValue(result, "textValue", AsnValueKind.CharacterString);
        AssertValue(result, "descriptorValue", AsnValueKind.CharacterString);
        AssertValue(result, "oidValue", AsnValueKind.ObjectIdentifier);
        AssertValue(result, "relativeOidValue", AsnValueKind.RelativeObjectIdentifier);
        AssertValue(result, "oidIriValue", AsnValueKind.ObjectIdentifierIri);
        AssertValue(result, "relativeOidIriValue", AsnValueKind.RelativeObjectIdentifierIri);
        AssertValue(result, "sequenceValue", AsnValueKind.Sequence);
        AssertValue(result, "setValue", AsnValueKind.Set);
        AssertValue(result, "sequenceOfValue", AsnValueKind.SequenceOf);
        AssertValue(result, "setOfValue", AsnValueKind.SetOf);
        AssertValue(result, "choiceValue", AsnValueKind.Choice);
        AssertValue(result, "externalValue", AsnValueKind.External);
        AssertValue(result, "embeddedValue", AsnValueKind.EmbeddedPdv);
        AssertValue(result, "instanceValue", AsnValueKind.InstanceOf);
        AssertValue(result, "openValue", AsnValueKind.OpenType);
        AssertValue(result, "utcValue", AsnValueKind.Time);
        AssertValue(result, "generalizedValue", AsnValueKind.Time);
        AssertValue(result, "timeValue", AsnValueKind.Time);
        AssertValue(result, "dateValue", AsnValueKind.Time);
        AssertValue(result, "timeOfDayValue", AsnValueKind.Time);
        AssertValue(result, "dateTimeValue", AsnValueKind.Time);
        AssertValue(result, "durationValue", AsnValueKind.Time);
        AssertValue(result, "definedValue", AsnValueKind.Integer);
        AssertValue(result, "xmlValue", AsnValueKind.Boolean);
    }

    [Test]
    public void CompilerPreservesTagModesComponentsOfSelectionAndExtensionGroups()
    {
        AsnCompilationResult result = Compile(
            "ExplicitCases DEFINITIONS EXPLICIT TAGS ::= BEGIN "
                + "ExplicitTagged ::= [APPLICATION 1] INTEGER; "
                + "ManualImplicit ::= [2] IMPLICIT BOOLEAN; "
                + "ManualExplicit ::= [3] EXPLICIT UTF8String; "
                + "Alternatives ::= CHOICE { number [4] IMPLICIT INTEGER, "
                + "flag [5] EXPLICIT BOOLEAN }; "
                + "Selected ::= number < Alternatives; "
                + "Base ::= SEQUENCE { root INTEGER, tail UTF8String }; "
                + "Extended ::= SEQUENCE { before NULL, COMPONENTS OF Base, ..., "
                + "[[ 2: added BOOLEAN ]], ..., after OCTET STRING } END",
            "ImplicitCases DEFINITIONS IMPLICIT TAGS ::= BEGIN "
                + "DefaultImplicit ::= [6] INTEGER END",
            "AutomaticCases DEFINITIONS AUTOMATIC TAGS ::= BEGIN "
                + "Auto ::= SEQUENCE { number INTEGER, flag BOOLEAN, ..., "
                + "[[ 2: text UTF8String ]], ... } END");

        Assert.That(result.Success, Is.True, Format(result));
        AssertTag(result, "ExplicitTagged", AsnTagClass.Application,
            AsnTaggingMode.Explicit, 1);
        AssertTag(result, "ManualImplicit", AsnTagClass.ContextSpecific,
            AsnTaggingMode.Implicit, 2);
        AssertTag(result, "ManualExplicit", AsnTagClass.ContextSpecific,
            AsnTaggingMode.Explicit, 3);
        AssertTag(result, "DefaultImplicit", AsnTagClass.ContextSpecific,
            AsnTaggingMode.Implicit, 6);
        AssertTag(result, "Selected", AsnTagClass.ContextSpecific,
            AsnTaggingMode.Implicit, 4);

        AsnType extended = result.Assignments.Single(item => item.Name == "Extended")
            .Type ?? throw new InvalidOperationException();
        Assert.That(extended.Extensible, Is.True);
        Assert.That(extended.Components.Select(item => item.Name),
            Is.EqualTo(new[] { "before", "root", "tail", "added", "after" }));
        Assert.That(extended.Components.Select(item => item.Extension),
            Is.EqualTo(new[] { false, false, false, true, false }));

        AsnTypeDescriptor extendedDescriptor = result.ReflectionCatalog?.Assignments
            .Single(item => item.Name == "Extended").Type
            ?? throw new InvalidOperationException();
        AsnTypeDescriptor autoDescriptor = result.ReflectionCatalog.Assignments
            .Single(item => item.Name == "Auto").Type
            ?? throw new InvalidOperationException();
        Assert.That(string.Join(",", autoDescriptor.Fields.Select(
            item => item.Tags[0].Number.ToString())), Is.EqualTo("0,1,2"));
        Assert.That(autoDescriptor.Fields.Select(item => item.Tags[0].Mode),
            Is.EqualTo(new[]
            {
                AsnTaggingMode.Implicit,
                AsnTaggingMode.Implicit,
                AsnTaggingMode.Implicit
            }));
        Assert.That(autoDescriptor.Fields.Select(
            item => item.Tags[0].AutomaticallyGenerated), Is.All.True);
        Assert.That(extendedDescriptor.ExtensionFields.Select(
            item => item.ExtensionGroupIndex), Is.EqualTo(new[] { 0 }));
        Assert.That(autoDescriptor.ExtensionFields.Select(
            item => item.ExtensionGroupIndex), Is.EqualTo(new[] { 0 }));
    }

    [Test]
    public void CompilerReportsEveryInvalidChoiceAndSetTagCollision()
    {
        AsnCompilationResult result = Compile(
            "Collisions DEFINITIONS ::= BEGIN "
                + "BadChoice ::= CHOICE { first INTEGER, second INTEGER }; "
                + "BadSet ::= SET { first BOOLEAN, second BOOLEAN } END");

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.Select(item => item.Code),
            Is.EqualTo(new[] { "ASNS0006", "ASNS0006" }));
        Assert.That(result.Diagnostics.Select(item => item.RelatedLocations.Length),
            Is.EqualTo(new[] { 1, 1 }));
    }

    private static void AssertType(
        AsnCompilationResult result,
        string name,
        AsnTypeKind expected)
    {
        Assert.That(result.Assignments.Single(item => item.Name == name).Type?.Kind,
            Is.EqualTo(expected), name);
    }

    private static void AssertValue(
        AsnCompilationResult result,
        string name,
        AsnValueKind expected)
    {
        Assert.That(result.Assignments.Single(item => item.Name == name).Value?.Kind,
            Is.EqualTo(expected), name);
    }

    private static void AssertTag(
        AsnCompilationResult result,
        string name,
        AsnTagClass expectedClass,
        AsnTaggingMode expectedMode,
        int expectedNumber)
    {
        AsnTag tag = result.Assignments.Single(item => item.Name == name).Type?.Tag
            ?? throw new InvalidOperationException();
        Assert.Multiple(() =>
        {
            Assert.That(tag.Class, Is.EqualTo(expectedClass), name);
            Assert.That(tag.Mode, Is.EqualTo(expectedMode), name);
            Assert.That(tag.Number,
                Is.EqualTo(new System.Numerics.BigInteger(expectedNumber)), name);
        });
    }

    private static AsnCompilationResult Compile(params string[] sources)
    {
        return new CompilerImplementation().Compile(new AsnCompilationInput
        {
            SourceUnits = sources.Select((source, index) => new AsnSourceUnit
            {
                Name = "type-matrix-" + index.ToString() + ".asn",
                Text = source
            }).ToArray()
        });
    }

    private static string Format(AsnCompilationResult result)
    {
        return "validation=" + result.ValidationResult?.Success + " | "
            + string.Join(" | ", result.Diagnostics.Select(item =>
            item.Code + ":" + item.Message));
    }

}
