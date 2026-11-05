using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class PythonSpecifierCompatibilityTests
{
    [Test]
    public void CompatibleReleaseSpecifiersMatchDocumentedUpperBounds()
    {
        AssertMatches(
            "~=2.2",
            new string[] { "2.2", "2.2.1", "2.9.9" },
            new string[] { "2.1.9", "3.0" });
        AssertMatches(
            "~=1.4.5",
            new string[] { "1.4.5", "1.4.99" },
            new string[] { "1.4.4", "1.5.0" });
        AssertMatches(
            "~=1.4.5.0",
            new string[] { "1.4.5", "1.4.5.9" },
            new string[] { "1.4.4", "1.4.6" });
    }

    [Test]
    public void ComparisonSpecifiersIntersectAcrossCommaSeparatedClauses()
    {
        AssertMatches(
            ">=1.0, <2.0",
            new string[] { "1.0", "1.5", "1.9.9" },
            new string[] { "0.9.9", "2.0" });
        AssertMatches(
            ">=1.0, <2.0, !=1.5",
            new string[] { "1.0", "1.4.9", "1.5.1" },
            new string[] { "0.9.9", "1.5", "2.0" });
    }

    [Test]
    public void EqualitySpecifiersUsePep440ReleasePadding()
    {
        AssertMatches(
            "==1.0",
            new string[] { "1", "1.0", "1.0.0" },
            new string[] { "1.0.1", "1.1" });
        AssertMatches(
            "!=1.0",
            new string[] { "1.0.1", "1.1" },
            new string[] { "1", "1.0", "1.0.0" });
    }

    [Test]
    public void PrefixMatchingSpecifiersMatchReleasePrefix()
    {
        AssertMatches(
            "==1.1.*",
            new string[] { "1.1", "1.1.0", "1.1.9", "1.1.post1" },
            new string[] { "1.0.9", "1.2.0" });
        AssertMatches(
            "!=1.1.*",
            new string[] { "1.0.9", "1.2.0" },
            new string[] { "1.1", "1.1.0", "1.1.9", "1.1.post1" });
    }

    [Test]
    public void DefaultPrereleasePolicyExcludesPrereleaseCandidates()
    {
        VersionRange range = ParseRange(">=1.0, <2.0");

        Assert.That(range.ContainsText("1.5", VersionDialect.Python), Is.True);
        Assert.That(range.ContainsText("1.5a1", VersionDialect.Python), Is.False);
        Assert.That(range.ContainsTextWithPrerelease("1.5a1", VersionDialect.Python, true), Is.True);
    }

    [Test]
    public void ExplicitPrereleaseSpecifierAllowsPrereleaseCandidatesByDefault()
    {
        VersionRange range = ParseRange(">=1.0rc1, <2.0");

        Assert.That(range.ContainsText("1.0rc1", VersionDialect.Python), Is.True);
        Assert.That(range.ContainsText("1.5a1", VersionDialect.Python), Is.True);
        Assert.That(range.ContainsText("1.5", VersionDialect.Python), Is.True);
        Assert.That(range.ContainsText("2.0a1", VersionDialect.Python), Is.True);
        Assert.That(range.ContainsText("2.0", VersionDialect.Python), Is.False);
    }

    [Test]
    public void EpochsAndPostReleasesParticipateInSpecifierMatching()
    {
        AssertMatches(
            ">=1!1.0",
            new string[] { "1!1.0", "1!1.0.post1" },
            new string[] { "2.0", "0!9.0" });
        AssertMatches(
            ">1.0",
            new string[] { "1.0.post1", "1.0.1" },
            new string[] { "1.0", "1.0rc1" });
    }

    [Test]
    public void InvalidPep440SpecifiersProduceFailures()
    {
        AssertFailure("", PythonSpecifierDiagnosticCodes.MissingSpecifier);
        AssertFailure("1.0", PythonSpecifierDiagnosticCodes.InvalidOperator);
        AssertFailure("~=1", PythonSpecifierDiagnosticCodes.InvalidVersion);
        AssertFailure("==1.*.2", PythonSpecifierDiagnosticCodes.InvalidVersion);
        AssertFailure(">=1.0,", PythonSpecifierDiagnosticCodes.MissingSpecifier);
    }

    static VersionRange ParseRange(string text)
    {
        VersionRangeParseResult result = PythonSpecifierParser.Parse(text);

        Assert.That(result.IsSuccess(), Is.True, text);
        Assert.That(result.HasRange(), Is.True, text);
        return result.GetRange();
    }

    static void AssertMatches(string specifierText, string[] matchingVersions, string[] rejectedVersions)
    {
        VersionRange range = ParseRange(specifierText);
        int index = 0;

        while (index < matchingVersions.Length)
        {
            Assert.That(
                range.ContainsText(matchingVersions[index], VersionDialect.Python),
                Is.True,
                specifierText + " should contain " + matchingVersions[index]);
            index = index + 1;
        }

        index = 0;
        while (index < rejectedVersions.Length)
        {
            Assert.That(
                range.ContainsText(rejectedVersions[index], VersionDialect.Python),
                Is.False,
                specifierText + " should not contain " + rejectedVersions[index]);
            index = index + 1;
        }
    }

    static void AssertFailure(string text, string expectedCode)
    {
        VersionRangeParseResult result = PythonSpecifierParser.Parse(text);

        Assert.That(result.IsFailure(), Is.True, text);
        Assert.That(result.HasRange(), Is.False, text);
        Assert.That(result.GetRangeSyntax(), Is.EqualTo(RangeSyntax.Python), text);
        Assert.That(result.GetDiagnosticCount(), Is.EqualTo(1), text);
        Assert.That(result.GetDiagnostic(0).GetCode(), Is.EqualTo(expectedCode), text);
    }
}
