namespace svg;

/// <summary>Maps Unicode scalars and scales parsed OpenType glyph metrics.</summary>
public class SvgOpenTypeMapping : ISvgOpenTypeMapping
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgOpenTypeMapping()
    {
    }

    /// <summary>Maps one Unicode scalar through a deterministically selected Unicode character map.</summary>
    /// <param name="font">A successfully parsed OpenType font.</param>
    /// <param name="codePoint">The Unicode scalar value to map.</param>
    /// <returns>The mapped glyph or a deterministic missing-glyph result.</returns>
    public SvgOpenTypeGlyphMapping MapCodePoint(SvgFont font, int codePoint)
    {
        SvgOpenTypeGlyphMapping result = new SvgOpenTypeGlyphMapping(codePoint, 0);
        if (!new SvgOpenTypeMapping().ValidFont(font) || !new SvgOpenTypeMapping().ValidScalar(codePoint))
        {
            result.Error = SvgOpenTypeMappingError.InvalidArgument;
            return result;
        }
        SvgOpenTypeCmapRecord? map = new SvgOpenTypeMapping().BestBaseMap(font);
        result.CharacterMap = map;
        if (map == null)
        {
            result.Success = true;
            return result;
        }
        SvgOpenTypeMapProbe probe = new SvgOpenTypeMapping().MapRecord(font, map, codePoint);
        if (!probe.Valid || probe.GlyphIndex < 0 || probe.GlyphIndex >= (font.MaximumProfile ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).GlyphCount)
        {
            result.Error = SvgOpenTypeMappingError.InvalidCharacterMap;
            return result;
        }
        new SvgOpenTypeMapping().Complete(result, probe.GlyphIndex);
        return result;
    }

    /// <summary>Maps a Unicode variation sequence using a format-14 character map.</summary>
    /// <param name="font">A successfully parsed OpenType font.</param>
    /// <param name="codePoint">The base Unicode scalar value.</param>
    /// <param name="variationSelector">A Unicode variation-selector scalar value.</param>
    /// <returns>The mapped glyph or a deterministic missing-glyph result.</returns>
    public SvgOpenTypeGlyphMapping MapVariationSequence(SvgFont font, int codePoint, int variationSelector)
    {
        SvgOpenTypeGlyphMapping result = new SvgOpenTypeGlyphMapping(codePoint, variationSelector);
        if (!new SvgOpenTypeMapping().ValidFont(font) || !new SvgOpenTypeMapping().ValidScalar(codePoint) || !new SvgOpenTypeMapping().ValidVariationSelector(variationSelector))
        {
            result.Error = SvgOpenTypeMappingError.InvalidArgument;
            return result;
        }
        SvgOpenTypeCmapRecord? variationMap = new SvgOpenTypeMapping().FindVariationMap(font);
        result.CharacterMap = variationMap;
        if (variationMap == null)
        {
            result.Success = true;
            return result;
        }
        SvgOpenTypeCmapBounds? bounds = new SvgOpenTypeMapping().Bounds(font, variationMap);
        if (bounds == null || bounds.Length < 10)
        {
            result.Error = SvgOpenTypeMappingError.InvalidCharacterMap;
            return result;
        }
        uint selectorCount = new SvgOpenTypeMapping().ReadUInt32(font.Bytes, bounds.Start + 6);
        if (10L + 11L * selectorCount > bounds.Length)
        {
            result.Error = SvgOpenTypeMappingError.InvalidCharacterMap;
            return result;
        }
        int selectorRecord = -1;
        int index = 0;
        int preceding = -1;
        while ((uint)index < selectorCount)
        {
            int offset = bounds.Start + 10 + index * 11;
            int selector = new SvgOpenTypeMapping().ReadUInt24(font.Bytes, offset);
            if (selector <= preceding)
            {
                result.Error = SvgOpenTypeMappingError.InvalidCharacterMap;
                return result;
            }
            if (selector == variationSelector) selectorRecord = offset;
            preceding = selector;
            index = index + 1;
        }
        if (selectorRecord < 0)
        {
            result.Success = true;
            return result;
        }
        uint defaultOffset = new SvgOpenTypeMapping().ReadUInt32(font.Bytes, selectorRecord + 3);
        uint nonDefaultOffset = new SvgOpenTypeMapping().ReadUInt32(font.Bytes, selectorRecord + 7);
        SvgOpenTypeMapProbe alternate = new SvgOpenTypeMapping().MapNonDefaultVariation(font.Bytes, bounds, nonDefaultOffset, codePoint);
        if (!alternate.Valid)
        {
            result.Error = SvgOpenTypeMappingError.InvalidCharacterMap;
            return result;
        }
        if (alternate.GlyphIndex >= 0)
        {
            if (alternate.GlyphIndex >= (font.MaximumProfile ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).GlyphCount)
            {
                result.Error = SvgOpenTypeMappingError.InvalidCharacterMap;
                return result;
            }
            result.UsedNonDefaultVariation = true;
            new SvgOpenTypeMapping().Complete(result, alternate.GlyphIndex);
            return result;
        }
        SvgOpenTypeMapProbe defaultMatch = new SvgOpenTypeMapping().ContainsDefaultVariation((font.Bytes ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), bounds, defaultOffset, codePoint);
        if (!defaultMatch.Valid)
        {
            result.Error = SvgOpenTypeMappingError.InvalidCharacterMap;
            return result;
        }
        if (defaultMatch.GlyphIndex == 1)
        {
            SvgOpenTypeGlyphMapping baseMapping = new SvgOpenTypeMapping().MapCodePoint(font, codePoint);
            if (!baseMapping.Success)
            {
                result.Error = baseMapping.Error;
                return result;
            }
            result.UsedDefaultVariation = true;
            new SvgOpenTypeMapping().Complete(result, baseMapping.GlyphIndex);
            return result;
        }
        result.Success = true;
        return result;
    }

    /// <summary>Gets and scales one glyph's horizontal and optional vertical metrics.</summary>
    /// <param name="font">A successfully parsed OpenType font.</param>
    /// <param name="glyphIndex">The zero-based glyph index.</param>
    /// <param name="emSize">The finite nonnegative output em size.</param>
    /// <returns>The design-unit and scaled glyph metrics.</returns>
    public SvgOpenTypeGlyphMetrics GetGlyphMetrics(SvgFont font, int glyphIndex, double emSize)
    {
        SvgOpenTypeGlyphMetrics result = new SvgOpenTypeGlyphMetrics(glyphIndex, emSize);
        if (!new SvgOpenTypeMapping().ValidFont(font) || glyphIndex < 0 || glyphIndex >= (font.MaximumProfile ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).GlyphCount || !SvgFiniteMath.IsFinite(emSize) || emSize < 0)
        {
            result.Error = SvgOpenTypeMetricError.InvalidArgument;
            return result;
        }
        if (font.Head == null || font.Head.UnitsPerEm < 1 || font.HorizontalMetrics == null || font.HorizontalMetrics.Count != font.MaximumProfile.GlyphCount)
        {
            result.Error = SvgOpenTypeMetricError.InvalidMetrics;
            return result;
        }
        SvgOpenTypeHorizontalMetric horizontal = font.HorizontalMetrics[glyphIndex];
        if (horizontal == null || horizontal.GlyphIndex != glyphIndex)
        {
            result.Error = SvgOpenTypeMetricError.InvalidMetrics;
            return result;
        }
        result.UnitsPerEm = font.Head.UnitsPerEm;
        result.Scale = emSize / font.Head.UnitsPerEm;
        result.AdvanceWidth = horizontal.AdvanceWidth;
        result.LeftSideBearing = horizontal.LeftSideBearing;
        result.ScaledAdvanceWidth = horizontal.AdvanceWidth * result.Scale;
        result.ScaledLeftSideBearing = horizontal.LeftSideBearing * result.Scale;
        if (font.VerticalHeader != null || font.VerticalMetrics.Count != 0)
        {
            if (font.VerticalHeader == null || font.VerticalMetrics.Count != font.MaximumProfile.GlyphCount)
            {
                result.Error = SvgOpenTypeMetricError.InvalidMetrics;
                return result;
            }
            SvgOpenTypeVerticalMetric vertical = font.VerticalMetrics[glyphIndex];
            if (vertical == null || vertical.GlyphIndex != glyphIndex)
            {
                result.Error = SvgOpenTypeMetricError.InvalidMetrics;
                return result;
            }
            result.HasVerticalMetrics = true;
            result.AdvanceHeight = vertical.AdvanceHeight;
            result.TopSideBearing = vertical.TopSideBearing;
            result.ScaledAdvanceHeight = vertical.AdvanceHeight * result.Scale;
            result.ScaledTopSideBearing = vertical.TopSideBearing * result.Scale;
        }
        if (!SvgFiniteMath.IsFinite(result.Scale) || !SvgFiniteMath.IsFinite(result.ScaledAdvanceWidth) || !SvgFiniteMath.IsFinite(result.ScaledLeftSideBearing) || !SvgFiniteMath.IsFinite(result.ScaledAdvanceHeight) || !SvgFiniteMath.IsFinite(result.ScaledTopSideBearing))
        {
            result.Error = SvgOpenTypeMetricError.NonFiniteResult;
            return result;
        }
        result.Scale = SvgFiniteMath.NormalizeZero(result.Scale);
        result.ScaledAdvanceWidth = SvgFiniteMath.NormalizeZero(result.ScaledAdvanceWidth);
        result.ScaledLeftSideBearing = SvgFiniteMath.NormalizeZero(result.ScaledLeftSideBearing);
        result.ScaledAdvanceHeight = SvgFiniteMath.NormalizeZero(result.ScaledAdvanceHeight);
        result.ScaledTopSideBearing = SvgFiniteMath.NormalizeZero(result.ScaledTopSideBearing);
        result.Success = true;
        return result;
    }
