namespace ttf;

internal class SfntKernClassParseResult
{
    int offset;
    int length;
    SfntKernClassTable table;

    internal SfntKernClassParseResult(
        int offset,
        int length,
        SfntKernClassTable table)
    {
        this.offset = offset;
        this.length = length;
        this.table = table;
    }

    internal int GetOffset()
    {
        return offset;
    }

    internal int GetLength()
    {
        return length;
    }

    internal SfntKernClassTable GetTable()
    {
        return table;
    }
}

internal static class SfntKernTableParser
{
    const uint AppleVersion = 0x00010000U;
    const int WindowsHeaderLength = 4;
    const int WindowsSubtableHeaderLength = 6;
    const int AppleHeaderLength = 8;
    const int AppleSubtableHeaderLength = 8;
    static readonly SfntTableParser parser = new SfntTableParser(
        FontTags.Kern,
        0,
        1,
        0,
        ReadVersion,
        Parse);

    internal static SfntTableParser GetParser()
    {
        return parser;
    }

    internal static SfntParsedTableView? FindView(SfntTableCache cache)
    {
        if (cache == null)
        {
            throw new System.ArgumentNullException("cache");
        }

        return cache.FindParsedTable(parser);
    }

    internal static SfntKernTable? FindTable(SfntTableCache cache)
    {
        SfntParsedTableView? view = FindView(cache);

        if (view == null || !view.HasParsedData())
        {
            return null;
        }

        return (SfntKernTable)view.GetParsedData();
    }

    static SfntTableVersion ReadVersion(SfntTableData table)
    {
        BigEndianReader reader = new BigEndianReader(table.GetDataSlice());
        int majorVersion = reader.ReadUInt16();

        if (majorVersion == 0)
        {
            reader.ReadUInt16();
            return new SfntTableVersion(0, 0);
        }

        int minorVersion = reader.ReadUInt16();
        return new SfntTableVersion(majorVersion, minorVersion);
    }

    static object Parse(
        SfntTableData table,
        SfntTableVersion version,
        SfntTableVersionSupport support,
        FontLimits limits,
        SfntTableCache cache)
    {
        FontDataSlice tableData = table.GetDataSlice();

        if (tableData.GetLength() < WindowsHeaderLength)
        {
            throw FontFormatException.Create("The kern table header is truncated.");
        }

        SfntMaxpTable? maxp = SfntMaxpTableParser.FindTable(cache);

        if (maxp == null)
        {
            throw FontFormatException.Create(
                "The kern table requires a supported maxp table.");
        }

        BigEndianReader reader = new BigEndianReader(tableData);
        uint encodedHeader = reader.ReadUInt32();

        if (encodedHeader == AppleVersion)
        {
            return ParseApple(tableData, limits, maxp.GetGlyphCount());
        }

        if ((encodedHeader >> 16) == 0)
        {
            return ParseWindows(tableData, limits, maxp.GetGlyphCount());
        }

        throw FontFormatException.Create("The kern table version is not supported.");
    }

