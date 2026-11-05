using asn1;

namespace asn1.Tests;

public class LegacySyntaxParserTests
{
    [Test]
    public void CompatibilityModeMapsAnyToAnExplicitOpenTypeNode()
    {
        TypeFixture fixture = ParseType("ANY", true);

        Assert.That(fixture.Result.Success, Is.True);
        Assert.That(fixture.Result.ConsumedTokenCount, Is.EqualTo(1));
        Assert.That(fixture.Result.Syntax?.Type.Kind, Is.EqualTo(AsnTypeKind.OpenType));
        Assert.That(fixture.Result.Syntax?.LegacyAnySyntax, Is.True);
        Assert.That(fixture.Result.Syntax?.HasDefinedByReference, Is.False);
        Assert.That(fixture.Result.Syntax?.SourceTypeName, Is.EqualTo("ANY"));
        Assert.That(fixture.Session.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void CompatibilityModeRetainsTheAnyDefinedBySelector()
    {
        TypeFixture fixture = ParseType("ANY DEFINED BY selector", true);

        Assert.That(fixture.Result.Success, Is.True);
        Assert.That(fixture.Result.ConsumedTokenCount, Is.EqualTo(4));
        Assert.That(fixture.Result.Syntax?.Type.Kind, Is.EqualTo(AsnTypeKind.OpenType));
        Assert.That(fixture.Result.Syntax?.LegacyAnySyntax, Is.True);
        Assert.That(fixture.Result.Syntax?.HasDefinedByReference, Is.True);
        Assert.That(fixture.Result.Syntax?.DefinedByReference, Is.EqualTo("selector"));
        Assert.That(fixture.Result.Syntax?.SourceTypeName, Is.EqualTo("ANY DEFINED BY selector"));
        Assert.That(fixture.Session.GetDiagnostics(), Is.Empty);
    }

    [TestCase("ANY", 1)]
    [TestCase("ANY DEFINED BY selector", 4)]
    public void LegacyAnyIsRejectedWhenCompatibilityModeIsDisabled(
        string source,
        int expectedConsumedTokens)
    {
        TypeFixture fixture = ParseType(source, false);

        Assert.That(fixture.Result.Success, Is.False);
        Assert.That(fixture.Result.ConsumedTokenCount, Is.EqualTo(expectedConsumedTokens));
        Assert.That(fixture.Result.Syntax, Is.Null);
        Assert.That(
            fixture.Session.GetDiagnostics().Select(diagnostic => diagnostic.Code),
            Is.EqualTo(new[] { "ASNP0222" }));
    }

    [Test]
    public void AnyDefinedByRequiresAComponentIdentifier()
    {
        TypeFixture missingBy = ParseType("ANY DEFINED selector", true);
        TypeFixture missingSelector = ParseType("ANY DEFINED BY INTEGER", true);

        Assert.That(missingBy.Result.Success, Is.False);
        Assert.That(
            missingBy.Session.GetDiagnostics().Select(diagnostic => diagnostic.Code),
            Does.Contain("ASNP0223"));
        Assert.That(missingSelector.Result.Success, Is.False);
        Assert.That(
            missingSelector.Session.GetDiagnostics().Select(diagnostic => diagnostic.Code),
            Does.Contain("ASNP0224"));
    }

    [Test]
    public void CompatibilityModeFlowsIntoNestedComponentTypes()
    {
        TypeFixture fixture = ParseType(
            "SEQUENCE { selector INTEGER, payload ANY DEFINED BY selector }",
            true);

        Assert.That(fixture.Result.Success, Is.True);
        Assert.That(fixture.Result.Syntax?.Type.Components, Has.Length.EqualTo(2));
        Assert.That(
            fixture.Result.Syntax?.Type.Components[1].Type?.Kind,
            Is.EqualTo(AsnTypeKind.OpenType));
        Assert.That(
            fixture.Result.Syntax?.ComponentSyntax[1].TypeSyntax?.DefinedByReference,
            Is.EqualTo("selector"));
        Assert.That(fixture.Session.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void RetiredMacroDefinitionDoesNotConsumeTheFollowingModule()
    {
        string source = "Legacy DEFINITIONS ::= BEGIN "
            + "OLD-MACRO MACRO ::= BEGIN "
            + "TYPE NOTATION ::= String "
            + "VALUE NOTATION ::= value(VALUE INTEGER) "
            + "END "
            + "After ::= BOOLEAN "
            + "END "
            + "Healthy DEFINITIONS ::= BEGIN Good ::= INTEGER END";
        IncrementalParseSession session = new IncrementalParseSession(
            "macro.asn",
            false,
            new AsnResourceLimits(),
            4);
        session.SupplyTextChunk(source, true);

        ModuleParseResult retired = session.ParseNext();
        ModuleParseResult healthy = session.ParseNext();

        Assert.That(retired.Status, Is.EqualTo(ModuleParseStatus.Invalid));
        Assert.That(retired.Module?.Name, Is.EqualTo("Legacy"));
        Assert.That(retired.Module?.BodyTokens.Select(token => token.Text), Does.Contain("After"));
        Assert.That(
            retired.Module?.AssignmentHeads.SelectMany(head => head.Alternatives)
                .Select(alternative => alternative.Name),
            Does.Contain("After"));
        Assert.That(healthy.Status, Is.EqualTo(ModuleParseStatus.Parsed));
        Assert.That(healthy.Module?.Name, Is.EqualTo("Healthy"));
        Assert.That(
            session.GetDiagnostics().Select(diagnostic => diagnostic.Code),
            Does.Contain("ASNP0221"));
        Assert.That(session.ParseNext().Status, Is.EqualTo(ModuleParseStatus.EndOfInput));
    }

    private static TypeFixture ParseType(string source, bool enableLegacyAny)
    {
        IncrementalLexSession session = new IncrementalLexSession(
            "legacy.asn",
            false,
            new AsnResourceLimits());
        session.UseCompilerOptions(new AsnCompilerOptions
        {
            EnableLegacyAny = enableLegacyAny
        });
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

        return new TypeFixture
        {
            Session = session,
            Result = new TypeParser(tokens.ToArray(), session.Diagnostics, true).ParseTypeSyntax()
        };
    }

    private class TypeFixture
    {
        internal IncrementalLexSession Session = null!;
        internal TypeParseResult Result = null!;
    }
}
