using asn1;

namespace asn1.Tests;

public class AssignmentClassifierTests
{
    [Test]
    public void ClassifierAllowsTypeReferencesInInformationObjectSettings()
    {
        ModuleSyntax module = ParseModule(
            "object-settings.asn",
            "ObjectSettings DEFINITIONS ::= BEGIN "
                + "ITEM ::= CLASS { &Type } WITH SYNTAX { TYPE &Type }; "
                + "Payload ::= INTEGER; item ITEM ::= { TYPE Payload } END");
        ClassificationFixture fixture = Classify([module]);

        Assert.That(fixture.Result.References.Single(reference =>
            reference.Dependency?.Target?.Symbol?.Name == "Payload").Success, Is.True);
        Assert.That(fixture.Result.Success, Is.True);
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void ClassifierResolvesAllSixAssignmentCategoriesFromGovernorsAndContext()
    {
        ModuleSyntax module = ParseModule(
            "categories.asn",
            "Categories DEFINITIONS ::= BEGIN "
                + "TypeA ::= INTEGER; "
                + "CLASSA ::= CLASS { &id INTEGER }; "
                + "valueA TypeA ::= 1; "
                + "ValuesA TypeA ::= { valueA }; "
                + "objectA CLASSA ::= { &id valueA }; "
                + "ObjectsA CLASSA ::= { objectA } "
                + "END");
        ClassificationFixture fixture = Classify([module]);

        Assert.That(
            fixture.Result.Assignments.Select(symbol => symbol.Kind),
            Is.EqualTo(new[]
            {
                AsnAssignmentKind.Type,
                AsnAssignmentKind.ObjectClass,
                AsnAssignmentKind.Value,
                AsnAssignmentKind.ValueSet,
                AsnAssignmentKind.Object,
                AsnAssignmentKind.ObjectSet
            }));
        Assert.That(fixture.Result.Assignments.All(symbol => symbol.KindClassified), Is.True);
        Assert.That(fixture.Result.References, Has.Length.EqualTo(7));
        Assert.That(fixture.Result.References.All(reference => reference.Success), Is.True);
        Assert.That(fixture.Tables[0].TypeSymbols.Single().Name, Is.EqualTo("TypeA"));
        Assert.That(fixture.Tables[0].ObjectClassSymbols.Single().Name, Is.EqualTo("CLASSA"));
        Assert.That(fixture.Tables[0].ValueSymbols.Single().Name, Is.EqualTo("valueA"));
        Assert.That(fixture.Tables[0].ValueSetSymbols.Single().Name, Is.EqualTo("ValuesA"));
        Assert.That(fixture.Tables[0].ObjectSymbols.Single().Name, Is.EqualTo("objectA"));
        Assert.That(fixture.Tables[0].ObjectSetSymbols.Single().Name, Is.EqualTo("ObjectsA"));
        Assert.That(fixture.Result.Success, Is.True);
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void ClassifierPropagatesKindsThroughLocalAndImportedAliases()
    {
        ModuleSyntax provider = ParseModule(
            "provider.asn",
            "Provider DEFINITIONS ::= BEGIN "
                + "Base ::= INTEGER; BASE-CLASS ::= CLASS { &id INTEGER } END");
        ModuleSyntax consumer = ParseModule(
            "consumer.asn",
            "Consumer DEFINITIONS ::= BEGIN IMPORTS Base, BASE-CLASS FROM Provider; "
                + "Alias ::= Base; ALIAS-CLASS ::= BASE-CLASS END");
        ClassificationFixture fixture = Classify([provider, consumer]);

        Assert.That(
            fixture.Tables[1].FindSymbol("Alias", AsnAssignmentKind.Type)?.Kind,
            Is.EqualTo(AsnAssignmentKind.Type));
        Assert.That(
            fixture.Tables[1].FindSymbol("ALIAS-CLASS", AsnAssignmentKind.ObjectClass)?.Kind,
            Is.EqualTo(AsnAssignmentKind.ObjectClass));
        Assert.That(
            fixture.Tables[1].ImportResolutions[0].Symbols.Select(binding => binding.Kind),
            Is.EqualTo(new[] { AsnAssignmentKind.Type, AsnAssignmentKind.ObjectClass }));
        Assert.That(fixture.Result.References.All(reference => reference.Success), Is.True);
        Assert.That(fixture.Result.Success, Is.True);
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void ClassifierHandlesGovernorFreeParameterizedTypesAndObjectClasses()
    {
        ModuleSyntax module = ParseModule(
            "templates.asn",
            "Templates DEFINITIONS ::= BEGIN "
                + "Template { T } ::= SEQUENCE { value T }; "
                + "TEMPLATE-CLASS { T } ::= CLASS { &Type } END");
        ClassificationFixture fixture = Classify([module]);

        Assert.That(fixture.Result.Assignments.Select(symbol => symbol.Kind),
            Is.EqualTo(new[] { AsnAssignmentKind.Type, AsnAssignmentKind.ObjectClass }));
        Assert.That(fixture.Result.Assignments.All(symbol => symbol.Parameterized), Is.True);
        Assert.That(
            fixture.Graph.AssignmentEdges.Any(edge => edge.Target?.Symbol?.Name == "T"),
            Is.False);
        Assert.That(fixture.Result.Success, Is.True);
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void ClassifierReportsReferenceKindConflictsWithTheDeclarationLocation()
    {
        ModuleSyntax module = ParseModule(
            "conflict.asn",
            "Conflict DEFINITIONS ::= BEGIN "
                + "actualValue INTEGER ::= 1; BadType ::= actualValue END");
        ClassificationFixture fixture = Classify([module]);
        AsnDiagnostic diagnostic = fixture.Diagnostics.GetDiagnostics().Single();
        ClassifiedReference conflict = fixture.Result.References.Single(reference => !reference.Success);

        Assert.That(conflict.ExpectedKinds, Is.EqualTo(new[] { AsnAssignmentKind.Type }));
        Assert.That(conflict.ActualKind, Is.EqualTo(AsnAssignmentKind.Value));
        Assert.That(fixture.Result.Success, Is.False);
        Assert.That(diagnostic.Code, Is.EqualTo("ASNB0010"));
        Assert.That(diagnostic.Span?.Start.SourceName, Is.EqualTo("conflict.asn"));
        Assert.That(diagnostic.RelatedLocations, Has.Length.EqualTo(1));
        Assert.That(
            diagnostic.RelatedLocations[0].Span.Start.Offset,
            Is.EqualTo(module.SymbolTable?.Symbols[0].Span?.Start.Offset));
    }

    [Test]
    public void ClassifierDiagnosesAnAssignmentThatRemainsAmbiguous()
    {
        ModuleSyntax module = ParseModule(
            "ambiguous.asn",
            "Ambiguous DEFINITIONS ::= BEGIN MYSTERY ::= UNKNOWN END");
        ClassificationFixture fixture = Classify([module]);

        Assert.That(fixture.Result.Assignments.Single().KindClassified, Is.False);
        Assert.That(fixture.Result.Assignments.Single().Kind, Is.EqualTo(AsnAssignmentKind.Unknown));
        Assert.That(fixture.Result.Success, Is.False);
        Assert.That(
            fixture.Diagnostics.GetDiagnostics().Select(diagnostic => diagnostic.Code),
            Is.EqualTo(new[] { "ASNB0011" }));
    }

    private static ClassificationFixture Classify(ModuleSyntax[] modules)
    {
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        SymbolTable[] tables = new SymbolTableBuilder(diagnostics).BuildTables(modules);
        new ImportResolver(diagnostics).ResolveImports(tables);
        CompilationGraph graph = new CompilationGraphBuilder(diagnostics).BuildGraph(tables);
        AssignmentClassificationResult result = new AssignmentClassifier(diagnostics)
            .ClassifyAssignments(tables, graph);
        return new ClassificationFixture
        {
            Diagnostics = diagnostics,
            Graph = graph,
            Result = result,
            Tables = tables
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

    private class ClassificationFixture
    {
        internal DiagnosticBag Diagnostics = null!;
        internal CompilationGraph Graph = null!;
        internal AssignmentClassificationResult Result = null!;
        internal SymbolTable[] Tables = [];
    }
}
