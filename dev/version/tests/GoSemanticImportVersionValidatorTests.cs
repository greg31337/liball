using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class GoSemanticImportVersionValidatorTests
{
    [Test]
    public void ValidateSemanticImportVersion_AcceptsV0AndV1WithoutPathSuffix()
    {
        AssertValid("example.com/project", "v0.9.0", "");
        AssertValid("example.com/project", "v1.2.3", "");
        Assert.That(GoVersions.IsValidSemanticImportVersion("example.com/project", "v1.2.3"), Is.True);
    }

    [Test]
    public void ValidateSemanticImportVersion_AcceptsV2AndHigherWithMatchingSlashSuffix()
    {
        AssertValid("example.com/project/v2", "v2.0.0", "v2");
        AssertValid("example.com/project/v10", "v10.3.0", "v10");
    }

    [Test]
    public void ValidateSemanticImportVersion_AcceptsGopkgInDotSuffixMatchingVersionMajor()
    {
        AssertValid("gopkg.in/unstable.v0", "v0.5.0", "v0");
        AssertValid("gopkg.in/check.v1", "v1.0.0", "v1");
        AssertValid("gopkg.in/yaml.v2", "v2.4.0", "v2");
    }

    [Test]
    public void ValidateSemanticImportVersion_ReportsMissingSuffixForV2AndHigher()
    {
        AssertFailure("example.com/project", "v2.0.0", GoDiagnosticCodes.MissingMajorVersionSuffix, "/v2");
        AssertFailure("example.com/project", "v10.0.0", GoDiagnosticCodes.MissingMajorVersionSuffix, "/v10");
    }

    [Test]
    public void ValidateSemanticImportVersion_ReportsUnexpectedSuffixForV0AndV1()
    {
        AssertFailure("example.com/project/v2", "v0.9.0", GoDiagnosticCodes.UnexpectedMajorVersionSuffix, "v0 and v1");
        AssertFailure("example.com/project/v2", "v1.0.0", GoDiagnosticCodes.UnexpectedMajorVersionSuffix, "v0 and v1");
    }

    [Test]
    public void ValidateSemanticImportVersion_ReportsMismatchedMajorSuffixes()
    {
        AssertFailure("example.com/project/v2", "v3.0.0", GoDiagnosticCodes.MajorVersionMismatch, "v3");
        AssertFailure("gopkg.in/yaml.v2", "v3.0.0", GoDiagnosticCodes.MajorVersionMismatch, "v3");
    }

    [Test]
    public void ValidateSemanticImportVersion_PropagatesModulePathAndVersionDiagnostics()
    {
        AssertFailure("example/project", "v1.0.0", GoDiagnosticCodes.InvalidFirstPathElement, "Invalid");
        AssertFailure("example.com/project", "1.0.0", GoDiagnosticCodes.MissingLeadingV, "lowercase v");
        Assert.That(GoVersions.IsValidSemanticImportVersion("example/project", "v1.0.0"), Is.False);
    }

    static void AssertValid(string modulePathText, string versionText, string majorVersionSuffix)
    {
        GoSemanticImportVersionResult result = GoVersions.ValidateSemanticImportVersion(modulePathText, versionText);

        Assert.That(result.IsSuccess(), Is.True, modulePathText + " " + versionText);
        Assert.That(result.HasModulePath(), Is.True);
        Assert.That(result.HasVersion(), Is.True);
        Assert.That(result.GetModulePath().GetValue(), Is.EqualTo(modulePathText));
        Assert.That(result.GetModulePath().GetMajorVersionSuffix(), Is.EqualTo(majorVersionSuffix));
        Assert.That(result.GetVersion().GetOriginalText(), Is.EqualTo(versionText));
    }

    static void AssertFailure(string modulePathText, string versionText, string diagnosticCode, string messageFragment)
    {
        GoSemanticImportVersionResult result = GoVersions.ValidateSemanticImportVersion(modulePathText, versionText);

        Assert.That(result.IsFailure(), Is.True, modulePathText + " " + versionText);
        Assert.That(result.HasModulePath(), Is.False);
        Assert.That(result.HasVersion(), Is.False);
        Assert.That(result.GetDiagnosticCount(), Is.EqualTo(1));
        Assert.That(result.GetDiagnostic(0).GetCode(), Is.EqualTo(diagnosticCode));
        Assert.That(result.GetDiagnostic(0).GetMessage(), Does.Contain(messageFragment));
    }
}
