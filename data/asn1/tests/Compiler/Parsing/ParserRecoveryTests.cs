using asn1;

namespace asn1.Tests;

public class ParserRecoveryTests
{
    [Test]
    public void ModuleListRetainsInvalidModuleAndContinuesAtNextModule()
    {
        string source = "Broken DEFINITIONS AUTOMATIC junk END "
            + "Healthy DEFINITIONS ::= BEGIN Value ::= INTEGER END";
        IncrementalLexSession session = CreateSession(source);
        TokenCursor cursor = new TokenCursor(session, 4);

        ModuleCollectionParseResult result = new ModuleParser(cursor).ParseModuleList();

        Assert.That(result.Status, Is.EqualTo(ModuleParseStatus.Invalid));
        Assert.That(result.Modules.Select(module => module.Name),
            Is.EqualTo(new[] { "Broken", "Healthy" }));
        Assert.That(result.Modules[0].Recoveries.Single().Boundary, Is.EqualTo("module"));
        Assert.That(result.Modules[0].Recoveries.Single().SkippedTokens.Select(token => token.Text),
            Is.EqualTo(new[] { "junk" }));
        Assert.That(result.Modules[1].AssignmentHeads, Has.Length.EqualTo(1));
        Assert.That(cursor.PeekNext(0).Status, Is.EqualTo(TokenCursorStatus.EndOfInput));
    }

    [Test]
    public void AssignmentRecoveryPreservesLaterAssignmentHeads()
    {
        IncrementalLexSession session = CreateSession(
            "Assignments DEFINITIONS ::= BEGIN ::= INTEGER Good ::= BOOLEAN END");
        TokenCursor cursor = new TokenCursor(session, 4);
        ModuleParseResult result = new ModuleParser(cursor).ParseModule();

        Assert.That(result.Status, Is.EqualTo(ModuleParseStatus.Invalid));
        Assert.That(result.Module?.AssignmentHeads, Has.Length.EqualTo(2));
        Assert.That(result.Module?.AssignmentHeads[1].Alternatives.Select(item => item.Name),
            Does.Contain("Good"));
        Assert.That(result.Module?.Recoveries.Select(item => item.Boundary),
            Does.Contain("assignment"));
        Assert.That(session.GetDiagnostics().Select(item => item.Code),
            Does.Contain("ASNP0013"));
    }

    [Test]
    public void ComponentRecoveryKeepsValidComponentsAfterIndependentErrors()
    {
        LexFixture lexical = Lex(
            "SEQUENCE { good INTEGER, broken, alsoBad DEFAULT, final BOOLEAN }");
        TypeParseResult result = new TypeParser(
            lexical.Tokens,
            lexical.Diagnostics,
            true).ParseTypeSyntax();

        Assert.That(result.Success, Is.False);
        Assert.That(result.ConsumedTokenCount, Is.EqualTo(lexical.Tokens.Length));
        Assert.That(result.Syntax?.Type.Components.Select(item => item.Name),
            Is.EqualTo(new[] { "good", "final" }));
        Assert.That(result.Syntax?.Recoveries, Has.Length.EqualTo(2));
        Assert.That(result.Syntax?.Recoveries.SelectMany(item => item.SkippedTokens)
            .Select(token => token.Text),
            Is.EqualTo(new[] { "broken", "alsoBad", "DEFAULT" }));
        Assert.That(lexical.Diagnostics.GetDiagnostics().Count(item => item.Code == "ASNP0014"),
            Is.EqualTo(2));
    }

    [Test]
    public void ConstraintRecoveryReturnsTheParsedRootAndConsumesItsBoundary()
    {
        LexFixture lexical = Lex("(1, 2)");
        ConstraintParseResult result = new ConstraintParser(
            lexical.Tokens,
            lexical.Diagnostics,
            true).ParseConstraintSyntax(new AsnType { Kind = AsnTypeKind.Integer });

        Assert.That(result.Success, Is.False);
        Assert.That(result.ConsumedTokenCount, Is.EqualTo(lexical.Tokens.Length));
        Assert.That(result.Syntax?.RootElement?.Value?.IntegerValue.ToString(), Is.EqualTo("1"));
        Assert.That(result.Syntax?.Recoveries.Single().Boundary, Is.EqualTo("constraint"));
        Assert.That(result.Syntax?.Recoveries.Single().SkippedTokens.Select(token => token.Text),
            Is.EqualTo(new[] { ",", "2", ")" }));
        Assert.That(lexical.Diagnostics.GetDiagnostics().Select(item => item.Code),
            Does.Contain("ASNP0112"));
    }

