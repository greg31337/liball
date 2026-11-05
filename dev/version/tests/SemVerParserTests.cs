using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class SemVerParserTests
{
    [Test]
    public void ParseStrict_ParsesCorePrereleaseAndBuildMetadata()
    {
        VersionParseResult result = SemVerParser.ParseStrict("1.2.3-alpha.1+build.5");

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.HasVersion(), Is.True);
        Assert.That(result.GetDiagnosticCount(), Is.EqualTo(0));

        UniversalVersion version = result.GetVersion();
        Assert.That(version.GetDialect(), Is.EqualTo(VersionDialect.SemVer));
        Assert.That(version.GetOriginalText(), Is.EqualTo("1.2.3-alpha.1+build.5"));
        Assert.That(version.GetNumericComponent(0), Is.EqualTo("1"));
        Assert.That(version.GetNumericComponent(1), Is.EqualTo("2"));
        Assert.That(version.GetNumericComponent(2), Is.EqualTo("3"));
        Assert.That(version.IsPrerelease(), Is.True);
        Assert.That(version.GetPrereleaseIdentifierCount(), Is.EqualTo(2));
        Assert.That(version.GetPrereleaseIdentifier(0).GetText(), Is.EqualTo("alpha"));
        Assert.That(version.GetPrereleaseIdentifier(0).IsText(), Is.True);
        Assert.That(version.GetPrereleaseIdentifier(1).GetText(), Is.EqualTo("1"));
        Assert.That(version.GetPrereleaseIdentifier(1).IsNumeric(), Is.True);
        Assert.That(version.HasBuildMetadata(), Is.True);
        Assert.That(version.GetBuildIdentifierCount(), Is.EqualTo(2));
        Assert.That(version.GetBuildIdentifier(0).GetText(), Is.EqualTo("build"));
        Assert.That(version.GetBuildIdentifier(1).GetText(), Is.EqualTo("5"));
        Assert.That(version.ToNormalizedString(), Is.EqualTo("1.2.3-alpha.1+build.5"));
    }

    [Test]
    public void ParseStrict_AcceptsSemVerBoundaryForms()
    {
        AssertParseSuccess("0.0.0");
        AssertParseSuccess("1.0.0-alpha");
        AssertParseSuccess("1.0.0-alpha.beta");
        AssertParseSuccess("1.0.0-0.3.7");
        AssertParseSuccess("1.0.0-x-y-z.--");
        AssertParseSuccess("1.0.0+20130313144700");
        AssertParseSuccess("1.0.0-beta+exp.sha.5114f85");
    }

    [Test]
    public void ParseStrict_RejectsLooseOrMalformedVersions()
    {
        AssertParseFailure("");
        AssertParseFailure("v1.2.3");
        AssertParseFailure("1.2");
        AssertParseFailure("1.2.3.4");
        AssertParseFailure("01.2.3");
        AssertParseFailure("1.02.3");
        AssertParseFailure("1.2.03");
        AssertParseFailure("1.2.3-");
        AssertParseFailure("1.2.3-alpha..1");
        AssertParseFailure("1.2.3-01");
        AssertParseFailure("1.2.3-alpha_1");
        AssertParseFailure("1.2.3+");
        AssertParseFailure("1.2.3+build..1");
        AssertParseFailure("1.2.3 build");
    }

    [Test]
    public void ParseStrict_FailureCarriesDiagnosticContext()
    {
        VersionParseResult result = SemVerParser.ParseStrict("1.2.3\nextra");

        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.HasVersion(), Is.False);
        Assert.That(result.GetSourceText(), Is.EqualTo("1.2.3\nextra"));
        Assert.That(result.GetDialect(), Is.EqualTo(VersionDialect.SemVer));
        Assert.That(result.GetDiagnosticCount(), Is.EqualTo(1));
        Assert.That(result.GetDiagnostic(0).GetCode(), Is.EqualTo(SemVerDiagnosticCodes.TrailingContent));
        Assert.That(result.GetDiagnostic(0).GetLineNumber(), Is.EqualTo(1));
        Assert.That(result.GetDiagnostic(0).GetColumnNumber(), Is.EqualTo(6));
    }

    [Test]
    public void ParseStrict_ReportsSpecificValidationDiagnostics()
    {
        AssertParseDiagnostic("", SemVerDiagnosticCodes.MissingMajor, 1, 1);
        AssertParseDiagnostic("1", SemVerDiagnosticCodes.MissingMinor, 1, 2);
        AssertParseDiagnostic("1.2", SemVerDiagnosticCodes.MissingPatch, 1, 4);
        AssertParseDiagnostic("1x.2.3", SemVerDiagnosticCodes.ExpectedDot, 1, 2);
        AssertParseDiagnostic("01.2.3", SemVerDiagnosticCodes.NumericComponentLeadingZero, 1, 1);
        AssertParseDiagnostic("1.2.3-01", SemVerDiagnosticCodes.PrereleaseLeadingZero, 1, 7);
        AssertParseDiagnostic("1.2.3-alpha..1", SemVerDiagnosticCodes.EmptyPrereleaseIdentifier, 1, 13);
        AssertParseDiagnostic("1.2.3+build..1", SemVerDiagnosticCodes.EmptyBuildIdentifier, 1, 13);
        AssertParseDiagnostic("1.2.3-alpha_1", SemVerDiagnosticCodes.IllegalCharacter, 1, 12);
        AssertParseDiagnostic("1.2.3-", SemVerDiagnosticCodes.MalformedPrerelease, 1, 7);
        AssertParseDiagnostic("1.2.3+", SemVerDiagnosticCodes.MalformedBuild, 1, 7);
    }

    [Test]
    public void ParseLoose_AcceptsLeadingVEqualsWhitespaceAndLeadingZeroes()
    {
        string text = " \t= v01.002.0003-alpha.01+build.0007 \r\n";
        VersionParseResult result = SemVerParser.ParseLoose(text);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.GetSourceText(), Is.EqualTo(text));
        Assert.That(result.GetDialect(), Is.EqualTo(VersionDialect.NodeSemVer));

        UniversalVersion version = result.GetVersion();
        Assert.That(version.GetOriginalText(), Is.EqualTo(text));
        Assert.That(version.GetDialect(), Is.EqualTo(VersionDialect.NodeSemVer));
        Assert.That(version.HasLeadingVPrefix(), Is.True);
        Assert.That(version.GetNumericComponent(0), Is.EqualTo("01"));
        Assert.That(version.GetNumericComponent(1), Is.EqualTo("002"));
        Assert.That(version.GetNumericComponent(2), Is.EqualTo("0003"));
        Assert.That(version.GetPrereleaseIdentifier(1).GetText(), Is.EqualTo("01"));
        Assert.That(version.GetBuildIdentifier(1).GetText(), Is.EqualTo("0007"));
        Assert.That(version.ToNormalizedString(), Is.EqualTo("1.2.3-alpha.1+build.7"));
        Assert.That(version.ToCanonicalString(), Is.EqualTo("v1.2.3-alpha.1+build.7"));

        Assert.That(SemVerParser.ParseStrict(text).IsFailure(), Is.True);
    }

    [Test]
    public void ParseLoose_AcceptsCommonCompatibilityPrefixes()
    {
        AssertLooseSuccess("v1.2.3", "1.2.3", true);
        AssertLooseSuccess("V1.2.3", "1.2.3", true);
        AssertLooseSuccess("=1.2.3", "1.2.3", false);
        AssertLooseSuccess("= v1.2.3", "1.2.3", true);
        AssertLooseSuccess(" 1.2.3 ", "1.2.3", false);
    }

    [Test]
    public void ParseLoose_DoesNotCoercePartialOrEmbeddedVersions()
    {
        AssertLooseFailure("1.2");
        AssertLooseFailure("version 1.2.3");
        AssertLooseFailure("1.2.3-alpha_1");
    }

    static void AssertParseSuccess(string text)
    {
        VersionParseResult result = SemVerParser.ParseStrict(text);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.HasVersion(), Is.True);
        Assert.That(result.GetVersion().ToOriginalString(), Is.EqualTo(text));
    }

    static void AssertParseFailure(string text)
    {
        VersionParseResult result = SemVerParser.ParseStrict(text);

        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.HasVersion(), Is.False);
        Assert.That(result.GetDiagnosticCount(), Is.EqualTo(1));
    }

    static void AssertParseDiagnostic(string text, string code, int lineNumber, int columnNumber)
    {
        VersionParseResult result = SemVerParser.ParseStrict(text);

        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.GetDiagnosticCount(), Is.EqualTo(1));
        Assert.That(result.GetDiagnostic(0).GetCode(), Is.EqualTo(code));
        Assert.That(result.GetDiagnostic(0).GetLineNumber(), Is.EqualTo(lineNumber));
        Assert.That(result.GetDiagnostic(0).GetColumnNumber(), Is.EqualTo(columnNumber));
    }

    static void AssertLooseSuccess(string text, string normalizedText, bool leadingVPrefix)
    {
        VersionParseResult result = SemVerParser.ParseLoose(text);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.GetVersion().ToNormalizedString(), Is.EqualTo(normalizedText));
        Assert.That(result.GetVersion().HasLeadingVPrefix(), Is.EqualTo(leadingVPrefix));
    }

    static void AssertLooseFailure(string text)
    {
        VersionParseResult result = SemVerParser.ParseLoose(text);

        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.HasVersion(), Is.False);
        Assert.That(result.GetDiagnosticCount(), Is.EqualTo(1));
    }
}
