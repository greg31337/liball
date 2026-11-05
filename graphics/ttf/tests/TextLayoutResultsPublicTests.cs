using ttf;

namespace ttf.Tests;

internal class TextLayoutResultsPublicTests
{
    [Test]
    public void ExposesParagraphLineRunAndGlyphGeometry()
    {
        FontFace face = HarfBuzzLayoutDifferentialTests.Load();
        TextLayoutResult layout = Prepare(
            face,
            "AB",
            new TextStyle(12));
        TextLineResult line = layout.GetLine(0);
        PositionedRunResult run = line.GetVisualRun(0);
        PositionedGlyphResult first = run.GetVisualGlyph(0);
        PositionedGlyphResult second = run.GetVisualGlyph(1);

        Assert.Multiple(() =>
        {
            Assert.That(layout.GetWritingMode(),
                Is.EqualTo(TextWritingMode.Horizontal));
            Assert.That(layout.GetLineCount(), Is.EqualTo(1));
            Assert.That(layout.GetRunCount(), Is.EqualTo(1));
            Assert.That(layout.GetResolvedFontCount(), Is.EqualTo(1));
            Assert.That(layout.GetResolvedFont(0).GetFace(), Is.SameAs(face));
            Assert.That(layout.GetAdvanceBounds().IsEmpty(), Is.False);
            Assert.That(layout.GetInkBounds().IsEmpty(), Is.False);
            Assert.That(line.GetLineIndex(), Is.Zero);
            Assert.That(line.GetClusterStart(), Is.Zero);
            Assert.That(line.GetClusterEnd(), Is.EqualTo(2));
            Assert.That(line.GetClusterCount(), Is.EqualTo(2));
            Assert.That(line.GetUtf16Start(), Is.Zero);
            Assert.That(line.GetUtf16Length(), Is.EqualTo(2));
            Assert.That(line.GetVisibleUtf16Start(), Is.Zero);
            Assert.That(line.GetVisibleUtf16Length(), Is.EqualTo(2));
            Assert.That(line.GetBaseline(), Is.Zero);
            Assert.That(line.GetAscent(), Is.GreaterThan(0));
            Assert.That(line.GetDescent(), Is.GreaterThanOrEqualTo(0));
            Assert.That(line.GetLineHeight(), Is.EqualTo(
                line.GetAscent() + line.GetDescent() + line.GetLineGap()));
            Assert.That(line.GetAdvanceBounds().GetWidth(),
                Is.EqualTo(layout.GetAdvanceBounds().GetWidth()));
            Assert.That(line.GetInkBounds().GetWidth(),
                Is.EqualTo(layout.GetInkBounds().GetWidth()));
            Assert.That(run.GetLineIndex(), Is.Zero);
            Assert.That(run.GetSourceRunIndex(), Is.Zero);
            Assert.That(run.GetClusterStart(), Is.Zero);
            Assert.That(run.GetClusterEnd(), Is.EqualTo(2));
            Assert.That(run.GetUtf16Start(), Is.Zero);
            Assert.That(run.GetUtf16Length(), Is.EqualTo(2));
            Assert.That(run.GetPositionedAdvance(),
                Is.EqualTo(line.GetAdvance()));
            Assert.That(run.GetInkBounds().IsEmpty(), Is.False);
            Assert.That(run.HasResolvedFont(), Is.True);
            Assert.That(run.GetFontFace(), Is.SameAs(face));
            Assert.That(run.GetFontInstance().GetFace(), Is.SameAs(face));
            Assert.That(run.GetGlyphCount(), Is.EqualTo(2));
            Assert.That(first.GetGlyphId(), Is.Not.Zero);
            Assert.That(first.GetClusterStart(), Is.Zero);
            Assert.That(first.GetClusterEnd(), Is.EqualTo(1));
            Assert.That(first.GetUtf16Start(), Is.Zero);
            Assert.That(first.GetUtf16Length(), Is.EqualTo(1));
            Assert.That(first.GetX(), Is.LessThan(second.GetX()));
            Assert.That(first.GetXAdvance(), Is.GreaterThan(0));
            Assert.That(first.GetYAdvance(), Is.Zero);
            Assert.That(first.GetInkBounds().IsEmpty(), Is.False);
            Assert.That(first.GetFontFace(), Is.SameAs(face));
            Assert.That(second.GetUtf16Start(), Is.EqualTo(1));
        });
    }

    [Test]
    public void PreservesLogicalAndVisualMappingsForBidirectionalRunsAndGlyphs()
    {
        FontFace source = HarfBuzzLayoutDifferentialTests.Load();
        FontFace mixed = FontClusterFallbackTests.Clone(
            source,
            "Mixed",
            new int[] { 0x0041, 0x05D0, 0x05D1 });
        TextLayoutResult layout = Prepare(
            mixed,
            "A\u05D0\u05D1",
            new TextStyle(12));
        TextLineResult line = layout.GetLine(0);
        PositionedRunResult rtl = FindOddRun(line);

        Assert.Multiple(() =>
        {
            Assert.That(line.GetRunCount(), Is.EqualTo(2));
            AssertInverseRunMappings(layout);
            AssertInverseLineMappings(line);
            Assert.That(rtl.GetBidiLevel() & 1, Is.EqualTo(1));
            Assert.That(rtl.GetDirection(),
                Is.EqualTo(ShapingDirection.RightToLeft));
            Assert.That(rtl.GetGlyphCount(), Is.EqualTo(2));
            AssertInverseGlyphMappings(rtl);
            Assert.That(rtl.GetLogicalGlyph(0).GetLogicalIndex(), Is.Zero);
            Assert.That(rtl.GetVisualGlyph(0).GetVisualIndex(), Is.Zero);
            Assert.That(rtl.GetLogicalGlyph(0).GetUtf16Start(), Is.EqualTo(1));
            Assert.That(rtl.GetLogicalGlyph(1).GetUtf16Start(), Is.EqualTo(2));
        });
    }

