using asn1;

namespace asn1.Tests;

public class SubtypeElementParserTests
{
    [Test]
    public void SingleValuesAndContainedSubtypesAreDistinguished()
    {
        TypeFixture singleFixture = ParseType("INTEGER (7)");
        TypeFixture explicitFixture = ParseType("INTEGER (INCLUDES BaseInteger)");
        TypeFixture implicitFixture = ParseType("INTEGER (BaseInteger)");

        ParsedConstraintSyntax single = RequireSingleConstraint(singleFixture);
        ParsedConstraintSyntax explicitSubtype = RequireSingleConstraint(explicitFixture);
        ParsedConstraintSyntax implicitSubtype = RequireSingleConstraint(implicitFixture);
        Assert.That(single.RootElement?.Kind, Is.EqualTo(AsnConstraintKind.SingleValue));
        Assert.That(single.RootElement?.Value?.IntegerValue.ToString(), Is.EqualTo("7"));
        Assert.That(explicitSubtype.RootElement?.Kind, Is.EqualTo(AsnConstraintKind.ContainedSubtype));
        Assert.That(explicitSubtype.RootElement?.Type?.Name, Is.EqualTo("BaseInteger"));
        Assert.That(implicitSubtype.RootElement?.Kind, Is.EqualTo(AsnConstraintKind.ContainedSubtype));
        Assert.That(implicitSubtype.SubtypeElements.Single().TypeSyntax?.ReferenceName,
            Is.EqualTo("BaseInteger"));
    }

    [Test]
    public void IntegerRangesRetainBoundsInfinityAndEndpointInclusion()
    {
        TypeFixture finiteFixture = ParseType("INTEGER (-5 < .. 9007199254740993)");
        TypeFixture unboundedFixture = ParseType("INTEGER (MIN .. < MAX)");

        AsnConstraintElement finite = RequireSingleConstraint(finiteFixture).RootElement
            ?? throw new InvalidOperationException();
        AsnConstraintElement unbounded = RequireSingleConstraint(unboundedFixture).RootElement
            ?? throw new InvalidOperationException();
        Assert.That(finite.Kind, Is.EqualTo(AsnConstraintKind.ValueRange));
        Assert.That(finite.HasLowerBound, Is.True);
        Assert.That(finite.LowerBound.ToString(), Is.EqualTo("-5"));
        Assert.That(finite.HasUpperBound, Is.True);
        Assert.That(finite.UpperBound.ToString(), Is.EqualTo("9007199254740993"));
        Assert.That(finite.LowerEndpointIncluded, Is.False);
        Assert.That(finite.UpperEndpointIncluded, Is.True);
        Assert.That(unbounded.LowerEndpointUnbounded, Is.True);
        Assert.That(unbounded.UpperEndpointUnbounded, Is.True);
        Assert.That(unbounded.LowerEndpointIncluded, Is.True);
        Assert.That(unbounded.UpperEndpointIncluded, Is.False);
    }

    [Test]
    public void NonIntegerRangesRetainEndpointValues()
    {
        TypeFixture fixture = ParseType("UTF8String (\"a\" .. < \"z\")");

        AsnConstraintElement range = RequireSingleConstraint(fixture).RootElement
            ?? throw new InvalidOperationException();
        Assert.That(range.Kind, Is.EqualTo(AsnConstraintKind.ValueRange));
        Assert.That(range.LowerValue?.TextValue, Is.EqualTo("a"));
        Assert.That(range.UpperValue?.TextValue, Is.EqualTo("z"));
        Assert.That(range.UpperEndpointIncluded, Is.False);
        Assert.That(range.HasLowerBound, Is.False);
        Assert.That(range.HasUpperBound, Is.False);
    }

    [Test]
    public void SizeAndPermittedAlphabetContainNestedConstraints()
    {
        TypeFixture sizeFixture = ParseType("OCTET STRING (SIZE (1..MAX))");
        TypeFixture alphabetFixture = ParseType("IA5String (FROM (\"A\" | \"B\"..\"Z\"))");

        ParsedConstraintSyntax size = RequireSingleConstraint(sizeFixture);
        ParsedConstraintSyntax alphabet = RequireSingleConstraint(alphabetFixture);
        Assert.That(size.RootElement?.Kind, Is.EqualTo(AsnConstraintKind.Size));
        Assert.That(size.RootElement?.Children.Single().Kind, Is.EqualTo(AsnConstraintKind.ValueRange));
        Assert.That(size.SubtypeElements.Single().NestedConstraintSyntax?.RootElement?.UpperEndpointUnbounded,
            Is.True);
        Assert.That(alphabet.RootElement?.Kind, Is.EqualTo(AsnConstraintKind.PermittedAlphabet));
        Assert.That(alphabet.RootElement?.Children.Single().Kind, Is.EqualTo(AsnConstraintKind.Union));
        Assert.That(
            alphabet.RootElement?.Children.Single().Children.Select(item => item.Kind),
            Is.EqualTo(new[] { AsnConstraintKind.SingleValue, AsnConstraintKind.ValueRange }));
    }

