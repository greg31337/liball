using asn1;

namespace asn1.Tests;

public class SymbolTableTests
{
    [Test]
    public void BuilderIndexesEveryAssignmentCategoryInStableDeclarationOrder()
    {
        ParseFixture fixture = Parse(
            "Symbols DEFINITIONS ::= BEGIN "
                + "TypeA ::= INTEGER "
                + "valueA INTEGER ::= 1 "
                + "ValuesA INTEGER ::= { 1 | 2 } "
                + "CLASSA ::= CLASS { &id INTEGER } "
                + "objectA CLASSA ::= { &id 1 } "
                + "ObjectsA CLASSA ::= { objectA } "
                + "END");

        SymbolTable table = new SymbolTableBuilder(fixture.Session.Diagnostics)
            .BuildTable(fixture.Module);

        Assert.That(
            table.Symbols.Select(symbol => symbol.Name),
            Is.EqualTo(new[] { "TypeA", "valueA", "ValuesA", "CLASSA", "objectA", "ObjectsA" }));
        Assert.That(
            table.Symbols.Select(symbol => symbol.DeclarationOrder),
            Is.EqualTo(new[] { 0, 1, 2, 3, 4, 5 }));
        Assert.That(table.Symbols[0].QualifiedName, Is.EqualTo("Symbols.TypeA"));
        Assert.That(fixture.Module.SymbolTable, Is.SameAs(table));
        Assert.That(table.Module, Is.SameAs(fixture.Module));
        Assert.That(table.Symbols[0].Declaration, Is.SameAs(fixture.Module.AssignmentHeads[0]));
        Assert.That(
            table.Symbols[0].DeclaredReference,
            Is.SameAs(fixture.Module.AssignmentHeads[0].DeclaredReference));
        Assert.That(table.Symbols[0].Span?.Start.SourceName, Is.EqualTo("symbols.asn"));
        Assert.That(table.FindSymbol("TypeA", AsnAssignmentKind.Type), Is.SameAs(table.Symbols[0]));
        Assert.That(table.FindSymbol("valueA", AsnAssignmentKind.Value), Is.SameAs(table.Symbols[1]));
        Assert.That(table.FindSymbol("ValuesA", AsnAssignmentKind.ValueSet), Is.SameAs(table.Symbols[2]));
        Assert.That(table.FindSymbol("CLASSA", AsnAssignmentKind.ObjectClass), Is.SameAs(table.Symbols[3]));
        Assert.That(table.FindSymbol("objectA", AsnAssignmentKind.Object), Is.SameAs(table.Symbols[4]));
        Assert.That(table.FindSymbol("ObjectsA", AsnAssignmentKind.ObjectSet), Is.SameAs(table.Symbols[5]));
        Assert.That(table.Symbols.All(symbol => symbol.Exported), Is.True);
        Assert.That(fixture.Session.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void ListedAndEmptyExportsControlSymbolVisibility()
    {
        ParseFixture listed = Parse(
            "Listed DEFINITIONS ::= BEGIN "
                + "EXPORTS TypeA, valueA; "
                + "TypeA ::= INTEGER valueA INTEGER ::= 1 Hidden ::= BOOLEAN END");
        ParseFixture empty = Parse(
            "Empty DEFINITIONS ::= BEGIN EXPORTS; TypeA ::= INTEGER END");

        SymbolTable listedTable = new SymbolTableBuilder(listed.Session.Diagnostics)
            .BuildTable(listed.Module);
        SymbolTable emptyTable = new SymbolTableBuilder(empty.Session.Diagnostics)
            .BuildTable(empty.Module);

        Assert.That(listedTable.IsExported("TypeA", AsnAssignmentKind.Type), Is.True);
        Assert.That(listedTable.IsExported("valueA", AsnAssignmentKind.Value), Is.True);
        Assert.That(listedTable.IsExported("Hidden", AsnAssignmentKind.Type), Is.False);
        Assert.That(listedTable.Symbols[0].ExportReference, Is.SameAs(listed.Module.ExportedSymbols[0]));
        Assert.That(emptyTable.Symbols.Single().Exported, Is.False);
        Assert.That(listed.Session.GetDiagnostics(), Is.Empty);
        Assert.That(empty.Session.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void DuplicateDeclarationsAreRetainedAndLinkedByDiagnostics()
    {
        ParseFixture fixture = Parse(
            "Duplicates DEFINITIONS ::= BEGIN "
                + "A ::= INTEGER A ::= BOOLEAN "
                + "v INTEGER ::= 1 v INTEGER ::= 2 "
                + "END");

        SymbolTable table = new SymbolTableBuilder(fixture.Session.Diagnostics)
            .BuildTable(fixture.Module);
        AsnDiagnostic[] diagnostics = fixture.Session.GetDiagnostics();

        Assert.That(table.Symbols, Has.Length.EqualTo(4));
        Assert.That(table.FindSymbols("A", AsnAssignmentKind.Type), Has.Length.EqualTo(2));
        Assert.That(table.FindSymbols("v", AsnAssignmentKind.Value), Has.Length.EqualTo(2));
        Assert.That(
            diagnostics.Select(diagnostic => diagnostic.Code),
            Is.EqualTo(new[] { "ASNB0001", "ASNB0001" }));
        Assert.That(diagnostics.All(diagnostic => diagnostic.RelatedLocations.Length == 1), Is.True);
        AsnSourceSpan firstDuplicateSpan = diagnostics[0].Span ?? throw new InvalidOperationException();
        AsnSourceSpan secondDuplicateSpan = diagnostics[1].Span ?? throw new InvalidOperationException();
        Assert.That(
            diagnostics[0].RelatedLocations[0].Span.Start.Offset,
            Is.LessThan(firstDuplicateSpan.Start.Offset));
        Assert.That(
            diagnostics[1].RelatedLocations[0].Span.Start.Offset,
            Is.LessThan(secondDuplicateSpan.Start.Offset));
    }

    [Test]
    public void DisjointCategoryNamespacesAllowTheSameReferenceText()
    {
        ModuleSyntax module = new ModuleSyntax
        {
            Name = "Separate",
            AssignmentHeads =
            [
                CreateHead("Shared", AsnAssignmentKind.Type),
                CreateHead("Shared", AsnAssignmentKind.ObjectClass)
            ]
        };
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());

        SymbolTable table = new SymbolTableBuilder(diagnostics).BuildTable(module);

        Assert.That(table.Symbols, Has.Length.EqualTo(2));
        Assert.That(table.TypeSymbols.Single(), Is.SameAs(table.Symbols[0]));
        Assert.That(table.ObjectClassSymbols.Single(), Is.SameAs(table.Symbols[1]));
        Assert.That(table.FindSymbol("Shared", AsnAssignmentKind.Value), Is.Null);
        Assert.That(diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void BuilderDiagnosesAnExportWithoutALocalDeclaration()
    {
        ParseFixture fixture = Parse(
            "Missing DEFINITIONS ::= BEGIN EXPORTS Unknown; Known ::= INTEGER END");

        SymbolTable table = new SymbolTableBuilder(fixture.Session.Diagnostics)
            .BuildTable(fixture.Module);

        Assert.That(table.Symbols.Single().Exported, Is.False);
        Assert.That(
            fixture.Session.GetDiagnostics().Select(diagnostic => diagnostic.Code),
            Is.EqualTo(new[] { "ASNB0002" }));
        Assert.That(fixture.Session.GetDiagnostics()[0].Span?.Start.Offset,
            Is.EqualTo(fixture.Module.ExportedSymbols[0].Span?.Start.Offset));
    }

    [Test]
    public void BuilderCreatesIsolatedTablesInModuleInputOrder()
    {
        ParseFixture first = Parse("First DEFINITIONS ::= BEGIN A ::= INTEGER END");
        ParseFixture second = Parse("Second DEFINITIONS ::= BEGIN B ::= BOOLEAN END");
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());

        SymbolTable[] tables = new SymbolTableBuilder(diagnostics).BuildTables(
            [first.Module, second.Module]);

        Assert.That(tables.Select(table => table.ModuleName), Is.EqualTo(new[] { "First", "Second" }));
        Assert.That(tables[0].FindSymbol("A", AsnAssignmentKind.Type)?.QualifiedName,
            Is.EqualTo("First.A"));
        Assert.That(tables[0].FindSymbol("B", AsnAssignmentKind.Type), Is.Null);
        Assert.That(tables[1].FindSymbol("B", AsnAssignmentKind.Type)?.QualifiedName,
            Is.EqualTo("Second.B"));
    }

    private static AssignmentHeadSyntax CreateHead(string name, AsnAssignmentKind kind)
    {
        AssignmentHeadAlternativeSyntax reference = new AssignmentHeadAlternativeSyntax
        {
            Name = name,
            CandidateKinds = [kind]
        };
        return new AssignmentHeadSyntax
        {
            Alternatives = [reference],
            DeclaredReference = reference
        };
    }

    private static ParseFixture Parse(string source)
    {
        IncrementalLexSession session = new IncrementalLexSession(
            "symbols.asn",
            false,
            new AsnResourceLimits());
        session.SupplyTextChunk(source, true);
        ModuleParseResult result = new ModuleParser(new TokenCursor(session, 4)).ParseModule();
        Assert.That(result.Status, Is.EqualTo(ModuleParseStatus.Parsed));
        Assert.That(result.Module, Is.Not.Null);
        return new ParseFixture
        {
            Session = session,
            Module = result.Module ?? throw new InvalidOperationException()
        };
    }

    private class ParseFixture
    {
        internal IncrementalLexSession Session = null!;
        internal ModuleSyntax Module = null!;
    }
}
