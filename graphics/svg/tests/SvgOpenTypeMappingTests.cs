namespace svg.Tests;

public class SvgOpenTypeMappingTests
{
    [TestCase(0, 0x41)]
    [TestCase(2, 0x41)]
    [TestCase(4, 0x41)]
    [TestCase(6, 0x41)]
    [TestCase(8, 0x1F600)]
    [TestCase(10, 0x1F600)]
    [TestCase(12, 0x1F600)]
    [TestCase(13, 0x1F600)]
    public void MapsEveryParsedBaseFormatAndReturnsGlyphZeroWhenMissing(int format, int codePoint)
    {
        SvgFont font = CreateFont(new CmapSpec(0, EncodingFor(format), BuildBaseSubtable(format, codePoint, 2)));

        SvgOpenTypeGlyphMapping mapped = font.MapCodePoint(codePoint);
        SvgOpenTypeGlyphMapping missing = font.MapCodePoint(0x2603);
        SvgOpenTypeGlyphMapping invalid = font.MapCodePoint(0xD800);

        Assert.Multiple(() =>
        {
            Assert.That(mapped.Success, Is.True);
            Assert.That(mapped.Found, Is.True);
            Assert.That(mapped.Missing, Is.False);
            Assert.That(mapped.GlyphIndex, Is.EqualTo(2));
            Assert.That(mapped.CharacterMap.Format, Is.EqualTo(format));
            Assert.That(missing.Success, Is.True);
            Assert.That(missing.GlyphIndex, Is.Zero);
            Assert.That(missing.Missing, Is.True);
            Assert.That(invalid.Success, Is.False);
            Assert.That(invalid.Error, Is.EqualTo(SvgOpenTypeMappingError.InvalidArgument));
        });
    }

    [Test]
    public void SelectsOneFullRepertoireMapInsteadOfFallingThroughToABmpMap()
    {
        SvgFont font = CreateFont(
            new CmapSpec(0, 4, BuildFormat12(0x41, 1)),
            new CmapSpec(3, 1, BuildFormat4(0x42, 2)));

        SvgOpenTypeGlyphMapping mapped = font.MapCodePoint(0x41);
        SvgOpenTypeGlyphMapping absentFromSelectedMap = font.MapCodePoint(0x42);

        Assert.Multiple(() =>
        {
            Assert.That(mapped.GlyphIndex, Is.EqualTo(1));
            Assert.That(mapped.CharacterMap.Format, Is.EqualTo(12));
            Assert.That(absentFromSelectedMap.Success, Is.True);
            Assert.That(absentFromSelectedMap.GlyphIndex, Is.Zero);
            Assert.That(absentFromSelectedMap.CharacterMap.Format, Is.EqualTo(12));
        });
    }

    [Test]
    public void MapsDefaultAndNonDefaultVariationSequencesWithoutImplicitFallback()
    {
        const int baseCharacter = 0x82A6;
        SvgFont font = CreateFont(
            new CmapSpec(0, 4, BuildFormat12(baseCharacter, 2)),
            new CmapSpec(0, 5, BuildFormat14(baseCharacter, 0xE0100, 1, 0xE0101)));

        SvgOpenTypeGlyphMapping nonDefault = font.MapVariationSequence(baseCharacter, 0xE0100);
        SvgOpenTypeGlyphMapping defaultMapping = font.MapVariationSequence(baseCharacter, 0xE0101);
        SvgOpenTypeGlyphMapping unsupported = font.MapVariationSequence(0x41, 0xE0101);
        SvgOpenTypeGlyphMapping unavailableSelector = font.MapVariationSequence(baseCharacter, 0xFE0F);
        SvgOpenTypeGlyphMapping invalidSelector = font.MapVariationSequence(baseCharacter, 0x41);

        Assert.Multiple(() =>
        {
            Assert.That(nonDefault.GlyphIndex, Is.EqualTo(1));
            Assert.That(nonDefault.UsedNonDefaultVariation, Is.True);
            Assert.That(nonDefault.CharacterMap.Format, Is.EqualTo(14));
            Assert.That(defaultMapping.GlyphIndex, Is.EqualTo(2));
            Assert.That(defaultMapping.UsedDefaultVariation, Is.True);
            Assert.That(unsupported.Success, Is.True);
            Assert.That(unsupported.Missing, Is.True);
            Assert.That(unavailableSelector.Success, Is.True);
            Assert.That(unavailableSelector.Missing, Is.True);
            Assert.That(invalidSelector.Success, Is.False);
            Assert.That(invalidSelector.Error, Is.EqualTo(SvgOpenTypeMappingError.InvalidArgument));
        });
    }

