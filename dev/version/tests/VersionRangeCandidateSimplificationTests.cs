using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class VersionRangeCandidateSimplificationTests
{
    [Test]
    public void SimplifyAgainstCandidates_ReturnsUnionOfMatchingCandidates()
    {
        VersionRange range = Range("1.0.0", "3.0.0");
        UniversalVersion[] candidates = new UniversalVersion[]
        {
            ParseVersion("0.9.9"),
            ParseVersion("1.0.0"),
            ParseVersion("1.5.0"),
            ParseVersion("3.0.0")
        };

        VersionRange simplified = VersionRangeAlgebra.SimplifyAgainstCandidates(range, candidates);

        Assert.That(simplified.GetSetCount(), Is.EqualTo(2));
        Assert.That(simplified.ContainsVersion(ParseVersion("0.9.9")), Is.False);
        Assert.That(simplified.ContainsVersion(ParseVersion("1.0.0")), Is.True);
        Assert.That(simplified.ContainsVersion(ParseVersion("1.5.0")), Is.True);
        Assert.That(simplified.ContainsVersion(ParseVersion("2.0.0")), Is.False);
        Assert.That(simplified.ContainsVersion(ParseVersion("3.0.0")), Is.False);
    }

    [Test]
    public void SimplifyAgainstCandidates_InstanceMethodMatchesStaticMethod()
    {
        VersionRange range = Range("1.0.0", "2.0.0");
        UniversalVersion[] candidates = new UniversalVersion[]
        {
            ParseVersion("1.0.0"),
            ParseVersion("1.5.0"),
            ParseVersion("2.0.0")
        };

        VersionRange simplified = range.SimplifyAgainstCandidates(candidates);

        Assert.That(simplified.GetSetCount(), Is.EqualTo(2));
        Assert.That(simplified.ContainsVersion(ParseVersion("1.0.0")), Is.True);
        Assert.That(simplified.ContainsVersion(ParseVersion("1.5.0")), Is.True);
        Assert.That(simplified.ContainsVersion(ParseVersion("2.0.0")), Is.False);
    }

    [Test]
    public void SimplifyAgainstCandidates_DeduplicatesEquivalentCandidates()
    {
        VersionRange range = Range("1.0.0", "2.0.0");
        UniversalVersion[] candidates = new UniversalVersion[]
        {
            ParseVersion("1.0.0"),
            ParseVersion("1.0.0+build.1"),
            ParseVersion("1.5.0")
        };

        VersionRange simplified = VersionRangeAlgebra.SimplifyAgainstCandidates(range, candidates);

        Assert.That(simplified.GetSetCount(), Is.EqualTo(2));
        Assert.That(simplified.ContainsVersion(ParseVersion("1.0.0")), Is.True);
        Assert.That(simplified.ContainsVersion(ParseVersion("1.0.0+other")), Is.True);
        Assert.That(simplified.ContainsVersion(ParseVersion("1.5.0")), Is.True);
    }

    [Test]
    public void SimplifyAgainstCandidates_ReturnsEmptyRangeWhenNoCandidatesMatch()
    {
        VersionRange range = Range("1.0.0", "2.0.0");
        UniversalVersion[] candidates = new UniversalVersion[]
        {
            ParseVersion("0.9.9"),
            ParseVersion("2.0.0")
        };

        VersionRange simplified = VersionRangeAlgebra.SimplifyAgainstCandidates(range, candidates);

        Assert.That(simplified.IsEmpty(), Is.True);
        Assert.That(simplified.ContainsVersion(ParseVersion("1.5.0")), Is.False);
    }

    [Test]
    public void SimplifyAgainstCandidates_PreservesOriginalMembershipForCandidateList()
    {
        VersionRange range = new VersionRange(new VersionComparatorSet[]
        {
            VersionComparatorSet.CreateSingle(
                VersionComparator.CreateLessThan(ParseVersion("1.0.0"), VersionComparisonMode.Precedence)),
            VersionComparatorSet.CreateSingle(
                VersionComparator.CreateGreaterThanOrEqual(ParseVersion("2.0.0"), VersionComparisonMode.Precedence))
        });
        UniversalVersion[] candidates = new UniversalVersion[]
        {
            ParseVersion("0.5.0"),
            ParseVersion("1.5.0"),
            ParseVersion("2.0.0"),
            ParseVersion("2.5.0")
        };

        VersionRange simplified = VersionRangeAlgebra.SimplifyAgainstCandidates(range, candidates);
        int index = 0;

        while (index < candidates.Length)
        {
            Assert.That(
                simplified.ContainsVersion(candidates[index]),
                Is.EqualTo(range.ContainsVersion(candidates[index])),
                candidates[index].GetOriginalText());
            index = index + 1;
        }
    }

    [Test]
    public void SimplifyAgainstCandidates_RejectsInvalidArguments()
    {
        VersionRange range = Range("1.0.0", "2.0.0");
        UniversalVersion[] candidates = new UniversalVersion[] { ParseVersion("1.0.0") };

        Assert.That(
            delegate
            {
                VersionRangeAlgebra.SimplifyAgainstCandidates(null!, candidates);
            },
            Throws.TypeOf<System.ArgumentNullException>());
        Assert.That(
            delegate
            {
                VersionRangeAlgebra.SimplifyAgainstCandidates(range, null!);
            },
            Throws.TypeOf<System.ArgumentNullException>());
        Assert.That(
            delegate
            {
                VersionRangeAlgebra.SimplifyAgainstCandidates(range, new UniversalVersion[] { null! });
            },
            Throws.TypeOf<System.ArgumentException>());
    }

    static VersionRange Range(string lowerText, string upperText)
    {
        VersionComparatorSet set = new VersionComparatorSet(new VersionComparator[]
        {
            VersionComparator.CreateGreaterThanOrEqual(ParseVersion(lowerText), VersionComparisonMode.Precedence),
            VersionComparator.CreateLessThan(ParseVersion(upperText), VersionComparisonMode.Precedence)
        });

        return VersionRange.CreateSingleSet(set);
    }

    static UniversalVersion ParseVersion(string text)
    {
        VersionParseResult result = SemVerParser.ParseStrict(text);

        Assert.That(result.IsSuccess(), Is.True);
        return result.GetVersion();
    }
}
