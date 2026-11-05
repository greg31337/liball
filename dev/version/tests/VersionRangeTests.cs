using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class VersionRangeTests
{
    [Test]
    public void ContainsVersion_MatchesAnyComparatorSet()
    {
        VersionRange range = new VersionRange(new VersionComparatorSet[]
        {
            VersionComparatorSet.CreateSingle(VersionComparator.CreateLessThan(ParseVersion("1.0.0"), VersionComparisonMode.Precedence)),
            VersionComparatorSet.CreateSingle(VersionComparator.CreateGreaterThanOrEqual(ParseVersion("2.0.0"), VersionComparisonMode.Precedence))
        });

        Assert.That(range.ContainsVersion(ParseVersion("0.9.9")), Is.True);
        Assert.That(range.ContainsVersion(ParseVersion("1.5.0")), Is.False);
        Assert.That(range.ContainsVersion(ParseVersion("2.0.0")), Is.True);
        Assert.That(range.ContainsVersion(ParseVersion("2.1.0")), Is.True);
    }

    [Test]
    public void ContainsVersion_UsesIntersectionInsideEachDisjunctionClause()
    {
        VersionComparatorSet oneX = new VersionComparatorSet(new VersionComparator[]
        {
            VersionComparator.CreateGreaterThanOrEqual(ParseVersion("1.0.0"), VersionComparisonMode.Precedence),
            VersionComparator.CreateLessThan(ParseVersion("2.0.0"), VersionComparisonMode.Precedence)
        });
        VersionComparatorSet threeX = new VersionComparatorSet(new VersionComparator[]
        {
            VersionComparator.CreateGreaterThanOrEqual(ParseVersion("3.0.0"), VersionComparisonMode.Precedence),
            VersionComparator.CreateLessThan(ParseVersion("4.0.0"), VersionComparisonMode.Precedence)
        });
        VersionRange range = new VersionRange(new VersionComparatorSet[] { oneX, threeX });

        Assert.That(range.ContainsVersion(ParseVersion("1.5.0")), Is.True);
        Assert.That(range.ContainsVersion(ParseVersion("2.5.0")), Is.False);
        Assert.That(range.ContainsVersion(ParseVersion("3.5.0")), Is.True);
    }

    [Test]
    public void Union_CombinesDisjunctionClauses()
    {
        VersionRange lowerRange = VersionRange.CreateSingleComparator(
            VersionComparator.CreateLessThan(ParseVersion("1.0.0"), VersionComparisonMode.Precedence));
        VersionRange upperRange = VersionRange.CreateSingleComparator(
            VersionComparator.CreateGreaterThanOrEqual(ParseVersion("2.0.0"), VersionComparisonMode.Precedence));

        VersionRange combined = VersionRange.Union(lowerRange, upperRange);

        Assert.That(combined.GetSetCount(), Is.EqualTo(2));
        Assert.That(lowerRange.GetSetCount(), Is.EqualTo(1));
        Assert.That(upperRange.GetSetCount(), Is.EqualTo(1));
        Assert.That(combined.ContainsVersion(ParseVersion("0.5.0")), Is.True);
        Assert.That(combined.ContainsVersion(ParseVersion("1.5.0")), Is.False);
        Assert.That(combined.ContainsVersion(ParseVersion("2.5.0")), Is.True);
    }

    [Test]
    public void UnionWithUniversalSetClauseMatchesEveryVersion()
    {
        VersionRange lowerRange = VersionRange.CreateSingleComparator(
            VersionComparator.CreateLessThan(ParseVersion("1.0.0"), VersionComparisonMode.Precedence));
        VersionRange universalRange = VersionRange.CreateSingleSet(VersionComparatorSet.CreateUniversal());

        VersionRange combined = lowerRange.UnionWith(universalRange);

        Assert.That(combined.ContainsVersion(ParseVersion("0.5.0")), Is.True);
        Assert.That(combined.ContainsVersion(ParseVersion("1.5.0")), Is.True);
        Assert.That(combined.ContainsVersion(ParseVersion("999.0.0")), Is.True);
    }

    [Test]
    public void Constructor_DefensivelyCopiesSetArrays()
    {
        VersionComparatorSet lowerSet = VersionComparatorSet.CreateSingle(
            VersionComparator.CreateLessThan(ParseVersion("1.0.0"), VersionComparisonMode.Precedence));
        VersionComparatorSet upperSet = VersionComparatorSet.CreateSingle(
            VersionComparator.CreateGreaterThan(ParseVersion("2.0.0"), VersionComparisonMode.Precedence));
        VersionComparatorSet[] sets = new VersionComparatorSet[] { lowerSet };

        VersionRange range = new VersionRange(sets);
        sets[0] = upperSet;

        VersionComparatorSet[] returnedSets = range.GetSets();
        returnedSets[0] = upperSet;

        Assert.That(range.GetSet(0), Is.SameAs(lowerSet));
        Assert.That(range.IsSingleSet(), Is.True);
    }

    [Test]
    public void Constructor_RejectsInvalidSetArrays()
    {
        Assert.That(
            delegate
            {
                new VersionRange(null!);
            },
            Throws.TypeOf<System.ArgumentNullException>());

        Assert.That(
            delegate
            {
                new VersionRange(new VersionComparatorSet[] { null! });
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
