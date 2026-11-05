using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class MavenRangeCompatibilityTests
{
    [Test]
    public void DocumentedSoftRequirementDoesNotCreateHardBounds()
    {
        AssertMatches(
            "1.0",
            new string[] { "0.9", "1.0", "2.0" },
            new string[] { });
    }

    [Test]
    public void DocumentedExactRequirementMatchesEquivalentMavenVersions()
    {
        AssertMatches(
            "[1.0]",
            new string[] { "1", "1.0", "1.ga" },
            new string[] { "1.0.1", "1.1" });
    }

    [Test]
    public void DocumentedOpenEndedRequirementsMatchExpectedEdges()
    {
        AssertMatches(
            "(,1.0]",
            new string[] { "0.9", "1.0" },
            new string[] { "1.0.1", "1.1" });
        AssertMatches(
            "[1.5,)",
            new string[] { "1.5", "1.5.1", "2.0" },
            new string[] { "1.4.9" });
    }

    [Test]
    public void DocumentedBoundedRequirementsMatchExpectedEdges()
    {
        AssertMatches(
            "[1.2,1.3]",
            new string[] { "1.2", "1.2.5", "1.3" },
            new string[] { "1.1.9", "1.3.1" });
        AssertMatches(
            "[1.0,2.0)",
            new string[] { "1.0", "1.5", "2.0-rc1" },
            new string[] { "0.9.9", "2.0" });
    }

    [Test]
    public void DocumentedCommaSeparatedRequirementsCreateUnion()
    {
        AssertMatches(
            "(,1.0],[1.2,)",
            new string[] { "0.9", "1.0", "1.2", "2.0" },
            new string[] { "1.1" });
    }

    [Test]
    public void DocumentedCommaSeparatedRequirementsCanExcludeSpecificVersion()
    {
        AssertMatches(
            "(,1.1),(1.1,)",
            new string[] { "1.0", "1.1-alpha", "1.1.1" },
            new string[] { "1.1" });
    }

    [Test]
    public void MavenQualifierOrderingAppliesWhenCheckingRanges()
    {
        AssertMatches(
            "[1.0-alpha,1.0)",
            new string[] { "1.0-beta", "1.0-rc1", "1.0-snapshot" },
            new string[] { "1.0", "1.0-sp" });
    }

    [Test]
    public void InvalidMavenRequirementsProduceFailures()
    {
        AssertFailure("(1.0)", MavenRangeDiagnosticCodes.InvalidRange);
        AssertFailure("[1.0,]", MavenRangeDiagnosticCodes.InvalidRange);
        AssertFailure("(,)", MavenRangeDiagnosticCodes.InvalidRange);
        AssertFailure("[2.0,1.0]", MavenRangeDiagnosticCodes.InvalidRange);
        AssertFailure("[bad version]", MavenRangeDiagnosticCodes.InvalidVersion);
    }

    static VersionRange ParseRange(string text)
    {
        VersionRangeParseResult result = MavenRangeParser.Parse(text);

        Assert.That(result.IsSuccess(), Is.True, text);
        Assert.That(result.HasRange(), Is.True, text);
        return result.GetRange();
    }

    static void AssertMatches(string rangeText, string[] matchingVersions, string[] rejectedVersions)
    {
        VersionRange range = ParseRange(rangeText);
        int index = 0;

        while (index < matchingVersions.Length)
        {
            Assert.That(
                range.ContainsText(matchingVersions[index], VersionDialect.Maven),
                Is.True,
                rangeText + " should contain " + matchingVersions[index]);
            index = index + 1;
        }

        index = 0;
        while (index < rejectedVersions.Length)
        {
            Assert.That(
                range.ContainsText(rejectedVersions[index], VersionDialect.Maven),
                Is.False,
                rangeText + " should not contain " + rejectedVersions[index]);
            index = index + 1;
        }
    }

    static void AssertFailure(string text, string expectedCode)
    {
        VersionRangeParseResult result = MavenRangeParser.Parse(text);

        Assert.That(result.IsFailure(), Is.True, text);
        Assert.That(result.HasRange(), Is.False, text);
        Assert.That(result.GetRangeSyntax(), Is.EqualTo(RangeSyntax.Maven), text);
        Assert.That(result.GetDiagnosticCount(), Is.EqualTo(1), text);
        Assert.That(result.GetDiagnostic(0).GetCode(), Is.EqualTo(expectedCode), text);
    }
}
