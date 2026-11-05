using asn1;

namespace asn1.Tests;

public class PermittedAlphabetSolverTests
{
    [Test]
    public void SolverIntersectsFromConstraintsWithNamedRepertoires()
    {
        ResolutionFixture fixture = Resolve(
            "Alphabets DEFINITIONS ::= BEGIN "
                + "Digits ::= NumericString (FROM (\"0\"..\"9\" | \"A\")); "
                + "Printable ::= PrintableString (FROM (ALL EXCEPT \"A\")); "
                + "Ascii ::= IA5String; "
                + "Named ::= UTF8String (FROM (INCLUDES Ascii)) END");
        PermittedAlphabetResult digits = RequireAlphabet(fixture, "Digits");
        PermittedAlphabetResult printable = RequireAlphabet(fixture, "Printable");
        PermittedAlphabetResult named = RequireAlphabet(fixture, "Named");

        Assert.That(digits.RootAlphabetSize, Is.EqualTo(10));
        Assert.That(digits.GetRootIndex('0'), Is.EqualTo(0));
        Assert.That(digits.GetRootIndex('9'), Is.EqualTo(9));
        Assert.That(digits.GetRootIndex('A'), Is.EqualTo(-1));
        Assert.That(printable.RootAlphabetSize, Is.EqualTo(73));
        Assert.That(printable.GetRootIndex('A'), Is.EqualTo(-1));
        Assert.That(printable.GetRootIndex('@'), Is.EqualTo(-1));
        Assert.That(printable.GetRootIndex('B'), Is.GreaterThanOrEqualTo(0));
        Assert.That(named.RootAlphabetSize, Is.EqualTo(128));
        Assert.That(named.GetRootScalar(127), Is.EqualTo(127));
        Assert.That(named.GetRootIndex(128), Is.EqualTo(-1));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void SolverUsesUnicodeScalarOrderAndExcludesSurrogates()
    {
        ResolutionFixture fixture = Resolve(
            "Alphabets DEFINITIONS ::= BEGIN "
                + "Emoji ::= UTF8String (FROM (\"A\" | \"😀\"..\"😁\")); "
                + "Boundary ::= UTF8String (FROM (\"\uD7FF\"..\"\uE000\")) END");
        PermittedAlphabetResult emoji = RequireAlphabet(fixture, "Emoji");
        PermittedAlphabetResult boundary = RequireAlphabet(fixture, "Boundary");

        Assert.That(emoji.RootAlphabetSize, Is.EqualTo(3));
        Assert.That(emoji.GetRootIndex('A'), Is.EqualTo(0));
        Assert.That(emoji.GetRootIndex(128512), Is.EqualTo(1));
        Assert.That(emoji.GetRootIndex(128513), Is.EqualTo(2));
        Assert.That(emoji.GetRootScalar(2), Is.EqualTo(128513));
        Assert.That(boundary.Root?.Intervals, Has.Length.EqualTo(2));
        Assert.That(boundary.RootAlphabetSize, Is.EqualTo(2));
        Assert.That(boundary.GetRootIndex(55295), Is.EqualTo(0));
        Assert.That(boundary.GetRootIndex(55296), Is.EqualTo(-1));
        Assert.That(boundary.GetRootIndex(57343), Is.EqualTo(-1));
        Assert.That(boundary.GetRootIndex(57344), Is.EqualTo(1));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void SolverCombinesSequentialAndInheritedAlphabetConstraints()
    {
        ResolutionFixture fixture = Resolve(
            "Alphabets DEFINITIONS ::= BEGIN "
                + "Base ::= IA5String (FROM (\"A\"..\"Z\")); "
                + "Derived ::= Base (FROM (\"M\"..\"z\")) "
                + "(FROM (ALL EXCEPT \"Q\")) END");
        PermittedAlphabetResult alphabet = RequireAlphabet(fixture, "Derived");

        Assert.That(alphabet.RootAlphabetSize, Is.EqualTo(13));
        Assert.That(alphabet.GetRootIndex('M'), Is.EqualTo(0));
        Assert.That(alphabet.GetRootIndex('Q'), Is.EqualTo(-1));
        Assert.That(alphabet.GetRootIndex('Z'), Is.EqualTo(12));
        Assert.That(alphabet.GetRootIndex('a'), Is.EqualTo(-1));
        Assert.That(alphabet.Root?.Intervals, Has.Length.EqualTo(2));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void SolverPreservesAlphabetExtensionPartitionsForPerIndexes()
    {
        ResolutionFixture fixture = Resolve(
            "Alphabets DEFINITIONS ::= BEGIN "
                + "Letters ::= UTF8String "
                + "(FROM (\"A\"..\"Z\", ..., \"a\"..\"z\")) END");
        PermittedAlphabetResult alphabet = RequireAlphabet(fixture, "Letters");

        Assert.That(alphabet.RootAlphabetSize, Is.EqualTo(26));
        Assert.That(alphabet.GetRootIndex('A'), Is.EqualTo(0));
        Assert.That(alphabet.GetRootIndex('Z'), Is.EqualTo(25));
        Assert.That(alphabet.GetRootIndex('a'), Is.EqualTo(-1));
        Assert.That(alphabet.Extension?.Contains(new System.Numerics.BigInteger('a')), Is.True);
        Assert.That(alphabet.Extension?.Contains(new System.Numerics.BigInteger('Z')), Is.False);
        Assert.That(alphabet.KnownScalars?.FiniteCardinality,
            Is.EqualTo(new System.Numerics.BigInteger(52)));
        Assert.That(alphabet.OpenExtension, Is.True);
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    private static PermittedAlphabetResult RequireAlphabet(
        ResolutionFixture fixture,
        string name)
    {
        return fixture.Tables[0].FindSymbol(name, AsnAssignmentKind.Type)
            ?.ResolvedType?.PermittedAlphabet
            ?? throw new InvalidOperationException();
    }

    private static ResolutionFixture Resolve(string source)
    {
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        IncrementalLexSession session = new IncrementalLexSession(
            "alphabets.asn",
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
        Assert.That(values.Success, Is.True);
        return new ResolutionFixture
        {
            Diagnostics = diagnostics,
            Tables = tables
        };
    }

    private class ResolutionFixture
    {
        internal DiagnosticBag Diagnostics = null!;
        internal SymbolTable[] Tables = [];
    }
}
