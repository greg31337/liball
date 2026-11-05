namespace ttf.Tests;

public class TextLineConstructorTests
{
    [Test]
    public void ReordersHorizontalRunsByBidiLevelAndBuildsBothMappings()
    {
        TextBreakMap breaks = PlanBreaks("abcd");
        TextLayoutOptions options = new TextLayoutOptions();
        GreedyLineFit fit = Fit(
            breaks,
            new double[] { 2, 3, 4, 1 },
            10,
            options).GetLine(0);
        TextLineRunFragment[] fragments = new TextLineRunFragment[]
        {
            Fragment(3, 3, 4, 0, 1),
            Fragment(1, 1, 2, 1, 3),
            Fragment(0, 0, 1, 0, 2),
            Fragment(2, 2, 3, 1, 4)
        };

        TextLine line = Construct(fit, breaks, fragments, options, 7);

        Assert.Multiple(() =>
        {
            Assert.That(line.GetRunCount(), Is.EqualTo(4));
            Assert.That(SourceRunIndexes(line), Is.EqualTo(new int[] { 0, 2, 1, 3 }));
            Assert.That(RunXPositions(line), Is.EqualTo(new double[] { 0, 2, 6, 9 }));
            Assert.That(line.GetVisualRun(0).GetY(), Is.EqualTo(7));
            Assert.That(line.GetVisualRun(1).GetDirection(),
                Is.EqualTo(ShapingDirection.RightToLeft));
            Assert.That(line.GetVisualRunIndex(0), Is.Zero);
            Assert.That(line.GetVisualRunIndex(1), Is.EqualTo(2));
            Assert.That(line.GetVisualRunIndex(2), Is.EqualTo(1));
            Assert.That(line.GetLogicalRunIndex(2), Is.EqualTo(1));
            Assert.That(line.GetLogicalRun(1).GetSourceRunIndex(), Is.EqualTo(1));
            Assert.That(line.GetUtf16Start(), Is.Zero);
            Assert.That(line.GetUtf16Length(), Is.EqualTo(4));
            Assert.That(line.GetAdvance(), Is.EqualTo(10));
        });

        Assert.That(fragments[0].GetSourceRunIndex(), Is.EqualTo(3));
    }

    [Test]
    public void AppliesNestedBidiLevelReversalPerLine()
    {
        TextBreakMap breaks = PlanBreaks("abcdef");
        TextLayoutOptions options = new TextLayoutOptions();
        GreedyLineFit fit = Fit(
            breaks,
            UniformAdvances(6, 1),
            6,
            options).GetLine(0);
        int[] levels = new int[] { 0, 1, 2, 2, 1, 0 };
        TextLineRunFragment[] fragments = new TextLineRunFragment[6];
        int index = 0;
        while (index < fragments.Length)
        {
            fragments[index] = Fragment(index, index, index + 1, levels[index], 1);
            index++;
        }

        TextLine line = Construct(fit, breaks, fragments, options, 0);

        Assert.Multiple(() =>
        {
            Assert.That(SourceRunIndexes(line),
                Is.EqualTo(new int[] { 0, 4, 2, 3, 1, 5 }));
            Assert.That(RunXPositions(line),
                Is.EqualTo(new double[] { 0, 1, 2, 3, 4, 5 }));
            Assert.That(line.GetVisualRunIndex(4), Is.EqualTo(1));
            Assert.That(line.GetLogicalRunIndex(4), Is.EqualTo(1));
        });
    }