    [Test]
    public void ExposesDistinctResolvedFontsAndGlobalSourceRanges()
    {
        FontFace source = HarfBuzzLayoutDifferentialTests.Load();
        FontFace first = FontClusterFallbackTests.Clone(
            source,
            "First",
            new int[] { 0x0041 });
        FontFace second = FontClusterFallbackTests.Clone(
            source,
            "Second",
            new int[] { 0x0042 });
        FontSet set = new FontSet();
        set.Add(first);
        set.Add(second);
        TextLayoutStyle[] styles = new TextLayoutStyle[]
        {
            new TextLayoutStyle(
                0,
                2,
                new FontRequest("First"),
                new TextStyle(10)),
            new TextLayoutStyle(
                2,
                2,
                new FontRequest("Second"),
                new TextStyle(14))
        };
        TextLayoutResult layout = new TextLayoutEngine().Prepare(
            set,
            "AABB",
            styles,
            new TextLayoutOptions()).Reflow(1000);
        PositionedRunResult firstRun = layout.GetLogicalRun(0);
        PositionedRunResult secondRun = layout.GetLogicalRun(1);

        Assert.Multiple(() =>
        {
            Assert.That(layout.GetResolvedFontCount(), Is.EqualTo(2));
            Assert.That(layout.GetResolvedFont(0).GetFace(), Is.SameAs(first));
            Assert.That(layout.GetResolvedFont(1).GetFace(), Is.SameAs(second));
            Assert.That(firstRun.GetFontFace(), Is.SameAs(first));
            Assert.That(firstRun.GetUtf16Start(), Is.Zero);
            Assert.That(firstRun.GetUtf16Length(), Is.EqualTo(2));
            Assert.That(firstRun.GetClusterStart(), Is.Zero);
            Assert.That(firstRun.GetClusterEnd(), Is.EqualTo(2));
            Assert.That(secondRun.GetFontFace(), Is.SameAs(second));
            Assert.That(secondRun.GetUtf16Start(), Is.EqualTo(2));
            Assert.That(secondRun.GetUtf16Length(), Is.EqualTo(2));
            Assert.That(secondRun.GetClusterStart(), Is.EqualTo(2));
            Assert.That(secondRun.GetClusterEnd(), Is.EqualTo(4));
            Assert.That(secondRun.GetVisualGlyph(0).GetUtf16Start(),
                Is.EqualTo(2));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                layout.GetResolvedFont(2));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                layout.GetVisualRun(2));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                firstRun.GetVisualGlyph(2));
            Assert.Throws<InvalidOperationException>(() =>
                layout.GetTruncationClusterIndex());
        });
    }

    static TextLayoutResult Prepare(
        FontFace face,
        string text,
        TextStyle textStyle)
    {
        FontSet set = new FontSet();
        set.Add(face);
        FontNames names = face.GetNames();
        FontNameValue? familyName = names.GetFamilyName();
        if (familyName == null)
        {
            familyName = names.GetTypographicFamilyName();
        }
        string family = familyName!.GetValue();
        return new TextLayoutEngine().Prepare(
            set,
            text,
            new TextLayoutStyle[]
            {
                new TextLayoutStyle(
                    0,
                    text.Length,
                    new FontRequest(family),
                    textStyle)
            },
            new TextLayoutOptions()).Reflow(1000);
    }

    static PositionedRunResult FindOddRun(TextLineResult line)
    {
        int index = 0;
        while (index < line.GetRunCount())
        {
            PositionedRunResult run = line.GetVisualRun(index);
            if ((run.GetBidiLevel() & 1) != 0)
            {
                return run;
            }
            index += 1;
        }
        throw new InvalidOperationException("No right-to-left run was produced.");
    }

    static void AssertInverseRunMappings(TextLayoutResult layout)
    {
        int logicalIndex = 0;
        while (logicalIndex < layout.GetRunCount())
        {
            int visualIndex = layout.GetVisualRunIndex(logicalIndex);
            Assert.That(layout.GetLogicalRunIndex(visualIndex),
                Is.EqualTo(logicalIndex));
            Assert.That(layout.GetLogicalRun(logicalIndex).GetSourceRunIndex(),
                Is.EqualTo(layout.GetVisualRun(visualIndex).GetSourceRunIndex()));
            logicalIndex += 1;
        }
    }

    static void AssertInverseLineMappings(TextLineResult line)
    {
        int logicalIndex = 0;
        while (logicalIndex < line.GetRunCount())
        {
            int visualIndex = line.GetVisualRunIndex(logicalIndex);
            Assert.That(line.GetLogicalRunIndex(visualIndex),
                Is.EqualTo(logicalIndex));
            logicalIndex += 1;
        }
    }

    static void AssertInverseGlyphMappings(PositionedRunResult run)
    {
        int logicalIndex = 0;
        while (logicalIndex < run.GetGlyphCount())
        {
            int visualIndex = run.GetVisualGlyphIndex(logicalIndex);
            Assert.That(run.GetLogicalGlyphIndex(visualIndex),
                Is.EqualTo(logicalIndex));
            Assert.That(run.GetLogicalGlyph(logicalIndex).GetGlyphId(),
                Is.EqualTo(run.GetVisualGlyph(visualIndex).GetGlyphId()));
            logicalIndex += 1;
        }
    }
}
