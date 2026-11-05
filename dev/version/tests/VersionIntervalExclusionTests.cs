using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class VersionIntervalExclusionTests
{
    [Test]
    public void ContainsVersionExcept_RemovesInteriorVersionFromInterval()
    {
        VersionInterval interval = VersionInterval.CreateBounded(
            BoundKind.Inclusive,
            ParseVersion("1.0.0"),
            BoundKind.Exclusive,
            ParseVersion("2.0.0"),
            VersionComparisonMode.Precedence);
        UniversalVersion excluded = ParseVersion("1.5.0");

        Assert.That(interval.ContainsExclusionHole(excluded), Is.True);
        Assert.That(interval.ContainsVersionExcept(ParseVersion("1.4.9"), excluded), Is.True);
        Assert.That(interval.ContainsVersionExcept(ParseVersion("1.5.0"), excluded), Is.False);
        Assert.That(interval.ContainsVersionExcept(ParseVersion("1.5.1"), excluded), Is.True);
        Assert.That(interval.ContainsVersionExcept(ParseVersion("2.0.0"), excluded), Is.False);
    }

    [Test]
    public void ContainsVersionExcept_DoesNotCreateHoleOutsideInterval()
    {
        VersionInterval interval = VersionInterval.CreateBounded(
            BoundKind.Inclusive,
            ParseVersion("1.0.0"),
            BoundKind.Exclusive,
            ParseVersion("2.0.0"),
            VersionComparisonMode.Precedence);
        UniversalVersion excluded = ParseVersion("3.0.0");

        Assert.That(interval.ContainsExclusionHole(excluded), Is.False);
        Assert.That(interval.ContainsVersionExcept(ParseVersion("1.5.0"), excluded), Is.True);
        Assert.That(interval.ContainsVersionExcept(ParseVersion("3.0.0"), excluded), Is.False);
    }

    [Test]
    public void ToComparatorSetExcluding_AddsNotEqualHoleToIntervalBounds()
    {
        VersionInterval interval = VersionInterval.CreateBounded(
            BoundKind.Inclusive,
            ParseVersion("1.0.0"),
            BoundKind.Exclusive,
            ParseVersion("2.0.0"),
            VersionComparisonMode.Precedence);

        VersionComparatorSet set = interval.ToComparatorSetExcluding(ParseVersion("1.5.0"));

        Assert.That(set.GetComparatorCount(), Is.EqualTo(3));
        Assert.That(set.GetComparator(0).GetOperator(), Is.EqualTo(RangeOperator.GreaterThanOrEqual));
        Assert.That(set.GetComparator(1).GetOperator(), Is.EqualTo(RangeOperator.LessThan));
        Assert.That(set.GetComparator(2).GetOperator(), Is.EqualTo(RangeOperator.NotEqual));
        Assert.That(set.ContainsVersion(ParseVersion("1.4.9")), Is.True);
        Assert.That(set.ContainsVersion(ParseVersion("1.5.0")), Is.False);
        Assert.That(set.ContainsVersion(ParseVersion("1.5.1")), Is.True);
    }

    [Test]
    public void ToRangeExcluding_RepresentsIntervalDifferenceAsSingleHoleClause()
    {
        VersionInterval interval = VersionInterval.CreateBounded(
            BoundKind.Exclusive,
            ParseVersion("1.0.0"),
            BoundKind.Inclusive,
            ParseVersion("2.0.0"),
            VersionComparisonMode.Precedence);

        VersionRange range = interval.ToRangeExcluding(ParseVersion("2.0.0"));

        Assert.That(range.GetSetCount(), Is.EqualTo(1));
        Assert.That(range.ContainsVersion(ParseVersion("1.0.0")), Is.False);
        Assert.That(range.ContainsVersion(ParseVersion("1.5.0")), Is.True);
        Assert.That(range.ContainsVersion(ParseVersion("2.0.0")), Is.False);
    }

    [Test]
    public void ToRangeExcluding_ExactIntervalCanBecomeUnsatisfiedByAnyCandidate()
    {
        VersionInterval interval = VersionInterval.CreateBounded(
            BoundKind.Inclusive,
            ParseVersion("1.0.0"),
            BoundKind.Inclusive,
            ParseVersion("1.0.0"),
            VersionComparisonMode.Precedence);

        VersionRange range = interval.ToRangeExcluding(ParseVersion("1.0.0"));

        Assert.That(range.ContainsVersion(ParseVersion("0.9.9")), Is.False);
        Assert.That(range.ContainsVersion(ParseVersion("1.0.0")), Is.False);
        Assert.That(range.ContainsVersion(ParseVersion("1.0.1")), Is.False);
    }

    [Test]
    public void ExclusionHelpers_RejectNullVersions()
    {
        VersionInterval interval = VersionInterval.CreateUnbounded(VersionComparisonMode.Precedence);
        UniversalVersion version = ParseVersion("1.0.0");

        Assert.That(
            delegate
            {
                interval.ContainsVersionExcept(null!, version);
            },
            Throws.TypeOf<System.ArgumentNullException>());

        Assert.That(
            delegate
            {
                interval.ContainsVersionExcept(version, null!);
            },
            Throws.TypeOf<System.ArgumentNullException>());

        Assert.That(
            delegate
            {
                interval.ToComparatorSetExcluding(null!);
            },
            Throws.TypeOf<System.ArgumentNullException>());
    }

    static UniversalVersion ParseVersion(string text)
    {
        VersionParseResult result = SemVerParser.ParseStrict(text);

        Assert.That(result.IsSuccess(), Is.True);
        return result.GetVersion();
    }
}