    static SfntKernTable ParseWindows(
        FontDataSlice tableData,
        FontLimits limits,
        int glyphCount)
    {
        BigEndianReader reader = new BigEndianReader(tableData);

        if (reader.ReadUInt16() != 0)
        {
            throw FontFormatException.Create("The Windows kern table version is invalid.");
        }

        int subtableCount = reader.ReadUInt16();
        ValidateSubtableCount(subtableCount, limits);
        SfntKernSubtable[] subtables = new SfntKernSubtable[subtableCount];
        int offset = WindowsHeaderLength;
        int index = 0;

        while (index < subtableCount)
        {
            if (offset > tableData.GetLength() - WindowsSubtableHeaderLength)
            {
                throw FontFormatException.Create("A Windows kern subtable header is truncated.");
            }

            BigEndianReader header = new BigEndianReader(tableData.GetSlice(
                offset,
                tableData.GetLength() - offset));
            int subtableVersion = header.ReadUInt16();
            int length = header.ReadUInt16();
            int coverage = header.ReadUInt16();

            if (subtableVersion != 0)
            {
                throw FontFormatException.Create("A Windows kern subtable version is invalid.");
            }

            if (length < WindowsSubtableHeaderLength ||
                length > tableData.GetLength() - offset)
            {
                throw FontFormatException.Create("A Windows kern subtable length is invalid.");
            }

            if ((coverage & 0x00F0) != 0)
            {
                throw FontFormatException.Create(
                    "A Windows kern subtable has nonzero reserved coverage bits.");
            }

            int format = coverage >> 8;

            if (format != 0 && format != 2)
            {
                throw FontFormatException.Create(
                    "The Windows kern subtable format is not supported.");
            }

            subtables[index] = ParseSubtable(
                tableData.GetSlice(offset, length),
                length,
                coverage,
                0,
                format,
                false,
                limits,
                glyphCount);
            offset = CheckedFontMath.AddOffset(offset, length);
            index += 1;
        }

        if (offset != tableData.GetLength())
        {
            throw FontFormatException.Create("The Windows kern table has trailing data.");
        }

        return new SfntKernTable(0, false, subtables);
    }

    static SfntKernTable ParseApple(
        FontDataSlice tableData,
        FontLimits limits,
        int glyphCount)
    {
        if (tableData.GetLength() < AppleHeaderLength)
        {
            throw FontFormatException.Create("The Apple kern table header is truncated.");
        }

        BigEndianReader reader = new BigEndianReader(tableData);

        if (reader.ReadUInt32() != AppleVersion)
        {
            throw FontFormatException.Create("The Apple kern table version is invalid.");
        }

        uint encodedCount = reader.ReadUInt32();

        if (encodedCount > int.MaxValue)
        {
            throw FontLimitException.Create("The Apple kern subtable count is too large.");
        }

        int subtableCount = (int)encodedCount;
        ValidateSubtableCount(subtableCount, limits);
        SfntKernSubtable[] subtables = new SfntKernSubtable[subtableCount];
        int offset = AppleHeaderLength;
        int index = 0;

        while (index < subtableCount)
        {
            if (offset > tableData.GetLength() - AppleSubtableHeaderLength)
            {
                throw FontFormatException.Create("An Apple kern subtable header is truncated.");
            }

            BigEndianReader header = new BigEndianReader(tableData.GetSlice(
                offset,
                tableData.GetLength() - offset));
            uint encodedLength = header.ReadUInt32();

            if (encodedLength > int.MaxValue)
            {
                throw FontFormatException.Create("An Apple kern subtable length is invalid.");
            }

            int length = (int)encodedLength;
            int coverage = header.ReadUInt16();
            int tupleIndex = header.ReadUInt16();

            if (length < AppleSubtableHeaderLength ||
                length > tableData.GetLength() - offset)
            {
                throw FontFormatException.Create("An Apple kern subtable length is invalid.");
            }

            if ((coverage & 0x1F00) != 0)
            {
                throw FontFormatException.Create(
                    "An Apple kern subtable has nonzero reserved coverage bits.");
            }

            int format = coverage & 0x00FF;

            if (format > 3)
            {
                throw FontFormatException.Create(
                    "The Apple kern subtable format is not supported.");
            }

            subtables[index] = ParseSubtable(
                tableData.GetSlice(offset, length),
                length,
                coverage,
                tupleIndex,
                format,
                true,
                limits,
                glyphCount);
            offset = CheckedFontMath.AddOffset(offset, length);
            index += 1;
        }

        if (offset != tableData.GetLength())
        {
            throw FontFormatException.Create("The Apple kern table has trailing data.");
        }

        return new SfntKernTable(AppleVersion, true, subtables);
    }

