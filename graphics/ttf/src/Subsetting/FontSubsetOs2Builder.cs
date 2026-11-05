namespace ttf;

internal static class FontSubsetOs2Builder
{
    const int WeightTag = 0x77676874;
    const int ItalicTag = 0x6974616C;
    const int SlantTag = 0x736C6E74;

    static readonly int[] unicodeBlockRanges = new int[]
    {
        0, 0x0000, 0x007F,
        1, 0x0080, 0x00FF,
        2, 0x0100, 0x017F,
        3, 0x0180, 0x024F,
        4, 0x0250, 0x02AF, 4, 0x1D00, 0x1DBF,
        5, 0x02B0, 0x02FF, 5, 0xA700, 0xA71F,
        6, 0x0300, 0x036F, 6, 0x1DC0, 0x1DFF,
        7, 0x0370, 0x03FF,
        8, 0x2C80, 0x2CFF,
        9, 0x0400, 0x052F, 9, 0x2DE0, 0x2DFF,
        9, 0xA640, 0xA69F,
        10, 0x0530, 0x058F,
        11, 0x0590, 0x05FF,
        12, 0xA500, 0xA63F,
        13, 0x0600, 0x06FF, 13, 0x0750, 0x077F,
        14, 0x07C0, 0x07FF,
        15, 0x0900, 0x097F,
        16, 0x0980, 0x09FF,
        17, 0x0A00, 0x0A7F,
        18, 0x0A80, 0x0AFF,
        19, 0x0B00, 0x0B7F,
        20, 0x0B80, 0x0BFF,
        21, 0x0C00, 0x0C7F,
        22, 0x0C80, 0x0CFF,
        23, 0x0D00, 0x0D7F,
        24, 0x0E00, 0x0E7F,
        25, 0x0E80, 0x0EFF,
        26, 0x10A0, 0x10FF, 26, 0x2D00, 0x2D2F,
        27, 0x1B00, 0x1B7F,
        28, 0x1100, 0x11FF,
        29, 0x1E00, 0x1EFF, 29, 0x2C60, 0x2C7F,
        29, 0xA720, 0xA7FF,
        30, 0x1F00, 0x1FFF,
        31, 0x2000, 0x206F, 31, 0x2E00, 0x2E7F,
        32, 0x2070, 0x209F,
        33, 0x20A0, 0x20CF,
        34, 0x20D0, 0x20FF,
        35, 0x2100, 0x214F,
        36, 0x2150, 0x218F,
        37, 0x2190, 0x21FF, 37, 0x27F0, 0x27FF,
        37, 0x2900, 0x297F, 37, 0x2B00, 0x2BFF,
        38, 0x2200, 0x22FF, 38, 0x27C0, 0x27EF,
        38, 0x2980, 0x29FF, 38, 0x2A00, 0x2AFF,
        39, 0x2300, 0x23FF,
        40, 0x2400, 0x243F,
        41, 0x2440, 0x245F,
        42, 0x2460, 0x24FF,
        43, 0x2500, 0x257F,
        44, 0x2580, 0x259F,
        45, 0x25A0, 0x25FF,
        46, 0x2600, 0x26FF,
        47, 0x2700, 0x27BF,
        48, 0x3000, 0x303F,
        49, 0x3040, 0x309F,
        50, 0x30A0, 0x30FF, 50, 0x31F0, 0x31FF,
        51, 0x3100, 0x312F, 51, 0x31A0, 0x31BF,
        52, 0x3130, 0x318F,
        53, 0xA840, 0xA87F,
        54, 0x3200, 0x32FF,
        55, 0x3300, 0x33FF,
        56, 0xAC00, 0xD7AF,
        58, 0x10900, 0x1091F,
        59, 0x2E80, 0x2FDF, 59, 0x2FF0, 0x2FFF,
        59, 0x3190, 0x319F,
        59, 0x3400, 0x9FFF, 59, 0x20000, 0x2A6DF,
        60, 0xE000, 0xF8FF,
        61, 0x31C0, 0x31EF, 61, 0xF900, 0xFAFF,
        61, 0x2F800, 0x2FA1F,
        62, 0xFB00, 0xFB4F,
        63, 0xFB50, 0xFDFF,
        64, 0xFE20, 0xFE2F,
        65, 0xFE10, 0xFE1F, 65, 0xFE30, 0xFE4F,
        66, 0xFE50, 0xFE6F,
        67, 0xFE70, 0xFEFF,
        68, 0xFF00, 0xFFEF,
        69, 0xFFF0, 0xFFFF,
        70, 0x0F00, 0x0FFF,
        71, 0x0700, 0x074F,
        72, 0x0780, 0x07BF,
        73, 0x0D80, 0x0DFF,
        74, 0x1000, 0x109F,
        75, 0x1200, 0x139F, 75, 0x2D80, 0x2DDF,
        76, 0x13A0, 0x13FF,
        77, 0x1400, 0x167F,
        78, 0x1680, 0x169F,
        79, 0x16A0, 0x16FF,
        80, 0x1780, 0x17FF, 80, 0x19E0, 0x19FF,
        81, 0x1800, 0x18AF,
        82, 0x2800, 0x28FF,
        83, 0xA000, 0xA4CF,
        84, 0x1700, 0x177F,
        85, 0x10300, 0x1032F,
        86, 0x10330, 0x1034F,
        87, 0x10400, 0x1044F,
        88, 0x1D000, 0x1D24F,
        89, 0x1D400, 0x1D7FF,
        90, 0xF0000, 0xFFFFD, 90, 0x100000, 0x10FFFD,
        91, 0xFE00, 0xFE0F, 91, 0xE0100, 0xE01EF,
        92, 0xE0000, 0xE007F,
        93, 0x1900, 0x194F,
        94, 0x1950, 0x197F,
        95, 0x1980, 0x19DF,
        96, 0x1A00, 0x1A1F,
        97, 0x2C00, 0x2C5F,
        98, 0x2D30, 0x2D7F,
        99, 0x4DC0, 0x4DFF,
        100, 0xA800, 0xA82F,
        101, 0x10000, 0x1013F,
        102, 0x10140, 0x1018F,
        103, 0x10380, 0x1039F,
        104, 0x103A0, 0x103DF,
        105, 0x10450, 0x1047F,
        106, 0x10480, 0x104AF,
        107, 0x10800, 0x1083F,
        108, 0x10A00, 0x10A5F,
        109, 0x1D300, 0x1D35F,
        110, 0x12000, 0x1247F,
        111, 0x1D360, 0x1D37F,
        112, 0x1B80, 0x1BBF,
        113, 0x1C00, 0x1C4F,
        114, 0x1C50, 0x1C7F,
        115, 0xA880, 0xA8DF,
        116, 0xA900, 0xA92F,
        117, 0xA930, 0xA95F,
        118, 0xAA00, 0xAA5F,
        119, 0x10190, 0x101CF,
        120, 0x101D0, 0x101FF,
        121, 0x10280, 0x102DF, 121, 0x10920, 0x1093F,
        122, 0x1F000, 0x1F09F
    };

