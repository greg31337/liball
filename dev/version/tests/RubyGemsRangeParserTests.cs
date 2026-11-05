using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class RubyGemsRangeParserTests
{
    [Test]
    public void Parse_BareVersionDefaultsToExactRequirement()
    {
        VersionRange range = ParseRange("1.0");

        Assert.That(range.GetSetCount(), Is.EqualTo(1));
        Assert.That(range.GetSet(0).GetComparatorCount(), Is.EqualTo(1));
        Assert.That(range.GetSet(0).GetComparator(0).GetOperator(), Is.EqualTo(RangeOperator.Equal));
        Assert.That(range.ContainsText("1", VersionDialect.RubyGems), Is.True);
        Assert.That(range.ContainsText("1.0.0", VersionDialect.RubyGems), Is.True);
        Assert.That(range.ContainsText("1.0.1", VersionDialect.RubyGems), Is.False);
    }

    [Test]
    public void Parse_ComparisonOperatorsMatchRubyGemsOrdering()
    {
        AssertMatches("> 1.0", new string[] { "1.0.1", "2.0" }, new string[] { "1.0", "1.0.a" });
        AssertMatches(">= 1.0", new string[] { "1.0", "1.0.1" }, new string[] { "1.0.a" });
        AssertMatches("< 2.0", new string[] { "1.9.9", "2.0.a" }, new string[] { "2.0", "2.0.1" });
        AssertMatches("<= 2.0", new string[] { "1.9.9", "2.0.a", "2.0" }, new string[] { "2.0.1" });
        AssertMatches("= 1.0", new string[] { "1", "1.0.0" }, new string[] { "1.0.1" });
        AssertMatches("!= 1.0", new string[] { "1.0.1", "1.0.a" }, new string[] { "1", "1.0.0" });
    }

    [Test]
    public void Parse_CommaSeparatedRequirementsCreateIntersection()
    {
        VersionRange range = ParseRange(">= 1.0, < 2.0, != 1.5");

        Assert.That(range.GetSetCount(), Is.EqualTo(1));
        Assert.That(range.GetSet(0).GetComparatorCount(), Is.EqualTo(3));
        Assert.That(range.ContainsText("1.0", VersionDialect.RubyGems), Is.True);
        Assert.That(range.ContainsText("1.4.9", VersionDialect.RubyGems), Is.True);
        Assert.That(range.ContainsText("1.5", VersionDialect.RubyGems), Is.False);
        Assert.That(range.ContainsText("2.0", VersionDialect.RubyGems), Is.False);
    }

    [Test]
    public void Parse_PessimisticRequirementUsesMajorUpperBoundForTwoSegments()
    {
        VersionRange range = ParseRange("~> 2.2");

        Assert.That(range.GetSetCount(), Is.EqualTo(1));
        Assert.That(range.GetSet(0).GetComparatorCount(), Is.EqualTo(2));
        Assert.That(range.ContainsText("2.1.9", VersionDialect.RubyGems), Is.False);
        Assert.That(range.ContainsText("2.2", VersionDialect.RubyGems), Is.True);
        Assert.That(range.ContainsText("2.9.9", VersionDialect.RubyGems), Is.True);
        Assert.That(range.ContainsText("3.0", VersionDialect.RubyGems), Is.False);
    }

    [Test]
    public void Parse_PessimisticRequirementUsesMinorUpperBoundForThreeSegments()
    {
        VersionRange range = ParseRange("~> 2.2.0");

        Assert.That(range.ContainsText("2.1.9", VersionDialect.RubyGems), Is.False);
        Assert.That(range.ContainsText("2.2.0", VersionDialect.RubyGems), Is.True);
        Assert.That(range.ContainsText("2.2.99", VersionDialect.RubyGems), Is.True);
        Assert.That(range.ContainsText("2.3.0", VersionDialect.RubyGems), Is.False);
    }

    [Test]
    public void Parse_PessimisticRequirementExtendsSingleSegmentToNextMajor()
    {
        VersionRange range = ParseRange("~> 3");

        Assert.That(range.ContainsText("2.9.9", VersionDialect.RubyGems), Is.False);
        Assert.That(range.ContainsText("3", VersionDialect.RubyGems), Is.True);
        Assert.That(range.ContainsText("3.9.9", VersionDialect.RubyGems), Is.True);
        Assert.That(range.ContainsText("4.0", VersionDialect.RubyGems), Is.False);
    }

    [Test]
    public void Parse_PessimisticRequirementIgnoresPrereleaseSegmentsForUpperBound()
    {
        VersionRange range = ParseRange("~> 2.0.0.a");

        Assert.That(range.ContainsText("2.0.0.a", VersionDialect.RubyGems), Is.True);
        Assert.That(range.ContainsText("2.0.0", VersionDialect.RubyGems), Is.True);
        Assert.That(range.ContainsText("2.0.9", VersionDialect.RubyGems), Is.True);
        Assert.That(range.ContainsText("2.1.0", VersionDialect.RubyGems), Is.False);
    }

    [Test]
    public void Parse_PessimisticRequirementIntersectsWithOtherRequirements()
    {
        VersionRange range = ParseRange("~> 2.2, != 2.2.5");

        Assert.That(range.GetSetCount(), Is.EqualTo(1));
        Assert.That(range.GetSet(0).GetComparatorCount(), Is.EqualTo(3));
        Assert.That(range.ContainsText("2.2.4", VersionDialect.RubyGems), Is.True);
        Assert.That(range.ContainsText("2.2.5", VersionDialect.RubyGems), Is.False);
        Assert.That(range.ContainsText("2.9.9", VersionDialect.RubyGems), Is.True);
        Assert.That(range.ContainsText("3.0", VersionDialect.RubyGems), Is.False);
    }

    [Test]
    public void Parse_AllowsWhitespaceAroundRequirements()
    {
        VersionRange range = ParseRange("  ~>   1.0  ,   !=  1.5  ");

        Assert.That(range.ContainsText("1.0", VersionDialect.RubyGems), Is.True);
        Assert.That(range.ContainsText("1.5", VersionDialect.RubyGems), Is.False);
        Assert.That(range.ContainsText("1.9.9", VersionDialect.RubyGems), Is.True);
        Assert.That(range.ContainsText("2.0", VersionDialect.RubyGems), Is.False);
    }

    [Test]
    public void Parse_ResultCarriesRubyGemsSyntaxAndRange()
    {
        VersionRangeParseResult result = RubyGemsRangeParser.Parse(">= 1.0");

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.GetRangeSyntax(), Is.EqualTo(RangeSyntax.RubyGems));
        Assert.That(result.HasRange(), Is.True);
        Assert.That(result.GetRange().ContainsText("1.0", VersionDialect.RubyGems), Is.True);
    }

    [Test]
    public void Parse_RejectsInvalidRequirements()
    {
        AssertFailure("", RubyGemsRequirementDiagnosticCodes.MissingRequirement);
        AssertFailure("   ", RubyGemsRequirementDiagnosticCodes.MissingRequirement);
        AssertFailure("!", RubyGemsRequirementDiagnosticCodes.InvalidOperator);
        AssertFailure("~", RubyGemsRequirementDiagnosticCodes.InvalidOperator);
        AssertFailure("~>", RubyGemsRequirementDiagnosticCodes.MissingVersion);
        AssertFailure("~> bad", RubyGemsRequirementDiagnosticCodes.InvalidVersion);
        AssertFailure(">=", RubyGemsRequirementDiagnosticCodes.MissingVersion);
        AssertFailure(">= bad", RubyGemsRequirementDiagnosticCodes.InvalidVersion);
        AssertFailure(">= 1.0 < 2.0", RubyGemsRequirementDiagnosticCodes.InvalidOperator);
        AssertFailure(">= 1.0,", RubyGemsRequirementDiagnosticCodes.MissingRequirement);
        AssertFailure(">= 1.0,, < 2.0", RubyGemsRequirementDiagnosticCodes.MissingRequirement);
    }

    [Test]
    public void FailureResult_DoesNotExposeRange()
    {
        VersionRangeParseResult result = RubyGemsRangeParser.Parse(">= bad");

        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.HasRange(), Is.False);
        Assert.That(result.GetRangeSyntax(), Is.EqualTo(RangeSyntax.RubyGems));
        Assert.That(
            delegate
            {
                result.GetRange();
            },
            Throws.TypeOf<System.InvalidOperationException>());
    }

    [Test]
    public void Parse_RejectsNullText()
    {
        Assert.That(
            delegate
            {
                RubyGemsRangeParser.Parse(null!);
            },
            Throws.TypeOf<System.ArgumentNullException>());
    }

    static VersionRange ParseRange(string text)
    {
        VersionRangeParseResult result = RubyGemsRangeParser.Parse(text);

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
                range.ContainsText(matchingVersions[index], VersionDialect.RubyGems),
                Is.True,
                rangeText + " should contain " + matchingVersions[index]);
            index = index + 1;
        }

        index = 0;
        while (index < rejectedVersions.Length)
        {
            Assert.That(
                range.ContainsText(rejectedVersions[index], VersionDialect.RubyGems),
                Is.False,
                rangeText + " should not contain " + rejectedVersions[index]);
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