bool ValidFont(SvgFont font)
    {
        return font != null && font.Success && font.Bytes != null && font.MaximumProfile != null && font.MaximumProfile.GlyphCount > 0 && font.CharacterMaps != null;
    }
bool ValidScalar(int value)
    {
        return value >= 0 && value <= 1114111 && (value < 55296 || value > 57343);
    }
bool ValidVariationSelector(int value)
    {
        return value >= 65024 && value <= 65039 || value >= 917760 && value <= 917999;
    }
void Complete(SvgOpenTypeGlyphMapping result, int glyphIndex)
    {
        result.GlyphIndex = glyphIndex;
        result.Found = glyphIndex != 0;
        result.Missing = glyphIndex == 0;
        result.Success = true;
        result.Error = SvgOpenTypeMappingError.None;
    }
SvgOpenTypeCmapRecord? BestBaseMap(SvgFont font)
    {
        SvgOpenTypeCmapRecord? best = null;
        int bestScore = -1;
        int index = 0;
        while (index < font.CharacterMaps.Count)
        {
            SvgOpenTypeCmapRecord map = font.CharacterMaps[index];
            int score = new SvgOpenTypeMapping().MapScore(map);
            if (score > bestScore)
            {
                best = map;
                bestScore = score;
            }
            index = index + 1;
        }
        return best;
    }
