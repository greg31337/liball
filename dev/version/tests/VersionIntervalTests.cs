using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class VersionIntervalTests
{
    [Test]
    public void ContainsVersion_RespectsInclusiveLowerAndExclusiveUpperBounds()
    {
        VersionInterval interval = VersionInterval.CreateBounded(
            BoundKind.Inclusive,
            ParseVersion("1.0.0"),
            BoundKind.Exclusive,
            ParseVersion("2.0.0"),
            VersionComparisonMode.Precedence);

        Assert.That(interval.ContainsVersion(ParseVersion("0.9.9")), Is.False);
        Assert.That(interval.ContainsVersion(ParseVersion("1.0.0")), Is.True);
        Assert.That(interval.ContainsVersion(ParseVersion("1.5.0")), Is.True);
        Assert.That(interval.ContainsVersion(ParseVersion("2.0.0")), Is.False);
    }

    [Test]
    public void ContainsVersion_RespectsExclusiveLowerAndInclusiveUpperBounds()
    {
        VersionInterval interval = VersionInterval.CreateBounded(
            BoundKind.Exclusive,
            ParseVersion("1.0.0"),
            BoundKind.Inclusive,
            ParseVersion("2.0.0"),
            VersionComparisonMode.Precedence);

        Assert.That(interval.ContainsVersion(ParseVersion("1.0.0")), Is.False);
        Assert.That(interval.ContainsVersion(ParseVersion("1.0.1")), Is.True);
        Assert.That(interval.ContainsVersion(ParseVersion("2.0.0")), Is.True);
        Assert.That(interval.ContainsVersion(ParseVersion("2.0.1")), Is.False);
    }

    [Test]
    public void ContainsVersion_HandlesUnboundedEdges()
    {
        VersionInterval lowerOnly = VersionInterval.CreateLowerBound(
            BoundKind.Inclusive,
            ParseVersion("1.0.0"),
            VersionComparisonMode.Precedence);
        VersionInterval upperOnly = VersionInterval.CreateUpperBound(
            BoundKind.Exclusive,
            ParseVersion("2.0.0"),
            VersionComparisonMode.Precedence);
        VersionInterval unbounded = VersionInterval.CreateUnbounded(VersionComparisonMode.Precedence);

        Assert.That(lowerOnly.ContainsVersion(ParseVersion("0.9.9")), Is.False);
        Assert.That(lowerOnly.ContainsVersion(ParseVersion("1.0.0")), Is.True);
        Assert.That(upperOnly.ContainsVersion(ParseVersion("1.9.9")), Is.True);
        Assert.That(upperOnly.ContainsVersion(ParseVersion("2.0.0")), Is.False);
        Assert.That(unbounded.ContainsVersion(ParseVersion("0.0.1")), Is.True);
        Assert.That(unbounded.ContainsVersion(ParseVersion("999.0.0")), Is.True);
    }

    [Test]
    public void Constructor_StoresBoundState()
    {
        UniversalVersion lower = ParseVersion("1.0.0");
        UniversalVersion upper = ParseVersion("2.0.0");
        VersionInterval interval = VersionInterval.CreateBounded(
            BoundKind.Inclusive,
            lower,
            BoundKind.Exclusive,
            upper,
            VersionComparisonMode.Identity);

        Assert.That(interval.GetLowerKind(), Is.EqualTo(BoundKind.Inclusive));
        Assert.That(interval.GetUpperKind(), Is.EqualTo(BoundKind.Exclusive));
        Assert.That(interval.HasLowerBound(), Is.True);
        Assert.That(interval.HasUpperBound(), Is.True);
        Assert.That(interval.GetLowerVersion(), Is.SameAs(lower));
        Assert.That(interval.GetUpperVersion(), Is.SameAs(upper));
        Assert.That(interval.GetComparisonMode(), Is.EqualTo(VersionComparisonMode.Identity));
    }

    [Test]
    public void ToComparatorSet_ConvertsBoundsToPrimitiveComparators()
    {
        VersionInterval interval = VersionInterval.CreateBounded(
            BoundKind.Exclusive,
            ParseVersion("1.0.0"),
            BoundKind.Inclusive,
            ParseVersion("2.0.0"),
            VersionComparisonMode.Precedence);

        VersionComparatorSet set = interval.ToComparatorSet();

        Assert.That(set.GetComparatorCount(), Is.EqualTo(2));
        Assert.That(set.GetComparator(0).GetOperator(), Is.EqualTo(RangeOperator.GreaterThan));
        Assert.That(set.GetComparator(1).GetOperator(), Is.EqualTo(RangeOperator.LessThanOrEqual));
        Assert.That(set.ContainsVersion(ParseVersion("1.0.0")), Is.False);
        Assert.That(set.ContainsVersion(ParseVersion("1.5.0")), Is.True);
        Assert.That(set.ContainsVersion(ParseVersion("2.0.0")), Is.True);
    }

    [Test]
    public void ToComparatorSet_UnboundedIntervalCreatesUniversalSet()
    {
        VersionComparatorSet set = VersionInterval.CreateUnbounded(VersionComparisonMode.Precedence).ToComparatorSet();

        Assert.That(set.IsUniversal(), Is.True);
        Assert.That(set.ContainsVersion(ParseVersion("1.0.0")), Is.True);
    }

    [Test]
    public void Constructor_RejectsInvalidBounds()
    {
        Assert.That(
            delegate
            {
                new VersionInterval(BoundKind.Unbounded, ParseVersion("1.0.0"), BoundKind.Unbounded, null, VersionComparisonMode.Precedence);
            },
            Throws.TypeOf<System.ArgumentException>());

        Assert.That(
            delegate
            {
                new VersionInterval(BoundKind.Inclusive, null, BoundKind.Unbounded, null, VersionComparisonMode.Precedence);
            },
            Throws.TypeOf<System.ArgumentNullException>());

        Assert.That(
            delegate
            {
                VersionInterval.CreateBounded(
                    BoundKind.Inclusive,
                    ParseVersion("2.0.0"),
                    BoundKind.Inclusive,
                    ParseVersion("1.0.0"),
                    VersionComparisonMode.Precedence);
            },
            Throws.TypeOf<System.ArgumentException>());

        Assert.That(
            delegate
            {
                VersionInterval.CreateBounded(
                    BoundKind.Exclusive,
                    ParseVersion("1.0.0"),
                    BoundKind.Inclusive,
                    ParseVersion("1.0.0"),
                    VersionComparisonMode.Precedence);
            },
            Throws.TypeOf<System.ArgumentException>());

        Assert.That(
            delegate
            {
                new VersionInterval((BoundKind)99, null, BoundKind.Unbounded, null, VersionComparisonMode.Precedence);
            },
            Throws.TypeOf<System.ArgumentOutOfRangeException>());
    }

    [Test]
    public void UnboundedAccessors_ThrowWhenBoundIsAbsent()
    {
        VersionInterval interval = VersionInterval.CreateUnbounded(VersionComparisonMode.Precedence);

        Assert.That(
            delegate
            {
                interval.GetLowerVersion();
            },
            Throws.TypeOf<System.InvalidOperationException>());

        Assert.That(
            delegate
            {
                interval.GetUpperVersion();
            },
            Throws.TypeOf<System.InvalidOperationException>());
    }

    static UniversalVersion ParseVersion(string text)
    {
        VersionParseResult result = SemVerParser.ParseStrict(text);

        Assert.That(result.IsSuccess(), Is.True);
        return result.GetVersion();
    }
}
