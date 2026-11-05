namespace ttf.Tests;

public class TextLayoutIntegrationTests
{
    [Test]
    public void WrapsMixedScriptFallbackRunsWithStableLogicalExtraction()
    {
        string text = "ab \u05D0\u05D1 cd";
        FontInstance latin = FontInstanceTests.CreateFace()
            .CreateDefaultInstance();
        FontInstance hebrew = FontInstanceTests.CreateFace()
            .CreateDefaultInstance();
        PreparedRunMetrics[] metrics = new PreparedRunMetrics[]
        {
            Run(0, latin, new PreparedGlyphMetrics[]
            {
                Glyph(10, 0, 0, 0, 0, 1, 0),
                Glyph(11, 1, 1, 1, 1, 1, 0),
                Glyph(3, 2, 2, 2, 2, 1, 0)
            }),
            Run(1, hebrew, new PreparedGlyphMetrics[]
            {
                Glyph(21, 4, 1, 0, 0, 1, 0),
                Glyph(20, 3, 0, 1, 1, 1, 0)
            }),
            Run(2, latin, new PreparedGlyphMetrics[]
            {
                Glyph(3, 5, 0, 0, 0, 1, 0),
                Glyph(12, 6, 1, 1, 1, 1, 0),
                Glyph(13, 7, 2, 2, 2, 1, 0)
            })
        };
        PreparedText prepared = Prepare(
            text,
            UniformAdvances(8, 1),
            new int[] { 0, 0, 0, 1, 1, 2, 2, 2 },
            new int[] { 0, 0, 0, 1, 1, 0, 0, 0 },
            metrics,
            new TextLayoutOptions(),
            10);

        TextLayout narrow = prepared.Layout(5);
        TextLayout wide = prepared.Layout(8);

        Assert.Multiple(() =>
        {
            Assert.That(narrow.GetLineCount(), Is.EqualTo(2));
            Assert.That(VisibleText(text, narrow.GetLine(0)),
                Is.EqualTo("ab \u05D0\u05D1"));
            Assert.That(VisibleText(text, narrow.GetLine(1)), Is.EqualTo("cd"));
            Assert.That(narrow.GetLine(0).GetVisibleUtf16Start(), Is.Zero);
            Assert.That(narrow.GetLine(0).GetVisibleUtf16Length(), Is.EqualTo(5));
            Assert.That(narrow.GetLine(1).GetVisibleUtf16Start(), Is.EqualTo(6));
            Assert.That(SourceRuns(narrow), Is.EqualTo(new int[] { 0, 1, 2 }));
            Assert.That(narrow.GetPositionedRun(0).GetX(), Is.Zero);
            Assert.That(narrow.GetPositionedRun(1).GetX(), Is.EqualTo(3));
            Assert.That(narrow.GetPositionedRun(1).GetGlyph(0).GetGlyphId(),
                Is.EqualTo(21));
            Assert.That(narrow.GetPositionedRun(1).GetGlyph(0).GetX(),
                Is.EqualTo(3));
            Assert.That(narrow.GetPositionedRun(1).GetGlyph(1).GetX(),
                Is.EqualTo(4));
            Assert.That(narrow.GetPositionedRun(2).GetX(), Is.Zero);
            Assert.That(narrow.GetPositionedRun(2).GetY(), Is.EqualTo(-10));
            Assert.That(narrow.GetResolvedFontCount(), Is.EqualTo(2));
            Assert.That(narrow.GetResolvedFont(0), Is.SameAs(latin));
            Assert.That(narrow.GetResolvedFont(1), Is.SameAs(hebrew));
            Assert.That(wide.GetLineCount(), Is.EqualTo(1));
            Assert.That(VisibleText(text, wide.GetLine(0)), Is.EqualTo(text));
            Assert.That(narrow.GetLineCount(), Is.EqualTo(2));
        });
    }

