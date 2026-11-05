namespace ttf.Tests;

internal class SfntCmapVariationTableTests
{
    [Test]
    public void ParsesDefaultAndNonDefaultVariationMappings()
    {
        byte[] formatBytes = BuildFormat14(
            new int[] { 0xFE0F, 0xE0100 },
            new int[][]
            {
                new int[] { 0x41, 0x1F600 },
                new int[0]
            },
            new int[][]
            {
                new int[] { 1, 0 },
                new int[0]
            },
            new int[][]
            {
                new int[] { 0x43, 0x1F601 },
                new int[] { 0x82A6 }
            },
            new int[][]
            {
                new int[] { 3, 4 },
                new int[] { 5 }
            });
        SfntCmapSubtable subtable = Parse(
            BuildCmap(formatBytes, 0, 5),
            6)!.GetEncodingRecord(0).GetSubtable();
        SfntCmapFormat14Data? format = subtable.GetFormat14();
        SfntCmapVariationSelectorRecord first = format!.GetSelectorRecord(0);
        SfntCmapVariationSelectorRecord second = format.GetSelectorRecord(1);

        Assert.Multiple(() =>
        {
            Assert.That(subtable.GetFormat(), Is.EqualTo(14));
            Assert.That(subtable.GetLength(), Is.EqualTo(formatBytes.Length));
            Assert.That(subtable.GetLanguage(), Is.Zero);
            Assert.That(subtable.MapCharacterCode(0x41), Is.Zero);
            Assert.That(format.GetSelectorRecordCount(), Is.EqualTo(2));
            Assert.That(first.GetVariationSelector(), Is.EqualTo(0xFE0F));
            Assert.That(first.GetDefaultRangeCount(), Is.EqualTo(2));
            Assert.That(first.GetDefaultRange(0).GetStartUnicodeValue(),
                Is.EqualTo(0x41));
            Assert.That(first.GetDefaultRange(0).GetEndUnicodeValue(),
                Is.EqualTo(0x42));
            Assert.That(first.GetNonDefaultMappingCount(), Is.EqualTo(2));
            Assert.That(first.GetNonDefaultMapping(1).GetGlyphId(), Is.EqualTo(4));
            Assert.That(second.GetVariationSelector(), Is.EqualTo(0xE0100));
            Assert.That(format.IsDefaultVariation(0x41, 0xFE0F), Is.True);
            Assert.That(format.IsDefaultVariation(0x42, 0xFE0F), Is.True);
            Assert.That(format.IsDefaultVariation(0x1F600, 0xFE0F), Is.True);
            Assert.That(format.IsDefaultVariation(0x43, 0xFE0F), Is.False);
            Assert.That(format.MapNonDefaultVariation(0x43, 0xFE0F),
                Is.EqualTo(3));
            Assert.That(format.MapNonDefaultVariation(0x1F601, 0xFE0F),
                Is.EqualTo(4));
            Assert.That(format.MapNonDefaultVariation(0x82A6, 0xE0100),
                Is.EqualTo(5));
            Assert.That(format.MapNonDefaultVariation(0x82A6, 0xE0101), Is.Zero);
            Assert.That(format.FindSelectorRecord(0xE0100), Is.SameAs(second));
            Assert.That(format.FindSelectorRecord(0xE0101), Is.Null);
            Assert.Throws<ArgumentOutOfRangeException>(
                () => format.GetSelectorRecord(2));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => first.GetDefaultRange(2));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => first.GetNonDefaultMapping(2));
        });
    }

    [Test]
    public void AcceptsAnEmptyVariationSubtable()
    {
        byte[] formatBytes = BuildSelectorOnlyFormat14(new int[0]);
        SfntCmapFormat14Data? format = Parse(
            BuildCmap(formatBytes, 0, 5),
            1)!.GetEncodingRecord(0).GetSubtable().GetFormat14();

        Assert.Multiple(() =>
        {
            Assert.That(format!.GetSelectorRecordCount(), Is.Zero);
            Assert.That(format.FindSelectorRecord(0xFE0F), Is.Null);
            Assert.That(format.IsDefaultVariation(0x41, 0xFE0F), Is.False);
            Assert.That(format.MapNonDefaultVariation(0x41, 0xFE0F), Is.Zero);
        });

        SfntCmapFormat14Data? mongolian = Parse(
            BuildCmap(BuildSelectorOnlyFormat14(new int[] { 0x180B }), 0, 5),
            1)!.GetEncodingRecord(0).GetSubtable().GetFormat14();
        Assert.That(mongolian!.GetSelectorRecord(0).GetVariationSelector(),
            Is.EqualTo(0x180B));
    }

    [Test]
    public void EnforcesTheFormatFourteenEncodingPair()
    {
        byte[] format14 = BuildSelectorOnlyFormat14(new int[0]);
        byte[] format12 = new TestBinaryBuilder()
            .WriteUInt16(12)
            .WriteUInt16(0)
            .WriteUInt32(16)
            .WriteUInt32(0)
            .WriteUInt32(0)
            .CopyBytes();

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(
                () => Parse(BuildCmap(format14, 0, 4), 1));
            Assert.Throws<FontFormatException>(
                () => Parse(BuildCmap(format14, 3, 10), 1));
            Assert.Throws<FontFormatException>(
                () => Parse(BuildCmap(format12, 0, 5), 1));
        });
    }

    [Test]
    public void RejectsInvalidFormatFourteenHeadersAndSelectors()
    {
        byte[] shortLength = BuildSelectorOnlyFormat14(new int[0]);
        SetUInt32(shortLength, 2, 9);
        byte[] longLength = BuildSelectorOnlyFormat14(new int[0]);
        SetUInt32(longLength, 2, uint.MaxValue);
        byte[] truncatedRecords = BuildSelectorOnlyFormat14(new int[0]);
        SetUInt32(truncatedRecords, 6, 1);
        byte[] duplicateSelectors = BuildSelectorOnlyFormat14(
            new int[] { 0xFE0F, 0xFE0F });
        byte[] unsortedSelectors = BuildSelectorOnlyFormat14(
            new int[] { 0xE0100, 0xFE0F });
        byte[] zeroSelector = BuildSelectorOnlyFormat14(new int[] { 0 });
        byte[] ordinaryCharacter = BuildSelectorOnlyFormat14(new int[] { 0x41 });
        byte[] largeSelector = BuildSelectorOnlyFormat14(new int[] { 0x110000 });
        byte[] surrogateSelector = BuildSelectorOnlyFormat14(new int[] { 0xD800 });

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(
                () => Parse(BuildCmap(shortLength, 0, 5), 1));
            Assert.Throws<FontFormatException>(
                () => Parse(BuildCmap(longLength, 0, 5), 1));
            Assert.Throws<FontFormatException>(
                () => Parse(BuildCmap(truncatedRecords, 0, 5), 1));
            Assert.Throws<FontFormatException>(
                () => Parse(BuildCmap(duplicateSelectors, 0, 5), 1));
            Assert.Throws<FontFormatException>(
                () => Parse(BuildCmap(unsortedSelectors, 0, 5), 1));
            Assert.Throws<FontFormatException>(
                () => Parse(BuildCmap(zeroSelector, 0, 5), 1));
            Assert.Throws<FontFormatException>(
                () => Parse(BuildCmap(ordinaryCharacter, 0, 5), 1));
            Assert.Throws<FontFormatException>(
                () => Parse(BuildCmap(largeSelector, 0, 5), 1));
            Assert.Throws<FontFormatException>(
                () => Parse(BuildCmap(surrogateSelector, 0, 5), 1));
        });
    }

    [Test]
    public void RejectsInvalidVariationTableOffsetsAndLengths()
    {
        byte[] insideRecords = BuildFormat14(
            new int[] { 0xFE0F },
            new int[][] { new int[] { 0x41 } },
            new int[][] { new int[] { 0 } },
            new int[][] { new int[0] },
            new int[][] { new int[0] });
        SetUInt32(insideRecords, 13, 10);
        byte[] pastEnd = (byte[])insideRecords.Clone();
        SetUInt32(pastEnd, 13, (uint)pastEnd.Length);
        byte[] truncatedChild = BuildFormat14(
            new int[] { 0xFE0F },
            new int[][] { new int[] { 0x41 } },
            new int[][] { new int[] { 0 } },
            new int[][] { new int[0] },
            new int[][] { new int[0] });
        SetUInt32(truncatedChild, 21, 2);

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(
                () => Parse(BuildCmap(insideRecords, 0, 5), 2));
            Assert.Throws<FontFormatException>(
                () => Parse(BuildCmap(pastEnd, 0, 5), 2));
            Assert.Throws<FontFormatException>(
                () => Parse(BuildCmap(truncatedChild, 0, 5), 2));
        });
    }

    [Test]
    public void RejectsMalformedDefaultVariationRanges()
    {
        byte[] overlap = BuildFormat14(
            new int[] { 0xFE0F },
            new int[][] { new int[] { 0x41, 0x42 } },
            new int[][] { new int[] { 1, 0 } },
            new int[][] { new int[0] },
            new int[][] { new int[0] });
        byte[] unsorted = BuildFormat14(
            new int[] { 0xFE0F },
            new int[][] { new int[] { 0x42, 0x41 } },
            new int[][] { new int[] { 0, 0 } },
            new int[][] { new int[0] },
            new int[][] { new int[0] });
        byte[] beyondUnicode = BuildFormat14(
            new int[] { 0xFE0F },
            new int[][] { new int[] { 0x10FFFF } },
            new int[][] { new int[] { 1 } },
            new int[][] { new int[0] },
            new int[][] { new int[0] });
        byte[] surrogate = BuildFormat14(
            new int[] { 0xFE0F },
            new int[][] { new int[] { 0xD800 } },
            new int[][] { new int[] { 0 } },
            new int[][] { new int[0] },
            new int[][] { new int[0] });

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(
                () => Parse(BuildCmap(overlap, 0, 5), 2));
            Assert.Throws<FontFormatException>(
                () => Parse(BuildCmap(unsorted, 0, 5), 2));
            Assert.Throws<FontFormatException>(
                () => Parse(BuildCmap(beyondUnicode, 0, 5), 2));
            Assert.Throws<FontFormatException>(
                () => Parse(BuildCmap(surrogate, 0, 5), 2));
        });
    }

    [Test]
    public void RejectsMalformedNonDefaultVariationMappings()
    {
        byte[] duplicate = BuildFormat14(
            new int[] { 0xFE0F },
            new int[][] { new int[0] },
            new int[][] { new int[0] },
            new int[][] { new int[] { 0x41, 0x41 } },
            new int[][] { new int[] { 1, 2 } });
        byte[] unsorted = BuildFormat14(
            new int[] { 0xFE0F },
            new int[][] { new int[0] },
            new int[][] { new int[0] },
            new int[][] { new int[] { 0x42, 0x41 } },
            new int[][] { new int[] { 1, 2 } });
        byte[] invalidUnicode = BuildFormat14(
            new int[] { 0xFE0F },
            new int[][] { new int[0] },
            new int[][] { new int[0] },
            new int[][] { new int[] { 0x110000 } },
            new int[][] { new int[] { 1 } });
        byte[] badGlyph = BuildFormat14(
            new int[] { 0xFE0F },
            new int[][] { new int[0] },
            new int[][] { new int[0] },
            new int[][] { new int[] { 0x41 } },
            new int[][] { new int[] { 3 } });

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(
                () => Parse(BuildCmap(duplicate, 0, 5), 3));
            Assert.Throws<FontFormatException>(
                () => Parse(BuildCmap(unsorted, 0, 5), 3));
            Assert.Throws<FontFormatException>(
                () => Parse(BuildCmap(invalidUnicode, 0, 5), 3));
            Assert.Throws<FontFormatException>(
                () => Parse(BuildCmap(badGlyph, 0, 5), 3));
        });
    }

    [Test]
    public void RejectsOverlappingPartitionsAndExcessiveCoverage()
    {
        byte[] overlap = BuildFormat14(
            new int[] { 0xFE0F },
            new int[][] { new int[] { 0x41 } },
            new int[][] { new int[] { 1 } },
            new int[][] { new int[] { 0x42 } },
            new int[][] { new int[] { 1 } });
        byte[] limited = BuildFormat14(
            new int[] { 0xFE0F },
            new int[][] { new int[] { 0x41 } },
            new int[][] { new int[] { 0 } },
            new int[][] { new int[0] },
            new int[][] { new int[0] });

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(
                () => Parse(BuildCmap(overlap, 0, 5), 2));
            Assert.Throws<FontLimitException>(() => Parse(
                BuildCmap(limited, 0, 5),
                2,
                CreateLimits(1)));
        });
    }

    static byte[] BuildSelectorOnlyFormat14(int[] variationSelectors)
    {
        int[][] empty = new int[variationSelectors.Length][];

        for (int index = 0; index < empty.Length; index += 1)
        {
            empty[index] = new int[0];
        }

        return BuildFormat14(
            variationSelectors,
            empty,
            empty,
            empty,
            empty);
    }

    static byte[] BuildFormat14(
        int[] variationSelectors,
        int[][] defaultStarts,
        int[][] defaultAdditionalCounts,
        int[][] nonDefaultUnicodeValues,
        int[][] nonDefaultGlyphIds)
    {
        int[] defaultOffsets = new int[variationSelectors.Length];
        int[] nonDefaultOffsets = new int[variationSelectors.Length];
        int length = 10 + variationSelectors.Length * 11;

        for (int index = 0; index < variationSelectors.Length; index += 1)
        {
            if (defaultStarts[index].Length != 0)
            {
                defaultOffsets[index] = length;
                length += 4 + defaultStarts[index].Length * 4;
            }

            if (nonDefaultUnicodeValues[index].Length != 0)
            {
                nonDefaultOffsets[index] = length;
                length += 4 + nonDefaultUnicodeValues[index].Length * 5;
            }
        }

        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(14)
            .WriteUInt32((uint)length)
            .WriteUInt32((uint)variationSelectors.Length);

        for (int index = 0; index < variationSelectors.Length; index += 1)
        {
            builder
                .WriteUInt24((uint)variationSelectors[index])
                .WriteUInt32((uint)defaultOffsets[index])
                .WriteUInt32((uint)nonDefaultOffsets[index]);
        }

        for (int selectorIndex = 0;
            selectorIndex < variationSelectors.Length;
            selectorIndex += 1)
        {
            if (defaultStarts[selectorIndex].Length != 0)
            {
                builder.WriteUInt32((uint)defaultStarts[selectorIndex].Length);

                for (int rangeIndex = 0;
                    rangeIndex < defaultStarts[selectorIndex].Length;
                    rangeIndex += 1)
                {
                    builder
                        .WriteUInt24((uint)defaultStarts[selectorIndex][rangeIndex])
                        .WriteUInt8((byte)defaultAdditionalCounts[selectorIndex][rangeIndex]);
                }
            }

            if (nonDefaultUnicodeValues[selectorIndex].Length != 0)
            {
                builder.WriteUInt32(
                    (uint)nonDefaultUnicodeValues[selectorIndex].Length);

                for (int mappingIndex = 0;
                    mappingIndex < nonDefaultUnicodeValues[selectorIndex].Length;
                    mappingIndex += 1)
                {
                    builder
                        .WriteUInt24(
                            (uint)nonDefaultUnicodeValues[selectorIndex][mappingIndex])
                        .WriteUInt16(
                            (ushort)nonDefaultGlyphIds[selectorIndex][mappingIndex]);
                }
            }
        }

        return builder.CopyBytes();
    }

    static byte[] BuildCmap(byte[] subtable, int platformId, int encodingId)
    {
        return new TestBinaryBuilder()
            .WriteUInt16(0)
            .WriteUInt16(1)
            .WriteUInt16((ushort)platformId)
            .WriteUInt16((ushort)encodingId)
            .WriteUInt32(12)
            .WriteBytes(subtable)
            .CopyBytes();
    }

    static SfntCmapTable? Parse(byte[] cmap, int glyphCount)
    {
        return Parse(cmap, glyphCount, FontLimits.CreateCompatible());
    }

    static SfntCmapTable? Parse(
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

        SfntTableCache cache = SfntTableCache.Create(
            new SfntTableStore(tables, new FontDiagnostic[0]),
            FontLoadMode.Compatible,
            limits);
        return SfntCmapTableParser.FindTable(cache);
    }

    static FontLimits CreateLimits(int maxCoverageEntries)
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
            maxCoverageEntries,
            100000,
            1000000,
            100,
            1000,
            100,
            100);
    }

    static void SetUInt32(byte[] bytes, int offset, uint value)
    {
        bytes[offset] = (byte)(value >> 24);
        bytes[offset + 1] = (byte)(value >> 16);
        bytes[offset + 2] = (byte)(value >> 8);
        bytes[offset + 3] = (byte)value;
    }
}
