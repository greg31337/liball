using asn1;

namespace asn1.Tests;

public class ConstraintNormalizerTests
{
    [Test]
    public void NormalizerPreservesSetOperationsAndSeparatesExtensionPartitions()
    {
        ResolutionFixture fixture = Resolve(
            "Constraints DEFINITIONS ::= BEGIN "
                + "Number ::= INTEGER (1 | 2 ^ 3, ..., 4 | 5 ! -7) END");
        ResolvedTypeSyntax number = fixture.Tables[0].TypeSymbols.Single().ResolvedType
            ?? throw new InvalidOperationException();
        NormalizedConstraintGraph graph = number.NormalizedConstraints.Single();
        NormalizedConstraintNode root = graph.RootPartition?.Expression
            ?? throw new InvalidOperationException();
        NormalizedConstraintNode extension = graph.ExtensionPartition?.Expression
            ?? throw new InvalidOperationException();

        Assert.That(root.Kind, Is.EqualTo(AsnConstraintKind.Union));
        Assert.That(root.Children, Has.Length.EqualTo(2));
        Assert.That(root.Children[1].Kind, Is.EqualTo(AsnConstraintKind.Intersection));
        Assert.That(root.Children[1].Children, Has.Length.EqualTo(2));
        Assert.That(extension.Kind, Is.EqualTo(AsnConstraintKind.Union));
        Assert.That(extension.Children, Has.Length.EqualTo(2));
        Assert.That(graph.RootPartition?.Extension, Is.False);
        Assert.That(graph.ExtensionPartition?.Extension, Is.True);
        Assert.That(graph.Exception?.Kind, Is.EqualTo(AsnConstraintKind.Exception));
        Assert.That(graph.Domain, Is.SameAs(number.ConstraintDomain));
        Assert.That(root.Domain, Is.SameAs(graph.Domain));
        Assert.That(extension.Domain, Is.SameAs(graph.Domain));
        Assert.That(graph.SourceSyntax?.RootElement, Is.SameAs(root.SourceElement));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void NormalizerRepresentsAnOpenExtensionWithAnEmptyExtensionExpression()
    {
        ResolutionFixture fixture = Resolve(
            "Constraints DEFINITIONS ::= BEGIN Number ::= INTEGER (1, ...) END");
        NormalizedConstraintGraph graph = fixture.Tables[0].TypeSymbols.Single()
            .ResolvedType?.NormalizedConstraints.Single()
            ?? throw new InvalidOperationException();

        Assert.That(graph.RootPartition?.Expression, Is.Not.Null);
        Assert.That(graph.ExtensionPartition, Is.Not.Null);
        Assert.That(graph.ExtensionPartition?.Extension, Is.True);
        Assert.That(graph.ExtensionPartition?.Expression, Is.Null);
        Assert.That(graph.ExtensionPartition?.Domain, Is.SameAs(graph.Domain));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void NormalizerSharesAliasDomainsButSeparatesUnrelatedTypeDomains()
    {
        ResolutionFixture fixture = Resolve(
            "Constraints DEFINITIONS ::= BEGIN "
                + "Base ::= INTEGER (1); Alias ::= Base (2); Other ::= INTEGER (3) END");
        ResolvedTypeSyntax baseType = FindType(fixture, "Base");
        ResolvedTypeSyntax alias = FindType(fixture, "Alias");
        ResolvedTypeSyntax other = FindType(fixture, "Other");

        Assert.That(alias.ConstraintDomain, Is.SameAs(baseType.ConstraintDomain));
        Assert.That(alias.NormalizedConstraints.Single().Domain, Is.SameAs(baseType.ConstraintDomain));
        Assert.That(other.ConstraintDomain, Is.Not.SameAs(baseType.ConstraintDomain));
        Assert.That(baseType.ConstraintDomain?.EffectiveKind, Is.EqualTo(AsnTypeKind.Integer));
        Assert.That(alias.ConstraintDomain?.CanonicalType, Is.SameAs(baseType));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void NormalizerVisitsNestedTypesAndKeepsSequentialGraphsInSourceOrder()
    {
        ResolutionFixture fixture = Resolve(
            "Constraints DEFINITIONS ::= BEGIN "
                + "Container ::= SEQUENCE { item INTEGER (1) (2), "
                + "items SEQUENCE OF INTEGER (3) } END");
        ResolvedTypeSyntax container = fixture.Tables[0].TypeSymbols.Single().ResolvedType
            ?? throw new InvalidOperationException();
        ResolvedTypeSyntax item = container.Components[0].TypeResolution
            ?? throw new InvalidOperationException();
        ResolvedTypeSyntax items = container.Components[1].TypeResolution
            ?? throw new InvalidOperationException();
        ResolvedTypeSyntax element = items.InnerType
            ?? throw new InvalidOperationException();

        Assert.That(item.NormalizedConstraints, Has.Length.EqualTo(2));
        Assert.That(
            item.NormalizedConstraints.Select(GraphValueText),
            Is.EqualTo(new[] { "1", "2" }));
        Assert.That(element.NormalizedConstraints.Single().RootPartition?.Expression?.SourceElement?
            .Value?.IntegerValue.ToString(), Is.EqualTo("3"));
        Assert.That(item.ConstraintDomain, Is.Not.SameAs(element.ConstraintDomain));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void NormalizerGivesNestedConstraintsTheirOwnPartitions()
    {
        ResolutionFixture fixture = Resolve(
            "Constraints DEFINITIONS ::= BEGIN "
                + "Text ::= UTF8String (SIZE (1, ..., 4 | 5)) END");
        NormalizedConstraintNode size = fixture.Tables[0].TypeSymbols.Single()
            .ResolvedType?.NormalizedConstraints.Single().RootPartition?.Expression
            ?? throw new InvalidOperationException();
        NormalizedConstraintGraph nested = size.NestedConstraints.Single();

        Assert.That(size.Kind, Is.EqualTo(AsnConstraintKind.Size));
        Assert.That(size.Children, Is.Empty);
        Assert.That(nested.RootPartition?.Expression?.Kind,
            Is.EqualTo(AsnConstraintKind.SingleValue));
        Assert.That(nested.ExtensionPartition?.Expression?.Kind,
            Is.EqualTo(AsnConstraintKind.Union));
        Assert.That(nested.ExtensionPartition?.Expression?.Children, Has.Length.EqualTo(2));
        Assert.That(nested.Domain, Is.SameAs(size.Domain));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void NormalizerCopiesValueRangeAndContainedSubtypeOperands()
    {
        ResolutionFixture fixture = Resolve(
            "Constraints DEFINITIONS ::= BEGIN "
                + "Base ::= INTEGER; Values ::= INTEGER "
                + "(INCLUDES Base | MIN..10 | 20<..MAX | 30) END");
        NormalizedConstraintNode root = FindType(fixture, "Values")
            .EffectiveConstraint?.RootPartition?.Expression
            ?? throw new InvalidOperationException();
        NormalizedConstraintNode subtype = root.Children[0];
        NormalizedConstraintNode lowerRange = root.Children[1];
        NormalizedConstraintNode upperRange = root.Children[2];
        NormalizedConstraintNode single = root.Children[3];

        Assert.That(root.Kind, Is.EqualTo(AsnConstraintKind.Union));
        Assert.That(root.Children, Has.Length.EqualTo(4));
        Assert.That(subtype.Kind, Is.EqualTo(AsnConstraintKind.ContainedSubtype));
        Assert.That(subtype.Type?.Name, Is.EqualTo("Base"));
        Assert.That(lowerRange.Kind, Is.EqualTo(AsnConstraintKind.ValueRange));
        Assert.That(lowerRange.LowerEndpointUnbounded, Is.True);
        Assert.That(lowerRange.UpperValue?.IntegerValue.ToString(), Is.EqualTo("10"));
        Assert.That(lowerRange.UpperEndpointIncluded, Is.True);
        Assert.That(upperRange.LowerValue?.IntegerValue.ToString(), Is.EqualTo("20"));
        Assert.That(upperRange.LowerEndpointIncluded, Is.False);
        Assert.That(upperRange.UpperEndpointUnbounded, Is.True);
        Assert.That(single.Kind, Is.EqualTo(AsnConstraintKind.SingleValue));
        Assert.That(single.Value?.IntegerValue.ToString(), Is.EqualTo("30"));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void NormalizerCombinesSequentialAndInheritedConstraintsByIntersection()
    {
        ResolutionFixture fixture = Resolve(
            "Constraints DEFINITIONS ::= BEGIN "
                + "Base ::= INTEGER (1 | 2 | 3); "
                + "Alias ::= Base ((2 | 3) EXCEPT 3) (ALL EXCEPT 0) END");
        ResolvedTypeSyntax baseType = FindType(fixture, "Base");
        ResolvedTypeSyntax alias = FindType(fixture, "Alias");
        NormalizedConstraintGraph effective = alias.EffectiveConstraint
            ?? throw new InvalidOperationException();
        NormalizedConstraintNode root = effective.RootPartition?.Expression
            ?? throw new InvalidOperationException();

        Assert.That(alias.NormalizedConstraints, Has.Length.EqualTo(2));
        Assert.That(effective.Applications, Has.Length.EqualTo(3));
        Assert.That(effective.Applications[0], Is.SameAs(baseType.EffectiveConstraint));
        Assert.That(root.Kind, Is.EqualTo(AsnConstraintKind.Intersection));
        Assert.That(
            root.Children.Select(child => child.Kind),
            Is.EqualTo(new[]
            {
                AsnConstraintKind.Union,
                AsnConstraintKind.Except,
                AsnConstraintKind.AllExcept
            }));
        Assert.That(root.Children[1].Children, Has.Length.EqualTo(2));
        Assert.That(root.Children[2].Children.Single().Value?.IntegerValue.ToString(),
            Is.EqualTo("0"));
        Assert.That(effective.Domain, Is.SameAs(baseType.ConstraintDomain));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void NormalizerCombinesSequentialNestedConstraintsWithoutFlatteningTheirDomains()
    {
        ResolutionFixture fixture = Resolve(
            "Constraints DEFINITIONS ::= BEGIN "
                + "Text ::= UTF8String (SIZE (1 | 2)) (SIZE (2 | 3)) END");
        NormalizedConstraintNode root = FindType(fixture, "Text")
            .EffectiveConstraint?.RootPartition?.Expression
            ?? throw new InvalidOperationException();

        Assert.That(root.Kind, Is.EqualTo(AsnConstraintKind.Intersection));
        Assert.That(root.Children, Has.Length.EqualTo(2));
        Assert.That(root.Children.Select(child => child.Kind),
            Is.EqualTo(new[] { AsnConstraintKind.Size, AsnConstraintKind.Size }));
        Assert.That(root.Children[0].NestedConstraints.Single()
            .RootPartition?.Expression?.Kind, Is.EqualTo(AsnConstraintKind.Union));
        Assert.That(root.Children[1].NestedConstraints.Single()
            .RootPartition?.Expression?.Children, Has.Length.EqualTo(2));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void NormalizerCombinesKnownExtensionAdditionsAndOpenState()
    {
        ResolutionFixture fixture = Resolve(
            "Constraints DEFINITIONS ::= BEGIN "
                + "Value ::= INTEGER (1, ..., 2) (1 | 2, ..., 3) (1 | 2 | 3) END");
        NormalizedConstraintGraph effective = fixture.Tables[0].TypeSymbols.Single()
            .ResolvedType?.EffectiveConstraint
            ?? throw new InvalidOperationException();
        NormalizedConstraintNode extension = effective.ExtensionPartition?.Expression
            ?? throw new InvalidOperationException();

        Assert.That(effective.Applications, Has.Length.EqualTo(3));
        Assert.That(effective.RootPartition?.Expression?.Kind,
            Is.EqualTo(AsnConstraintKind.Intersection));
        Assert.That(effective.ExtensionPartition, Is.Not.Null);
        Assert.That(effective.ExtensionPartition?.Open, Is.False);
        Assert.That(extension.Kind, Is.EqualTo(AsnConstraintKind.Except));
        Assert.That(extension.Children, Has.Length.EqualTo(2));
        Assert.That(extension.Children[0].Kind, Is.EqualTo(AsnConstraintKind.Intersection));
        Assert.That(extension.Children[1], Is.SameAs(effective.RootPartition?.Expression));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    private static string GraphValueText(NormalizedConstraintGraph graph)
    {
        return graph.RootPartition?.Expression?.SourceElement?.Value?.IntegerValue.ToString()
            ?? "";
    }

    private static ResolvedTypeSyntax FindType(ResolutionFixture fixture, string name)
    {
        return fixture.Tables[0].FindSymbol(name, AsnAssignmentKind.Type)?.ResolvedType
            ?? throw new InvalidOperationException();
    }

    private static ResolutionFixture Resolve(string source)
    {
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        IncrementalLexSession session = new IncrementalLexSession(
            "constraints.asn",
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
        TypeResolutionResult result = new TypeResolver(diagnostics).ResolveTypes(tables);
        Assert.That(classification.Success, Is.True);
        Assert.That(result.Success, Is.True);
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
