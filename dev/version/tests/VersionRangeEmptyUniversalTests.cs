using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class VersionRangeEmptyUniversalTests
{
    [Test]
    public void EmptyRange_HasNoSetsAndContainsNoVersions()
    {
        VersionRange range = VersionRange.CreateEmpty();

        Assert.That(range.IsEmpty(), Is.True);
        Assert.That(range.IsUniversal(), Is.False);
        Assert.That(range.IsSingleSet(), Is.False);
        Assert.That(range.GetSetCount(), Is.EqualTo(0));
        Assert.That(range.ContainsVersion(ParseVersion("0.0.1")), Is.False);
        Assert.That(range.ContainsVersion(ParseVersion("999.0.0")), Is.False);
    }

    [Test]
    public void UniversalRange_HasUniversalSetAndContainsEveryVersion()
    {
        VersionRange range = VersionRange.CreateUniversal();

        Assert.That(range.IsEmpty(), Is.False);
        Assert.That(range.IsUniversal(), Is.True);
        Assert.That(range.IsSingleSet(), Is.True);
        Assert.That(range.GetSetCount(), Is.EqualTo(1));
        Assert.That(range.GetSet(0).IsUniversal(), Is.True);
        Assert.That(range.ContainsVersion(ParseVersion("0.0.1")), Is.True);
        Assert.That(range.ContainsVersion(ParseVersion("999.0.0")), Is.True);
    }

    [Test]
    public void Constructor_AllowsEmptySetArrayAsEmptyRange()
    {
        VersionRange range = new VersionRange(new VersionComparatorSet[0]);

        Assert.That(range.IsEmpty(), Is.True);
        Assert.That(range.GetSets(), Is.EqualTo(new VersionComparatorSet[0]));
    }

    [Test]
    public void Union_WithEmptyRangeReturnsEquivalentDisjunction()
    {
        VersionRange empty = VersionRange.CreateEmpty();
        VersionRange lowerRange = VersionRange.CreateSingleComparator(
            VersionComparator.CreateLessThan(ParseVersion("1.0.0"), VersionComparisonMode.Precedence));

        VersionRange leftCombined = VersionRange.Union(empty, lowerRange);
        VersionRange rightCombined = lowerRange.UnionWith(empty);

        Assert.That(leftCombined.GetSetCount(), Is.EqualTo(1));
        Assert.That(leftCombined.ContainsVersion(ParseVersion("0.9.9")), Is.True);
        Assert.That(leftCombined.ContainsVersion(ParseVersion("1.0.0")), Is.False);
        Assert.That(rightCombined.GetSetCount(), Is.EqualTo(1));
        Assert.That(rightCombined.ContainsVersion(ParseVersion("0.9.9")), Is.True);
        Assert.That(rightCombined.ContainsVersion(ParseVersion("1.0.0")), Is.False);
    }

    [Test]
    public void Union_WithUniversalRangeIsSemanticallyUniversal()
    {
        VersionRange upperRange = VersionRange.CreateSingleComparator(
            VersionComparator.CreateGreaterThan(ParseVersion("10.0.0"), VersionComparisonMode.Precedence));
        VersionRange universal = VersionRange.CreateUniversal();

        VersionRange combined = upperRange.UnionWith(universal);

        Assert.That(combined.IsUniversal(), Is.True);
        Assert.That(combined.IsEmpty(), Is.False);
        Assert.That(combined.ContainsVersion(ParseVersion("0.0.1")), Is.True);
        Assert.That(combined.ContainsVersion(ParseVersion("10.0.1")), Is.True);
    }

    [Test]
    public void EmptyRange_GetSetRejectsAnyIndex()
    {
        VersionRange range = VersionRange.CreateEmpty();

        Assert.That(
            delegate
            {
                range.GetSet(0);
            },
            Throws.TypeOf<System.ArgumentOutOfRangeException>());
    }

    static UniversalVersion ParseVersion(string text)
    {
        VersionParseResult result = SemVerParser.ParseStrict(text);

        Assert.That(result.IsSuccess(), Is.True);
        return result.GetVersion();
    }
}
