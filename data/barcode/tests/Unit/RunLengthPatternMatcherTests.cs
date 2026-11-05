namespace barcode.Tests;

public class RunLengthPatternMatcherTests
{
    [Test]
    public void ForwardIteratorReportsColorsLengthsAndEndPosition()
    {
        BitRow row = RunLength.ExpandRuns(new int[] { 2, 3, 1, 2 }, true);
        RunIterator iterator = new RunIterator(row, 0, 1);

        Assert.That(iterator.GetRunColor(), Is.True);
        Assert.That(iterator.ReadNextRun(), Is.EqualTo(2));
        Assert.That(iterator.GetRunColor(), Is.False);
        Assert.That(iterator.ReadNextRun(), Is.EqualTo(3));
        Assert.That(iterator.GetRunColor(), Is.True);
        Assert.That(iterator.ReadNextRun(), Is.EqualTo(1));
        Assert.That(iterator.GetRunColor(), Is.False);
        Assert.That(iterator.ReadNextRun(), Is.EqualTo(2));

        Assert.Multiple(() =>
        {
            Assert.That(iterator.HasNextRun(), Is.False);
            Assert.That(iterator.GetPosition(), Is.EqualTo(8));
            Assert.That(() => iterator.GetRunColor(), Throws.TypeOf<BarcodeValidationException>().With.Message.EqualTo("Run iterator is exhausted."));
            Assert.That(() => iterator.ReadNextRun(), Throws.TypeOf<BarcodeValidationException>().With.Message.EqualTo("Run iterator is exhausted."));
        });
    }

    [Test]
    public void ReverseIteratorTraversesRunsFromRightToLeft()
    {
        BitRow row = RunLength.ExpandRuns(new int[] { 2, 3, 1, 2 }, true);
        RunIterator iterator = new RunIterator(row, 7, -1);

        Assert.That(iterator.GetRunColor(), Is.False);
        Assert.That(iterator.ReadNextRun(), Is.EqualTo(2));
        Assert.That(iterator.GetRunColor(), Is.True);
        Assert.That(iterator.ReadNextRun(), Is.EqualTo(1));
        Assert.That(iterator.GetRunColor(), Is.False);
        Assert.That(iterator.ReadNextRun(), Is.EqualTo(3));
        Assert.That(iterator.GetRunColor(), Is.True);
        Assert.That(iterator.ReadNextRun(), Is.EqualTo(2));

        Assert.Multiple(() =>
        {
            Assert.That(iterator.HasNextRun(), Is.False);
            Assert.That(iterator.GetPosition(), Is.EqualTo(-1));
        });
    }

    [Test]
    public void IteratorCanBeginInsideARun()
    {
        BitRow row = RunLength.ExpandRuns(new int[] { 3, 2 }, true);
        RunIterator forward = new RunIterator(row, 1, 1);
        RunIterator reverse = new RunIterator(row, 3, -1);

        Assert.Multiple(() =>
        {
            Assert.That(forward.ReadNextRun(), Is.EqualTo(2));
            Assert.That(reverse.ReadNextRun(), Is.EqualTo(1));
        });
    }

    [Test]
    public void IteratorRejectsNullSourceInvalidDirectionAndInvalidStart()
    {
        BitRow row = new BitRow(3);

        Assert.Multiple(() =>
        {
            Assert.That(() => new RunIterator(null!, 0, 1), Throws.TypeOf<BarcodeValidationException>().With.Message.EqualTo("Run source must not be null."));
            Assert.That(() => new RunIterator(row, 0, 0), Throws.TypeOf<BarcodeValidationException>().With.Message.EqualTo("Run direction must be forward or reverse."));
            Assert.That(() => new RunIterator(row, -1, 1), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => new RunIterator(row, 3, -1), Throws.TypeOf<BarcodeValidationException>());
        });
    }

