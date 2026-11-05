using asn1;

namespace asn1.Tests;

public class ImportResolverTests
{
    [Test]
    public void ResolverBindsExportedSymbolsAndRetainsSourceLinks()
    {
        ModuleSyntax provider = ParseModule(
            "provider.asn",
            "Provider DEFINITIONS ::= BEGIN "
                + "EXPORTS Item; Item ::= INTEGER Hidden ::= BOOLEAN END");
        ModuleSyntax consumer = ParseModule(
            "consumer.asn",
            "Consumer DEFINITIONS ::= BEGIN "
                + "IMPORTS Item FROM Provider; Local ::= Item END");
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        SymbolTable[] tables = BuildTables([provider, consumer]);

        ImportResolution[] resolutions = new ImportResolver(diagnostics).ResolveImports(tables);

        Assert.That(resolutions, Has.Length.EqualTo(1));
        Assert.That(resolutions[0].Success, Is.True);
        Assert.That(resolutions[0].SourceModule, Is.SameAs(tables[1]));
        Assert.That(resolutions[0].TargetModule, Is.SameAs(tables[0]));
        Assert.That(resolutions[0].Import, Is.SameAs(consumer.ImportedModules[0]));
        Assert.That(consumer.ImportedModules[0].Resolution, Is.SameAs(resolutions[0]));
        Assert.That(tables[1].ImportResolutions.Single(), Is.SameAs(resolutions[0]));
        Assert.That(resolutions[0].Symbols.Single().QualifiedName, Is.EqualTo("Provider.Item"));
        Assert.That(
            resolutions[0].Symbols.Single().Import,
            Is.SameAs(consumer.ImportedModules[0].Symbols[0]));
        Assert.That(
            resolutions[0].Symbols.Single().Target,
            Is.SameAs(tables[0].FindSymbol("Item", AsnAssignmentKind.Type)));
        Assert.That(diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void ResolverSelectsSameNamedRevisionsByOidAndIri()
    {
        ModuleSyntax first = ParseModule(
            "catalog-v1.asn",
            "Catalog { iso(1) 2 } \"/catalog/v1\" DEFINITIONS ::= BEGIN V1 ::= INTEGER END");
        ModuleSyntax second = ParseModule(
            "catalog-v2.asn",
            "Catalog { iso(1) 3 } \"/catalog/v2\" DEFINITIONS ::= BEGIN V2 ::= BOOLEAN END");
        ModuleSyntax consumer = ParseModule(
            "catalog-user.asn",
            "User DEFINITIONS ::= BEGIN IMPORTS "
                + "V1 FROM Catalog { iso(1) 2 } "
                + "V2 FROM Catalog \"/catalog/v2\"; "
                + "Local ::= INTEGER END");
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        SymbolTable[] tables = BuildTables([first, second, consumer]);

        ImportResolution[] resolutions = new ImportResolver(diagnostics).ResolveImports(tables);

        Assert.That(tables[0].ObjectIdentifier, Is.EqualTo("1.2"));
        Assert.That(tables[1].ObjectIdentifier, Is.EqualTo("1.3"));
        Assert.That(tables[0].Iri, Is.EqualTo("/catalog/v1"));
        Assert.That(tables[1].Iri, Is.EqualTo("/catalog/v2"));
        Assert.That(resolutions.Select(item => item.TargetModule),
            Is.EqualTo(new[] { tables[0], tables[1] }));
        Assert.That(resolutions.Select(item => item.Symbols.Single().QualifiedName),
            Is.EqualTo(new[] { "Catalog.V1", "Catalog.V2" }));
        Assert.That(resolutions.All(item => item.Success), Is.True);
        Assert.That(diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void ResolverSelectsSameNamedRevisionsByLocalAndQualifiedVersion()
    {
        ModuleSyntax first = ParseModule(
            "version-v1.asn",
            "Versioned DEFINITIONS ::= BEGIN Old ::= INTEGER END");
        first.HasVersion = true;
        first.Version = "v1";
        ModuleSyntax second = ParseModule(
            "version-v2.asn",
            "Versioned DEFINITIONS ::= BEGIN New ::= BOOLEAN END");
        second.HasVersion = true;
        second.VersionModule = "Ids";
        second.Version = "v2";
        ModuleSyntax consumer = ParseModule(
            "version-user.asn",
            "User DEFINITIONS ::= BEGIN IMPORTS "
                + "Old FROM Versioned v1 "
                + "New FROM Versioned Ids.v2; "
                + "Local ::= INTEGER END");
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        SymbolTable[] tables = BuildTables([first, second, consumer]);

        ImportResolution[] resolutions = new ImportResolver(diagnostics).ResolveImports(tables);

        Assert.That(tables[0].Version, Is.EqualTo("v1"));
        Assert.That(tables[1].VersionModule, Is.EqualTo("Ids"));
        Assert.That(resolutions.Select(item => item.TargetModule),
            Is.EqualTo(new[] { tables[0], tables[1] }));
        Assert.That(resolutions.All(item => item.Success), Is.True);
        Assert.That(diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void ResolverDiagnosesMissingAndAmbiguousModuleRevisions()
    {
        ModuleSyntax first = ParseModule(
            "shared-v1.asn",
            "Shared { 1 1 } DEFINITIONS ::= BEGIN Item ::= INTEGER END");
        ModuleSyntax second = ParseModule(
            "shared-v2.asn",
            "Shared { 1 2 } DEFINITIONS ::= BEGIN Item ::= INTEGER END");
        ModuleSyntax consumer = ParseModule(
            "shared-user.asn",
            "User DEFINITIONS ::= BEGIN IMPORTS "
                + "Item FROM Shared "
                + "Other FROM Missing "
                + "Wrong FROM Shared { 9 9 }; "
                + "Local ::= INTEGER END");
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        SymbolTable[] tables = BuildTables([first, second, consumer]);

        ImportResolution[] resolutions = new ImportResolver(diagnostics).ResolveImports(tables);
        AsnDiagnostic[] reported = diagnostics.GetDiagnostics();

        Assert.That(resolutions[0].Ambiguous, Is.True);
        Assert.That(resolutions[0].TargetModule, Is.Null);
        Assert.That(resolutions[1].TargetModule, Is.Null);
        Assert.That(resolutions[2].TargetModule, Is.Null);
        Assert.That(reported.Select(item => item.Code),
            Is.EqualTo(new[] { "ASNB0004", "ASNB0003", "ASNB0003" }));
        Assert.That(reported[0].RelatedLocations, Has.Length.EqualTo(2));
    }

    [Test]
    public void ResolverEnforcesExportsAndDiagnosesMissingSymbols()
    {
        ModuleSyntax provider = ParseModule(
            "visibility-provider.asn",
            "Provider DEFINITIONS ::= BEGIN "
                + "EXPORTS Visible; Visible ::= INTEGER Hidden ::= BOOLEAN END");
        ModuleSyntax consumer = ParseModule(
            "visibility-user.asn",
            "User DEFINITIONS ::= BEGIN IMPORTS "
                + "Hidden, Missing FROM Provider; Local ::= INTEGER END");
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        SymbolTable[] tables = BuildTables([provider, consumer]);

        ImportResolution resolution = new ImportResolver(diagnostics).ResolveImports(tables).Single();
        AsnDiagnostic[] reported = diagnostics.GetDiagnostics();

        Assert.That(resolution.TargetModule, Is.SameAs(tables[0]));
        Assert.That(resolution.Success, Is.False);
        Assert.That(resolution.Symbols, Is.Empty);
        Assert.That(reported.Select(item => item.Code),
            Is.EqualTo(new[] { "ASNB0006", "ASNB0005" }));
        Assert.That(reported[0].RelatedLocations, Has.Length.EqualTo(1));
    }

    [Test]
    public void ResolverDiagnosesDuplicateExportedAndCrossModuleImports()
    {
        ModuleSyntax duplicate = ParseModule(
            "duplicate.asn",
            "Duplicate DEFINITIONS ::= BEGIN A ::= INTEGER A ::= BOOLEAN END");
        ModuleSyntax one = ParseModule(
            "one.asn",
            "One DEFINITIONS ::= BEGIN Shared ::= INTEGER END");
        ModuleSyntax two = ParseModule(
            "two.asn",
            "Two DEFINITIONS ::= BEGIN Shared ::= INTEGER END");
        ModuleSyntax consumer = ParseModule(
            "ambiguity-user.asn",
            "User DEFINITIONS ::= BEGIN IMPORTS "
                + "A FROM Duplicate "
                + "Shared FROM One "
                + "Shared FROM Two; Local ::= INTEGER END");
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        SymbolTable[] tables = BuildTables([duplicate, one, two, consumer]);

        ImportResolution[] resolutions = new ImportResolver(diagnostics).ResolveImports(tables);
        AsnDiagnostic[] reported = diagnostics.GetDiagnostics();

        Assert.That(resolutions[0].Success, Is.False);
        Assert.That(resolutions[0].Symbols, Is.Empty);
        Assert.That(resolutions[2].Ambiguous, Is.True);
        Assert.That(resolutions[2].Success, Is.False);
        Assert.That(reported.Select(item => item.Code),
            Is.EqualTo(new[] { "ASNB0007", "ASNB0008" }));
        Assert.That(reported.All(item => item.RelatedLocations.Length > 0), Is.True);
    }

    private static SymbolTable[] BuildTables(ModuleSyntax[] modules)
    {
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        return new SymbolTableBuilder(diagnostics).BuildTables(modules);
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
}
