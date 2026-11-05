namespace ttf.Tests;

public class TextLayoutMetricsTests
{
    [Test]
    public void ReportsExactLineRunAndGlyphMetrics()
    {
        FontInstance font = FontInstanceTests.CreateFace()
            .CreateDefaultInstance();
        PreparedGlyphMetrics firstVisual = Glyph(
            20,
            1,
            1,
            0,
            0,
            0,
            4,
            0,
            TextInkBounds.Create(0, -1, 2, 2));
        PreparedGlyphMetrics secondVisual = Glyph(
            10,
            0,
            0,
            1,
            3,
            0,
            3,
            0,
            TextInkBounds.Create(-1, -2, 1, 3));
        PreparedRunMetrics metrics = new PreparedRunMetrics(
            7,
            font,
            5,
            2,
            1,
            new PreparedGlyphMetrics[] { secondVisual, firstVisual });
        TextLayout layout = Prepare(
            "ab",
            new double[] { 3, 4 },
            new int[] { 7, 7 },
            new int[] { 0, 0 },
            new PreparedRunMetrics[] { metrics },
            new TextLayoutOptions(),
            10).Layout(7);
        TextLine line = layout.GetLine(0);
        PositionedRun run = line.GetVisualRun(0);

        Assert.Multiple(() =>
        {
            Assert.That(line.GetAscent(), Is.EqualTo(5));
            Assert.That(line.GetDescent(), Is.EqualTo(2));
            Assert.That(line.GetLineGap(), Is.EqualTo(1));
            Assert.That(line.GetLineHeight(), Is.EqualTo(8));
            AssertBounds(line.GetAdvanceBounds(), 0, -2, 7, 5);
            AssertBounds(line.GetInkBounds(), 0, -2, 4, 3);
            AssertBounds(layout.GetBounds(), 0, -2, 4, 3);
            AssertBounds(layout.GetAdvanceBounds(), 0, -2, 7, 5);
            Assert.That(run.GetAscent(), Is.EqualTo(5));
            Assert.That(run.GetResolvedFont(), Is.SameAs(font));
            Assert.That(run.GetGlyphCount(), Is.EqualTo(2));
            Assert.That(run.GetGlyph(0).GetGlyphId(), Is.EqualTo(20));
            Assert.That(run.GetGlyph(1).GetX(), Is.EqualTo(3));
            Assert.That(run.GetVisualGlyphIndex(0), Is.EqualTo(1));
            Assert.That(run.GetLogicalGlyphIndex(0), Is.EqualTo(1));
            Assert.That(run.GetLogicalGlyph(0).GetGlyphId(), Is.EqualTo(10));
            Assert.That(run.GetGlyph(0).GetResolvedFont(), Is.SameAs(font));
        });
    }

    [Test]
    public void ExposesParagraphRunMappingsAndDistinctResolvedFonts()
    {
        FontInstance firstFont = FontInstanceTests.CreateFace()
            .CreateDefaultInstance();
        FontInstance secondFont = FontInstanceTests.CreateFace()
            .CreateDefaultInstance();
        int[] sourceRuns = new int[] { 0, 1, 2, 3 };
        PreparedRunMetrics[] metrics = new PreparedRunMetrics[]
        {
            Run(0, firstFont, Glyph(1, 0, 0, 0, 0, 0, 1, 0,
                TextInkBounds.Create(0, 0, 1, 1))),
            Run(1, secondFont, Glyph(2, 1, 0, 0, 0, 0, 1, 0,
                TextInkBounds.Create(0, 0, 1, 1))),
            Run(2, secondFont, Glyph(3, 2, 0, 0, 0, 0, 1, 0,
                TextInkBounds.Create(0, 0, 1, 1))),
            Run(3, firstFont, Glyph(4, 3, 0, 0, 0, 0, 1, 0,
                TextInkBounds.Create(0, 0, 1, 1)))
        };
        TextLayout layout = Prepare(
            "abcd",
            UniformAdvances(4, 1),
            sourceRuns,
            new int[] { 0, 1, 1, 0 },
            metrics,
            new TextLayoutOptions(),
            10).Layout(4);

        Assert.Multiple(() =>
        {
            Assert.That(SourceRuns(layout), Is.EqualTo(new int[] { 0, 2, 1, 3 }));
            Assert.That(layout.GetVisualRunIndex(0), Is.Zero);
            Assert.That(layout.GetVisualRunIndex(1), Is.EqualTo(2));
            Assert.That(layout.GetVisualRunIndex(2), Is.EqualTo(1));
            Assert.That(layout.GetLogicalRunIndex(2), Is.EqualTo(1));
            Assert.That(layout.GetLogicalRun(2).GetSourceRunIndex(), Is.EqualTo(2));
            Assert.That(layout.GetResolvedFontCount(), Is.EqualTo(2));
            Assert.That(layout.GetResolvedFont(0), Is.SameAs(firstFont));
            Assert.That(layout.GetResolvedFont(1), Is.SameAs(secondFont));
        });
    }