    [Test]
    public void ConstructsTopToBottomAndBottomToTopVerticalLines()
    {
        TextBreakMap breaks = PlanBreaks("abc");
        double[] advances = new double[] { 2, 3, 4 };
        TextLineRunFragment[] fragments = new TextLineRunFragment[]
        {
            Fragment(0, 0, 1, 0, 2),
            Fragment(1, 1, 2, 1, 3),
            Fragment(2, 2, 3, 1, 4)
        };
        TextLayoutOptions top = new TextLayoutOptions();
        top.SetWritingMode(TextWritingMode.VerticalTopToBottom);
        TextLayoutOptions bottom = new TextLayoutOptions();
        bottom.SetWritingMode(TextWritingMode.VerticalBottomToTop);
        GreedyLineFit topFit = Fit(breaks, advances, 9, top).GetLine(0);
        GreedyLineFit bottomFit = Fit(breaks, advances, 9, bottom).GetLine(0);

        TextLine topLine = Construct(topFit, breaks, fragments, top, 12);
        TextLine bottomLine = Construct(bottomFit, breaks, fragments, bottom, 12);

        Assert.Multiple(() =>
        {
            Assert.That(SourceRunIndexes(topLine), Is.EqualTo(new int[] { 0, 2, 1 }));
            Assert.That(RunYPositions(topLine), Is.EqualTo(new double[] { 0, -2, -6 }));
            Assert.That(RunYPositions(bottomLine), Is.EqualTo(new double[] { 0, 2, 6 }));
            Assert.That(topLine.GetVisualRun(0).GetX(), Is.EqualTo(12));
            Assert.That(bottomLine.GetVisualRun(2).GetX(), Is.EqualTo(12));
            Assert.That(topLine.GetVisualRun(1).GetDirection(),
                Is.EqualTo(ShapingDirection.TopToBottom));
            Assert.That(bottomLine.GetVisualRun(1).GetDirection(),
                Is.EqualTo(ShapingDirection.BottomToTop));
            Assert.That(topLine.GetWritingMode(),
                Is.EqualTo(TextWritingMode.VerticalTopToBottom));
        });
    }

    [Test]
    public void AppliesAlignmentOffsetAlongTheSelectedInlineAxis()
    {
        TextBreakMap breaks = PlanBreaks("ab");
        TextLineRunFragment[] fragments = new TextLineRunFragment[]
        {
            Fragment(0, 0, 1, 0, 2),
            Fragment(1, 1, 2, 0, 3)
        };
        TextLayoutOptions horizontal = new TextLayoutOptions();
        horizontal.SetAlignment(TextAlignment.End);
        TextLayoutOptions vertical = new TextLayoutOptions();
        vertical.SetAlignment(TextAlignment.End);
        vertical.SetWritingMode(TextWritingMode.VerticalTopToBottom);
        GreedyLineFit horizontalFit = Fit(
            breaks,
            new double[] { 2, 3 },
            10,
            horizontal).GetLine(0);
        GreedyLineFit verticalFit = Fit(
            breaks,
            new double[] { 2, 3 },
            10,
            vertical).GetLine(0);

        TextLine horizontalLine = Construct(
            horizontalFit, breaks, fragments, horizontal, 4);
        TextLine verticalLine = Construct(
            verticalFit, breaks, fragments, vertical, 4);

        Assert.Multiple(() =>
        {
            Assert.That(horizontalLine.GetInlineOffset(), Is.EqualTo(5));
            Assert.That(horizontalLine.GetVisualRun(0).GetX(), Is.EqualTo(5));
            Assert.That(horizontalLine.GetVisualRun(0).GetY(), Is.EqualTo(4));
            Assert.That(verticalLine.GetInlineOffset(), Is.EqualTo(5));
            Assert.That(verticalLine.GetVisualRun(0).GetX(), Is.EqualTo(4));
            Assert.That(verticalLine.GetVisualRun(0).GetY(), Is.EqualTo(-5));
        });
    }

    [Test]
    public void CarriesJustificationAdjustmentsIntoFollowingRunPositions()
    {
        TextBreakMap breaks = PlanBreaks("a b");
        TextLayoutOptions options = new TextLayoutOptions();
        options.SetAlignment(TextAlignment.Justify);
        GreedyLineFit fit = Fit(
            breaks,
            UniformAdvances(3, 1),
            6,
            options).GetLine(0);
        LineJustificationPlan justification = TextJustificationPlanner.Plan(
            fit,
            false,
            breaks,
            6,
            options,
            Array.Empty<TextJustificationOpportunity>());
        TextLineRunFragment[] fragments = new TextLineRunFragment[]
        {
            Fragment(0, 0, 1, 0, 1),
            Fragment(1, 1, 2, 0, 1),
            Fragment(2, 2, 3, 0, 1)
        };

        TextLine line = TextLineConstructor.Construct(
            fit,
            breaks,
            fragments,
            justification,
            options,
            0);

        Assert.Multiple(() =>
        {
            Assert.That(line.GetAdvance(), Is.EqualTo(6));
            Assert.That(RunXPositions(line), Is.EqualTo(new double[] { 0, 1, 5 }));
            Assert.That(line.GetVisualRun(1).GetNaturalAdvance(), Is.EqualTo(1));
            Assert.That(line.GetVisualRun(1).GetPositionedAdvance(), Is.EqualTo(4));
        });
    }

