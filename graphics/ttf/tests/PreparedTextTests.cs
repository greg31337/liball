namespace ttf.Tests;

public class PreparedTextTests
{
    [Test]
    public void ReflowsOnePreparedSnapshotWithoutMutatingEarlierLayouts()
    {
        TextLayoutOptions options = new TextLayoutOptions();
        PreparedText prepared = Prepare(
            "aa bb cc",
            UniformAdvances(8, 1),
            UniformIndexes(8, 0),
            UniformIndexes(8, 0),
            options,
            10);

        TextLayout wide = prepared.Layout(8);
        TextLayout wrapped = prepared.Layout(5);
        TextLayout wideAgain = prepared.Layout(8);

        Assert.Multiple(() =>
        {
            Assert.That(wide.GetLineCount(), Is.EqualTo(1));
            Assert.That(wide.GetLine(0).GetClusterEnd(), Is.EqualTo(8));
            Assert.That(wide.GetLine(0).GetAdvance(), Is.EqualTo(8));
            Assert.That(wrapped.GetLineCount(), Is.EqualTo(2));
            Assert.That(wrapped.GetLine(0).GetVisibleClusterEnd(), Is.EqualTo(5));
            Assert.That(wrapped.GetLine(1).GetClusterStart(), Is.EqualTo(6));
            Assert.That(wide.GetLineCount(), Is.EqualTo(1));
            Assert.That(wideAgain.GetLineCount(), Is.EqualTo(1));
            Assert.That(wideAgain.GetLine(0), Is.Not.SameAs(wide.GetLine(0)));
            Assert.That(wide.GetSourceText(), Is.EqualTo("aa bb cc"));
            Assert.That(wrapped.GetMaximumAdvance(), Is.EqualTo(5));
        });
    }

    [Test]
    public void SnapshotsMetricsRunMetadataOpportunitiesAndOptions()
    {
        double[] advances = UniformAdvances(3, 1);
        int[] sourceRuns = new int[] { 10, 11, 12 };
        int[] levels = new int[] { 0, 1, 1 };
        TextLayoutOptions options = new TextLayoutOptions();
        options.SetAlignment(TextAlignment.End);
        TextJustificationOpportunity[] opportunities =
            Array.Empty<TextJustificationOpportunity>();
        PreparedText prepared = Prepare(
            "abc",
            advances,
            sourceRuns,
            levels,
            options,
            10,
            opportunities);

        advances[0] = 99;
        sourceRuns[0] = 99;
        levels[1] = 0;
        options.SetAlignment(TextAlignment.Start);
        options.SetMaximumLines(1);
        options.SetWritingMode(TextWritingMode.VerticalTopToBottom);
        opportunities = new TextJustificationOpportunity[]
        {
            new TextJustificationOpportunity(
                TextJustificationOpportunityKind.Jstf,
                1,
                50,
                0)
        };

        TextLayout layout = prepared.Layout(6);

        Assert.Multiple(() =>
        {
            Assert.That(layout.GetWritingMode(), Is.EqualTo(TextWritingMode.Horizontal));
            Assert.That(layout.GetLine(0).GetInlineOffset(), Is.EqualTo(3));
            Assert.That(layout.GetLine(0).GetAdvance(), Is.EqualTo(3));
            Assert.That(layout.GetLine(0).GetVisualRun(0).GetSourceRunIndex(),
                Is.EqualTo(10));
            Assert.That(SourceRunIndexes(layout.GetLine(0)),
                Is.EqualTo(new int[] { 10, 12, 11 }));
        });
    }

    [Test]
    public void RecomputesJustificationForEachRequestedWidth()
    {
        TextLayoutOptions options = new TextLayoutOptions();
        options.SetAlignment(TextAlignment.Justify);
        options.SetJustifyLastLine(true);
        PreparedText prepared = Prepare(
            "a b",
            UniformAdvances(3, 1),
            new int[] { 0, 1, 2 },
            UniformIndexes(3, 0),
            options,
            10);

        TextLayout six = prepared.Layout(6);
        TextLayout eight = prepared.Layout(8);

        Assert.Multiple(() =>
        {
            Assert.That(six.GetLine(0).GetAdvance(), Is.EqualTo(6));
            Assert.That(eight.GetLine(0).GetAdvance(), Is.EqualTo(8));
            Assert.That(six.GetLine(0).GetVisualRun(2).GetX(), Is.EqualTo(5));
            Assert.That(eight.GetLine(0).GetVisualRun(2).GetX(), Is.EqualTo(7));
            Assert.That(six.GetLine(0).GetVisualRun(2).GetX(), Is.EqualTo(5));
        });
    }

