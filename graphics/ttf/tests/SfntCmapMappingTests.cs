namespace ttf.Tests;

internal class SfntCmapMappingTests
{
    [Test]
    public void RanksUnicodeSourcesAndFallsBackAcrossTheirMergedMappings()
    {
        byte[] unicodeFull = BuildFormat6(65, new int[] { 2, 0, 4 });
        byte[] windowsBmp = BuildFormat6(65, new int[] { 1, 3, 0, 7 });
        byte[] windowsFull = BuildFormat6(65, new int[] { 5, 0, 6 });
        byte[] cmap = BuildCmap(
            new int[] { 0, 3, 3 },
            new int[] { 4, 1, 10 },
            new byte[][] { unicodeFull, windowsBmp, windowsFull });
        SfntTableCache cache = CreateCache(cmap, 8);
        SfntCmapMapping mapping = SfntCmapTableParser
            .FindTable(cache)!
            .GetMapping();
        FontFace face = CreateFace(cache);

        Assert.Multiple(() =>
        {
            Assert.That(mapping.GetSourceCount(), Is.EqualTo(3));
            Assert.That(mapping.GetSource(0).GetPlatformId(), Is.EqualTo(3));
            Assert.That(mapping.GetSource(0).GetEncodingId(), Is.EqualTo(10));
            Assert.That(mapping.GetSourceRank(0), Is.EqualTo(100));
            Assert.That(mapping.GetSource(1).GetPlatformId(), Is.Zero);
            Assert.That(mapping.GetSource(1).GetEncodingId(), Is.EqualTo(4));
            Assert.That(mapping.GetSourceRank(1), Is.EqualTo(120));
            Assert.That(mapping.GetSource(2).GetEncodingId(), Is.EqualTo(1));
            Assert.That(mapping.GetSourceRank(2), Is.EqualTo(200));
            Assert.That(mapping.MapUnicode(65), Is.EqualTo(5));
            Assert.That(mapping.MapUnicode(66), Is.EqualTo(3));
            Assert.That(mapping.MapUnicode(67), Is.EqualTo(6));
            Assert.That(mapping.MapUnicode(68), Is.EqualTo(7));
            Assert.That(mapping.MapUnicode(69), Is.Zero);
            Assert.That(mapping.GetCoverageRangeCount(), Is.EqualTo(1));
            Assert.That(mapping.GetCoverageRange(0).GetStartUnicodeValue(),
                Is.EqualTo(65));
            Assert.That(mapping.GetCoverageRange(0).GetEndUnicodeValue(),
                Is.EqualTo(68));
            Assert.That(mapping.CoversUnicodeValue(67), Is.True);
            Assert.That(mapping.CoversUnicodeValue(69), Is.False);
            Assert.That(face.GetCmapMapping(), Is.SameAs(mapping));
        });
    }

    [Test]
    public void UsesSymbolSubtableWithDirectThenPrivateUseFallbackRules()
    {
        byte[] unicode = BuildFormat6(66, new int[] { 2 });
        byte[] symbol = BuildGroupedFormat(
            12,
            new uint[] { 0x41, 0xF041 },
            new uint[] { 0x41, 0xF042 },
            new uint[] { 3, 4 });
        SfntCmapMapping mapping = Parse(
            BuildCmap(
                new int[] { 0, 3 },
                new int[] { 4, 0 },
                new byte[][] { unicode, symbol }),
            6)!.GetMapping();

        Assert.Multiple(() =>
        {
            Assert.That(mapping.IsSymbolMapping(), Is.True);
            Assert.That(mapping.GetSourceCount(), Is.EqualTo(1));
            Assert.That(mapping.GetSource(0).GetPlatformId(), Is.EqualTo(3));
            Assert.That(mapping.GetSource(0).GetEncodingId(), Is.Zero);
            Assert.That(mapping.MapUnicode(0x41), Is.EqualTo(3));
            Assert.That(mapping.MapUnicode(0x42), Is.EqualTo(5));
            Assert.That(mapping.MapUnicode(0xF041), Is.EqualTo(4));
            Assert.That(mapping.CoversUnicodeValue(0x41), Is.True);
            Assert.That(mapping.CoversUnicodeValue(0x42), Is.True);
            Assert.That(mapping.CoversUnicodeValue(0xF041), Is.True);
        });
    }

