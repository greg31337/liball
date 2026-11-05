namespace ttf.Tests;

public class OriginalClusterLayoutTests
{
    [Test]
    public void CanonicalEquivalentFallbackRunsKeepCallerUtf16Ranges()
    {
        FontFace source = HarfBuzzLayoutDifferentialTests.Load();
        FontFace primary = Clone(source, "Primary", new int[] { 0x00E9 });
        FontFace fallback = Clone(source, "Fallback", new int[] { 0x0059 });
        FontSet set = CreateSet(primary, fallback);
        FontRequest request = new FontRequest("Primary");
        string composedText = "\u00E9Y\u00E9";
        string decomposedText = "e\u0301Ye\u0301";

        TextShapingResult composedShaping = TextShaper.Shape(
            composedText,
            set,
            request);
        TextShapingResult decomposedShaping = TextShaper.Shape(
            decomposedText,
            set,
            request);
        TextLayout composed = PrepareFromShaping(
            composedText,
            composedShaping,
            new double[] { 2, 1, 2 },
            10).Layout(3);
        TextLayout decomposed = PrepareFromShaping(
            decomposedText,
            decomposedShaping,
            new double[] { 2, 1, 2 },
            10).Layout(3);

        Assert.Multiple(() =>
        {
            Assert.That(composedShaping.GetDiagnosticCount(), Is.Zero);
            Assert.That(decomposedShaping.GetDiagnosticCount(), Is.Zero);
            Assert.That(composedShaping.GetRunCount(), Is.EqualTo(3));
            Assert.That(decomposedShaping.GetRunCount(), Is.EqualTo(3));
            Assert.That(composedShaping.GetRun(0).GetUtf16Length(), Is.EqualTo(1));
            Assert.That(decomposedShaping.GetRun(0).GetUtf16Length(), Is.EqualTo(2));
            Assert.That(composedShaping.GetRun(1).GetUtf16Start(), Is.EqualTo(1));
            Assert.That(decomposedShaping.GetRun(1).GetUtf16Start(), Is.EqualTo(2));
            Assert.That(composedShaping.GetRun(2).GetUtf16Start(), Is.EqualTo(2));
            Assert.That(decomposedShaping.GetRun(2).GetUtf16Start(), Is.EqualTo(3));
            Assert.That(composedShaping.GetRun(0).GetShapedRun().GetGlyph(0)
                .GetGlyphId(), Is.EqualTo(decomposedShaping.GetRun(0)
                    .GetShapedRun().GetGlyph(0).GetGlyphId()));
            Assert.That(composed.GetLineCount(), Is.EqualTo(2));
            Assert.That(decomposed.GetLineCount(), Is.EqualTo(2));
            Assert.That(VisibleText(composedText, composed.GetLine(0)),
                Is.EqualTo("\u00E9Y"));
            Assert.That(VisibleText(decomposedText, decomposed.GetLine(0)),
                Is.EqualTo("e\u0301Y"));
            Assert.That(VisibleText(composedText, composed.GetLine(1)),
                Is.EqualTo("\u00E9"));
            Assert.That(VisibleText(decomposedText, decomposed.GetLine(1)),
                Is.EqualTo("e\u0301"));
            Assert.That(RunRanges(composed),
                Is.EqualTo(new int[] { 0, 1, 1, 1, 2, 1 }));
            Assert.That(RunRanges(decomposed),
                Is.EqualTo(new int[] { 0, 2, 2, 1, 3, 2 }));
            Assert.That(CaretUtf16Indexes(composed),
                Is.EqualTo(new int[] { 0, 1, 2, 2, 3 }));
            Assert.That(CaretUtf16Indexes(decomposed),
                Is.EqualTo(new int[] { 0, 2, 3, 3, 5 }));
        });
    }

