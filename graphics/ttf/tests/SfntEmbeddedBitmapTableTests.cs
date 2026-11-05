using ttf;

namespace ttf.Tests;

internal class SfntEmbeddedBitmapTableTests
{
    static byte[] png = new byte[] {
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2
    };

    [TestCase(1)]
    [TestCase(3)]
    public void ParsesContiguousVariableIndexFormats(int indexFormat)
    {
        byte[] first = Join(SmallMetrics(2, 3, -1, 4, 5), new byte[] { 0xA0 });
        byte[] second = Join(SmallMetrics(4, 5, 2, 6, 7), new byte[] { 0xB0, 0xC0 });
        byte[] data = BuildDataTable(2, new byte[][] { first, second });
        byte[] index = VariableIndex(indexFormat, 1, new int[] {
            0, first.Length, first.Length + second.Length
        });
        SfntBitmapStrike strike = ParseEblc(
            BuildLocationTable(2, 1, 2, 1, 1, index), data, 5)!
            .GetStrike(0);
        SfntBitmapGlyphData glyph = strike.GetGlyph(2);

        Assert.Multiple(() =>
        {
            Assert.That(strike.IsColor(), Is.False);
            Assert.That(strike.GetPixelsPerEmX(), Is.EqualTo(16));
            Assert.That(strike.GetPixelsPerEmY(), Is.EqualTo(17));
            Assert.That(strike.GetBitDepth(), Is.EqualTo(1));
            Assert.That(strike.GetHorizontalLineMetrics().GetAscender(), Is.EqualTo(8));
            Assert.That(strike.GetVerticalLineMetrics().GetDescender(), Is.EqualTo(-3));
            Assert.That(glyph.GetImageFormat(), Is.EqualTo(1));
            Assert.That(glyph.GetMetrics()!.IsBig(), Is.False);
            Assert.That(glyph.GetMetrics()!.GetHeight(), Is.EqualTo(4));
            Assert.That(glyph.GetMetrics()!.GetBearingX(), Is.EqualTo(2));
            Assert.That(glyph.CopyPayload(), Is.EqualTo(new byte[] { 0xB0, 0xC0 }));
            Assert.That(glyph.GetDataOffset(), Is.EqualTo(4 + first.Length));
            Assert.That(strike.HasGlyph(0), Is.False);
        });
    }

    [Test]
    public void ParsesConstantMetricsIndexFormatTwo()
    {
        byte[] metrics = BigMetrics(3, 4, 1, 5, 6, -2, 7, 8);
        byte[] records = new byte[] { 0x11, 0x22, 0x33, 0x44 };
        byte[] data = BuildDataTable(2, new byte[][] { records });
        byte[] index = ConstantIndex(2, 5, 2, metrics, null);
        SfntBitmapStrike strike = ParseEblc(
            BuildLocationTable(2, 1, 2, 1, 1, index), data, 5)!
            .GetStrike(0);

        Assert.Multiple(() =>
        {
            Assert.That(strike.GetGlyph(1).CopyPayload(),
                Is.EqualTo(new byte[] { 0x11, 0x22 }));
            Assert.That(strike.GetGlyph(2).CopyPayload(),
                Is.EqualTo(new byte[] { 0x33, 0x44 }));
            Assert.That(strike.GetGlyph(2).GetMetrics()!.IsBig(), Is.True);
            Assert.That(strike.GetGlyph(2).GetMetrics()!.GetVerticalAdvance(), Is.EqualTo(8));
        });
    }

    [Test]
    public void ParsesSparseVariableIndexFormatFour()
    {
        byte[] first = Join(SmallMetrics(1, 2, 3, 4, 5), new byte[] { 0x10 });
        byte[] second = Join(SmallMetrics(6, 7, 8, 9, 10), new byte[] { 0x20 });
        byte[] data = BuildDataTable(2, new byte[][] { first, second });
        byte[] index = SparseVariableIndex(
            2,
            new int[] { 1, 4 },
            new int[] { 0, first.Length, first.Length + second.Length });
        SfntBitmapStrike strike = ParseEblc(
            BuildLocationTable(2, 1, 4, 1, 1, index), data, 6)!
            .GetStrike(0);

        Assert.Multiple(() =>
        {
            Assert.That(strike.HasGlyph(1), Is.True);
            Assert.That(strike.HasGlyph(2), Is.False);
            Assert.That(strike.HasGlyph(4), Is.True);
            Assert.That(strike.GetGlyph(4).GetImageFormat(), Is.EqualTo(2));
            Assert.That(strike.GetGlyph(4).CopyPayload(), Is.EqualTo(new byte[] { 0x20 }));
        });
    }

