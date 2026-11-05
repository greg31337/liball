using asn1;

namespace asn1.Tests;

public class TypeParserTests
{
    [TestCase("BOOLEAN", AsnTypeKind.Boolean)]
    [TestCase("INTEGER", AsnTypeKind.Integer)]
    [TestCase("REAL", AsnTypeKind.Real)]
    [TestCase("BIT STRING", AsnTypeKind.BitString)]
    [TestCase("OCTET STRING", AsnTypeKind.OctetString)]
    [TestCase("NULL", AsnTypeKind.Null)]
    [TestCase("OBJECT IDENTIFIER", AsnTypeKind.ObjectIdentifier)]
    [TestCase("RELATIVE-OID", AsnTypeKind.RelativeObjectIdentifier)]
    [TestCase("OID-IRI", AsnTypeKind.ObjectIdentifierIri)]
    [TestCase("RELATIVE-OID-IRI", AsnTypeKind.RelativeObjectIdentifierIri)]
    public void ParserRecognizesEveryScalarTypeInThisGrammarStage(
        string source,
        AsnTypeKind expectedKind)
    {
        TypeFixture fixture = ParseType(source);

        ParsedTypeSyntax syntax = RequireSyntax(fixture.Result);
        Assert.That(fixture.Result.Success, Is.True);
        Assert.That(syntax.Type.Kind, Is.EqualTo(expectedKind));
        Assert.That(fixture.Result.ConsumedTokenCount, Is.EqualTo(fixture.Tokens.Length));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void IntegerNamedNumbersSupportSignedAndDefinedValues()
    {
        TypeFixture fixture = ParseType(
            "INTEGER { zero(0), negative(-1), limit(limitValue), external(Ids.number) }");

        ParsedTypeSyntax syntax = RequireSyntax(fixture.Result);
        Assert.That(fixture.Result.Success, Is.True);
        Assert.That(syntax.Type.NamedValues.Select(item => item.Name), Is.EqualTo(new[]
        {
            "zero", "negative", "limit", "external"
        }));
        Assert.That(syntax.Type.NamedValues[0].Value?.IntegerValue.ToString(), Is.EqualTo("0"));
        Assert.That(syntax.Type.NamedValues[1].Value?.IntegerValue.ToString(), Is.EqualTo("-1"));
        Assert.That(syntax.Type.NamedValues[2].Value?.Kind, Is.EqualTo(AsnValueKind.Defined));
        Assert.That(syntax.Type.NamedValues[2].Value?.TextValue, Is.EqualTo("limitValue"));
        Assert.That(syntax.Type.NamedValues[3].Value?.TextValue, Is.EqualTo("Ids.number"));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void BitStringNamedBitsRetainNumericAndDefinedPositions()
    {
        TypeFixture fixture = ParseType(
            "BIT STRING { first(0), second(1), last(lastBit) }");

        ParsedTypeSyntax syntax = RequireSyntax(fixture.Result);
        Assert.That(fixture.Result.Success, Is.True);
        Assert.That(syntax.Type.Kind, Is.EqualTo(AsnTypeKind.BitString));
        Assert.That(syntax.RootNamedValues.Select(item => item.Name), Is.EqualTo(new[]
        {
            "first", "second", "last"
        }));
        Assert.That(syntax.RootNamedValues[1].Value?.IntegerValue.ToString(), Is.EqualTo("1"));
        Assert.That(syntax.RootNamedValues[2].Value?.TextValue, Is.EqualTo("lastBit"));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void EnumeratedSeparatesRootAndExtensionItemsAndRetainsExceptionTokens()
    {
        TypeFixture fixture = ParseType(
            "ENUMERATED { red, green(3), ... ! 7, blue, violet(9) }");

        ParsedTypeSyntax syntax = RequireSyntax(fixture.Result);
        Assert.That(fixture.Result.Success, Is.True);
        Assert.That(syntax.Type.Kind, Is.EqualTo(AsnTypeKind.Enumerated));
        Assert.That(syntax.Type.Extensible, Is.True);
        Assert.That(syntax.RootNamedValues.Select(item => item.Name), Is.EqualTo(new[] { "red", "green" }));
        Assert.That(
            syntax.ExtensionNamedValues.Select(item => item.Name),
            Is.EqualTo(new[] { "blue", "violet" }));
        Assert.That(
            syntax.ExtensionExceptionTokens.Select(token => token.Text),
            Is.EqualTo(new[] { "!", "7" }));
        Assert.That(syntax.Type.NamedValues.Select(item => item.Name), Is.EqualTo(new[]
        {
            "red", "green", "blue", "violet"
        }));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [TestCase("LocalType", "", "LocalType")]
    [TestCase("Types.RemoteType", "Types", "RemoteType")]
    [TestCase("ALLCAPS", "", "ALLCAPS")]
    public void ParserRecognizesLocalExternalAndLexicallyAmbiguousDefinedTypes(
        string source,
        string expectedModule,
        string expectedName)
    {
        TypeFixture fixture = ParseType(source);

        ParsedTypeSyntax syntax = RequireSyntax(fixture.Result);
        Assert.That(fixture.Result.Success, Is.True);
        Assert.That(syntax.Type.Kind, Is.EqualTo(AsnTypeKind.Defined));
        Assert.That(syntax.ReferenceModule, Is.EqualTo(expectedModule));
        Assert.That(syntax.ReferenceName, Is.EqualTo(expectedName));
        Assert.That(
            syntax.Type.Name,
            Is.EqualTo(expectedModule.Length == 0 ? expectedName : expectedModule + "." + expectedName));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void AssignmentHeadsAttachACompleteParsedGovernorWithoutForcingItsCategory()
    {
        ModuleFixture fixture = ParseModule(
            "Governed DEFINITIONS ::= BEGIN value INTEGER { small(1) } ::= 1 END");

        AssignmentHeadAlternativeSyntax alternative = RequireModule(fixture.Result)
            .AssignmentHeads
            .Single()
            .Alternatives
            .Single(item => item.Name == "value");
        Assert.That(alternative.GovernorType, Is.Not.Null);
        Assert.That(alternative.GovernorType?.Type.Kind, Is.EqualTo(AsnTypeKind.Integer));
        Assert.That(alternative.GovernorType?.Type.NamedValues.Single().Name, Is.EqualTo("small"));
        Assert.That(alternative.CandidateKinds, Is.EqualTo(new[] { AsnAssignmentKind.Value }));
        Assert.That(fixture.Session.GetDiagnostics(), Is.Empty);
    }

    [TestCase("SEQUENCE SIZE (1..MAX) OF INTEGER", AsnTypeKind.SequenceOf)]
    [TestCase("SET SIZE (1..4) OF UTF8String", AsnTypeKind.SetOf)]
    public void CollectionTypesAcceptSizeConstraintBeforeOf(
        string source,
        AsnTypeKind expectedKind)
    {
        TypeFixture fixture = ParseType(source);

        ParsedTypeSyntax syntax = RequireSyntax(fixture.Result);
        Assert.That(fixture.Result.Success, Is.True);
        Assert.That(fixture.Result.ConsumedTokenCount, Is.EqualTo(fixture.Tokens.Length));
        Assert.That(syntax.Type.Kind, Is.EqualTo(expectedKind));
        Assert.That(syntax.ConstraintSyntax.Single().Constraint.Kind,
            Is.EqualTo(AsnConstraintKind.Size));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [TestCase("ENUMERATED", "ASNP0015")]
    [TestCase("BIT OCTET", "ASNP0016")]
    [TestCase("INTEGER { red }", "ASNP0024")]
    [TestCase("BIT STRING { bad(-1) }", "ASNP0026")]
    [TestCase("INTEGER {}", "ASNP0029")]
    public void MalformedTypesProduceSpecificDiagnostics(string source, string expectedCode)
    {
        TypeFixture fixture = ParseType(source);

        Assert.That(fixture.Result.Success, Is.False);
        Assert.That(
            fixture.Diagnostics.GetDiagnostics().Select(item => item.Code),
            Does.Contain(expectedCode));
    }

    private static TypeFixture ParseType(string source)
    {
        IncrementalLexSession session = CreateSession("type.asn", source);
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
        IncrementalLexSession session = CreateSession("governor.asn", source);
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