    [Test]
    public void FallbackBidiReorderingRetainsOriginalCombiningCluster()
    {
        FontFace source = HarfBuzzLayoutDifferentialTests.Load();
        FontFace latin = Clone(
            source,
            "Latin",
            new int[] { 0x0041, 0x0042 });
        FontFace aleph = Clone(
            source,
            "Aleph",
            new int[] { 0x05B7, 0x05D0 });
        FontFace bet = Clone(source, "Bet", new int[] { 0x05D1 });
        FontSet set = new FontSet();
        set.Add(latin);
        set.Add(aleph);
        set.Add(bet);
        set.AddFallbackFamily("Aleph");
        set.AddFallbackFamily("Bet");
        string text = "A\u05D0\u05B7\u05D1B";

        TextShapingResult shaping = TextShaper.Shape(
            text,
            set,
            new FontRequest("Latin"));
        PreparedText prepared = PrepareFromShaping(
            text,
            shaping,
            new double[] { 1, 1, 1, 1 },
            9);
        TextLayout wide = prepared.Layout(4);
        TextLayout narrow = prepared.Layout(2);

        Assert.Multiple(() =>
        {
            Assert.That(shaping.GetDiagnosticCount(), Is.Zero);
            Assert.That(shaping.GetRunCount(), Is.EqualTo(4));
            Assert.That(shaping.GetRun(0).GetFontFace(), Is.SameAs(latin));
            Assert.That(shaping.GetRun(1).GetFontFace(), Is.SameAs(bet));
            Assert.That(shaping.GetRun(2).GetFontFace(), Is.SameAs(aleph));
            Assert.That(shaping.GetRun(3).GetFontFace(), Is.SameAs(latin));
            Assert.That(shaping.GetRun(1).GetUtf16Start(), Is.EqualTo(3));
            Assert.That(shaping.GetRun(1).GetUtf16Length(), Is.EqualTo(1));
            Assert.That(shaping.GetRun(2).GetUtf16Start(), Is.EqualTo(1));
            Assert.That(shaping.GetRun(2).GetUtf16Length(), Is.EqualTo(2));
            Assert.That(SourceRuns(wide), Is.EqualTo(new int[] { 0, 1, 2, 3 }));
            Assert.That(wide.GetVisualRunIndex(1), Is.EqualTo(2));
            Assert.That(wide.GetVisualRunIndex(2), Is.EqualTo(1));
            Assert.That(RunText(text, wide.GetPositionedRun(1)), Is.EqualTo("\u05D1"));
            Assert.That(RunText(text, wide.GetPositionedRun(2)),
                Is.EqualTo("\u05D0\u05B7"));
            Assert.That(wide.GetPositionedRun(2).GetUtf16Length(), Is.EqualTo(2));
            Assert.That(narrow.GetLineCount(), Is.EqualTo(2));
            Assert.That(VisibleText(text, narrow.GetLine(0)),
                Is.EqualTo("A\u05D0\u05B7"));
            Assert.That(VisibleText(text, narrow.GetLine(1)), Is.EqualTo("\u05D1B"));
            Assert.That(FindRun(narrow, 2).GetUtf16Start(), Is.EqualTo(1));
            Assert.That(FindRun(narrow, 2).GetUtf16Length(), Is.EqualTo(2));
            Assert.That(FindRun(narrow, 1).GetUtf16Start(), Is.EqualTo(3));
            Assert.That(wide.GetLineCount(), Is.EqualTo(1));
            Assert.That(RunText(text, FindRun(wide, 2)),
                Is.EqualTo(RunText(text, FindRun(narrow, 2))));
        });
    }

