using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class VersionRangeOperationsTests
{
    [Test]
    public void EmptyAndUniversalRanges_AreConstructedAndInspectedPublicly()
    {
        IVersionRangeOperations operations = new VersionRangeOperations();
        VersionRange empty = operations.CreateEmptyRange();
        VersionRange universal = operations.CreateUniversalRange();
        UniversalVersion stable = ParseVersion("1.2.3");

        Assert.That(operations.IsEmpty(empty), Is.True);
        Assert.That(operations.IsUniversal(empty), Is.False);
        Assert.That(empty.sets, Is.Empty);
        Assert.That(operations.ContainsVersion(empty, stable), Is.False);
        Assert.That(operations.IsEmpty(universal), Is.False);
        Assert.That(operations.IsUniversal(universal), Is.True);
        Assert.That(universal.sets.Length, Is.EqualTo(1));
        Assert.That(universal.sets[0].comparators, Is.Empty);
        Assert.That(operations.ContainsVersion(universal, stable), Is.True);
    }

    [Test]
    public void ParsedMembership_UsesComparatorSetsAndExplicitPrereleaseHandling()
    {
        IVersionRangeOperations operations = new VersionRangeOperations();
        VersionRange range = ParseRange(">=1.0.0 <2.0.0");
        UniversalVersion stable = ParseVersion("1.5.0");
        UniversalVersion prerelease = ParseVersion("1.5.0-alpha.1");
        UniversalVersion outside = ParseVersion("2.0.0");

        Assert.That(operations.ContainsVersion(range, stable), Is.True);
        Assert.That(operations.ContainsVersion(range, prerelease), Is.True);
        Assert.That(operations.ContainsVersion(range, outside), Is.False);
        Assert.That(operations.ContainsVersionWithPrerelease(range, prerelease, false), Is.False);
        Assert.That(operations.ContainsVersionWithPrerelease(range, prerelease, true), Is.True);
        Assert.That(operations.ContainsVersionWithPrerelease(range, stable, false), Is.True);
    }

    [Test]
    public void TextMembership_UsesParseOptionsAndAllowsAnExplicitOverride()
    {
        IVersionRangeOperations operations = new VersionRangeOperations();
        VersionRange range = ParseRange(">=1.0.0 <2.0.0");
        VersionParseOptions include = ParseOptions(PrereleasePolicy.Include);
        VersionParseOptions exclude = ParseOptions(PrereleasePolicy.Exclude);

        Assert.That(operations.ContainsText(range, "1.5.0", exclude), Is.True);
        Assert.That(operations.ContainsText(range, "2.0.0", include), Is.False);
        Assert.That(operations.ContainsText(range, "not-a-version", include), Is.False);
        Assert.That(operations.ContainsText(range, "1.5.0-alpha.1", exclude), Is.False);
        Assert.That(operations.ContainsText(range, "1.5.0-alpha.1", include), Is.True);
        Assert.That(
            operations.ContainsTextWithPrerelease(range, "1.5.0-alpha.1", exclude, true),
            Is.True);
        Assert.That(
            operations.ContainsTextWithPrerelease(range, "1.5.0-alpha.1", include, false),
            Is.False);
    }

    [Test]
    public void MatchExplanations_ExposeSuccessAndFailedComparatorData()
    {
        IVersionRangeOperations operations = new VersionRangeOperations();
        VersionRange range = ParseRange(">=1.0.0 <2.0.0");
        UniversalVersion accepted = ParseVersion("1.5.0");
        UniversalVersion rejected = ParseVersion("2.0.0");

        VersionMatchExplanation match = operations.ExplainMatch(range, accepted);
        VersionMatchExplanation failure = operations.ExplainMatch(range, rejected);

        Assert.That(match.match, Is.True);
        Assert.That(match.version, Is.SameAs(accepted));
        Assert.That(match.matchedSetIndex, Is.EqualTo(0));
        Assert.That(match.failedSetIndex, Is.EqualTo(-1));
        Assert.That(match.failedComparator, Is.Null);
        Assert.That(match.message, Is.EqualTo("Version satisfies range."));
        Assert.That(failure.match, Is.False);
        Assert.That(failure.version, Is.SameAs(rejected));
        Assert.That(failure.matchedSetIndex, Is.EqualTo(-1));
        Assert.That(failure.failedSetIndex, Is.EqualTo(0));
        Assert.That(failure.failedComparator, Is.Not.Null);
        Assert.That(failure.failedComparator!.rangeOperator, Is.EqualTo(RangeOperator.LessThan));
        Assert.That(failure.message, Is.EqualTo("Version does not satisfy comparator < 2.0.0."));
    }

    [Test]
    public void PrereleaseExplanation_DistinguishesGatingFromComparatorFailure()
    {
        IVersionRangeOperations operations = new VersionRangeOperations();
        VersionRange range = ParseRange(">=1.0.0 <2.0.0");
        UniversalVersion prerelease = ParseVersion("1.5.0-alpha.1");

        VersionMatchExplanation raw = operations.ExplainMatch(range, prerelease);
        VersionMatchExplanation excluded = operations.ExplainMatchWithPrerelease(range, prerelease, false);
        VersionMatchExplanation included = operations.ExplainMatchWithPrerelease(range, prerelease, true);

        Assert.That(raw.match, Is.True);
        Assert.That(excluded.match, Is.False);
        Assert.That(excluded.failedSetIndex, Is.EqualTo(-1));
        Assert.That(excluded.failedComparator, Is.Null);
        Assert.That(excluded.message, Is.EqualTo("Prerelease version is not admitted by range."));
        Assert.That(included.match, Is.True);
        Assert.That(included.matchedSetIndex, Is.EqualTo(0));
    }

    static UniversalVersion ParseVersion(string text)
    {
        IVersionParsing parsing = new VersionService();
        return parsing.Parse(text, ParseOptions(PrereleasePolicy.Include));
    }

    static VersionRange ParseRange(string text)
    {
        IVersionRangeParsing parsing = new VersionService();
        VersionRangeParseOptions options = new VersionRangeParseOptions(
            RangeSyntax.NodeSemVer,
            PrereleasePolicy.Default,
            4096,
            true,
            true,
            true,
            true,
            true);

        return parsing.ParseRange(text, options);
    }

    static VersionParseOptions ParseOptions(PrereleasePolicy prereleasePolicy)
    {
        return new VersionParseOptions(
            VersionDialect.SemVer,
            VersionParseMode.Strict,
            4096,
            true,
            true,
            prereleasePolicy);
    }
}