int MapScore(SvgOpenTypeCmapRecord? map)
    {
        if (map == null || map.Format == 14) return -1;
        bool full = map.Format == 8 || map.Format == 10 || map.Format == 12 || map.Format == 13;
        int formatScore = map.Format == 12 ? 40 : map.Format == 10 ? 30 : map.Format == 13 ? 20 : map.Format == 8 ? 10 : map.Format == 4 ? 8 : map.Format == 6 ? 6 : map.Format == 2 ? 4 : map.Format == 0 ? 2 : -1000;
        if (map.PlatformIdentifier == 3 && map.EncodingIdentifier == 10 && full) return 500 + formatScore;
        if (map.PlatformIdentifier == 0 && map.EncodingIdentifier == 4 && full) return 480 + formatScore;
        if (map.PlatformIdentifier == 0 && map.EncodingIdentifier == 6 && map.Format == 13) return 470 + formatScore;
        if (map.PlatformIdentifier == 0 && map.EncodingIdentifier == 3 && !full) return 400 + formatScore;
        if (map.PlatformIdentifier == 3 && map.EncodingIdentifier == 1 && !full) return 380 + formatScore;
        if (map.PlatformIdentifier == 0 && map.EncodingIdentifier >= 0 && map.EncodingIdentifier <= 2) return 300 + formatScore;
        return -1;
    }