    [Test]
    public void ReflowNeverCreatesACaretInsideCanonicalCombiningClusters()
    {
        FontFace source = HarfBuzzLayoutDifferentialTests.Load();
        FontFace primary = Clone(source, "Primary", new int[] { 0x00E9 });
        FontSet set = new FontSet();
        set.Add(primary);
        string text = "e\u0301e\u0301";
        TextShapingResult shaping = TextShaper.Shape(
            text,
            set,
            new FontRequest("Primary"));
        PreparedText prepared = PrepareFromShaping(
            text,
            shaping,
            new double[] { 2, 2 },
            8);

        TextLayout wide = prepared.Layout(4);
        TextLayout narrow = prepared.Layout(2);

        Assert.Multiple(() =>
        {
            Assert.That(CaretUtf16Indexes(wide),
                Is.EqualTo(new int[] { 0, 2, 4 }));
            Assert.That(CaretUtf16Indexes(narrow),
                Is.EqualTo(new int[] { 0, 2, 2, 4 }));
            Assert.That(narrow.GetLineCount(), Is.EqualTo(2));
            Assert.That(VisibleText(text, narrow.GetLine(0)),
                Is.EqualTo("e\u0301"));
            Assert.That(VisibleText(text, narrow.GetLine(1)),
                Is.EqualTo("e\u0301"));
            Assert.Throws<ArgumentException>(() => wide.GetSelection(1, 1));
            Assert.Throws<ArgumentException>(() => narrow.GetSelection(3, 1));
            Assert.That(wide.GetSelection(0, 4).Length, Is.EqualTo(1));
            Assert.That(narrow.GetSelection(0, 4).Length, Is.EqualTo(2));
        });
    }

    static PreparedText PrepareFromShaping(
        string text,
        TextShapingResult shaping,
        double[] clusterAdvances,
        double lineStep)
    {
        TextLayoutOptions options = new TextLayoutOptions();
        options.SetAllowEmergencyBreaks(true);
        TextBreakMap breaks = Plan(text, true);
        int[] sourceRunIndexes = new int[breaks.GetClusterCount()];
        int[] bidiLevels = new int[breaks.GetClusterCount()];
        PreparedRunMetrics[] metrics =
            new PreparedRunMetrics[shaping.GetRunCount()];
        int runIndex = 0;
        while (runIndex < shaping.GetRunCount())
        {
            TextShapingRun run = shaping.GetRun(runIndex);
            AssignClusters(
                breaks,
                run,
                runIndex,
                sourceRunIndexes,
                bidiLevels);
            metrics[runIndex] = BuildRunMetrics(
                breaks,
                run,
                runIndex);
            runIndex++;
        }

        return PreparedText.Create(
            text,
            breaks,
            clusterAdvances,
            sourceRunIndexes,
            bidiLevels,
            options,
            Array.Empty<TextJustificationOpportunity>(),
            metrics,
            1,
            1,
            lineStep);
    }

    static void AssignClusters(
        TextBreakMap breaks,
        TextShapingRun run,
        int runIndex,
        int[] sourceRunIndexes,
        int[] bidiLevels)
    {
        int runEnd = run.GetUtf16Start() + run.GetUtf16Length();
        int clusterIndex = 0;
        while (clusterIndex < breaks.GetClusterCount())
        {
            int clusterStart = breaks.GetUtf16Start(clusterIndex);
            int clusterEnd = clusterStart + breaks.GetUtf16Length(clusterIndex);
            if (clusterStart >= run.GetUtf16Start() && clusterEnd <= runEnd)
            {
                sourceRunIndexes[clusterIndex] = runIndex;
                bidiLevels[clusterIndex] = run.GetBidiLevel();
            }

            clusterIndex++;
        }
    }

    static PreparedRunMetrics BuildRunMetrics(
        TextBreakMap breaks,
        TextShapingRun run,
        int runIndex)
    {
        ShapedRun shaped = run.GetShapedRun();
        PreparedGlyphMetrics[] glyphs =
            new PreparedGlyphMetrics[shaped.GetGlyphCount()];
        int visualIndex = 0;
        while (visualIndex < glyphs.Length)
        {
            ShapedGlyph glyph = shaped.GetGlyph(visualIndex);
            int utf16Start = glyph.GetUtf16Start();
            int utf16End = utf16Start + glyph.GetUtf16Length();
            int clusterStart = FindCluster(breaks, utf16Start);
            int clusterEnd = FindClusterEnd(breaks, utf16End, clusterStart);
            glyphs[visualIndex] = PreparedGlyphMetrics.Create(
                glyph.GetGlyphId(),
                clusterStart,
                clusterEnd,
                glyph.GetLogicalIndex(),
                visualIndex,
                visualIndex,
                0,
                1,
                0,
                TextInkBounds.Create());
            visualIndex++;
        }

        return new PreparedRunMetrics(
            runIndex,
            run.GetFontInstance(),
            3,
            1,
            0,
            glyphs);
    }

