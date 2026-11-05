using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class GoIncompatibleVersionTests
{
    [Test]
    public void ValidateSemanticImportVersion_AllowsIncompatibleV2AndHigherWithoutPathSuffix()
    {
        AssertValid("example.com/legacy", "v2.0.0+incompatible");
        AssertValid("example.com/legacy", "v3.1.0-pre+incompatible");
        Assert.That(GoVersions.IsIncompatibleVersion("v2.0.0+incompatible"), Is.True);
        Assert.That(GoVersions.IsIncompatibleVersion("v2.0.0+metadata"), Is.False);
    }

    [Test]
    public void ValidateSemanticImportVersion_RejectsInvalidIncompatiblePlacements()
    {
        AssertFailure("example.com/legacy", "v1.9.0+incompatible");
        AssertFailure("example.com/legacy/v2", "v2.0.0+incompatible");
        AssertFailure("gopkg.in/yaml.v2", "v2.0.0+incompatible");
    }

    [Test]
    public void CanonicalizeModuleVersion_PreservesOnlyIncompatibleBuildMetadata()
    {
        Assert.That(
            GoVersions.CanonicalizeModuleVersion("example.com/legacy", "v2.0.0+incompatible"),
            Is.EqualTo("v2.0.0+incompatible"));
        Assert.That(
            GoVersions.CanonicalizeModuleVersion("example.com/legacy", "v3.1.0-pre+incompatible"),
            Is.EqualTo("v3.1.0-pre+incompatible"));
        Assert.That(
            GoVersions.CanonicalizeModuleVersion("example.com/current/v2", "v2.0.0+metadata"),
            Is.EqualTo("v2.0.0"));
        Assert.That(
            GoVersions.CanonicalizeModuleVersion("example.com/legacy", "v2.0.0+metadata"),
            Is.EqualTo(""));
    }

    [Test]
    public void CompareModuleVersions_IgnoresIncompatibleBuildMetadataForPrecedence()
    {
        Assert.That(GoVersions.CompareModuleVersions("v2.0.0+incompatible", "v2.0.0"), Is.EqualTo(0));
        Assert.That(GoVersions.CompareModuleVersions("v2.1.0+incompatible", "v2.0.9"), Is.GreaterThan(0));
        Assert.That(GoVersions.CompareModuleVersions("v2.0.0-pre+incompatible", "v2.0.0+incompatible"), Is.LessThan(0));
    }

    [Test]
    public void FilterSemanticImportVersions_KeepsCompatibleCandidatesForUnsuffixedPaths()
    {
        string[] filtered = GoVersions.FilterSemanticImportVersions(
            "example.com/legacy",
            new string[]
            {
                "v1.5.0",
                "v2.0.0",
                "v2.1.0+incompatible",
                "v3.0.0+incompatible",
                "bad"
            });

        Assert.That(filtered, Is.EqualTo(new string[] { "v1.5.0", "v2.1.0+incompatible", "v3.0.0+incompatible" }));
        Assert.That(
            GoVersions.SelectHighestSemanticImportVersion(
                "example.com/legacy",
                new string[] { "v1.5.0", "v2.1.0+incompatible", "v3.0.0+incompatible" }),
            Is.EqualTo("v3.0.0+incompatible"));
    }

    [Test]
    public void FilterSemanticImportVersions_DropsIncompatibleCandidatesForSuffixedPaths()
    {
        string[] filtered = GoVersions.FilterSemanticImportVersions(
            "example.com/current/v2",
            new string[]
            {
                "v1.5.0",
                "v2.0.0",
                "v2.1.0+incompatible",
                "v2.2.0"
            });

        Assert.That(filtered, Is.EqualTo(new string[] { "v2.0.0", "v2.2.0" }));
        Assert.That(
            GoVersions.SelectHighestSemanticImportVersion("example.com/current/v2", filtered),
            Is.EqualTo("v2.2.0"));
    }

    static void AssertValid(string modulePathText, string versionText)
    {
        GoSemanticImportVersionResult result = GoVersions.ValidateSemanticImportVersion(modulePathText, versionText);

        Assert.That(result.IsSuccess(), Is.True, modulePathText + " " + versionText);
        Assert.That(result.HasModulePath(), Is.True);
        Assert.That(result.HasVersion(), Is.True);
    }

    static void AssertFailure(string modulePathText, string versionText)
    {
        GoSemanticImportVersionResult result = GoVersions.ValidateSemanticImportVersion(modulePathText, versionText);

        Assert.That(result.IsFailure(), Is.True, modulePathText + " " + versionText);
        Assert.That(result.GetDiagnosticCount(), Is.EqualTo(1));
        Assert.That(result.GetDiagnostic(0).GetCode(), Is.EqualTo(GoDiagnosticCodes.InvalidIncompatibleVersion));
        Assert.That(result.GetDiagnostic(0).GetMessage(), Does.Contain("+incompatible"));
    }
}
