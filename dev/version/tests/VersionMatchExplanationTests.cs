using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class VersionMatchExplanationTests
{
    [Test]
    public void Explain_ReturnsMatchedSetForSuccessfulCandidate()
    {
        VersionRange range = Range("1.0.0", "2.0.0");

        VersionMatchExplanation explanation = range.Explain(ParseVersion("1.5.0"));

        Assert.That(explanation.IsMatch(), Is.True);
        Assert.That(explanation.IsFailure(), Is.False);
        Assert.That(explanation.HasMatchedSet(), Is.True);
        Assert.That(explanation.GetMatchedSetIndex(), Is.EqualTo(0));
        Assert.That(explanation.HasFailedSet(), Is.False);
        Assert.That(explanation.HasFailedComparator(), Is.False);
        Assert.That(explanation.GetVersion().GetOriginalText(), Is.EqualTo("1.5.0"));
    }

    [Test]
    public void Explain_ReturnsFailedComparatorForRejectedCandidate()
    {
        VersionRange range = Range("1.0.0", "2.0.0");

        VersionMatchExplanation explanation = range.Explain(ParseVersion("2.0.0"));

        Assert.That(explanation.IsMatch(), Is.False);
        Assert.That(explanation.IsFailure(), Is.True);
        Assert.That(explanation.HasFailedSet(), Is.True);
        Assert.That(explanation.GetFailedSetIndex(), Is.EqualTo(0));
        Assert.That(explanation.HasFailedComparator(), Is.True);
        Assert.That(explanation.GetFailedComparator().GetOperator(), Is.EqualTo(RangeOperator.LessThan));
        Assert.That(explanation.GetFailedComparator().GetVersion().GetOriginalText(), Is.EqualTo("2.0.0"));
        Assert.That(explanation.GetMessage(), Is.EqualTo("Version does not satisfy comparator < 2.0.0."));
    }

    [Test]
    public void Explain_ReturnsLaterMatchedSetForUnionRanges()
    {
        VersionRange range = new VersionRange(new VersionComparatorSet[]
        {
            VersionComparatorSet.CreateSingle(
                VersionComparator.CreateLessThan(ParseVersion("1.0.0"), VersionComparisonMode.Precedence)),
            VersionComparatorSet.CreateSingle(
                VersionComparator.CreateGreaterThanOrEqual(ParseVersion("2.0.0"), VersionComparisonMode.Precedence))
        });

        VersionMatchExplanation explanation = range.Explain(ParseVersion("2.5.0"));

        Assert.That(explanation.IsMatch(), Is.True);
        Assert.That(explanation.GetMatchedSetIndex(), Is.EqualTo(1));
    }

    [Test]
    public void Explain_ReportsEmptyRangeFailure()
    {
        VersionRange range = VersionRange.CreateEmpty();

        VersionMatchExplanation explanation = range.Explain(ParseVersion("1.0.0"));

        Assert.That(explanation.IsFailure(), Is.True);
        Assert.That(explanation.HasFailedSet(), Is.False);
        Assert.That(explanation.HasFailedComparator(), Is.False);
        Assert.That(explanation.GetMessage(), Is.EqualTo("Range is empty."));
    }

    [Test]
    public void Explain_ReportsUniversalRangeSuccess()
    {
        VersionRange range = VersionRange.CreateUniversal();

        VersionMatchExplanation explanation = range.Explain(ParseVersion("99.0.0"));

        Assert.That(explanation.IsMatch(), Is.True);
        Assert.That(explanation.GetMatchedSetIndex(), Is.EqualTo(0));
    }

    [Test]
    public void ExplanationAccessorsRejectAbsentDetails()
    {
        VersionMatchExplanation match = VersionRange.CreateUniversal().Explain(ParseVersion("1.0.0"));
        VersionMatchExplanation failure = VersionRange.CreateEmpty().Explain(ParseVersion("1.0.0"));

        Assert.That(
            delegate
            {
                match.GetFailedSetIndex();
            },
            Throws.TypeOf<System.InvalidOperationException>());
        Assert.That(
            delegate
            {
                match.GetFailedComparator();
            },
            Throws.TypeOf<System.InvalidOperationException>());
        Assert.That(
            delegate
            {
                failure.GetMatchedSetIndex();
            },
            Throws.TypeOf<System.InvalidOperationException>());
    }

    [Test]
    public void Explain_RejectsNullVersion()
    {
        VersionRange range = VersionRange.CreateUniversal();

        Assert.That(
            delegate
            {
                range.Explain(null!);
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