    internal static byte[]? Create(
        FontInstance instance,
        FontSubsetUnicodeMapping[] mappings,
        int[] oldToNewGlyphIds,
        int[] newToOldGlyphIds,
        int[] requestedCodePoints)
    {
        ValidateArguments(
            instance,
            mappings,
            oldToNewGlyphIds,
            newToOldGlyphIds,
            requestedCodePoints);
        FontFace face = instance.GetFace();
        SfntOs2Table? table = face.GetOs2Table();

        if (table == null)
        {
            return null;
        }

        SfntTableData? os2TableData = face.GetTableStore().FindTable(FontTags.Os2);
        System.ArgumentNullException.ThrowIfNull(os2TableData);
        byte[] source = os2TableData
            .GetDataSlice()
            .CopyBytes();
        int embeddingFlags = ReadUInt16(source, 8);
        byte[] data = new SfntStaticInstanceMetadata(
            instance,
            new int[] { 0, 0, 0, 0 })
            .PatchTable(FontTags.Os2, source);
        System.Collections.Generic.HashSet<int> codePoints =
            CollectCodePoints(mappings, oldToNewGlyphIds);
        WriteInt16(data, 2, CalculateAverageWidth(
            instance,
            newToOldGlyphIds));
        WriteUnicodeRanges(data, codePoints);
        WriteCharacterIndices(data, codePoints);
        WriteSelection(data, source, instance, table.GetVersion());

        if (table.HasCodePageRanges())
        {
            WriteCodePageRanges(
                data,
                codePoints,
                table.GetVersion(),
                FontSubsetCmapBuilder.ShouldPreserveSymbol(
                    instance,
                    requestedCodePoints));
        }

        WriteUInt16(data, 8, embeddingFlags);
        return data;
    }