    [Test]
    public void PreservesConsumedAndVisibleLogicalRangesAcrossHardBreaks()
    {
        TextBreakMap breaks = PlanBreaks("ab\r\ncd");
        TextLayoutOptions options = new TextLayoutOptions();
        GreedyLineFitResult fits = Fit(
            breaks,
            new double[] { 1, 1, 0, 1, 1 },
            20,
            options);
        GreedyLineFit firstFit = fits.GetLine(0);
        GreedyLineFit secondFit = fits.GetLine(1);
        TextLine first = Construct(
            firstFit,
            breaks,
            new TextLineRunFragment[]
            {
                Fragment(0, 0, 1, 0, 1),
                Fragment(1, 1, 2, 0, 1)
            },
            options,
            0);
        TextLine second = Construct(
            secondFit,
            breaks,
            new TextLineRunFragment[]
            {
                Fragment(2, 3, 4, 0, 1),
                Fragment(3, 4, 5, 0, 1)
            },
            options,
            -10);

        Assert.Multiple(() =>
        {
            Assert.That(first.GetClusterStart(), Is.Zero);
            Assert.That(first.GetClusterEnd(), Is.EqualTo(3));
            Assert.That(first.GetVisibleClusterEnd(), Is.EqualTo(2));
            Assert.That(first.GetUtf16Start(), Is.Zero);
            Assert.That(first.GetUtf16Length(), Is.EqualTo(4));
            Assert.That(first.GetVisibleUtf16Length(), Is.EqualTo(2));
            Assert.That(second.GetUtf16Start(), Is.EqualTo(4));
            Assert.That(second.GetUtf16Length(), Is.EqualTo(2));
            Assert.That(second.GetLinePosition(), Is.EqualTo(-10));
        });
    }

    [Test]
    public void ConstructsTheEmptyLineAfterATerminalHardBreak()
    {
        TextBreakMap breaks = PlanBreaks("a\n");
        TextLayoutOptions options = new TextLayoutOptions();
        GreedyLineFit emptyFit = Fit(
            breaks,
            new double[] { 1, 0 },
            10,
            options).GetLine(1);

        TextLine line = Construct(
            emptyFit,
            breaks,
            Array.Empty<TextLineRunFragment>(),
            options,
            -8);

        Assert.Multiple(() =>
        {
            Assert.That(line.GetRunCount(), Is.Zero);
            Assert.That(line.GetClusterStart(), Is.EqualTo(2));
            Assert.That(line.GetClusterEnd(), Is.EqualTo(2));
            Assert.That(line.GetUtf16Start(), Is.EqualTo(2));
            Assert.That(line.GetUtf16Length(), Is.Zero);
            Assert.That(line.GetAdvance(), Is.Zero);
        });
    }