    [Test]
    public void ParsesSparseConstantIndexFormatFive()
    {
        byte[] data = BuildDataTable(2, new byte[][] {
            new byte[] { 1, 2, 3, 4 }
        });
        byte[] index = ConstantIndex(
            5,
            5,
            2,
            BigMetrics(2, 2, 0, 2, 3, 0, 2, 3),
            new int[] { 1, 4 });
        SfntBitmapStrike strike = ParseEblc(
            BuildLocationTable(2, 1, 4, 1, 1, index), data, 6)!
            .GetStrike(0);

        Assert.Multiple(() =>
        {
            Assert.That(strike.GetGlyph(1).CopyPayload(), Is.EqualTo(new byte[] { 1, 2 }));
            Assert.That(strike.HasGlyph(2), Is.False);
            Assert.That(strike.GetGlyph(4).CopyPayload(), Is.EqualTo(new byte[] { 3, 4 }));
        });
    }

    [TestCase(6)]
    [TestCase(7)]
    public void ParsesBigMetricRasterImageFormats(int imageFormat)
    {
        byte[] record = Join(
            BigMetrics(5, 6, 1, 4, 7, -1, 5, 8),
            new byte[] { 0xAA, 0xBB });
        SfntBitmapGlyphData glyph = ParseOneEbdtGlyph(imageFormat, record);

        Assert.Multiple(() =>
        {
            Assert.That(glyph.IsSupported(), Is.True);
            Assert.That(glyph.GetMetrics()!.IsBig(), Is.True);
            Assert.That(glyph.GetMetrics()!.GetWidth(), Is.EqualTo(6));
            Assert.That(glyph.GetMetrics()!.GetVerticalBearingX(), Is.EqualTo(-1));
            Assert.That(glyph.CopyPayload(), Is.EqualTo(new byte[] { 0xAA, 0xBB }));
        });
    }

    [TestCase(8)]
    [TestCase(9)]
    public void ParsesCompositeImageFormatsAndComponents(int imageFormat)
    {
        byte[] empty = CompositeRecord(imageFormat, new int[0]);
        byte[] composite = CompositeRecord(imageFormat, new int[] { 0, 3, -4 });
        byte[] data = BuildDataTable(2, new byte[][] { empty, composite });
        byte[] index = VariableIndex(1, imageFormat, new int[] {
            0, empty.Length, empty.Length + composite.Length
        });
        SfntBitmapGlyphData glyph = ParseEblc(
            BuildLocationTable(2, 0, 1, 1, 1, index), data, 3)!
            .GetStrike(0).GetGlyph(1);

        Assert.Multiple(() =>
        {
            Assert.That(glyph.GetComponentCount(), Is.EqualTo(1));
            Assert.That(glyph.GetComponent(0).GetGlyphId(), Is.Zero);
            Assert.That(glyph.GetComponent(0).GetXOffset(), Is.EqualTo(3));
            Assert.That(glyph.GetComponent(0).GetYOffset(), Is.EqualTo(-4));
            Assert.That(glyph.GetPayloadLength(), Is.Zero);
        });
    }

    [TestCase(17)]
    [TestCase(18)]
    public void ParsesInlineMetricCbdtPngFormats(int imageFormat)
    {
        byte[] metrics = imageFormat == 17 ?
            SmallMetrics(3, 4, 1, 2, 5) :
            BigMetrics(3, 4, 1, 2, 5, -1, 3, 6);
        byte[] record = Join(metrics, UInt32Bytes(png.Length), png);
        SfntBitmapGlyphData glyph = ParseOneCbdtGlyph(imageFormat, record, 32);

        Assert.Multiple(() =>
        {
            Assert.That(glyph.IsPng(), Is.True);
            Assert.That(glyph.IsSupported(), Is.True);
            Assert.That(glyph.CopyPayload(), Is.EqualTo(png));
            Assert.That(glyph.GetMetrics()!.GetWidth(), Is.EqualTo(4));
        });
    }

