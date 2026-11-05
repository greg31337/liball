using asn1;

namespace asn1.Tests;

public class CompilationGraphTests
{
    [Test]
    public void GraphGroupsCyclicModulesAndRecursiveTypesDeterministically()
    {
        ModuleSyntax first = ParseModule(
            "a.asn",
            "A DEFINITIONS ::= BEGIN "
                + "IMPORTS BType FROM B; AType ::= BType END");
        ModuleSyntax second = ParseModule(
            "b.asn",
            "B DEFINITIONS ::= BEGIN "
                + "IMPORTS AType FROM A; BType ::= SEQUENCE { next AType OPTIONAL } END");
        ModuleSyntax third = ParseModule(
            "c.asn",
            "C DEFINITIONS ::= BEGIN "
                + "IMPORTS AType FROM A; CType ::= AType END");
        GraphFixture fixture = BuildGraph([first, second, third]);

        Assert.That(fixture.Graph.ModuleEdges, Has.Length.EqualTo(3));
        Assert.That(fixture.Graph.ModuleGroups, Has.Length.EqualTo(2));
        Assert.That(
            fixture.Graph.ModuleGroups[0].Nodes.Select(node => node.Module?.ModuleName),
            Is.EqualTo(new[] { "A", "B" }));
        Assert.That(fixture.Graph.ModuleGroups[0].Recursive, Is.True);
        Assert.That(
            fixture.Graph.ModuleGroups[1].Nodes.Select(node => node.Module?.ModuleName),
            Is.EqualTo(new[] { "C" }));

        AssignmentDependencyGroup recursive = fixture.Graph.AssignmentGroups[0];
        Assert.That(
            recursive.Nodes.Select(node => node.Symbol?.QualifiedName),
            Is.EqualTo(new[] { "A.AType", "B.BType" }));
        Assert.That(recursive.Recursive, Is.True);
        Assert.That(recursive.LegalTypeRecursion, Is.True);
        Assert.That(
            fixture.Graph.AssignmentGroups[1].Nodes.Single().Symbol?.QualifiedName,
            Is.EqualTo("C.CType"));
        Assert.That(fixture.Graph.Success, Is.True);
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void GraphAllowsDirectRecursiveTypeDefinitions()
    {
        ModuleSyntax module = ParseModule(
            "node.asn",
            "Nodes DEFINITIONS ::= BEGIN "
                + "Node ::= SEQUENCE { next Node OPTIONAL } END");
        GraphFixture fixture = BuildGraph([module]);

        AssignmentDependencyGroup group = fixture.Graph.AssignmentGroups.Single();
        Assert.That(group.Nodes.Single().Symbol?.QualifiedName, Is.EqualTo("Nodes.Node"));
        Assert.That(group.Recursive, Is.True);
        Assert.That(group.LegalTypeRecursion, Is.True);
        Assert.That(fixture.Graph.Success, Is.True);
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void GraphAllowsRecursiveObjectClassDefinitions()
    {
        ModuleSyntax module = ParseModule(
            "classes.asn",
            "Classes DEFINITIONS ::= BEGIN "
                + "OPERATION ::= CLASS { &Linked OPERATION OPTIONAL } END");
        GraphFixture fixture = BuildGraph([module]);

        AssignmentDependencyGroup group = fixture.Graph.AssignmentGroups.Single();
        string details = string.Join(",", group.Nodes.Single().Symbol?.CandidateKinds ?? []);
        Assert.That(group.Nodes.Single().Symbol?.QualifiedName,
            Is.EqualTo("Classes.OPERATION"), details);
        Assert.That(group.Recursive, Is.True, details);
        Assert.That(group.LegalTypeRecursion, Is.True, details);
        Assert.That(fixture.Graph.Success, Is.True, details);
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void GraphRejectsValueCyclesWithRelatedDeclarationLocations()
    {
        ModuleSyntax module = ParseModule(
            "values.asn",
            "Values DEFINITIONS ::= BEGIN "
                + "a INTEGER ::= b; b INTEGER ::= a END");
        GraphFixture fixture = BuildGraph([module]);
        AssignmentDependencyGroup group = fixture.Graph.AssignmentGroups.Single();
        AsnDiagnostic diagnostic = fixture.Diagnostics.GetDiagnostics().Single();

        Assert.That(group.Nodes.Select(node => node.Symbol?.Name),
            Is.EqualTo(new[] { "a", "b" }));
        Assert.That(group.Recursive, Is.True);
        Assert.That(group.LegalTypeRecursion, Is.False);
        Assert.That(fixture.Graph.Success, Is.False);
        Assert.That(diagnostic.Code, Is.EqualTo("ASNB0009"));
        Assert.That(diagnostic.RelatedLocations, Has.Length.EqualTo(1));
        Assert.That(diagnostic.Span?.Start.SourceName, Is.EqualTo("values.asn"));
    }

    [Test]
    public void GraphRejectsObjectCycles()
    {
        ModuleSyntax module = ParseModule(
            "objects.asn",
            "Objects DEFINITIONS ::= BEGIN "
                + "CLASSA ::= CLASS { &id INTEGER }; "
                + "a CLASSA ::= b; b CLASSA ::= a END");
        GraphFixture fixture = BuildGraph([module]);
        AssignmentDependencyGroup cycle = fixture.Graph.AssignmentGroups
            .Single(group => group.Recursive);

        Assert.That(cycle.Nodes.Select(node => node.Symbol?.Name),
            Is.EqualTo(new[] { "a", "b" }));
        Assert.That(cycle.LegalTypeRecursion, Is.False);
        Assert.That(fixture.Graph.Success, Is.False);
        Assert.That(
            fixture.Diagnostics.GetDiagnostics().Select(item => item.Code),
            Is.EqualTo(new[] { "ASNB0009" }));
    }

    [Test]
    public void GraphRejectsParameterizedTypeCyclesAndIgnoresDummyParameters()
    {
        ModuleSyntax module = ParseModule(
            "parameters.asn",
            "Parameters DEFINITIONS ::= BEGIN "
                + "A { T } ::= B { T }; B { T } ::= A { T } END");
        GraphFixture fixture = BuildGraph([module]);
        AssignmentDependencyGroup cycle = fixture.Graph.AssignmentGroups
            .Single(group => group.Recursive);

        Assert.That(cycle.Nodes.Select(node => node.Symbol?.Name),
            Is.EqualTo(new[] { "A", "B" }));
        Assert.That(cycle.Nodes.All(node => node.Symbol?.Parameterized == true), Is.True);
        Assert.That(
            cycle.Nodes.SelectMany(node => node.Symbol?.ParameterNames ?? [])
                .Distinct(),
            Is.EqualTo(new[] { "T" }));
        Assert.That(
            fixture.Graph.AssignmentEdges.Any(edge => edge.Target?.Symbol?.Name == "T"),
            Is.False);
        Assert.That(cycle.LegalTypeRecursion, Is.False);
        Assert.That(fixture.Graph.Success, Is.False);
        Assert.That(fixture.Diagnostics.GetDiagnostics().Single().Code, Is.EqualTo("ASNB0009"));
    }

    [Test]
    public void GraphRejectsParameterizedValueAndObjectCycles()
    {
        ModuleSyntax module = ParseModule(
            "parameter-values.asn",
            "ParameterValues DEFINITIONS ::= BEGIN "
                + "ITEM-CLASS ::= CLASS { &id INTEGER }; "
                + "valueLoop { INTEGER : value } INTEGER ::= "
                + "valueLoop { value }; "
                + "objectLoop { ITEM-CLASS : item } ITEM-CLASS ::= "
                + "objectLoop { item } END");
        GraphFixture fixture = BuildGraph([module]);
        AssignmentDependencyGroup[] cycles = fixture.Graph.AssignmentGroups
            .Where(group => group.Recursive)
            .ToArray();

        Assert.That(cycles, Has.Length.EqualTo(2));
        Assert.That(cycles.All(group => !group.LegalTypeRecursion), Is.True);
        Assert.That(fixture.Graph.Success, Is.False);
        Assert.That(fixture.Diagnostics.GetDiagnostics()
            .Count(item => item.Code == "ASNB0009"), Is.EqualTo(2));
    }

    [Test]
    public void GraphDefersGuardedParameterizedTypeCycles()
    {
        ModuleSyntax module = ParseModule(
            "guarded-parameters.asn",
            "GuardedParameters DEFINITIONS ::= BEGIN "
                + "List { T } ::= SEQUENCE { "
                + "elem T, next List { T } OPTIONAL } END");
        GraphFixture fixture = BuildGraph([module]);
        AssignmentDependencyGroup group = fixture.Graph.AssignmentGroups.Single();
        string details = string.Join(" | ", group.Nodes.Select(node =>
            node.Symbol?.Name + ":" + node.Symbol?.Parameterized + ":"
                + string.Join(",", node.Symbol?.CandidateKinds ?? [])))
            + " | " + string.Join(" | ", fixture.Graph.AssignmentEdges.Select(edge =>
                edge.ReferenceToken?.Text ?? ""));

        Assert.That(group.Recursive, Is.True, details);
        Assert.That(group.LegalTypeRecursion, Is.True, details);
        Assert.That(fixture.Graph.Success, Is.True, details);
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void GraphOrdersAcyclicDependenciesBeforeDependentsWithStableTies()
    {
        ModuleSyntax module = ParseModule(
            "order.asn",
            "Order DEFINITIONS ::= BEGIN "
                + "C ::= B; B ::= A; A ::= INTEGER; D ::= BOOLEAN END");
        GraphFixture first = BuildGraph([module]);
        GraphFixture second = BuildGraph([module]);

        string[] firstOrder = first.Graph.AssignmentGroups
            .Select(group => group.Nodes.Single().Symbol?.Name ?? "")
            .ToArray();
        string[] secondOrder = second.Graph.AssignmentGroups
            .Select(group => group.Nodes.Single().Symbol?.Name ?? "")
            .ToArray();

        Assert.That(firstOrder, Is.EqualTo(new[] { "A", "B", "C", "D" }));
        Assert.That(secondOrder, Is.EqualTo(firstOrder));
        Assert.That(
            first.Graph.AssignmentGroups.Select(group => group.CompilationOrder),
            Is.EqualTo(new[] { 0, 1, 2, 3 }));
        Assert.That(first.Graph.Success, Is.True);
        Assert.That(first.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    private static GraphFixture BuildGraph(ModuleSyntax[] modules)
    {
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        SymbolTable[] tables = new SymbolTableBuilder(diagnostics).BuildTables(modules);
        new ImportResolver(diagnostics).ResolveImports(tables);
        return new GraphFixture
        {
            Diagnostics = diagnostics,
            Graph = new CompilationGraphBuilder(diagnostics).BuildGraph(tables)
        };
    }

    private static ModuleSyntax ParseModule(string sourceName, string source)
    {
        IncrementalLexSession session = new IncrementalLexSession(
            sourceName,
            false,
            new AsnResourceLimits());
        session.SupplyTextChunk(source, true);
        ModuleParseResult result = new ModuleParser(new TokenCursor(session, 4)).ParseModule();
        Assert.That(result.Status, Is.EqualTo(ModuleParseStatus.Parsed));
        Assert.That(session.GetDiagnostics(), Is.Empty);
        return result.Module ?? throw new InvalidOperationException();
    }

    private class GraphFixture
    {
        internal DiagnosticBag Diagnostics = null!;
        internal CompilationGraph Graph = null!;
    }
}