    [Test]
    public void RunExtractionSupportsAllLimitedForwardAndReverseTraversal()
    {
        BitRow row = RunLength.ExpandRuns(new int[] { 2, 3, 1, 2 }, true);

        Assert.Multiple(() =>
        {
            Assert.That(RunLength.ExtractAll(row), Is.EqualTo(new int[] { 2, 3, 1, 2 }));
            Assert.That(RunLength.ExtractForward(row, 0, 2), Is.EqualTo(new int[] { 2, 3 }));
            Assert.That(RunLength.ExtractForward(row, 1, 3), Is.EqualTo(new int[] { 1, 3, 1 }));
            Assert.That(RunLength.ExtractReverse(row, 7, 4), Is.EqualTo(new int[] { 2, 1, 3, 2 }));
            Assert.That(RunLength.ExtractReverse(row, 6, 2), Is.EqualTo(new int[] { 1, 1 }));
        });
    }

    [Test]
    public void RunExpansionSupportsLightStartAndDoesNotAliasInput()
    {
        int[] runs = new int[] { 1, 2, 1 };

        BitRow row = RunLength.ExpandRuns(runs, false);
        runs[1] = 1;

        Assert.That(row.CopyBits(), Is.EqualTo(new bool[] { false, true, true, false }));
    }

    [Test]
    public void RunHelpersRejectMalformedAndOverflowingInputs()
    {
        BitRow row = new BitRow(3);

        Assert.Multiple(() =>
        {
            Assert.That(() => RunLength.ExtractAll(null!), Throws.TypeOf<BarcodeValidationException>().With.Message.EqualTo("Run source must not be null."));
            Assert.That(() => RunLength.ExtractForward(row, 0, 0), Throws.TypeOf<BarcodeValidationException>().With.Message.EqualTo("Maximum run count must be greater than zero."));
            Assert.That(() => RunLength.ExtractReverse(row, 0, -1), Throws.TypeOf<BarcodeValidationException>().With.Message.EqualTo("Maximum run count must be greater than zero."));
            Assert.That(() => RunLength.ExpandRuns(null!, true), Throws.TypeOf<BarcodeValidationException>().With.Message.EqualTo("Run lengths must not be null."));
            Assert.That(() => RunLength.ExpandRuns(new int[0], true), Throws.TypeOf<BarcodeValidationException>().With.Message.EqualTo("At least one run length is required."));
            Assert.That(() => RunLength.ExpandRuns(new int[] { 1, 0 }, true), Throws.TypeOf<BarcodeValidationException>().With.Message.EqualTo("Run lengths must be greater than zero."));
            Assert.That(() => RunLength.ExpandRuns(new int[] { int.MaxValue, 1 }, true), Throws.TypeOf<BarcodeValidationException>().With.Message.EqualTo("Integer addition exceeds the supported range."));
        });
    }

    [Test]
    public void NormalizedVarianceIsZeroForExactAndScaledPatterns()
    {
        Assert.Multiple(() =>
        {
            Assert.That(PatternMatcher.CalculateNormalizedVariance(new int[] { 1, 2, 1 }, new int[] { 1, 2, 1 }, 0.0), Is.EqualTo(0.0));
            Assert.That(PatternMatcher.CalculateNormalizedVariance(new int[] { 2, 4, 2 }, new int[] { 1, 2, 1 }, 0.0), Is.EqualTo(0.0));
            Assert.That(PatternMatcher.IsPatternMatch(new int[] { 3, 6, 3 }, new int[] { 1, 2, 1 }, 0.0, 0.0), Is.True);
        });
    }

    [Test]
    public void NormalizedVarianceMeasuresNoiseAndEnforcesIndividualLimit()
    {
        double variance = PatternMatcher.CalculateNormalizedVariance(new int[] { 2, 3, 2 }, new int[] { 1, 2, 1 }, 0.5);
        double rejected = PatternMatcher.CalculateNormalizedVariance(new int[] { 1, 5, 1 }, new int[] { 1, 2, 1 }, 0.5);

        Assert.Multiple(() =>
        {
            Assert.That(variance, Is.EqualTo(1.0 / 7.0).Within(0.000000001));
            Assert.That(double.IsPositiveInfinity(rejected), Is.True);
            Assert.That(PatternMatcher.IsPatternMatch(new int[] { 2, 3, 2 }, new int[] { 1, 2, 1 }, 0.15, 0.5), Is.True);
            Assert.That(PatternMatcher.IsPatternMatch(new int[] { 2, 3, 2 }, new int[] { 1, 2, 1 }, 0.14, 0.5), Is.False);
        });
    }