    static void ValidateSubtableCount(int count, FontLimits limits)
    {
        if (count > limits.GetMaxTablesPerFace())
        {
            throw FontLimitException.Create(
                "The kern subtable count exceeds the configured table limit.");
        }
    }

    static SfntKernSubtable ParseSubtable(
        FontDataSlice subtableData,
        int length,
        int coverage,
        int tupleIndex,
        int format,
        bool apple,
        FontLimits limits,
        int glyphCount)
    {
        int headerLength = apple ?
            AppleSubtableHeaderLength :
            WindowsSubtableHeaderLength;
        SfntKernFormat0Data? format0 = null;
        SfntKernFormat1Data? format1 = null;
        SfntKernFormat2Data? format2 = null;
        SfntKernFormat3Data? format3 = null;

        if (format == 0)
        {
            format0 = ParseFormat0(
                subtableData,
                headerLength,
                apple,
                limits,
                glyphCount);
        }
        else if (format == 1)
        {
            format1 = ParseFormat1(subtableData, limits, glyphCount);
        }
        else if (format == 2)
        {
            format2 = ParseFormat2(
                subtableData,
                headerLength,
                apple,
                limits,
                glyphCount);
        }
        else
        {
            format3 = ParseFormat3(subtableData, limits, glyphCount);
        }

        return new SfntKernSubtable(
            length,
            coverage,
            tupleIndex,
            format,
            apple,
            format0,
            format1,
            format2,
            format3);
    }

    static SfntKernFormat0Data ParseFormat0(
        FontDataSlice subtableData,
        int headerLength,
        bool apple,
        FontLimits limits,
        int glyphCount)
    {
        const int FormatHeaderLength = 8;
        const int PairLength = 6;
        int availableLength = subtableData.GetLength() - headerLength;

        if (availableLength < FormatHeaderLength)
        {
            throw FontFormatException.Create("A kern format 0 header is truncated.");
        }

        BigEndianReader reader = new BigEndianReader(subtableData.GetSlice(
            headerLength,
            availableLength));
        int pairCount = reader.ReadUInt16();
        int searchRange = reader.ReadUInt16();
        int entrySelector = reader.ReadUInt16();
        int rangeShift = reader.ReadUInt16();

        if (pairCount > limits.GetMaxCoverageEntries())
        {
            throw FontLimitException.Create(
                "A kern pair count exceeds the configured coverage limit.");
        }

        int pairsLength = CheckedFontMath.MultiplyCount(pairCount, PairLength);
        int expectedLength = CheckedFontMath.AddOffset(
            FormatHeaderLength,
            pairsLength);

        if (availableLength != expectedLength)
        {
            throw FontFormatException.Create("A kern format 0 length is invalid.");
        }

        ValidateSearchHeader(
            pairCount,
            PairLength,
            searchRange,
            entrySelector,
            rangeShift);
        SfntKernPair[] pairs = new SfntKernPair[pairCount];
        uint previousKey = 0;
        int index = 0;

        while (index < pairCount)
        {
            int leftGlyphId = reader.ReadUInt16();
            int rightGlyphId = reader.ReadUInt16();
            int value = reader.ReadInt16();
            uint key = ((uint)leftGlyphId << 16) | (uint)rightGlyphId;

            if (index > 0 && key <= previousKey)
            {
                throw FontFormatException.Create(
                    "The kern format 0 pairs are not strictly ordered.");
            }

            bool appleTerminator = apple &&
                index == pairCount - 1 &&
                leftGlyphId == 0xFFFF &&
                rightGlyphId == 0xFFFF &&
                value == 0;

            if (!appleTerminator &&
                (leftGlyphId >= glyphCount || rightGlyphId >= glyphCount))
            {
                throw FontFormatException.Create(
                    "A kern format 0 pair references an invalid glyph.");
            }

            pairs[index] = new SfntKernPair(leftGlyphId, rightGlyphId, value);
            previousKey = key;
            index += 1;
        }

        return new SfntKernFormat0Data(
            searchRange,
            entrySelector,
            rangeShift,
            pairs);
    }

