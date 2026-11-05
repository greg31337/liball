using asn1;

namespace asn1.Tests;

public class ModuleImportIntegrationTests
{
    [Test]
    public void CompilerSelectsSameNameRevisionsByOidAndIri()
    {
        AsnCompilationResult result = Compile(
            Source(
                "catalog-v1.asn",
                "Catalog { iso(1) 2 } \"/catalog/v1\" DEFINITIONS ::= BEGIN "
                    + "EXPORTS V1; V1 ::= INTEGER END"),
            Source(
                "catalog-v2.asn",
                "Catalog { iso(1) 3 } \"/catalog/v2\" DEFINITIONS ::= BEGIN "
                    + "EXPORTS V2; V2 ::= BOOLEAN END"),
            Source(
                "consumers.asn",
                "OidConsumer DEFINITIONS ::= BEGIN "
                    + "IMPORTS V1 FROM Catalog { iso(1) 2 }; SelectedOid ::= V1 END "
                    + "IriConsumer DEFINITIONS ::= BEGIN "
                    + "IMPORTS V2 FROM Catalog \"/catalog/v2\"; SelectedIri ::= V2 END"));

        Assert.That(result.Success, Is.True, Format(result));
        Assert.That(result.Modules.Count(item => item.Name == "Catalog"), Is.EqualTo(2));
        Assert.That(Assignment(result, "SelectedOid").Type?.Kind,
            Is.EqualTo(AsnTypeKind.Integer));
        Assert.That(Assignment(result, "SelectedIri").Type?.Kind,
            Is.EqualTo(AsnTypeKind.Boolean));
    }

    [Test]
    public void CompilerEnforcesExportsAndReportsMissingAndAmbiguousImports()
    {
        AsnCompilationResult result = Compile(
            Source(
                "shared-one.asn",
                "Shared { 1 1 } DEFINITIONS ::= BEGIN EXPORTS Item; "
                    + "Item ::= INTEGER END"),
            Source(
                "shared-two.asn",
                "Shared { 1 1 } DEFINITIONS ::= BEGIN EXPORTS Item; "
                    + "Item ::= BOOLEAN END"),
            Source(
                "provider.asn",
                "Provider DEFINITIONS ::= BEGIN EXPORTS Visible; "
                    + "Visible ::= INTEGER Hidden ::= BOOLEAN END"),
            Source(
                "consumer.asn",
                "Consumer DEFINITIONS ::= BEGIN IMPORTS "
                    + "Item FROM Shared { 1 1 } "
                    + "Absent FROM Missing "
                    + "Hidden, Unknown FROM Provider; Local ::= INTEGER END"));

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.Select(item => item.Code),
            Does.Contain("ASNB0004"));
        Assert.That(result.Diagnostics.Select(item => item.Code),
            Does.Contain("ASNB0003"));
        Assert.That(result.Diagnostics.Select(item => item.Code),
            Does.Contain("ASNB0006"));
        Assert.That(result.Diagnostics.Select(item => item.Code),
            Does.Contain("ASNB0005"));
        Assert.That(result.Diagnostics.Where(item => item.Code == "ASNB0004")
            .Single().RelatedLocations, Has.Length.EqualTo(2));
    }

    [Test]
    public void CompilerAllowsCyclicImportsAndCrossModuleRecursiveTypes()
    {
        AsnCompilationResult result = Compile(
            Source(
                "a.asn",
                "A DEFINITIONS ::= BEGIN EXPORTS AType; "
                    + "IMPORTS BType FROM B; "
                    + "AType ::= SEQUENCE { b BType OPTIONAL } END"),
            Source(
                "b.asn",
                "B DEFINITIONS ::= BEGIN EXPORTS BType; "
                    + "IMPORTS AType FROM A; "
                    + "BType ::= SEQUENCE { a AType OPTIONAL } END"));

        Assert.That(result.Success, Is.True, Format(result));
        Assert.That(Assignment(result, "AType").Type?.Components.Single().Name,
            Is.EqualTo("b"));
        Assert.That(Assignment(result, "BType").Type?.Components.Single().Name,
            Is.EqualTo("a"));
    }

    [Test]
    public void CompilerOrdersModulesAndImportsDeterministically()
    {
        AsnSourceUnit provider = Source(
            "z-provider.asn",
            "Provider DEFINITIONS ::= BEGIN EXPORTS Base; Base ::= INTEGER END");
        AsnSourceUnit middle = Source(
            "m-middle.asn",
            "Middle DEFINITIONS ::= BEGIN EXPORTS Wrapped; "
                + "IMPORTS Base FROM Provider; Wrapped ::= SEQUENCE { value Base } END");
        AsnSourceUnit consumer = Source(
            "a-consumer.asn",
            "Consumer DEFINITIONS ::= BEGIN IMPORTS Wrapped FROM Middle; "
                + "Final ::= Wrapped END");

        AsnCompilationResult forward = Compile(provider, middle, consumer);
        AsnCompilationResult reverse = Compile(consumer, middle, provider);

        Assert.That(forward.Success, Is.True, Format(forward));
        Assert.That(reverse.Success, Is.True, Format(reverse));
        Assert.That(Snapshot(forward), Is.EqualTo(Snapshot(reverse)));
        Assert.That(forward.Modules.Select(item => item.Name),
            Is.EqualTo(new[] { "Provider", "Middle", "Consumer" }));
    }

    private static AsnCompilationResult Compile(params AsnSourceUnit[] sources)
    {
        return new CompilerImplementation().Compile(
            new AsnCompilationInput { SourceUnits = sources });
    }

    private static AsnSourceUnit Source(string name, string text)
    {
        return new AsnSourceUnit { Name = name, Text = text };
    }

    private static AsnAssignment Assignment(AsnCompilationResult result, string name)
    {
        return result.Assignments.Single(item => item.Name == name);
    }

    private static string[] Snapshot(AsnCompilationResult result)
    {
        return result.Modules.Select(module =>
            module.Name + ":" + string.Join(",", module.Imports.Select(imported =>
                imported.ModuleName + "=" + string.Join(",", imported.Symbols)))
                + ":" + string.Join(",", module.Assignments.Select(item => item.Name)))
            .ToArray();
    }

    private static string Format(AsnCompilationResult result)
    {
        return string.Join(" | ", result.Diagnostics.Select(item =>
            item.Code + ":" + item.Message));
    }
}
