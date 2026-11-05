using ttf;

namespace ttf.Tests;

internal class FontCharacterMapPublicTests
{
    [Test]
    public void MapsEverySupportedNominalCmapFormatThroughPublicApi()
    {
        AssertPublicMapping(BuildFormat0(0x41, 2), 0, 0x41, 2, 3);
        AssertPublicMapping(BuildFormat2(), 2, 0x3000, 6, 8, 1, 1);
        AssertPublicMapping(BuildFormat4(), 4, 0x43, 3, 6);
        AssertPublicMapping(BuildFormat6(0x41, new int[] { 2 }), 6, 0x41, 2, 3);
        AssertPublicMapping(BuildFormat8(), 8, 0x10001, 4, 5, 0, 4);
        AssertPublicMapping(BuildFormat10(0x1F600, new int[] { 2, 3 }),
            10, 0x1F601, 3, 4, 0, 4);
        AssertPublicMapping(BuildGroupedFormat(12, 0x1F600, 0x1F601, 2),
            12, 0x1F601, 3, 4, 0, 4);
        AssertPublicMapping(BuildGroupedFormat(13, 0x1F600, 0x1F601, 2),
            13, 0x1F601, 2, 3, 0, 6);
    }

    [Test]
    public void MapsRankedUnicodeSourcesAndExposesCompactCoverageProvenance()
    {
        byte[] unicodeFull = BuildFormat6(0x41, new int[] { 2, 3, 0 });
        byte[] windowsBmp = BuildFormat6(0x41, new int[] { 1, 0, 4 });
        byte[] windowsFull = BuildFormat6(0x41, new int[] { 5, 0, 0 });
        byte[] variations = BuildFormat14(0xFE0F, 0x42, 0x43, 6);
        FontCharacterMap map = LoadMap(
            BuildCmap(
                new int[] { 0, 0, 3, 3 },
                new int[] { 4, 5, 1, 10 },
                new byte[][]
                {
                    unicodeFull,
                    variations,
                    windowsBmp,
                    windowsFull
                }),
            7);
        FontCharacterMapping direct = map.MapUnicode(0x41);
        FontCharacterMapping fallback = map.MapUnicode(0x42);
        FontCharacterMapping finalSource = map.MapUnicode(0x43);
        FontCharacterMapping missing = map.MapUnicode(0x44);
        FontCharacterMappingSource variationSource = map.GetVariationSource()!;

        Assert.Multiple(() =>
        {
            Assert.That(map.GetNominalSourceCount(), Is.EqualTo(3));
            Assert.That(map.GetSourceCount(), Is.EqualTo(4));
            Assert.That(map.IsSymbolMapping(), Is.False);
            Assert.That(map.GetSource(0).GetPlatformId(), Is.EqualTo(3));
            Assert.That(map.GetSource(0).GetEncodingId(), Is.EqualTo(10));
            Assert.That(map.GetSource(0).GetEncodingRecordIndex(), Is.EqualTo(3));
            Assert.That(map.GetSource(0).GetPriorityRank(), Is.EqualTo(100));
            Assert.That(map.GetSource(0).GetFormat(), Is.EqualTo(6));
            Assert.That(map.GetSource(0).GetLanguage(), Is.Zero);
            Assert.That(map.GetSource(0).GetSubtableOffset(), Is.GreaterThan(0));
            Assert.That(map.GetSource(1).GetEncodingId(), Is.EqualTo(4));
            Assert.That(map.GetSource(1).GetPriorityRank(), Is.EqualTo(120));
            Assert.That(map.GetSource(2).GetEncodingId(), Is.EqualTo(1));
            Assert.That(map.GetSource(2).GetPriorityRank(), Is.EqualTo(200));
            Assert.That(variationSource.GetSourceIndex(), Is.EqualTo(3));
            Assert.That(variationSource.GetEncodingRecordIndex(), Is.EqualTo(1));
            Assert.That(variationSource.GetPlatformId(), Is.Zero);
            Assert.That(variationSource.GetEncodingId(), Is.EqualTo(5));
            Assert.That(variationSource.GetFormat(), Is.EqualTo(14));
            Assert.That(variationSource.GetPriorityRank(), Is.EqualTo(-1));
            Assert.That(variationSource.IsVariationSelectorSource(), Is.True);
            Assert.That(direct.IsMapped(), Is.True);
            Assert.That(direct.GetGlyphId(), Is.EqualTo(5));
            Assert.That(direct.GetKind(), Is.EqualTo(FontCharacterMappingKind.Direct));
            Assert.That(direct.GetSource(), Is.SameAs(map.GetSource(0)));
            Assert.That(direct.HasVariationSelector(), Is.False);
            Assert.That(direct.GetVariationSource(), Is.Null);
            Assert.That(fallback.GetGlyphId(), Is.EqualTo(3));
            Assert.That(fallback.GetSource(), Is.SameAs(map.GetSource(1)));
            Assert.That(finalSource.GetGlyphId(), Is.EqualTo(4));
            Assert.That(finalSource.GetSource(), Is.SameAs(map.GetSource(2)));
            Assert.That(missing.GetKind(), Is.EqualTo(FontCharacterMappingKind.Missing));
            Assert.That(missing.IsMapped(), Is.False);
            Assert.That(missing.GetGlyphId(), Is.Zero);
            Assert.That(missing.GetSource(), Is.Null);
            Assert.That(map.GetCoverage().GetRangeCount(), Is.EqualTo(1));
            Assert.That(map.GetCoverage().GetRange(0).GetStartUnicodeValue(),
                Is.EqualTo(0x41));
            Assert.That(map.GetCoverage().GetRange(0).GetEndUnicodeValue(),
                Is.EqualTo(0x43));
            Assert.Throws<InvalidOperationException>(() => direct.GetVariationSelector());
            Assert.Throws<ArgumentOutOfRangeException>(() => map.GetSource(4));
            Assert.Throws<ArgumentOutOfRangeException>(() => map.MapUnicode(0xD800));
            Assert.Throws<ArgumentOutOfRangeException>(() => map.MapUnicode(0x110000));
        });
    }