    [Test]
    public void ParsesSharedMetricCbdtPngFormatNineteen()
    {
        byte[] record = Join(UInt32Bytes(png.Length), png);
        byte[] data = BuildDataTable(3, new byte[][] { record });
        byte[] index = ConstantIndex(
            2,
            19,
            record.Length,
            BigMetrics(9, 10, 1, 8, 11, -1, 8, 12),
            null);
        SfntBitmapLocationTable? table = ParseCblc(
            BuildLocationTable(3, 1, 1, 32, 1, index), data, 4);
        SfntBitmapGlyphData glyph = table!.GetStrike(0).GetGlyph(1);

        Assert.Multiple(() =>
        {
            Assert.That(table.IsColor(), Is.True);
            Assert.That(glyph.IsPng(), Is.True);
            Assert.That(glyph.GetMetrics()!.GetHeight(), Is.EqualTo(9));
            Assert.That(glyph.CopyPayload(), Is.EqualTo(png));
        });
    }

    [Test]
    public void PreservesUnsupportedImageFormatsAsOpaquePayloads()
    {
        byte[] record = new byte[] { 1, 2, 3, 4 };
        SfntBitmapGlyphData glyph = ParseOneEbdtGlyph(4, record);

        Assert.Multiple(() =>
        {
            Assert.That(glyph.IsSupported(), Is.False);
            Assert.That(glyph.HasMetrics(), Is.False);
            Assert.That(glyph.CopyPayload(), Is.EqualTo(record));
        });
    }

    [Test]
    public void ExposesPublicEbdtPayloadGlyphAndLineMetrics()
    {
        byte[] record = Join(
            BigMetrics(5, 6, 1, 4, 7, -1, 5, 8),
            new byte[] { 0xAA, 0xBB });
        byte[] data = BuildDataTable(2, new byte[][] { record });
        byte[] location = BuildLocationTable(
            2,
            1,
            1,
            1,
            1,
            VariableIndex(1, 6, new int[] { 0, record.Length }));
        FontFace face = CreateFace(CreateCache(
            location,
            data,
            null,
            null,
            null,
            4,
            FontLimits.CreateCompatible()));

        ColorGlyph glyph = new ColorGlyphResolver().ResolveGlyph(
            face.CreateDefaultInstance(),
            1);
        ColorBitmapStrike strike = glyph.GetSelectedBitmapStrike()!;
        ColorBitmapGlyphMetrics metrics = strike.GetGlyphMetrics()!;

        Assert.Multiple(() =>
        {
            Assert.That(glyph.GetSelectedRepresentation(),
                Is.EqualTo(ColorGlyphRepresentation.Ebdt));
            Assert.That(strike.GetPayloadKind(),
                Is.EqualTo(ColorBitmapPayloadKind.RawBitmap));
            Assert.That(strike.IsSupported(), Is.True);
            Assert.That(strike.IsCompressed(), Is.False);
            Assert.That(strike.GetGlyphId(), Is.EqualTo(1));
            Assert.That(strike.GetStartGlyphId(), Is.EqualTo(1));
            Assert.That(strike.GetEndGlyphId(), Is.EqualTo(1));
            Assert.That(metrics.HasBothDirections(), Is.True);
            Assert.That(metrics.GetHeight(), Is.EqualTo(5));
            Assert.That(metrics.GetWidth(), Is.EqualTo(6));
            Assert.That(metrics.GetBearingX(), Is.EqualTo(1));
            Assert.That(metrics.GetVerticalBearingX(), Is.EqualTo(-1));
            Assert.That(metrics.GetVerticalAdvance(), Is.EqualTo(8));
            Assert.That(strike.GetHorizontalLineMetrics()!.GetAscender(),
                Is.EqualTo(8));
            Assert.That(strike.GetHorizontalLineMetrics()!.GetMaximumWidth(),
                Is.EqualTo(20));
            Assert.That(strike.GetVerticalLineMetrics()!.GetDescender(),
                Is.EqualTo(-3));
            Assert.That(strike.CopyData(), Is.EqualTo(new byte[] { 0xAA, 0xBB }));
        });
    }