    static void ValidateSearchHeader(
        int itemCount,
        int itemSize,
        int searchRange,
        int entrySelector,
        int rangeShift)
    {
        int power = 0;
        int selector = 0;

        if (itemCount > 0)
        {
            power = 1;

            while (power <= itemCount / 2)
            {
                power *= 2;
                selector += 1;
            }
        }

        int expectedSearchRange = power * itemSize;
        int expectedRangeShift = itemCount * itemSize - expectedSearchRange;

        if (searchRange != expectedSearchRange ||
            entrySelector != selector ||
            rangeShift != expectedRangeShift)
        {
            throw FontFormatException.Create("A kern binary-search header is invalid.");
        }
    }

    static SfntKernFormat2Data ParseFormat2(
        FontDataSlice subtableData,
        int headerLength,
        bool apple,
        FontLimits limits,
        int glyphCount)
    {
        const int FormatHeaderLength = 8;
        int minimumLength = CheckedFontMath.AddOffset(
            headerLength,
            FormatHeaderLength);

        if (subtableData.GetLength() < minimumLength)
        {
            throw FontFormatException.Create("A kern format 2 header is truncated.");
        }

        BigEndianReader reader = new BigEndianReader(subtableData.GetSlice(
            headerLength,
            FormatHeaderLength));
        int rowWidth = reader.ReadUInt16();
        int leftClassOffset = reader.ReadUInt16();
        int rightClassOffset = reader.ReadUInt16();
        int kerningArrayOffset = reader.ReadUInt16();

        if (rowWidth == 0 || (rowWidth & 1) != 0)
        {
            throw FontFormatException.Create("A kern format 2 row width is invalid.");
        }

        SfntKernClassParseResult leftResult = ParseClassTable(
            subtableData,
            leftClassOffset,
            apple ? kerningArrayOffset : 0,
            minimumLength,
            glyphCount,
            limits);
        SfntKernClassParseResult rightResult = ParseClassTable(
            subtableData,
            rightClassOffset,
            0,
            minimumLength,
            glyphCount,
            limits);
        ValidateClassTableOverlap(leftResult, rightResult);

        SfntKernClassTable leftTable = leftResult.GetTable();
        SfntKernClassTable rightTable = rightResult.GetTable();
        int leftOrigin = apple ? kerningArrayOffset : 0;
        int maximumLeftOffset = leftTable.GetDefaultOffset();
        int index = 0;

        while (index < leftTable.GetGlyphCount())
        {
            int value = leftTable.GetOffset(index);

            if (value < leftOrigin ||
                (value - leftOrigin) % rowWidth != 0)
            {
                throw FontFormatException.Create(
                    "A kern format 2 left class offset is invalid.");
            }

            if (value > maximumLeftOffset)
            {
                maximumLeftOffset = value;
            }

            index += 1;
        }

        index = 0;

        while (index < rightTable.GetGlyphCount())
        {
            int value = rightTable.GetOffset(index);

            if ((value & 1) != 0 || value >= rowWidth)
            {
                throw FontFormatException.Create(
                    "A kern format 2 right class offset is invalid.");
            }

            index += 1;
        }

        int normalizedMaximumLeft = maximumLeftOffset - leftOrigin;
        int arrayLength = CheckedFontMath.AddOffset(
            normalizedMaximumLeft,
            rowWidth);

        if (kerningArrayOffset < minimumLength ||
            arrayLength > subtableData.GetLength() - kerningArrayOffset)
        {
            throw FontFormatException.Create("The kern format 2 array is truncated.");
        }

        ValidateNoRangeOverlap(
            kerningArrayOffset,
            arrayLength,
            leftResult.GetOffset(),
            leftResult.GetLength(),
            "The kern format 2 array overlaps a class table.");
        ValidateNoRangeOverlap(
            kerningArrayOffset,
            arrayLength,
            rightResult.GetOffset(),
            rightResult.GetLength(),
            "The kern format 2 array overlaps a class table.");
        int valueCount = arrayLength / 2;

        if (valueCount > limits.GetMaxCoverageEntries())
        {
            throw FontLimitException.Create(
                "A kern format 2 array exceeds the configured coverage limit.");
        }

        BigEndianReader arrayReader = new BigEndianReader(subtableData.GetSlice(
            kerningArrayOffset,
            arrayLength));
        int[] values = new int[valueCount];
        index = 0;

        while (index < valueCount)
        {
            values[index] = arrayReader.ReadInt16();
            index += 1;
        }

        return new SfntKernFormat2Data(
            rowWidth,
            kerningArrayOffset,
            leftOrigin,
            leftTable,
            rightTable,
            values);
    }

