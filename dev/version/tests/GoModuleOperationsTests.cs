using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class GoModuleOperationsTests
{
    [Test]
    public void VersionOperations_ValidateParseCanonicalizeAndExtract()
    {
        IGoModuleVersioning operations = new GoModuleOperations();

        VersionParseResult parsed = operations.ParseVersion("v2.3");

        Assert.That(operations.IsValidVersion("v2.3.4-pre.1+build.5"), Is.True);
        Assert.That(operations.IsValidVersion("2.3.4"), Is.False);
        Assert.That(parsed.success, Is.True);
        Assert.That(parsed.version, Is.Not.Null);
        Assert.That(parsed.version!.missingComponentCount, Is.EqualTo(1));
        Assert.That(
            operations.CanonicalizeVersion("v2.3.0-pre.1+build.5"),
            Is.EqualTo("v2.3.0-pre.1"));
        Assert.That(operations.CanonicalizeVersion("bad"), Is.Empty);
        Assert.That(operations.GetMajorVersion("v2.3.4"), Is.EqualTo("v2"));
        Assert.That(operations.GetMajorMinorVersion("v2.3.4"), Is.EqualTo("v2.3"));
        Assert.That(
            operations.GetPrerelease("v2.3.4-pre.1+build.5"),
            Is.EqualTo("-pre.1"));
        Assert.That(
            operations.GetBuild("v2.3.4-pre.1+build.5"),
            Is.EqualTo("+build.5"));
        Assert.That(operations.GetBuild("v2.3.4"), Is.Empty);
    }

    [Test]
    public void VersionOrdering_UsesGoPrecedenceAndDoesNotChangeCandidates()
    {
        IGoModuleVersioning operations = new GoModuleOperations();
        string[] candidates = new string[]
        {
            "v1.0.0+build.2",
            "v1.0.0",
            "bad",
            "v1.0.0-pre",
            "v1.0.0+build.1"
        };

        string[] ascending = operations.SortVersionsAscending(candidates);
        string[] descending = operations.SortVersionsDescending(candidates);

        Assert.That(operations.CompareVersions("bad", "v0"), Is.LessThan(0));
        Assert.That(
            operations.CompareVersions("v1.0.0+build.1", "v1.0.0+build.2"),
            Is.Zero);
        Assert.That(ascending, Is.EqualTo(new string[]
        {
            "bad",
            "v1.0.0-pre",
            "v1.0.0",
            "v1.0.0+build.1",
            "v1.0.0+build.2"
        }));
        Assert.That(descending, Is.EqualTo(new string[]
        {
            "v1.0.0+build.2",
            "v1.0.0+build.1",
            "v1.0.0",
            "v1.0.0-pre",
            "bad"
        }));
        Assert.That(candidates, Is.EqualTo(new string[]
        {
            "v1.0.0+build.2",
            "v1.0.0",
            "bad",
            "v1.0.0-pre",
            "v1.0.0+build.1"
        }));
    }

    [Test]
    public void ModulePathOperations_ParsePlainAndEscapedPaths()
    {
        IGoModuleVersioning operations = new GoModuleOperations();

        GoModulePathParseResult plain = operations.ParseModulePath("example.com/project/v2");
        GoModulePathParseResult escaped = operations.ParseEscapedModulePath(
            "github.com/!azure/azure-sdk-for-go");

        Assert.That(plain.success, Is.True);
        Assert.That(plain.modulePath, Is.Not.Null);
        Assert.That(plain.modulePath!.majorVersionSuffix, Is.EqualTo("v2"));
        Assert.That(escaped.success, Is.True);
        Assert.That(escaped.modulePath, Is.Not.Null);
        Assert.That(
            escaped.modulePath!.value,
            Is.EqualTo("github.com/Azure/azure-sdk-for-go"));
        Assert.That(operations.IsValidModulePath("golang.org/x/mod"), Is.True);
        Assert.That(operations.IsValidModulePath("example/project"), Is.False);
        Assert.That(
            operations.IsValidEscapedModulePath("github.com/!azure/azure-sdk-for-go"),
            Is.True);
        Assert.That(
            operations.IsValidEscapedModulePath("github.com/!9azure/module"),
            Is.False);
    }

    [Test]
    public void SemanticImportOperations_ValidateCanonicalizeFilterAndSelect()
    {
        IGoModuleVersioning operations = new GoModuleOperations();

        GoSemanticImportVersionResult compatible = operations.ValidateSemanticImportVersion(
            "example.com/project/v2",
            "v2.3.0+metadata");
        GoSemanticImportVersionResult mismatch = operations.ValidateSemanticImportVersion(
            "example.com/project/v2",
            "v3.0.0");

        Assert.That(compatible.success, Is.True);
        Assert.That(mismatch.success, Is.False);
        Assert.That(
            operations.IsValidSemanticImportVersion("gopkg.in/yaml.v3", "v3.0.1"),
            Is.True);
        Assert.That(
            operations.IsValidSemanticImportVersion("example.com/project", "v2.0.0"),
            Is.False);
        Assert.That(operations.IsIncompatibleVersion("v2.0.0+incompatible"), Is.True);
        Assert.That(operations.IsIncompatibleVersion("v2.0.0+other"), Is.False);
        Assert.That(
            operations.CanonicalizeModuleVersion(
                "example.com/legacy",
                "v2.1.0+incompatible"),
            Is.EqualTo("v2.1.0+incompatible"));

        string[] filtered = operations.FilterSemanticImportVersions(
            "example.com/legacy",
            new string[]
            {
                "bad",
                "v1.9.0",
                "v2.0.0",
                "v2.0.0+incompatible",
                "v3.0.0+incompatible"
            });

        Assert.That(filtered, Is.EqualTo(new string[]
        {
            "v1.9.0",
            "v2.0.0+incompatible",
            "v3.0.0+incompatible"
        }));
        Assert.That(
            operations.SelectHighestSemanticImportVersion("example.com/legacy", filtered),
            Is.EqualTo("v3.0.0+incompatible"));
        Assert.That(
            operations.SelectHighestSemanticImportVersion(
                "example.com/project/v4",
                new string[] { "v1.0.0", "bad" }),
            Is.Null);
    }
}