    [Test]
    public void ResolvesDefaultAndNonDefaultVariationMappings()
    {
        byte[] nominal = BuildFormat6(0x41, new int[] { 1, 2 });
        byte[] variations = BuildFormat14(0xFE0F, 0x41, 0x42, 3);
        SfntCmapMapping mapping = Parse(
            BuildCmap(
                new int[] { 0, 0 },
                new int[] { 4, 5 },
                new byte[][] { nominal, variations }),
            4)!.GetMapping();

        Assert.Multiple(() =>
        {
            Assert.That(mapping.GetVariations(), Is.Not.Null);
            Assert.That(mapping.MapVariation(0x41, 0xFE0F), Is.EqualTo(1));
            Assert.That(mapping.MapVariation(0x42, 0xFE0F), Is.EqualTo(3));
            Assert.That(mapping.MapVariation(0x43, 0xFE0F), Is.Zero);
            Assert.That(mapping.MapVariation(0x41, 0xFE0E), Is.Zero);
        });
    }

    [Test]
    public void KeepsLargeGroupedCoverageAsCompactRanges()
    {
        byte[] format12 = BuildGroupedFormat(
            12,
            new uint[] { 0x10000 },
            new uint[] { 0x1FFFD },
            new uint[] { 1 });
        byte[] format13 = BuildGroupedFormat(
            13,
            new uint[] { 0 },
            new uint[] { 0x10FFFF },
            new uint[] { 1 });
        SfntCmapMapping sequential = Parse(
            BuildCmap(
                new int[] { 0 },
                new int[] { 4 },
                new byte[][] { format12 }),
            65535)!.GetMapping();
        SfntCmapMapping constant = Parse(
            BuildCmap(
                new int[] { 0 },
                new int[] { 6 },
                new byte[][] { format13 }),
            2)!.GetMapping();

        Assert.Multiple(() =>
        {
            Assert.That(sequential.GetCoverageRangeCount(), Is.EqualTo(1));
            Assert.That(sequential.GetCoverageRange(0).GetStartUnicodeValue(),
                Is.EqualTo(0x10000));
            Assert.That(sequential.GetCoverageRange(0).GetEndUnicodeValue(),
                Is.EqualTo(0x1FFFD));
            Assert.That(sequential.MapUnicode(0x1FFFD), Is.EqualTo(65534));
            Assert.That(constant.GetCoverageRangeCount(), Is.EqualTo(2));
            Assert.That(constant.GetCoverageRange(0).GetStartUnicodeValue(),
                Is.Zero);
            Assert.That(constant.GetCoverageRange(0).GetEndUnicodeValue(),
                Is.EqualTo(0xD7FF));
            Assert.That(constant.GetCoverageRange(1).GetStartUnicodeValue(),
                Is.EqualTo(0xE000));
            Assert.That(constant.GetCoverageRange(1).GetEndUnicodeValue(),
                Is.EqualTo(0x10FFFF));
            Assert.That(constant.CoversUnicodeValue(0xD800), Is.False);
            Assert.That(constant.MapUnicode(0x10FFFF), Is.EqualTo(1));
        });
    }

