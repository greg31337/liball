using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class GoModulePathParserTests
{
    [Test]
    public void ParseModulePath_AcceptsDownloadableModulePaths()
    {
        AssertModulePath("github.com/example/project", "", false);
        AssertModulePath("example.com/Project_One~two", "", false);
        AssertModulePath("example.com/project/v2", "v2", false);
    }

    [Test]
    public void ParseModulePath_AcceptsGopkgInDotMajorSuffixes()
    {
        AssertModulePath("gopkg.in/yaml.v2", "v2", true);
        AssertModulePath("gopkg.in/check.v1", "v1", true);
        AssertModulePath("gopkg.in/unstable.v0", "v0", true);
    }

    [Test]
    public void ParseModulePath_RejectsInvalidPathStructureAndFirstElement()
    {
        AssertFailure("", GoDiagnosticCodes.MissingModulePath);
        AssertFailure("/example.com/project", GoDiagnosticCodes.InvalidModulePath);
        AssertFailure("example.com/project/", GoDiagnosticCodes.InvalidModulePath);
        AssertFailure("example.com//project", GoDiagnosticCodes.InvalidPathElement);
        AssertFailure("example/project", GoDiagnosticCodes.InvalidFirstPathElement);
        AssertFailure("-example.com/project", GoDiagnosticCodes.InvalidFirstPathElement);
        AssertFailure("Example.com/project", GoDiagnosticCodes.InvalidFirstPathElement);
    }

    [Test]
    public void ParseModulePath_RejectsInvalidPathElements()
    {
        AssertFailure("example.com/.hidden", GoDiagnosticCodes.InvalidPathElement);
        AssertFailure("example.com/trailing.", GoDiagnosticCodes.InvalidPathElement);
        AssertFailure("example.com/bad space", GoDiagnosticCodes.InvalidPathElement);
        AssertFailure("example.com/CON/file", GoDiagnosticCodes.InvalidPathElement);
        AssertFailure("example.com/aux.txt/file", GoDiagnosticCodes.InvalidPathElement);
        AssertFailure("example.com/EXAMPL~1.COM/file", GoDiagnosticCodes.InvalidPathElement);
    }

    [Test]
    public void ParseModulePath_RejectsInvalidMajorVersionSuffixes()
    {
        AssertFailure("example.com/project/v0", GoDiagnosticCodes.InvalidMajorVersionSuffix);
        AssertFailure("example.com/project/v1", GoDiagnosticCodes.InvalidMajorVersionSuffix);
        AssertFailure("example.com/project/v01", GoDiagnosticCodes.InvalidMajorVersionSuffix);
        AssertFailure("example.com/project/v2.0", GoDiagnosticCodes.InvalidMajorVersionSuffix);
        AssertFailure("gopkg.in/yaml", GoDiagnosticCodes.InvalidGopkgInPath);
        AssertFailure("gopkg.in/yaml/v2", GoDiagnosticCodes.InvalidGopkgInPath);
    }

    [Test]
    public void ParseEscapedModulePath_DecodesUppercaseEscapesBeforeValidation()
    {
        GoModulePathParseResult result = GoVersions.ParseEscapedModulePath("github.com/!azure/azure-sdk-for-go");

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.GetSourceText(), Is.EqualTo("github.com/!azure/azure-sdk-for-go"));
        Assert.That(result.GetModulePath().GetValue(), Is.EqualTo("github.com/Azure/azure-sdk-for-go"));
        Assert.That(GoVersions.IsValidEscapedModulePath("github.com/!azure/azure-sdk-for-go"), Is.True);
    }

    [Test]
    public void ParseEscapedModulePath_RejectsMalformedEscapes()
    {
        AssertEscapedFailure("github.com/!", GoDiagnosticCodes.InvalidEscapedPathElement);
        AssertEscapedFailure("github.com/!9azure/module", GoDiagnosticCodes.InvalidEscapedPathElement);
        AssertEscapedFailure("github.com/azure!/module", GoDiagnosticCodes.InvalidEscapedPathElement);
        Assert.That(GoVersions.IsValidModulePath("github.com/!azure/module"), Is.False);
    }

    static void AssertModulePath(string text, string majorVersionSuffix, bool gopkgInPath)
    {
        GoModulePathParseResult result = GoVersions.ParseModulePath(text);

        Assert.That(result.IsSuccess(), Is.True, text);
        Assert.That(result.HasModulePath(), Is.True, text);
        Assert.That(result.GetModulePath().GetValue(), Is.EqualTo(text));
        Assert.That(result.GetModulePath().GetMajorVersionSuffix(), Is.EqualTo(majorVersionSuffix));
        Assert.That(result.GetModulePath().HasMajorVersionSuffix(), Is.EqualTo(majorVersionSuffix.Length > 0));
        Assert.That(result.GetModulePath().IsGopkgInPath(), Is.EqualTo(gopkgInPath));
        Assert.That(GoVersions.IsValidModulePath(text), Is.True);
    }

    static void AssertFailure(string text, string diagnosticCode)
    {
        GoModulePathParseResult result = GoVersions.ParseModulePath(text);

        Assert.That(result.IsFailure(), Is.True, text);
        Assert.That(result.HasModulePath(), Is.False, text);
        Assert.That(result.GetDiagnosticCount(), Is.EqualTo(1), text);
        Assert.That(result.GetDiagnostic(0).GetCode(), Is.EqualTo(diagnosticCode), text);
        Assert.That(GoVersions.IsValidModulePath(text), Is.False, text);
    }

    static void AssertEscapedFailure(string text, string diagnosticCode)
    {
        GoModulePathParseResult result = GoVersions.ParseEscapedModulePath(text);

        Assert.That(result.IsFailure(), Is.True, text);
        Assert.That(result.HasModulePath(), Is.False, text);
        Assert.That(result.GetDiagnosticCount(), Is.EqualTo(1), text);
        Assert.That(result.GetDiagnostic(0).GetCode(), Is.EqualTo(diagnosticCode), text);
        Assert.That(GoVersions.IsValidEscapedModulePath(text), Is.False, text);
    }
}