    [Test]
    public void PatternSearchFindsSameBoundsInBothDirections()
    {
        BitRow row = RunLength.ExpandRuns(new int[] { 2, 2, 1, 3, 2 }, false);

        int[]? forward = PatternMatcher.FindForward(row, new int[] { 2, 1, 3 }, true, 0.0, 0.0);
        int[]? reverse = PatternMatcher.FindReverse(row, new int[] { 3, 1, 2 }, true, 0.0, 0.0);

        Assert.Multiple(() =>
        {
            Assert.That(forward, Is.EqualTo(new int[] { 2, 8 }));
            Assert.That(reverse, Is.EqualTo(new int[] { 2, 8 }));
        });
    }

    [Test]
    public void PatternSearchReturnsNullWhenNoRunWindowMatches()
    {
        BitRow row = RunLength.ExpandRuns(new int[] { 2, 2, 1, 3, 2 }, false);

        Assert.Multiple(() =>
        {
            Assert.That(PatternMatcher.FindForward(row, new int[] { 4, 4, 4 }, true, 0.0, 0.0), Is.Null);
            Assert.That(PatternMatcher.FindReverse(row, new int[] { 4, 4, 4 }, true, 0.0, 0.0), Is.Null);
        });
    }

    [Test]
    public void PatternMatcherRejectsMalformedPatternsAndVarianceLimits()
    {
        BitRow row = new BitRow(3);

        Assert.Multiple(() =>
        {
            Assert.That(() => PatternMatcher.CalculateNormalizedVariance(null!, new int[] { 1 }, 0.5), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => PatternMatcher.CalculateNormalizedVariance(new int[] { 1 }, null!, 0.5), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => PatternMatcher.CalculateNormalizedVariance(new int[0], new int[0], 0.5), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => PatternMatcher.CalculateNormalizedVariance(new int[] { 1 }, new int[] { 1, 1 }, 0.5), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => PatternMatcher.CalculateNormalizedVariance(new int[] { 0 }, new int[] { 1 }, 0.5), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => PatternMatcher.CalculateNormalizedVariance(new int[] { 1 }, new int[] { -1 }, 0.5), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => PatternMatcher.CalculateNormalizedVariance(new int[] { int.MaxValue, 1 }, new int[] { 1, 1 }, 0.5), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => PatternMatcher.CalculateNormalizedVariance(new int[] { 1 }, new int[] { 1 }, -0.1), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => PatternMatcher.CalculateNormalizedVariance(new int[] { 1 }, new int[] { 1 }, double.NaN), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => PatternMatcher.IsPatternMatch(new int[] { 1 }, new int[] { 1 }, -0.1, 0.5), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => PatternMatcher.SumPositiveRuns(null!), Throws.TypeOf<BarcodeValidationException>().With.Message.EqualTo("Run pattern must not be null."));
            Assert.That(() => PatternMatcher.SumPositiveRuns(new int[0]), Throws.TypeOf<BarcodeValidationException>().With.Message.EqualTo("Run pattern must not be empty."));
            Assert.That(() => PatternMatcher.FindForward(null!, new int[] { 1 }, true, 0.1, 0.5), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => PatternMatcher.FindForward(row, new int[0], true, 0.1, 0.5), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => PatternMatcher.FindForward(row, new int[] { 1 }, true, -0.1, 0.5), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => PatternMatcher.FindForward(row, new int[] { 1 }, true, 0.1, -0.5), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => PatternMatcher.FindForward(row, new int[] { 1 }, true, 0.1, double.NaN), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => PatternMatcher.FindReverse(row, new int[] { 1 }, true, double.NaN, 0.5), Throws.TypeOf<BarcodeValidationException>());
        });
    }
}
