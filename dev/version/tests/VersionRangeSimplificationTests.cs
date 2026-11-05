using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class VersionRangeSimplificationTests
{
    [Test]
    public void GetMinimumRepresentableVersion_ReturnsDialectSpecificZeroVersion()
    {
        AssertMinimum(VersionDialect.SemVer, "0.0.0");
        AssertMinimum(VersionDialect.NodeSemVer, "0.0.0");
        AssertMinimum(VersionDialect.Cargo, "0.0.0");
        AssertMinimum(VersionDialect.NuGet, "0");
        AssertMinimum(VersionDialect.Maven, "0");
        AssertMinimum(VersionDialect.Go, "v0.0.0");
        AssertMinimum(VersionDialect.RubyGems, "0");
        AssertMinimum(VersionDialect.Python, "0");
    }

    [Test]
    public void GetMinimumRepresentableVersion_RejectsUnknownDialect()
    {
        Assert.That(
            delegate
            {
                VersionRangeAlgebra.GetMinimumRepresentableVersion(VersionDialect.Unknown);
            },
            Throws.TypeOf<System.ArgumentOutOfRangeException>());
    }

    [Test]
    public void Simplify_RemovesWeakerLowerAndUpperBounds()
    {
        VersionComparatorSet set = new VersionComparatorSet(new VersionComparator[]
        {
            VersionComparator.CreateGreaterThanOrEqual(ParseVersion("1.0.0"), VersionComparisonMode.Precedence),
            VersionComparator.CreateGreaterThanOrEqual(ParseVersion("2.0.0"), VersionComparisonMode.Precedence),
            VersionComparator.CreateLessThan(ParseVersion("5.0.0"), VersionComparisonMode.Precedence),
            VersionComparator.CreateLessThan(ParseVersion("4.0.0"), VersionComparisonMode.Precedence)
        });

        VersionComparatorSet simplified = VersionRangeAlgebra.Simplify(set);

        Assert.That(simplified.GetComparatorCount(), Is.EqualTo(2));
        Assert.That(simplified.ContainsVersion(ParseVersion("1.5.0")), Is.False);
        Assert.That(simplified.ContainsVersion(ParseVersion("2.0.0")), Is.True);
        Assert.That(simplified.ContainsVersion(ParseVersion("3.9.9")), Is.True);
        Assert.That(simplified.ContainsVersion(ParseVersion("4.0.0")), Is.False);
    }

    [Test]
    public void Simplify_PrefersStrictBoundAtSameVersion()
    {
        VersionComparatorSet set = new VersionComparatorSet(new VersionComparator[]
        {
            VersionComparator.CreateGreaterThanOrEqual(ParseVersion("1.0.0"), VersionComparisonMode.Precedence),
            VersionComparator.CreateGreaterThan(ParseVersion("1.0.0"), VersionComparisonMode.Precedence),
            VersionComparator.CreateLessThanOrEqual(ParseVersion("2.0.0"), VersionComparisonMode.Precedence),
            VersionComparator.CreateLessThan(ParseVersion("2.0.0"), VersionComparisonMode.Precedence)
        });

        VersionComparatorSet simplified = VersionRangeAlgebra.Simplify(set);

        Assert.That(simplified.GetComparatorCount(), Is.EqualTo(2));
        Assert.That(simplified.ContainsVersion(ParseVersion("1.0.0")), Is.False);
        Assert.That(simplified.ContainsVersion(ParseVersion("1.0.1")), Is.True);
        Assert.That(simplified.ContainsVersion(ParseVersion("2.0.0")), Is.False);
    }

    [Test]
    public void Simplify_EqualComparatorMakesBoundsAndUnrelatedExclusionsRedundant()
    {
        VersionComparatorSet set = new VersionComparatorSet(new VersionComparator[]
        {
            VersionComparator.CreateGreaterThanOrEqual(ParseVersion("1.0.0"), VersionComparisonMode.Precedence),
            VersionComparator.CreateEqual(ParseVersion("2.0.0"), VersionComparisonMode.Precedence),
            VersionComparator.CreateLessThan(ParseVersion("3.0.0"), VersionComparisonMode.Precedence),
            VersionComparator.CreateNotEqual(ParseVersion("4.0.0"), VersionComparisonMode.Precedence)
        });

        VersionComparatorSet simplified = VersionRangeAlgebra.Simplify(set);

        Assert.That(simplified.GetComparatorCount(), Is.EqualTo(1));
        Assert.That(simplified.GetComparator(0).GetOperator(), Is.EqualTo(RangeOperator.Equal));
        Assert.That(simplified.ContainsVersion(ParseVersion("2.0.0")), Is.True);
        Assert.That(simplified.ContainsVersion(ParseVersion("2.0.1")), Is.False);
    }

    [Test]
    public void Simplify_RemovesDuplicateAndOutOfRangeExclusions()
    {
        VersionComparatorSet set = new VersionComparatorSet(new VersionComparator[]
        {
            VersionComparator.CreateGreaterThanOrEqual(ParseVersion("1.0.0"), VersionComparisonMode.Precedence),
            VersionComparator.CreateLessThan(ParseVersion("3.0.0"), VersionComparisonMode.Precedence),
            VersionComparator.CreateNotEqual(ParseVersion("0.5.0"), VersionComparisonMode.Precedence),
            VersionComparator.CreateNotEqual(ParseVersion("2.0.0"), VersionComparisonMode.Precedence),
            VersionComparator.CreateNotEqual(ParseVersion("2.0.0"), VersionComparisonMode.Precedence),
            VersionComparator.CreateNotEqual(ParseVersion("4.0.0"), VersionComparisonMode.Precedence)
        });

        VersionComparatorSet simplified = VersionRangeAlgebra.Simplify(set);

        Assert.That(simplified.GetComparatorCount(), Is.EqualTo(3));
        Assert.That(simplified.ContainsVersion(ParseVersion("1.5.0")), Is.True);
        Assert.That(simplified.ContainsVersion(ParseVersion("2.0.0")), Is.False);
        Assert.That(simplified.ContainsVersion(ParseVersion("3.0.0")), Is.False);
    }

    [Test]
    public void Simplify_RemovesUnsatisfiableSetsFromRanges()
    {
        VersionComparatorSet impossible = new VersionComparatorSet(new VersionComparator[]
        {
            VersionComparator.CreateGreaterThanOrEqual(ParseVersion("2.0.0"), VersionComparisonMode.Precedence),
            VersionComparator.CreateLessThan(ParseVersion("2.0.0"), VersionComparisonMode.Precedence)
        });
        VersionComparatorSet possible = new VersionComparatorSet(new VersionComparator[]
        {
            VersionComparator.CreateGreaterThanOrEqual(ParseVersion("1.0.0"), VersionComparisonMode.Precedence),
            VersionComparator.CreateLessThan(ParseVersion("3.0.0"), VersionComparisonMode.Precedence)
        });
        VersionRange range = new VersionRange(new VersionComparatorSet[] { impossible, possible });

        VersionRange simplified = VersionRangeAlgebra.Simplify(range);

        Assert.That(simplified.GetSetCount(), Is.EqualTo(1));
        Assert.That(simplified.ContainsVersion(ParseVersion("1.5.0")), Is.True);
        Assert.That(simplified.ContainsVersion(ParseVersion("3.0.0")), Is.False);
        Assert.That(range.Simplify().GetSetCount(), Is.EqualTo(1));
    }

    [Test]
    public void Simplify_AllUnsatisfiableSetsBecomeEmptyRange()
    {
        VersionRange range = new VersionRange(new VersionComparatorSet[]
        {
            new VersionComparatorSet(new VersionComparator[]
            {
                VersionComparator.CreateGreaterThan(ParseVersion("2.0.0"), VersionComparisonMode.Precedence),
                VersionComparator.CreateLessThan(ParseVersion("1.0.0"), VersionComparisonMode.Precedence)
            })
        });

        VersionRange simplified = VersionRangeAlgebra.Simplify(range);

        Assert.That(simplified.IsEmpty(), Is.True);
        Assert.That(simplified.ContainsVersion(ParseVersion("1.5.0")), Is.False);
    }

    static void AssertMinimum(VersionDialect dialect, string expectedText)
    {
        UniversalVersion version = VersionRangeAlgebra.GetMinimumRepresentableVersion(dialect);

        Assert.That(version.GetDialect(), Is.EqualTo(dialect));
        Assert.That(version.GetOriginalText(), Is.EqualTo(expectedText));
    }

    static UniversalVersion ParseVersion(string text)
    {
        VersionParseResult result = SemVerParser.ParseStrict(text);

        Assert.That(result.IsSuccess(), Is.True);
        return result.GetVersion();
    }
}
