using asn1;

namespace asn1.Tests;

public class PrefixedAndSpecialTypeParserTests
{
    [TestCase(
        "[0] INTEGER",
        AsnTagClass.ContextSpecific,
        AsnTaggingMode.Unspecified,
        "0",
        AsnTypeKind.Integer)]
    [TestCase(
        "[APPLICATION 12] IMPLICIT UTF8String",
        AsnTagClass.Application,
        AsnTaggingMode.Implicit,
        "12",
        AsnTypeKind.Utf8String)]
    [TestCase(
        "[PRIVATE 999999999999999999999] EXPLICIT SEQUENCE OF INTEGER",
        AsnTagClass.Private,
        AsnTaggingMode.Explicit,
        "999999999999999999999",
        AsnTypeKind.SequenceOf)]
    [TestCase(
        "[UNIVERSAL 4] OCTET STRING",
        AsnTagClass.Universal,
        AsnTaggingMode.Unspecified,
        "4",
        AsnTypeKind.OctetString)]
    [TestCase(
        "[TAG:3] BOOLEAN",
        AsnTagClass.ContextSpecific,
        AsnTaggingMode.Unspecified,
        "3",
        AsnTypeKind.Boolean)]
    public void NumericTagsRetainClassModeNumberAndInnerType(
        string source,
        AsnTagClass expectedClass,
        AsnTaggingMode expectedMode,
        string expectedNumber,
        AsnTypeKind expectedInnerKind)
    {
        TypeFixture fixture = ParseType(source);

        ParsedTypeSyntax syntax = RequireSyntax(fixture.Result);
        Assert.That(fixture.Result.Success, Is.True);
        Assert.That(syntax.Type.Kind, Is.EqualTo(AsnTypeKind.Tagged));
        Assert.That(syntax.Type.Tag?.Class, Is.EqualTo(expectedClass));
        Assert.That(syntax.Type.Tag?.Mode, Is.EqualTo(expectedMode));
        Assert.That(syntax.Type.Tag?.Number.ToString(), Is.EqualTo(expectedNumber));
        Assert.That(syntax.Type.ElementType?.Kind, Is.EqualTo(expectedInnerKind));
        Assert.That(syntax.HasNumericTagNumber, Is.True);
        Assert.That(syntax.EncodingReference, Is.EqualTo("TAG"));
        Assert.That(fixture.Result.ConsumedTokenCount, Is.EqualTo(fixture.Tokens.Length));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [TestCase("[tagNumber] INTEGER", "tagNumber")]
    [TestCase("[Tags.number] BOOLEAN", "Tags.number")]
    public void TagsAcceptLocalAndExternalDefinedClassNumbers(
        string source,
        string expectedReference)
    {
        TypeFixture fixture = ParseType(source);

        ParsedTypeSyntax syntax = RequireSyntax(fixture.Result);
        Assert.That(fixture.Result.Success, Is.True);
        Assert.That(syntax.Type.Kind, Is.EqualTo(AsnTypeKind.Tagged));
        Assert.That(syntax.HasNumericTagNumber, Is.False);
        Assert.That(syntax.TagNumberReference, Is.EqualTo(expectedReference));
        Assert.That(syntax.Type.Tag?.Class, Is.EqualTo(AsnTagClass.ContextSpecific));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [TestCase("[XER:ATTRIBUTE] UTF8String", "XER", AsnInstructionKind.Attribute, "ATTRIBUTE")]
    [TestCase("[PER:SIZE 4] SEQUENCE OF INTEGER", "PER", AsnInstructionKind.PerApplication, "SIZE 4")]
    [TestCase("[ECN:custom-rule 7] INTEGER", "ECN", AsnInstructionKind.Ecn, "custom-rule 7")]
    [TestCase("[ATTRIBUTE] UTF8String", "", AsnInstructionKind.Attribute, "ATTRIBUTE")]
    public void EncodingPrefixesRetainReferenceInstructionAndInnerType(
        string source,
        string expectedReference,
        AsnInstructionKind expectedKind,
        string expectedTokens)
    {
        TypeFixture fixture = ParseType(source);

        ParsedTypeSyntax syntax = RequireSyntax(fixture.Result);
        Assert.That(fixture.Result.Success, Is.True);
        Assert.That(syntax.Type.Kind, Is.EqualTo(AsnTypeKind.Prefixed));
        Assert.That(syntax.EncodingReference, Is.EqualTo(expectedReference));
        Assert.That(syntax.EncodingInstructions, Has.Length.EqualTo(1));
        Assert.That(syntax.EncodingInstructions[0].Kind, Is.EqualTo(expectedKind));
        Assert.That(
            string.Join(" ", syntax.EncodingInstructionTokens.Select(item => item.Text)),
            Is.EqualTo(expectedTokens));
        Assert.That(syntax.Type.ElementType, Is.Not.Null);
        Assert.That(fixture.Result.ConsumedTokenCount, Is.EqualTo(fixture.Tokens.Length));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void PrefixesComposeRecursivelyWithoutLosingTheirIndividualSyntax()
    {
        TypeFixture fixture = ParseType(
            "[XER:ATTRIBUTE] [APPLICATION 3] IMPLICIT UTF8String");

        ParsedTypeSyntax outer = RequireSyntax(fixture.Result);
        ParsedTypeSyntax inner = outer.InnerTypeSyntax ?? throw new InvalidOperationException();
        Assert.That(outer.Type.Kind, Is.EqualTo(AsnTypeKind.Prefixed));
        Assert.That(inner.Type.Kind, Is.EqualTo(AsnTypeKind.Tagged));
        Assert.That(inner.Type.Tag?.Class, Is.EqualTo(AsnTagClass.Application));
        Assert.That(inner.Type.Tag?.Mode, Is.EqualTo(AsnTaggingMode.Implicit));
        Assert.That(inner.InnerTypeSyntax?.Type.Kind, Is.EqualTo(AsnTypeKind.Utf8String));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void SelectionTypeRetainsSelectedIdentifierAndChoiceType()
    {
        TypeFixture fixture = ParseType("selected < ChoiceType");

        ParsedTypeSyntax syntax = RequireSyntax(fixture.Result);
        Assert.That(fixture.Result.Success, Is.True);
        Assert.That(syntax.Type.Kind, Is.EqualTo(AsnTypeKind.Selection));
        Assert.That(syntax.SelectionIdentifier, Is.EqualTo("selected"));
        Assert.That(syntax.Type.Name, Is.EqualTo("selected"));
        Assert.That(syntax.Type.ElementType?.Kind, Is.EqualTo(AsnTypeKind.Defined));
        Assert.That(syntax.InnerTypeSyntax?.ReferenceName, Is.EqualTo("ChoiceType"));
        Assert.That(fixture.Result.ConsumedTokenCount, Is.EqualTo(fixture.Tokens.Length));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [TestCase("INSTANCE OF TYPE-IDENTIFIER", "", "TYPE-IDENTIFIER")]
    [TestCase("INSTANCE OF ABSTRACT-SYNTAX", "", "ABSTRACT-SYNTAX")]
    [TestCase("INSTANCE OF Classes.CERT-CLASS", "Classes", "CERT-CLASS")]
    public void InstanceOfRetainsUsefulLocalAndExternalObjectClassReferences(
        string source,
        string expectedModule,
        string expectedName)
    {
        TypeFixture fixture = ParseType(source);

        ParsedTypeSyntax syntax = RequireSyntax(fixture.Result);
        Assert.That(fixture.Result.Success, Is.True);
        Assert.That(syntax.Type.Kind, Is.EqualTo(AsnTypeKind.InstanceOf));
        Assert.That(syntax.ReferenceModule, Is.EqualTo(expectedModule));
        Assert.That(syntax.ReferenceName, Is.EqualTo(expectedName));
        Assert.That(
            syntax.Type.Name,
            Is.EqualTo(expectedModule.Length == 0 ? expectedName : expectedModule + "." + expectedName));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [TestCase("EXTERNAL", AsnTypeKind.External)]
    [TestCase("EMBEDDED PDV", AsnTypeKind.EmbeddedPdv)]
    [TestCase("CHARACTER STRING", AsnTypeKind.CharacterString)]
    public void ParserRecognizesSpecialBuiltInTypes(string source, AsnTypeKind expectedKind)
    {
        TypeFixture fixture = ParseType(source);

        ParsedTypeSyntax syntax = RequireSyntax(fixture.Result);
        Assert.That(fixture.Result.Success, Is.True);
        Assert.That(syntax.Type.Kind, Is.EqualTo(expectedKind));
        Assert.That(fixture.Result.ConsumedTokenCount, Is.EqualTo(fixture.Tokens.Length));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void AssignmentGovernorsAcceptTaggedConstructedTypes()
    {
        ModuleFixture fixture = ParseModule(
            "Tagged DEFINITIONS ::= BEGIN "
            + "value [APPLICATION 2] EXPLICIT CHOICE { number INTEGER, text UTF8String } ::= number: 1 "
            + "END");

        AssignmentHeadAlternativeSyntax alternative = RequireModule(fixture.Result)
            .AssignmentHeads
            .Single()
            .Alternatives
            .Single(item => item.Name == "value");
        Assert.That(alternative.GovernorType?.Type.Kind, Is.EqualTo(AsnTypeKind.Tagged));
        Assert.That(
            alternative.GovernorType?.InnerTypeSyntax?.Type.Kind,
            Is.EqualTo(AsnTypeKind.Choice));
        Assert.That(
            alternative.GovernorType?.InnerTypeSyntax?.Type.Components,
            Has.Length.EqualTo(2));
        Assert.That(fixture.Session.GetDiagnostics(), Is.Empty);
    }

    [TestCase("[APPLICATION] INTEGER", "ASNP0051")]
    [TestCase("[0 INTEGER", "ASNP0052")]
    [TestCase("[0]", "ASNP0053")]
    [TestCase("[XER:] INTEGER", "ASNP0056")]
    [TestCase("[XER:ATTRIBUTE INTEGER", "ASNP0055")]
    [TestCase("[XER:ATTRIBUTE [0] INTEGER", "ASNP0054")]
    [TestCase("selected <", "ASNP0058")]
    [TestCase("INSTANCE INTEGER", "ASNP0059")]
    [TestCase("INSTANCE OF", "ASNP0060")]
    [TestCase("EMBEDDED INTEGER", "ASNP0017")]
    public void MalformedPrefixedAndSpecialTypesProduceSpecificDiagnostics(
        string source,
        string expectedCode)
    {
        TypeFixture fixture = ParseType(source);

        Assert.That(fixture.Result.Success, Is.False);
        Assert.That(
            fixture.Diagnostics.GetDiagnostics().Select(item => item.Code),
            Does.Contain(expectedCode));
    }

    private static TypeFixture ParseType(string source)
    {
        IncrementalLexSession session = CreateSession("prefixed-type.asn", source);
        List<AsnToken> tokens = new List<AsnToken>();
        while (true)
        {
            IncrementalLexResult lexical = session.ScanNext();
            if (lexical.Status == IncrementalLexStatus.EndOfInput)
            {
                break;
            }

            Assert.That(lexical.Status, Is.EqualTo(IncrementalLexStatus.Token));
            AsnToken token = lexical.Token ?? throw new InvalidOperationException();
            if (token.Kind != AsnTokenKind.Whitespace
                && token.Kind != AsnTokenKind.Comment
                && token.Kind != AsnTokenKind.EndOfInput)
            {
                tokens.Add(token);
            }
        }

        TypeParser parser = new TypeParser(tokens.ToArray(), session.Diagnostics, true);
        return new TypeFixture
        {
            Tokens = tokens.ToArray(),
            Diagnostics = session.Diagnostics,
            Result = parser.ParseTypeSyntax()
        };
    }

    private static ModuleFixture ParseModule(string source)
    {
        IncrementalLexSession session = CreateSession("prefixed-governor.asn", source);
        TokenCursor cursor = new TokenCursor(session, 4);
        return new ModuleFixture
        {
            Session = session,
            Result = new ModuleParser(cursor).ParseModule()
        };
    }

    private static IncrementalLexSession CreateSession(string sourceName, string source)
    {
        IncrementalLexSession session = new IncrementalLexSession(
            sourceName,
            false,
            new AsnResourceLimits());
        session.SupplyTextChunk(source, true);
        return session;
    }

    private static ParsedTypeSyntax RequireSyntax(TypeParseResult result)
    {
        Assert.That(result.Syntax, Is.Not.Null);
        return result.Syntax ?? throw new InvalidOperationException();
    }

    private static ModuleSyntax RequireModule(ModuleParseResult result)
    {
        Assert.That(result.Module, Is.Not.Null);
        return result.Module ?? throw new InvalidOperationException();
    }

    private class TypeFixture
    {
        internal AsnToken[] Tokens = [];
        internal DiagnosticBag Diagnostics = null!;
        internal TypeParseResult Result = null!;
    }

    private class ModuleFixture
    {
        internal IncrementalLexSession Session = null!;
        internal ModuleParseResult Result = null!;
    }
}
