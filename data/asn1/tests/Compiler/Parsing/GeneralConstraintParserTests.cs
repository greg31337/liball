using asn1;

namespace asn1.Tests;

public class GeneralConstraintParserTests
{
    [Test]
    public void UserDefinedConstraintRetainsOpaqueParametersAndException()
    {
        ConstraintFixture fixture = ParseConstraint(
            "(CONSTRAINED BY { INTEGER : 5, PayloadType, OCTET STRING : 'AB'H } ! 9)",
            new AsnType { Kind = AsnTypeKind.BitString });

        ParsedConstraintSyntax syntax = RequireSyntax(fixture);
        ParsedGeneralConstraintSyntax general = syntax.GeneralConstraints.Single();
        Assert.That(syntax.RootElement?.Kind, Is.EqualTo(AsnConstraintKind.UserDefined));
        Assert.That(general.Parameters.Length, Is.EqualTo(3));
        Assert.That(general.Parameters.Select(JoinTokens), Is.EqualTo(new[]
        {
            "INTEGER : 5",
            "PayloadType",
            "OCTET STRING : 'AB'H"
        }));
        Assert.That(syntax.RootElement?.Children.Select(item => item.Text), Is.EqualTo(new[]
        {
            "INTEGER : 5",
            "PayloadType",
            "OCTET STRING : 'AB'H"
        }));
        Assert.That(syntax.ExceptionValueSyntax?.Value.IntegerValue.ToString(), Is.EqualTo("9"));
    }

    [Test]
    public void UserDefinedConstraintAllowsAnEmptyBody()
    {
        ConstraintFixture fixture = ParseConstraint(
            "(CONSTRAINED BY {})",
            new AsnType { Kind = AsnTypeKind.OctetString });

        ParsedConstraintSyntax syntax = RequireSyntax(fixture);
        Assert.That(syntax.RootElement?.Kind, Is.EqualTo(AsnConstraintKind.UserDefined));
        Assert.That(syntax.GeneralConstraints.Single().BodyTokens, Is.Empty);
        Assert.That(syntax.GeneralConstraints.Single().Parameters, Is.Empty);
    }

    [Test]
    public void UserDefinedParameterCommasInsideNestedValuesAreNotSeparators()
    {
        ConstraintFixture fixture = ParseConstraint(
            "(CONSTRAINED BY { SEQUENCE { a INTEGER, b BOOLEAN } : { a 1, b TRUE }, TypeRef })",
            new AsnType { Kind = AsnTypeKind.BitString });

        ParsedGeneralConstraintSyntax syntax = RequireSyntax(fixture).GeneralConstraints.Single();
        Assert.That(syntax.Parameters.Length, Is.EqualTo(2));
        Assert.That(JoinTokens(syntax.Parameters[0]),
            Is.EqualTo("SEQUENCE { a INTEGER , b BOOLEAN } : { a 1 , b TRUE }"));
        Assert.That(JoinTokens(syntax.Parameters[1]), Is.EqualTo("TypeRef"));
    }

    [Test]
    public void SimpleTableConstraintRetainsTheObjectSetReference()
    {
        ConstraintFixture fixture = ParseConstraint(
            "({ErrorSet})",
            new AsnType { Kind = AsnTypeKind.OpenType });

        ParsedConstraintSyntax syntax = RequireSyntax(fixture);
        ParsedGeneralConstraintSyntax general = syntax.GeneralConstraints.Single();
        Assert.That(syntax.RootElement?.Kind, Is.EqualTo(AsnConstraintKind.SimpleTable));
        Assert.That(syntax.RootElement?.Name, Is.EqualTo("ErrorSet"));
        Assert.That(general.ObjectSetTokens.Select(token => token.Text), Is.EqualTo(new[] { "ErrorSet" }));
        Assert.That(general.AtNotations, Is.Empty);
    }

    [Test]
    public void InstanceOfTypeAcceptsAnIntegratedSimpleTableConstraint()
    {
        TypeFixture fixture = ParseType("INSTANCE OF MESSAGE-CLASS ({MessageSet})");

        ParsedConstraintSyntax syntax = RequireTypeConstraint(fixture);
        Assert.That(syntax.RootElement?.Kind, Is.EqualTo(AsnConstraintKind.SimpleTable));
        Assert.That(syntax.RootElement?.Name, Is.EqualTo("MessageSet"));
    }

