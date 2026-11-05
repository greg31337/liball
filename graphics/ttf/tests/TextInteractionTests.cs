namespace ttf.Tests;

public class TextInteractionTests
{
    [Test]
    public void PlacesCaretStopsAtOriginalGraphemeBoundaries()
    {
        TextLayout layout = Prepare(
            "a\U0001F600b",
            new double[] { 2, 4, 2 },
            new int[] { 0, 0, 0 },
            new int[] { 0, 0, 0 },
            Array.Empty<PreparedRunMetrics>(),
            new TextLayoutOptions(),
            10).Layout(8);

        Assert.Multiple(() =>
        {
            Assert.That(layout.GetCaretStopCount(), Is.EqualTo(4));
            Assert.That(CaretUtf16Indexes(layout),
                Is.EqualTo(new int[] { 0, 1, 3, 4 }));
            Assert.That(CaretXPositions(layout),
                Is.EqualTo(new double[] { 0, 2, 6, 8 }));
            Assert.That(layout.HitTest(5.5, 0).GetUtf16Index(), Is.EqualTo(3));
            Assert.That(layout.HitTest(5.5, 0).GetDistance(), Is.EqualTo(0.5));
        });
    }

    [Test]
    public void SuppressesCaretAndSelectionInsideMultiClusterLigatures()
    {
        FontInstance font = FontInstanceTests.CreateFace()
            .CreateDefaultInstance();
        PreparedGlyphMetrics ligature = PreparedGlyphMetrics.Create(
            12,
            0,
            2,
            0,
            0,
            0,
            0,
            4,
            0,
            TextInkBounds.Create(0, -1, 4, 2));
        PreparedRunMetrics run = new PreparedRunMetrics(
            0,
            font,
            3,
            1,
            0,
            new PreparedGlyphMetrics[] { ligature });
        TextLayout layout = Prepare(
            "fi",
            new double[] { 2, 2 },
            new int[] { 0, 0 },
            new int[] { 0, 0 },
            new PreparedRunMetrics[] { run },
            new TextLayoutOptions(),
            10).Layout(4);

        TextSelectionRectangle[] selection = layout.GetSelection(0, 2);

        Assert.Multiple(() =>
        {
            Assert.That(CaretUtf16Indexes(layout), Is.EqualTo(new int[] { 0, 2 }));
            Assert.That(layout.HitTest(2, 0).GetUtf16Index(), Is.Zero);
            Assert.That(selection.Length, Is.EqualTo(1));
            Assert.That(selection[0].GetX(), Is.Zero);
            Assert.That(selection[0].GetWidth(), Is.EqualTo(4));
            Assert.That(selection[0].GetY(), Is.EqualTo(-1));
            Assert.That(selection[0].GetHeight(), Is.EqualTo(4));
            Assert.Throws<ArgumentException>(() => layout.GetSelection(1, 1));
        });
    }

    [Test]
    public void KeepsDualBidiCaretsAndDiscontiguousSelections()
    {
        TextLayout layout = Prepare(
            "abc",
            new double[] { 1, 1, 1 },
            new int[] { 0, 1, 1 },
            new int[] { 0, 1, 1 },
            Array.Empty<PreparedRunMetrics>(),
            new TextLayoutOptions(),
            10).Layout(3);

        TextSelectionRectangle[] firstTwo = layout.GetSelection(0, 2);

        Assert.Multiple(() =>
        {
            Assert.That(layout.GetCaretStopCount(), Is.EqualTo(5));
            Assert.That(CaretPositionsAt(layout, 1),
                Is.EqualTo(new double[] { 1, 3 }));
            Assert.That(layout.HitTest(2.1, 0).GetUtf16Index(), Is.EqualTo(2));
            Assert.That(firstTwo.Length, Is.EqualTo(2));
            Assert.That(firstTwo[0].GetX(), Is.Zero);
            Assert.That(firstTwo[0].GetWidth(), Is.EqualTo(1));
            Assert.That(firstTwo[1].GetX(), Is.EqualTo(2));
            Assert.That(firstTwo[1].GetWidth(), Is.EqualTo(1));
        });
    }

