namespace svg.Tests;

public class SvgTextLayoutTests
{
    [Test]
    public void FormsChunksFromAbsoluteInlinePositionsAndConsumesPositionLists()
    {
        SvgShapingResult shaping = Shape("ABC", new Dictionary<int, int> { [0x41] = 1, [0x42] = 2, [0x43] = 3 });
        SvgTextLayoutOptions options = new SvgTextLayoutOptions();
        options.X.AddRange(new[] { 10.0, 30.0 });
        options.Y.Add(5);
        options.Dx.AddRange(new[] { 1.0, 2.0, 3.0 });
        options.Dy.AddRange(new[] { 0.5, 1.0, 1.5 });
        options.Rotate.AddRange(new[] { 10.0, 20.0 });

        SvgTextLayoutResult result = new SvgTextLayout().LayoutWithShapingAndOptions(shaping, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.CharacterCount, Is.EqualTo(3));
            Assert.That(result.Chunks, Has.Count.EqualTo(2));
            Assert.That(result.Chunks.Select(chunk => chunk.GlyphCount), Is.EqualTo(new[] { 1, 2 }));
            Assert.That(result.Glyphs.Select(glyph => glyph.ChunkIndex), Is.EqualTo(new[] { 0, 1, 1 }));
            Assert.That(result.Glyphs.Select(glyph => glyph.CharacterIndex), Is.EqualTo(new[] { 0, 1, 2 }));
            Assert.That(result.Glyphs.Select(glyph => glyph.X), Is.EqualTo(new[] { 11.0, 32.0, 41.0 }));
            Assert.That(result.Glyphs.Select(glyph => glyph.Y), Is.EqualTo(new[] { 5.5, 6.5, 8.0 }));
            Assert.That(result.Glyphs.Select(glyph => glyph.Rotation), Is.EqualTo(new[] { 10.0, 20.0, 20.0 }));
        });
    }

    [Test]
    public void ConsumesAddressableListsAcrossLigaturesAndAppliesRangeOverrides()
    {
        SvgFont font = SvgOpenTypeLayoutTests.CreateFont(
            new Dictionary<int, int> { [0x66] = 3, [0x69] = 4, [0x41] = 1 },
            new Dictionary<string, byte[]> { ["GSUB"] = SvgOpenTypeLayoutTests.Layout("liga", 4, SvgOpenTypeLayoutTests.LigatureSubtable()) });
        SvgShapingOptions shapingOptions = new SvgShapingOptions();
        shapingOptions.EmSize = 10;
        SvgShapingResult shaping = new SvgOpenTypeLayout().ShapeWithSvgFontAndStringAndSvgShapingOptions(font, "fiA", shapingOptions);
        SvgTextLayoutOptions options = new SvgTextLayoutOptions();
        options.Dx.AddRange(new[] { 1.0, 99.0, 3.0 });
        options.Rotate.AddRange(new[] { 10.0, 20.0 });
        SvgTextPositioningRange range = new SvgTextPositioningRange();
        range.CharacterStart = 2;
        range.CharacterCount = 1;
        range.X.Add(50);
        range.Rotate.Add(90);
        options.PositioningRanges.Add(range);

        SvgTextLayoutResult result = new SvgTextLayout().LayoutWithShapingAndOptions(shaping, options);

        Assert.Multiple(() =>
        {
            Assert.That(shaping.Glyphs.Select(glyph => glyph.GlyphIndex), Is.EqualTo(new[] { 6, 1 }));
            Assert.That(result.Success, Is.True);
            Assert.That(result.CharacterCount, Is.EqualTo(3));
            Assert.That(result.Glyphs.Select(glyph => glyph.CharacterIndex), Is.EqualTo(new[] { 0, 2 }));
            Assert.That(result.Glyphs.Select(glyph => glyph.X), Is.EqualTo(new[] { 1.0, 53.0 }));
            Assert.That(result.Glyphs.Select(glyph => glyph.Rotation), Is.EqualTo(new[] { 10.0, 90.0 }));
            Assert.That(result.Chunks, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void VariationSelectorsDoNotConsumeAddressablePositionValues()
    {
        SvgShapingResult shaping = Shape("A\uFE0FB", new Dictionary<int, int> { [0x41] = 1, [0x42] = 2 });
        SvgTextLayoutOptions options = new SvgTextLayoutOptions();
        options.Dx.AddRange(new[] { 1.0, 2.0, 99.0 });
        options.Rotate.AddRange(new[] { 10.0, 20.0, 30.0 });

        SvgTextLayoutResult result = new SvgTextLayout().LayoutWithShapingAndOptions(shaping, options);

        Assert.Multiple(() =>
        {
            Assert.That(shaping.Glyphs, Has.Count.EqualTo(2));
            Assert.That(shaping.Glyphs[0].ScalarCount, Is.EqualTo(2));
            Assert.That(result.Success, Is.True);
            Assert.That(result.CharacterCount, Is.EqualTo(2));
            Assert.That(result.Glyphs.Select(glyph => glyph.CharacterIndex), Is.EqualTo(new[] { 0, 1 }));
            Assert.That(result.Glyphs.Select(glyph => glyph.X), Is.EqualTo(new[] { 1.0, 9.0 }));
            Assert.That(result.Glyphs.Select(glyph => glyph.Rotation), Is.EqualTo(new[] { 10.0, 20.0 }));
        });
    }

    [Test]
    public void AppliesLetterWordSpacingAndDirectionAwareAnchorsPerChunk()
    {
        SvgShapingResult shaping = Shape("A B", new Dictionary<int, int> { [0x41] = 1, [0x20] = 2, [0x42] = 3 });
        SvgTextLayoutOptions middleOptions = new SvgTextLayoutOptions();
        middleOptions.InitialX = 100;
        middleOptions.TextAnchor = SvgTextAnchor.Middle;
        middleOptions.LetterSpacing = 1;
        middleOptions.WordSpacing = 2;
        SvgTextLayoutOptions rtlOptions = new SvgTextLayoutOptions();
        rtlOptions.InitialX = 100;
        rtlOptions.Direction = SvgTextDirection.RightToLeft;
        rtlOptions.TextAnchor = SvgTextAnchor.End;

        SvgTextLayoutResult middle = new SvgTextLayout().LayoutWithShapingAndOptions(shaping, middleOptions);
        SvgTextLayoutResult rtl = new SvgTextLayout().LayoutWithShapingAndOptions(shaping, rtlOptions);

        Assert.Multiple(() =>
        {
            Assert.That(middle.Success, Is.True);
            Assert.That(middle.NaturalTextLength, Is.EqualTo(22));
            Assert.That(middle.Chunks[0].AnchorOffset, Is.EqualTo(-11));
            Assert.That(middle.Glyphs.Select(glyph => glyph.X), Is.EqualTo(new[] { 89.0, 96.0, 105.0 }));
            Assert.That(middle.Glyphs.Select(glyph => glyph.SpacingAfter), Is.EqualTo(new[] { 1.0, 3.0, 0.0 }));
            Assert.That(middle.Glyphs[1].WordSpace, Is.True);
            Assert.That(rtl.Success, Is.True);
            Assert.That(rtl.Chunks[0].AnchorOffset, Is.EqualTo(18));
            Assert.That(rtl.Glyphs.Select(glyph => glyph.X), Is.EqualTo(new[] { 118.0, 112.0, 106.0 }));
        });
    }

    [Test]
    public void ResolvesDominantAlignmentBaselinesAndBaselineShift()
    {
        SvgShapingResult shaping = Shape("A", new Dictionary<int, int> { [0x41] = 1 });
        SvgTextLayoutOptions dominantOptions = new SvgTextLayoutOptions();
        dominantOptions.InitialY = 100;
        dominantOptions.FontSize = 10;
        dominantOptions.Ascent = 8;
        dominantOptions.Descent = 2;
        dominantOptions.XHeight = 4;
        dominantOptions.DominantBaseline = SvgDominantBaseline.Central;
        dominantOptions.BaselineShift = SvgBaselineShiftKind.Super;
        SvgTextLayoutOptions alignedOptions = new SvgTextLayoutOptions();
        alignedOptions.InitialY = 100;
        alignedOptions.Ascent = 8;
        alignedOptions.Descent = 2;
        alignedOptions.XHeight = 4;
        alignedOptions.AlignmentBaseline = SvgAlignmentBaseline.Hanging;
        alignedOptions.BaselineShift = SvgBaselineShiftKind.Length;
        alignedOptions.BaselineShiftValue = 2;

        SvgTextLayoutResult dominant = new SvgTextLayout().LayoutWithShapingAndOptions(shaping, dominantOptions);
        SvgTextLayoutResult aligned = new SvgTextLayout().LayoutWithShapingAndOptions(shaping, alignedOptions);

        Assert.Multiple(() =>
        {
            Assert.That(dominant.Success, Is.True);
            Assert.That(dominant.BaselineOffset, Is.EqualTo(-3));
            Assert.That(dominant.Glyphs[0].Y, Is.EqualTo(97));
            Assert.That(aligned.Success, Is.True);
            Assert.That(aligned.BaselineOffset, Is.EqualTo(4.4).Within(0.0001));
            Assert.That(aligned.Glyphs[0].Y, Is.EqualTo(104.4).Within(0.0001));
        });
    }

    [Test]
    public void AdjustsTextLengthThroughSpacingOrGlyphScaling()
    {
        SvgShapingResult shaping = Shape("ABC", new Dictionary<int, int> { [0x41] = 1, [0x42] = 2, [0x43] = 3 });
        SvgTextLayoutOptions spacingOptions = new SvgTextLayoutOptions();
        spacingOptions.HasTextLength = true;
        spacingOptions.TextLength = 24;
        spacingOptions.LengthAdjust = SvgTextLengthAdjust.Spacing;
        SvgTextLayoutOptions scalingOptions = new SvgTextLayoutOptions();
        scalingOptions.HasTextLength = true;
        scalingOptions.TextLength = 9;
        scalingOptions.LengthAdjust = SvgTextLengthAdjust.SpacingAndGlyphs;

        SvgTextLayoutResult spacing = new SvgTextLayout().LayoutWithShapingAndOptions(shaping, spacingOptions);
        SvgTextLayoutResult scaling = new SvgTextLayout().LayoutWithShapingAndOptions(shaping, scalingOptions);

        Assert.Multiple(() =>
        {
            Assert.That(spacing.Success, Is.True);
            Assert.That(spacing.NaturalTextLength, Is.EqualTo(18));
            Assert.That(spacing.ActualTextLength, Is.EqualTo(24));
            Assert.That(spacing.SpacingAdjustment, Is.EqualTo(3));
            Assert.That(spacing.Glyphs.Select(glyph => glyph.X), Is.EqualTo(new[] { 0.0, 9.0, 18.0 }));
            Assert.That(scaling.Success, Is.True);
            Assert.That(scaling.NaturalTextLength, Is.EqualTo(18));
            Assert.That(scaling.ActualTextLength, Is.EqualTo(9));
            Assert.That(scaling.GlyphScale, Is.EqualTo(0.5));
            Assert.That(scaling.Glyphs.Select(glyph => glyph.X), Is.EqualTo(new[] { 0.0, 3.0, 6.0 }));
            Assert.That(scaling.Glyphs.Select(glyph => glyph.InlineAdvance), Is.All.EqualTo(3));
            Assert.That(scaling.Glyphs.Select(glyph => glyph.InlineScale), Is.All.EqualTo(0.5));
        });
    }

    [Test]
    public void SupportsVerticalInlinePositioningAndCrossAxisBaselines()
    {
        SvgShapingResult shaping = Shape("AB", new Dictionary<int, int> { [0x41] = 1, [0x42] = 2 });
        shaping.Glyphs[0].YAdvance = 10;
        shaping.Glyphs[1].YAdvance = 10;
        SvgTextLayoutOptions options = new SvgTextLayoutOptions();
        options.InitialX = 50;
        options.InitialY = 20;
        options.WritingMode = SvgWritingMode.VerticalRightToLeft;
        options.AlignmentBaseline = SvgAlignmentBaseline.Ideographic;
        options.Descent = 2;
        options.Y.AddRange(new[] { 20.0, 60.0 });
        options.Dx.Add(1);

        SvgTextLayoutResult result = new SvgTextLayout().LayoutWithShapingAndOptions(shaping, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Chunks, Has.Count.EqualTo(2));
            Assert.That(result.BaselineOffset, Is.EqualTo(-2));
            Assert.That(result.Glyphs.Select(glyph => glyph.X), Is.EqualTo(new[] { 49.0, 49.0 }));
            Assert.That(result.Glyphs.Select(glyph => glyph.Y), Is.EqualTo(new[] { 20.0, 60.0 }));
        });
    }

    [Test]
    public void RejectsMalformedRangesNonFiniteValuesLimitsAndCancellation()
    {
        SvgShapingResult shaping = Shape("AB", new Dictionary<int, int> { [0x41] = 1, [0x42] = 2 });
        SvgTextLayoutOptions invalidList = new SvgTextLayoutOptions();
        invalidList.X.Add(double.NaN);
        SvgTextLayoutOptions invalidRange = new SvgTextLayoutOptions();
        SvgTextPositioningRange range = new SvgTextPositioningRange();
        range.CharacterStart = 2;
        range.CharacterCount = 1;
        invalidRange.PositioningRanges.Add(range);
        SvgTextLayoutOptions glyphLimit = new SvgTextLayoutOptions();
        glyphLimit.MaxGlyphs = 1;
        SvgTextLayoutOptions chunkLimit = new SvgTextLayoutOptions();
        chunkLimit.MaxChunks = 1;
        chunkLimit.X.AddRange(new[] { 0.0, 10.0 });
        SvgTextLayoutOptions valueLimit = new SvgTextLayoutOptions();
        valueLimit.MaxPositionValues = 1;
        valueLimit.Dx.AddRange(new[] { 1.0, 2.0 });
        SvgTextLayoutOptions cancelled = new SvgTextLayoutOptions();
        CancellationTokenSource source = new CancellationTokenSource();
        source.Cancel();
        cancelled.CancellationToken = source.Token;
        SvgShapingResult zeroAdvance = Shape("A", new Dictionary<int, int> { [0x41] = 1 });
        zeroAdvance.Glyphs[0].XAdvance = 0;
        SvgTextLayoutOptions impossibleScale = new SvgTextLayoutOptions();
        impossibleScale.HasTextLength = true;
        impossibleScale.TextLength = 1;
        impossibleScale.LengthAdjust = SvgTextLengthAdjust.SpacingAndGlyphs;

        SvgTextLayoutResult badList = new SvgTextLayout().LayoutWithShapingAndOptions(shaping, invalidList);
        SvgTextLayoutResult badRange = new SvgTextLayout().LayoutWithShapingAndOptions(shaping, invalidRange);
        SvgTextLayoutResult tooManyGlyphs = new SvgTextLayout().LayoutWithShapingAndOptions(shaping, glyphLimit);
        SvgTextLayoutResult tooManyChunks = new SvgTextLayout().LayoutWithShapingAndOptions(shaping, chunkLimit);
        SvgTextLayoutResult tooManyValues = new SvgTextLayout().LayoutWithShapingAndOptions(shaping, valueLimit);
        SvgTextLayoutResult stopped = new SvgTextLayout().LayoutWithShapingAndOptions(shaping, cancelled);
        SvgTextLayoutResult unscalable = new SvgTextLayout().LayoutWithShapingAndOptions(zeroAdvance, impossibleScale);
        SvgTextLayoutResult failedShaping = new SvgTextLayout().LayoutWithShaping(new SvgOpenTypeLayout().ShapeWithFontAndText(null, "A"));
        SvgShapingResult inconsistentSource = Shape("AB", new Dictionary<int, int> { [0x41] = 1, [0x42] = 2 });
        inconsistentSource.Glyphs[1].Utf16Start = 0;
        SvgTextLayoutResult inconsistent = new SvgTextLayout().LayoutWithShaping(inconsistentSource);

        Assert.Multiple(() =>
        {
            Assert.That(badList.Error, Is.EqualTo(SvgTextLayoutError.InvalidArgument));
            Assert.That(badRange.Error, Is.EqualTo(SvgTextLayoutError.InvalidArgument));
            Assert.That(tooManyGlyphs.Error, Is.EqualTo(SvgTextLayoutError.LimitExceeded));
            Assert.That(tooManyChunks.Error, Is.EqualTo(SvgTextLayoutError.LimitExceeded));
            Assert.That(tooManyValues.Error, Is.EqualTo(SvgTextLayoutError.LimitExceeded));
            Assert.That(stopped.Error, Is.EqualTo(SvgTextLayoutError.Cancelled));
            Assert.That(unscalable.Error, Is.EqualTo(SvgTextLayoutError.InvalidArgument));
            Assert.That(inconsistent.Error, Is.EqualTo(SvgTextLayoutError.InvalidArgument));
            Assert.That(failedShaping.Error, Is.EqualTo(SvgTextLayoutError.InvalidArgument));
        });
    }

    internal static SvgShapingResult Shape(string text, Dictionary<int, int> mappings)
    {
        SvgFont font = SvgOpenTypeLayoutTests.CreateFont(mappings, new Dictionary<string, byte[]>());
        SvgShapingOptions options = new SvgShapingOptions();
        options.EmSize = 10;
        return new SvgOpenTypeLayout().ShapeWithSvgFontAndStringAndSvgShapingOptions(font, text, options);
    }
}