    [Test]
    public void ComponentRelationParsesOutermostRelativeAndMultiLevelAtNotations()
    {
        ConstraintFixture fixture = ParseConstraint(
            "({ErrorSet}{@errorCategory, @.errorCode, @...nested.identifier})",
            new AsnType { Kind = AsnTypeKind.OpenType });

        ParsedConstraintSyntax syntax = RequireSyntax(fixture);
        ParsedGeneralConstraintSyntax general = syntax.GeneralConstraints.Single();
        Assert.That(syntax.RootElement?.Kind, Is.EqualTo(AsnConstraintKind.ComponentRelation));
        Assert.That(general.AtNotations.Length, Is.EqualTo(3));
        Assert.That(general.AtNotations[0].Relative, Is.False);
        Assert.That(general.AtNotations[0].Levels, Is.EqualTo(0));
        Assert.That(general.AtNotations[0].ComponentIdentifiers, Is.EqualTo(new[] { "errorCategory" }));
        Assert.That(general.AtNotations[1].Relative, Is.True);
        Assert.That(general.AtNotations[1].Levels, Is.EqualTo(0));
        Assert.That(general.AtNotations[1].ComponentIdentifiers, Is.EqualTo(new[] { "errorCode" }));
        Assert.That(general.AtNotations[2].Relative, Is.True);
        Assert.That(general.AtNotations[2].Levels, Is.EqualTo(2));
        Assert.That(general.AtNotations[2].ComponentIdentifiers,
            Is.EqualTo(new[] { "nested", "identifier" }));
        Assert.That(syntax.RootElement?.Children.Select(item => item.Name), Is.EqualTo(new[]
        {
            "errorCategory", "errorCode", "nested.identifier"
        }));
    }

    [Test]
    public void ContentsConstraintParsesContainingType()
    {
        TypeFixture fixture = ParseType("OCTET STRING (CONTAINING UTF8String (SIZE (1..20)))");

        ParsedConstraintSyntax syntax = RequireTypeConstraint(fixture);
        ParsedGeneralConstraintSyntax general = syntax.GeneralConstraints.Single();
        Assert.That(syntax.RootElement?.Kind, Is.EqualTo(AsnConstraintKind.Contents));
        Assert.That(syntax.RootElement?.Text, Is.EqualTo("CONTAINING"));
        Assert.That(syntax.RootElement?.Type?.Kind, Is.EqualTo(AsnTypeKind.Utf8String));
        Assert.That(syntax.RootElement?.Type?.Constraints.Single().Kind, Is.EqualTo(AsnConstraintKind.Size));
        Assert.That(general.ContainedTypeSyntax?.ConstraintSyntax.Single().RootElement?.Kind,
            Is.EqualTo(AsnConstraintKind.Size));
        Assert.That(general.EncodingValueSyntax, Is.Null);
    }

    [Test]
    public void ContentsConstraintParsesEncodingObjectIdentifier()
    {
        TypeFixture fixture = ParseType("BIT STRING (ENCODED BY { 1 2 840 113549 })");

        ParsedConstraintSyntax syntax = RequireTypeConstraint(fixture);
        ParsedGeneralConstraintSyntax general = syntax.GeneralConstraints.Single();
        Assert.That(syntax.RootElement?.Text, Is.EqualTo("ENCODED BY"));
        Assert.That(syntax.RootElement?.Type, Is.Null);
        Assert.That(syntax.RootElement?.Value?.Kind, Is.EqualTo(AsnValueKind.ObjectIdentifier));
        Assert.That(general.EncodingValueSyntax?.ObjectIdentifierValue?.Arcs.Select(item => item.ToString()),
            Is.EqualTo(new[] { "1", "2", "840", "113549" }));
    }

    [Test]
    public void ContentsConstraintCombinesTypeAndEncodingIdentifier()
    {
        TypeFixture fixture = ParseType(
            "OCTET STRING (CONTAINING SEQUENCE { id INTEGER } ENCODED BY encodingRules)");

        ParsedConstraintSyntax syntax = RequireTypeConstraint(fixture);
        ParsedGeneralConstraintSyntax general = syntax.GeneralConstraints.Single();
        Assert.That(syntax.RootElement?.Text, Is.EqualTo("CONTAINING ENCODED BY"));
        Assert.That(general.ContainedTypeSyntax?.Type.Kind, Is.EqualTo(AsnTypeKind.Sequence));
        Assert.That(general.ContainedTypeSyntax?.Type.Components.Single().Name, Is.EqualTo("id"));
        Assert.That(general.EncodingValueSyntax?.Value.Kind, Is.EqualTo(AsnValueKind.Defined));
        Assert.That(general.EncodingValueSyntax?.ReferenceName, Is.EqualTo("encodingRules"));
    }