    [Test]
    public void DistinguishesDefaultNonDefaultAndMissingVariationMappings()
    {
        byte[] nominal = BuildFormat6(0x41, new int[] { 1, 2, 3 });
        byte[] variations = BuildFormat14(0xFE0F, 0x41, 0x42, 4);
        FontCharacterMap map = LoadMap(
            BuildCmap(
                new int[] { 0, 0 },
                new int[] { 4, 5 },
                new byte[][] { nominal, variations }),
            5);
        FontCharacterMapping defaultMapping = map.MapVariation(0x41, 0xFE0F);
        FontCharacterMapping nonDefault = map.MapVariation(0x42, 0xFE0F);
        FontCharacterMapping missingValue = map.MapVariation(0x43, 0xFE0F);
        FontCharacterMapping missingSelector = map.MapVariation(0x41, 0xFE0E);

        Assert.Multiple(() =>
        {
            Assert.That(defaultMapping.GetKind(),
                Is.EqualTo(FontCharacterMappingKind.DefaultVariation));
            Assert.That(defaultMapping.GetGlyphId(), Is.EqualTo(1));
            Assert.That(defaultMapping.GetUnicodeValue(), Is.EqualTo(0x41));
            Assert.That(defaultMapping.HasVariationSelector(), Is.True);
            Assert.That(defaultMapping.GetVariationSelector(), Is.EqualTo(0xFE0F));
            Assert.That(defaultMapping.GetSource(), Is.SameAs(map.GetSource(0)));
            Assert.That(defaultMapping.GetVariationSource(),
                Is.SameAs(map.GetVariationSource()));
            Assert.That(nonDefault.GetKind(),
                Is.EqualTo(FontCharacterMappingKind.NonDefaultVariation));
            Assert.That(nonDefault.GetGlyphId(), Is.EqualTo(4));
            Assert.That(nonDefault.GetSource(), Is.SameAs(map.GetVariationSource()));
            Assert.That(nonDefault.GetVariationSource(),
                Is.SameAs(map.GetVariationSource()));
            Assert.That(missingValue.GetKind(),
                Is.EqualTo(FontCharacterMappingKind.Missing));
            Assert.That(missingValue.GetVariationSource(),
                Is.SameAs(map.GetVariationSource()));
            Assert.That(missingSelector.IsMapped(), Is.False);
            Assert.That(missingSelector.GetVariationSource(),
                Is.SameAs(map.GetVariationSource()));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                map.MapVariation(0x41, 0x41));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                map.MapVariation(0xD800, 0xFE0F));
        });
    }

    [Test]
    public void ReportsSymbolRemappingAndBoundedReverseCandidates()
    {
        byte[] symbol = BuildGroupedFormat12(
            new uint[] { 0x41, 0xF041 },
            new uint[] { 0x41, 0xF042 },
            new uint[] { 3, 4 });
        FontCharacterMap map = LoadMap(
            BuildCmap(
                new int[] { 3 },
                new int[] { 0 },
                new byte[][] { symbol }),
            6);
        FontCharacterMapping direct = map.MapUnicode(0x41);
        FontCharacterMapping remapped = map.MapUnicode(0x42);
        FontCharacterMapping privateUse = map.MapUnicode(0xF042);
        FontCharacterMapping[] candidates = map.GetUnicodeCandidates(5);
        FontCharacterMapping[] secondCopy = map.GetUnicodeCandidates(5);

        Assert.Multiple(() =>
        {
            Assert.That(map.IsSymbolMapping(), Is.True);
            Assert.That(map.GetNominalSourceCount(), Is.EqualTo(1));
            Assert.That(map.GetVariationSource(), Is.Null);
            Assert.That(map.GetSource(0).GetPriorityRank(), Is.Zero);
            Assert.That(direct.GetKind(), Is.EqualTo(FontCharacterMappingKind.Direct));
            Assert.That(direct.GetGlyphId(), Is.EqualTo(3));
            Assert.That(remapped.GetKind(),
                Is.EqualTo(FontCharacterMappingKind.SymbolRemapped));
            Assert.That(remapped.GetGlyphId(), Is.EqualTo(5));
            Assert.That(privateUse.GetKind(),
                Is.EqualTo(FontCharacterMappingKind.Direct));
            Assert.That(privateUse.GetGlyphId(), Is.EqualTo(5));
            Assert.That(candidates.Length, Is.EqualTo(2));
            Assert.That(candidates[0].GetUnicodeValue(), Is.EqualTo(0x42));
            Assert.That(candidates[0].GetKind(),
                Is.EqualTo(FontCharacterMappingKind.SymbolRemapped));
            Assert.That(candidates[1].GetUnicodeValue(), Is.EqualTo(0xF042));
            Assert.That(candidates[1].GetKind(),
                Is.EqualTo(FontCharacterMappingKind.Direct));
            Assert.That(candidates, Is.Not.SameAs(secondCopy));
            Assert.That(map.GetUnicodeCandidates(0), Is.Empty);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                map.GetUnicodeCandidates(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                map.GetUnicodeCandidates(6));
        });

        candidates[0] = direct;
        Assert.That(map.GetUnicodeCandidates(5)[0].GetUnicodeValue(),
            Is.EqualTo(0x42));
    }

    static FontCharacterMap LoadMap(byte[] cmap, int glyphCount)
    {
        byte[] font = new TinySfntBuilder()
            .AddTable("cmap", cmap)
            .AddTable("glyf", new byte[4])
            .AddTable("head", new byte[12])
            .AddTable("hhea", new byte[4])
            .AddTable("hmtx", new byte[4])
            .AddTable("loca", new byte[4])
            .AddTable("maxp", BuildMaxp(glyphCount))
            .AddTable("name", new byte[6])
            .AddTable("OS/2", new byte[4])
            .AddTable("post", new byte[4])
            .Build();
        FontFace face = new FontLoader()
            .LoadFromBytes(font, FontLoadOptions.CreateCompatible())
            .GetFace(0);
        FontCharacterMap first = face.GetCharacterMap();
        FontCharacterMap second = face.GetCharacterMap();
        Assert.That(first, Is.SameAs(second));
        return first;
    }

    static byte[] BuildMaxp(int glyphCount)
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt32(0x00010000U)
            .WriteUInt16((ushort)glyphCount);
        int index = 0;

        while (index < 13)
        {
            builder.WriteUInt16((ushort)(index == 4 ? 1 : 0));
            index += 1;
        }

        return builder.CopyBytes();
    }

    static byte[] BuildFormat6(int firstCode, int[] glyphIds)
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(6)
            .WriteUInt16((ushort)(10 + glyphIds.Length * 2))
            .WriteUInt16(0)
            .WriteUInt16((ushort)firstCode)
            .WriteUInt16((ushort)glyphIds.Length);
        int index = 0;

        while (index < glyphIds.Length)
        {
            builder.WriteUInt16((ushort)glyphIds[index]);
            index += 1;
        }

        return builder.CopyBytes();
    }

    static void AssertPublicMapping(
        byte[] subtable,
        int format,
        int unicodeValue,
        int glyphId,
        int glyphCount)
    {
        AssertPublicMapping(
            subtable,
            format,
            unicodeValue,
            glyphId,
            glyphCount,
            3,
            format >= 8 ? 10 : 1);
    }

    static void AssertPublicMapping(
        byte[] subtable,
        int format,
        int unicodeValue,
        int glyphId,
        int glyphCount,
        int platformId,
        int encodingId)
    {
        FontCharacterMap map = LoadMap(
            BuildCmap(
                new int[] { platformId },
                new int[] { encodingId },
                new byte[][] { subtable }),
            glyphCount);
        FontCharacterMapping mapping = map.MapUnicode(unicodeValue);
        FontCharacterMapping[] candidates = map.GetUnicodeCandidates(glyphId);

        Assert.Multiple(() =>
        {
            Assert.That(map.GetSourceCount(), Is.EqualTo(1));
            Assert.That(map.GetSource(0).GetFormat(), Is.EqualTo(format));
            Assert.That(mapping.IsMapped(), Is.True);
            Assert.That(mapping.GetGlyphId(), Is.EqualTo(glyphId));
            Assert.That(mapping.GetSource(), Is.SameAs(map.GetSource(0)));
            Assert.That(map.GetCoverage().Covers(unicodeValue), Is.True);
            Assert.That(candidates, Has.Some.Matches<FontCharacterMapping>(
                candidate => candidate.GetUnicodeValue() == unicodeValue));
        });
    }

    static byte[] BuildFormat0(int characterCode, int glyphId)
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(0)
            .WriteUInt16(262)
            .WriteUInt16(0);
        int index = 0;

        while (index < 256)
        {
            builder.WriteUInt8((byte)(index == characterCode ? glyphId : 0));
            index += 1;
        }

        return builder.CopyBytes();
    }

    static byte[] BuildFormat2()
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(2)
            .WriteUInt16(0)
            .WriteUInt16(0);
        int index = 0;

        while (index < 256)
        {
            builder.WriteUInt16((ushort)(index == 0x81 ? 8 : 0));
            index += 1;
        }

        builder
            .WriteUInt16(0x20)
            .WriteUInt16(2)
            .WriteInt16(0)
            .WriteUInt16(10)
            .WriteUInt16(0x40)
            .WriteUInt16(2)
            .WriteInt16(1)
            .WriteUInt16(6)
            .WriteUInt16(3)
            .WriteUInt16(0)
            .WriteUInt16(5)
            .WriteUInt16(6)
            .PatchUInt16(2, (ushort)builder.GetLength());
        return builder.CopyBytes();
    }

    static byte[] BuildFormat4()
    {
        return new TestBinaryBuilder()
            .WriteUInt16(4)
            .WriteUInt16(44)
            .WriteUInt16(0)
            .WriteUInt16(6)
            .WriteUInt16(4)
            .WriteUInt16(1)
            .WriteUInt16(2)
            .WriteUInt16(0x43)
            .WriteUInt16(0x51)
            .WriteUInt16(0xFFFF)
            .WriteUInt16(0)
            .WriteUInt16(0x41)
            .WriteUInt16(0x50)
            .WriteUInt16(0xFFFF)
            .WriteInt16(-64)
            .WriteInt16(1)
            .WriteInt16(1)
            .WriteUInt16(0)
            .WriteUInt16(4)
            .WriteUInt16(0)
            .WriteUInt16(4)
            .WriteUInt16(0)
            .CopyBytes();
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

    static byte[] BuildFormat10(int startCode, int[] glyphIds)
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(10)
            .WriteUInt16(0)
            .WriteUInt32((uint)(20 + glyphIds.Length * 2))
            .WriteUInt32(0)
            .WriteUInt32((uint)startCode)
            .WriteUInt32((uint)glyphIds.Length);
        int index = 0;

        while (index < glyphIds.Length)
        {
            builder.WriteUInt16((ushort)glyphIds[index]);
            index += 1;
        }

        return builder.CopyBytes();
    }

    static byte[] BuildGroupedFormat(
        int format,
        int start,
        int end,
        int glyphId)
    {
        return new TestBinaryBuilder()
            .WriteUInt16((ushort)format)
            .WriteUInt16(0)
            .WriteUInt32(28)
            .WriteUInt32(0)
            .WriteUInt32(1)
            .WriteUInt32((uint)start)
            .WriteUInt32((uint)end)
            .WriteUInt32((uint)glyphId)
            .CopyBytes();
    }

    static byte[] BuildGroupedFormat12(
        uint[] starts,
        uint[] ends,
        uint[] glyphIds)
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteUInt16(12)
            .WriteUInt16(0)
            .WriteUInt32((uint)(16 + starts.Length * 12))
            .WriteUInt32(0)
            .WriteUInt32((uint)starts.Length);
        int index = 0;

        while (index < starts.Length)
        {
            builder
                .WriteUInt32(starts[index])
                .WriteUInt32(ends[index])
                .WriteUInt32(glyphIds[index]);
            index += 1;
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
        int index = 0;

        while (index < subtables.Length)
        {
            builder
                .WriteUInt16((ushort)platformIds[index])
                .WriteUInt16((ushort)encodingIds[index])
                .WriteUInt32((uint)offset);
            offset += subtables[index].Length;
            index += 1;
        }

        index = 0;
        while (index < subtables.Length)
        {
            builder.WriteBytes(subtables[index]);
            index += 1;
        }

        return builder.CopyBytes();
    }
}
