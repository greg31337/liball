using asn1;

namespace asn1.Tests;

public class ConstructedTypeParserTests
{
    [TestCase("SEQUENCE {}", AsnTypeKind.Sequence)]
    [TestCase("SET {}", AsnTypeKind.Set)]
    [TestCase("CHOICE { number INTEGER }", AsnTypeKind.Choice)]
    public void ParserRecognizesStructuredTypeKinds(string source, AsnTypeKind expectedKind)
    {
        TypeFixture fixture = ParseType(source);

        ParsedTypeSyntax syntax = RequireSyntax(fixture.Result);
        Assert.That(fixture.Result.Success, Is.True);
        Assert.That(syntax.Type.Kind, Is.EqualTo(expectedKind));
        Assert.That(fixture.Result.ConsumedTokenCount, Is.EqualTo(fixture.Tokens.Length));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void ChoiceAlternativesRetainNamesAndNestedTypes()
    {
        TypeFixture fixture = ParseType(
            "CHOICE { enabled BOOLEAN, details SEQUENCE { code INTEGER } }");

        ParsedTypeSyntax syntax = RequireSyntax(fixture.Result);
        Assert.That(fixture.Result.Success, Is.True);
        Assert.That(syntax.Type.Components.Select(item => item.Name),
            Is.EqualTo(new[] { "enabled", "details" }));
        Assert.That(syntax.Type.Components[0].Type?.Kind, Is.EqualTo(AsnTypeKind.Boolean));
        Assert.That(syntax.Type.Components[1].Type?.Kind, Is.EqualTo(AsnTypeKind.Sequence));
        Assert.That(syntax.Type.Components[1].Type?.Components.Single().Name, Is.EqualTo("code"));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [TestCase("SEQUENCE OF INTEGER", AsnTypeKind.SequenceOf, "", AsnTypeKind.Integer)]
    [TestCase("SET OF item UTF8String", AsnTypeKind.SetOf, "item", AsnTypeKind.Utf8String)]
    [TestCase("SEQUENCE OF row SET { id INTEGER }", AsnTypeKind.SequenceOf, "row", AsnTypeKind.Set)]
    public void CollectionTypesSupportPlainAndNamedElementTypes(
        string source,
        AsnTypeKind expectedKind,
        string expectedName,
        AsnTypeKind expectedElementKind)
    {
        TypeFixture fixture = ParseType(source);

        ParsedTypeSyntax syntax = RequireSyntax(fixture.Result);
        Assert.That(fixture.Result.Success, Is.True);
        Assert.That(syntax.Type.Kind, Is.EqualTo(expectedKind));
        Assert.That(syntax.ElementName, Is.EqualTo(expectedName));
        Assert.That(syntax.Type.ElementType?.Kind, Is.EqualTo(expectedElementKind));
        Assert.That(fixture.Result.ConsumedTokenCount, Is.EqualTo(fixture.Tokens.Length));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void SequenceComponentsRetainOptionalAndDefaultSyntax()
    {
        TypeFixture fixture = ParseType(
            "SEQUENCE { required INTEGER, note UTF8String OPTIONAL, settings Settings DEFAULT { mode fast, count 3 } }");

        ParsedTypeSyntax syntax = RequireSyntax(fixture.Result);
        Assert.That(fixture.Result.Success, Is.True);
        Assert.That(syntax.Type.Components.Select(item => item.Name),
            Is.EqualTo(new[] { "required", "note", "settings" }));
        Assert.That(syntax.Type.Components[0].Optional, Is.False);
        Assert.That(syntax.Type.Components[1].Optional, Is.True);
        Assert.That(syntax.Type.Components[2].HasDefaultValue, Is.True);
        Assert.That(
            syntax.Type.Components[2].DefaultValue?.TextValue,
            Is.EqualTo("{ mode fast , count 3 }"));
        Assert.That(
            syntax.ComponentSyntax[2].DefaultValueTokens.Select(item => item.Text),
            Is.EqualTo(new[] { "{", "mode", "fast", ",", "count", "3", "}" }));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void ComponentsOfRetainsTheIncludedTypeForLaterExpansion()
    {
        TypeFixture fixture = ParseType(
            "SET { local INTEGER, COMPONENTS OF CommonFields, final BOOLEAN }");

        ParsedTypeSyntax syntax = RequireSyntax(fixture.Result);
        Assert.That(fixture.Result.Success, Is.True);
        Assert.That(syntax.Type.Components.Select(item => item.Name),
            Is.EqualTo(new[] { "local", "final" }));
        Assert.That(syntax.ComponentSyntax, Has.Length.EqualTo(3));
        Assert.That(syntax.ComponentSyntax[1].ComponentsOf, Is.True);
        Assert.That(syntax.ComponentSyntax[1].TypeSyntax?.Type.Kind, Is.EqualTo(AsnTypeKind.Defined));
        Assert.That(syntax.ComponentSyntax[1].TypeSyntax?.ReferenceName, Is.EqualTo("CommonFields"));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void SequenceSeparatesRootExtensionsVersionGroupsAndTrailingRoot()
    {
        TypeFixture fixture = ParseType(
            "SEQUENCE { root INTEGER, ... ! 7, added BOOLEAN, "
            + "[[ 2: grouped UTF8String, other INTEGER OPTIONAL ]], "
            + "..., tail OCTET STRING }");

        ParsedTypeSyntax syntax = RequireSyntax(fixture.Result);
        Assert.That(fixture.Result.Success, Is.True);
        Assert.That(syntax.Type.Extensible, Is.True);
        Assert.That(syntax.HasExtensionMarker, Is.True);
        Assert.That(syntax.HasExtensionEndMarker, Is.True);
        Assert.That(syntax.ExtensionExceptionTokens.Select(item => item.Text),
            Is.EqualTo(new[] { "!", "7" }));
        Assert.That(syntax.Type.Components.Select(item => item.Name),
            Is.EqualTo(new[] { "root", "added", "grouped", "other", "tail" }));
        Assert.That(syntax.Type.Components.Select(item => item.Extension),
            Is.EqualTo(new[] { false, true, true, true, false }));

        ParsedComponentSyntax grouped = syntax.ComponentSyntax.Single(
            item => item.Component?.Name == "grouped");
        ParsedComponentSyntax other = syntax.ComponentSyntax.Single(
            item => item.Component?.Name == "other");
        ParsedComponentSyntax tail = syntax.ComponentSyntax.Single(
            item => item.Component?.Name == "tail");
        Assert.That(grouped.ExtensionGroupIndex, Is.EqualTo(0));
        Assert.That(grouped.HasVersionNumber, Is.True);
        Assert.That(grouped.VersionNumber.ToString(), Is.EqualTo("2"));
        Assert.That(other.ExtensionGroupIndex, Is.EqualTo(0));
        Assert.That(other.Component?.Optional, Is.True);
        Assert.That(tail.AfterExtensionEnd, Is.True);
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void ChoiceSupportsVersionedExtensionAlternativeGroupsAndFinalMarker()
    {
        TypeFixture fixture = ParseType(
            "CHOICE { root INTEGER, ..., [[ 3: text UTF8String, flag BOOLEAN ]], ... }");

        ParsedTypeSyntax syntax = RequireSyntax(fixture.Result);
        Assert.That(fixture.Result.Success, Is.True);
        Assert.That(syntax.Type.Components.Select(item => item.Name),
            Is.EqualTo(new[] { "root", "text", "flag" }));
        Assert.That(syntax.Type.Components.Select(item => item.Extension),
            Is.EqualTo(new[] { false, true, true }));
        Assert.That(syntax.ComponentSyntax[1].VersionNumber.ToString(), Is.EqualTo("3"));
        Assert.That(syntax.HasExtensionEndMarker, Is.True);
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void AssignmentGovernorsAcceptCompleteConstructedTypes()
    {
        ModuleFixture fixture = ParseModule(
            "Constructed DEFINITIONS ::= BEGIN "
            + "value SEQUENCE { id INTEGER, labels SEQUENCE OF UTF8String OPTIONAL } ::= { id 1 } "
            + "END");

        AssignmentHeadAlternativeSyntax alternative = RequireModule(fixture.Result)
            .AssignmentHeads
            .Single()
            .Alternatives
            .Single(item => item.Name == "value");
        Assert.That(alternative.GovernorType?.Type.Kind, Is.EqualTo(AsnTypeKind.Sequence));
        Assert.That(alternative.GovernorType?.Type.Components, Has.Length.EqualTo(2));
        Assert.That(
            alternative.GovernorType?.Type.Components[1].Type?.Kind,
            Is.EqualTo(AsnTypeKind.SequenceOf));
        Assert.That(fixture.Session.GetDiagnostics(), Is.Empty);
    }

    [TestCase("CHOICE {}", "ASNP0033")]
    [TestCase("SEQUENCE", "ASNP0031")]
    [TestCase("SET { item INTEGER, }", "ASNP0037")]
    [TestCase("SEQUENCE { COMPONENTS INTEGER }", "ASNP0045")]
    [TestCase("SEQUENCE { [[ item INTEGER ]] }", "ASNP0035")]
    [TestCase("SEQUENCE { ..., [[]] }", "ASNP0040")]
    [TestCase("CHOICE { item INTEGER OPTIONAL }", "ASNP0047")]
    [TestCase("SEQUENCE { item INTEGER DEFAULT }", "ASNP0049")]
    [TestCase("SEQUENCE { item INTEGER", "ASNP0039")]
    public void MalformedConstructedTypesProduceSpecificDiagnostics(
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
        IncrementalLexSession session = CreateSession("constructed-type.asn", source);
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
        IncrementalLexSession session = CreateSession("constructed-governor.asn", source);
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
