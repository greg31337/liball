using asn1;

namespace asn1.Tests;

public class ConstraintParserTests
{
    [TestCase("INTEGER (1 | 2 ^ 3)")]
    [TestCase("INTEGER (1 UNION 2 INTERSECTION 3)")]
    public void UnionAndIntersectionUseStandardPrecedence(string source)
    {
        TypeFixture fixture = ParseType(source);

        ParsedConstraintSyntax constraint = RequireSingleConstraint(fixture);
        Assert.That(constraint.RootElement?.Kind, Is.EqualTo(AsnConstraintKind.Union));
        Assert.That(constraint.RootElement?.Children.Length, Is.EqualTo(2));
        Assert.That(constraint.RootElement?.Children[0].Value?.IntegerValue.ToString(), Is.EqualTo("1"));
        Assert.That(
            constraint.RootElement?.Children[1].Kind,
            Is.EqualTo(AsnConstraintKind.Intersection));
        Assert.That(constraint.Constraint.Elements.Length, Is.EqualTo(2));
        Assert.That(constraint.Constraint.Elements[1].Kind, Is.EqualTo(AsnConstraintKind.Intersection));
        Assert.That(
            constraint.RootElement?.Children[1].Children.Select(ValueText),
            Is.EqualTo(new[] { "2", "3" }));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void ParenthesizedSetsAndExclusionsRetainTheirOperandTree()
    {
        TypeFixture fixture = ParseType("INTEGER ((1 | 2) EXCEPT 3)");

        ParsedConstraintSyntax constraint = RequireSingleConstraint(fixture);
        Assert.That(constraint.RootElement?.Kind, Is.EqualTo(AsnConstraintKind.Except));
        Assert.That(constraint.RootElement?.Children[0].Kind, Is.EqualTo(AsnConstraintKind.Union));
        Assert.That(
            constraint.RootElement?.Children[0].Children.Select(ValueText),
            Is.EqualTo(new[] { "1", "2" }));
        Assert.That(constraint.RootElement?.Children[1].Value?.IntegerValue.ToString(), Is.EqualTo("3"));
    }

    [Test]
    public void AllExceptIsRepresentedAsAComplementOperand()
    {
        TypeFixture fixture = ParseType("INTEGER (ALL EXCEPT (0 | 1))");

        ParsedConstraintSyntax constraint = RequireSingleConstraint(fixture);
        Assert.That(constraint.Constraint.Kind, Is.EqualTo(AsnConstraintKind.AllExcept));
        Assert.That(constraint.RootElement?.Children.Single().Kind, Is.EqualTo(AsnConstraintKind.Union));
        Assert.That(
            constraint.RootElement?.Children.Single().Children.Select(ValueText),
            Is.EqualTo(new[] { "0", "1" }));
    }

    [Test]
    public void ParenthesesAllowAllExceptToParticipateInSetArithmetic()
    {
        TypeFixture fixture = ParseType("INTEGER ((ALL EXCEPT 0) | 1)");

        ParsedConstraintSyntax constraint = RequireSingleConstraint(fixture);
        Assert.That(constraint.RootElement?.Kind, Is.EqualTo(AsnConstraintKind.Union));
        Assert.That(constraint.RootElement?.Children[0].Kind, Is.EqualTo(AsnConstraintKind.AllExcept));
        Assert.That(constraint.RootElement?.Children[1].Value?.IntegerValue.ToString(), Is.EqualTo("1"));
    }

    [Test]
    public void ExtensionAdditionAndExceptionAreKeptSeparateFromTheRoot()
    {
        TypeFixture fixture = ParseType("INTEGER (1, ..., 2 | 3 ! -7)");

        ParsedConstraintSyntax constraint = RequireSingleConstraint(fixture);
        Assert.That(constraint.Constraint.Extensible, Is.True);
        Assert.That(constraint.RootElement?.Value?.IntegerValue.ToString(), Is.EqualTo("1"));
        Assert.That(constraint.AdditionalElement?.Kind, Is.EqualTo(AsnConstraintKind.Union));
        Assert.That(
            constraint.AdditionalElement?.Children.Select(ValueText),
            Is.EqualTo(new[] { "2", "3" }));
        Assert.That(constraint.ExceptionElement?.Kind, Is.EqualTo(AsnConstraintKind.Exception));
        Assert.That(constraint.ExceptionValueSyntax?.Value.IntegerValue.ToString(), Is.EqualTo("-7"));
        Assert.That(
            constraint.Constraint.Elements.Select(item => item.Kind),
            Is.EqualTo(new[]
            {
                AsnConstraintKind.SingleValue,
                AsnConstraintKind.Extension,
                AsnConstraintKind.Exception
            }));
        Assert.That(constraint.Constraint.Elements[1].Children.Single(), Is.SameAs(constraint.AdditionalElement));
        Assert.That(constraint.SourceTokens.Select(token => token.Text), Is.EqualTo(new[]
        {
            "(", "1", ",", "...", ",", "2", "|", "3", "!", "-", "7", ")"
        }));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void BareExtensionMarkerCreatesAnEmptyExtensionPartition()
    {
        TypeFixture fixture = ParseType("INTEGER (1, ...)");

        ParsedConstraintSyntax constraint = RequireSingleConstraint(fixture);
        Assert.That(constraint.Constraint.Extensible, Is.True);
        Assert.That(constraint.AdditionalElement, Is.Null);
        Assert.That(constraint.Constraint.Elements[1].Kind, Is.EqualTo(AsnConstraintKind.Extension));
        Assert.That(constraint.Constraint.Elements[1].Children, Is.Empty);
    }

    [Test]
    public void TypedAndDefinedExceptionIdentifiersAreParsed()
    {
        TypeFixture typedFixture = ParseType("INTEGER (1, ... ! UTF8String : \"reason\")");
        TypeFixture definedFixture = ParseType("INTEGER (1, ... ! problemCode)");

        ParsedConstraintSyntax typed = RequireSingleConstraint(typedFixture);
        ParsedConstraintSyntax defined = RequireSingleConstraint(definedFixture);
        Assert.That(typed.ExceptionTypeSyntax?.Type.Kind, Is.EqualTo(AsnTypeKind.Utf8String));
        Assert.That(typed.ExceptionValueSyntax?.Value.TextValue, Is.EqualTo("reason"));
        Assert.That(defined.ExceptionTypeSyntax, Is.Null);
        Assert.That(defined.ExceptionValueSyntax?.Value.Kind, Is.EqualTo(AsnValueKind.Defined));
        Assert.That(defined.ExceptionValueSyntax?.ReferenceName, Is.EqualTo("problemCode"));
        Assert.That(typedFixture.Diagnostics.GetDiagnostics(), Is.Empty);
        Assert.That(definedFixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void SequentialConstraintsAreAttachedInSourceOrder()
    {
        TypeFixture fixture = ParseType("INTEGER (1 | 2) (ALL EXCEPT 0)");

        ParsedTypeSyntax syntax = RequireSyntax(fixture.Result);
        Assert.That(syntax.Type.Constraints.Length, Is.EqualTo(2));
        Assert.That(
            syntax.Type.Constraints.Select(item => item.Kind),
            Is.EqualTo(new[] { AsnConstraintKind.Union, AsnConstraintKind.AllExcept }));
        Assert.That(syntax.ConstraintSyntax.Length, Is.EqualTo(2));
        Assert.That(fixture.Result.ConsumedTokenCount, Is.EqualTo(fixture.Tokens.Length));
    }

    [Test]
    public void ConstraintsAreParsedRecursivelyInsideConstructedTypes()
    {
        TypeFixture fixture = ParseType(
            "SEQUENCE { id INTEGER (1 | 2), items SEQUENCE OF INTEGER (0 | 1) }");

        ParsedTypeSyntax syntax = RequireSyntax(fixture.Result);
        AsnType idType = syntax.Type.Components.Single(item => item.Name == "id").Type
            ?? throw new InvalidOperationException();
        AsnType itemsType = syntax.Type.Components.Single(item => item.Name == "items").Type
            ?? throw new InvalidOperationException();
        Assert.That(idType.Constraints.Single().Kind, Is.EqualTo(AsnConstraintKind.Union));
        Assert.That(itemsType.Kind, Is.EqualTo(AsnTypeKind.SequenceOf));
        Assert.That(itemsType.Constraints, Is.Empty);
        Assert.That(itemsType.ElementType?.Constraints.Single().Kind, Is.EqualTo(AsnConstraintKind.Union));
        Assert.That(fixture.Result.ConsumedTokenCount, Is.EqualTo(fixture.Tokens.Length));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void CollectionConstraintBeforeOfAppliesToTheCollectionType()
    {
        TypeFixture fixture = ParseType("SEQUENCE ({ 1 }, ..., { 2 }) OF INTEGER");

        ParsedTypeSyntax syntax = RequireSyntax(fixture.Result);
        Assert.That(syntax.Type.Kind, Is.EqualTo(AsnTypeKind.SequenceOf));
        Assert.That(syntax.Type.Constraints.Single().Extensible, Is.True);
        Assert.That(syntax.Type.ElementType?.Kind, Is.EqualTo(AsnTypeKind.Integer));
        Assert.That(syntax.ConstraintSyntax.Single().AdditionalElement?.Value?.Kind,
            Is.EqualTo(AsnValueKind.SequenceOf));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [TestCase("INTEGER ()", "ASNP0111")]
    [TestCase("INTEGER (1 |)", "ASNP0116")]
    [TestCase("INTEGER (1, 2)", "ASNP0112")]
    [TestCase("INTEGER (ALL 1)", "ASNP0119")]
    [TestCase("INTEGER (1", "ASNP0115")]
    [TestCase("INTEGER (1 !)", "ASNP0114")]
    [TestCase("INTEGER (1 EXCEPT 2 EXCEPT 3)", "ASNP0115")]
    [TestCase("INTEGER (1 | ALL EXCEPT 2)", "ASNP0116")]
    public void MalformedConstraintsProduceSpecificDiagnostics(string source, string expectedCode)
    {
        TypeFixture fixture = ParseType(source);

        Assert.That(fixture.Result.Success, Is.False);
        Assert.That(
            fixture.Diagnostics.GetDiagnostics().Select(item => item.Code),
            Does.Contain(expectedCode));
    }

    private static string ValueText(AsnConstraintElement element)
    {
        return element.Value?.IntegerValue.ToString() ?? "";
    }

    private static ParsedConstraintSyntax RequireSingleConstraint(TypeFixture fixture)
    {
        ParsedTypeSyntax syntax = RequireSyntax(fixture.Result);
        Assert.That(fixture.Result.Success, Is.True);
        Assert.That(fixture.Result.ConsumedTokenCount, Is.EqualTo(fixture.Tokens.Length));
        return syntax.ConstraintSyntax.Single();
    }

    private static ParsedTypeSyntax RequireSyntax(TypeParseResult result)
    {
        Assert.That(result.Syntax, Is.Not.Null);
        return result.Syntax ?? throw new InvalidOperationException();
    }

    private static TypeFixture ParseType(string source)
    {
        IncrementalLexSession session = new IncrementalLexSession(
            "constraint.asn",
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

        TypeParser parser = new TypeParser(tokens.ToArray(), session.Diagnostics, true);
        return new TypeFixture
        {
            Tokens = tokens.ToArray(),
            Diagnostics = session.Diagnostics,
            Result = parser.ParseTypeSyntax()
        };
    }

    private class TypeFixture
    {
        internal AsnToken[] Tokens = [];
        internal DiagnosticBag Diagnostics = null!;
        internal TypeParseResult Result = null!;
    }
}