    [Test]
    public void ConstructsIndependentVerticalColumnsFromThePreparedData()
    {
        TextLayoutOptions options = new TextLayoutOptions();
        options.SetAllowEmergencyBreaks(true);
        options.SetWritingMode(TextWritingMode.VerticalTopToBottom);
        PreparedText prepared = Prepare(
            "abcd",
            UniformAdvances(4, 1),
            new int[] { 0, 1, 2, 3 },
            UniformIndexes(4, 0),
            options,
            10);

        TextLayout layout = prepared.Layout(2);

        Assert.Multiple(() =>
        {
            Assert.That(layout.GetLineCount(), Is.EqualTo(2));
            Assert.That(layout.GetLine(0).GetLinePosition(), Is.Zero);
            Assert.That(layout.GetLine(1).GetLinePosition(), Is.EqualTo(-10));
            Assert.That(layout.GetLine(0).GetVisualRun(1).GetY(), Is.EqualTo(-1));
            Assert.That(layout.GetLine(1).GetVisualRun(0).GetX(), Is.EqualTo(-10));
            Assert.That(layout.GetLine(1).GetVisualRun(0).GetDirection(),
                Is.EqualTo(ShapingDirection.TopToBottom));
        });
    }

    [Test]
    public void PreservesTruncationMappingsToOriginalUtf16Text()
    {
        TextLayoutOptions options = new TextLayoutOptions();
        options.SetAllowEmergencyBreaks(true);
        options.SetMaximumLines(1);
        options.SetTrimmingMode(TextTrimmingMode.CharacterEllipsis);
        PreparedText prepared = Prepare(
            "ab\U0001F600cd",
            UniformAdvances(5, 1),
            UniformIndexes(5, 0),
            UniformIndexes(5, 0),
            options,
            10);

        TextLayout layout = prepared.Layout(4);

        Assert.Multiple(() =>
        {
            Assert.That(layout.IsTruncated(), Is.True);
            Assert.That(layout.GetTruncationClusterIndex(), Is.EqualTo(3));
            Assert.That(layout.GetTruncationUtf16Index(), Is.EqualTo(4));
            Assert.That(layout.GetLine(0).GetVisibleUtf16Length(), Is.EqualTo(4));
            Assert.That(layout.GetLine(0).GetAdvance(), Is.EqualTo(4));
        });
    }

    [Test]
    public void HandlesEmptyPreparedTextAndSkipsClustersWithoutSourceRuns()
    {
        TextLayoutOptions options = new TextLayoutOptions();
        PreparedText empty = Prepare(
            string.Empty,
            Array.Empty<double>(),
            Array.Empty<int>(),
            Array.Empty<int>(),
            options,
            10);
        PreparedText skipped = Prepare(
            "abc",
            UniformAdvances(3, 1),
            new int[] { 0, -1, 1 },
            UniformIndexes(3, 0),
            options,
            10);

        TextLayout emptyLayout = empty.Layout(20);
        TextLayout skippedLayout = skipped.Layout(3);

        Assert.Multiple(() =>
        {
            Assert.That(emptyLayout.GetLineCount(), Is.EqualTo(1));
            Assert.That(emptyLayout.GetLine(0).GetRunCount(), Is.Zero);
            Assert.That(emptyLayout.GetLine(0).GetAdvance(), Is.Zero);
            Assert.That(emptyLayout.IsTruncated(), Is.False);
            Assert.That(skippedLayout.GetLine(0).GetRunCount(), Is.EqualTo(2));
            Assert.That(skippedLayout.GetLine(0).GetVisualRun(1).GetClusterStart(),
                Is.EqualTo(2));
        });
        Assert.Throws<InvalidOperationException>(() =>
            emptyLayout.GetTruncationClusterIndex());
    }