    static void ValidateArguments(
        FontInstance instance,
        FontSubsetUnicodeMapping[] mappings,
        int[] oldToNewGlyphIds,
        int[] newToOldGlyphIds,
        int[] requestedCodePoints)
    {
        if (instance == null) { throw new System.ArgumentNullException("instance"); }
        if (mappings == null) { throw new System.ArgumentNullException("mappings"); }
        if (oldToNewGlyphIds == null)
        {
            throw new System.ArgumentNullException("oldToNewGlyphIds");
        }
        if (newToOldGlyphIds == null)
        {
            throw new System.ArgumentNullException("newToOldGlyphIds");
        }
        if (requestedCodePoints == null)
        {
            throw new System.ArgumentNullException("requestedCodePoints");
        }
        if (oldToNewGlyphIds.Length != instance.GetGlyphCount())
        {
            throw new System.ArgumentException(
                "The glyph map does not match the font instance.",
                "oldToNewGlyphIds");
        }
    }

    static System.Collections.Generic.HashSet<int> CollectCodePoints(
        FontSubsetUnicodeMapping[] mappings,
        int[] oldToNewGlyphIds)
    {
        System.Collections.Generic.HashSet<int> result =
            new System.Collections.Generic.HashSet<int>();
        int index = 0;

        while (index < mappings.Length)
        {
            FontSubsetUnicodeMapping mapping = mappings[index];
            int oldGlyphId = mapping.GetGlyphId();

            if (oldGlyphId < 0 || oldGlyphId >= oldToNewGlyphIds.Length)
            {
                throw FontFormatException.Create(
                    "A subset Unicode mapping references an invalid glyph.");
            }

            int[] sequence = mapping.CopyUnicodeSequence();

            if (oldToNewGlyphIds[oldGlyphId] > 0 && sequence.Length == 1)
            {
                result.Add(sequence[0]);
            }

            index++;
        }

        return result;
    }

    static int CalculateAverageWidth(
        FontInstance instance,
        int[] newToOldGlyphIds)
    {
        long total = 0;
        int count = 0;
        int index = 0;

        while (index < newToOldGlyphIds.Length)
        {
            int oldGlyphId = newToOldGlyphIds[index];

            if (oldGlyphId >= 0)
            {
                int width = instance.GetGlyphMetrics(oldGlyphId)
                    .GetAdvanceWidth();

                if (width > 0)
                {
                    total += width;
                    count++;
                }
            }

            index++;
        }

        if (count == 0) { return 0; }
        long rounded = (long)System.Math.Round(
            (double)total / count,
            System.MidpointRounding.AwayFromZero);
        return Clamp((int)rounded, short.MinValue, short.MaxValue);
    }

    static void WriteUnicodeRanges(
        byte[] data,
        System.Collections.Generic.HashSet<int> codePoints)
    {
        uint[] ranges = new uint[4];

        foreach (int codePoint in codePoints)
        {
            int index = 0;

            while (index < unicodeBlockRanges.Length)
            {
                if (codePoint >= unicodeBlockRanges[index + 1] &&
                    codePoint <= unicodeBlockRanges[index + 2])
                {
                    SetRangeBit(ranges, unicodeBlockRanges[index]);
                }

                index += 3;
            }

            if (codePoint >= 0x10000)
            {
                SetRangeBit(ranges, 57);
            }
        }

        int rangeIndex = 0;
        while (rangeIndex < ranges.Length)
        {
            WriteUInt32(data, 42 + rangeIndex * 4, ranges[rangeIndex]);
            rangeIndex++;
        }
    }

    static void WriteCharacterIndices(
        byte[] data,
        System.Collections.Generic.HashSet<int> codePoints)
    {
        int first = 0;
        int last = 0;

        if (codePoints.Count != 0)
        {
            first = int.MaxValue;

            foreach (int codePoint in codePoints)
            {
                if (codePoint < first) { first = codePoint; }
                if (codePoint > last) { last = codePoint; }
            }

            first = System.Math.Min(first, 0xFFFF);
            last = System.Math.Min(last, 0xFFFF);
        }

        WriteUInt16(data, 64, first);
        WriteUInt16(data, 66, last);
    }

