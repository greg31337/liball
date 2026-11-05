using ttf;

namespace ttf.Tests;

internal class PdfEncodedRunTests
{
    [Test]
    public void AutomaticType0UsesTwoByteBigEndianCidsByDefault()
    {
        FontInstance instance = HarfBuzzLayoutDifferentialTests.Load()
            .CreateDefaultInstance();
        PdfFontUsage usage = new PdfFontUsageAccumulator().GetOrAdd(
            instance,
            PdfFontOptions.Create());
        PdfCidAllocator allocator = PdfCidAllocator.Create(usage);

        PdfEncodedRun encoded = PdfFontRunEncoder.Encode(
            usage,
            allocator,
            CreateRun(
                new int[] { 5, 4, 5 },
                ShapingDirection.LeftToRight));

        Assert.Multiple(() =>
        {
            Assert.That(encoded.GetCodeByteCount(), Is.EqualTo(2));
            Assert.That(encoded.GetCodes(),
                Is.EqualTo(new byte[] { 0, 1, 0, 2, 0, 1 }));
            Assert.That(encoded.GetCids(),
                Is.EqualTo(new int[] { 1, 2, 1 }));
            Assert.That(encoded.GetSourceGlyphIds(),
                Is.EqualTo(new int[] { 5, 4, 5 }));
            Assert.That(encoded.GetWritingMode(),
                Is.EqualTo(PdfWritingMode.Horizontal));
            Assert.That(usage.CopyGlyphIds(),
                Is.EqualTo(new int[] { 0, 4, 5 }));
        });
    }

    [Test]
    public void ExplicitSimpleTrueTypeUsesOneByteCodesWhenSafe()
    {
        FontInstance instance = HarfBuzzLayoutDifferentialTests.Load()
            .CreateDefaultInstance();
        PdfFontOptions options = PdfFontOptions.Create();
        options.SetFontKind(PdfFontKind.SimpleTrueType);
        PdfFontUsage usage = new PdfFontUsageAccumulator().GetOrAdd(
            instance,
            options);
        PdfCidAllocator allocator = PdfCidAllocator.Create(usage);

        PdfEncodedRun encoded = PdfFontRunEncoder.Encode(
            usage,
            allocator,
            CreateRun(
                new int[] { 4, 5 },
                ShapingDirection.RightToLeft));
        byte[] codes = encoded.GetCodes();
        codes[0] = 99;

        Assert.Multiple(() =>
        {
            Assert.That(encoded.GetCodeByteCount(), Is.EqualTo(1));
            Assert.That(encoded.GetCodes(), Is.EqualTo(new byte[] { 1, 2 }));
            Assert.That(encoded.GetGlyphCount(), Is.EqualTo(2));
            Assert.That(encoded.GetCid(0), Is.EqualTo(1));
            Assert.That(encoded.GetSourceGlyphId(1), Is.EqualTo(5));
        });
    }

    [Test]
    public void Type0EncodesCidsAboveOneByteWithoutTruncation()
    {
        FontInstance instance = CreateTrueTypeInstance(300);
        PdfFontUsage usage = new PdfFontUsageAccumulator().GetOrAdd(
            instance,
            PdfFontOptions.Create());
        PdfCidAllocator allocator = PdfCidAllocator.Create(usage);
        int glyphId = 1;

        while (glyphId <= 255)
        {
            allocator.GetOrAddCid(glyphId);
            glyphId += 1;
        }

        PdfEncodedRun encoded = PdfFontRunEncoder.Encode(
            usage,
            allocator,
            CreateRun(
                new int[] { 256 },
                ShapingDirection.LeftToRight));

        Assert.Multiple(() =>
        {
            Assert.That(encoded.GetCid(0), Is.EqualTo(256));
            Assert.That(encoded.GetCodes(),
                Is.EqualTo(new byte[] { 0x01, 0x00 }));
        });
    }