    [Test]
    public void UnionsExactInkAcrossReflowedBaselines()
    {
        FontInstance font = FontInstanceTests.CreateFace()
            .CreateDefaultInstance();
        PreparedRunMetrics run = new PreparedRunMetrics(
            0,
            font,
            2,
            1,
            1,
            new PreparedGlyphMetrics[]
            {
                Glyph(1, 0, 0, 0, 0, 0, 1, 0,
                    TextInkBounds.Create(-1, -1, 1, 2)),
                Glyph(2, 2, 1, 1, 2, 0, 1, 0,
                    TextInkBounds.Create(0, -2, 2, 1))
            });
        TextLayout layout = Prepare(
            "a b",
            UniformAdvances(3, 1),
            new int[] { 0, 0, 0 },
            new int[] { 0, 0, 0 },
            new PreparedRunMetrics[] { run },
            new TextLayoutOptions(),
            10).Layout(1);

        Assert.Multiple(() =>
        {
            Assert.That(layout.GetLineCount(), Is.EqualTo(2));
            AssertBounds(layout.GetLine(0).GetInkBounds(), -1, -1, 1, 2);
            AssertBounds(layout.GetLine(1).GetInkBounds(), 0, -12, 2, -9);
            AssertBounds(layout.GetInkBounds(), -1, -12, 2, 2);
            Assert.That(layout.GetPositionedRunCount(), Is.EqualTo(2));
            Assert.That(layout.GetResolvedFontCount(), Is.EqualTo(1));
        });
    }

    [Test]
    public void ComputesVerticalAdvanceAndInkBoundsOnTheirActualAxes()
    {
        FontInstance font = FontInstanceTests.CreateFace()
            .CreateDefaultInstance();
        TextLayoutOptions options = new TextLayoutOptions();
        options.SetWritingMode(TextWritingMode.VerticalTopToBottom);
        PreparedRunMetrics[] metrics = new PreparedRunMetrics[]
        {
            Run(0, font, Glyph(1, 0, 0, 0, 0, 0, 0, -1,
                TextInkBounds.Create(0, 0, 1, 1))),
            Run(1, font, Glyph(2, 1, 0, 0, 0, 0, 0, -1,
                TextInkBounds.Create(0, 0, 1, 1)))
        };
        TextLayout layout = Prepare(
            "ab",
            UniformAdvances(2, 1),
            new int[] { 0, 1 },
            new int[] { 0, 0 },
            metrics,
            options,
            10).Layout(2);

        Assert.Multiple(() =>
        {
            AssertBounds(layout.GetAdvanceBounds(), -1, -2, 2, 0);
            AssertBounds(layout.GetInkBounds(), 0, -1, 1, 1);
            Assert.That(layout.GetPositionedRun(1).GetGlyph(0).GetY(),
                Is.EqualTo(-1));
            Assert.That(layout.GetPositionedRun(1).GetGlyph(0).GetYAdvance(),
                Is.EqualTo(-1));
        });
    }

    [Test]
    public void ValidatesPreparedMetricOwnershipMappingsAndIndexes()
    {
        FontInstance font = FontInstanceTests.CreateFace()
            .CreateDefaultInstance();
        PreparedGlyphMetrics glyph = Glyph(
            1,
            0,
            0,
            0,
            0,
            0,
            1,
            0,
            TextInkBounds.Create());
        PreparedRunMetrics run = Run(1, font, glyph);

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() =>
                new PreparedRunMetrics(
                    0,
                    font,
                    1,
                    1,
                    0,
                    new PreparedGlyphMetrics[] { glyph, glyph }));
            Assert.Throws<ArgumentException>(() => Prepare(
                "a",
                new double[] { 1 },
                new int[] { 0 },
                new int[] { 0 },
                new PreparedRunMetrics[] { run },
                new TextLayoutOptions(),
                10));
        });

        TextLayout plain = Prepare(
            "a",
            new double[] { 1 },
            new int[] { 0 },
            new int[] { 0 },
            Array.Empty<PreparedRunMetrics>(),
            new TextLayoutOptions(),
            10).Layout(1);
        Assert.Multiple(() =>
        {
            Assert.That(plain.GetPositionedRun(0).HasResolvedFont(), Is.False);
            Assert.Throws<InvalidOperationException>(() =>
                plain.GetPositionedRun(0).GetResolvedFont());
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                plain.GetPositionedRun(0).GetGlyph(0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                plain.GetResolvedFont(0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                plain.GetVisualRunIndex(1));
        });
    }

    static PreparedGlyphMetrics Glyph(
        int glyphId,
        int clusterIndex,
        int logicalIndex,
        int visualIndex,
        double x,
        double y,
        double xAdvance,
        double yAdvance,
        TextInkBounds bounds)
    {
        return PreparedGlyphMetrics.Create(
            glyphId,
            clusterIndex,
            logicalIndex,
            visualIndex,
            x,
            y,
            xAdvance,
            yAdvance,
            bounds);
    }

    static PreparedRunMetrics Run(
        int sourceRunIndex,
        FontInstance font,
        PreparedGlyphMetrics glyph)
    {
        return new PreparedRunMetrics(
            sourceRunIndex,
            font,
            2,
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

    static double[] UniformAdvances(int count, double value)
    {
        double[] advances = new double[count];
        int index = 0;
        while (index < count)
        {
            advances[index] = value;
            index++;
        }

        return advances;
    }

    static int[] SourceRuns(TextLayout layout)
    {
        int[] indexes = new int[layout.GetPositionedRunCount()];
        int index = 0;
        while (index < indexes.Length)
        {
            indexes[index] = layout.GetPositionedRun(index).GetSourceRunIndex();
            index++;
        }

        return indexes;
    }

    static void AssertBounds(
        TextInkBounds bounds,
        double xMinimum,
        double yMinimum,
        double xMaximum,
        double yMaximum)
    {
        Assert.Multiple(() =>
        {
            Assert.That(bounds.IsEmpty(), Is.False);
            Assert.That(bounds.GetXMinimum(), Is.EqualTo(xMinimum));
            Assert.That(bounds.GetYMinimum(), Is.EqualTo(yMinimum));
            Assert.That(bounds.GetXMaximum(), Is.EqualTo(xMaximum));
            Assert.That(bounds.GetYMaximum(), Is.EqualTo(yMaximum));
        });
    }
}