    [Test]
    public void IncludesJustificationInCaretAndSelectionGeometry()
    {
        TextLayoutOptions options = new TextLayoutOptions();
        options.SetAlignment(TextAlignment.Justify);
        options.SetJustifyLastLine(true);
        TextLayout layout = Prepare(
            "a b",
            new double[] { 1, 1, 1 },
            new int[] { 0, 0, 0 },
            new int[] { 0, 0, 0 },
            Array.Empty<PreparedRunMetrics>(),
            options,
            10).Layout(6);

        TextSelectionRectangle[] space = layout.GetSelection(1, 1);

        Assert.Multiple(() =>
        {
            Assert.That(CaretXPositions(layout),
                Is.EqualTo(new double[] { 0, 1, 5, 6 }));
            Assert.That(space.Length, Is.EqualTo(1));
            Assert.That(space[0].GetX(), Is.EqualTo(1));
            Assert.That(space[0].GetWidth(), Is.EqualTo(4));
        });
    }

    [Test]
    public void HitTestsAndSelectsOnTheVerticalInlineAxis()
    {
        FontInstance font = FontInstanceTests.CreateFace()
            .CreateDefaultInstance();
        PreparedRunMetrics[] metrics = new PreparedRunMetrics[]
        {
            Run(0, font, Glyph(1, 0)),
            Run(1, font, Glyph(2, 1))
        };
        TextLayoutOptions options = new TextLayoutOptions();
        options.SetWritingMode(TextWritingMode.VerticalTopToBottom);
        TextLayout layout = Prepare(
            "ab",
            new double[] { 1, 1 },
            new int[] { 0, 1 },
            new int[] { 0, 0 },
            metrics,
            options,
            10).Layout(2);

        TextSelectionRectangle[] first = layout.GetSelection(0, 1);

        Assert.Multiple(() =>
        {
            Assert.That(CaretYPositions(layout),
                Is.EqualTo(new double[] { 0, -1, -2 }));
            Assert.That(layout.HitTest(0, -1.8).GetUtf16Index(), Is.EqualTo(2));
            Assert.That(first.Length, Is.EqualTo(1));
            Assert.That(first[0].GetX(), Is.EqualTo(-1));
            Assert.That(first[0].GetWidth(), Is.EqualTo(4));
            Assert.That(first[0].GetY(), Is.EqualTo(-1));
            Assert.That(first[0].GetHeight(), Is.EqualTo(1));
        });
    }

    [Test]
    public void KeepsSelectionAndHitTestingAttachedToReflowedLines()
    {
        TextLayout layout = Prepare(
            "a b",
            new double[] { 1, 1, 1 },
            new int[] { 0, 0, 0 },
            new int[] { 0, 0, 0 },
            Array.Empty<PreparedRunMetrics>(),
            new TextLayoutOptions(),
            10).Layout(1);

        TextSelectionRectangle[] selection = layout.GetSelection(0, 3);

        Assert.Multiple(() =>
        {
            Assert.That(layout.GetLineCount(), Is.EqualTo(2));
            Assert.That(selection.Length, Is.EqualTo(2));
            Assert.That(selection[0].GetLineIndex(), Is.Zero);
            Assert.That(selection[0].GetUtf16Start(), Is.Zero);
            Assert.That(selection[1].GetLineIndex(), Is.EqualTo(1));
            Assert.That(selection[1].GetUtf16Start(), Is.EqualTo(2));
            Assert.That(layout.HitTest(0.8, -9.8).GetLineIndex(), Is.EqualTo(1));
            Assert.That(layout.HitTest(0.8, -9.8).GetUtf16Index(), Is.EqualTo(3));
        });
    }

