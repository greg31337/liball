using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodePdfBidiRunTests
{
    [Test]
    public void GetBidiRuns_GroupsContiguousDirectionHints()
    {
        string text = "A\u0600A";

        UnicodePdfBidiRun[] runs = new UnicodePdfText().GetBidiRuns(text);

        Assert.That(runs, Has.Length.EqualTo(3));
        AssertRun(runs[0], 0, 1, UnicodePdfBidiDirection.LeftToRight);
        AssertRun(runs[1], 1, 2, UnicodePdfBidiDirection.RightToLeft);
        AssertRun(runs[2], 2, 3, UnicodePdfBidiDirection.LeftToRight);
    }

    [Test]
    public void GetBidiRuns_AttachesNeutralTextToNearbyStrongDirections()
    {
        UnicodePdfBidiRun[] leadingNeutral = new UnicodePdfText().GetBidiRuns(" \u0600");
        UnicodePdfBidiRun[] middleNeutral = new UnicodePdfText().GetBidiRuns("A \u0600");

        Assert.That(leadingNeutral, Has.Length.EqualTo(1));
        AssertRun(leadingNeutral[0], 0, 2, UnicodePdfBidiDirection.RightToLeft);

        Assert.That(middleNeutral, Has.Length.EqualTo(2));
        AssertRun(middleNeutral[0], 0, 2, UnicodePdfBidiDirection.LeftToRight);
        AssertRun(middleNeutral[1], 2, 3, UnicodePdfBidiDirection.RightToLeft);
    }

    [Test]
    public void GetBidiRuns_UsesNeutralWhenTextHasNoStrongDirection()
    {
        UnicodePdfBidiRun[] runs = new UnicodePdfText().GetBidiRuns(" () ");

        Assert.That(runs, Has.Length.EqualTo(1));
        AssertRun(runs[0], 0, 4, UnicodePdfBidiDirection.Neutral);
    }

    [Test]
    public void GetBidiRuns_TreatsEuropeanNumbersAsLeftToRight()
    {
        UnicodePdfBidiRun[] runs = new UnicodePdfText().GetBidiRuns("09");

        Assert.That(runs, Has.Length.EqualTo(1));
        AssertRun(runs[0], 0, 2, UnicodePdfBidiDirection.LeftToRight);
    }

    [Test]
    public void GetBidiRuns_AttachesNonspacingMarksToPreviousDirection()
    {
        UnicodePdfBidiRun[] runs = new UnicodePdfText().GetBidiRuns("A\u0301B");

        Assert.That(runs, Has.Length.EqualTo(1));
        AssertRun(runs[0], 0, 3, UnicodePdfBidiDirection.LeftToRight);
    }

    [Test]
    public void GetBidiRuns_PreservesUtf16OffsetsForSupplementaryScalars()
    {
        UnicodePdfBidiRun[] runs = new UnicodePdfText().GetBidiRuns("A\U0001F600\u0628");

        Assert.That(runs, Has.Length.EqualTo(2));
        AssertRun(runs[0], 0, 3, UnicodePdfBidiDirection.LeftToRight);
        AssertRun(runs[1], 3, 4, UnicodePdfBidiDirection.RightToLeft);
    }

    [Test]
    public void ResolveLayoutBidiDirections_ReturnsPerScalarDirections()
    {
        UnicodeScalarRecord[] scalars = new UnicodeScalarReader().ReadAll(" A\u0600");

        Assert.That(
            UnicodePdfText.ResolveLayoutBidiDirections(scalars),
            Is.EqualTo(new UnicodePdfBidiDirection[]
            {
                UnicodePdfBidiDirection.LeftToRight,
                UnicodePdfBidiDirection.LeftToRight,
                UnicodePdfBidiDirection.RightToLeft,
            }));
    }

    [Test]
    public void GetBidiRuns_ReturnsNoRunsForEmptyText()
    {
        Assert.That(new UnicodePdfText().GetBidiRuns(""), Is.Empty);
    }

    [Test]
    public void BidiRunHelpers_RejectInvalidArguments()
    {
        Assert.Throws<ArgumentNullException>(() => new UnicodePdfText().GetBidiRuns(null!));
        Assert.Throws<ArgumentNullException>(() => UnicodePdfText.ResolveLayoutBidiDirections(null!));
        Assert.Throws<ArgumentNullException>(() => UnicodePdfText.FindNearestStrongBidiDirection(null!, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => UnicodePdfText.FindNearestStrongBidiDirection(new UnicodeScalarReader().ReadAll("A"), -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => UnicodePdfText.FindNearestStrongBidiDirection(new UnicodeScalarReader().ReadAll("A"), 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new UnicodePdfBidiRun(-1, 0, UnicodePdfBidiDirection.LeftToRight));
        Assert.Throws<ArgumentException>(() => new UnicodePdfBidiRun(2, 1, UnicodePdfBidiDirection.LeftToRight));
    }

    static void AssertRun(
        UnicodePdfBidiRun run,
        int startUtf16Index,
        int endUtf16Index,
        UnicodePdfBidiDirection direction)
    {
        Assert.That(run.StartUtf16Index, Is.EqualTo(startUtf16Index));
        Assert.That(run.EndUtf16Index, Is.EqualTo(endUtf16Index));
        Assert.That(run.Utf16Length, Is.EqualTo(endUtf16Index - startUtf16Index));
        Assert.That(run.Direction, Is.EqualTo(direction));
        Assert.That(run.Contains(startUtf16Index), Is.True);
        Assert.That(run.Contains(endUtf16Index), Is.False);
    }
}
