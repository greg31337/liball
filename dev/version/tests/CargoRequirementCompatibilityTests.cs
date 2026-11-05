using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class CargoRequirementCompatibilityTests
{
    [Test]
    public void DocumentedCaretAndDefaultRequirementsMatchCargoCompatibility()
    {
        AssertMatches(
            "1.2.3",
            new string[] { "1.2.3", "1.5.0" },
            new string[] { "1.2.2", "2.0.0" });
        AssertMatches(
            "^1.2.3",
            new string[] { "1.2.3", "1.9.9" },
            new string[] { "1.2.2", "2.0.0" });
        AssertMatches(
            "0.2.3",
            new string[] { "0.2.3", "0.2.99" },
            new string[] { "0.2.2", "0.3.0" });
        AssertMatches(
            "0.0.3",
            new string[] { "0.0.3" },
            new string[] { "0.0.2", "0.0.4" });
    }

    [Test]
    public void DocumentedPartialDefaultsMatchCargoCompatibility()
    {
        AssertMatches(
            "1.2",
            new string[] { "1.2.0", "1.9.9" },
            new string[] { "1.1.9", "2.0.0" });
        AssertMatches(
            "1",
            new string[] { "1.0.0", "1.9.9" },
            new string[] { "0.9.9", "2.0.0" });
        AssertMatches(
            "0.0",
            new string[] { "0.0.0", "0.0.99" },
            new string[] { "0.1.0", "1.0.0" });
        AssertMatches(
            "0",
            new string[] { "0.0.0", "0.9.9" },
            new string[] { "1.0.0" });
    }

    [Test]
    public void DocumentedTildeRequirementsMatchCargoCompatibility()
    {
        AssertMatches(
            "~1.2.3",
            new string[] { "1.2.3", "1.2.99" },
            new string[] { "1.2.2", "1.3.0" });
        AssertMatches(
            "~1.2",
            new string[] { "1.2.0", "1.2.99" },
            new string[] { "1.1.9", "1.3.0" });
        AssertMatches(
            "~1",
            new string[] { "1.0.0", "1.9.9" },
            new string[] { "0.9.9", "2.0.0" });
    }

    [Test]
    public void DocumentedWildcardRequirementsMatchCargoCompatibility()
    {
        AssertMatches(
            "*",
            new string[] { "0.0.0", "999.0.0" },
            new string[] { });
        AssertMatches(
            "1.*",
            new string[] { "1.0.0", "1.9.9" },
            new string[] { "0.9.9", "2.0.0" });
        AssertMatches(
            "1.2.*",
            new string[] { "1.2.0", "1.2.99" },
            new string[] { "1.1.9", "1.3.0" });
        AssertMatches(
            "1.X",
            new string[] { "1.0.0", "1.9.9" },
            new string[] { "0.9.9", "2.0.0" });
    }

    [Test]
    public void DocumentedComparisonRequirementsMatchCargoCompatibility()
    {
        AssertMatches(
            ">=1.2.0",
            new string[] { "1.2.0", "2.0.0" },
            new string[] { "1.1.9" });
        AssertMatches(
            ">1",
            new string[] { "2.0.0", "3.1.0" },
            new string[] { "1.0.0", "1.9.9" });
        AssertMatches(
            "<2",
            new string[] { "0.1.0", "1.9.9" },
            new string[] { "2.0.0" });
        AssertMatches(
            "=1.2.3",
            new string[] { "1.2.3", "1.2.3+build.7" },
            new string[] { "1.2.2", "1.2.4" });
    }

    [Test]
    public void DocumentedCommaSeparatedRequirementsRequireEveryClause()
    {
        AssertMatches(
            ">=1.2, <1.5",
            new string[] { "1.2.0", "1.4.99" },
            new string[] { "1.1.9", "1.5.0" });
        AssertMatches(
            "1.2.3, <1.4.0",
            new string[] { "1.2.3", "1.3.9" },
            new string[] { "1.2.2", "1.4.0", "2.0.0" });
    }

    [Test]
    public void PrereleasesOnlyMatchByDefaultWhenRequirementNamesSameCore()
    {
        AssertMatches(
            "1.0",
            new string[] { "1.0.0", "1.5.0" },
            new string[] { "1.0.0-alpha", "1.5.0-alpha" });
        AssertMatches(
            "1.0.0-alpha",
            new string[] { "1.0.0-alpha.1", "1.0.0-beta", "1.0.0", "1.5.0" },
            new string[] { "1.0.1-alpha", "2.0.0-alpha" });
    }

    [Test]
    public void IncludePrereleaseOverrideAllowsMatchingPrereleaseCandidates()
    {
        VersionRange range = ParseRange("1.0");

        Assert.That(range.ContainsText("1.5.0-alpha", VersionDialect.Cargo), Is.False);
        Assert.That(range.ContainsTextWithPrerelease("1.5.0-alpha", VersionDialect.Cargo, true), Is.True);
    }

    [Test]
    public void BuildMetadataIsIgnoredForCargoRequirementMatching()
    {
        AssertMatches(
            "=1.2.3+ignored",
            new string[] { "1.2.3", "1.2.3+different" },
            new string[] { "1.2.4" });
        AssertMatches(
            "1.2.3+ignored",
            new string[] { "1.2.3", "1.2.3+different", "1.9.9" },
            new string[] { "1.2.2", "2.0.0" });
    }

    [Test]
    public void InvalidCargoRequirementsProduceFailures()
    {
        AssertFailure("!=1.2.3", CargoRangeDiagnosticCodes.InvalidOperator);
        AssertFailure("1.*.*", CargoRangeDiagnosticCodes.InvalidVersion);
        AssertFailure("1.2.*-alpha", CargoRangeDiagnosticCodes.InvalidVersion);
        AssertFailure("~1.*", CargoRangeDiagnosticCodes.InvalidVersion);
        AssertFailure(">=1.*", CargoRangeDiagnosticCodes.InvalidVersion);
        AssertFailure("1.2.3 || 2.0.0", CargoRangeDiagnosticCodes.InvalidOperator);
    }

    static VersionRange ParseRange(string text)
    {
        VersionRangeParseResult result = CargoRangeParser.Parse(text);

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
                range.ContainsText(matchingVersions[index], VersionDialect.Cargo),
                Is.True,
                rangeText + " should contain " + matchingVersions[index]);
            index = index + 1;
        }

        index = 0;
        while (index < rejectedVersions.Length)
        {
            Assert.That(
                range.ContainsText(rejectedVersions[index], VersionDialect.Cargo),
                Is.False,
                rangeText + " should not contain " + rejectedVersions[index]);
            index = index + 1;
        }
    }

    static void AssertFailure(string text, string expectedCode)
    {
        VersionRangeParseResult result = CargoRangeParser.Parse(text);

        Assert.That(result.IsFailure(), Is.True, text);
        Assert.That(result.HasRange(), Is.False, text);
        Assert.That(result.GetRangeSyntax(), Is.EqualTo(RangeSyntax.Cargo), text);
        Assert.That(result.GetDiagnosticCount(), Is.EqualTo(1), text);
        Assert.That(result.GetDiagnostic(0).GetCode(), Is.EqualTo(expectedCode), text);
    }
}