    [Test]
    public void ValidatesPreparedInputsAndLayoutWidths()
    {
        TextBreakMap breaks = Plan("a", false);
        TextLayoutOptions options = new TextLayoutOptions();
        TextJustificationOpportunity[] noOpportunities =
            Array.Empty<TextJustificationOpportunity>();

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() =>
                PreparedText.Create(
                    null!,
                    breaks,
                    new double[] { 1 },
                    new int[] { 0 },
                    new int[] { 0 },
                    options,
                    noOpportunities,
                    0,
                    0,
                    1));
            Assert.Throws<ArgumentException>(() =>
                PreparedText.Create(
                    "aa",
                    breaks,
                    new double[] { 1 },
                    new int[] { 0 },
                    new int[] { 0 },
                    options,
                    noOpportunities,
                    0,
                    0,
                    1));
            Assert.Throws<ArgumentException>(() =>
                PreparedText.Create(
                    "a",
                    breaks,
                    Array.Empty<double>(),
                    new int[] { 0 },
                    new int[] { 0 },
                    options,
                    noOpportunities,
                    0,
                    0,
                    1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                PreparedText.Create(
                    "a",
                    breaks,
                    new double[] { 1 },
                    new int[] { -2 },
                    new int[] { 0 },
                    options,
                    noOpportunities,
                    0,
                    0,
                    1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                PreparedText.Create(
                    "a",
                    breaks,
                    new double[] { 1 },
                    new int[] { 0 },
                    new int[] { 126 },
                    options,
                    noOpportunities,
                    0,
                    0,
                    1));
            Assert.Throws<ArgumentException>(() =>
                PreparedText.Create(
                    "a",
                    breaks,
                    new double[] { 1 },
                    new int[] { 0 },
                    new int[] { 0 },
                    options,
                    new TextJustificationOpportunity[] { null! },
                    0,
                    0,
                    1));
        });

        PreparedText prepared = Prepare(
            "a",
            new double[] { 1 },
            new int[] { 0 },
            new int[] { 0 },
            options,
            1);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            prepared.Layout(double.NaN));
        TextLayout layout = prepared.Layout(1);
        Assert.Throws<ArgumentOutOfRangeException>(() => layout.GetLine(1));
    }

    static PreparedText Prepare(
        string text,
        double[] advances,
        int[] sourceRunIndexes,
        int[] bidiLevels,
        TextLayoutOptions options,
        double lineStep)
    {
        return Prepare(
            text,
            advances,
            sourceRunIndexes,
            bidiLevels,
            options,
            lineStep,
            Array.Empty<TextJustificationOpportunity>());
    }

    static PreparedText Prepare(
        string text,
        double[] advances,
        int[] sourceRunIndexes,
        int[] bidiLevels,
        TextLayoutOptions options,
        double lineStep,
        TextJustificationOpportunity[] opportunities)
    {
        return PreparedText.Create(
            text,
            Plan(text, options.AllowsEmergencyBreaks()),
            advances,
            sourceRunIndexes,
            bidiLevels,
            options,
            opportunities,
            1,
            1,
            lineStep);
    }

    static TextBreakMap Plan(string text, bool emergency)
    {
        RuneBuffer input = Utf16Decoder.Decode(
            text,
            Utf16DecodingMode.Strict,
            text.Length);
        UnicodeData data = UnicodeData.LoadDefault();
        GraphemeClusterMap clusters = GraphemeSegmenter.Segment(
            input,
            data,
            input.GetCount(),
            input.GetCount());
        LineBreakMap lineBreaks = LineBreakAlgorithm.Analyze(
            input,
            data,
            input.GetCount());
        return TextBreakPlanner.Plan(
            input,
            clusters,
            lineBreaks,
            emergency,
            clusters.GetClusterCount());
    }

    static double[] UniformAdvances(int count, double value)
    {
        double[] values = new double[count];
        int index = 0;
        while (index < count)
        {
            values[index] = value;
            index++;
        }

        return values;
    }

    static int[] UniformIndexes(int count, int value)
    {
        int[] values = new int[count];
        int index = 0;
        while (index < count)
        {
            values[index] = value;
            index++;
        }

        return values;
    }

    static int[] SourceRunIndexes(TextLine line)
    {
        int[] indexes = new int[line.GetRunCount()];
        int index = 0;
        while (index < indexes.Length)
        {
            indexes[index] = line.GetVisualRun(index).GetSourceRunIndex();
            index++;
        }

        return indexes;
    }
}