    [Test]
    public void PatternAndPropertySettingsRetainDecodedText()
    {
        TypeFixture patternFixture = ParseType("UTF8String (PATTERN \"[A-Z]+\")");
        TypeFixture settingsFixture = ParseType(
            "TIME (SETTINGS \"Basic=Time Midnight=Start\")");

        AsnConstraintElement pattern = RequireSingleConstraint(patternFixture).RootElement
            ?? throw new InvalidOperationException();
        AsnConstraintElement settings = RequireSingleConstraint(settingsFixture).RootElement
            ?? throw new InvalidOperationException();
        Assert.That(pattern.Kind, Is.EqualTo(AsnConstraintKind.Pattern));
        Assert.That(pattern.Text, Is.EqualTo("[A-Z]+"));
        Assert.That(pattern.Value?.Kind, Is.EqualTo(AsnValueKind.CharacterString));
        Assert.That(settings.Kind, Is.EqualTo(AsnConstraintKind.PropertySettings));
        Assert.That(settings.Text, Is.EqualTo("Basic=Time Midnight=Start"));
    }

    [Test]
    public void OpenTypeParentProducesATypeConstraint()
    {
        ConstraintFixture fixture = ParseConstraint(
            "(UTF8String (SIZE (1..20)))",
            new AsnType { Kind = AsnTypeKind.OpenType });

        ParsedConstraintSyntax syntax = RequireConstraintSyntax(fixture.Result);
        Assert.That(fixture.Result.Success, Is.True);
        Assert.That(syntax.RootElement?.Kind, Is.EqualTo(AsnConstraintKind.Type));
        Assert.That(syntax.RootElement?.Type?.Kind, Is.EqualTo(AsnTypeKind.Utf8String));
        Assert.That(syntax.RootElement?.Type?.Constraints.Single().Kind, Is.EqualTo(AsnConstraintKind.Size));
        Assert.That(syntax.SubtypeElements.Single().TypeSyntax?.ConstraintSyntax.Single().RootElement?.Kind,
            Is.EqualTo(AsnConstraintKind.Size));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void WithComponentConstrainsTheCollectionElementType()
    {
        AsnType parent = new AsnType
        {
            Kind = AsnTypeKind.SequenceOf,
            ElementType = new AsnType { Kind = AsnTypeKind.Integer }
        };
        ConstraintFixture fixture = ParseConstraint("(WITH COMPONENT (0..10))", parent);

        ParsedConstraintSyntax syntax = RequireConstraintSyntax(fixture.Result);
        Assert.That(fixture.Result.Success, Is.True);
        Assert.That(syntax.RootElement?.Kind, Is.EqualTo(AsnConstraintKind.WithComponent));
        Assert.That(syntax.RootElement?.Children.Single().Kind, Is.EqualTo(AsnConstraintKind.ValueRange));
        Assert.That(
            syntax.SubtypeElements.Single().NestedConstraintSyntax?.RootElement?.UpperBound.ToString(),
            Is.EqualTo("10"));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void WithComponentsParsesFullValueAndPresenceConstraints()
    {
        TypeFixture fixture = ParseType(
            "SEQUENCE { count INTEGER OPTIONAL, label UTF8String OPTIONAL } "
            + "(WITH COMPONENTS { count (0..9) PRESENT, label (SIZE (1..20)) OPTIONAL })");

        ParsedConstraintSyntax syntax = RequireSingleConstraint(fixture);
        ParsedSubtypeElementSyntax subtype = syntax.SubtypeElements.Single();
        Assert.That(syntax.RootElement?.Kind, Is.EqualTo(AsnConstraintKind.WithComponents));
        Assert.That(syntax.RootElement?.Text, Is.EqualTo("FULL"));
        Assert.That(subtype.PartialSpecification, Is.False);
        Assert.That(subtype.NamedConstraints.Select(item => item.Name),
            Is.EqualTo(new[] { "count", "label" }));
        Assert.That(subtype.NamedConstraints.Select(item => item.Presence),
            Is.EqualTo(new[] { "PRESENT", "OPTIONAL" }));
        Assert.That(subtype.NamedConstraints[0].ValueConstraintSyntax?.RootElement?.Kind,
            Is.EqualTo(AsnConstraintKind.ValueRange));
        Assert.That(subtype.NamedConstraints[1].ValueConstraintSyntax?.RootElement?.Kind,
            Is.EqualTo(AsnConstraintKind.Size));
        Assert.That(syntax.RootElement?.Children[1].Name, Is.EqualTo("label"));
        Assert.That(syntax.RootElement?.Children[1].Text, Is.EqualTo("OPTIONAL"));
    }

    [Test]
    public void WithComponentsParsesPartialPresenceOnlySpecification()
    {
        TypeFixture fixture = ParseType(
            "CHOICE { text UTF8String, number INTEGER } "
            + "(WITH COMPONENTS { ..., text PRESENT, number ABSENT })");

        ParsedConstraintSyntax syntax = RequireSingleConstraint(fixture);
        ParsedSubtypeElementSyntax subtype = syntax.SubtypeElements.Single();
        Assert.That(subtype.PartialSpecification, Is.True);
        Assert.That(syntax.RootElement?.Text, Is.EqualTo("PARTIAL"));
        Assert.That(subtype.NamedConstraints.Select(item => item.Presence),
            Is.EqualTo(new[] { "PRESENT", "ABSENT" }));
        Assert.That(subtype.NamedConstraints.All(item => item.ValueConstraintSyntax == null), Is.True);
    }

    [TestCase("INTEGER (INCLUDES)", "ASNP0125")]
    [TestCase("OCTET STRING (SIZE 1)", "ASNP0126")]
    [TestCase("UTF8String (PATTERN)", "ASNP0127")]
    [TestCase("TIME (SETTINGS 1)", "ASNP0128")]
    [TestCase("SEQUENCE OF INTEGER (WITH UNKNOWN)", "ASNP0129")]
    [TestCase("SEQUENCE { a INTEGER } (WITH COMPONENTS a PRESENT)", "ASNP0130")]
    [TestCase("SEQUENCE { a INTEGER } (WITH COMPONENTS { ... a PRESENT })", "ASNP0131")]
    [TestCase("SEQUENCE { a INTEGER } (WITH COMPONENTS { a PRESENT, })", "ASNP0132")]
    [TestCase("SEQUENCE { a INTEGER } (WITH COMPONENTS {})", "ASNP0133")]
    [TestCase("INTEGER (MIN)", "ASNP0136")]
    [TestCase("INTEGER (1..)", "ASNP0137")]
    public void MalformedSubtypeElementsProduceSpecificDiagnostics(string source, string expectedCode)
    {
        TypeFixture fixture = ParseType(source);

        Assert.That(fixture.Result.Success, Is.False);
        Assert.That(fixture.Diagnostics.GetDiagnostics().Select(item => item.Code),
            Does.Contain(expectedCode));
    }

    private static ParsedConstraintSyntax RequireSingleConstraint(TypeFixture fixture)
    {
        ParsedTypeSyntax syntax = RequireTypeSyntax(fixture.Result);
        Assert.That(fixture.Result.Success, Is.True);
        Assert.That(fixture.Result.ConsumedTokenCount, Is.EqualTo(fixture.Tokens.Length));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
        return syntax.ConstraintSyntax.Single();
    }

    private static ParsedTypeSyntax RequireTypeSyntax(TypeParseResult result)
    {
        Assert.That(result.Syntax, Is.Not.Null);
        return result.Syntax ?? throw new InvalidOperationException();
    }

    private static ParsedConstraintSyntax RequireConstraintSyntax(ConstraintParseResult result)
    {
        Assert.That(result.Syntax, Is.Not.Null);
        return result.Syntax ?? throw new InvalidOperationException();
    }

    private static TypeFixture ParseType(string source)
    {
        LexFixture lexical = Lex(source);
        TypeParser parser = new TypeParser(lexical.Tokens, lexical.Diagnostics, true);
        return new TypeFixture
        {
            Tokens = lexical.Tokens,
            Diagnostics = lexical.Diagnostics,
            Result = parser.ParseTypeSyntax()
        };
    }

    private static ConstraintFixture ParseConstraint(string source, AsnType parentType)
    {
        LexFixture lexical = Lex(source);
        ConstraintParser parser = new ConstraintParser(lexical.Tokens, lexical.Diagnostics, true);
        return new ConstraintFixture
        {
            Diagnostics = lexical.Diagnostics,
            Result = parser.ParseConstraintSyntax(parentType)
        };
    }

    private static LexFixture Lex(string source)
    {
        IncrementalLexSession session = new IncrementalLexSession(
            "subtype-constraint.asn",
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

    private class TypeFixture
    {
        internal AsnToken[] Tokens = [];
        internal DiagnosticBag Diagnostics = null!;
        internal TypeParseResult Result = null!;
    }

    private class ConstraintFixture
    {
        internal DiagnosticBag Diagnostics = null!;
        internal ConstraintParseResult Result = null!;
    }
}