    [Test]
    public void ReturnsHorizontalPositionsBaselineAndTjSuggestions()
    {
        FontInstance instance = HarfBuzzLayoutDifferentialTests.Load()
            .CreateDefaultInstance();
        int unitsPerEm = instance.GetGlobalMetrics().GetUnitsPerEm();
        int firstWidth = instance.GetGlyphMetrics(4).GetAdvanceWidth();
        int secondWidth = instance.GetGlyphMetrics(5).GetAdvanceWidth();
        PdfFontUsage usage = new PdfFontUsageAccumulator().GetOrAdd(
            instance,
            PdfFontOptions.Create());
        ShapedRun run = CreatePositionedRun(
            new int[] { 4, 5 },
            ShapingDirection.LeftToRight,
            new double[] { firstWidth - 100, secondWidth + 50 },
            new double[] { 25, 0 },
            new double[] { 20, 0 },
            new double[] { -30, 0 });

        PdfEncodedRun encoded = PdfFontRunEncoder.Encode(
            usage,
            PdfCidAllocator.Create(usage),
            run,
            2.5);
        PdfPositionedGlyph first = encoded.GetGlyph(0);
        PdfPositionedGlyph?[] copiedGlyphs = encoded.GetGlyphs();
        PdfTjAdjustment?[] copiedAdjustments =
            encoded.GetTextAdjustments();
        copiedGlyphs[0] = null;
        copiedAdjustments[0] = null;
        double scale = 1000.0 / unitsPerEm;
        double firstAdjustment =
            PdfCidWidthsBuilder.ConvertAdvance(firstWidth, unitsPerEm) -
            (firstWidth - 100) * scale;
        double secondAdjustment =
            PdfCidWidthsBuilder.ConvertAdvance(secondWidth, unitsPerEm) -
            (secondWidth + 50) * scale;

        Assert.Multiple(() =>
        {
            Assert.That(first.GetVisualIndex(), Is.Zero);
            Assert.That(first.GetCharacterCode(), Is.EqualTo(1));
            Assert.That(first.GetCid(), Is.EqualTo(1));
            Assert.That(first.GetSourceGlyphId(), Is.EqualTo(4));
            Assert.That(first.GetUtf16Start(), Is.Zero);
            Assert.That(first.GetUtf16Length(), Is.EqualTo(1));
            Assert.That(first.GetXAdvance(),
                Is.EqualTo((firstWidth - 100) * scale).Within(0.000001));
            Assert.That(first.GetYAdvance(),
                Is.EqualTo(25 * scale).Within(0.000001));
            Assert.That(first.GetXOffset(),
                Is.EqualTo(20 * scale).Within(0.000001));
            Assert.That(first.GetYOffset(),
                Is.EqualTo(-30 * scale).Within(0.000001));
            Assert.That(first.GetBaselineShift(), Is.EqualTo(2.5));
            Assert.That(first.HasExplicitPlacement(), Is.True);
            Assert.That(encoded.GetGlyph(1).HasExplicitPlacement(), Is.True);
            Assert.That(encoded.GetXAdvance(), Is.EqualTo(
                (firstWidth + secondWidth - 50) * scale).Within(0.000001));
            Assert.That(encoded.GetYAdvance(),
                Is.EqualTo(25 * scale).Within(0.000001));
            Assert.That(encoded.GetBaselineShift(), Is.EqualTo(2.5));
            Assert.That(encoded.GetSourceUtf16Start(), Is.Zero);
            Assert.That(encoded.GetSourceUtf16Length(), Is.EqualTo(2));
            Assert.That(encoded.GetTextAdjustmentCount(), Is.EqualTo(2));
            AssertAdjustment(
                encoded.GetTextAdjustment(0), 0, firstAdjustment);
            AssertAdjustment(
                encoded.GetTextAdjustment(1), 1, secondAdjustment);
            Assert.That(encoded.GetGlyphs()[0], Is.Not.Null);
            Assert.That(encoded.GetPositions()[0], Is.SameAs(first));
            Assert.That(encoded.GetTextAdjustments()[0], Is.Not.Null);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                encoded.GetGlyph(2));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                encoded.GetTextAdjustment(2));
        });
    }

    [Test]
    public void ReturnsVerticalAdvancesOffsetsAndTjSuggestions()
    {
        FontInstance instance = HarfBuzzLayoutDifferentialTests.Load()
            .CreateDefaultInstance();
        int unitsPerEm = instance.GetGlobalMetrics().GetUnitsPerEm();
        int advanceHeight = instance.GetGlyphMetrics(4).GetAdvanceHeight();
        PdfFontOptions options = PdfFontOptions.Create();
        options.SetWritingMode(PdfWritingMode.Vertical);
        PdfFontUsage usage = new PdfFontUsageAccumulator().GetOrAdd(
            instance,
            options);
        ShapedRun run = CreatePositionedRun(
            new int[] { 4 },
            ShapingDirection.TopToBottom,
            new double[] { 10 },
            new double[] { -advanceHeight + 120 },
            new double[] { 40 },
            new double[] { -15 });

        PdfEncodedRun encoded = PdfFontRunEncoder.Encode(
            usage,
            PdfCidAllocator.Create(usage),
            run,
            -3);
        PdfPositionedGlyph glyph = encoded.GetGlyph(0);
        double scale = 1000.0 / unitsPerEm;
        double adjustment =
            PdfCidVerticalWidthsBuilder.ConvertDisplacement(
                advanceHeight,
                unitsPerEm) -
            (-advanceHeight + 120) * scale;
        double bottomAdvance =
            -PdfCidVerticalWidthsBuilder.ConvertDisplacement(
                advanceHeight,
                unitsPerEm) * unitsPerEm / 1000.0;
        PdfEncodedRun bottomToTop = PdfFontRunEncoder.Encode(
            usage,
            PdfCidAllocator.Create(usage),
            CreatePositionedRun(
                new int[] { 4 },
                ShapingDirection.BottomToTop,
                new double[] { 0 },
                new double[] { bottomAdvance },
                new double[] { 0 },
                new double[] { 0 }));

        Assert.Multiple(() =>
        {
            Assert.That(encoded.GetWritingMode(),
                Is.EqualTo(PdfWritingMode.Vertical));
            Assert.That(glyph.GetXAdvance(),
                Is.EqualTo(10 * scale).Within(0.000001));
            Assert.That(glyph.GetYAdvance(), Is.EqualTo(
                (-advanceHeight + 120) * scale).Within(0.000001));
            Assert.That(glyph.GetXOffset(),
                Is.EqualTo(40 * scale).Within(0.000001));
            Assert.That(glyph.GetYOffset(),
                Is.EqualTo(-15 * scale).Within(0.000001));
            Assert.That(glyph.GetBaselineShift(), Is.EqualTo(-3));
            Assert.That(encoded.GetTextAdjustmentCount(), Is.EqualTo(1));
            AssertAdjustment(
                encoded.GetTextAdjustment(0),
                0,
                adjustment);
            Assert.That(bottomToTop.GetYAdvance(),
                Is.GreaterThan(0));
            Assert.That(bottomToTop.GetTextAdjustmentCount(), Is.Zero);
        });
    }

    [Test]
    public void OmitsZeroAdjustmentsAndRejectsNonfinitePositioning()
    {
        FontInstance instance = HarfBuzzLayoutDifferentialTests.Load()
            .CreateDefaultInstance();
        int width = instance.GetGlyphMetrics(4).GetAdvanceWidth();
        int unitsPerEm = instance.GetGlobalMetrics().GetUnitsPerEm();
        double naturalAdvance =
            PdfCidWidthsBuilder.ConvertAdvance(width, unitsPerEm) *
            unitsPerEm / 1000.0;
        PdfFontUsage usage = new PdfFontUsageAccumulator().GetOrAdd(
            instance,
            PdfFontOptions.Create());
        PdfCidAllocator allocator = PdfCidAllocator.Create(usage);
        ShapedRun naturalRun = CreatePositionedRun(
            new int[] { 4 },
            ShapingDirection.LeftToRight,
            new double[] { naturalAdvance },
            new double[] { 0 },
            new double[] { 0 },
            new double[] { 0 });
        PdfEncodedRun natural = PdfFontRunEncoder.Encode(
            usage,
            allocator,
            naturalRun);
        ShapedRun invalid = CreatePositionedRun(
            new int[] { 5 },
            ShapingDirection.LeftToRight,
            new double[] { double.PositiveInfinity },
            new double[] { 0 },
            new double[] { 0 },
            new double[] { 0 });

        Assert.Multiple(() =>
        {
            Assert.That(natural.GetTextAdjustmentCount(), Is.Zero);
            Assert.That(natural.GetGlyph(0).HasExplicitPlacement(), Is.False);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                PdfFontRunEncoder.Encode(
                    usage,
                    allocator,
                    naturalRun,
                    double.NaN));
        });
        Assert.Throws<ArgumentException>(() => PdfFontRunEncoder.Encode(
            usage,
            allocator,
            invalid));
    }

    [Test]
    public void OneByteCapacityFailureIsAtomic()
    {
        FontInstance instance = CreateTrueTypeInstance(300);
        PdfFontOptions options = PdfFontOptions.Create();
        options.SetFontKind(PdfFontKind.SimpleTrueType);
        PdfFontUsage usage = new PdfFontUsageAccumulator().GetOrAdd(
            instance,
            options);
        PdfCidAllocator allocator = PdfCidAllocator.Create(usage);
        int glyphId = 1;

        while (glyphId <= 255)
        {
            allocator.GetOrAddCid(glyphId);
            glyphId += 1;
        }

        Assert.Throws<FontLimitException>(() => PdfFontRunEncoder.Encode(
            usage,
            allocator,
            CreateRun(
                new int[] { 256 },
                ShapingDirection.LeftToRight)));
        Assert.Multiple(() =>
        {
            Assert.That(allocator.GetCount(), Is.EqualTo(256));
            Assert.That(usage.CopyGlyphIds(), Is.EqualTo(new int[] { 0 }));
        });
    }

    [Test]
    public void SimpleCodesRejectUnsafeFontVersionAndWritingChoices()
    {
        FontInstance trueType = HarfBuzzLayoutDifferentialTests.Load()
            .CreateDefaultInstance();
        PdfFontOptions oldVersion = SimpleOptions();
        oldVersion.SetVersion(PdfVersion.Pdf1_0);
        PdfFontOptions vertical = SimpleOptions();
        vertical.SetWritingMode(PdfWritingMode.Vertical);
        FontFace cffFace = FontInstanceCffTests.CreateFace(
            FontOutlineKind.Cff1,
            FontTags.Cff,
            CffCharsetEncodingTests.BuildCidCff(false),
            4);

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidOperationException>(() => Encode(
                trueType,
                oldVersion,
                CreateRun(
                    new int[] { 4 },
                    ShapingDirection.LeftToRight)));
            Assert.Throws<InvalidOperationException>(() => Encode(
                trueType,
                vertical,
                CreateRun(
                    new int[] { 4 },
                    ShapingDirection.TopToBottom)));
            Assert.Throws<InvalidOperationException>(() => Encode(
                cffFace.CreateDefaultInstance(),
                SimpleOptions(),
                CreateRun(
                    new int[] { 1 },
                    ShapingDirection.LeftToRight)));
        });
    }

    [Test]
    public void RejectsMismatchedWritingModeAllocatorAndIndexes()
    {
        FontInstance instance = HarfBuzzLayoutDifferentialTests.Load()
            .CreateDefaultInstance();
        PdfFontUsage usage = new PdfFontUsageAccumulator().GetOrAdd(
            instance,
            PdfFontOptions.Create());
        PdfCidAllocator wrongAllocator = PdfCidAllocator.Create(4);

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() =>
                PdfFontRunEncoder.Encode(
                    usage,
                    PdfCidAllocator.Create(usage),
                    CreateRun(
                        new int[] { 4 },
                        ShapingDirection.TopToBottom)));
            Assert.Throws<ArgumentException>(() =>
                PdfFontRunEncoder.Encode(
                    usage,
                    wrongAllocator,
                    CreateRun(
                        new int[] { 1 },
                        ShapingDirection.LeftToRight)));
        });

        PdfEncodedRun encoded = PdfFontRunEncoder.Encode(
            usage,
            PdfCidAllocator.Create(usage),
            CreateRun(
                new int[] { 4 },
                ShapingDirection.LeftToRight));
        Assert.Throws<ArgumentOutOfRangeException>(() => encoded.GetCid(1));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            encoded.GetSourceGlyphId(-1));
    }

    static PdfEncodedRun Encode(
        FontInstance instance,
        PdfFontOptions options,
        ShapedRun run)
    {
        PdfFontUsage usage = new PdfFontUsageAccumulator().GetOrAdd(
            instance,
            options);
        return PdfFontRunEncoder.Encode(
            usage,
            PdfCidAllocator.Create(usage),
            run);
    }

    static PdfFontOptions SimpleOptions()
    {
        PdfFontOptions options = PdfFontOptions.Create();
        options.SetFontKind(PdfFontKind.SimpleTrueType);
        return options;
    }

    static ShapedRun CreateRun(
        int[] glyphIds,
        ShapingDirection direction)
    {
        ShapedGlyph[] glyphs = new ShapedGlyph[glyphIds.Length];
        ShapedCluster[] clusters = new ShapedCluster[glyphIds.Length];
        int[] identity = new int[glyphIds.Length];
        int index = 0;

        while (index < glyphIds.Length)
        {
            glyphs[index] = new ShapedGlyph(
                glyphIds[index],
                index,
                index,
                1,
                new int[] { 0x41 + index },
                1,
                0,
                index,
                index,
                ShapedGlyphFlags.None,
                500,
                0,
                0,
                0);
            clusters[index] = new ShapedCluster(
                index,
                1,
                new int[] { 0x41 + index },
                new int[] { index },
                index,
                index,
                false);
            identity[index] = index;
            index += 1;
        }

        return new ShapedRun(
            glyphs,
            clusters,
            identity,
            identity,
            identity,
            identity,
            glyphIds.Length,
            direction);
    }

    static ShapedRun CreatePositionedRun(
        int[] glyphIds,
        ShapingDirection direction,
        double[] xAdvances,
        double[] yAdvances,
        double[] xOffsets,
        double[] yOffsets)
    {
        ShapedGlyph[] glyphs = new ShapedGlyph[glyphIds.Length];
        ShapedCluster[] clusters = new ShapedCluster[glyphIds.Length];
        int[] identity = new int[glyphIds.Length];
        int index = 0;

        while (index < glyphIds.Length)
        {
            glyphs[index] = new ShapedGlyph(
                glyphIds[index],
                index,
                index,
                1,
                new int[] { 0x41 + index },
                1,
                0,
                index,
                index,
                ShapedGlyphFlags.None,
                xAdvances[index],
                yAdvances[index],
                xOffsets[index],
                yOffsets[index]);
            clusters[index] = new ShapedCluster(
                index,
                1,
                new int[] { 0x41 + index },
                new int[] { index },
                index,
                index,
                false);
            identity[index] = index;
            index += 1;
        }

        return new ShapedRun(
            glyphs,
            clusters,
            identity,
            identity,
            identity,
            identity,
            glyphIds.Length,
            direction);
    }

    static void AssertAdjustment(
        PdfTjAdjustment adjustment,
        int afterGlyphIndex,
        double value)
    {
        Assert.Multiple(() =>
        {
            Assert.That(adjustment.GetAfterGlyphIndex(),
                Is.EqualTo(afterGlyphIndex));
            Assert.That(adjustment.GetValue(),
                Is.EqualTo(value).Within(0.000001));
        });
    }

    static FontInstance CreateTrueTypeInstance(int glyphCount)
    {
        byte[] maxp = new TestBinaryBuilder()
            .WriteUInt32(0x00010000U)
            .WriteUInt16((ushort)glyphCount)
            .WriteUInt16(0)
            .WriteUInt16(0)
            .WriteUInt16(0)
            .WriteUInt16(0)
            .WriteUInt16(1)
            .WriteUInt16(0)
            .WriteUInt16(0)
            .WriteUInt16(0)
            .WriteUInt16(0)
            .WriteUInt16(0)
            .WriteUInt16(0)
            .WriteUInt16(0)
            .WriteUInt16(0)
            .CopyBytes();
        FontData data = FontData.Create(maxp);
        SfntTableData table = new SfntTableData(
            FontTags.Maxp,
            FontChecksum.CalculateTable(maxp),
            data.GetSlice(0, data.GetLength()),
            SfntTableClassification.Known,
            true);
        SfntTableCache cache = SfntTableCache.Create(
            new SfntTableStore(
                new SfntTableData[] { table },
                Array.Empty<FontDiagnostic>()),
            FontLoadMode.Strict);
        FontFace face = new FontFace(
            0,
            0x00010000U,
            new SfntValidationResult(
                FontOutlineKind.TrueType,
                Array.Empty<FontDiagnostic>()),
            cache);
        return face.CreateDefaultInstance();
    }
}
