using asn1;

namespace asn1.Tests;

public class ConstraintSolverTests
{
    [Test]
    public void SolverUsesExactArbitraryIntegersAndProducesDisjointIntervals()
    {
        ResolutionFixture fixture = Resolve(
            "Ranges DEFINITIONS ::= BEGIN "
                + "Huge ::= INTEGER "
                + "((-1000000000000000000000000000000"
                + "..1000000000000000000000000000000) EXCEPT 0) END");
        IntegerConstraintSet set = RequireKnownSet(fixture, "Huge");

        Assert.That(set.Intervals, Has.Length.EqualTo(2));
        Assert.That(set.Intervals[0].LowerBound.ToString(),
            Is.EqualTo("-1000000000000000000000000000000"));
        Assert.That(set.Intervals[0].UpperBound.ToString(), Is.EqualTo("-1"));
        Assert.That(set.Intervals[1].LowerBound.ToString(), Is.EqualTo("1"));
        Assert.That(set.Intervals[1].UpperBound.ToString(),
            Is.EqualTo("1000000000000000000000000000000"));
        Assert.That(set.Contains(System.Numerics.BigInteger.Zero), Is.False);
        Assert.That(set.Contains(System.Numerics.BigInteger.One), Is.True);
        Assert.That(set.HasFiniteCardinality, Is.True);
        Assert.That(set.FiniteCardinality.ToString(),
            Is.EqualTo("2000000000000000000000000000000"));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void SolverCanonicalizesOpenAndUnboundedEndpoints()
    {
        ResolutionFixture fixture = Resolve(
            "Ranges DEFINITIONS ::= BEGIN "
                + "Lower ::= INTEGER (MIN..<-1); "
                + "Upper ::= INTEGER (1<..MAX) END");
        IntegerConstraintSet lower = RequireKnownSet(fixture, "Lower");
        IntegerConstraintSet upper = RequireKnownSet(fixture, "Upper");

        Assert.That(lower.Intervals, Has.Length.EqualTo(1));
        Assert.That(lower.Intervals[0].HasLowerBound, Is.False);
        Assert.That(lower.Intervals[0].UpperBound, Is.EqualTo(new System.Numerics.BigInteger(-2)));
        Assert.That(lower.Contains(new System.Numerics.BigInteger(-1)), Is.False);
        Assert.That(upper.Intervals[0].LowerBound, Is.EqualTo(new System.Numerics.BigInteger(2)));
        Assert.That(upper.Intervals[0].HasUpperBound, Is.False);
        Assert.That(upper.Contains(System.Numerics.BigInteger.One), Is.False);
        Assert.That(lower.HasFiniteCardinality, Is.False);
        Assert.That(upper.HasFiniteCardinality, Is.False);
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void SolverEvaluatesSequentialComplementsAndDetectsEmptySets()
    {
        ResolutionFixture fixture = ResolveAllowingFailure(
            "Ranges DEFINITIONS ::= BEGIN "
                + "Remaining ::= INTEGER (MIN..MAX) (ALL EXCEPT (0 | 10..20)); "
                + "Empty ::= INTEGER (1..3) (4..6) END");
        IntegerConstraintSet remaining = RequireKnownSet(fixture, "Remaining");
        IntegerConstraintSet empty = RequireKnownSet(fixture, "Empty");

        Assert.That(remaining.Intervals, Has.Length.EqualTo(3));
        Assert.That(remaining.Contains(System.Numerics.BigInteger.Zero), Is.False);
        Assert.That(remaining.Contains(new System.Numerics.BigInteger(9)), Is.True);
        Assert.That(remaining.Contains(new System.Numerics.BigInteger(15)), Is.False);
        Assert.That(remaining.Contains(new System.Numerics.BigInteger(21)), Is.True);
        Assert.That(remaining.HasFiniteCardinality, Is.False);
        Assert.That(empty.IsEmpty, Is.True);
        Assert.That(empty.Intervals, Is.Empty);
        Assert.That(empty.HasFiniteCardinality, Is.True);
        Assert.That(empty.FiniteCardinality, Is.EqualTo(System.Numerics.BigInteger.Zero));
        AsnDiagnostic diagnostic = fixture.Diagnostics.GetDiagnostics().Single();
        Assert.That(diagnostic.Code, Is.EqualTo("ASNC0027"));
        Assert.That(diagnostic.Span, Is.Not.Null);
        Assert.That(diagnostic.RelatedLocations, Is.Not.Empty);
        Assert.That(fixture.Values.Success, Is.False);
    }

    [Test]
    public void SolverRestrictsEnumeratedRangesToDeclaredValues()
    {
        ResolutionFixture fixture = Resolve(
            "Ranges DEFINITIONS ::= BEGIN "
                + "Color ::= ENUMERATED { red, green(5), blue } "
                + "((red..green) EXCEPT blue) END");
        IntegerConstraintSet set = RequireKnownSet(fixture, "Color");

        Assert.That(set.Intervals, Has.Length.EqualTo(2));
        Assert.That(set.Contains(System.Numerics.BigInteger.Zero), Is.True);
        Assert.That(set.Contains(System.Numerics.BigInteger.One), Is.False);
        Assert.That(set.Contains(new System.Numerics.BigInteger(2)), Is.False);
        Assert.That(set.Contains(new System.Numerics.BigInteger(5)), Is.True);
        Assert.That(set.HasFiniteCardinality, Is.True);
        Assert.That(set.FiniteCardinality, Is.EqualTo(new System.Numerics.BigInteger(2)));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void SolverUsesContainedSubtypesAndResolvedValueEndpoints()
    {
        ResolutionFixture fixture = Resolve(
            "Ranges DEFINITIONS ::= BEGIN "
                + "lower INTEGER ::= -100000000000000000000; "
                + "upper INTEGER ::= 100000000000000000000; "
                + "Base ::= INTEGER (lower..upper); "
                + "Derived ::= INTEGER ((INCLUDES Base) EXCEPT 5) END");
        IntegerConstraintSet set = RequireKnownSet(fixture, "Derived");

        Assert.That(set.Intervals, Has.Length.EqualTo(2));
        Assert.That(set.Intervals[0].LowerBound.ToString(), Is.EqualTo("-100000000000000000000"));
        Assert.That(set.Intervals[0].UpperBound, Is.EqualTo(new System.Numerics.BigInteger(4)));
        Assert.That(set.Intervals[1].LowerBound, Is.EqualTo(new System.Numerics.BigInteger(6)));
        Assert.That(set.Intervals[1].UpperBound.ToString(), Is.EqualTo("100000000000000000000"));
        Assert.That(set.Contains(new System.Numerics.BigInteger(5)), Is.False);
        Assert.That(set.HasFiniteCardinality, Is.True);
        Assert.That(set.FiniteCardinality.ToString(), Is.EqualTo("200000000000000000000"));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    private static IntegerConstraintSet RequireKnownSet(
        ResolutionFixture fixture,
        string name)
    {
        return fixture.Tables[0].FindSymbol(name, AsnAssignmentKind.Type)
            ?.ResolvedType?.IntegerConstraints?.KnownValues
            ?? throw new InvalidOperationException();
    }

    private static ResolutionFixture Resolve(string source)
    {
        ResolutionFixture fixture = ResolveCore(source);
        Assert.That(fixture.Values.Success, Is.True);
        return fixture;
    }

    private static ResolutionFixture ResolveAllowingFailure(string source)
    {
        return ResolveCore(source);
    }

    private static ResolutionFixture ResolveCore(string source)
    {
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        IncrementalLexSession session = new IncrementalLexSession(
            "ranges.asn",
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