    [Test]
    public void JustifiesSpacesWithoutChangingExtractedLogicalText()
    {
        string text = "a b c";
        TextLayoutOptions options = new TextLayoutOptions();
        options.SetAlignment(TextAlignment.Justify);
        options.SetJustifyLastLine(true);
        TextLayout layout = Prepare(
            text,
            UniformAdvances(5, 1),
            new int[] { 0, 0, 0, 0, 0 },
            new int[] { 0, 0, 0, 0, 0 },
            Array.Empty<PreparedRunMetrics>(),
            options,
            10).Layout(9);

        TextSelectionRectangle[] firstSpace = layout.GetSelection(1, 1);

        Assert.Multiple(() =>
        {
            Assert.That(layout.GetLineCount(), Is.EqualTo(1));
            Assert.That(layout.GetLine(0).GetAdvance(), Is.EqualTo(9));
            Assert.That(CaretXPositions(layout),
                Is.EqualTo(new double[] { 0, 1, 4, 5, 8, 9 }));
            Assert.That(firstSpace.Length, Is.EqualTo(1));
            Assert.That(firstSpace[0].GetX(), Is.EqualTo(1));
            Assert.That(firstSpace[0].GetWidth(), Is.EqualTo(3));
            Assert.That(firstSpace[0].GetUtf16Start(), Is.EqualTo(1));
            Assert.That(firstSpace[0].GetUtf16Length(), Is.EqualTo(1));
            Assert.That(VisibleText(text, layout.GetLine(0)), Is.EqualTo(text));
            Assert.That(text.Substring(
                firstSpace[0].GetUtf16Start(),
                firstSpace[0].GetUtf16Length()), Is.EqualTo(" "));
        });
    }

    [Test]
    public void ReflowsVerticalFallbackRunsIntoStableColumns()
    {
        string text = "A\u4E2DB";
        FontInstance latin = FontInstanceTests.CreateFace()
            .CreateDefaultInstance();
        FontInstance cjk = FontInstanceTests.CreateFace()
            .CreateDefaultInstance();
        PreparedRunMetrics[] metrics = new PreparedRunMetrics[]
        {
            Run(0, latin, new PreparedGlyphMetrics[]
            {
                Glyph(1, 0, 0, 0, 0, 0, -2)
            }),
            Run(1, cjk, new PreparedGlyphMetrics[]
            {
                Glyph(2, 1, 0, 0, 0, 0, -3)
            }),
            Run(2, latin, new PreparedGlyphMetrics[]
            {
                Glyph(3, 2, 0, 0, 0, 0, -2)
            })
        };
        TextLayoutOptions options = new TextLayoutOptions();
        options.SetAllowEmergencyBreaks(true);
        options.SetWritingMode(TextWritingMode.VerticalTopToBottom);
        TextLayout layout = Prepare(
            text,
            new double[] { 2, 3, 2 },
            new int[] { 0, 1, 2 },
            new int[] { 0, 0, 0 },
            metrics,
            options,
            12).Layout(5);

        TextSelectionRectangle[] selection = layout.GetSelection(0, text.Length);

        Assert.Multiple(() =>
        {
            Assert.That(layout.GetLineCount(), Is.EqualTo(2));
            Assert.That(VisibleText(text, layout.GetLine(0)), Is.EqualTo("A\u4E2D"));
            Assert.That(VisibleText(text, layout.GetLine(1)), Is.EqualTo("B"));
            Assert.That(SourceRuns(layout), Is.EqualTo(new int[] { 0, 1, 2 }));
            Assert.That(layout.GetPositionedRun(0).GetX(), Is.Zero);
            Assert.That(layout.GetPositionedRun(0).GetY(), Is.Zero);
            Assert.That(layout.GetPositionedRun(1).GetX(), Is.Zero);
            Assert.That(layout.GetPositionedRun(1).GetY(), Is.EqualTo(-2));
            Assert.That(layout.GetPositionedRun(2).GetX(), Is.EqualTo(-12));
            Assert.That(layout.GetPositionedRun(2).GetY(), Is.Zero);
            Assert.That(selection.Length, Is.EqualTo(2));
            Assert.That(selection[0].GetX(), Is.EqualTo(-1));
            Assert.That(selection[0].GetY(), Is.EqualTo(-5));
            Assert.That(selection[0].GetWidth(), Is.EqualTo(4));
            Assert.That(selection[0].GetHeight(), Is.EqualTo(5));
            Assert.That(selection[1].GetX(), Is.EqualTo(-13));
            Assert.That(selection[1].GetY(), Is.EqualTo(-2));
            Assert.That(selection[1].GetHeight(), Is.EqualTo(2));
            Assert.That(layout.GetResolvedFontCount(), Is.EqualTo(2));
        });
    }

