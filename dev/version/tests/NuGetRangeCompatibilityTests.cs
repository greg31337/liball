using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class NuGetRangeCompatibilityTests
{
    [Test]
    public void DocumentedVersionRangeExamplesMatchNuGetSemantics()
    {
        AssertRangeMatches(
            "1.0",
            new string[] { "1.0", "1.1.0", "2.0.0" },
            new string[] { "0.9.9" });
        AssertRangeMatches(
            "(,1.0]",
            new string[] { "0.9.9", "1.0" },
            new string[] { "1.0.1" });
        AssertRangeMatches(
            "(,1.0)",
            new string[] { "0.9.9" },
            new string[] { "1.0", "1.0.1" });
        AssertRangeMatches(
            "[1.0]",
            new string[] { "1.0", "1.0.0.0" },
            new string[] { "0.9.9", "1.0.1" });
    }

    [Test]
    public void DocumentedBoundedRangeExamplesMatchNuGetSemantics()
    {
        AssertRangeMatches(
            "(1.0,)",
            new string[] { "1.0.1", "2.0.0" },
            new string[] { "1.0" });
        AssertRangeMatches(
            "[1.0,2.0]",
            new string[] { "1.0", "1.5.0", "2.0" },
            new string[] { "0.9.9", "2.0.1" });
        AssertRangeMatches(
            "(1.0,2.0)",
            new string[] { "1.0.1", "1.5.0" },
            new string[] { "1.0", "2.0" });
        AssertRangeMatches(
            "[1.0,2.0)",
            new string[] { "1.0", "1.5.0" },
            new string[] { "0.9.9", "2.0" });
    }

    [Test]
    public void DocumentedFloatingResolutionExamplesChooseExpectedCandidate()
    {
        AssertFloatingSelects(
            "*",
            new string[] { "1.1.0", "1.1.1", "1.2.0", "1.3.0-alpha" },
            "1.2.0");
        AssertFloatingSelects(
            "1.1.*",
            new string[] { "1.1.0", "1.1.1", "1.1.2-alpha", "1.2.0-alpha" },
            "1.1.1");
        AssertFloatingSelects(
            "*-*",
            new string[] { "1.1.0", "1.1.1", "1.1.2-alpha", "1.3.0-alpha" },
            "1.3.0-alpha");
        AssertFloatingSelects(
            "1.1.*-*",
            new string[] { "1.1.0", "1.1.1", "1.1.2-alpha", "1.3.0-alpha" },
            "1.1.2-alpha");
        AssertFloatingSelects(
            "1.2.0-rc.*",
            new string[] { "1.1.0", "1.2.0-rc.1", "1.2.0-rc.2", "1.2.0" },
            "1.2.0");
    }

    [Test]
    public void FloatingRangeMatchingAndSelectionUseSameCandidateSet()
    {
        VersionRange range = ParseRange("1.1.*-*");
        NuGetFloatingVersion floatingVersion = ParseFloatingVersion("1.1.*-*");
        string[] candidates = new string[] { "1.1.0", "1.1.1", "1.1.2-alpha", "1.3.0-alpha" };

        Assert.That(range.ContainsText("1.1.2-alpha", VersionDialect.NuGet), Is.True);
        Assert.That(range.ContainsText("1.3.0-alpha", VersionDialect.NuGet), Is.False);
        Assert.That(floatingVersion.SelectHighestMatchingText(candidates), Is.EqualTo("1.1.2-alpha"));
    }

    [Test]
    public void NuGetRangesExcludePrereleasesByDefaultUnlessOverridden()
    {
        VersionRange range = ParseRange("[1.0.0,2.0.0)");

        Assert.That(range.ContainsText("1.5.0", VersionDialect.NuGet), Is.True);
        Assert.That(range.ContainsText("1.5.0-alpha", VersionDialect.NuGet), Is.False);
        Assert.That(range.ContainsTextWithPrerelease("1.5.0-alpha", VersionDialect.NuGet, true), Is.True);
    }

    [Test]
    public void InvalidDocumentedRangeFormsFail()
    {
        AssertFailure("(1.0)", NuGetRangeDiagnosticCodes.InvalidRange);
        AssertFailure("[1.0,1.0)", NuGetRangeDiagnosticCodes.InvalidRange);
        AssertFailure("[1.0,]", NuGetRangeDiagnosticCodes.InvalidRange);
        AssertFailure("(,)", NuGetRangeDiagnosticCodes.InvalidRange);
    }

    static VersionRange ParseRange(string text)
    {
        VersionRangeParseResult result = NuGetRangeParser.Parse(text);

        Assert.That(result.IsSuccess(), Is.True, text);
        Assert.That(result.HasRange(), Is.True, text);
        return result.GetRange();
    }

    static NuGetFloatingVersion ParseFloatingVersion(string text)
    {
        NuGetFloatingVersion floatingVersion = NuGetFloatingVersion.Parse(text);

        Assert.That(floatingVersion.IsValid(), Is.True, text);
        return floatingVersion;
    }

    static void AssertRangeMatches(string rangeText, string[] matchingVersions, string[] rejectedVersions)
    {
        VersionRange range = ParseRange(rangeText);
        int index = 0;

        while (index < matchingVersions.Length)
        {
            Assert.That(
                range.ContainsText(matchingVersions[index], VersionDialect.NuGet),
                Is.True,
                rangeText + " should contain " + matchingVersions[index]);
            index = index + 1;
        }

        index = 0;
        while (index < rejectedVersions.Length)
        {
            Assert.That(
                range.ContainsText(rejectedVersions[index], VersionDialect.NuGet),
                Is.False,
                rangeText + " should not contain " + rejectedVersions[index]);
            index = index + 1;
        }
    }

    static void AssertFloatingSelects(string floatingText, string[] candidateTexts, string expectedText)
    {
        NuGetFloatingVersion floatingVersion = ParseFloatingVersion(floatingText);

        Assert.That(floatingVersion.SelectHighestMatchingText(candidateTexts), Is.EqualTo(expectedText), floatingText);
    }

    static void AssertFailure(string text, string expectedCode)
    {
        VersionRangeParseResult result = NuGetRangeParser.Parse(text);

        Assert.That(result.IsFailure(), Is.True, text);
        Assert.That(result.HasRange(), Is.False, text);
        Assert.That(result.GetRangeSyntax(), Is.EqualTo(RangeSyntax.NuGet), text);
        Assert.That(result.GetDiagnosticCount(), Is.EqualTo(1), text);
        Assert.That(result.GetDiagnostic(0).GetCode(), Is.EqualTo(expectedCode), text);
    }
}