    static SfntKernClassParseResult ParseClassTable(
        FontDataSlice subtableData,
        int offset,
        int defaultOffset,
        int minimumOffset,
        int glyphCount,
        FontLimits limits)
    {
        const int HeaderLength = 4;

        if (offset < minimumOffset || offset > subtableData.GetLength() - HeaderLength)
        {
            throw FontFormatException.Create("A kern format 2 class-table offset is invalid.");
        }

        BigEndianReader reader = new BigEndianReader(subtableData.GetSlice(
            offset,
            subtableData.GetLength() - offset));
        int firstGlyphId = reader.ReadUInt16();
        int count = reader.ReadUInt16();

        if (count > limits.GetMaxCoverageEntries())
        {
            throw FontLimitException.Create(
                "A kern format 2 class count exceeds the configured coverage limit.");
        }

        if (firstGlyphId > glyphCount || count > glyphCount - firstGlyphId)
        {
            throw FontFormatException.Create(
                "A kern format 2 class table references invalid glyphs.");
        }

        int length = CheckedFontMath.GetRangeEnd(HeaderLength, count, 2);

        if (length > subtableData.GetLength() - offset)
        {
            throw FontFormatException.Create("A kern format 2 class table is truncated.");
        }

        int[] offsets = new int[count];
        int index = 0;

        while (index < count)
        {
            offsets[index] = reader.ReadUInt16();
            index += 1;
        }

        return new SfntKernClassParseResult(
            offset,
            length,
            new SfntKernClassTable(firstGlyphId, defaultOffset, offsets));
    }

    static void ValidateClassTableOverlap(
        SfntKernClassParseResult left,
        SfntKernClassParseResult right)
    {
        if (left.GetOffset() == right.GetOffset() &&
            left.GetLength() == right.GetLength())
        {
            return;
        }

        ValidateNoRangeOverlap(
            left.GetOffset(),
            left.GetLength(),
            right.GetOffset(),
            right.GetLength(),
            "The kern format 2 class tables overlap.");
    }