    static void WriteSelection(
        byte[] data,
        byte[] source,
        FontInstance instance,
        int version)
    {
        int sourceSelection = ReadUInt16(source, 62);
        NormalizedFontCoordinates coordinates = instance.GetCoordinates();
        bool bold = HasAxis(coordinates, WeightTag)
            ? GetAxisValue(coordinates, WeightTag) >= 700
            : HasBit(sourceSelection, 5);
        bool italic = HasAxis(coordinates, ItalicTag)
            ? GetAxisValue(coordinates, ItalicTag) != 0
            : HasBit(sourceSelection, 0);
        bool oblique = version >= 4 && (HasAxis(coordinates, SlantTag)
            ? GetAxisValue(coordinates, SlantTag) != 0
            : HasBit(sourceSelection, 9));

        if (version < 4 && HasAxis(coordinates, SlantTag) &&
            GetAxisValue(coordinates, SlantTag) != 0)
        {
            italic = true;
        }

        int selection = sourceSelection;
        selection = SetBit(selection, 0, italic);
        selection = SetBit(selection, 5, bold);
        selection = SetBit(selection, 6, !bold && !italic && !oblique);
        selection = SetBit(selection, 9, oblique);
        WriteUInt16(data, 62, selection);
    }

    static void WriteCodePageRanges(
        byte[] data,
        System.Collections.Generic.HashSet<int> codePoints,
        int version,
        bool symbol)
    {
        ulong ranges = CalculateCodePageRanges(codePoints);

        if (symbol) { ranges |= 1UL << 31; }
        if (version == 1) { ranges &= ~(1UL << 8); }
        if (ranges == 0) { ranges = 1; }
        WriteUInt32(data, 78, (uint)(ranges & 0xFFFFFFFFUL));
        WriteUInt32(data, 82, (uint)(ranges >> 32));
    }

    static ulong CalculateCodePageRanges(
        System.Collections.Generic.HashSet<int> codePoints)
    {
        ulong ranges = 0;
        bool hasAscii = HasRange(codePoints, 0x20, 0x7D);
        bool hasLineArt = codePoints.Contains(0x2524);

        if (codePoints.Contains(0x00DE) && hasAscii) { ranges = SetBit(ranges, 0); }
        if (codePoints.Contains(0x013D) && hasAscii)
        {
            ranges = SetBit(ranges, 1);
            if (hasLineArt) { ranges = SetBit(ranges, 58); }
        }
        if (codePoints.Contains(0x0411))
        {
            ranges = SetBit(ranges, 2);
            if (codePoints.Contains(0x0405) && hasLineArt)
            {
                ranges = SetBit(ranges, 57);
            }
            if (codePoints.Contains(0x255C) && hasLineArt)
            {
                ranges = SetBit(ranges, 49);
            }
        }
        if (codePoints.Contains(0x0386))
        {
            ranges = SetBit(ranges, 3);
            if (hasLineArt && codePoints.Contains(0x00BD))
            {
                ranges = SetBit(ranges, 48);
            }
            if (hasLineArt && codePoints.Contains(0x221A))
            {
                ranges = SetBit(ranges, 60);
            }
        }
        if (codePoints.Contains(0x0130) && hasAscii)
        {
            ranges = SetBit(ranges, 4);
            if (hasLineArt) { ranges = SetBit(ranges, 56); }
        }
        if (codePoints.Contains(0x05D0))
        {
            ranges = SetBit(ranges, 5);
            if (hasLineArt && codePoints.Contains(0x221A))
            {
                ranges = SetBit(ranges, 53);
            }
        }
        if (codePoints.Contains(0x0631))
        {
            ranges = SetBit(ranges, 6);
            if (codePoints.Contains(0x221A))
            {
                ranges = SetBit(ranges, 51);
            }
            if (hasLineArt) { ranges = SetBit(ranges, 61); }
        }
        if (codePoints.Contains(0x0157) && hasAscii)
        {
            ranges = SetBit(ranges, 7);
            if (hasLineArt) { ranges = SetBit(ranges, 59); }
        }
        if (codePoints.Contains(0x20AB) && hasAscii)
        {
            ranges = SetBit(ranges, 8);
        }
        if (codePoints.Contains(0x0E45)) { ranges = SetBit(ranges, 16); }
        if (codePoints.Contains(0x30A8)) { ranges = SetBit(ranges, 17); }
        if (codePoints.Contains(0x3105)) { ranges = SetBit(ranges, 18); }
        if (codePoints.Contains(0x3131)) { ranges = SetBit(ranges, 19); }
        if (codePoints.Contains(0x592E)) { ranges = SetBit(ranges, 20); }
        if (codePoints.Contains(0xACF4)) { ranges = SetBit(ranges, 21); }
        if (codePoints.Contains(0x2665) && hasAscii)
        {
            ranges = SetBit(ranges, 30);
        }
        if (codePoints.Contains(0x00FE) && hasAscii && hasLineArt)
        {
            ranges = SetBit(ranges, 54);
        }
        if (codePoints.Contains(0x255A) && hasAscii)
        {
            ranges = SetBit(ranges, 62);
            ranges = SetBit(ranges, 63);
        }
        if (hasAscii && hasLineArt && codePoints.Contains(0x221A))
        {
            if (codePoints.Contains(0x00C5))
            {
                ranges = SetBit(ranges, 50);
            }
            if (codePoints.Contains(0x00E9))
            {
                ranges = SetBit(ranges, 52);
            }
            if (codePoints.Contains(0x00F5))
            {
                ranges = SetBit(ranges, 55);
            }
        }
        if (hasAscii && codePoints.Contains(0x2030) &&
            codePoints.Contains(0x2211))
        {
            ranges = SetBit(ranges, 29);
        }

        return ranges;
    }