    static int FindCluster(TextBreakMap breaks, int utf16Index)
    {
        int clusterIndex = 0;
        while (clusterIndex < breaks.GetClusterCount())
        {
            int start = breaks.GetUtf16Start(clusterIndex);
            int end = start + breaks.GetUtf16Length(clusterIndex);
            if (utf16Index >= start && utf16Index < end)
            {
                return clusterIndex;
            }

            clusterIndex++;
        }

        throw new InvalidOperationException("Shaped glyph is outside the source clusters.");
    }

    static int FindClusterEnd(
        TextBreakMap breaks,
        int utf16End,
        int clusterStart)
    {
        int clusterIndex = clusterStart;
        while (clusterIndex < breaks.GetClusterCount())
        {
            int end = breaks.GetUtf16Start(clusterIndex) +
                breaks.GetUtf16Length(clusterIndex);
            if (end >= utf16End)
            {
                return clusterIndex + 1;
            }

            clusterIndex++;
        }

        return breaks.GetClusterCount();
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

    static FontSet CreateSet(FontFace primary, FontFace fallback)
    {
        FontSet set = new FontSet();
        set.Add(primary);
        set.Add(fallback);
        set.AddFallbackFamily(fallback.GetNames()
            .GetTypographicFamilyName()!.GetValue());
        return set;
    }

    static FontFace Clone(
        FontFace source,
        string family,
        int[] codePoints)
    {
        return FontMatchingTests.CloneFaceWithCmap(
            source,
            family,
            BuildCmap(BuildFormat12(codePoints)));
    }

    static byte[] BuildFormat12(int[] codePoints)
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(12)
            .WriteUInt16(0)
            .WriteUInt32((uint)(16 + codePoints.Length * 12))
            .WriteUInt32(0)
            .WriteUInt32((uint)codePoints.Length);
        int index = 0;
        while (index < codePoints.Length)
        {
            builder.WriteUInt32((uint)codePoints[index])
                .WriteUInt32((uint)codePoints[index])
                .WriteUInt32((uint)(index + 1));
            index++;
        }

        return builder.CopyBytes();
    }

    static byte[] BuildCmap(byte[] nominal)
    {
        return new TestBinaryBuilder()
            .WriteUInt16(0)
            .WriteUInt16(1)
            .WriteUInt16(0)
            .WriteUInt16(4)
            .WriteUInt32(12)
            .WriteBytes(nominal)
            .CopyBytes();
    }

    static string VisibleText(string text, TextLine line)
    {
        return text.Substring(
            line.GetVisibleUtf16Start(),
            line.GetVisibleUtf16Length());
    }

    static string RunText(string text, PositionedRun run)
    {
        return text.Substring(run.GetUtf16Start(), run.GetUtf16Length());
    }

    static PositionedRun FindRun(TextLayout layout, int sourceRunIndex)
    {
        int index = 0;
        while (index < layout.GetPositionedRunCount())
        {
            PositionedRun run = layout.GetPositionedRun(index);
            if (run.GetSourceRunIndex() == sourceRunIndex)
            {
                return run;
            }

            index++;
        }

        throw new InvalidOperationException("Expected positioned run was not found.");
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

    static int[] RunRanges(TextLayout layout)
    {
        int[] values = new int[layout.GetPositionedRunCount() * 2];
        int index = 0;
        while (index < layout.GetPositionedRunCount())
        {
            PositionedRun run = layout.GetPositionedRun(index);
            values[index * 2] = run.GetUtf16Start();
            values[index * 2 + 1] = run.GetUtf16Length();
            index++;
        }

        return values;
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
}
