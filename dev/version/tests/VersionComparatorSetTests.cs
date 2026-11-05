using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class VersionComparatorSetTests
{
    [Test]
    public void UniversalSet_ContainsEveryVersion()
    {
        VersionComparatorSet set = VersionComparatorSet.CreateUniversal();

        Assert.That(set.IsUniversal(), Is.True);
        Assert.That(set.GetComparatorCount(), Is.EqualTo(0));
        Assert.That(set.ContainsVersion(ParseVersion("0.0.1")), Is.True);
        Assert.That(set.ContainsVersion(ParseVersion("999.0.0")), Is.True);
    }

    [Test]
    public void ContainsVersion_RequiresEveryComparatorToMatch()
    {
        VersionComparatorSet set = new VersionComparatorSet(new VersionComparator[]
        {
            VersionComparator.CreateGreaterThanOrEqual(ParseVersion("1.0.0"), VersionComparisonMode.Precedence),
            VersionComparator.CreateLessThan(ParseVersion("2.0.0"), VersionComparisonMode.Precedence)
        });

        Assert.That(set.ContainsVersion(ParseVersion("0.9.9")), Is.False);
        Assert.That(set.ContainsVersion(ParseVersion("1.0.0")), Is.True);
        Assert.That(set.ContainsVersion(ParseVersion("1.5.0")), Is.True);
        Assert.That(set.ContainsVersion(ParseVersion("2.0.0")), Is.False);
    }

    [Test]
    public void ContainsVersion_AppliesNotEqualExclusionsInsideIntersection()
    {
        VersionComparatorSet set = new VersionComparatorSet(new VersionComparator[]
        {
            VersionComparator.CreateGreaterThanOrEqual(ParseVersion("1.0.0"), VersionComparisonMode.Precedence),
            VersionComparator.CreateLessThan(ParseVersion("2.0.0"), VersionComparisonMode.Precedence),
            VersionComparator.CreateNotEqual(ParseVersion("1.5.0"), VersionComparisonMode.Precedence)
        });

        Assert.That(set.ContainsVersion(ParseVersion("1.4.9")), Is.True);
        Assert.That(set.ContainsVersion(ParseVersion("1.5.0")), Is.False);
        Assert.That(set.ContainsVersion(ParseVersion("1.5.1")), Is.True);
    }

    [Test]
    public void Intersect_CombinesComparatorClauses()
    {
        VersionComparatorSet lowerBound = VersionComparatorSet.CreateSingle(
            VersionComparator.CreateGreaterThanOrEqual(ParseVersion("1.0.0"), VersionComparisonMode.Precedence));
        VersionComparatorSet upperBound = VersionComparatorSet.CreateSingle(
            VersionComparator.CreateLessThan(ParseVersion("2.0.0"), VersionComparisonMode.Precedence));

        VersionComparatorSet combined = VersionComparatorSet.Intersect(lowerBound, upperBound);

        Assert.That(combined.GetComparatorCount(), Is.EqualTo(2));
        Assert.That(lowerBound.GetComparatorCount(), Is.EqualTo(1));
        Assert.That(upperBound.GetComparatorCount(), Is.EqualTo(1));
        Assert.That(combined.ContainsVersion(ParseVersion("1.2.3")), Is.True);
        Assert.That(combined.ContainsVersion(ParseVersion("2.0.0")), Is.False);
    }

    [Test]
    public void Intersect_WithUniversalReturnsEquivalentSet()
    {
        VersionComparatorSet universal = VersionComparatorSet.CreateUniversal();
        VersionComparatorSet lowerBound = VersionComparatorSet.CreateSingle(
            VersionComparator.CreateGreaterThan(ParseVersion("1.0.0"), VersionComparisonMode.Precedence));

        VersionComparatorSet combined = universal.Intersect(lowerBound);

        Assert.That(combined.GetComparatorCount(), Is.EqualTo(1));
        Assert.That(combined.ContainsVersion(ParseVersion("1.0.0")), Is.False);
        Assert.That(combined.ContainsVersion(ParseVersion("1.0.1")), Is.True);
    }

    [Test]
    public void Constructor_DefensivelyCopiesComparatorArrays()
    {
        VersionComparator greaterThan = VersionComparator.CreateGreaterThan(ParseVersion("1.0.0"), VersionComparisonMode.Precedence);
        VersionComparator lessThan = VersionComparator.CreateLessThan(ParseVersion("2.0.0"), VersionComparisonMode.Precedence);
        VersionComparator[] comparators = new VersionComparator[] { greaterThan };

        VersionComparatorSet set = new VersionComparatorSet(comparators);
        comparators[0] = lessThan;

        VersionComparator[] returnedComparators = set.GetComparators();
        returnedComparators[0] = lessThan;

        Assert.That(set.GetComparator(0), Is.SameAs(greaterThan));
    }

    [Test]
    public void Constructor_RejectsInvalidComparatorArrays()
    {
        Assert.That(
            delegate
            {
                new VersionComparatorSet(null!);
            },
            Throws.TypeOf<System.ArgumentNullException>());

        Assert.That(
            delegate
            {
                new VersionComparatorSet(new VersionComparator[] { null! });
            },
            Throws.TypeOf<System.ArgumentException>());
    }

    static UniversalVersion ParseVersion(string text)
    {
        VersionParseResult result = SemVerParser.ParseStrict(text);

        Assert.That(result.IsSuccess(), Is.True);
        return result.GetVersion();
    }
}