    [Test]
    public void ExposesPublicCbdtPngCompressionAndMetrics()
    {
        byte[] record = Join(
            SmallMetrics(3, 4, 1, 2, 5),
            UInt32Bytes(png.Length),
            png);
        byte[] data = BuildDataTable(3, new byte[][] { record });
        byte[] location = BuildLocationTable(
            3,
            1,
            1,
            32,
            1,
            VariableIndex(1, 17, new int[] { 0, record.Length }));
        FontFace face = CreateFace(CreateCache(
            null,
            null,
            location,
            data,
            null,
            4,
            FontLimits.CreateCompatible()));

        ColorBitmapStrike strike = new ColorGlyphResolver().ResolveGlyph(
            face.CreateDefaultInstance(),
            1).GetSelectedBitmapStrike()!;

        Assert.Multiple(() =>
        {
            Assert.That(strike.GetRepresentation(),
                Is.EqualTo(ColorGlyphRepresentation.Cbdt));
            Assert.That(strike.GetPayloadKind(),
                Is.EqualTo(ColorBitmapPayloadKind.Png));
            Assert.That(strike.IsPng(), Is.True);
            Assert.That(strike.IsCompressed(), Is.True);
            Assert.That(strike.GetGlyphMetrics()!.HasBothDirections(), Is.False);
            Assert.That(strike.GetGlyphMetrics()!.IsHorizontal(), Is.True);
            Assert.Throws<InvalidOperationException>(() =>
                strike.GetGlyphMetrics()!.GetVerticalAdvance());
            Assert.That(strike.CopyData(), Is.EqualTo(png));
        });
    }

    [Test]
    public void ExposesCompositeComponentsAndOpaqueUnsupportedPayloads()
    {
        byte[] empty = CompositeRecord(8, new int[0]);
        byte[] composite = CompositeRecord(8, new int[] { 0, 3, -4 });
        byte[] compositeData = BuildDataTable(
            2,
            new byte[][] { empty, composite });
        byte[] compositeLocation = BuildLocationTable(
            2,
            0,
            1,
            1,
            1,
            VariableIndex(1, 8, new int[] {
                0, empty.Length, empty.Length + composite.Length
            }));
        FontFace compositeFace = CreateFace(CreateCache(
            compositeLocation,
            compositeData,
            null,
            null,
            null,
            3,
            FontLimits.CreateCompatible()));
        ColorBitmapStrike compositeStrike = new ColorGlyphResolver().ResolveGlyph(
            compositeFace.CreateDefaultInstance(),
            1).GetSelectedBitmapStrike()!;

        byte[] opaque = new byte[] { 1, 2, 3, 4 };
        byte[] opaqueData = BuildDataTable(2, new byte[][] { opaque });
        byte[] opaqueLocation = BuildLocationTable(
            2,
            0,
            0,
            1,
            1,
            VariableIndex(1, 4, new int[] { 0, opaque.Length }));
        FontFace opaqueFace = CreateFace(CreateCache(
            opaqueLocation,
            opaqueData,
            null,
            null,
            null,
            2,
            FontLimits.CreateCompatible()));
        ColorGlyph opaqueGlyph = new ColorGlyphResolver().ResolveGlyph(
            opaqueFace.CreateDefaultInstance(),
            0);
        ColorBitmapStrike opaqueStrike = opaqueGlyph.GetBitmapStrikes()[0];

        Assert.Multiple(() =>
        {
            Assert.That(compositeStrike.GetPayloadKind(),
                Is.EqualTo(ColorBitmapPayloadKind.CompositeBitmap));
            Assert.That(compositeStrike.GetComponentCount(), Is.EqualTo(1));
            Assert.That(compositeStrike.GetComponent(0).GetGlyphId(), Is.Zero);
            Assert.That(compositeStrike.GetComponent(0).GetXOffset(), Is.EqualTo(3));
            Assert.That(compositeStrike.GetComponent(0).GetYOffset(), Is.EqualTo(-4));
            Assert.That(opaqueGlyph.GetSelectedRepresentation(),
                Is.EqualTo(ColorGlyphRepresentation.None));
            Assert.That(opaqueStrike.GetPayloadKind(),
                Is.EqualTo(ColorBitmapPayloadKind.Unsupported));
            Assert.That(opaqueStrike.IsSupported(), Is.False);
            Assert.That(opaqueStrike.CopyData(), Is.EqualTo(opaque));
        });
    }

    [Test]
    public void ParsesEbscScalingRecordsAgainstSubstituteStrike()
    {
        byte[] record = Join(SmallMetrics(1, 1, 0, 1, 1), new byte[] { 0x80 });
        byte[] data = BuildDataTable(2, new byte[][] { record });
        byte[] location = BuildLocationTable(
            2, 1, 1, 1, 1,
            VariableIndex(1, 1, new int[] { 0, record.Length }));
        byte[] ebsc = BuildEbsc(32, 34, 16, 17);
        SfntEbscTable? table = SfntEbscTableParser.FindTable(CreateCache(
            location, data, null, null, ebsc, 4, FontLimits.CreateCompatible()));
        SfntBitmapScale scale = table!.GetScale(0);

        Assert.Multiple(() =>
        {
            Assert.That(table.GetScaleCount(), Is.EqualTo(1));
            Assert.That(table.FindScale(32, 34), Is.SameAs(scale));
            Assert.That(scale.GetSubstitutePixelsPerEmX(), Is.EqualTo(16));
            Assert.That(scale.GetSubstitutePixelsPerEmY(), Is.EqualTo(17));
            Assert.That(scale.GetHorizontalLineMetrics().GetAscender(), Is.EqualTo(8));
        });
    }