    [Test]
    public void KeepsFallbackAndBidiRunRangesStableAcrossWidths()
    {
        string text = "x \u05D0\u05D1 y";
        FontInstance latin = FontInstanceTests.CreateFace()
            .CreateDefaultInstance();
        FontInstance hebrew = FontInstanceTests.CreateFace()
            .CreateDefaultInstance();
        PreparedRunMetrics[] metrics = new PreparedRunMetrics[]
        {
            Run(0, latin, new PreparedGlyphMetrics[]
            {
                Glyph(1, 0, 0, 0, 0, 1, 0),
                Glyph(3, 1, 1, 1, 1, 1, 0)
            }),
            Run(1, hebrew, new PreparedGlyphMetrics[]
            {
                Glyph(5, 3, 1, 0, 0, 1, 0),
                Glyph(4, 2, 0, 1, 1, 1, 0)
            }),
            Run(2, latin, new PreparedGlyphMetrics[]
            {
                Glyph(3, 4, 0, 0, 0, 1, 0),
                Glyph(2, 5, 1, 1, 1, 1, 0)
            })
        };
        PreparedText prepared = Prepare(
            text,
            UniformAdvances(6, 1),
            new int[] { 0, 0, 1, 1, 2, 2 },
            new int[] { 0, 0, 1, 1, 0, 0 },
            metrics,
            new TextLayoutOptions(),
            9);

        TextLayout wide = prepared.Layout(6);
        TextLayout narrow = prepared.Layout(4);

        Assert.Multiple(() =>
        {
            Assert.That(wide.GetLineCount(), Is.EqualTo(1));
            Assert.That(SourceRuns(wide), Is.EqualTo(new int[] { 0, 1, 2 }));
            Assert.That(wide.GetPositionedRun(1).GetUtf16Start(), Is.EqualTo(2));
            Assert.That(wide.GetPositionedRun(1).GetUtf16Length(), Is.EqualTo(2));
            Assert.That(narrow.GetLineCount(), Is.EqualTo(2));
            Assert.That(VisibleText(text, narrow.GetLine(0)),
                Is.EqualTo("x \u05D0\u05D1"));
            Assert.That(VisibleText(text, narrow.GetLine(1)), Is.EqualTo("y"));
            Assert.That(narrow.GetPositionedRun(1).GetUtf16Start(), Is.EqualTo(2));
            Assert.That(narrow.GetPositionedRun(1).GetUtf16Length(), Is.EqualTo(2));
            Assert.That(wide.GetLineCount(), Is.EqualTo(1));
            Assert.That(VisibleText(text, wide.GetLine(0)), Is.EqualTo(text));
        });
    }

    static PreparedGlyphMetrics Glyph(
        int glyphId,
        int clusterIndex,
        int logicalIndex,
        int visualIndex,
        double inlinePosition,
        double xAdvance,
        double yAdvance)
    {
        return PreparedGlyphMetrics.Create(
            glyphId,
            clusterIndex,
            logicalIndex,
            visualIndex,
            inlinePosition,
            0,
            xAdvance,
            yAdvance,
            TextInkBounds.Create());
    }

    static PreparedRunMetrics Run(
        int sourceRunIndex,
        FontInstance font,
        PreparedGlyphMetrics[] glyphs)
    {
        return new PreparedRunMetrics(
            sourceRunIndex,
            font,
            3,
            1,
            0,
            glyphs);
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

    static string VisibleText(string source, TextLine line)
    {
        return source.Substring(
            line.GetVisibleUtf16Start(),
            line.GetVisibleUtf16Length());
    }

    static int[] SourceRuns(TextLayout layout)
    {
        int[] values = new int[layout.GetPositionedRunCount()];
        int index = 0;
        while (index < values.Length)
        {
            values[index] = layout.GetPositionedRun(index).GetSourceRunIndex();
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
}