    [Test]
    public void ReturnsMultipleCandidatesInWinningSourcePreferenceOrder()
    {
        byte[] windowsBmp = BuildFormat6(0x41, new int[] { 4, 4 });
        byte[] windowsFull = BuildFormat6(0x42, new int[] { 3, 4, 4 });
        byte[] cmap = BuildCmap(
            new int[] { 3, 3 },
            new int[] { 1, 10 },
            new byte[][] { windowsBmp, windowsFull });
        SfntTableCache cache = CreateCache(cmap, 5);
        SfntCmapMapping mapping = SfntCmapTableParser
            .FindTable(cache)!
            .GetMapping();
        FontFace face = CreateFace(cache);

        Assert.Multiple(() =>
        {
            Assert.That(mapping.GetUnicodeCandidates(4),
                Is.EqualTo(new int[] { 0x43, 0x44, 0x41 }));
            Assert.That(mapping.GetUnicodeCandidates(3),
                Is.EqualTo(new int[] { 0x42 }));
            Assert.That(mapping.GetUnicodeCandidates(0), Is.Empty);
            Assert.That(face.GetUnicodeCandidates(4),
                Is.EqualTo(new int[] { 0x43, 0x44, 0x41 }));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => mapping.GetUnicodeCandidates(-1));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => mapping.GetUnicodeCandidates(5));
        });
    }

    [Test]
    public void ReturnsSymbolAliasesAndNonBmpCandidates()
    {
        byte[] symbol = BuildGroupedFormat(
            12,
            new uint[] { 0xF041 },
            new uint[] { 0xF042 },
            new uint[] { 4 });
        byte[] supplementary = BuildGroupedFormat(
            13,
            new uint[] { 0x1F600 },
            new uint[] { 0x1F602 },
            new uint[] { 1 });
        SfntCmapMapping symbolMapping = Parse(
            BuildCmap(
                new int[] { 3 },
                new int[] { 0 },
                new byte[][] { symbol }),
            6)!.GetMapping();
        SfntCmapMapping supplementaryMapping = Parse(
            BuildCmap(
                new int[] { 0 },
                new int[] { 6 },
                new byte[][] { supplementary }),
            2)!.GetMapping();
        SfntCmapMapping packedMapping = Parse(
            BuildCmap(
                new int[] { 0 },
                new int[] { 4 },
                new byte[][] { BuildFormat8() }),
            5)!.GetMapping();

        Assert.Multiple(() =>
        {
            Assert.That(symbolMapping.GetUnicodeCandidates(4),
                Is.EqualTo(new int[] { 0x41, 0xF041 }));
            Assert.That(symbolMapping.GetUnicodeCandidates(5),
                Is.EqualTo(new int[] { 0x42, 0xF042 }));
            Assert.That(supplementaryMapping.GetUnicodeCandidates(1),
                Is.EqualTo(new int[] { 0x1F600, 0x1F601, 0x1F602 }));
            Assert.That(packedMapping.GetUnicodeCandidates(3),
                Is.EqualTo(new int[] { 0x10000 }));
            Assert.That(packedMapping.GetUnicodeCandidates(4),
                Is.EqualTo(new int[] { 0x10001 }));
        });
    }

    [Test]
    public void BoundsReverseCandidateExpansionByCoverageLimit()
    {
        byte[] supplementary = BuildGroupedFormat(
            13,
            new uint[] { 0x1F600 },
            new uint[] { 0x1F602 },
            new uint[] { 1 });
        SfntCmapMapping mapping = Parse(
            BuildCmap(
                new int[] { 0 },
                new int[] { 6 },
                new byte[][] { supplementary }),
            2,
            CreateLimits(2))!.GetMapping();

        Assert.Throws<FontLimitException>(
            () => mapping.GetUnicodeCandidates(1));
    }

    [Test]
    public void MapsWindowsAndMacintoshLegacyCmapsThroughUnicode()
    {
        byte[] chineseSubtable = BuildGroupedFormat(
            12,
            new uint[] { 0xD6D0 },
            new uint[] { 0xD6D0 },
            new uint[] { 2 });
        byte[] macintoshSubtable = BuildFormat0(0x80, 3);
        SfntCmapMapping chinese = Parse(
            BuildCmap(
                new int[] { 3 },
                new int[] { 3 },
                new byte[][] { chineseSubtable }),
            4)!.GetMapping();
        SfntCmapMapping macintosh = Parse(
            BuildCmap(
                new int[] { 1 },
                new int[] { 0 },
                new byte[][] { macintoshSubtable }),
            4)!.GetMapping();

        Assert.Multiple(() =>
        {
            Assert.That(chinese.GetSourceCount(), Is.EqualTo(1));
            Assert.That(chinese.GetSourceRank(0), Is.EqualTo(303));
            Assert.That(chinese.MapUnicode(0x4E2D), Is.EqualTo(2));
            Assert.That(chinese.CoversUnicodeValue(0x4E2D), Is.True);
            Assert.That(chinese.GetUnicodeCandidates(2),
                Is.EqualTo(new int[] { 0x4E2D }));
            Assert.That(macintosh.GetSourceRank(0), Is.EqualTo(400));
            Assert.That(macintosh.MapUnicode(0x00C4), Is.EqualTo(3));
            Assert.That(macintosh.GetUnicodeCandidates(3),
                Is.EqualTo(new int[] { 0x00C4 }));
        });
    }

    static byte[] BuildFormat6(int firstCode, int[] glyphIds)
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(6)
            .WriteUInt16((ushort)(10 + glyphIds.Length * 2))
            .WriteUInt16(0)
            .WriteUInt16((ushort)firstCode)
            .WriteUInt16((ushort)glyphIds.Length);

        for (int index = 0; index < glyphIds.Length; index += 1)
        {
            builder.WriteUInt16((ushort)glyphIds[index]);
        }

        return builder.CopyBytes();
    }

    static byte[] BuildFormat0(int characterCode, int glyphId)
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(0)
            .WriteUInt16(262)
            .WriteUInt16(0);

        for (int index = 0; index < 256; index += 1)
        {
            builder.WriteUInt8((byte)(index == characterCode ? glyphId : 0));
        }

        return builder.CopyBytes();
    }

    static byte[] BuildFormat8()
    {
        byte[] is32 = new byte[8192];
        SetIs32(is32, 0xD800);
        SetIs32(is32, 0xDC00);
        SetIs32(is32, 0xDC01);
        return new TestBinaryBuilder()
            .WriteUInt16(8)
            .WriteUInt16(0)
            .WriteUInt32(8232)
            .WriteUInt32(0)
            .WriteBytes(is32)
            .WriteUInt32(2)
            .WriteUInt32(0x41)
            .WriteUInt32(0x42)
            .WriteUInt32(1)
            .WriteUInt32(0xD800DC00U)
            .WriteUInt32(0xD800DC01U)
            .WriteUInt32(3)
            .CopyBytes();
    }

    static void SetIs32(byte[] bytes, int codeUnit)
    {
        int index = codeUnit / 8;
        int mask = 1 << (7 - codeUnit % 8);
        bytes[index] = (byte)(bytes[index] | mask);
    }

    static byte[] BuildGroupedFormat(
        int format,
        uint[] starts,
        uint[] ends,
        uint[] glyphIds)
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16((ushort)format)
            .WriteUInt16(0)
            .WriteUInt32((uint)(16 + starts.Length * 12))
            .WriteUInt32(0)
            .WriteUInt32((uint)starts.Length);

        for (int index = 0; index < starts.Length; index += 1)
        {
            builder
                .WriteUInt32(starts[index])
                .WriteUInt32(ends[index])
                .WriteUInt32(glyphIds[index]);
        }

        return builder.CopyBytes();
    }

    static byte[] BuildFormat14(
        int variationSelector,
        int defaultUnicodeValue,
        int nonDefaultUnicodeValue,
        int nonDefaultGlyphId)
    {
        return new TestBinaryBuilder()
            .WriteUInt16(14)
            .WriteUInt32(38)
            .WriteUInt32(1)
            .WriteUInt24((uint)variationSelector)
            .WriteUInt32(21)
            .WriteUInt32(29)
            .WriteUInt32(1)
            .WriteUInt24((uint)defaultUnicodeValue)
            .WriteUInt8(0)
            .WriteUInt32(1)
            .WriteUInt24((uint)nonDefaultUnicodeValue)
            .WriteUInt16((ushort)nonDefaultGlyphId)
            .CopyBytes();
    }

    static byte[] BuildCmap(
        int[] platformIds,
        int[] encodingIds,
        byte[][] subtables)
    {
        int offset = 4 + subtables.Length * 8;
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(0)
            .WriteUInt16((ushort)subtables.Length);

        for (int index = 0; index < subtables.Length; index += 1)
        {
            builder
                .WriteUInt16((ushort)platformIds[index])
                .WriteUInt16((ushort)encodingIds[index])
                .WriteUInt32((uint)offset);
            offset += subtables[index].Length;
        }

        for (int index = 0; index < subtables.Length; index += 1)
        {
            builder.WriteBytes(subtables[index]);
        }

        return builder.CopyBytes();
    }

    static SfntCmapTable? Parse(byte[] cmap, int glyphCount)
    {
        return SfntCmapTableParser.FindTable(CreateCache(cmap, glyphCount));
    }

    static SfntCmapTable? Parse(
        byte[] cmap,
        int glyphCount,
        FontLimits limits)
    {
        return SfntCmapTableParser.FindTable(
            CreateCache(cmap, glyphCount, limits));
    }

    static SfntTableCache CreateCache(byte[] cmap, int glyphCount)
    {
        return CreateCache(cmap, glyphCount, FontLimits.CreateCompatible());
    }

    static SfntTableCache CreateCache(
        byte[] cmap,
        int glyphCount,
        FontLimits limits)
    {
        byte[] maxp = new TestBinaryBuilder()
            .WriteUInt32(0x00005000U)
            .WriteUInt16((ushort)glyphCount)
            .CopyBytes();
        byte[][] bytes = new byte[][] { cmap, maxp };
        int[] tags = new int[] { FontTags.Cmap, FontTags.Maxp };
        SfntTableData[] tables = new SfntTableData[2];

        for (int index = 0; index < tables.Length; index += 1)
        {
            FontData data = FontData.Create(bytes[index]);
            tables[index] = new SfntTableData(
                tags[index],
                FontChecksum.CalculateTable(bytes[index]),
                data.GetSlice(0, data.GetLength()),
                SfntTableClassification.Known,
                true);
        }

        return SfntTableCache.Create(
            new SfntTableStore(tables, new FontDiagnostic[0]),
            FontLoadMode.Compatible,
            limits);
    }

    static FontFace CreateFace(SfntTableCache cache)
    {
        return new FontFace(
            0,
            0x00010000U,
            new SfntValidationResult(
                FontOutlineKind.TrueType,
                new FontDiagnostic[0]),
            cache);
    }

    static FontLimits CreateLimits(int maximumCoverageEntries)
    {
        return FontLimits.Create(
            1000000,
            1000000,
            10,
            100,
            65535,
            1000,
            1000,
            10,
            10,
            10,
            100,
            100000,
            maximumCoverageEntries,
            100000,
            1000000,
            100,
            1000,
            100,
            100);
    }
}