    [Test]
    public void RejectsEbscMissingSubstituteAndZeroTargetSizes()
    {
        byte[] record = Join(SmallMetrics(1, 1, 0, 1, 1), new byte[] { 0x80 });
        byte[] data = BuildDataTable(2, new byte[][] { record });
        byte[] location = BuildLocationTable(
            2, 1, 1, 1, 1,
            VariableIndex(1, 1, new int[] { 0, record.Length }));
        SfntTableCache missingSubstitute = CreateCache(
            location, data, null, null, BuildEbsc(32, 34, 20, 20),
            4, FontLimits.CreateCompatible());
        SfntTableCache zeroTarget = CreateCache(
            location, data, null, null, BuildEbsc(0, 34, 16, 17),
            4, FontLimits.CreateCompatible());

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() =>
                SfntEbscTableParser.FindTable(missingSubstitute));
            Assert.Throws<FontFormatException>(() =>
                SfntEbscTableParser.FindTable(zeroTarget));
        });
    }

    [Test]
    public void RejectsCompositeCyclesAndDepthOverflow()
    {
        byte[] firstCycle = CompositeRecord(8, new int[] { 1, 0, 0 });
        byte[] secondCycle = CompositeRecord(8, new int[] { 0, 0, 0 });
        byte[] cycleData = BuildDataTable(2, new byte[][] { firstCycle, secondCycle });
        byte[] cycleIndex = VariableIndex(1, 8, new int[] {
            0, firstCycle.Length, firstCycle.Length + secondCycle.Length
        });
        byte[][] chain = new byte[][] {
            CompositeRecord(8, new int[0]),
            CompositeRecord(8, new int[] { 0, 0, 0 }),
            CompositeRecord(8, new int[] { 1, 0, 0 }),
            CompositeRecord(8, new int[] { 2, 0, 0 })
        };
        byte[] chainData = BuildDataTable(2, chain);
        int[] chainOffsets = CumulativeOffsets(chain);

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() => ParseEblc(
                BuildLocationTable(2, 0, 1, 1, 1, cycleIndex), cycleData, 4));
            Assert.Throws<FontLimitException>(() => ParseEblc(
                BuildLocationTable(
                    2, 0, 3, 1, 1,
                    VariableIndex(1, 8, chainOffsets)),
                chainData,
                4,
                CreateLimits(100, 2)));
        });
    }

    [TestCaseSource(nameof(InvalidBitmapTables))]
    public void RejectsMalformedLocationIndexAndImageData(
        byte[] location,
        byte[] data,
        bool color)
    {
        Assert.Throws<FontFormatException>(() =>
        {
            if (color)
            {
                ParseCblc(location, data, 5);
            }
            else
            {
                ParseEblc(location, data, 5);
            }
        });
    }

    [Test]
    public void EnforcesCoverageLimitsAndValidatesAccessors()
    {
        byte[] record = Join(SmallMetrics(1, 1, 0, 1, 1), new byte[] { 1 });
        byte[] data = BuildDataTable(2, new byte[][] { record });
        byte[] location = BuildLocationTable(
            2, 1, 1, 1, 1,
            VariableIndex(1, 1, new int[] { 0, record.Length }));
        SfntBitmapLocationTable? table = ParseEblc(location, data, 4);

        Assert.Multiple(() =>
        {
            Assert.Throws<FontLimitException>(() => ParseEblc(
                location, data, 4, CreateLimits(2, 10)));
            Assert.Throws<ArgumentOutOfRangeException>(() => table!.GetStrike(1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                table!.GetStrike(0).GetGlyph(4));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                table!.GetStrike(0).GetGlyph(1).GetComponent(0));
        });
    }

    static IEnumerable<TestCaseData> InvalidBitmapTables()
    {
        byte[] record = Join(SmallMetrics(1, 1, 0, 1, 1), new byte[] { 1 });
        byte[] data = BuildDataTable(2, new byte[][] { record });

        byte[] badFlags = BuildLocationTable(
            2, 1, 1, 1, 3,
            VariableIndex(1, 1, new int[] { 0, record.Length }));
        yield return InvalidCase(badFlags, data, false, "BadFlags");

        byte[] badDepth = BuildLocationTable(
            2, 1, 1, 32, 1,
            VariableIndex(1, 1, new int[] { 0, record.Length }));
        yield return InvalidCase(badDepth, data, false, "BadDepth");

        byte[] unknownIndex = BuildLocationTable(
            2, 1, 1, 1, 1,
            new TestBinaryBuilder()
                .WriteUInt16(6).WriteUInt16(1).WriteUInt32(4).CopyBytes());
        yield return InvalidCase(unknownIndex, data, false, "UnknownIndex");

        byte[] unsortedOffsets = BuildLocationTable(
            2, 1, 2, 1, 1,
            VariableIndex(1, 1, new int[] { 0, 10, 5 }));
        yield return InvalidCase(unsortedOffsets, data, false, "UnsortedOffsets");

        byte[] outsideData = BuildLocationTable(
            2, 1, 1, 1, 1,
            VariableIndex(1, 1, new int[] { 0, 100 }));
        yield return InvalidCase(outsideData, data, false, "OutsideData");

        byte[] missingSharedMetrics = BuildLocationTable(
            2, 1, 1, 1, 1,
            VariableIndex(1, 5, new int[] { 0, 1 }));
        yield return InvalidCase(
            missingSharedMetrics,
            BuildDataTable(2, new byte[][] { new byte[] { 1 } }),
            false,
            "MissingSharedMetrics");

        byte[] badPngRecord = Join(SmallMetrics(1, 1, 0, 1, 1),
            UInt32Bytes(8), new byte[8]);
        byte[] badPngLocation = BuildLocationTable(
            3, 1, 1, 32, 1,
            VariableIndex(1, 17, new int[] { 0, badPngRecord.Length }));
        yield return InvalidCase(
            badPngLocation,
            BuildDataTable(3, new byte[][] { badPngRecord }),
            true,
            "BadPng");
    }

    static TestCaseData InvalidCase(
        byte[] location,
        byte[] data,
        bool color,
        string name)
    {
        return new TestCaseData(location, data, color)
            .SetName("Rejects" + name);
    }

    static SfntBitmapGlyphData ParseOneEbdtGlyph(
        int imageFormat,
        byte[] record)
    {
        byte[] data = BuildDataTable(2, new byte[][] { record });
        byte[] location = BuildLocationTable(
            2, 1, 1, 1, 1,
            VariableIndex(1, imageFormat, new int[] { 0, record.Length }));
        return ParseEblc(location, data, 4)!.GetStrike(0).GetGlyph(1);
    }

    static SfntBitmapGlyphData ParseOneCbdtGlyph(
        int imageFormat,
        byte[] record,
        int bitDepth)
    {
        byte[] data = BuildDataTable(3, new byte[][] { record });
        byte[] location = BuildLocationTable(
            3, 1, 1, bitDepth, 1,
            VariableIndex(1, imageFormat, new int[] { 0, record.Length }));
        return ParseCblc(location, data, 4)!.GetStrike(0).GetGlyph(1);
    }

    static SfntBitmapLocationTable? ParseEblc(
        byte[] location,
        byte[] data,
        int glyphCount)
    {
        return ParseEblc(
            location, data, glyphCount, FontLimits.CreateCompatible());
    }

    static SfntBitmapLocationTable? ParseEblc(
        byte[] location,
        byte[] data,
        int glyphCount,
        FontLimits limits)
    {
        return SfntEblcTableParser.FindTable(CreateCache(
            location, data, null, null, null, glyphCount, limits));
    }

    static SfntBitmapLocationTable? ParseCblc(
        byte[] location,
        byte[] data,
        int glyphCount)
    {
        return SfntCblcTableParser.FindTable(CreateCache(
            null, null, location, data, null, glyphCount,
            FontLimits.CreateCompatible()));
    }

    static SfntTableCache CreateCache(
        byte[]? eblc,
        byte[]? ebdt,
        byte[]? cblc,
        byte[]? cbdt,
        byte[]? ebsc,
        int glyphCount,
        FontLimits limits)
    {
        List<SfntTableData> tables = new List<SfntTableData>();
        AddTable(tables, FontTags.Eblc, eblc);
        AddTable(tables, FontTags.Ebdt, ebdt);
        AddTable(tables, FontTags.Cblc, cblc);
        AddTable(tables, FontTags.Cbdt, cbdt);
        AddTable(tables, FontTags.Ebsc, ebsc);
        AddTable(tables, FontTags.Maxp, BuildMaxp(glyphCount));
        return SfntTableCache.Create(
            new SfntTableStore(tables.ToArray(), Array.Empty<FontDiagnostic>()),
            FontLoadMode.Compatible,
            limits);
    }

    static FontFace CreateFace(SfntTableCache cache)
    {
        return new FontFace(
            0,
            0x00010000U,
            new SfntValidationResult(
                FontOutlineKind.BitmapOnly,
                Array.Empty<FontDiagnostic>()),
            cache);
    }

    static void AddTable(List<SfntTableData> tables, int tag, byte[]? bytes)
    {
        if (bytes == null)
        {
            return;
        }
        FontData data = FontData.Create(bytes);
        tables.Add(new SfntTableData(
            tag, FontChecksum.CalculateTable(bytes),
            data.GetSlice(0, data.GetLength()),
            SfntTableClassification.Known, true));
    }

    static byte[] BuildLocationTable(
        int majorVersion,
        int firstGlyphId,
        int lastGlyphId,
        int bitDepth,
        int flags,
        byte[] indexSubtable)
    {
        const int listOffset = 56;
        const int subtableOffset = 8;
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16((ushort)majorVersion)
            .WriteUInt16(0)
            .WriteUInt32(1)
            .WriteUInt32(listOffset)
            .WriteUInt32((uint)(subtableOffset + indexSubtable.Length))
            .WriteUInt32(1)
            .WriteUInt32(0);
        WriteLineMetrics(builder, 8, -2);
        WriteLineMetrics(builder, 9, -3);
        builder
            .WriteUInt16((ushort)firstGlyphId)
            .WriteUInt16((ushort)lastGlyphId)
            .WriteUInt8(16)
            .WriteUInt8(17)
            .WriteUInt8((byte)bitDepth)
            .WriteUInt8((byte)flags)
            .WriteUInt16((ushort)firstGlyphId)
            .WriteUInt16((ushort)lastGlyphId)
            .WriteUInt32(subtableOffset)
            .WriteBytes(indexSubtable);
        return builder.CopyBytes();
    }

    static void WriteLineMetrics(
        TestBinaryBuilder builder,
        int ascender,
        int descender)
    {
        builder
            .WriteInt8((sbyte)ascender)
            .WriteInt8((sbyte)descender)
            .WriteUInt8(20)
            .WriteInt8(1)
            .WriteInt8(0)
            .WriteInt8(0)
            .WriteInt8(-1)
            .WriteInt8(-2)
            .WriteInt8(3)
            .WriteInt8(-4)
            .WriteInt8(0)
            .WriteInt8(0);
    }

    static byte[] VariableIndex(
        int indexFormat,
        int imageFormat,
        int[] offsets)
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16((ushort)indexFormat)
            .WriteUInt16((ushort)imageFormat)
            .WriteUInt32(4);
        int index = 0;
        while (index < offsets.Length)
        {
            if (indexFormat == 1)
            {
                builder.WriteUInt32((uint)offsets[index]);
            }
            else
            {
                builder.WriteUInt16((ushort)offsets[index]);
            }
            index += 1;
        }
        return builder.CopyBytes();
    }

    static byte[] ConstantIndex(
        int indexFormat,
        int imageFormat,
        int imageSize,
        byte[] metrics,
        int[]? glyphIds)
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16((ushort)indexFormat)
            .WriteUInt16((ushort)imageFormat)
            .WriteUInt32(4)
            .WriteUInt32((uint)imageSize)
            .WriteBytes(metrics);
        if (indexFormat == 5)
        {
            builder.WriteUInt32((uint)glyphIds!.Length);
            int index = 0;
            while (index < glyphIds.Length)
            {
                builder.WriteUInt16((ushort)glyphIds[index]);
                index += 1;
            }
        }
        return builder.CopyBytes();
    }

    static byte[] SparseVariableIndex(
        int imageFormat,
        int[] glyphIds,
        int[] offsets)
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(4)
            .WriteUInt16((ushort)imageFormat)
            .WriteUInt32(4)
            .WriteUInt32((uint)glyphIds.Length);
        int index = 0;
        while (index < glyphIds.Length)
        {
            builder.WriteUInt16((ushort)glyphIds[index]);
            builder.WriteUInt16((ushort)offsets[index]);
            index += 1;
        }
        builder.WriteUInt16(0).WriteUInt16((ushort)offsets[offsets.Length - 1]);
        return builder.CopyBytes();
    }

    static byte[] BuildDataTable(int majorVersion, byte[][] records)
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16((ushort)majorVersion)
            .WriteUInt16(0);
        int index = 0;
        while (index < records.Length)
        {
            builder.WriteBytes(records[index]);
            index += 1;
        }
        return builder.CopyBytes();
    }

    static byte[] SmallMetrics(
        int height,
        int width,
        int bearingX,
        int bearingY,
        int advance)
    {
        return new TestBinaryBuilder()
            .WriteUInt8((byte)height)
            .WriteUInt8((byte)width)
            .WriteInt8((sbyte)bearingX)
            .WriteInt8((sbyte)bearingY)
            .WriteUInt8((byte)advance)
            .CopyBytes();
    }

    static byte[] BigMetrics(
        int height,
        int width,
        int horizontalBearingX,
        int horizontalBearingY,
        int horizontalAdvance,
        int verticalBearingX,
        int verticalBearingY,
        int verticalAdvance)
    {
        return new TestBinaryBuilder()
            .WriteUInt8((byte)height)
            .WriteUInt8((byte)width)
            .WriteInt8((sbyte)horizontalBearingX)
            .WriteInt8((sbyte)horizontalBearingY)
            .WriteUInt8((byte)horizontalAdvance)
            .WriteInt8((sbyte)verticalBearingX)
            .WriteInt8((sbyte)verticalBearingY)
            .WriteUInt8((byte)verticalAdvance)
            .CopyBytes();
    }

    static byte[] CompositeRecord(int imageFormat, int[] component)
    {
        TestBinaryBuilder builder = new TestBinaryBuilder();
        if (imageFormat == 8)
        {
            builder.WriteBytes(SmallMetrics(2, 2, 0, 2, 2)).WriteUInt8(0);
        }
        else
        {
            builder.WriteBytes(BigMetrics(2, 2, 0, 2, 2, 0, 2, 2));
        }
        int count = component.Length == 0 ? 0 : 1;
        builder.WriteUInt16((ushort)count);
        if (count == 1)
        {
            builder
                .WriteUInt16((ushort)component[0])
                .WriteInt8((sbyte)component[1])
                .WriteInt8((sbyte)component[2]);
        }
        return builder.CopyBytes();
    }

    static int[] CumulativeOffsets(byte[][] records)
    {
        int[] offsets = new int[records.Length + 1];
        int index = 0;
        while (index < records.Length)
        {
            offsets[index + 1] = offsets[index] + records[index].Length;
            index += 1;
        }
        return offsets;
    }

    static byte[] BuildEbsc(
        int pixelsPerEmX,
        int pixelsPerEmY,
        int substitutePixelsPerEmX,
        int substitutePixelsPerEmY)
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(2).WriteUInt16(0).WriteUInt32(1);
        WriteLineMetrics(builder, 8, -2);
        WriteLineMetrics(builder, 9, -3);
        return builder
            .WriteUInt8((byte)pixelsPerEmX)
            .WriteUInt8((byte)pixelsPerEmY)
            .WriteUInt8((byte)substitutePixelsPerEmX)
            .WriteUInt8((byte)substitutePixelsPerEmY)
            .CopyBytes();
    }

    static byte[] BuildMaxp(int glyphCount)
    {
        return new TestBinaryBuilder()
            .WriteUInt32(0x00005000)
            .WriteUInt16((ushort)glyphCount)
            .CopyBytes();
    }

    static byte[] UInt32Bytes(int value)
    {
        return new TestBinaryBuilder().WriteUInt32((uint)value).CopyBytes();
    }

    static byte[] Join(params byte[][] values)
    {
        TestBinaryBuilder builder = new TestBinaryBuilder();
        int index = 0;
        while (index < values.Length)
        {
            builder.WriteBytes(values[index]);
            index += 1;
        }
        return builder.CopyBytes();
    }

    static FontLimits CreateLimits(
        int maximumCoverageEntries,
        int maximumComponentDepth)
    {
        return FontLimits.Create(
            10000, 10000, 10, 100, 100, 100, 100,
            maximumComponentDepth, 10, 10, 100, 1000,
            maximumCoverageEntries, 1000, 1000, 10, 100, 20, 100);
    }
}
