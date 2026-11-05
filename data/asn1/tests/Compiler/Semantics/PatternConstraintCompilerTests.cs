using asn1;

namespace asn1.Tests;

public class PatternConstraintCompilerTests
{
    [Test]
    public void CompilerBuildsGroupedAlternativeAndRepetitionPrograms()
    {
        ResolutionFixture fixture = Resolve(
            "Patterns DEFINITIONS ::= BEGIN "
                + "Code ::= UTF8String (PATTERN \"^(ab|c[0-9]{2,4})+$\") END");
        PatternProgram program = RequirePatterns(fixture, "Code").Single();

        Assert.That(program.Root?.Kind, Is.EqualTo(PatternNodeKind.Sequence));
        Assert.That(program.Source, Is.EqualTo("^(ab|c[0-9]{2,4})+$"));
        Assert.That(program.Matches("ab"), Is.True);
        Assert.That(program.Matches("abc12c3456"), Is.True);
        Assert.That(program.Matches("c1"), Is.False);
        Assert.That(program.Matches("prefix-ab"), Is.False);
        Assert.That(program.Matches("ab-suffix"), Is.False);
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void MatcherSupportsSearchAnchorsClassesEscapesAndUnicodeScalars()
    {
        ResolutionFixture fixture = Resolve(
            "Patterns DEFINITIONS ::= BEGIN "
                + "Needle ::= UTF8String (PATTERN \"ab\"); "
                + "Negated ::= UTF8String (PATTERN \"^[^A-C]{2}$\"); "
                + "Literal ::= UTF8String (PATTERN \"^a\\+b\\?$\"); "
                + "Unicode ::= UTF8String (PATTERN \"^[😀-🙏]+$\") END");
        PatternProgram needle = RequirePatterns(fixture, "Needle").Single();
        PatternProgram negated = RequirePatterns(fixture, "Negated").Single();
        PatternProgram literal = RequirePatterns(fixture, "Literal").Single();
        PatternProgram unicode = RequirePatterns(fixture, "Unicode").Single();

        Assert.That(needle.Matches("xxabyy"), Is.True);
        Assert.That(needle.Matches("acb"), Is.False);
        Assert.That(negated.Matches("DZ"), Is.True);
        Assert.That(negated.Matches("AZ"), Is.False);
        Assert.That(literal.Matches("a+b?"), Is.True);
        Assert.That(literal.Matches("aaab"), Is.False);
        Assert.That(unicode.Matches("😀🙏"), Is.True);
        Assert.That(unicode.Matches("😀A"), Is.False);
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void CompilerRetainsSequentialAndInheritedPatternPrograms()
    {
        ResolutionFixture fixture = Resolve(
            "Patterns DEFINITIONS ::= BEGIN "
                + "Base ::= UTF8String (PATTERN \"^A.*$\"); "
                + "Derived ::= Base (PATTERN \"^.*Z$\") END");
        PatternProgram[] basePatterns = RequirePatterns(fixture, "Base");
        PatternProgram[] derivedPatterns = RequirePatterns(fixture, "Derived");

        Assert.That(basePatterns, Has.Length.EqualTo(1));
        Assert.That(derivedPatterns, Has.Length.EqualTo(2));
        Assert.That(derivedPatterns.Select(program => program.Source),
            Is.EqualTo(new[] { "^A.*$", "^.*Z$" }));
        Assert.That(derivedPatterns.All(program => program.Matches("ABZ")), Is.True);
        Assert.That(derivedPatterns[0].Matches("XBZ"), Is.False);
        Assert.That(derivedPatterns[1].Matches("ABX"), Is.False);
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void CompilerDiagnosesInvalidPatternNotation()
    {
        ResolutionFixture fixture = Resolve(
            "Patterns DEFINITIONS ::= BEGIN "
                + "Unclosed ::= UTF8String (PATTERN \"(abc\"); "
                + "Range ::= UTF8String (PATTERN \"[z-a]\"); "
                + "Repeat ::= UTF8String (PATTERN \"a{4,2}\"); "
                + "Anchor ::= UTF8String (PATTERN \"a^b\") END");
        AsnDiagnostic[] diagnostics = fixture.Diagnostics.GetDiagnostics();

        Assert.That(fixture.Values.Success, Is.False);
        Assert.That(diagnostics.Count(diagnostic => diagnostic.Code == "ASNC0003"),
            Is.EqualTo(4));
        Assert.That(diagnostics.Where(diagnostic => diagnostic.Code == "ASNC0003")
            .All(diagnostic => diagnostic.Span != null), Is.True);
        Assert.That(diagnostics.Where(diagnostic => diagnostic.Code == "ASNC0003")
            .All(diagnostic => diagnostic.Message.Contains("character")), Is.True);
        Assert.That(RequirePatterns(fixture, "Unclosed"), Is.Empty);
    }

    [Test]
    public void ParserRejectsInvalidUnicodeEscapesAndMultipleQuantifiers()
    {
        PatternParseResult surrogate = new PatternParser("\\uD800").ParsePattern();
        PatternParseResult repeated = new PatternParser("a**").ParsePattern();
        PatternParseResult emptyAlternative = new PatternParser("a|").ParsePattern();

        Assert.That(surrogate.Success, Is.False);
        Assert.That(surrogate.ErrorMessage, Does.Contain("scalar"));
        Assert.That(repeated.Success, Is.False);
        Assert.That(repeated.ErrorMessage, Does.Contain("multiple"));
        Assert.That(emptyAlternative.Success, Is.False);
        Assert.That(emptyAlternative.ErrorMessage, Does.Contain("empty"));
    }

    private static PatternProgram[] RequirePatterns(
        ResolutionFixture fixture,
        string name)
    {
        return fixture.Tables[0].FindSymbol(name, AsnAssignmentKind.Type)
            ?.ResolvedType?.Patterns
            ?? throw new InvalidOperationException();
    }

    private static ResolutionFixture Resolve(string source)
    {
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        IncrementalLexSession session = new IncrementalLexSession(
            "patterns.asn",
            false,
            new AsnResourceLimits());
        session.SupplyTextChunk(source, true);
        ModuleParseResult parsed = new ModuleParser(new TokenCursor(session, 4)).ParseModule();
        Assert.That(parsed.Status, Is.EqualTo(ModuleParseStatus.Parsed));
        Assert.That(session.GetDiagnostics(), Is.Empty);
        SymbolTable[] tables = new SymbolTableBuilder(diagnostics).BuildTables(
            [parsed.Module ?? throw new InvalidOperationException()]);
        new ImportResolver(diagnostics).ResolveImports(tables);
        CompilationGraph graph = new CompilationGraphBuilder(diagnostics).BuildGraph(tables);
        AssignmentClassificationResult classification = new AssignmentClassifier(diagnostics)
            .ClassifyAssignments(tables, graph);
        TypeResolutionResult types = new TypeResolver(diagnostics).ResolveTypes(tables);
        Assert.That(classification.Success, Is.True);
        Assert.That(types.Success, Is.True);
        ValueResolutionResult values = new ValueResolver(diagnostics).ResolveValues(tables);
        return new ResolutionFixture
        {
            Diagnostics = diagnostics,
            Tables = tables,
            Values = values
        };
    }

    private class ResolutionFixture
    {
        internal DiagnosticBag Diagnostics = null!;
        internal SymbolTable[] Tables = [];
        internal ValueResolutionResult Values = null!;
    }
}
