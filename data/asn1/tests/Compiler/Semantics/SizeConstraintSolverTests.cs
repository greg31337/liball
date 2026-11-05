using asn1;

namespace asn1.Tests;

public class SizeConstraintSolverTests
{
    [Test]
    public void SolverComputesBoundsForStringsAndCollections()
    {
        ResolutionFixture fixture = Resolve(
            "Sizes DEFINITIONS ::= BEGIN "
                + "Bits ::= BIT STRING (SIZE (1..8)); "
                + "Octets ::= OCTET STRING (SIZE (0 | 4..6)); "
                + "Text ::= UTF8String (SIZE (2..MAX)); "
                + "Items ::= SEQUENCE (SIZE (3..5)) OF INTEGER; "
                + "Values ::= SET (SIZE (1)) OF UTF8String END");
        SizeConstraintResult bits = RequireSizes(fixture, "Bits");
        SizeConstraintResult octets = RequireSizes(fixture, "Octets");
        SizeConstraintResult text = RequireSizes(fixture, "Text");
        SizeConstraintResult items = RequireSizes(fixture, "Items");
        SizeConstraintResult values = RequireSizes(fixture, "Values");

        AssertBounds(bits, "1", "8", "8");
        Assert.That(octets.Root?.Intervals, Has.Length.EqualTo(2));
        AssertBounds(octets, "0", "6", "4");
        Assert.That(text.HasRootLowerBound, Is.True);
        Assert.That(text.RootLowerBound, Is.EqualTo(new System.Numerics.BigInteger(2)));
        Assert.That(text.HasRootUpperBound, Is.False);
        Assert.That(text.HasRootCardinality, Is.False);
        AssertBounds(items, "3", "5", "3");
        AssertBounds(values, "1", "1", "1");
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void SolverIntersectsSequentialAndInheritedSizeConstraints()
    {
        ResolutionFixture fixture = Resolve(
            "Sizes DEFINITIONS ::= BEGIN "
                + "Base ::= OCTET STRING (SIZE (1..20)); "
                + "Derived ::= Base (SIZE (4..10)) (SIZE (6..8 | 12)); "
                + "Disjoint ::= UTF8String (SIZE (1..3 | 10..12)) END");
        SizeConstraintResult derived = RequireSizes(fixture, "Derived");
        SizeConstraintResult disjoint = RequireSizes(fixture, "Disjoint");

        AssertBounds(derived, "6", "8", "3");
        Assert.That(derived.Root?.Intervals, Has.Length.EqualTo(1));
        Assert.That(disjoint.Root?.Intervals, Has.Length.EqualTo(2));
        AssertBounds(disjoint, "1", "12", "6");
        Assert.That(disjoint.Root?.Contains(new System.Numerics.BigInteger(5)), Is.False);
        Assert.That(disjoint.Root?.Contains(new System.Numerics.BigInteger(11)), Is.True);
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void SolverPreservesSizeExtensionPartitionsAndTreatsMinAsZero()
    {
        ResolutionFixture fixture = Resolve(
            "Sizes DEFINITIONS ::= BEGIN "
                + "Text ::= UTF8String (SIZE (1..5, ..., 10)); "
                + "Any ::= OCTET STRING (SIZE (MIN..MAX)) END");
        SizeConstraintResult text = RequireSizes(fixture, "Text");
        SizeConstraintResult any = RequireSizes(fixture, "Any");

        AssertBounds(text, "1", "5", "5");
        Assert.That(text.Extension?.Intervals, Has.Length.EqualTo(1));
        Assert.That(text.Extension?.Contains(new System.Numerics.BigInteger(10)), Is.True);
        Assert.That(text.KnownSizes?.Intervals, Has.Length.EqualTo(2));
        Assert.That(text.OpenExtension, Is.True);
        Assert.That(any.HasRootLowerBound, Is.True);
        Assert.That(any.RootLowerBound, Is.EqualTo(System.Numerics.BigInteger.Zero));
        Assert.That(any.HasRootUpperBound, Is.False);
        Assert.That(any.HasRootCardinality, Is.False);
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void SolverRejectsNegativeAndImpossibleSizes()
    {
        ResolutionFixture fixture = Resolve(
            "Sizes DEFINITIONS ::= BEGIN "
                + "Negative ::= OCTET STRING (SIZE (-1..5)); "
                + "NegativeItem ::= SEQUENCE (SIZE (-3)) OF INTEGER; "
                + "Impossible ::= UTF8String (SIZE (1..2)) (SIZE (3..4)) END");
        string[] codes = fixture.Diagnostics.GetDiagnostics()
            .Select(diagnostic => diagnostic.Code)
            .ToArray();

        Assert.That(fixture.Values.Success, Is.False);
        Assert.That(codes.Count(code => code == "ASNC0001"), Is.EqualTo(2));
        Assert.That(codes, Does.Contain("ASNC0002"));
        Assert.That(RequireSizes(fixture, "Impossible").Root?.IsEmpty, Is.True);
        Assert.That(
            fixture.Diagnostics.GetDiagnostics().Where(
                diagnostic => diagnostic.Code == "ASNC0001"
                    || diagnostic.Code == "ASNC0002").All(
                        diagnostic => diagnostic.Span != null),
            Is.True);
    }

    private static void AssertBounds(
        SizeConstraintResult result,
        string lower,
        string upper,
        string cardinality)
    {
        Assert.That(result.HasRootLowerBound, Is.True);
        Assert.That(result.RootLowerBound.ToString(), Is.EqualTo(lower));
        Assert.That(result.HasRootUpperBound, Is.True);
        Assert.That(result.RootUpperBound.ToString(), Is.EqualTo(upper));
        Assert.That(result.HasRootCardinality, Is.True);
        Assert.That(result.RootCardinality.ToString(), Is.EqualTo(cardinality));
    }

    private static SizeConstraintResult RequireSizes(
        ResolutionFixture fixture,
        string name)
    {
        return fixture.Tables[0].FindSymbol(name, AsnAssignmentKind.Type)
            ?.ResolvedType?.SizeConstraints
            ?? throw new InvalidOperationException();
    }

    private static ResolutionFixture Resolve(string source)
    {
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        IncrementalLexSession session = new IncrementalLexSession(
            "sizes.asn",
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
