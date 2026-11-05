using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class PublicVersionRangeAlgebraPropertyTests
{
    [Test]
    public void BinarySetOperations_MatchBooleanPropertiesAcrossEveryRangeShape()
    {
        IVersionRangeOperations operations = new VersionRangeOperations();
        UniversalVersion[] candidates = FixedCandidates();
        VersionRange bounded = ParseRange(">=1.0.0 <3.0.0");
        VersionRange[] leftCases = new VersionRange[]
        {
            operations.CreateEmptyRange(),
            operations.CreateUniversalRange(),
            ParseRange("<2.0.0"),
            ParseRange("<=2.0.0"),
            ParseRange(">=1.0.0 <=2.0.0"),
            ParseRange("<2.0.0 || >2.0.0"),
            ParseRange("<1.0.0 || >=3.0.0"),
            ParseRange(">=2.0.0 <2.0.0"),
            ParseRange(">=1.0.0-alpha <1.0.0")
        };
        VersionRange[] rightCases = new VersionRange[]
        {
            bounded,
            ParseRange("<2.0.0 || >2.0.0"),
            ParseRange(">=2.0.0"),
            ParseRange(">=2.0.0"),
            ParseRange(">1.0.0 <2.0.0"),
            ParseRange("=2.0.0"),
            ParseRange(">=1.0.0 <3.0.0"),
            bounded,
            ParseRange(">=1.0.0 <2.0.0")
        };

        int caseIndex = 0;
        while (caseIndex < leftCases.Length)
        {
            VersionRange intersection = operations.IntersectRanges(leftCases[caseIndex], rightCases[caseIndex]);
            VersionRange union = operations.UnionRanges(leftCases[caseIndex], rightCases[caseIndex]);
            VersionRange difference = operations.DifferenceRanges(leftCases[caseIndex], rightCases[caseIndex]);
            int candidateIndex = 0;

            while (candidateIndex < candidates.Length)
            {
                UniversalVersion candidate = candidates[candidateIndex];
                bool inLeft = operations.ContainsVersion(leftCases[caseIndex], candidate);
                bool inRight = operations.ContainsVersion(rightCases[caseIndex], candidate);
                string message = "case " + caseIndex + ", candidate " + candidate.originalText;

                Assert.That(
                    operations.ContainsVersion(intersection, candidate),
                    Is.EqualTo(inLeft && inRight),
                    message + ", intersection");
                Assert.That(
                    operations.ContainsVersion(union, candidate),
                    Is.EqualTo(inLeft || inRight),
                    message + ", union");
                Assert.That(
                    operations.ContainsVersion(difference, candidate),
                    Is.EqualTo(inLeft && !inRight),
                    message + ", difference");
                candidateIndex = candidateIndex + 1;
            }

            caseIndex = caseIndex + 1;
        }
    }

    [Test]
    public void ComplementAndSimplification_PreserveExpectedMembershipForEveryRangeShape()
    {
        IVersionRangeOperations operations = new VersionRangeOperations();
        UniversalVersion[] candidates = FixedCandidates();
        VersionRange[] ranges = EveryRangeShape(operations);
        int rangeIndex = 0;

        while (rangeIndex < ranges.Length)
        {
            VersionRange complement = operations.ComplementRange(ranges[rangeIndex]);
            VersionRange simplified = operations.SimplifyRange(ranges[rangeIndex]);
            int candidateIndex = 0;

            while (candidateIndex < candidates.Length)
            {
                UniversalVersion candidate = candidates[candidateIndex];
                bool original = operations.ContainsVersion(ranges[rangeIndex], candidate);
                string message = "range " + rangeIndex + ", candidate " + candidate.originalText;

                Assert.That(
                    operations.ContainsVersion(complement, candidate),
                    Is.EqualTo(!original),
                    message + ", complement");
                Assert.That(
                    operations.ContainsVersion(simplified, candidate),
                    Is.EqualTo(original),
                    message + ", simplification");
                candidateIndex = candidateIndex + 1;
            }

            rangeIndex = rangeIndex + 1;
        }

        Assert.That(operations.IsUniversal(operations.ComplementRange(operations.CreateEmptyRange())), Is.True);
        Assert.That(operations.IsEmpty(operations.ComplementRange(operations.CreateUniversalRange())), Is.True);
        Assert.That(operations.IsEmpty(operations.SimplifyRange(ParseRange(">=2.0.0 <2.0.0"))), Is.True);
    }

    [Test]
    public void FiniteCandidateSimplification_PreservesEachRangeOnTheFixedDomain()
    {
        IVersionRangeOperations operations = new VersionRangeOperations();
        UniversalVersion[] candidates = FixedCandidates();
        VersionRange[] ranges = EveryRangeShape(operations);
        UniversalVersion outsideDomain = ParseVersion("9.9.9");
        int rangeIndex = 0;

        while (rangeIndex < ranges.Length)
        {
            VersionRange simplified = operations.SimplifyRangeAgainstCandidates(ranges[rangeIndex], candidates);
            int candidateIndex = 0;

            while (candidateIndex < candidates.Length)
            {
                UniversalVersion candidate = candidates[candidateIndex];
                Assert.That(
                    operations.ContainsVersion(simplified, candidate),
                    Is.EqualTo(operations.ContainsVersion(ranges[rangeIndex], candidate)),
                    "range " + rangeIndex + ", candidate " + candidate.originalText);
                candidateIndex = candidateIndex + 1;
            }

            Assert.That(
                operations.ContainsVersion(simplified, outsideDomain),
                Is.False,
                "range " + rangeIndex + ", outside finite domain");
            rangeIndex = rangeIndex + 1;
        }
    }

    [Test]
    public void UnsatisfiableDetection_DistinguishesEmptyAndContradictoryRanges()
    {
        IVersionRangeOperations operations = new VersionRangeOperations();

        Assert.That(operations.IsUnsatisfiable(operations.CreateEmptyRange()), Is.True);
        Assert.That(operations.IsUnsatisfiable(ParseRange(">=2.0.0 <2.0.0")), Is.True);
        Assert.That(operations.IsUnsatisfiable(operations.CreateUniversalRange()), Is.False);
        Assert.That(operations.IsUnsatisfiable(ParseRange(">1.0.0 <2.0.0")), Is.False);
        Assert.That(operations.IsUnsatisfiable(ParseRange("<2.0.0 || >2.0.0")), Is.False);
        Assert.That(operations.IsUnsatisfiable(ParseRange(">=1.0.0-alpha <1.0.0")), Is.False);
    }

    [Test]
    public void RelationPredicates_MatchFixedCandidateExamplesAndBoundarySemantics()
    {
        IVersionRangeOperations operations = new VersionRangeOperations();
        UniversalVersion[] candidates = FixedCandidates();
        VersionRange empty = operations.CreateEmptyRange();
        VersionRange universal = operations.CreateUniversalRange();
        VersionRange open = ParseRange(">1.0.0 <2.0.0");
        VersionRange closed = ParseRange(">=1.0.0 <=2.0.0");
        VersionRange belowTouch = ParseRange("<2.0.0");
        VersionRange atOrAboveTouch = ParseRange(">=2.0.0");
        VersionRange throughTouch = ParseRange("<=2.0.0");
        VersionRange hole = ParseRange("<2.0.0 || >2.0.0");
        VersionRange exact = ParseRange("=2.0.0");
        VersionRange disjunction = ParseRange("<1.0.0 || >=3.0.0");
        VersionRange middle = ParseRange(">=1.0.0 <3.0.0");
        VersionRange contradictory = ParseRange(">=2.0.0 <2.0.0");
        VersionRange prerelease = ParseRange(">=1.0.0-alpha <1.0.0");
        VersionRange belowStable = ParseRange("<1.0.0");

        AssertRelation(operations, empty, closed, candidates, true, true);
        AssertRelation(operations, closed, universal, candidates, true, false);
        AssertRelation(operations, open, closed, candidates, true, false);
        AssertRelation(operations, closed, open, candidates, false, false);
        AssertRelation(operations, contradictory, middle, candidates, true, true);
        AssertRelation(operations, prerelease, belowStable, candidates, true, false);

        Assert.That(operations.IsSuperset(closed, open), Is.True);
        Assert.That(operations.IsSuperset(open, closed), Is.False);
        Assert.That(operations.IsSuperset(universal, closed), Is.True);
        Assert.That(operations.IsDisjoint(belowTouch, atOrAboveTouch), Is.True);
        Assert.That(NoCandidateInBoth(operations, belowTouch, atOrAboveTouch, candidates), Is.True);
        Assert.That(operations.IsDisjoint(throughTouch, atOrAboveTouch), Is.False);
        Assert.That(NoCandidateInBoth(operations, throughTouch, atOrAboveTouch, candidates), Is.False);
        Assert.That(operations.IsDisjoint(hole, exact), Is.True);
        Assert.That(NoCandidateInBoth(operations, hole, exact, candidates), Is.True);
        Assert.That(operations.IsDisjoint(disjunction, middle), Is.True);
        Assert.That(NoCandidateInBoth(operations, disjunction, middle, candidates), Is.True);
    }

    [Test]
    public void AlgebraExamples_PreserveTouchingBoundsExclusionHolesAndPrereleases()
    {
        IVersionRangeOperations operations = new VersionRangeOperations();
        VersionRange closed = ParseRange(">=1.0.0 <=2.0.0");
        VersionRange fromTouch = ParseRange(">=2.0.0 <3.0.0");
        VersionRange exactTouch = operations.IntersectRanges(closed, fromTouch);
        VersionRange withoutTouch = operations.DifferenceRanges(closed, ParseRange("=2.0.0"));
        VersionRange restoredTouch = operations.ComplementRange(ParseRange("<2.0.0 || >2.0.0"));
        VersionRange prereleaseOverlap = operations.IntersectRanges(
            ParseRange(">=1.0.0-alpha <1.0.0"),
            ParseRange(">=1.0.0-beta <1.0.0"));

        AssertMembership(operations, exactTouch, "1.9.9", false);
        AssertMembership(operations, exactTouch, "2.0.0", true);
        AssertMembership(operations, exactTouch, "2.0.1", false);
        AssertMembership(operations, withoutTouch, "1.5.0", true);
        AssertMembership(operations, withoutTouch, "2.0.0", false);
        AssertMembership(operations, restoredTouch, "1.9.9", false);
        AssertMembership(operations, restoredTouch, "2.0.0", true);
        AssertMembership(operations, restoredTouch, "2.0.1", false);
        AssertMembership(operations, prereleaseOverlap, "1.0.0-alpha", false);
        AssertMembership(operations, prereleaseOverlap, "1.0.0-beta", true);
        AssertMembership(operations, prereleaseOverlap, "1.0.0-rc.1", true);
        AssertMembership(operations, prereleaseOverlap, "1.0.0", false);
    }

    static VersionRange[] EveryRangeShape(IVersionRangeOperations operations)
    {
        return new VersionRange[]
        {
            operations.CreateEmptyRange(),
            operations.CreateUniversalRange(),
            ParseRange("<2.0.0"),
            ParseRange(">=2.0.0"),
            ParseRange(">1.0.0 <2.0.0"),
            ParseRange(">=1.0.0 <=2.0.0"),
            ParseRange("<2.0.0 || >2.0.0"),
            ParseRange("<1.0.0 || >=3.0.0"),
            ParseRange(">=2.0.0 <2.0.0"),
            ParseRange(">=1.0.0-alpha <1.0.0")
        };
    }

    static UniversalVersion[] FixedCandidates()
    {
        return new UniversalVersion[]
        {
            ParseVersion("0.0.0"),
            ParseVersion("0.9.9"),
            ParseVersion("1.0.0-alpha"),
            ParseVersion("1.0.0-beta"),
            ParseVersion("1.0.0-rc.1"),
            ParseVersion("1.0.0"),
            ParseVersion("1.5.0"),
            ParseVersion("1.9.9"),
            ParseVersion("2.0.0-alpha"),
            ParseVersion("2.0.0"),
            ParseVersion("2.0.1"),
            ParseVersion("2.5.0"),
            ParseVersion("3.0.0-alpha"),
            ParseVersion("3.0.0"),
            ParseVersion("4.0.0")
        };
    }

    static void AssertRelation(
        IVersionRangeOperations operations,
        VersionRange left,
        VersionRange right,
        UniversalVersion[] candidates,
        bool subset,
        bool disjoint)
    {
        Assert.That(AllMembershipImplies(operations, left, right, candidates), Is.EqualTo(subset));
        Assert.That(operations.IsSubset(left, right), Is.EqualTo(subset));
        Assert.That(operations.IsSuperset(right, left), Is.EqualTo(subset));
        Assert.That(NoCandidateInBoth(operations, left, right, candidates), Is.EqualTo(disjoint));
        Assert.That(operations.IsDisjoint(left, right), Is.EqualTo(disjoint));
    }

    static bool AllMembershipImplies(
        IVersionRangeOperations operations,
        VersionRange left,
        VersionRange right,
        UniversalVersion[] candidates)
    {
        int index = 0;
        while (index < candidates.Length)
        {
            if (operations.ContainsVersion(left, candidates[index])
                && !operations.ContainsVersion(right, candidates[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static bool NoCandidateInBoth(
        IVersionRangeOperations operations,
        VersionRange left,
        VersionRange right,
        UniversalVersion[] candidates)
    {
        int index = 0;
        while (index < candidates.Length)
        {
            if (operations.ContainsVersion(left, candidates[index])
                && operations.ContainsVersion(right, candidates[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static void AssertMembership(
        IVersionRangeOperations operations,
        VersionRange range,
        string text,
        bool expected)
    {
        UniversalVersion version = ParseVersion(text);
        Assert.That(operations.ContainsVersion(range, version), Is.EqualTo(expected), text);
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
