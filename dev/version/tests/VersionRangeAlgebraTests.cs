using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class VersionRangeAlgebraTests
{
    [Test]
    public void Intersect_CombinesEveryDisjunctionPair()
    {
        VersionRange oneToThree = Range("1.0.0", "3.0.0");
        VersionRange twoToFour = Range("2.0.0", "4.0.0");

        VersionRange intersection = VersionRangeAlgebra.Intersect(oneToThree, twoToFour);

        Assert.That(intersection.ContainsVersion(ParseVersion("1.5.0")), Is.False);
        Assert.That(intersection.ContainsVersion(ParseVersion("2.0.0")), Is.True);
        Assert.That(intersection.ContainsVersion(ParseVersion("2.5.0")), Is.True);
        Assert.That(intersection.ContainsVersion(ParseVersion("3.0.0")), Is.False);
        Assert.That(oneToThree.IntersectWith(twoToFour).ContainsVersion(ParseVersion("2.5.0")), Is.True);
    }

    [Test]
    public void Complement_InvertsComparatorSetSemantics()
    {
        VersionRange range = Range("1.0.0", "2.0.0");
        VersionRange complement = VersionRangeAlgebra.Complement(range);

        Assert.That(complement.ContainsVersion(ParseVersion("0.9.9")), Is.True);
        Assert.That(complement.ContainsVersion(ParseVersion("1.0.0")), Is.False);
        Assert.That(complement.ContainsVersion(ParseVersion("1.5.0")), Is.False);
        Assert.That(complement.ContainsVersion(ParseVersion("2.0.0")), Is.True);
        Assert.That(range.Complement().ContainsVersion(ParseVersion("2.0.0")), Is.True);
    }

    [Test]
    public void Complement_HandlesEmptyAndUniversalRanges()
    {
        VersionRange emptyComplement = VersionRangeAlgebra.Complement(VersionRange.CreateEmpty());
        VersionRange universalComplement = VersionRangeAlgebra.Complement(VersionRange.CreateUniversal());

        Assert.That(emptyComplement.IsUniversal(), Is.True);
        Assert.That(universalComplement.IsEmpty(), Is.True);
        Assert.That(universalComplement.ContainsVersion(ParseVersion("1.0.0")), Is.False);
    }

    [Test]
    public void Difference_RemovesRightRangeFromLeftRange()
    {
        VersionRange oneToThree = Range("1.0.0", "3.0.0");
        VersionRange twoToFour = Range("2.0.0", "4.0.0");

        VersionRange difference = VersionRangeAlgebra.Difference(oneToThree, twoToFour);

        Assert.That(difference.ContainsVersion(ParseVersion("1.5.0")), Is.True);
        Assert.That(difference.ContainsVersion(ParseVersion("2.0.0")), Is.False);
        Assert.That(difference.ContainsVersion(ParseVersion("2.5.0")), Is.False);
        Assert.That(difference.ContainsVersion(ParseVersion("3.5.0")), Is.False);
        Assert.That(oneToThree.DifferenceWith(twoToFour).ContainsVersion(ParseVersion("1.5.0")), Is.True);
    }

    [Test]
    public void Difference_CanRepresentSingleVersionExclusion()
    {
        VersionRange baseRange = Range("1.0.0", "3.0.0");
        VersionRange excluded = VersionRange.CreateSingleComparator(
            VersionComparator.CreateEqual(ParseVersion("2.0.0"), VersionComparisonMode.Precedence));

        VersionRange difference = VersionRangeAlgebra.Difference(baseRange, excluded);

        Assert.That(difference.ContainsVersion(ParseVersion("1.5.0")), Is.True);
        Assert.That(difference.ContainsVersion(ParseVersion("2.0.0")), Is.False);
        Assert.That(difference.ContainsVersion(ParseVersion("2.5.0")), Is.True);
    }

    [Test]
    public void SubsetSupersetAndDisjointUseRangeAlgebra()
    {
        VersionRange oneToTwo = Range("1.0.0", "2.0.0");
        VersionRange oneToThree = Range("1.0.0", "3.0.0");
        VersionRange threeToFour = Range("3.0.0", "4.0.0");

        Assert.That(VersionRangeAlgebra.IsSubset(oneToTwo, oneToThree), Is.True);
        Assert.That(VersionRangeAlgebra.IsSubset(oneToThree, oneToTwo), Is.False);
        Assert.That(VersionRangeAlgebra.IsSuperset(oneToThree, oneToTwo), Is.True);
        Assert.That(VersionRangeAlgebra.IsDisjoint(oneToTwo, threeToFour), Is.True);
        Assert.That(oneToTwo.IsSubsetOf(oneToThree), Is.True);
        Assert.That(oneToThree.IsSupersetOf(oneToTwo), Is.True);
        Assert.That(oneToTwo.IsDisjointWith(threeToFour), Is.True);
    }

    [Test]
    public void Unsatisfiable_DetectsContradictoryBoundsAndExcludedExactVersion()
    {
        VersionRange contradictoryBounds = new VersionRange(new VersionComparatorSet[]
        {
            new VersionComparatorSet(new VersionComparator[]
            {
                VersionComparator.CreateGreaterThanOrEqual(ParseVersion("2.0.0"), VersionComparisonMode.Precedence),
                VersionComparator.CreateLessThan(ParseVersion("2.0.0"), VersionComparisonMode.Precedence)
            })
        });
        VersionRange excludedExactVersion = new VersionRange(new VersionComparatorSet[]
        {
            new VersionComparatorSet(new VersionComparator[]
            {
                VersionComparator.CreateGreaterThanOrEqual(ParseVersion("1.0.0"), VersionComparisonMode.Precedence),
                VersionComparator.CreateLessThanOrEqual(ParseVersion("1.0.0"), VersionComparisonMode.Precedence),
                VersionComparator.CreateNotEqual(ParseVersion("1.0.0"), VersionComparisonMode.Precedence)
            })
        });

        Assert.That(VersionRangeAlgebra.IsUnsatisfiable(contradictoryBounds), Is.True);
        Assert.That(VersionRangeAlgebra.IsUnsatisfiable(excludedExactVersion), Is.True);
        Assert.That(VersionRangeAlgebra.IsUnsatisfiable(Range("1.0.0", "2.0.0")), Is.False);
        Assert.That(contradictoryBounds.IsUnsatisfiable(), Is.True);
    }

    [Test]
    public void AlgebraMethods_RejectNullArguments()
    {
        VersionRange range = Range("1.0.0", "2.0.0");

        Assert.That(
            delegate
            {
                VersionRangeAlgebra.Intersect(null!, range);
            },
            Throws.TypeOf<System.ArgumentNullException>());
        Assert.That(
            delegate
            {
                VersionRangeAlgebra.Difference(range, null!);
            },
            Throws.TypeOf<System.ArgumentNullException>());
        Assert.That(
            delegate
            {
                VersionRangeAlgebra.Complement(null!);
            },
            Throws.TypeOf<System.ArgumentNullException>());
        Assert.That(
            delegate
            {
                VersionRangeAlgebra.IsUnsatisfiable((VersionRange)null!);
            },
            Throws.TypeOf<System.ArgumentNullException>());
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
