using asn1;

namespace asn1.Tests;

public class ImportExportParserTests
{
    [TestCase("", false, true, 0)]
    [TestCase("EXPORTS ALL;", true, true, 0)]
    [TestCase("EXPORTS;", true, false, 0)]
    [TestCase("EXPORTS TypeA, valueA;", true, false, 2)]
    public void ParserDistinguishesAbsentAllEmptyAndListedExports(
        string exports,
        bool specified,
        bool exportAll,
        int symbolCount)
    {
        ParseFixture fixture = Parse(
            "Exports DEFINITIONS ::= BEGIN " + exports + " END");

        ModuleSyntax module = RequireModule(fixture.Result);
        Assert.That(fixture.Result.Status, Is.EqualTo(ModuleParseStatus.Parsed));
        Assert.That(module.ExportsSpecified, Is.EqualTo(specified));
        Assert.That(module.ExportsAll, Is.EqualTo(exportAll));
        Assert.That(module.ExportedSymbols, Has.Length.EqualTo(symbolCount));
        Assert.That(fixture.Session.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void ExportSymbolsRetainParameterizedFormAndEveryCategoryCandidate()
    {
        ParseFixture fixture = Parse(
            "Kinds DEFINITIONS ::= BEGIN "
            + "EXPORTS TypeA, valueA, CLASSA, SetA{}; END");

        ModuleSyntax module = RequireModule(fixture.Result);
        Assert.That(module.ExportedSymbols.Select(symbol => symbol.Name), Is.EqualTo(new[]
        {
            "TypeA", "valueA", "CLASSA", "SetA"
        }));
        Assert.That(module.ExportedSymbols[3].Parameterized, Is.True);
        Assert.That(module.ExportedSymbols[3].Span?.End.Offset, Is.EqualTo(
            fixture.Source.IndexOf("SetA{}", StringComparison.Ordinal) + 6));
        AsnAssignmentKind[] candidates = module.ExportedSymbols
            .SelectMany(symbol => symbol.CandidateKinds)
            .Distinct()
            .OrderBy(kind => kind)
            .ToArray();
        Assert.That(candidates, Is.EqualTo(new[]
        {
            AsnAssignmentKind.Type,
            AsnAssignmentKind.Value,
            AsnAssignmentKind.ValueSet,
            AsnAssignmentKind.ObjectClass,
            AsnAssignmentKind.Object,
            AsnAssignmentKind.ObjectSet
        }));
        Assert.That(fixture.Session.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void ParserGroupsImportsAndRetainsOidAndVersionIdentifiers()
    {
        string source = "Main DEFINITIONS ::= BEGIN\n"
            + "IMPORTS\n"
            + " TypeA, valueA, CLASSA{}, thing FROM Other { iso(1) 2 Arcs.relative member(arcNumber) }\n"
            + " nextValue FROM Versioned versionOne\n"
            + " SetB{} FROM Qualified Ids.versionTwo;\n"
            + "Local ::= INTEGER\n"
            + "END";
        ParseFixture fixture = Parse(source);

        ModuleSyntax module = RequireModule(fixture.Result);
        Assert.That(fixture.Result.Status, Is.EqualTo(ModuleParseStatus.Parsed));
        Assert.That(module.ImportsSpecified, Is.True);
        Assert.That(module.ImportedModules, Has.Length.EqualTo(3));
        Assert.That(module.ImportedModules[0].ModuleName, Is.EqualTo("Other"));
        Assert.That(
            module.ImportedModules[0].Symbols.Select(symbol => symbol.Name),
            Is.EqualTo(new[] { "TypeA", "valueA", "CLASSA", "thing" }));
        Assert.That(module.ImportedModules[0].Symbols[2].Parameterized, Is.True);
        AssignedModuleIdentifierSyntax oid = RequireIdentifier(module.ImportedModules[0]);
        Assert.That(oid.HasObjectIdentifier, Is.True);
        Assert.That(oid.ObjectIdentifierComponents, Has.Length.EqualTo(4));
        Assert.That(oid.ObjectIdentifierComponents[0].Name, Is.EqualTo("iso"));
        Assert.That(oid.ObjectIdentifierComponents[1].Number.ToString(), Is.EqualTo("2"));
        Assert.That(oid.ObjectIdentifierComponents[2].DefinedValueModule, Is.EqualTo("Arcs"));
        Assert.That(oid.ObjectIdentifierComponents[2].DefinedValue, Is.EqualTo("relative"));
        Assert.That(oid.ObjectIdentifierComponents[3].Name, Is.EqualTo("member"));
        Assert.That(oid.ObjectIdentifierComponents[3].NumberDefinedValue, Is.EqualTo("arcNumber"));

        Assert.That(module.ImportedModules[1].ModuleName, Is.EqualTo("Versioned"));
        AssignedModuleIdentifierSyntax localVersion = RequireIdentifier(module.ImportedModules[1]);
        Assert.That(localVersion.HasDefinedValue, Is.True);
        Assert.That(localVersion.DefinedValueModule, Is.Empty);
        Assert.That(localVersion.DefinedValue, Is.EqualTo("versionOne"));

        Assert.That(module.ImportedModules[2].ModuleName, Is.EqualTo("Qualified"));
        Assert.That(module.ImportedModules[2].Symbols.Single().Parameterized, Is.True);
        AssignedModuleIdentifierSyntax externalVersion = RequireIdentifier(module.ImportedModules[2]);
        Assert.That(externalVersion.HasDefinedValue, Is.True);
        Assert.That(externalVersion.DefinedValueModule, Is.EqualTo("Ids"));
        Assert.That(externalVersion.DefinedValue, Is.EqualTo("versionTwo"));
        Assert.That(module.BodyTokens.Select(token => token.Text), Is.EqualTo(new[]
        {
            "Local", "::=", "INTEGER"
        }));
        Assert.That(fixture.Session.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void ParserAcceptsAnExplicitlyEmptyImportsClause()
    {
        ParseFixture fixture = Parse(
            "EmptyImports DEFINITIONS ::= BEGIN IMPORTS; Item ::= INTEGER END");

        ModuleSyntax module = RequireModule(fixture.Result);
        Assert.That(fixture.Result.Status, Is.EqualTo(ModuleParseStatus.Parsed));
        Assert.That(module.ImportsSpecified, Is.True);
        Assert.That(module.ImportedModules, Is.Empty);
        Assert.That(module.BodyTokens.Select(token => token.Text), Is.EqualTo(new[]
        {
            "Item", "::=", "INTEGER"
        }));
        Assert.That(fixture.Session.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void ParserReadsMultipleModulesFromOneSourceInOrder()
    {
        string source = "One DEFINITIONS ::= BEGIN EXPORTS; END\n"
            + "Two DEFINITIONS ::= BEGIN EXPORTS ALL; IMPORTS; END";
        IncrementalLexSession session = CreateSession(source);
        TokenCursor cursor = new TokenCursor(session, 4);

        ModuleCollectionParseResult result = new ModuleParser(cursor).ParseModuleList();

        Assert.That(result.Status, Is.EqualTo(ModuleParseStatus.Parsed));
        Assert.That(result.Modules.Select(module => module.Name), Is.EqualTo(new[] { "One", "Two" }));
        Assert.That(result.Modules[0].ExportsAll, Is.False);
        Assert.That(result.Modules[1].ExportsAll, Is.True);
        Assert.That(result.Modules[1].ImportsSpecified, Is.True);
        Assert.That(cursor.PeekNext(0).Status, Is.EqualTo(TokenCursorStatus.EndOfInput));
        Assert.That(session.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void InvalidImportListReportsTheBoundaryAndLeavesEndForRecovery()
    {
        ParseFixture fixture = Parse(
            "Broken DEFINITIONS ::= BEGIN IMPORTS TypeA Other; END");

        Assert.That(fixture.Result.Status, Is.EqualTo(ModuleParseStatus.Invalid));
        Assert.That(fixture.Cursor.PeekNext(0).Token?.Text, Is.EqualTo("END"));
        Assert.That(fixture.Session.GetDiagnostics().Select(item => item.Code), Does.Contain("ASNP0008"));
    }

    [Test]
    public void ParserRejectsTrailingCommasAndMisorderedClauses()
    {
        ParseFixture trailing = Parse(
            "Trailing DEFINITIONS ::= BEGIN EXPORTS TypeA,; END");
        ParseFixture misordered = Parse(
            "Order DEFINITIONS ::= BEGIN IMPORTS; EXPORTS ALL; END");

        Assert.That(trailing.Result.Status, Is.EqualTo(ModuleParseStatus.Invalid));
        Assert.That(trailing.Session.GetDiagnostics().Select(item => item.Code), Does.Contain("ASNP0007"));
        Assert.That(misordered.Result.Status, Is.EqualTo(ModuleParseStatus.Invalid));
        Assert.That(misordered.Session.GetDiagnostics().Select(item => item.Code), Does.Contain("ASNP0011"));
    }

    private static ParseFixture Parse(string source)
    {
        IncrementalLexSession session = CreateSession(source);
        TokenCursor cursor = new TokenCursor(session, 4);
        return new ParseFixture
        {
            Source = source,
            Session = session,
            Cursor = cursor,
            Result = new ModuleParser(cursor).ParseModule()
        };
    }

    private static IncrementalLexSession CreateSession(string source)
    {
        IncrementalLexSession session = new IncrementalLexSession(
            "imports.asn",
            false,
            new AsnResourceLimits());
        session.SupplyTextChunk(source, true);
        return session;
    }

    private static ModuleSyntax RequireModule(ModuleParseResult result)
    {
        Assert.That(result.Module, Is.Not.Null);
        return result.Module ?? throw new InvalidOperationException();
    }

    private static AssignedModuleIdentifierSyntax RequireIdentifier(ImportedModuleSyntax imported)
    {
        Assert.That(imported.AssignedIdentifier, Is.Not.Null);
        return imported.AssignedIdentifier ?? throw new InvalidOperationException();
    }

    private class ParseFixture
    {
        internal string Source = "";
        internal IncrementalLexSession Session = null!;
        internal TokenCursor Cursor = null!;
        internal ModuleParseResult Result = null!;
    }
}
