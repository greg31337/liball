using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class PublicVersionRangeFormattingCompletenessTests
{
    [TestCase(RangeSyntax.NodeSemVer, ">=0.0.0 <0.0.0", "*")]
    [TestCase(RangeSyntax.Cargo, ">=0.0.0, <0.0.0", "*")]
    [TestCase(RangeSyntax.NuGet, "[0.0.0,0.0.0)", "*")]
    [TestCase(RangeSyntax.Maven, "[0,0)", "0")]
    [TestCase(RangeSyntax.RubyGems, ">= 0, < 0", ">= 0")]
    [TestCase(RangeSyntax.Python, ">=0,<0", ">=0")]
    public void EmptyAndUniversalRanges_HaveCanonicalNormalizedText(
        RangeSyntax syntax,
        string emptyText,
        string universalText)
    {
        IVersionRangeOperations operations = new VersionRangeOperations();

        Assert.That(
            operations.FormatNormalizedRange(operations.CreateEmptyRange(), syntax),
            Is.EqualTo(emptyText));
        Assert.That(
            operations.FormatNormalizedRange(operations.CreateUniversalRange(), syntax),
            Is.EqualTo(universalText));
    }

    [TestCase(RangeSyntax.NodeSemVer, ">=  v1.2.3   < v2.0.0", ">=1.2.3 <2.0.0")]
    [TestCase(RangeSyntax.Cargo, ">=1.2, <2", ">=1.2.0, <2.0.0")]
    [TestCase(RangeSyntax.NuGet, "[01.002,03.004)", "[1.2.0,3.4.0)")]
    [TestCase(RangeSyntax.Maven, "[1.0-final,2.0)", "[1,2)")]
    [TestCase(RangeSyntax.RubyGems, ">= 01.002, < 03.000", ">= 1.2, < 3.0")]
    [TestCase(RangeSyntax.Python, ">=1.0RC1,<2.0", ">=1.0rc1, <2.0")]
    public void NormalizedOutput_UsesDialectNativeVersionFormatting(
        RangeSyntax syntax,
        string sourceText,
        string expectedText)
    {
        IVersionRangeOperations operations = new VersionRangeOperations();
        VersionRange range = ParseRange(syntax, sourceText);

        Assert.That(operations.FormatNormalizedRange(range, syntax), Is.EqualTo(expectedText));
    }

    [TestCase(RangeSyntax.NodeSemVer, VersionDialect.NodeSemVer, "^1.2.3 || >=3.0.0 <4.0.0")]
    [TestCase(RangeSyntax.Cargo, VersionDialect.Cargo, "1.2.*")]
    [TestCase(RangeSyntax.NuGet, VersionDialect.NuGet, "1.2.*-*")]
    [TestCase(RangeSyntax.Maven, VersionDialect.Maven, "(,1.0],[2.0,3.0)")]
    [TestCase(RangeSyntax.RubyGems, VersionDialect.RubyGems, "~> 1.2, != 1.5")]
    [TestCase(RangeSyntax.Python, VersionDialect.Python, "!=1.2.*")]
    public void ParseFormatParse_PreservesMembershipAcrossEverySupportedSyntax(
        RangeSyntax syntax,
        VersionDialect dialect,
        string sourceText)
    {
        IVersionRangeOperations operations = new VersionRangeOperations();
        VersionRange original = ParseRange(syntax, sourceText);
        string normalized = operations.FormatNormalizedRange(original, syntax);
        VersionRange reparsed = ParseRange(syntax, normalized);
        string[] candidates = FixedCandidateTexts();
        VersionParseOptions options = VersionOptions(dialect);
        int index = 0;

        while (index < candidates.Length)
        {
            Assert.That(
                operations.ContainsText(reparsed, candidates[index], options),
                Is.EqualTo(operations.ContainsText(original, candidates[index], options)),
                syntax + ": " + normalized + ", candidate " + candidates[index]);
            index = index + 1;
        }
    }

    [TestCase(RangeSyntax.NodeSemVer, ">=1.0.0 >=2.0.0 <5.0.0 <4.0.0", ">=2.0.0 <4.0.0")]
    [TestCase(RangeSyntax.Cargo, ">=1.0.0, >=2.0.0, <5.0.0, <4.0.0", ">=2.0.0, <4.0.0")]
    [TestCase(RangeSyntax.RubyGems, ">= 1.0, >= 2.0, < 5.0, < 4.0", ">= 2.0, < 4.0")]
    [TestCase(RangeSyntax.Python, ">=1.0,>=2.0,<5.0,<4.0", ">=2.0, <4.0")]
    public void NormalizedOutput_RemovesRedundantComparators(
        RangeSyntax syntax,
        string sourceText,
        string expectedText)
    {
        IVersionRangeOperations operations = new VersionRangeOperations();
        VersionRange range = ParseRange(syntax, sourceText);

        Assert.That(operations.FormatNormalizedRange(range, syntax), Is.EqualTo(expectedText));
    }

    [TestCase(RangeSyntax.NuGet, "[1.0,5.0)", "[2.0,4.0)", "[2.0.0,4.0.0)")]
    [TestCase(RangeSyntax.Maven, "[1.0,5.0)", "[2.0,4.0)", "[2,4)")]
    public void NormalizedIntervals_RemoveRedundantBoundsCreatedByIntersection(
        RangeSyntax syntax,
        string leftText,
        string rightText,
        string expectedText)
    {
        IVersionRangeOperations operations = new VersionRangeOperations();
        VersionRange intersection = operations.IntersectRanges(
            ParseRange(syntax, leftText),
            ParseRange(syntax, rightText));

        Assert.That(
            operations.FormatNormalizedRange(intersection, syntax),
            Is.EqualTo(expectedText));
    }

    [TestCase(RangeSyntax.NodeSemVer, "1.2.x", ">=1.2.0 <1.3.0-0")]
    [TestCase(RangeSyntax.Cargo, "1.2.*", ">=1.2.0, <1.3.0")]
    [TestCase(RangeSyntax.NuGet, "01.002.*-*", "1.2.*-*")]
    [TestCase(RangeSyntax.Python, "==01.002.*", ">=1.2, <1.3")]
    [TestCase(RangeSyntax.Python, "!=01.002.*", "!=1.2.*")]
    public void FloatingAndWildcardForms_ProduceNormalizedDialectSpecificText(
        RangeSyntax syntax,
        string sourceText,
        string expectedText)
    {
        IVersionRangeOperations operations = new VersionRangeOperations();
        VersionRange range = ParseRange(syntax, sourceText);

        Assert.That(operations.FormatNormalizedRange(range, syntax), Is.EqualTo(expectedText));
    }

    [TestCase(RangeSyntax.NodeSemVer, VersionDialect.NodeSemVer, "=1.2.3", "1.2.3")]
    [TestCase(RangeSyntax.Cargo, VersionDialect.Cargo, "=1.2.3", "1.2.3")]
    [TestCase(RangeSyntax.NuGet, VersionDialect.NuGet, "[1.2.3]", "1.2.3")]
    [TestCase(RangeSyntax.Maven, VersionDialect.Maven, "[1.2.3]", "1.2.3")]
    [TestCase(RangeSyntax.RubyGems, VersionDialect.RubyGems, "= 1.2.3", "1.2.3")]
    [TestCase(RangeSyntax.Python, VersionDialect.Python, "==1.2.3", "1.2.3")]
    public void ExactRanges_ReturnRepresentedMinimaAcrossEverySyntax(
        RangeSyntax syntax,
        VersionDialect dialect,
        string sourceText,
        string expectedText)
    {
        AssertMinimum(syntax, dialect, ParseRange(syntax, sourceText), expectedText);
    }

    [TestCase(RangeSyntax.NodeSemVer, VersionDialect.NodeSemVer, "<2.0.0", "0.0.0")]
    [TestCase(RangeSyntax.Cargo, VersionDialect.Cargo, "<2.0.0", "0.0.0")]
    [TestCase(RangeSyntax.NuGet, VersionDialect.NuGet, "(,2.0]", "0.0.0")]
    [TestCase(RangeSyntax.Maven, VersionDialect.Maven, "(,2.0]", "0")]
    [TestCase(RangeSyntax.RubyGems, VersionDialect.RubyGems, "< 2.0", "0")]
    [TestCase(RangeSyntax.Python, VersionDialect.Python, "<2.0", "0")]
    public void UpperBoundedRanges_ReturnDialectMinimums(
        RangeSyntax syntax,
        VersionDialect dialect,
        string sourceText,
        string expectedText)
    {
        AssertMinimum(syntax, dialect, ParseRange(syntax, sourceText), expectedText);
    }

    [TestCase(RangeSyntax.NodeSemVer, VersionDialect.NodeSemVer, ">1.2.3 <2.0.0", "1.2.4")]
    [TestCase(RangeSyntax.Cargo, VersionDialect.Cargo, ">1.2.3, <2.0.0", "1.2.4")]
    [TestCase(RangeSyntax.NuGet, VersionDialect.NuGet, "(1.2.3,2.0.0]", "1.2.3.1")]
    [TestCase(RangeSyntax.NodeSemVer, VersionDialect.NodeSemVer, ">1.0.0-alpha <1.0.0", "1.0.0-alpha.0")]
    public void StrictDiscreteBounds_ReturnTheNextRepresentedVersion(
        RangeSyntax syntax,
        VersionDialect dialect,
        string sourceText,
        string expectedText)
    {
        AssertMinimum(syntax, dialect, ParseRange(syntax, sourceText), expectedText);
    }

    [TestCase(RangeSyntax.Maven, "(1.0,2.0]")]
    [TestCase(RangeSyntax.RubyGems, "> 1.0")]
    [TestCase(RangeSyntax.Python, ">1.0")]
    [TestCase(RangeSyntax.RubyGems, "!= 0")]
    [TestCase(RangeSyntax.Python, "!=0")]
    public void DenseOrExcludedLowerInfima_ReturnExplicitNoValue(
        RangeSyntax syntax,
        string sourceText)
    {
        AssertNoMinimum(syntax, ParseRange(syntax, sourceText));
    }

    [TestCase(RangeSyntax.NodeSemVer)]
    [TestCase(RangeSyntax.Cargo)]
    [TestCase(RangeSyntax.NuGet)]
    [TestCase(RangeSyntax.Maven)]
    [TestCase(RangeSyntax.RubyGems)]
    [TestCase(RangeSyntax.Python)]
    public void EmptyAndContradictoryRanges_ReturnExplicitNoValue(RangeSyntax syntax)
    {
        IVersionRangeOperations operations = new VersionRangeOperations();
        VersionRange empty = operations.CreateEmptyRange();
        VersionRange contradiction = operations.IntersectRanges(
            ParseRange(syntax, LowerRangeText(syntax)),
            ParseRange(syntax, UpperRangeText(syntax)));

        AssertNoMinimum(syntax, empty);
        AssertNoMinimum(syntax, contradiction);
    }

    [TestCase(RangeSyntax.NodeSemVer, VersionDialect.NodeSemVer, "0.0.0")]
    [TestCase(RangeSyntax.Cargo, VersionDialect.Cargo, "0.0.0")]
    [TestCase(RangeSyntax.NuGet, VersionDialect.NuGet, "0.0.0")]
    [TestCase(RangeSyntax.Maven, VersionDialect.Maven, "0")]
    [TestCase(RangeSyntax.RubyGems, VersionDialect.RubyGems, "0")]
    [TestCase(RangeSyntax.Python, VersionDialect.Python, "0")]
    public void UniversalRanges_ReturnEveryDialectMinimum(
        RangeSyntax syntax,
        VersionDialect dialect,
        string expectedText)
    {
        IVersionRangeOperations operations = new VersionRangeOperations();
        AssertMinimum(syntax, dialect, operations.CreateUniversalRange(), expectedText);
    }

    [Test]
    public void DisjunctionMinimum_SelectsTheLowestRepresentedBranch()
    {
        AssertMinimum(
            RangeSyntax.NodeSemVer,
            VersionDialect.NodeSemVer,
            ParseRange(RangeSyntax.NodeSemVer, ">=3.0.0 || >=1.0.0"),
            "1.0.0");
        AssertMinimum(
            RangeSyntax.Maven,
            VersionDialect.Maven,
            ParseRange(RangeSyntax.Maven, "(2.0,3.0],[0,1.0]"),
            "0");
    }

    static void AssertMinimum(
        RangeSyntax syntax,
        VersionDialect dialect,
        VersionRange range,
        string expectedText)
    {
        IVersionRangeOperations operations = new VersionRangeOperations();
        IVersionFormatting formatting = new VersionService();

        VersionRangeMinimumResult result = operations.GetMinimumPossibleVersion(range, syntax);

        Assert.That(result.hasValue, Is.True);
        Assert.That(result.version, Is.Not.Null);
        Assert.That(result.version!.dialect, Is.EqualTo(dialect));
        Assert.That(formatting.FormatNormalized(result.version), Is.EqualTo(expectedText));
        Assert.That(operations.ContainsVersion(range, result.version), Is.True);
    }

    static void AssertNoMinimum(RangeSyntax syntax, VersionRange range)
    {
        IVersionRangeOperations operations = new VersionRangeOperations();

        VersionRangeMinimumResult result = operations.GetMinimumPossibleVersion(range, syntax);

        Assert.That(result.hasValue, Is.False);
        Assert.That(result.version, Is.Null);
    }

    static string LowerRangeText(RangeSyntax syntax)
    {
        if (syntax == RangeSyntax.NodeSemVer)
        {
            return ">=2.0.0";
        }

        if (syntax == RangeSyntax.Cargo)
        {
            return ">=2.0.0";
        }

        if (syntax == RangeSyntax.NuGet || syntax == RangeSyntax.Maven)
        {
            return "[2.0,)";
        }

        if (syntax == RangeSyntax.RubyGems)
        {
            return ">= 2.0";
        }

        return ">=2.0";
    }

    static string UpperRangeText(RangeSyntax syntax)
    {
        if (syntax == RangeSyntax.NodeSemVer)
        {
            return "<2.0.0";
        }

        if (syntax == RangeSyntax.Cargo)
        {
            return "<2.0.0";
        }

        if (syntax == RangeSyntax.NuGet || syntax == RangeSyntax.Maven)
        {
            return "(,2.0)";
        }

        if (syntax == RangeSyntax.RubyGems)
        {
            return "< 2.0";
        }

        return "<2.0";
    }

    static string[] FixedCandidateTexts()
    {
        return new string[]
        {
            "0.9.9",
            "1.0",
            "1.2",
            "1.2.5",
            "1.5",
            "2.0",
            "2.5",
            "3.0",
            "3.5",
            "4.0"
        };
    }

    static VersionRange ParseRange(RangeSyntax syntax, string text)
    {
        IVersionRangeParsing parsing = new VersionService();
        return parsing.ParseRange(text, RangeOptions(syntax));
    }

    static VersionRangeParseOptions RangeOptions(RangeSyntax syntax)
    {
        return new VersionRangeParseOptions(
            syntax,
            PrereleasePolicy.Default,
            4096,
            true,
            true,
            true,
            true,
            true);
    }

    static VersionParseOptions VersionOptions(VersionDialect dialect)
    {
        return new VersionParseOptions(
            dialect,
            VersionParseMode.DialectSpecific,
            4096,
            true,
            true,
            PrereleasePolicy.Include);
    }
}