    [Test]
    public void ValidatesWritingModesFragmentsPlansAndCoordinates()
    {
        TextBreakMap breaks = PlanBreaks("ab");
        TextLayoutOptions options = new TextLayoutOptions();
        GreedyLineFit fit = Fit(
            breaks,
            UniformAdvances(2, 1),
            2,
            options).GetLine(0);
        LineJustificationPlan plan = CreatePlan(fit, breaks, options);
        TextLineRunFragment valid = Fragment(0, 0, 1, 0, 1);

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                options.SetWritingMode((TextWritingMode)99));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                Fragment(-1, 0, 1, 0, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                Fragment(0, 0, 0, 0, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                Fragment(0, 0, 1, 126, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                Fragment(0, 0, 1, 0, double.NaN));
            Assert.Throws<ArgumentNullException>(() =>
                TextLineConstructor.Construct(
                    null!, breaks, Array.Empty<TextLineRunFragment>(),
                    plan, options, 0));
            Assert.Throws<ArgumentNullException>(() =>
                TextLineConstructor.Construct(
                    fit, null, Array.Empty<TextLineRunFragment>(),
                    plan, options, 0));
            Assert.Throws<ArgumentNullException>(() =>
                TextLineConstructor.Construct(
                    fit, breaks, null!, plan, options, 0));
            Assert.Throws<ArgumentNullException>(() =>
                TextLineConstructor.Construct(
                    fit, breaks, Array.Empty<TextLineRunFragment>(),
                    null!, options, 0));
            Assert.Throws<ArgumentNullException>(() =>
                TextLineConstructor.Construct(
                    fit, breaks, Array.Empty<TextLineRunFragment>(),
                    plan, null, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                TextLineConstructor.Construct(
                    fit, breaks, new TextLineRunFragment[] { valid },
                    plan, options, double.PositiveInfinity));
            Assert.Throws<ArgumentException>(() =>
                TextLineConstructor.Construct(
                    fit, breaks, new TextLineRunFragment[] { null! },
                    plan, options, 0));
            Assert.Throws<ArgumentException>(() =>
                TextLineConstructor.Construct(
                    fit,
                    breaks,
                    new TextLineRunFragment[]
                    {
                        Fragment(0, 0, 2, 0, 1),
                        Fragment(1, 1, 2, 0, 1)
                    },
                    plan,
                    options,
                    0));
            Assert.Throws<ArgumentException>(() =>
                TextLineConstructor.Construct(
                    fit,
                    breaks,
                    new TextLineRunFragment[]
                    {
                        Fragment(0, 1, 3, 0, 1)
                    },
                    plan,
                    options,
                    0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                Construct(
                    fit,
                    breaks,
                    new TextLineRunFragment[] { valid },
                    options,
                    0).GetVisualRun(2));
        });
    }

    static TextLine Construct(
        GreedyLineFit fit,
        TextBreakMap breaks,
        TextLineRunFragment[] fragments,
        TextLayoutOptions options,
        double linePosition)
    {
        return TextLineConstructor.Construct(
            fit,
            breaks,
            fragments,
            CreatePlan(fit, breaks, options),
            options,
            linePosition);
    }

    static LineJustificationPlan CreatePlan(
        GreedyLineFit fit,
        TextBreakMap breaks,
        TextLayoutOptions options)
    {
        return TextJustificationPlanner.Plan(
            fit,
            true,
            breaks,
            fit.GetAdvance(),
            options,
            Array.Empty<TextJustificationOpportunity>());
    }

    static GreedyLineFitResult Fit(
        TextBreakMap breaks,
        double[] advances,
        double maximumAdvance,
        TextLayoutOptions options)
    {
        return GreedyLineFitter.Fit(
            breaks,
            advances,
            maximumAdvance,
            options,
            0,
            1);
    }

    static TextLineRunFragment Fragment(
        int sourceRunIndex,
        int clusterStart,
        int clusterEnd,
        int bidiLevel,
        double advance)
    {
        return TextLineRunFragment.Create(
            sourceRunIndex,
            clusterStart,
            clusterEnd,
            bidiLevel,
            advance);
    }

    static TextBreakMap PlanBreaks(string text)
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
            false,
            clusters.GetClusterCount());
    }

    static double[] UniformAdvances(int count, double value)
    {
        double[] advances = new double[count];
        int index = 0;
        while (index < advances.Length)
        {
            advances[index] = value;
            index++;
        }

        return advances;
    }

    static int[] SourceRunIndexes(TextLine line)
    {
        int[] values = new int[line.GetRunCount()];
        int index = 0;
        while (index < values.Length)
        {
            values[index] = line.GetVisualRun(index).GetSourceRunIndex();
            index++;
        }

        return values;
    }

    static double[] RunXPositions(TextLine line)
    {
        double[] values = new double[line.GetRunCount()];
        int index = 0;
        while (index < values.Length)
        {
            values[index] = line.GetVisualRun(index).GetX();
            index++;
        }

        return values;
    }

    static double[] RunYPositions(TextLine line)
    {
        double[] values = new double[line.GetRunCount()];
        int index = 0;
        while (index < values.Length)
        {
            values[index] = line.GetVisualRun(index).GetY();
            index++;
        }

        return values;
    }
}
