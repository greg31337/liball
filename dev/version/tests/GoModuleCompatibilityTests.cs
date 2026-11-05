using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class GoModuleCompatibilityTests
{
    [Test]
    public void Compatibility_ValidAndInvalidModulePathExamples()
    {
        AssertValidPath("github.com/google/uuid", "", false);
        AssertValidPath("golang.org/x/mod", "", false);
        AssertValidPath("example.com/project/v2", "v2", false);
        AssertValidPath("gopkg.in/yaml.v3", "v3", true);
        AssertValidEscapedPath("github.com/!azure/azure-sdk-for-go", "github.com/Azure/azure-sdk-for-go");

        AssertInvalidPath("example/project", GoDiagnosticCodes.InvalidFirstPathElement);
        AssertInvalidPath("example.com/project/v1", GoDiagnosticCodes.InvalidMajorVersionSuffix);
        AssertInvalidPath("gopkg.in/yaml", GoDiagnosticCodes.InvalidGopkgInPath);
        AssertInvalidPath("example.com/bad path", GoDiagnosticCodes.InvalidPathElement);
        AssertInvalidEscapedPath("github.com/!9azure/module", GoDiagnosticCodes.InvalidEscapedPathElement);
    }

    [Test]
    public void Compatibility_SemanticImportVersionExamples()
    {
        AssertValidSemanticVersion("github.com/google/uuid", "v1.6.0", "");
        AssertValidSemanticVersion("example.com/project/v2", "v2.3.4", "v2");
        AssertValidSemanticVersion("gopkg.in/yaml.v3", "v3.0.1", "v3");

        AssertInvalidSemanticVersion("example.com/project", "v2.0.0", GoDiagnosticCodes.MissingMajorVersionSuffix);
        AssertInvalidSemanticVersion("example.com/project/v2", "v1.9.0", GoDiagnosticCodes.UnexpectedMajorVersionSuffix);
        AssertInvalidSemanticVersion("example.com/project/v2", "v3.0.0", GoDiagnosticCodes.MajorVersionMismatch);
    }

    [Test]
    public void Compatibility_IncompatibleVersionsWorkForLegacyUnsuffixedModules()
    {
        AssertValidSemanticVersion("example.com/legacy", "v2.0.0+incompatible", "");
        AssertValidSemanticVersion("example.com/legacy", "v2.0.0-pre+incompatible", "");

        AssertInvalidSemanticVersion("example.com/legacy/v2", "v2.0.0+incompatible", GoDiagnosticCodes.InvalidIncompatibleVersion);
        AssertInvalidSemanticVersion("example.com/legacy", "v1.9.9+incompatible", GoDiagnosticCodes.InvalidIncompatibleVersion);
        Assert.That(
            GoVersions.CanonicalizeModuleVersion("example.com/legacy", "v2.0.0+incompatible"),
            Is.EqualTo("v2.0.0+incompatible"));
    }

    [Test]
    public void Compatibility_PrereleaseHandlingFollowsGoSemVerPrecedence()
    {
        Assert.That(GoVersions.CompareModuleVersions("v2.0.0-pre+incompatible", "v2.0.0+incompatible"), Is.LessThan(0));
        Assert.That(GoVersions.CompareModuleVersions("v2.0.0-pre.1+incompatible", "v2.0.0-pre.2+incompatible"), Is.LessThan(0));
        Assert.That(GoVersions.CanonicalizeModuleVersion("example.com/legacy", "v2.0.0-pre+incompatible"), Is.EqualTo("v2.0.0-pre+incompatible"));
    }

    [Test]
    public void Compatibility_CanonicalSortOrderForFilteredModuleCandidates()
    {
        string[] filtered = GoVersions.FilterSemanticImportVersions(
            "example.com/legacy",
            new string[]
            {
                "bad",
                "v3.0.0+incompatible",
                "v2.0.0",
                "v2.0.0+incompatible",
                "v2.0.0-pre+incompatible",
                "v1.9.0"
            });
        string[] sorted = GoVersionComparer.SortAscending(filtered);
        string[] canonical = CanonicalizeCandidates("example.com/legacy", sorted);

        Assert.That(sorted, Is.EqualTo(new string[]
        {
            "v1.9.0",
            "v2.0.0-pre+incompatible",
            "v2.0.0+incompatible",
            "v3.0.0+incompatible"
        }));
        Assert.That(canonical, Is.EqualTo(new string[]
        {
            "v1.9.0",
            "v2.0.0-pre+incompatible",
            "v2.0.0+incompatible",
            "v3.0.0+incompatible"
        }));
    }

    static void AssertValidPath(string pathText, string majorSuffix, bool gopkgInPath)
    {
        GoModulePathParseResult result = GoVersions.ParseModulePath(pathText);

        Assert.That(result.IsSuccess(), Is.True, pathText);
        Assert.That(result.GetModulePath().GetMajorVersionSuffix(), Is.EqualTo(majorSuffix), pathText);
        Assert.That(result.GetModulePath().IsGopkgInPath(), Is.EqualTo(gopkgInPath), pathText);
    }

    static void AssertValidEscapedPath(string escapedPathText, string decodedPathText)
    {
        GoModulePathParseResult result = GoVersions.ParseEscapedModulePath(escapedPathText);

        Assert.That(result.IsSuccess(), Is.True, escapedPathText);
        Assert.That(result.GetModulePath().GetValue(), Is.EqualTo(decodedPathText));
    }

    static void AssertInvalidPath(string pathText, string diagnosticCode)
    {
        GoModulePathParseResult result = GoVersions.ParseModulePath(pathText);

        Assert.That(result.IsFailure(), Is.True, pathText);
        Assert.That(result.GetDiagnosticCount(), Is.EqualTo(1), pathText);
        Assert.That(result.GetDiagnostic(0).GetCode(), Is.EqualTo(diagnosticCode), pathText);
    }

    static void AssertInvalidEscapedPath(string escapedPathText, string diagnosticCode)
    {
        GoModulePathParseResult result = GoVersions.ParseEscapedModulePath(escapedPathText);

        Assert.That(result.IsFailure(), Is.True, escapedPathText);
        Assert.That(result.GetDiagnosticCount(), Is.EqualTo(1), escapedPathText);
        Assert.That(result.GetDiagnostic(0).GetCode(), Is.EqualTo(diagnosticCode), escapedPathText);
    }

    static void AssertValidSemanticVersion(string pathText, string versionText, string majorSuffix)
    {
        GoSemanticImportVersionResult result = GoVersions.ValidateSemanticImportVersion(pathText, versionText);

        Assert.That(result.IsSuccess(), Is.True, pathText + " " + versionText);
        Assert.That(result.GetModulePath().GetMajorVersionSuffix(), Is.EqualTo(majorSuffix));
        Assert.That(result.GetVersion().GetOriginalText(), Is.EqualTo(versionText));
    }

    static void AssertInvalidSemanticVersion(string pathText, string versionText, string diagnosticCode)
    {
        GoSemanticImportVersionResult result = GoVersions.ValidateSemanticImportVersion(pathText, versionText);

        Assert.That(result.IsFailure(), Is.True, pathText + " " + versionText);
        Assert.That(result.GetDiagnosticCount(), Is.EqualTo(1));
        Assert.That(result.GetDiagnostic(0).GetCode(), Is.EqualTo(diagnosticCode));
    }

    static string[] CanonicalizeCandidates(string modulePathText, string[] versionTexts)
    {
        string[] canonical = new string[versionTexts.Length];
        int index = 0;

        while (index < versionTexts.Length)
        {
            canonical[index] = GoVersions.CanonicalizeModuleVersion(modulePathText, versionTexts[index]);
            index = index + 1;
        }

        return canonical;
    }
}
