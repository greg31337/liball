using asn1;

namespace asn1.Tests;

public class SyntaxGrammarCompilerTests
{
    [Test]
    public void CompilerBuildsAndExecutesDeterministicDefinedSyntaxGrammar()
    {
        GrammarFixture fixture = Bind(
            "Classes DEFINITIONS ::= BEGIN "
                + "GRAMMAR ::= CLASS { "
                + "&Type OPTIONAL, "
                + "&id INTEGER, "
                + "&flag BOOLEAN OPTIONAL, "
                + "&name UTF8String OPTIONAL } "
                + "WITH SYNTAX { [ TYPE &Type ] ID &id [ FLAG &flag ] CODE [ &name ] } "
                + "END");
        ParsedObjectClassSyntax grammar = RequireClass(fixture, "GRAMMAR");
        ObjectParseFixture complete = ParseObject(
            "{ TYPE UTF8String ID 7 FLAG TRUE CODE \"alpha\" }",
            grammar);
        ObjectParseFixture omitted = ParseObject("{ ID 8 CODE }", grammar);

        Assert.That(grammar.SyntaxGrammar, Is.Not.Null,
            string.Join(" | ", fixture.Diagnostics.GetDiagnostics().Select(item => item.Code
                + ":" + item.Message)));
        Assert.That(grammar.SyntaxGrammar?.Items.Length, Is.EqualTo(6));
        Assert.That(grammar.SyntaxGrammar?.Items[0].OptionalGroup, Is.True);
        Assert.That(grammar.SyntaxGrammar?.Items[0].FirstItems.Single().Literal,
            Is.EqualTo("TYPE"));
        Assert.That(grammar.SyntaxGrammar?.Items[5].FirstItems.Single().Field?.Name,
            Is.EqualTo("&name"));
        Assert.That(RequireObject(complete).Object.FieldSettings
            .Select(setting => setting.FieldName),
            Is.EqualTo(new[] { "&Type", "&id", "&flag", "&name" }));
        Assert.That(RequireObject(omitted).Object.FieldSettings
            .Select(setting => setting.FieldName),
            Is.EqualTo(new[] { "&id" }));
        Assert.That(fixture.Values.Success, Is.True);
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [TestCase(
        "CLASS { &Type } WITH SYNTAX { INTEGER &Type }",
        "ASNB0026")]
    [TestCase(
        "CLASS { &Type } WITH SYNTAX { MixedCase &Type }",
        "ASNB0026")]
    [TestCase(
        "CLASS { &Type } WITH SYNTAX { TOKEN &Missing }",
        "ASNB0027")]
    [TestCase(
        "CLASS { &Type } WITH SYNTAX { &Type AGAIN &Type }",
        "ASNB0028")]
    [TestCase(
        "CLASS { &Type, &id INTEGER } WITH SYNTAX { &Type }",
        "ASNB0029")]
    [TestCase(
        "CLASS { &id INTEGER } WITH SYNTAX { [ FLAG ] CODE &id }",
        "ASNB0030")]
    [TestCase(
        "CLASS { &first INTEGER OPTIONAL, &second INTEGER } "
            + "WITH SYNTAX { [ &first ] &second }",
        "ASNB0031")]
    [TestCase(
        "CLASS { &first INTEGER OPTIONAL, &second INTEGER OPTIONAL, &id INTEGER } "
            + "WITH SYNTAX { [ FLAG &first ] [ FLAG &second ] CODE &id }",
        "ASNB0031")]
    [TestCase(
        "CLASS { &flag BOOLEAN OPTIONAL, &id INTEGER } "
            + "WITH SYNTAX { [ FLAG &flag ] &id }",
        "ASNB0032")]
    public void CompilerRejectsInvalidOrNondeterministicTemplates(
        string classSyntax,
        string expectedCode)
    {
        GrammarFixture fixture = Bind(
            "Classes DEFINITIONS ::= BEGIN BAD ::= " + classSyntax + " END");
        ParsedObjectClassSyntax syntax = RequireClass(fixture, "BAD");

        Assert.That(fixture.Values.Success, Is.False);
        Assert.That(syntax.SyntaxGrammar, Is.Null);
        Assert.That(fixture.Diagnostics.GetDiagnostics().Select(item => item.Code),
            Does.Contain(expectedCode));
    }

    [Test]
    public void ParserRequiresASettingWhenOptionalGroupLiteralsArePresent()
    {
        GrammarFixture fixture = Bind(
            "Classes DEFINITIONS ::= BEGIN "
                + "GRAMMAR ::= CLASS { &flag BOOLEAN OPTIONAL, &id INTEGER } "
                + "WITH SYNTAX { [ HAS [ FLAG &flag ] ] CODE &id } END");
        ParsedObjectClassSyntax grammar = RequireClass(fixture, "GRAMMAR");
        ObjectParseFixture parsed = ParseObject("{ HAS CODE 7 }", grammar);

        Assert.That(fixture.Values.Success, Is.True);
        Assert.That(parsed.Result.Success, Is.False);
        Assert.That(parsed.Diagnostics.GetDiagnostics().Select(item => item.Code),
            Does.Contain("ASNP0195"));
    }

    private static ParsedObjectClassSyntax RequireClass(
        GrammarFixture fixture,
        string name)
    {
        return fixture.Tables[0]
            .FindSymbol(name, AsnAssignmentKind.ObjectClass)?.ResolvedObjectClassSyntax
            ?? throw new InvalidOperationException();
    }

    private static ParsedInformationObjectSyntax RequireObject(ObjectParseFixture fixture)
    {
        Assert.That(fixture.Result.Success, Is.True,
            string.Join(" | ", fixture.Diagnostics.GetDiagnostics().Select(item => item.Code
                + ":" + item.Message)));
        Assert.That(fixture.Result.ConsumedTokenCount, Is.EqualTo(fixture.TokenCount));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
        return fixture.Result.Syntax ?? throw new InvalidOperationException();
    }

    private static ObjectParseFixture ParseObject(
        string source,
        ParsedObjectClassSyntax grammar)
    {
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        IncrementalLexSession session = new IncrementalLexSession(
            "defined-object.asn",
            false,
            new AsnResourceLimits());
        session.SupplyTextChunk(source, true);
        System.Collections.Generic.List<AsnToken> tokens =
            new System.Collections.Generic.List<AsnToken>();
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

        AsnToken[] tokenArray = tokens.ToArray();
        diagnostics = session.Diagnostics;
        return new ObjectParseFixture
        {
            Diagnostics = diagnostics,
            TokenCount = tokenArray.Length,
            Result = new InformationObjectParser(
                tokenArray,
                diagnostics,
                true).ParseObjectSyntax(grammar)
        };
    }

    private static GrammarFixture Bind(string source)
    {
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        IncrementalLexSession session = new IncrementalLexSession(
            "syntax-grammar.asn",
            false,
            new AsnResourceLimits());
        session.SupplyTextChunk(source, true);
        ModuleParseResult parsed = new ModuleParser(new TokenCursor(session, 4))
            .ParseModule();
        Assert.That(parsed.Status, Is.EqualTo(ModuleParseStatus.Parsed));
        Assert.That(session.GetDiagnostics(), Is.Empty);
        ModuleSyntax module = parsed.Module ?? throw new InvalidOperationException();
        SymbolTable[] tables = new SymbolTableBuilder(diagnostics).BuildTables([module]);
        new ImportResolver(diagnostics).ResolveImports(tables);
        CompilationGraph graph = new CompilationGraphBuilder(diagnostics).BuildGraph(tables);
        AssignmentClassificationResult classification = new AssignmentClassifier(diagnostics)
            .ClassifyAssignments(tables, graph);
        TypeResolutionResult types = new TypeResolver(diagnostics).ResolveTypes(tables);
        Assert.That(classification.Success, Is.True);
        Assert.That(types.Success, Is.True);
        ValueResolutionResult values = new ValueResolver(diagnostics).ResolveValues(tables);
        return new GrammarFixture
        {
            Diagnostics = diagnostics,
            Tables = tables,
            Values = values
        };
    }

    private class GrammarFixture
    {
        internal DiagnosticBag Diagnostics = null!;
        internal SymbolTable[] Tables = [];
        internal ValueResolutionResult Values = null!;
    }

    private class ObjectParseFixture
    {
        internal DiagnosticBag Diagnostics = null!;
        internal InformationObjectParseResult Result = new InformationObjectParseResult();
        internal int TokenCount;
    }
}