SvgOpenTypeCmapRecord? FindVariationMap(SvgFont font)
    {
        int index = 0;
        while (index < font.CharacterMaps.Count)
        {
            SvgOpenTypeCmapRecord map = font.CharacterMaps[index];
            if (map != null && map.PlatformIdentifier == 0 && map.EncodingIdentifier == 5 && map.Format == 14) return map;
            index = index + 1;
        }
        return null;
    }
SvgOpenTypeCmapBounds? Bounds(SvgFont font, SvgOpenTypeCmapRecord? record)
    {
        SvgOpenTypeTable? cmap = font.FindTable("cmap");
        if (cmap == null || record == null || record.Offset < 0 || record.Length < 2 || (long)record.Offset + record.Length > cmap.Length) return null;
        long start = (long)cmap.Offset + record.Offset;
        if (start < 0 || start + record.Length > (font.Bytes ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Length) return null;
        return new SvgOpenTypeCmapBounds((int)start, record.Length);
    }
SvgOpenTypeMapProbe MapRecord(SvgFont font, SvgOpenTypeCmapRecord record, int codePoint)
    {
        SvgOpenTypeCmapBounds? bounds = new SvgOpenTypeMapping().Bounds(font, record);
        if (bounds == null) return new SvgOpenTypeMapProbe(0, false);
        if (record.Format == 0) return new SvgOpenTypeMapping().MapFormat0(font.Bytes, bounds, codePoint);
        if (record.Format == 2) return new SvgOpenTypeMapping().MapFormat2(font.Bytes, bounds, codePoint);
        if (record.Format == 4) return new SvgOpenTypeMapping().MapFormat4(font.Bytes, bounds, codePoint);
        if (record.Format == 6) return new SvgOpenTypeMapping().MapFormat6(font.Bytes, bounds, codePoint);
        if (record.Format == 8) return new SvgOpenTypeMapping().MapFormat8(font.Bytes, bounds, codePoint);
        if (record.Format == 10) return new SvgOpenTypeMapping().MapFormat10(font.Bytes, bounds, codePoint);
        if (record.Format == 12) return new SvgOpenTypeMapping().MapGrouped(font.Bytes, bounds, codePoint, true);
        if (record.Format == 13) return new SvgOpenTypeMapping().MapGrouped(font.Bytes, bounds, codePoint, false);
        return new SvgOpenTypeMapProbe(0, false);
    }
SvgOpenTypeMapProbe MapFormat0(byte[]? bytes, SvgOpenTypeCmapBounds bounds, int codePoint)
    {
        if (bounds.Length != 262) return new SvgOpenTypeMapProbe(0, false);
        if (codePoint > 255) return new SvgOpenTypeMapProbe(0, true);
        return new SvgOpenTypeMapProbe((bytes ?? throw new System.InvalidOperationException("A required SVG value is unavailable."))[bounds.Start + 6 + codePoint], true);
    }
SvgOpenTypeMapProbe MapFormat2(byte[]? bytes, SvgOpenTypeCmapBounds bounds, int codePoint)
    {
        if (bounds.Length < 526 || codePoint > 65535) return new SvgOpenTypeMapProbe(0, bounds.Length >= 526);
        int high = codePoint > 255 ? codePoint >> 8 : 0;
        int low = codePoint > 255 ? codePoint & 255 : codePoint;
        int key = new SvgOpenTypeMapping().ReadUInt16(bytes, bounds.Start + 6 + high * 2);
        if (key % 8 != 0 || 518L + key + 8 > bounds.Length) return new SvgOpenTypeMapProbe(0, false);
        int subheader = bounds.Start + 518 + key;
        int firstCode = new SvgOpenTypeMapping().ReadUInt16(bytes, subheader);
        int entryCount = new SvgOpenTypeMapping().ReadUInt16(bytes, subheader + 2);
        if (firstCode > 255 || entryCount > 256 || firstCode + entryCount > 256) return new SvgOpenTypeMapProbe(0, false);
        if (low < firstCode || low >= firstCode + entryCount) return new SvgOpenTypeMapProbe(0, true);
        int delta = new SvgOpenTypeMapping().ReadInt16((bytes ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), subheader + 4);
        int rangeOffset = new SvgOpenTypeMapping().ReadUInt16(bytes, subheader + 6);
        long glyphOffset = (long)(subheader - bounds.Start) + 6 + rangeOffset + 2L * (low - firstCode);
        if (glyphOffset < 0 || glyphOffset + 2 > bounds.Length) return new SvgOpenTypeMapProbe(0, false);
        int glyph = new SvgOpenTypeMapping().ReadUInt16(bytes, bounds.Start + (int)glyphOffset);
        if (glyph != 0) glyph = (glyph + delta) & 65535;
        return new SvgOpenTypeMapProbe(glyph, true);
    }
SvgOpenTypeMapProbe MapFormat4(byte[]? bytes, SvgOpenTypeCmapBounds bounds, int codePoint)
    {
        if (bounds.Length < 24 || codePoint > 65535) return new SvgOpenTypeMapProbe(0, bounds.Length >= 24);
        int segmentBytes = new SvgOpenTypeMapping().ReadUInt16(bytes, bounds.Start + 6);
        if (segmentBytes < 2 || segmentBytes % 2 != 0 || 16L + 4L * segmentBytes > bounds.Length) return new SvgOpenTypeMapProbe(0, false);
        int segmentCount = segmentBytes / 2;
        int endCodes = bounds.Start + 14;
        int startCodes = bounds.Start + 16 + segmentCount * 2;
        int deltas = bounds.Start + 16 + segmentCount * 4;
        int rangeOffsets = bounds.Start + 16 + segmentCount * 6;
        int segment = 0;
        while (segment < segmentCount)
        {
            int end = new SvgOpenTypeMapping().ReadUInt16(bytes, endCodes + segment * 2);
            if (codePoint <= end)
            {
                int start = new SvgOpenTypeMapping().ReadUInt16(bytes, startCodes + segment * 2);
                if (codePoint < start) return new SvgOpenTypeMapProbe(0, true);
                int delta = new SvgOpenTypeMapping().ReadInt16((bytes ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), deltas + segment * 2);
                int range = new SvgOpenTypeMapping().ReadUInt16(bytes, rangeOffsets + segment * 2);
                if (range == 0) return new SvgOpenTypeMapProbe((codePoint + delta) & 65535, true);
                long glyphOffset = (long)(rangeOffsets - bounds.Start) + segment * 2 + range + 2L * (codePoint - start);
                if (glyphOffset < 0 || glyphOffset + 2 > bounds.Length) return new SvgOpenTypeMapProbe(0, false);
                int glyph = new SvgOpenTypeMapping().ReadUInt16(bytes, bounds.Start + (int)glyphOffset);
                if (glyph != 0) glyph = (glyph + delta) & 65535;
                return new SvgOpenTypeMapProbe(glyph, true);
            }
            segment = segment + 1;
        }
        return new SvgOpenTypeMapProbe(0, true);
    }
SvgOpenTypeMapProbe MapFormat6(byte[]? bytes, SvgOpenTypeCmapBounds bounds, int codePoint)
    {
        if (bounds.Length < 10 || codePoint > 65535) return new SvgOpenTypeMapProbe(0, bounds.Length >= 10);
        int first = new SvgOpenTypeMapping().ReadUInt16(bytes, bounds.Start + 6);
        int count = new SvgOpenTypeMapping().ReadUInt16(bytes, bounds.Start + 8);
        if (10L + 2L * count > bounds.Length) return new SvgOpenTypeMapProbe(0, false);
        if (codePoint < first || codePoint >= first + count) return new SvgOpenTypeMapProbe(0, true);
        return new SvgOpenTypeMapProbe(new SvgOpenTypeMapping().ReadUInt16(bytes, bounds.Start + 10 + (codePoint - first) * 2), true);
    }
SvgOpenTypeMapProbe MapFormat8(byte[]? bytes, SvgOpenTypeCmapBounds bounds, int codePoint)
    {
        if (bounds.Length < 8208) return new SvgOpenTypeMapProbe(0, false);
        uint encoded = (uint)codePoint;
        int firstWord = codePoint;
        if (codePoint >= 65536)
        {
            int value = codePoint - 65536;
            int high = 55296 + (value >> 10);
            int low = 56320 + (value & 1023);
            firstWord = high;
            encoded = (uint)high << 16 | (uint)low;
        }
        bool marked = ((bytes ?? throw new System.InvalidOperationException("A required SVG value is unavailable."))[bounds.Start + 12 + firstWord / 8] & 1 << (7 - firstWord % 8)) != 0;
        if (marked != (codePoint >= 65536)) return new SvgOpenTypeMapProbe(0, false);
        uint groups = new SvgOpenTypeMapping().ReadUInt32(bytes, bounds.Start + 8204);
        if (8208L + 12L * groups > bounds.Length) return new SvgOpenTypeMapProbe(0, false);
        return new SvgOpenTypeMapping().MapGroups(bytes, bounds, bounds.Start + 8208, groups, encoded, true);
    }
SvgOpenTypeMapProbe MapFormat10(byte[]? bytes, SvgOpenTypeCmapBounds bounds, int codePoint)
    {
        if (bounds.Length < 20) return new SvgOpenTypeMapProbe(0, false);
        uint first = new SvgOpenTypeMapping().ReadUInt32(bytes, bounds.Start + 12);
        uint count = new SvgOpenTypeMapping().ReadUInt32(bytes, bounds.Start + 16);
        if (20L + 2L * count > bounds.Length) return new SvgOpenTypeMapProbe(0, false);
        uint value = (uint)codePoint;
        if (value < first || (ulong)value >= (ulong)first + count) return new SvgOpenTypeMapProbe(0, true);
        return new SvgOpenTypeMapProbe(new SvgOpenTypeMapping().ReadUInt16(bytes, bounds.Start + 20 + (int)(value - first) * 2), true);
    }
SvgOpenTypeMapProbe MapGrouped(byte[]? bytes, SvgOpenTypeCmapBounds bounds, int codePoint, bool sequential)
    {
        if (bounds.Length < 16) return new SvgOpenTypeMapProbe(0, false);
        uint groups = new SvgOpenTypeMapping().ReadUInt32(bytes, bounds.Start + 12);
        if (16L + 12L * groups > bounds.Length) return new SvgOpenTypeMapProbe(0, false);
        return new SvgOpenTypeMapping().MapGroups(bytes, bounds, bounds.Start + 16, groups, (uint)codePoint, sequential);
    }
SvgOpenTypeMapProbe MapGroups(byte[]? bytes, SvgOpenTypeCmapBounds bounds, int groupStart, uint groups, uint codePoint, bool sequential)
    {
        uint precedingEnd = 0;
        int index = 0;
        while ((uint)index < groups)
        {
            int offset = groupStart + index * 12;
            uint start = new SvgOpenTypeMapping().ReadUInt32(bytes, offset);
            uint end = new SvgOpenTypeMapping().ReadUInt32(bytes, offset + 4);
            uint glyph = new SvgOpenTypeMapping().ReadUInt32(bytes, offset + 8);
            if (start > end || index > 0 && start <= precedingEnd) return new SvgOpenTypeMapProbe(0, false);
            if (codePoint >= start && codePoint <= end)
            {
                ulong mapped = sequential ? (ulong)glyph + codePoint - start : glyph;
                if (mapped > System.Int32.MaxValue) return new SvgOpenTypeMapProbe(0, false);
                return new SvgOpenTypeMapProbe((int)mapped, true);
            }
            precedingEnd = end;
            index = index + 1;
        }
        return new SvgOpenTypeMapProbe(0, true);
    }
SvgOpenTypeMapProbe MapNonDefaultVariation(byte[]? bytes, SvgOpenTypeCmapBounds bounds, uint relativeOffset, int codePoint)
    {
        if (relativeOffset == 0) return new SvgOpenTypeMapProbe(-1, true);
        if (relativeOffset > System.Int32.MaxValue || relativeOffset + 4L > bounds.Length) return new SvgOpenTypeMapProbe(0, false);
        int start = bounds.Start + (int)relativeOffset;
        uint count = new SvgOpenTypeMapping().ReadUInt32(bytes, start);
        if ((long)relativeOffset + 4L + 5L * count > bounds.Length) return new SvgOpenTypeMapProbe(0, false);
        int preceding = -1;
        int index = 0;
        while ((uint)index < count)
        {
            int offset = start + 4 + index * 5;
            int value = new SvgOpenTypeMapping().ReadUInt24(bytes, offset);
            if (value <= preceding || !new SvgOpenTypeMapping().ValidScalar(value)) return new SvgOpenTypeMapProbe(0, false);
            if (value == codePoint) return new SvgOpenTypeMapProbe(new SvgOpenTypeMapping().ReadUInt16(bytes, offset + 3), true);
            preceding = value;
            index = index + 1;
        }
        return new SvgOpenTypeMapProbe(-1, true);
    }
SvgOpenTypeMapProbe ContainsDefaultVariation(byte[] bytes, SvgOpenTypeCmapBounds bounds, uint relativeOffset, int codePoint)
    {
        if (relativeOffset == 0) return new SvgOpenTypeMapProbe(0, true);
        if (relativeOffset > System.Int32.MaxValue || relativeOffset + 4L > bounds.Length) return new SvgOpenTypeMapProbe(0, false);
        int start = bounds.Start + (int)relativeOffset;
        uint count = new SvgOpenTypeMapping().ReadUInt32(bytes, start);
        if ((long)relativeOffset + 4L + 4L * count > bounds.Length) return new SvgOpenTypeMapProbe(0, false);
        int precedingEnd = -1;
        int index = 0;
        while ((uint)index < count)
        {
            int offset = start + 4 + index * 4;
            int first = new SvgOpenTypeMapping().ReadUInt24(bytes, offset);
            int end = first + bytes[offset + 3];
            if (!new SvgOpenTypeMapping().ValidScalar(first) || !new SvgOpenTypeMapping().ValidScalar(end) || first <= precedingEnd) return new SvgOpenTypeMapProbe(0, false);
            if (codePoint >= first && codePoint <= end) return new SvgOpenTypeMapProbe(1, true);
            precedingEnd = end;
            index = index + 1;
        }
        return new SvgOpenTypeMapProbe(0, true);
    }
int ReadUInt16(byte[]? bytes, int offset)
    {
        return (bytes ?? throw new System.InvalidOperationException("A required SVG value is unavailable."))[offset] << 8 | bytes[offset + 1];
    }
int ReadInt16(byte[] bytes, int offset)
    {
        int value = new SvgOpenTypeMapping().ReadUInt16(bytes, offset);
        return value >= 32768 ? value - 65536 : value;
    }
int ReadUInt24(byte[]? bytes, int offset)
    {
        return (bytes ?? throw new System.InvalidOperationException("A required SVG value is unavailable."))[offset] << 16 | bytes[offset + 1] << 8 | bytes[offset + 2];
    }
uint ReadUInt32(byte[]? bytes, int offset)
    {
        return (uint)(bytes ?? throw new System.InvalidOperationException("A required SVG value is unavailable."))[offset] << 24 | (uint)bytes[offset + 1] << 16 | (uint)bytes[offset + 2] << 8 | bytes[offset + 3];
    }
}