    static bool HasRange(
        System.Collections.Generic.HashSet<int> values,
        int start,
        int end)
    {
        int value = start;
        while (value <= end)
        {
            if (!values.Contains(value)) { return false; }
            value++;
        }
        return true;
    }

    static bool HasAxis(NormalizedFontCoordinates coordinates, int tag)
    {
        int index = 0;
        while (index < coordinates.GetAxisCount())
        {
            if (coordinates.GetAxisTag(index) == tag) { return true; }
            index++;
        }
        return false;
    }

    static double GetAxisValue(
        NormalizedFontCoordinates coordinates,
        int tag)
    {
        int index = 0;
        while (index < coordinates.GetAxisCount())
        {
            if (coordinates.GetAxisTag(index) == tag)
            {
                return coordinates.GetUserValue(index);
            }
            index++;
        }
        throw new System.InvalidOperationException(
            "The requested variation axis is unavailable.");
    }

    static void SetRangeBit(uint[] ranges, int bit)
    {
        ranges[bit / 32] |= 1U << (bit & 31);
    }

    static ulong SetBit(ulong value, int bit)
    {
        return value | 1UL << bit;
    }

    static int SetBit(int value, int bit, bool enabled)
    {
        return enabled ? value | 1 << bit : value & ~(1 << bit);
    }

    static bool HasBit(int value, int bit)
    {
        return (value & 1 << bit) != 0;
    }

    static int Clamp(int value, int minimum, int maximum)
    {
        if (value < minimum) { return minimum; }
        return value > maximum ? maximum : value;
    }

    static int ReadUInt16(byte[] data, int offset)
    {
        return data[offset] << 8 | data[offset + 1];
    }

    static void WriteInt16(byte[] data, int offset, int value)
    {
        WriteUInt16(data, offset, value);
    }

    static void WriteUInt16(byte[] data, int offset, int value)
    {
        uint encoded = CheckedFontMath.ReinterpretInt32AsUInt32(value);
        data[offset] = (byte)((encoded >> 8) & 0xFFU);
        data[offset + 1] = (byte)(encoded & 0xFFU);
    }

    static void WriteUInt32(byte[] data, int offset, uint value)
    {
        data[offset] = (byte)((value >> 24) & 0xFFU);
        data[offset + 1] = (byte)((value >> 16) & 0xFFU);
        data[offset + 2] = (byte)((value >> 8) & 0xFFU);
        data[offset + 3] = (byte)(value & 0xFFU);
    }
}