    [Test]
    public void ValidatesHitCoordinatesCaretIndexesAndSelectionRanges()
    {
        TextLayout layout = Prepare(
            "ab",
            new double[] { 1, 1 },
            new int[] { 0, 0 },
            new int[] { 0, 0 },
            Array.Empty<PreparedRunMetrics>(),
            new TextLayoutOptions(),
            10).Layout(2);

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                layout.GetCaretStop(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                layout.HitTest(double.NaN, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                layout.HitTest(0, double.PositiveInfinity));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                layout.GetSelection(-1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                layout.GetSelection(0, 3));
            Assert.That(layout.GetSelection(1, 0), Is.Empty);
        });
    }

    [Test]
    public void SuppressesExplicitShapingUnsafeBoundaries()
    {
        string text = "ab";
        TextBreakMap breaks = new TextBreakMap(
            new int[] { 0, 1 },
            new int[] { 1, 1 },
            new int[] { 0, 1 },
            new int[] { 1, 1 },
            new bool[2],
            new bool[2],
            new bool[2],
            new bool[2],
            new bool[2],
            new bool[] { false, true, false },
            new TextBreakKind[]
            {
                TextBreakKind.Prohibited,
                TextBreakKind.Prohibited,
                TextBreakKind.Mandatory
            },
            text.Length);
        PreparedText prepared = PreparedText.Create(
            text,
            breaks,
            new double[] { 1, 1 },
            new int[] { 0, 0 },
            new int[] { 0, 0 },
            new TextLayoutOptions(),
            Array.Empty<TextJustificationOpportunity>(),
            Array.Empty<PreparedRunMetrics>(),
            1,
            1,
            10);

        TextLayout layout = prepared.Layout(2);

        Assert.Multiple(() =>
        {
            Assert.That(CaretUtf16Indexes(layout), Is.EqualTo(new int[] { 0, 2 }));
            Assert.Throws<ArgumentException>(() => layout.GetSelection(1, 1));
        });
    }

    static PreparedGlyphMetrics Glyph(int glyphId, int clusterIndex)
    {
        return PreparedGlyphMetrics.Create(
            glyphId,
            clusterIndex,
            0,
            0,
            0,
            0,
            0,
            -1,
            TextInkBounds.Create(0, 0, 1, 1));
    }

    static PreparedRunMetrics Run(
        int sourceRunIndex,
        FontInstance font,
        PreparedGlyphMetrics glyph)
    {
        return new PreparedRunMetrics(
            sourceRunIndex,
            font,
            3,
            1,
            0,
            new PreparedGlyphMetrics[] { glyph });
    }

    static PreparedText Prepare(
        string text,
        double[] advances,
        int[] sourceRunIndexes,
        int[] bidiLevels,
        PreparedRunMetrics[] runMetrics,
        TextLayoutOptions options,
        double lineStep)
    {
        return PreparedText.Create(
            text,
            Plan(text, options.AllowsEmergencyBreaks()),
            advances,
            sourceRunIndexes,
            bidiLevels,
            options,
            Array.Empty<TextJustificationOpportunity>(),
            runMetrics,
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

    static int[] CaretUtf16Indexes(TextLayout layout)
    {
        int[] values = new int[layout.GetCaretStopCount()];
        int index = 0;
        while (index < values.Length)
        {
            values[index] = layout.GetCaretStop(index).GetUtf16Index();
            index++;
        }

        return values;
    }

    static double[] CaretXPositions(TextLayout layout)
    {
        double[] values = new double[layout.GetCaretStopCount()];
        int index = 0;
        while (index < values.Length)
        {
            values[index] = layout.GetCaretStop(index).GetX();
            index++;
        }

        return values;
    }

    static double[] CaretYPositions(TextLayout layout)
    {
        double[] values = new double[layout.GetCaretStopCount()];
        int index = 0;
        while (index < values.Length)
        {
            values[index] = layout.GetCaretStop(index).GetY();
            index++;
        }

        return values;
    }

    static double[] CaretPositionsAt(TextLayout layout, int utf16Index)
    {
        System.Collections.Generic.List<double> positions =
            new System.Collections.Generic.List<double>();
        int index = 0;
        while (index < layout.GetCaretStopCount())
        {
            TextCaretStop stop = layout.GetCaretStop(index);
            if (stop.GetUtf16Index() == utf16Index)
            {
                positions.Add(stop.GetX());
            }

            index++;
        }

        positions.Sort();
        return positions.ToArray();
    }
}