    [Test]
    public void RejectsOutOfRangeGlyphIdsRetainedInCharacterMapData()
    {
        SvgFont font = CreateFont(new CmapSpec(0, 4, BuildFormat12(0x41, 99)));

        SvgOpenTypeGlyphMapping result = font.MapCodePoint(0x41);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(SvgOpenTypeMappingError.InvalidCharacterMap));
            Assert.That(result.GlyphIndex, Is.Zero);
        });
    }

    [Test]
    public void ScalesHorizontalAndOptionalVerticalMetricsFromUnitsPerEm()
    {
        SvgFont font = CreateFont(new CmapSpec(0, 4, BuildFormat12(0x41, 1)));
        font.VerticalHeader = new SvgOpenTypeVerticalHeaderTable();
        font.VerticalMetrics.Add(new SvgOpenTypeVerticalMetric(0, 1000, 50));
        font.VerticalMetrics.Add(new SvgOpenTypeVerticalMetric(1, 1100, -25));
        font.VerticalMetrics.Add(new SvgOpenTypeVerticalMetric(2, 1100, -10));

        SvgOpenTypeGlyphMetrics metrics = font.GetGlyphMetrics(1, 16);
        SvgFont horizontalOnly = CreateFont(new CmapSpec(0, 4, BuildFormat12(0x41, 1)));
        SvgOpenTypeGlyphMetrics horizontal = horizontalOnly.GetGlyphMetrics(2, 20);
        SvgOpenTypeGlyphMetrics invalidGlyph = font.GetGlyphMetrics(3, 16);
        SvgOpenTypeGlyphMetrics invalidSize = font.GetGlyphMetrics(1, double.PositiveInfinity);

        Assert.Multiple(() =>
        {
            Assert.That(metrics.Success, Is.True);
            Assert.That(metrics.Scale, Is.EqualTo(0.016));
            Assert.That(metrics.AdvanceWidth, Is.EqualTo(600));
            Assert.That(metrics.ScaledAdvanceWidth, Is.EqualTo(9.6));
            Assert.That(metrics.ScaledLeftSideBearing, Is.EqualTo(0.16));
            Assert.That(metrics.HasVerticalMetrics, Is.True);
            Assert.That(metrics.AdvanceHeight, Is.EqualTo(1100));
            Assert.That(metrics.ScaledAdvanceHeight, Is.EqualTo(17.6));
            Assert.That(metrics.ScaledTopSideBearing, Is.EqualTo(-0.4));
            Assert.That(horizontal.Success, Is.True);
            Assert.That(horizontal.HasVerticalMetrics, Is.False);
            Assert.That(horizontal.ScaledAdvanceWidth, Is.EqualTo(12));
            Assert.That(invalidGlyph.Error, Is.EqualTo(SvgOpenTypeMetricError.InvalidArgument));
            Assert.That(invalidSize.Error, Is.EqualTo(SvgOpenTypeMetricError.InvalidArgument));
        });
    }

    static SvgFont CreateFont(params CmapSpec[] maps)
    {
        int headerLength = 4 + maps.Length * 8;
        int length = headerLength;
        int index = 0;
        while (index < maps.Length)
        {
            maps[index].Offset = length;
            length = length + maps[index].Bytes.Length;
            index = index + 1;
        }
        byte[] cmap = new byte[length];
        WriteUInt16(cmap, 0, 0);
        WriteUInt16(cmap, 2, maps.Length);
        index = 0;
        while (index < maps.Length)
        {
            CmapSpec map = maps[index];
            int recordOffset = 4 + index * 8;
            WriteUInt16(cmap, recordOffset, map.Platform);
            WriteUInt16(cmap, recordOffset + 2, map.Encoding);
            WriteUInt32(cmap, recordOffset + 4, (uint)map.Offset);
            Array.Copy(map.Bytes, 0, cmap, map.Offset, map.Bytes.Length);
            index = index + 1;
        }

        SvgFont font = new SvgFont(cmap, false);
        font.Tables.Add(new SvgOpenTypeTable("cmap", 0, 0, cmap.Length));
        font.Head = new SvgOpenTypeHeadTable();
        font.Head.UnitsPerEm = 1000;
        font.MaximumProfile = new SvgOpenTypeMaximumProfileTable();
        font.MaximumProfile.GlyphCount = 3;
        font.HorizontalMetrics.Add(new SvgOpenTypeHorizontalMetric(0, 500, 0));
        font.HorizontalMetrics.Add(new SvgOpenTypeHorizontalMetric(1, 600, 10));
        font.HorizontalMetrics.Add(new SvgOpenTypeHorizontalMetric(2, 600, -20));
        index = 0;
        while (index < maps.Length)
        {
            CmapSpec map = maps[index];
            SvgOpenTypeCmapRecord record = new SvgOpenTypeCmapRecord();
            record.PlatformIdentifier = map.Platform;
            record.EncodingIdentifier = map.Encoding;
            record.Offset = map.Offset;
            record.Format = ReadUInt16(map.Bytes, 0);
            record.Length = map.Bytes.Length;
            font.CharacterMaps.Add(record);
            index = index + 1;
        }
        font.Success = true;
        return font;
    }

    static int EncodingFor(int format)
    {
        if (format == 13) return 6;
        if (format == 8 || format == 10 || format == 12) return 4;
        return 3;
    }

    static byte[] BuildBaseSubtable(int format, int codePoint, int glyph)
    {
        if (format == 0) return BuildFormat0(codePoint, glyph);
        if (format == 2) return BuildFormat2(codePoint, glyph);
        if (format == 4) return BuildFormat4(codePoint, glyph);
        if (format == 6) return BuildFormat6(codePoint, glyph);
        if (format == 8) return BuildFormat8(codePoint, glyph);
        if (format == 10) return BuildFormat10(codePoint, glyph);
        if (format == 12) return BuildFormat12(codePoint, glyph);
        return BuildFormat13(codePoint, glyph);
    }

    static byte[] BuildFormat0(int codePoint, int glyph)
    {
        byte[] bytes = new byte[262];
        WriteUInt16(bytes, 0, 0);
        WriteUInt16(bytes, 2, bytes.Length);
        bytes[6 + codePoint] = (byte)glyph;
        return bytes;
    }

    static byte[] BuildFormat2(int codePoint, int glyph)
    {
        byte[] bytes = new byte[528];
        WriteUInt16(bytes, 0, 2);
        WriteUInt16(bytes, 2, bytes.Length);
        WriteUInt16(bytes, 518, codePoint);
        WriteUInt16(bytes, 520, 1);
        WriteInt16(bytes, 522, 0);
        WriteUInt16(bytes, 524, 2);
        WriteUInt16(bytes, 526, glyph);
        return bytes;
    }

    static byte[] BuildFormat4(int codePoint, int glyph)
    {
        byte[] bytes = new byte[32];
        WriteUInt16(bytes, 0, 4);
        WriteUInt16(bytes, 2, bytes.Length);
        WriteUInt16(bytes, 6, 4);
        WriteUInt16(bytes, 8, 4);
        WriteUInt16(bytes, 10, 1);
        WriteUInt16(bytes, 14, codePoint);
        WriteUInt16(bytes, 16, 0xFFFF);
        WriteUInt16(bytes, 18, 0);
        WriteUInt16(bytes, 20, codePoint);
        WriteUInt16(bytes, 22, 0xFFFF);
        WriteInt16(bytes, 24, glyph - codePoint);
        WriteInt16(bytes, 26, 1);
        return bytes;
    }

    static byte[] BuildFormat6(int codePoint, int glyph)
    {
        byte[] bytes = new byte[12];
        WriteUInt16(bytes, 0, 6);
        WriteUInt16(bytes, 2, bytes.Length);
        WriteUInt16(bytes, 6, codePoint);
        WriteUInt16(bytes, 8, 1);
        WriteUInt16(bytes, 10, glyph);
        return bytes;
    }

    static byte[] BuildFormat8(int codePoint, int glyph)
    {
        byte[] bytes = new byte[8220];
        WriteUInt16(bytes, 0, 8);
        WriteUInt32(bytes, 4, (uint)bytes.Length);
        int value = codePoint - 0x10000;
        int high = 0xD800 + (value >> 10);
        int low = 0xDC00 + (value & 0x3FF);
        bytes[12 + high / 8] = (byte)(1 << (7 - high % 8));
        WriteUInt32(bytes, 8204, 1);
        uint encoded = (uint)(high << 16 | low);
        WriteUInt32(bytes, 8208, encoded);
        WriteUInt32(bytes, 8212, encoded);
        WriteUInt32(bytes, 8216, (uint)glyph);
        return bytes;
    }

    static byte[] BuildFormat10(int codePoint, int glyph)
    {
        byte[] bytes = new byte[22];
        WriteUInt16(bytes, 0, 10);
        WriteUInt32(bytes, 4, (uint)bytes.Length);
        WriteUInt32(bytes, 12, (uint)codePoint);
        WriteUInt32(bytes, 16, 1);
        WriteUInt16(bytes, 20, glyph);
        return bytes;
    }

    static byte[] BuildFormat12(int codePoint, int glyph)
    {
        byte[] bytes = new byte[28];
        WriteUInt16(bytes, 0, 12);
        WriteUInt32(bytes, 4, (uint)bytes.Length);
        WriteUInt32(bytes, 12, 1);
        WriteUInt32(bytes, 16, (uint)codePoint);
        WriteUInt32(bytes, 20, (uint)codePoint);
        WriteUInt32(bytes, 24, (uint)glyph);
        return bytes;
    }

    static byte[] BuildFormat13(int codePoint, int glyph)
    {
        byte[] bytes = BuildFormat12(codePoint, glyph);
        WriteUInt16(bytes, 0, 13);
        return bytes;
    }

    static byte[] BuildFormat14(int codePoint, int nonDefaultSelector, int alternateGlyph, int defaultSelector)
    {
        byte[] bytes = new byte[49];
        WriteUInt16(bytes, 0, 14);
        WriteUInt32(bytes, 2, (uint)bytes.Length);
        WriteUInt32(bytes, 6, 2);
        WriteUInt24(bytes, 10, nonDefaultSelector);
        WriteUInt32(bytes, 13, 0);
        WriteUInt32(bytes, 17, 32);
        WriteUInt24(bytes, 21, defaultSelector);
        WriteUInt32(bytes, 24, 41);
        WriteUInt32(bytes, 28, 0);
        WriteUInt32(bytes, 32, 1);
        WriteUInt24(bytes, 36, codePoint);
        WriteUInt16(bytes, 39, alternateGlyph);
        WriteUInt32(bytes, 41, 1);
        WriteUInt24(bytes, 45, codePoint);
        bytes[48] = 0;
        return bytes;
    }

    static int ReadUInt16(byte[] bytes, int offset)
    {
        return bytes[offset] << 8 | bytes[offset + 1];
    }

    static void WriteUInt16(byte[] bytes, int offset, int value)
    {
        bytes[offset] = (byte)(value >> 8);
        bytes[offset + 1] = (byte)value;
    }

    static void WriteInt16(byte[] bytes, int offset, int value)
    {
        WriteUInt16(bytes, offset, value & 0xFFFF);
    }

    static void WriteUInt24(byte[] bytes, int offset, int value)
    {
        bytes[offset] = (byte)(value >> 16);
        bytes[offset + 1] = (byte)(value >> 8);
        bytes[offset + 2] = (byte)value;
    }

    static void WriteUInt32(byte[] bytes, int offset, uint value)
    {
        bytes[offset] = (byte)(value >> 24);
        bytes[offset + 1] = (byte)(value >> 16);
        bytes[offset + 2] = (byte)(value >> 8);
        bytes[offset + 3] = (byte)value;
    }

    class CmapSpec
    {
        public int Platform;
        public int Encoding;
        public byte[] Bytes;
        public int Offset;

        public CmapSpec(int platform, int encoding, byte[] bytes)
        {
            Platform = platform;
            Encoding = encoding;
            Bytes = bytes;
            Offset = 0;
        }
    }
}