    static SfntKernFormat1Data ParseFormat1(
        FontDataSlice subtableData,
        FontLimits limits,
        int glyphCount)
    {
        const int StateHeaderLength = 10;
        int stateTableLength = subtableData.GetLength() - AppleSubtableHeaderLength;

        if (stateTableLength < StateHeaderLength)
        {
            throw FontFormatException.Create("The kern format 1 state header is truncated.");
        }

        FontDataSlice stateData = subtableData.GetSlice(
            AppleSubtableHeaderLength,
            stateTableLength);
        BigEndianReader reader = new BigEndianReader(stateData);
        int classCount = reader.ReadUInt16();
        int classTableOffset = reader.ReadUInt16();
        int stateArrayOffset = reader.ReadUInt16();
        int entryTableOffset = reader.ReadUInt16();
        int valueTableOffset = reader.ReadUInt16();

        if (classCount < 4 || classCount > 256)
        {
            throw FontFormatException.Create("The kern format 1 class count is invalid.");
        }

        if (classTableOffset < StateHeaderLength ||
            valueTableOffset < entryTableOffset ||
            valueTableOffset > stateTableLength)
        {
            throw FontFormatException.Create("A kern format 1 section offset is invalid.");
        }

        int stateDataOffset = FindStateDataOffset(
            stateData,
            classTableOffset,
            stateTableLength);

        if (entryTableOffset <= stateDataOffset)
        {
            throw FontFormatException.Create("A kern format 1 section offset is invalid.");
        }

        SfntKernStateClassTable classTable = ParseStateClassTable(
            stateData,
            classTableOffset,
            stateDataOffset,
            classCount,
            glyphCount,
            limits);
        int encodedStateLength = entryTableOffset - stateDataOffset;
        int stateCount = encodedStateLength / classCount;
        int stateEntryCount = CheckedFontMath.MultiplyCount(stateCount, classCount);

        if (stateCount < 2)
        {
            throw FontFormatException.Create("The kern format 1 state array is invalid.");
        }

        if (stateEntryCount > limits.GetMaxCoverageEntries())
        {
            throw FontLimitException.Create(
                "A kern state array exceeds the configured coverage limit.");
        }

        BigEndianReader stateReader = new BigEndianReader(stateData.GetSlice(
            stateDataOffset,
            encodedStateLength));
        int[] stateEntries = new int[stateEntryCount];
        int index = 0;

        while (index < stateEntryCount)
        {
            stateEntries[index] = stateReader.ReadUInt8();
            index += 1;
        }

        while (stateReader.GetRemainingLength() > 0)
        {
            if (stateReader.ReadUInt8() != 0)
            {
                throw FontFormatException.Create(
                    "The kern format 1 state-array padding is nonzero.");
            }
        }

        int initialStateDelta = stateArrayOffset - stateDataOffset;

        if (initialStateDelta < 0 ||
            initialStateDelta % classCount != 0 ||
            initialStateDelta / classCount >= stateCount)
        {
            throw FontFormatException.Create(
                "The kern format 1 initial state offset is invalid.");
        }

        int entryBytes = valueTableOffset - entryTableOffset;

        if ((entryBytes & 3) != 0 || entryBytes == 0)
        {
            throw FontFormatException.Create("The kern format 1 entry table is invalid.");
        }

        int entryCount = entryBytes / 4;

        if (entryCount > limits.GetMaxCoverageEntries())
        {
            throw FontLimitException.Create(
                "A kern state entry count exceeds the configured coverage limit.");
        }

        index = 0;

        while (index < stateEntries.Length)
        {
            if (stateEntries[index] >= entryCount)
            {
                throw FontFormatException.Create(
                    "A kern state cell references an invalid entry.");
            }

            index += 1;
        }

        BigEndianReader entryReader = new BigEndianReader(stateData.GetSlice(
            entryTableOffset,
            entryBytes));
        SfntKernStateEntry[] entries = new SfntKernStateEntry[entryCount];
        index = 0;

        while (index < entryCount)
        {
            int newStateOffset = entryReader.ReadUInt16();
            int flags = entryReader.ReadUInt16();
            int stateDelta = newStateOffset - stateDataOffset;

            if (stateDelta < 0 ||
                stateDelta % classCount != 0 ||
                stateDelta / classCount >= stateCount)
            {
                throw FontFormatException.Create(
                    "A kern state entry references an invalid new state.");
            }

            int actionOffset = flags & 0x3FFF;

            if (actionOffset != 0 &&
                (actionOffset < valueTableOffset ||
                actionOffset >= stateTableLength ||
                (actionOffset & 1) != 0))
            {
                throw FontFormatException.Create(
                    "A kern state entry references an invalid value list.");
            }

            entries[index] = new SfntKernStateEntry(
                newStateOffset,
                stateDelta / classCount,
                flags);
            index += 1;
        }

        int valueBytes = stateTableLength - valueTableOffset;

        if ((valueBytes & 1) != 0)
        {
            throw FontFormatException.Create("The kern format 1 value table is truncated.");
        }

        int valueCount = valueBytes / 2;

        if (valueCount > limits.GetMaxCoverageEntries())
        {
            throw FontLimitException.Create(
                "A kern state value count exceeds the configured coverage limit.");
        }

        BigEndianReader valueReader = new BigEndianReader(stateData.GetSlice(
            valueTableOffset,
            valueBytes));
        int[] values = new int[valueCount];
        index = 0;

        while (index < valueCount)
        {
            values[index] = valueReader.ReadInt16();
            index += 1;
        }

        return new SfntKernFormat1Data(
            classTableOffset,
            stateArrayOffset,
            stateDataOffset,
            entryTableOffset,
            valueTableOffset,
            classCount,
            stateCount,
            initialStateDelta / classCount,
            classTable,
            stateEntries,
            entries,
            values);
    }