    [Test]
    public void ObjectRecoveryKeepsSettingsBeforeAndAfterMalformedSettings()
    {
        ParsedObjectClassSyntax objectClass = ParseClass(
            "CLASS { &one INTEGER, &two INTEGER, &three INTEGER }");
        LexFixture lexical = Lex("{ &one 1, garbage, &two, &three 3 }");
        InformationObjectParseResult result = new InformationObjectParser(
            lexical.Tokens,
            lexical.Diagnostics,
            true).ParseObjectSyntax(objectClass);

        Assert.That(result.Success, Is.False);
        Assert.That(result.ConsumedTokenCount, Is.EqualTo(lexical.Tokens.Length));
        Assert.That(result.Syntax?.Object.FieldSettings.Select(item => item.FieldName),
            Is.EqualTo(new[] { "&one", "&three" }));
        Assert.That(result.Syntax?.Recoveries, Has.Length.EqualTo(2));
        Assert.That(lexical.Diagnostics.GetDiagnostics().Select(item => item.Code),
            Does.Contain("ASNP0178"));
        Assert.That(lexical.Diagnostics.GetDiagnostics().Select(item => item.Code),
            Does.Contain("ASNP0187"));
    }

    [Test]
    public void ParameterListRecoveryReportsMultipleErrorsAndKeepsLaterParameters()
    {
        LexFixture lexical = Lex(
            "{ INTEGER :, , BOOLEAN : valueParam, TypeParam }");
        ParameterListParseResult result = new ParameterizedParser(
            lexical.Tokens,
            lexical.Diagnostics,
            true).ParseFormalParameterListSyntax();

        Assert.That(result.Success, Is.False);
        Assert.That(result.ConsumedTokenCount, Is.EqualTo(lexical.Tokens.Length));
        Assert.That(result.Parameters.Select(item => item.Parameter.Name),
            Is.EqualTo(new[] { "valueParam", "TypeParam" }));
        Assert.That(result.Recoveries.Single().SkippedTokens.Select(token => token.Text),
            Is.EqualTo(new[] { "INTEGER", ":" }));
        Assert.That(lexical.Diagnostics.GetDiagnostics().Select(item => item.Code),
            Does.Contain("ASNP0205"));
        Assert.That(lexical.Diagnostics.GetDiagnostics().Select(item => item.Code),
            Does.Contain("ASNP0202"));
    }

    [Test]
    public void EncodingControlRecoveryContinuesToLaterSections()
    {
        IncrementalLexSession session = CreateSession(
            "Controls DEFINITIONS ::= BEGIN Value ::= INTEGER "
            + "ENCODING-CONTROL 123 invalid "
            + "ENCODING-CONTROL TAG invalid "
            + "ENCODING-CONTROL XER valid END");
        ModuleParseResult result = new ModuleParser(new TokenCursor(session, 4)).ParseModule();

        Assert.That(result.Status, Is.EqualTo(ModuleParseStatus.Parsed));
        Assert.That(result.Module?.EncodingControlSections, Has.Length.EqualTo(3));
        Assert.That(result.Module?.EncodingControlSections[0].Recoveries.Single().Boundary,
            Is.EqualTo("encoding control"));
        Assert.That(result.Module?.EncodingControlSections[2].EncodingReference,
            Is.EqualTo("XER"));
        Assert.That(session.GetDiagnostics().Select(item => item.Code),
            Is.EqualTo(new[] { "ASNP0004", "ASNP0005" }));
    }

    private static ParsedObjectClassSyntax ParseClass(string source)
    {
        LexFixture lexical = Lex(source);
        ObjectClassParseResult result = new ObjectClassParser(
            lexical.Tokens,
            lexical.Diagnostics,
            true).ParseObjectClassSyntax();
        Assert.That(result.Success, Is.True);
        return result.Syntax ?? throw new InvalidOperationException();
    }

    private static IncrementalLexSession CreateSession(string source)
    {
        IncrementalLexSession session = new IncrementalLexSession(
            "recovery.asn",
            false,
            new AsnResourceLimits());
        session.SupplyTextChunk(source, true);
        return session;
    }

    private static LexFixture Lex(string source)
    {
        IncrementalLexSession session = CreateSession(source);
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
        public AsnToken[] Tokens = [];
        public DiagnosticBag Diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
    }
}