    [TestCase("(CONSTRAINED {})", "ASNP0138")]
    [TestCase("(CONSTRAINED BY value)", "ASNP0139")]
    [TestCase("(CONSTRAINED BY { , INTEGER })", "ASNP0140")]
    [TestCase("({})", "ASNP0141")]
    [TestCase("({ErrorSet}{@field)", "ASNP0142")]
    [TestCase("({ErrorSet}{field})", "ASNP0147")]
    [TestCase("({ErrorSet}{@})", "ASNP0148")]
    [TestCase("({ErrorSet}{@field.})", "ASNP0149")]
    [TestCase("({ErrorSet}{@first @second})", "ASNP0150")]
    [TestCase("({ErrorSet}{@first,})", "ASNP0151")]
    [TestCase("(CONSTRAINED BY {} | 1)", "ASNP0152")]
    [TestCase("(CONSTRAINED BY {}, ...)", "ASNP0153")]
    public void MalformedOpaqueAndTableConstraintsProduceSpecificDiagnostics(
        string source,
        string expectedCode)
    {
        ConstraintFixture fixture = ParseConstraint(source, new AsnType { Kind = AsnTypeKind.OpenType });

        Assert.That(fixture.Result.Success, Is.False);
        Assert.That(fixture.Diagnostics.GetDiagnostics().Select(item => item.Code),
            Does.Contain(expectedCode));
    }

    [TestCase("OCTET STRING (CONTAINING)", "ASNP0143")]
    [TestCase("OCTET STRING (ENCODED value)", "ASNP0144")]
    [TestCase("OCTET STRING (ENCODED BY)", "ASNP0145")]
    public void MalformedContentsConstraintsProduceSpecificDiagnostics(
        string source,
        string expectedCode)
    {
        TypeFixture fixture = ParseType(source);

        Assert.That(fixture.Result.Success, Is.False);
        Assert.That(fixture.Diagnostics.GetDiagnostics().Select(item => item.Code),
            Does.Contain(expectedCode));
    }

    private static ParsedConstraintSyntax RequireSyntax(ConstraintFixture fixture)
    {
        Assert.That(fixture.Result.Success, Is.True);
        Assert.That(fixture.Result.Syntax, Is.Not.Null);
        Assert.That(fixture.Result.ConsumedTokenCount, Is.EqualTo(fixture.TokenCount));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
        return fixture.Result.Syntax ?? throw new InvalidOperationException();
    }

    private static ParsedConstraintSyntax RequireTypeConstraint(TypeFixture fixture)
    {
        Assert.That(fixture.Result.Success, Is.True);
        Assert.That(fixture.Result.Syntax, Is.Not.Null);
        Assert.That(fixture.Result.ConsumedTokenCount, Is.EqualTo(fixture.TokenCount));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
        return (fixture.Result.Syntax ?? throw new InvalidOperationException()).ConstraintSyntax.Single();
    }

    private static string JoinTokens(AsnToken[] tokens)
    {
        return string.Join(" ", tokens.Select(token => token.Text));
    }

    private static ConstraintFixture ParseConstraint(string source, AsnType parentType)
    {
        LexFixture lexical = Lex(source);
        ConstraintParser parser = new ConstraintParser(lexical.Tokens, lexical.Diagnostics, true);
        return new ConstraintFixture
        {
            TokenCount = lexical.Tokens.Length,
            Diagnostics = lexical.Diagnostics,
            Result = parser.ParseConstraintSyntax(parentType)
        };
    }

    private static TypeFixture ParseType(string source)
    {
        LexFixture lexical = Lex(source);
        TypeParser parser = new TypeParser(lexical.Tokens, lexical.Diagnostics, true);
        return new TypeFixture
        {
            TokenCount = lexical.Tokens.Length,
            Diagnostics = lexical.Diagnostics,
            Result = parser.ParseTypeSyntax()
        };
    }

    private static LexFixture Lex(string source)
    {
        IncrementalLexSession session = new IncrementalLexSession(
            "general-constraint.asn",
            false,
            new AsnResourceLimits());
        session.SupplyTextChunk(source, true);
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

        return new LexFixture
        {
            Tokens = tokens.ToArray(),
            Diagnostics = session.Diagnostics
        };
    }

    private class LexFixture
    {
        internal AsnToken[] Tokens = [];
        internal DiagnosticBag Diagnostics = null!;
    }

    private class ConstraintFixture
    {
        internal int TokenCount;
        internal DiagnosticBag Diagnostics = null!;
        internal ConstraintParseResult Result = null!;
    }

    private class TypeFixture
    {
        internal int TokenCount;
        internal DiagnosticBag Diagnostics = null!;
        internal TypeParseResult Result = null!;
    }
}