    static int FindStateDataOffset(
        FontDataSlice stateData,
        int classTableOffset,
        int stateTableLength)
    {
        const int HeaderLength = 4;

        if (classTableOffset > stateTableLength - HeaderLength)
        {
            throw FontFormatException.Create("The kern state class table is truncated.");
        }

        BigEndianReader reader = new BigEndianReader(stateData.GetSlice(
            classTableOffset,
            stateTableLength - classTableOffset));
        reader.ReadUInt16();
        int count = reader.ReadUInt16();
        int classEnd = CheckedFontMath.GetRangeEnd(
            CheckedFontMath.AddOffset(classTableOffset, HeaderLength),
            count,
            1);
        int stateDataOffset = CheckedFontMath.AlignFour(classEnd);

        if (stateDataOffset > stateTableLength)
        {
            throw FontFormatException.Create("The kern state class table is truncated.");
        }

        return stateDataOffset;
    }

    static SfntKernStateClassTable ParseStateClassTable(
        FontDataSlice stateData,
        int offset,
        int nextOffset,
        int classCount,
        int glyphCount,
        FontLimits limits)
    {
        const int HeaderLength = 4;

        if (offset > nextOffset - HeaderLength)
        {
            throw FontFormatException.Create("The kern state class table is truncated.");
        }

        BigEndianReader reader = new BigEndianReader(stateData.GetSlice(
            offset,
            nextOffset - offset));
        int firstGlyphId = reader.ReadUInt16();
        int count = reader.ReadUInt16();

        if (count > limits.GetMaxCoverageEntries())
        {
            throw FontLimitException.Create(
                "A kern state class count exceeds the configured coverage limit.");
        }

        if (firstGlyphId > glyphCount || count > glyphCount - firstGlyphId)
        {
            throw FontFormatException.Create(
                "A kern state class table references invalid glyphs.");
        }

        if (count > reader.GetRemainingLength())
        {
            throw FontFormatException.Create("The kern state class array is truncated.");
        }

        int[] classCodes = new int[count];
        int index = 0;

        while (index < count)
        {
            int classCode = reader.ReadUInt8();

            if (classCode >= classCount)
            {
                throw FontFormatException.Create(
                    "A kern state class code is invalid.");
            }

            classCodes[index] = classCode;
            index += 1;
        }

        while (reader.GetRemainingLength() > 0)
        {
            if (reader.ReadUInt8() != 0)
            {
                throw FontFormatException.Create(
                    "The kern state class-table padding is nonzero.");
            }
        }

        return new SfntKernStateClassTable(firstGlyphId, classCodes);
    }

