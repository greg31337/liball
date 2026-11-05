using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class VersionRangeAlgebraPropertyTests
{
    [Test]
    public void AlgebraOperations_MatchBooleanSetOperationsOverFixedCandidates()
    {
        UniversalVersion[] candidates = FixedCandidates();
        VersionRange left = RangeWithExactVersion("1.0.0", "3.0.0", "4.0.0");
        VersionRange right = RangeWithExcludedVersion("2.0.0", "5.0.0", "3.0.0");

        AssertIntersectionMatches(left, right, VersionRangeAlgebra.Intersect(left, right), candidates);
        AssertUnionMatches(left, right, VersionRangeAlgebra.Union(left, right), candidates);
        AssertDifferenceMatches(left, right, VersionRangeAlgebra.Difference(left, right), candidates);
        AssertComplementMatches(left, VersionRangeAlgebra.Complement(left), candidates);
    }

    [Test]
    public void AlgebraIdentities_HoldOverFixedCandidates()
    {
        UniversalVersion[] candidates = FixedCandidates();
        VersionRange left = RangeWithExactVersion("1.0.0", "3.0.0", "4.0.0");
        VersionRange middle = RangeWithExcludedVersion("2.0.0", "5.0.0", "3.0.0");
        VersionRange right = Range("0.5.0", "2.5.0");

        AssertEquivalentOverCandidates(
            VersionRangeAlgebra.Intersect(left, middle),
            VersionRangeAlgebra.Intersect(middle, left),
            candidates);
        AssertEquivalentOverCandidates(
            VersionRangeAlgebra.Union(left, middle),
            VersionRangeAlgebra.Union(middle, left),
            candidates);
        AssertEquivalentOverCandidates(
            VersionRangeAlgebra.Intersect(VersionRangeAlgebra.Intersect(left, middle), right),
            VersionRangeAlgebra.Intersect(left, VersionRangeAlgebra.Intersect(middle, right)),
            candidates);
        AssertEquivalentOverCandidates(
            VersionRangeAlgebra.Complement(VersionRangeAlgebra.Union(left, middle)),
            VersionRangeAlgebra.Intersect(VersionRangeAlgebra.Complement(left), VersionRangeAlgebra.Complement(middle)),
            candidates);
    }

    [Test]
    public void RelationPredicates_MatchFixedCandidateExpectations()
    {
        UniversalVersion[] candidates = FixedCandidates();
        VersionRange oneToTwo = Range("1.0.0", "2.0.0");
        VersionRange oneToThree = Range("1.0.0", "3.0.0");
        VersionRange threeToFive = Range("3.0.0", "5.0.0");

        Assert.That(AllCandidateMembershipImplies(oneToTwo, oneToThree, candidates), Is.True);
        Assert.That(AnyCandidateInLeftButNotRight(oneToThree, oneToTwo, candidates), Is.True);
        Assert.That(NoCandidateInBoth(oneToTwo, threeToFive, candidates), Is.True);

        Assert.That(VersionRangeAlgebra.IsSubset(oneToTwo, oneToThree), Is.True);
        Assert.That(VersionRangeAlgebra.IsSubset(oneToThree, oneToTwo), Is.False);
        Assert.That(VersionRangeAlgebra.IsSuperset(oneToThree, oneToTwo), Is.True);
        Assert.That(VersionRangeAlgebra.IsDisjoint(oneToTwo, threeToFive), Is.True);
    }

    static UniversalVersion[] FixedCandidates()
    {
        return new UniversalVersion[]
        {
            ParseVersion("0.5.0"),
            ParseVersion("0.9.9"),
            ParseVersion("1.0.0"),
            ParseVersion("1.5.0"),
            ParseVersion("2.0.0"),
            ParseVersion("2.5.0"),
            ParseVersion("3.0.0"),
            ParseVersion("3.5.0"),
            ParseVersion("4.0.0"),
            ParseVersion("4.5.0"),
            ParseVersion("5.0.0")
        };
    }

    static VersionRange Range(string lowerText, string upperText)
    {
        return VersionRange.CreateSingleSet(new VersionComparatorSet(new VersionComparator[]
        {
            VersionComparator.CreateGreaterThanOrEqual(ParseVersion(lowerText), VersionComparisonMode.Precedence),
            VersionComparator.CreateLessThan(ParseVersion(upperText), VersionComparisonMode.Precedence)
        }));
    }

    static VersionRange RangeWithExactVersion(string lowerText, string upperText, string exactText)
    {
        return VersionRangeAlgebra.Union(
            Range(lowerText, upperText),
            VersionRange.CreateSingleComparator(
                VersionComparator.CreateEqual(ParseVersion(exactText), VersionComparisonMode.Precedence)));
    }

    static VersionRange RangeWithExcludedVersion(string lowerText, string upperText, string excludedText)
    {
        return VersionRange.CreateSingleSet(new VersionComparatorSet(new VersionComparator[]
        {
            VersionComparator.CreateGreaterThanOrEqual(ParseVersion(lowerText), VersionComparisonMode.Precedence),
            VersionComparator.CreateLessThan(ParseVersion(upperText), VersionComparisonMode.Precedence),
            VersionComparator.CreateNotEqual(ParseVersion(excludedText), VersionComparisonMode.Precedence)
        }));
    }

    static void AssertIntersectionMatches(VersionRange left, VersionRange right, VersionRange actual, UniversalVersion[] candidates)
    {
        int index = 0;
        while (index < candidates.Length)
        {
            bool expected = left.ContainsVersion(candidates[index]) && right.ContainsVersion(candidates[index]);
            Assert.That(actual.ContainsVersion(candidates[index]), Is.EqualTo(expected), candidates[index].ToNormalizedString());
            index = index + 1;
        }
    }

    static void AssertUnionMatches(VersionRange left, VersionRange right, VersionRange actual, UniversalVersion[] candidates)
    {
        int index = 0;
        while (index < candidates.Length)
        {
            bool expected = left.ContainsVersion(candidates[index]) || right.ContainsVersion(candidates[index]);
            Assert.That(actual.ContainsVersion(candidates[index]), Is.EqualTo(expected), candidates[index].ToNormalizedString());
            index = index + 1;
        }
    }

    static void AssertDifferenceMatches(VersionRange left, VersionRange right, VersionRange actual, UniversalVersion[] candidates)
    {
        int index = 0;
        while (index < candidates.Length)
        {
            bool expected = left.ContainsVersion(candidates[index]) && !right.ContainsVersion(candidates[index]);
            Assert.That(actual.ContainsVersion(candidates[index]), Is.EqualTo(expected), candidates[index].ToNormalizedString());
            index = index + 1;
        }
    }

    static void AssertComplementMatches(VersionRange original, VersionRange actual, UniversalVersion[] candidates)
    {
        int index = 0;
        while (index < candidates.Length)
        {
            bool expected = !original.ContainsVersion(candidates[index]);
            Assert.That(actual.ContainsVersion(candidates[index]), Is.EqualTo(expected), candidates[index].ToNormalizedString());
            index = index + 1;
        }
    }

    static void AssertEquivalentOverCandidates(VersionRange left, VersionRange right, UniversalVersion[] candidates)
    {
        int index = 0;
        while (index < candidates.Length)
        {
            Assert.That(
                left.ContainsVersion(candidates[index]),
                Is.EqualTo(right.ContainsVersion(candidates[index])),
                candidates[index].ToNormalizedString());
            index = index + 1;
        }
    }

    static bool AllCandidateMembershipImplies(VersionRange left, VersionRange right, UniversalVersion[] candidates)
    {
        int index = 0;
        while (index < candidates.Length)
        {
            if (left.ContainsVersion(candidates[index]) && !right.ContainsVersion(candidates[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static bool AnyCandidateInLeftButNotRight(VersionRange left, VersionRange right, UniversalVersion[] candidates)
    {
        int index = 0;
        while (index < candidates.Length)
        {
            if (left.ContainsVersion(candidates[index]) && !right.ContainsVersion(candidates[index]))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static bool NoCandidateInBoth(VersionRange left, VersionRange right, UniversalVersion[] candidates)
    {
        int index = 0;
        while (index < candidates.Length)
        {
            if (left.ContainsVersion(candidates[index]) && right.ContainsVersion(candidates[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static UniversalVersion ParseVersion(string text)
    {
        VersionParseResult result = SemVerParser.ParseStrict(text);

        Assert.That(result.IsSuccess(), Is.True, text);
        return result.GetVersion();
    }
}
