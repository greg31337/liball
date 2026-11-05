using asn1;

namespace asn1.Tests;

public class CharacterAndTimeTypeParserTests
{
    [TestCase("BMPString", AsnTypeKind.BmpString)]
    [TestCase("GeneralString", AsnTypeKind.GeneralString)]
    [TestCase("GraphicString", AsnTypeKind.GraphicString)]
    [TestCase("IA5String", AsnTypeKind.Ia5String)]
    [TestCase("ISO646String", AsnTypeKind.VisibleString)]
    [TestCase("NumericString", AsnTypeKind.NumericString)]
    [TestCase("PrintableString", AsnTypeKind.PrintableString)]
    [TestCase("TeletexString", AsnTypeKind.TeletexString)]
    [TestCase("T61String", AsnTypeKind.TeletexString)]
    [TestCase("UniversalString", AsnTypeKind.UniversalString)]
    [TestCase("UTF8String", AsnTypeKind.Utf8String)]
    [TestCase("VideotexString", AsnTypeKind.VideotexString)]
    [TestCase("VisibleString", AsnTypeKind.VisibleString)]
    public void ParserRecognizesEveryRestrictedCharacterStringType(
        string source,
        AsnTypeKind expectedKind)
    {
        TypeFixture fixture = ParseType(source);

        ParsedTypeSyntax syntax = RequireSyntax(fixture.Result);
        Assert.That(fixture.Result.Success, Is.True);
        Assert.That(syntax.Type.Kind, Is.EqualTo(expectedKind));
        Assert.That(syntax.SourceTypeName, Is.EqualTo(source));
        Assert.That(fixture.Result.ConsumedTokenCount, Is.EqualTo(fixture.Tokens.Length));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void ParserRecognizesUnrestrictedCharacterStringAndObjectDescriptor()
    {
        TypeFixture unrestricted = ParseType("CHARACTER STRING");
        TypeFixture descriptor = ParseType("ObjectDescriptor");

        Assert.That(RequireSyntax(unrestricted.Result).Type.Kind, Is.EqualTo(AsnTypeKind.CharacterString));
        Assert.That(RequireSyntax(unrestricted.Result).SourceTypeName, Is.EqualTo("CHARACTER STRING"));
        Assert.That(RequireSyntax(descriptor.Result).Type.Kind, Is.EqualTo(AsnTypeKind.ObjectDescriptor));
        Assert.That(RequireSyntax(descriptor.Result).SourceTypeName, Is.EqualTo("ObjectDescriptor"));
        Assert.That(unrestricted.Diagnostics.GetDiagnostics(), Is.Empty);
        Assert.That(descriptor.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [TestCase("GeneralizedTime", AsnTypeKind.GeneralizedTime)]
    [TestCase("UTCTime", AsnTypeKind.UtcTime)]
    [TestCase("TIME", AsnTypeKind.Time)]
    [TestCase("DATE", AsnTypeKind.Date)]
    [TestCase("TIME-OF-DAY", AsnTypeKind.TimeOfDay)]
    [TestCase("DATE-TIME", AsnTypeKind.DateTime)]
    [TestCase("DURATION", AsnTypeKind.Duration)]
    public void ParserRecognizesLegacyAndModernTimeTypes(string source, AsnTypeKind expectedKind)
    {
        TypeFixture fixture = ParseType(source);

        ParsedTypeSyntax syntax = RequireSyntax(fixture.Result);
        Assert.That(fixture.Result.Success, Is.True);
        Assert.That(syntax.Type.Kind, Is.EqualTo(expectedKind));
        Assert.That(syntax.SourceTypeName, Is.EqualTo(source));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void CharacterAndTimeTypesAreAttachedToAssignmentGovernors()
    {
        string source = "Useful DEFINITIONS ::= BEGIN\n"
            + "text UTF8String ::= \"hello\"\n"
            + "timestamp DATE-TIME ::= \"2026-08-28T10:15:00\"\n"
            + "END";
        IncrementalLexSession session = CreateSession("governors.asn", source);
        TokenCursor cursor = new TokenCursor(session, 4);

        ModuleSyntax module = RequireModule(new ModuleParser(cursor).ParseModule());

        AssignmentHeadAlternativeSyntax text = module.AssignmentHeads[0]
            .Alternatives.Single(item => item.Name == "text");
        AssignmentHeadAlternativeSyntax timestamp = module.AssignmentHeads[1]
            .Alternatives.Single(item => item.Name == "timestamp");
        Assert.That(text.GovernorType?.Type.Kind, Is.EqualTo(AsnTypeKind.Utf8String));
        Assert.That(timestamp.GovernorType?.Type.Kind, Is.EqualTo(AsnTypeKind.DateTime));
        Assert.That(session.GetDiagnostics(), Is.Empty);
    }

    [TestCase("CHARACTER", "ASNP0017")]
    [TestCase("CHARACTER UTF8String", "ASNP0017")]
    public void MalformedUnrestrictedCharacterStringTypesAreDiagnosed(
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
        IncrementalLexSession session = CreateSession("character-type.asn", source);
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
}