    static SfntKernFormat3Data ParseFormat3(
        FontDataSlice subtableData,
        FontLimits limits,
        int maxpGlyphCount)
    {
        const int FormatHeaderLength = 6;
        int bodyLength = subtableData.GetLength() - AppleSubtableHeaderLength;

        if (bodyLength < FormatHeaderLength)
        {
            throw FontFormatException.Create("The kern format 3 header is truncated.");
        }

        BigEndianReader reader = new BigEndianReader(subtableData.GetSlice(
            AppleSubtableHeaderLength,
            bodyLength));
        int glyphCount = reader.ReadUInt16();
        int valueCount = reader.ReadUInt8();
        int leftClassCount = reader.ReadUInt8();
        int rightClassCount = reader.ReadUInt8();
        int flags = reader.ReadUInt8();

        if (glyphCount != maxpGlyphCount)
        {
            throw FontFormatException.Create(
                "The kern format 3 glyph count does not match maxp.");
        }

        if (valueCount == 0 || leftClassCount == 0 || rightClassCount == 0)
        {
            throw FontFormatException.Create("A kern format 3 count is invalid.");
        }

        if (flags != 0)
        {
            throw FontFormatException.Create("The kern format 3 reserved flags are nonzero.");
        }

        int indexCount = CheckedFontMath.MultiplyCount(
            leftClassCount,
            rightClassCount);
        int expectedBodyLength = CheckedFontMath.AddCounts(
            FormatHeaderLength,
            CheckedFontMath.MultiplyCount(valueCount, 2));
        expectedBodyLength = CheckedFontMath.AddCounts(expectedBodyLength, glyphCount);
        expectedBodyLength = CheckedFontMath.AddCounts(expectedBodyLength, glyphCount);
        expectedBodyLength = CheckedFontMath.AddCounts(expectedBodyLength, indexCount);

        if (bodyLength != expectedBodyLength)
        {
            throw FontFormatException.Create("The kern format 3 length is invalid.");
        }

        int inspectedEntryCount = CheckedFontMath.AddCounts(
            valueCount,
            CheckedFontMath.MultiplyCount(glyphCount, 2));
        inspectedEntryCount = CheckedFontMath.AddCounts(
            inspectedEntryCount,
            indexCount);

        if (inspectedEntryCount > limits.GetMaxCoverageEntries())
        {
            throw FontLimitException.Create(
                "A kern format 3 table exceeds the configured coverage limit.");
        }

        int[] values = new int[valueCount];
        int index = 0;

        while (index < valueCount)
        {
            values[index] = reader.ReadInt16();
            index += 1;
        }

        int[] leftClasses = new int[glyphCount];
        index = 0;

        while (index < glyphCount)
        {
            int classCode = reader.ReadUInt8();

            if (classCode >= leftClassCount)
            {
                throw FontFormatException.Create("A kern format 3 left class is invalid.");
            }

            leftClasses[index] = classCode;
            index += 1;
        }

        int[] rightClasses = new int[glyphCount];
        index = 0;

        while (index < glyphCount)
        {
            int classCode = reader.ReadUInt8();

            if (classCode >= rightClassCount)
            {
                throw FontFormatException.Create("A kern format 3 right class is invalid.");
            }

            rightClasses[index] = classCode;
            index += 1;
        }

        int[] valueIndexes = new int[indexCount];
        index = 0;

        while (index < indexCount)
        {
            int valueIndex = reader.ReadUInt8();

            if (valueIndex >= valueCount)
            {
                throw FontFormatException.Create(
                    "A kern format 3 cell references an invalid value.");
            }

            valueIndexes[index] = valueIndex;
            index += 1;
        }

        return new SfntKernFormat3Data(
            values,
            leftClasses,
            rightClasses,
            leftClassCount,
            rightClassCount,
            valueIndexes);
    }

    static void ValidateNoRangeOverlap(
        int firstOffset,
        int firstLength,
        int secondOffset,
        int secondLength,
        string message)
    {
        int firstEnd = CheckedFontMath.AddOffset(firstOffset, firstLength);
        int secondEnd = CheckedFontMath.AddOffset(secondOffset, secondLength);

        if (firstOffset < secondEnd && secondOffset < firstEnd)
        {
            throw FontFormatException.Create(message);
        }
    }
}
