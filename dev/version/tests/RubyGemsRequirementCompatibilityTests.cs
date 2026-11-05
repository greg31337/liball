using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class RubyGemsRequirementCompatibilityTests
{
    [Test]
    public void DefaultRequirementUsesExactRubyGemsVersionEquality()
    {
        AssertMatches(
            "1.0",
            new string[] { "1", "1.0", "1.0.0" },
            new string[] { "1.0.1", "1.1" });
    }

    [Test]
    public void ComparisonRequirementsUseRubyGemsOrdering()
    {
        AssertMatches(
            ">= 2.0",
            new string[] { "2.0", "2.0.1", "3.0" },
            new string[] { "1.9.9", "2.0.a" });
        AssertMatches(
            "< 2.0",
            new string[] { "1.9.9", "2.0.a" },
            new string[] { "2.0", "2.0.1" });
        AssertMatches(
            "> 1.0.a",
            new string[] { "1.0.b", "1.0", "1.1" },
            new string[] { "1.0.a", "0.9.9" });
        AssertMatches(
            "<= 1.0",
            new string[] { "1.0.a", "1", "1.0.0" },
            new string[] { "1.0.1" });
    }

    [Test]
    public void NotEqualRequirementExcludesAllEquivalentRubyGemsVersions()
    {
        AssertMatches(
            "!= 1.0",
            new string[] { "1.0.a", "1.0.1", "2.0" },
            new string[] { "1", "1.0", "1.0.0" });
    }

    [Test]
    public void MultipleRequirementsMustAllBeSatisfied()
    {
        AssertMatches(
            ">= 1.0, < 2.0",
            new string[] { "1.0", "1.5", "1.9.9" },
            new string[] { "0.9.9", "1.0.a", "2.0" });
        AssertMatches(
            ">= 1.0, < 2.0, != 1.5",
            new string[] { "1.0", "1.4.9", "1.5.1" },
            new string[] { "1.0.a", "1.5", "2.0" });
    }

    [Test]
    public void PessimisticRequirementsMatchDocumentedGranularity()
    {
        AssertMatches(
            "~> 2.2",
            new string[] { "2.2", "2.9.9" },
            new string[] { "2.1.9", "3.0" });
        AssertMatches(
            "~> 2.2.0",
            new string[] { "2.2.0", "2.2.99" },
            new string[] { "2.1.9", "2.3.0" });
        AssertMatches(
            "~> 2.0.3",
            new string[] { "2.0.3", "2.0.99" },
            new string[] { "2.0.2", "2.1.0" });
    }

    [Test]
    public void PessimisticRequirementsCanBeCombinedWithExclusions()
    {
        AssertMatches(
            "~> 2.2, != 2.2.5",
            new string[] { "2.2", "2.2.4", "2.2.6", "2.9.9" },
            new string[] { "2.1.9", "2.2.5", "3.0" });
    }

    [Test]
    public void PrereleasePessimisticRequirementsUseRubyGemsBumpRules()
    {
        AssertMatches(
            "~> 2.0.0.a",
            new string[] { "2.0.0.a", "2.0.0.b", "2.0.0", "2.0.9" },
            new string[] { "1.9.9", "2.1.0" });
    }

    [Test]
    public void InvalidRubyGemsRequirementsProduceFailures()
    {
        AssertFailure("", RubyGemsRequirementDiagnosticCodes.MissingRequirement);
        AssertFailure("~", RubyGemsRequirementDiagnosticCodes.InvalidOperator);
        AssertFailure("~>", RubyGemsRequirementDiagnosticCodes.MissingVersion);
        AssertFailure(">= bad", RubyGemsRequirementDiagnosticCodes.InvalidVersion);
        AssertFailure(">= 1.0 < 2.0", RubyGemsRequirementDiagnosticCodes.InvalidOperator);
    }

    static VersionRange ParseRange(string text)
    {
        VersionRangeParseResult result = RubyGemsRangeParser.Parse(text);

        Assert.That(result.IsSuccess(), Is.True, text);
        Assert.That(result.HasRange(), Is.True, text);
        return result.GetRange();
    }

    static void AssertMatches(string requirementText, string[] matchingVersions, string[] rejectedVersions)
    {
        VersionRange range = ParseRange(requirementText);
        int index = 0;

        while (index < matchingVersions.Length)
        {
            Assert.That(
                range.ContainsText(matchingVersions[index], VersionDialect.RubyGems),
                Is.True,
                requirementText + " should contain " + matchingVersions[index]);
            index = index + 1;
        }

        index = 0;
        while (index < rejectedVersions.Length)
        {
            Assert.That(
                range.ContainsText(rejectedVersions[index], VersionDialect.RubyGems),
                Is.False,
                requirementText + " should not contain " + rejectedVersions[index]);
            index = index + 1;
        }
    }

    static void AssertFailure(string text, string expectedCode)
    {
        VersionRangeParseResult result = RubyGemsRangeParser.Parse(text);

        Assert.That(result.IsFailure(), Is.True, text);
        Assert.That(result.HasRange(), Is.False, text);
        Assert.That(result.GetRangeSyntax(), Is.EqualTo(RangeSyntax.RubyGems), text);
        Assert.That(result.GetDiagnosticCount(), Is.EqualTo(1), text);
        Assert.That(result.GetDiagnostic(0).GetCode(), Is.EqualTo(expectedCode), text);
    }
}
