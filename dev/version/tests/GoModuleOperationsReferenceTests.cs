using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class GoModuleOperationsReferenceTests
{
    [TestCase("v0", "v0.0.0", 2)]
    [TestCase("v1.2", "v1.2.0", 1)]
    [TestCase("v1.2.3", "v1.2.3", 0)]
    [TestCase("v8.0.5-pre", "v8.0.5-pre", 0)]
    [TestCase("v2.0.9+meta", "v2.0.9", 0)]
    [TestCase("v0.0.0-20191109021931-daa7c04131f5", "v0.0.0-20191109021931-daa7c04131f5", 0)]
    public void SemVerReference_AcceptsShorthandFullAndCanonicalForms(
        string text,
        string canonicalText,
        int missingComponentCount)
    {
        IGoModuleVersioning operations = new GoModuleOperations();

        VersionParseResult result = operations.ParseVersion(text);

        Assert.That(operations.IsValidVersion(text), Is.True, text);
        Assert.That(result.success, Is.True, text);
        Assert.That(result.dialect, Is.EqualTo(VersionDialect.Go), text);
        Assert.That(result.version, Is.Not.Null, text);
        Assert.That(result.version!.missingComponentCount, Is.EqualTo(missingComponentCount), text);
        Assert.That(operations.CanonicalizeVersion(text), Is.EqualTo(canonicalText), text);
    }

    [TestCase("")]
    [TestCase("v")]
    [TestCase("1.2.3")]
    [TestCase("V1.2.3")]
    [TestCase("v01.2.3")]
    [TestCase("v1.02.3")]
    [TestCase("v1.2.03")]
    [TestCase("v1.2-pre")]
    [TestCase("v1.2+meta")]
    [TestCase("v1.2.3-01")]
    [TestCase("v1.2.3-pre..one")]
    [TestCase("v1.2.3+bad_meta")]
    public void SemVerReference_RejectsMalformedForms(string text)
    {
        IGoModuleVersioning operations = new GoModuleOperations();

        VersionParseResult result = operations.ParseVersion(text);

        Assert.That(operations.IsValidVersion(text), Is.False, text);
        Assert.That(result.success, Is.False, text);
        Assert.That(result.version, Is.Null, text);
        Assert.That(result.diagnostics, Is.Not.Empty, text);
        Assert.That(operations.CanonicalizeVersion(text), Is.Empty, text);
        Assert.That(operations.GetMajorVersion(text), Is.Empty, text);
        Assert.That(operations.GetMajorMinorVersion(text), Is.Empty, text);
        Assert.That(operations.GetPrerelease(text), Is.Empty, text);
        Assert.That(operations.GetBuild(text), Is.Empty, text);
    }

    [TestCase("v2", "v2", "v2.0", "", "")]
    [TestCase("v2.1", "v2", "v2.1", "", "")]
    [TestCase("v2.1.0+meta", "v2", "v2.1", "", "+meta")]
    [TestCase("v2.1.0-pre+meta", "v2", "v2.1", "-pre", "+meta")]
    [TestCase("v10.20.30-alpha.1+build.001", "v10", "v10.20", "-alpha.1", "+build.001")]
    public void SemVerReference_ExtractionHelpersReturnCompleteSuffixes(
        string text,
        string major,
        string majorMinor,
        string prerelease,
        string build)
    {
        IGoModuleVersioning operations = new GoModuleOperations();

        Assert.That(operations.GetMajorVersion(text), Is.EqualTo(major));
        Assert.That(operations.GetMajorMinorVersion(text), Is.EqualTo(majorMinor));
        Assert.That(operations.GetPrerelease(text), Is.EqualTo(prerelease));
        Assert.That(operations.GetBuild(text), Is.EqualTo(build));
    }

    [Test]
    public void SemVerReference_ComparisonUsesPrecedenceAndInvalidOrdering()
    {
        IGoModuleVersioning operations = new GoModuleOperations();
        string[] precedence = new string[]
        {
            "v1.0.0-alpha",
            "v1.0.0-alpha.1",
            "v1.0.0-alpha.beta",
            "v1.0.0-beta",
            "v1.0.0-beta.2",
            "v1.0.0-beta.11",
            "v1.0.0-rc.1",
            "v1.0.0"
        };
        int index = 1;

        while (index < precedence.Length)
        {
            Assert.That(
                operations.CompareVersions(precedence[index - 1], precedence[index]),
                Is.EqualTo(-1),
                precedence[index - 1] + " < " + precedence[index]);
            index = index + 1;
        }

        Assert.That(operations.CompareVersions("bad", "also-bad"), Is.Zero);
        Assert.That(operations.CompareVersions("bad", "v0"), Is.EqualTo(-1));
        Assert.That(operations.CompareVersions("v0", "bad"), Is.EqualTo(1));
        Assert.That(operations.CompareVersions("v1", "v1.0.0"), Is.Zero);
        Assert.That(
            operations.CompareVersions("v1.0.0+build.1", "v1.0.0+build.2"),
            Is.Zero);
    }

    [Test]
    public void SemVerReference_SortingUsesOrdinalFallbackAndReturnsNewArrays()
    {
        IGoModuleVersioning operations = new GoModuleOperations();
        string[] candidates = new string[]
        {
            "v1.0.0+build.2",
            "bad",
            "v1.0.0",
            "also-bad",
            "v1.0.0-pre",
            "v1.0.0+build.1"
        };

        string[] ascending = operations.SortVersionsAscending(candidates);
        string[] descending = operations.SortVersionsDescending(candidates);

        Assert.That(ascending, Is.EqualTo(new string[]
        {
            "also-bad",
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
            "bad",
            "also-bad"
        }));
        Assert.That(candidates, Is.EqualTo(new string[]
        {
            "v1.0.0+build.2",
            "bad",
            "v1.0.0",
            "also-bad",
            "v1.0.0-pre",
            "v1.0.0+build.1"
        }));
        Assert.That(ascending, Is.Not.SameAs(candidates));
        Assert.That(descending, Is.Not.SameAs(candidates));
        Assert.That(operations.SortVersionsAscending(new string[0]), Is.Empty);
    }

    [TestCase("golang.org/x/net", "", false)]
    [TestCase("golang.org/x/repo/sub/v2", "v2", false)]
    [TestCase("example.com/Project_One~two", "", false)]
    [TestCase("gopkg.in/unstable.v0", "v0", true)]
    [TestCase("gopkg.in/check.v1", "v1", true)]
    [TestCase("gopkg.in/yaml.v3", "v3", true)]
    public void ModuleReference_AcceptsModulePathsAndMajorSuffixForms(
        string text,
        string majorVersionSuffix,
        bool gopkgInPath)
    {
        IGoModuleVersioning operations = new GoModuleOperations();

        GoModulePathParseResult result = operations.ParseModulePath(text);

        Assert.That(operations.IsValidModulePath(text), Is.True, text);
        Assert.That(result.success, Is.True, text);
        Assert.That(result.sourceText, Is.EqualTo(text), text);
        Assert.That(result.modulePath, Is.Not.Null, text);
        Assert.That(result.modulePath!.value, Is.EqualTo(text), text);
        Assert.That(result.modulePath.majorVersionSuffix, Is.EqualTo(majorVersionSuffix), text);
        Assert.That(
            result.modulePath.majorVersionSuffixSpecified,
            Is.EqualTo(majorVersionSuffix.Length > 0),
            text);
        Assert.That(result.modulePath.gopkgInPath, Is.EqualTo(gopkgInPath), text);
    }

    [TestCase("", GoDiagnosticCodes.MissingModulePath)]
    [TestCase("/example.com/project", GoDiagnosticCodes.InvalidModulePath)]
    [TestCase("example.com/project/", GoDiagnosticCodes.InvalidModulePath)]
    [TestCase("example.com//project", GoDiagnosticCodes.InvalidPathElement)]
    [TestCase("example/project", GoDiagnosticCodes.InvalidFirstPathElement)]
    [TestCase("Example.com/project", GoDiagnosticCodes.InvalidFirstPathElement)]
    [TestCase("-example.com/project", GoDiagnosticCodes.InvalidFirstPathElement)]
    [TestCase("example.com/.hidden", GoDiagnosticCodes.InvalidPathElement)]
    [TestCase("example.com/trailing.", GoDiagnosticCodes.InvalidPathElement)]
    [TestCase("example.com/CON/file", GoDiagnosticCodes.InvalidPathElement)]
    [TestCase("example.com/EXAMPL~1.COM/file", GoDiagnosticCodes.InvalidPathElement)]
    [TestCase("example.com/project/v1", GoDiagnosticCodes.InvalidMajorVersionSuffix)]
    [TestCase("example.com/project/v01", GoDiagnosticCodes.InvalidMajorVersionSuffix)]
    [TestCase("example.com/project/v2.0", GoDiagnosticCodes.InvalidMajorVersionSuffix)]
    [TestCase("gopkg.in/yaml", GoDiagnosticCodes.InvalidGopkgInPath)]
    [TestCase("gopkg.in/yaml/v2", GoDiagnosticCodes.InvalidGopkgInPath)]
    public void ModuleReference_RejectsInvalidModulePaths(string text, string diagnosticCode)
    {
        IGoModuleVersioning operations = new GoModuleOperations();

        GoModulePathParseResult result = operations.ParseModulePath(text);

        Assert.That(operations.IsValidModulePath(text), Is.False, text);
        Assert.That(result.success, Is.False, text);
        Assert.That(result.modulePath, Is.Null, text);
        Assert.That(result.diagnostics, Has.Length.EqualTo(1), text);
        Assert.That(result.diagnostics[0].code, Is.EqualTo(diagnosticCode), text);
    }

    [Test]
    public void ModuleReference_EscapedPathsDecodeUppercaseAndRejectMalformedEscapes()
    {
        IGoModuleVersioning operations = new GoModuleOperations();

        GoModulePathParseResult azure = operations.ParseEscapedModulePath(
            "github.com/!azure/azure-sdk-for-go");
        GoModulePathParseResult microsoft = operations.ParseEscapedModulePath(
            "github.com/!microsoft/!go-winio");

        Assert.That(azure.success, Is.True);
        Assert.That(azure.sourceText, Is.EqualTo("github.com/!azure/azure-sdk-for-go"));
        Assert.That(azure.modulePath, Is.Not.Null);
        Assert.That(azure.modulePath!.value, Is.EqualTo("github.com/Azure/azure-sdk-for-go"));
        Assert.That(microsoft.success, Is.True);
        Assert.That(microsoft.modulePath, Is.Not.Null);
        Assert.That(microsoft.modulePath!.value, Is.EqualTo("github.com/Microsoft/Go-winio"));
        Assert.That(
            operations.IsValidEscapedModulePath("github.com/!azure/azure-sdk-for-go"),
            Is.True);

        AssertEscapedPathFailure(operations, "github.com/!");
        AssertEscapedPathFailure(operations, "github.com/!9azure/module");
        AssertEscapedPathFailure(operations, "github.com/azure!/module");
        Assert.That(operations.IsValidModulePath("github.com/!azure/module"), Is.False);
    }

    [TestCase("example.com/project", "v0.9.0", "")]
    [TestCase("example.com/project", "v1.2.3+meta", "")]
    [TestCase("example.com/project/v2", "v2.0.0", "v2")]
    [TestCase("example.com/project/v10", "v10.3.0-pre", "v10")]
    [TestCase("gopkg.in/unstable.v0", "v0.5.0", "v0")]
    [TestCase("gopkg.in/check.v1", "v1.0.0", "v1")]
    [TestCase("gopkg.in/yaml.v3", "v3.0.1", "v3")]
    public void ModuleReference_AcceptsMatchingSemanticImportVersions(
        string modulePathText,
        string versionText,
        string majorVersionSuffix)
    {
        IGoModuleVersioning operations = new GoModuleOperations();

        GoSemanticImportVersionResult result = operations.ValidateSemanticImportVersion(
            modulePathText,
            versionText);

        Assert.That(
            operations.IsValidSemanticImportVersion(modulePathText, versionText),
            Is.True);
        Assert.That(result.success, Is.True);
        Assert.That(result.modulePath, Is.Not.Null);
        Assert.That(result.version, Is.Not.Null);
        Assert.That(result.modulePath!.value, Is.EqualTo(modulePathText));
        Assert.That(result.modulePath.majorVersionSuffix, Is.EqualTo(majorVersionSuffix));
        Assert.That(result.version!.originalText, Is.EqualTo(versionText));
    }

    [TestCase("example.com/project", "v2.0.0", GoDiagnosticCodes.MissingMajorVersionSuffix)]
    [TestCase("example.com/project/v2", "v0.9.0", GoDiagnosticCodes.UnexpectedMajorVersionSuffix)]
    [TestCase("example.com/project/v2", "v1.0.0", GoDiagnosticCodes.UnexpectedMajorVersionSuffix)]
    [TestCase("example.com/project/v2", "v3.0.0", GoDiagnosticCodes.MajorVersionMismatch)]
    [TestCase("gopkg.in/yaml.v2", "v3.0.0", GoDiagnosticCodes.MajorVersionMismatch)]
    [TestCase("example/project", "v1.0.0", GoDiagnosticCodes.InvalidFirstPathElement)]
    [TestCase("example.com/project", "1.0.0", GoDiagnosticCodes.MissingLeadingV)]
    public void ModuleReference_ReportsSemanticImportMismatches(
        string modulePathText,
        string versionText,
        string diagnosticCode)
    {
        IGoModuleVersioning operations = new GoModuleOperations();

        GoSemanticImportVersionResult result = operations.ValidateSemanticImportVersion(
            modulePathText,
            versionText);

        Assert.That(
            operations.IsValidSemanticImportVersion(modulePathText, versionText),
            Is.False);
        Assert.That(result.success, Is.False);
        Assert.That(result.modulePath, Is.Null);
        Assert.That(result.version, Is.Null);
        Assert.That(result.diagnostics, Has.Length.EqualTo(1));
        Assert.That(result.diagnostics[0].code, Is.EqualTo(diagnosticCode));
    }

    [Test]
    public void ModuleReference_IncompatibleVersionsRequireLegacyUnsuffixedV2OrHigher()
    {
        IGoModuleVersioning operations = new GoModuleOperations();
        string pseudoVersion = "v2.0.1-20200722182040-012345abcdef+incompatible";

        Assert.That(
            operations.IsValidSemanticImportVersion(
                "example.com/m",
                "v4.1.2+incompatible"),
            Is.True);
        Assert.That(
            operations.IsValidSemanticImportVersion("example.com/m", pseudoVersion),
            Is.True);
        Assert.That(operations.IsIncompatibleVersion("v4.1.2+incompatible"), Is.True);
        Assert.That(operations.IsIncompatibleVersion(pseudoVersion), Is.True);
        Assert.That(operations.IsIncompatibleVersion("v4.1.2+meta"), Is.False);
        Assert.That(operations.IsIncompatibleVersion("v4.1.2+incompatible.extra"), Is.False);
        Assert.That(operations.IsIncompatibleVersion("bad+incompatible"), Is.False);
        Assert.That(
            operations.CanonicalizeModuleVersion(
                "example.com/m",
                "v4.1.2+incompatible"),
            Is.EqualTo("v4.1.2+incompatible"));
        Assert.That(
            operations.CanonicalizeModuleVersion("example.com/m", pseudoVersion),
            Is.EqualTo(pseudoVersion));
        Assert.That(
            operations.CanonicalizeModuleVersion(
                "example.com/project/v2",
                "v2.0.0+meta"),
            Is.EqualTo("v2.0.0"));

        AssertIncompatibleFailure(operations, "example.com/m", "v1.9.0+incompatible");
        AssertIncompatibleFailure(operations, "example.com/m/v2", "v2.0.0+incompatible");
        AssertIncompatibleFailure(operations, "gopkg.in/yaml.v2", "v2.0.0+incompatible");
    }

    [Test]
    public void ModuleReference_FiniteSelectionFiltersByPathAndUsesHighestPrecedence()
    {
        IGoModuleVersioning operations = new GoModuleOperations();
        string[] legacyCandidates = new string[]
        {
            "bad",
            "v1.9.0",
            "v2.0.0",
            "v2.0.0-pre+incompatible",
            "v2.0.0+incompatible",
            "v3.0.0+incompatible"
        };
        string[] suffixedCandidates = new string[]
        {
            "v1.9.0",
            "v2.0.0+incompatible",
            "v2.0.0-pre",
            "v2.0.0+meta",
            "v2.1.0"
        };

        string[] legacy = operations.FilterSemanticImportVersions(
            "example.com/legacy",
            legacyCandidates);
        string[] suffixed = operations.FilterSemanticImportVersions(
            "example.com/current/v2",
            suffixedCandidates);

        Assert.That(legacy, Is.EqualTo(new string[]
        {
            "v1.9.0",
            "v2.0.0-pre+incompatible",
            "v2.0.0+incompatible",
            "v3.0.0+incompatible"
        }));
        Assert.That(suffixed, Is.EqualTo(new string[]
        {
            "v2.0.0-pre",
            "v2.0.0+meta",
            "v2.1.0"
        }));
        Assert.That(
            operations.SelectHighestSemanticImportVersion(
                "example.com/legacy",
                legacyCandidates),
            Is.EqualTo("v3.0.0+incompatible"));
        Assert.That(
            operations.SelectHighestSemanticImportVersion(
                "example.com/current/v2",
                suffixedCandidates),
            Is.EqualTo("v2.1.0"));
        Assert.That(
            operations.SelectHighestSemanticImportVersion(
                "example.com/current/v4",
                new string[] { "bad", "v1.0.0", "v4.0.0+incompatible" }),
            Is.Null);
        Assert.That(legacyCandidates[0], Is.EqualTo("bad"));
        Assert.That(suffixedCandidates[0], Is.EqualTo("v1.9.0"));
    }

    static void AssertEscapedPathFailure(IGoModuleVersioning operations, string text)
    {
        GoModulePathParseResult result = operations.ParseEscapedModulePath(text);

        Assert.That(operations.IsValidEscapedModulePath(text), Is.False, text);
        Assert.That(result.success, Is.False, text);
        Assert.That(result.modulePath, Is.Null, text);
        Assert.That(result.diagnostics, Has.Length.EqualTo(1), text);
        Assert.That(
            result.diagnostics[0].code,
            Is.EqualTo(GoDiagnosticCodes.InvalidEscapedPathElement),
            text);
    }

    static void AssertIncompatibleFailure(
        IGoModuleVersioning operations,
        string modulePathText,
        string versionText)
    {
        GoSemanticImportVersionResult result = operations.ValidateSemanticImportVersion(
            modulePathText,
            versionText);

        Assert.That(result.success, Is.False, modulePathText + " " + versionText);
        Assert.That(result.diagnostics, Has.Length.EqualTo(1));
        Assert.That(
            result.diagnostics[0].code,
            Is.EqualTo(GoDiagnosticCodes.InvalidIncompatibleVersion));
        Assert.That(
            operations.CanonicalizeModuleVersion(modulePathText, versionText),
            Is.Empty);
    }
}
