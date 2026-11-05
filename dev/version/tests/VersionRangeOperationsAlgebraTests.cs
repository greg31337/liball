using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class VersionRangeOperationsAlgebraTests
{
    [Test]
    public void SetOperations_ExposeIntersectionUnionDifferenceAndComplement()
    {
        IVersionRangeOperations operations = new VersionRangeOperations();
        VersionRange left = ParseRange(">=1.0.0 <3.0.0");
        VersionRange right = ParseRange(">=2.0.0 <4.0.0");

        VersionRange intersection = operations.IntersectRanges(left, right);
        VersionRange union = operations.UnionRanges(left, right);
        VersionRange difference = operations.DifferenceRanges(left, right);
        VersionRange complement = operations.ComplementRange(left);

        AssertMembership(operations, intersection, "1.9.9", false);
        AssertMembership(operations, intersection, "2.0.0", true);
        AssertMembership(operations, intersection, "3.0.0", false);
        AssertMembership(operations, union, "1.5.0", true);
        AssertMembership(operations, union, "3.5.0", true);
        AssertMembership(operations, union, "4.0.0", false);
        AssertMembership(operations, difference, "1.5.0", true);
        AssertMembership(operations, difference, "2.0.0", false);
        AssertMembership(operations, complement, "0.9.9", true);
        AssertMembership(operations, complement, "1.0.0", false);
        AssertMembership(operations, complement, "3.0.0", true);
    }

    [Test]
    public void SimplifyRange_RemovesRedundantBoundsAndContradictorySets()
    {
        IVersionRangeOperations operations = new VersionRangeOperations();
        VersionRange redundant = ParseRange(">=1.0.0 >=2.0.0 <4.0.0 <5.0.0");
        VersionRange contradictory = ParseRange(">=2.0.0 <2.0.0");

        VersionRange simplified = operations.SimplifyRange(redundant);
        VersionRange empty = operations.SimplifyRange(contradictory);

        Assert.That(simplified.sets, Has.Length.EqualTo(1));
        Assert.That(simplified.sets[0].comparators, Has.Length.EqualTo(2));
        AssertMembership(operations, simplified, "1.9.9", false);
        AssertMembership(operations, simplified, "2.0.0", true);
        AssertMembership(operations, simplified, "3.9.9", true);
        AssertMembership(operations, simplified, "4.0.0", false);
        Assert.That(operations.IsEmpty(empty), Is.True);
        Assert.That(operations.IsUnsatisfiable(empty), Is.True);
    }

    [Test]
    public void FiniteCandidateSimplification_PreservesOnlyMatchingCandidates()
    {
        IVersionRangeOperations operations = new VersionRangeOperations();
        VersionRange range = ParseRange(">=1.0.0 <2.0.0");
        UniversalVersion[] candidates = new UniversalVersion[]
        {
            ParseVersion("0.9.9"),
            ParseVersion("1.0.0"),
            ParseVersion("1.5.0"),
            ParseVersion("2.0.0")
        };

        VersionRange simplified = operations.SimplifyRangeAgainstCandidates(range, candidates);

        Assert.That(simplified.sets, Has.Length.EqualTo(2));
        AssertMembership(operations, simplified, "0.9.9", false);
        AssertMembership(operations, simplified, "1.0.0", true);
        AssertMembership(operations, simplified, "1.5.0", true);
        AssertMembership(operations, simplified, "1.7.0", false);
        AssertMembership(operations, simplified, "2.0.0", false);
    }

    [Test]
    public void RelationChecks_ExposeSubsetSupersetDisjointAndUnsatisfiableResults()
    {
        IVersionRangeOperations operations = new VersionRangeOperations();
        VersionRange narrow = ParseRange(">=1.0.0 <2.0.0");
        VersionRange wide = ParseRange(">=1.0.0 <3.0.0");
        VersionRange separate = ParseRange(">=3.0.0 <4.0.0");
        VersionRange contradictory = ParseRange(">=2.0.0 <2.0.0");

        Assert.That(operations.IsSubset(narrow, wide), Is.True);
        Assert.That(operations.IsSubset(wide, narrow), Is.False);
        Assert.That(operations.IsSuperset(wide, narrow), Is.True);
        Assert.That(operations.IsSuperset(narrow, wide), Is.False);
        Assert.That(operations.IsDisjoint(narrow, separate), Is.True);
        Assert.That(operations.IsDisjoint(wide, narrow), Is.False);
        Assert.That(operations.IsUnsatisfiable(contradictory), Is.True);
        Assert.That(operations.IsUnsatisfiable(narrow), Is.False);
    }

    [Test]
    public void EmptyAndUniversalRanges_AreAlgebraIdentities()
    {
        IVersionRangeOperations operations = new VersionRangeOperations();
        VersionRange empty = operations.CreateEmptyRange();
        VersionRange universal = operations.CreateUniversalRange();
        VersionRange bounded = ParseRange(">=1.0.0 <2.0.0");

        Assert.That(operations.IsEmpty(operations.IntersectRanges(empty, bounded)), Is.True);
        Assert.That(operations.IsUniversal(operations.UnionRanges(universal, bounded)), Is.True);
        Assert.That(operations.IsUniversal(operations.ComplementRange(empty)), Is.True);
        Assert.That(operations.IsEmpty(operations.ComplementRange(universal)), Is.True);
        Assert.That(operations.IsSubset(empty, bounded), Is.True);
        Assert.That(operations.IsSuperset(universal, bounded), Is.True);
        Assert.That(operations.IsDisjoint(empty, bounded), Is.True);
    }

    [Test]
    public void AlgebraOperations_RejectEveryNullInputAtThePublicBoundary()
    {
        IVersionRangeOperations operations = new VersionRangeOperations();
        VersionRange range = ParseRange(">=1.0.0 <2.0.0");
        UniversalVersion[] candidates = new UniversalVersion[] { ParseVersion("1.5.0") };

        Assert.That(() => operations.IntersectRanges(null!, range), Throws.ArgumentNullException);
        Assert.That(() => operations.IntersectRanges(range, null!), Throws.ArgumentNullException);
        Assert.That(() => operations.UnionRanges(null!, range), Throws.ArgumentNullException);
        Assert.That(() => operations.UnionRanges(range, null!), Throws.ArgumentNullException);
        Assert.That(() => operations.DifferenceRanges(null!, range), Throws.ArgumentNullException);
        Assert.That(() => operations.DifferenceRanges(range, null!), Throws.ArgumentNullException);
        Assert.That(() => operations.ComplementRange(null!), Throws.ArgumentNullException);
        Assert.That(() => operations.SimplifyRange(null!), Throws.ArgumentNullException);
        Assert.That(
            () => operations.SimplifyRangeAgainstCandidates(null!, candidates),
            Throws.ArgumentNullException);
        Assert.That(
            () => operations.SimplifyRangeAgainstCandidates(range, null!),
            Throws.ArgumentNullException);
        Assert.That(
            () => operations.SimplifyRangeAgainstCandidates(
                range,
                new UniversalVersion[] { null! }),
            Throws.ArgumentException);
        Assert.That(() => operations.IsUnsatisfiable(null!), Throws.ArgumentNullException);
        Assert.That(() => operations.IsSubset(null!, range), Throws.ArgumentNullException);
        Assert.That(() => operations.IsSubset(range, null!), Throws.ArgumentNullException);
        Assert.That(() => operations.IsSuperset(null!, range), Throws.ArgumentNullException);
        Assert.That(() => operations.IsSuperset(range, null!), Throws.ArgumentNullException);
        Assert.That(() => operations.IsDisjoint(null!, range), Throws.ArgumentNullException);
        Assert.That(() => operations.IsDisjoint(range, null!), Throws.ArgumentNullException);
    }

    static void AssertMembership(
        IVersionRangeOperations operations,
        VersionRange range,
        string text,
        bool expected)
    {
        Assert.That(
            operations.ContainsText(range, text, ParseOptions()),
            Is.EqualTo(expected),
            text);
    }

    static VersionRange ParseRange(string text)
    {
        IVersionRangeParsing parsing = new VersionService();
        return parsing.ParseRange(text, RangeOptions());
    }

    static UniversalVersion ParseVersion(string text)
    {
        IVersionParsing parsing = new VersionService();
        return parsing.Parse(text, ParseOptions());
    }

    static VersionParseOptions ParseOptions()
    {
        return new VersionParseOptions(
            VersionDialect.NodeSemVer,
            VersionParseMode.Strict,
            4096,
            true,
            true,
            PrereleasePolicy.Include);
    }

    static VersionRangeParseOptions RangeOptions()
    {
        return new VersionRangeParseOptions(
            RangeSyntax.NodeSemVer,
            PrereleasePolicy.Include,
            4096,
            true,
            true,
            true,
            true,
            true);
    }
}
