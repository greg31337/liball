namespace ttf;

internal class SfntCmapEncodingRecord
{
    int platformId;
    int encodingId;
    long subtableOffset;
    SfntCmapSubtable subtable;

    internal SfntCmapEncodingRecord(
        int platformId,
        int encodingId,
        long subtableOffset,
        SfntCmapSubtable subtable)
    {
        this.platformId = platformId;
        this.encodingId = encodingId;
        this.subtableOffset = subtableOffset;
        this.subtable = subtable;
    }

    internal int GetPlatformId()
    {
        return platformId;
    }

    internal int GetEncodingId()
    {
        return encodingId;
    }

    internal long GetSubtableOffset()
    {
        return subtableOffset;
    }

    internal SfntCmapSubtable GetSubtable()
    {
        return subtable;
    }
}

internal class SfntCmapSubtable
{
    int format;
    int length;
    long language;
    SfntCmapFormat0Data? format0;
    SfntCmapFormat2Data? format2;
    SfntCmapFormat4Data? format4;
    SfntCmapFormat6Data? format6;
    SfntCmapFormat8Data? format8;
    SfntCmapFormat10Data? format10;
    SfntCmapGroupedData? format12;
    SfntCmapGroupedData? format13;
    SfntCmapFormat14Data? format14;

    internal SfntCmapSubtable(
        int format,
        int length,
        long language,
        SfntCmapFormat0Data? format0,
        SfntCmapFormat2Data? format2,
        SfntCmapFormat4Data? format4,
        SfntCmapFormat6Data? format6,
        SfntCmapFormat8Data? format8,
        SfntCmapFormat10Data? format10,
        SfntCmapGroupedData? format12,
        SfntCmapGroupedData? format13,
        SfntCmapFormat14Data? format14)
    {
        this.format = format;
        this.length = length;
        this.language = language;
        this.format0 = format0;
        this.format2 = format2;
        this.format4 = format4;
        this.format6 = format6;
        this.format8 = format8;
        this.format10 = format10;
        this.format12 = format12;
        this.format13 = format13;
        this.format14 = format14;
    }

    internal int GetFormat()
    {
        return format;
    }

    internal int GetLength()
    {
        return length;
    }

    internal long GetLanguage()
    {
        return language;
    }

    internal SfntCmapFormat0Data? GetFormat0()
    {
        return format0;
    }

    internal SfntCmapFormat2Data? GetFormat2()
    {
        return format2;
    }

    internal SfntCmapFormat4Data? GetFormat4()
    {
        return format4;
    }

    internal SfntCmapFormat6Data? GetFormat6()
    {
        return format6;
    }

    internal SfntCmapFormat8Data? GetFormat8()
    {
        return format8;
    }

    internal SfntCmapFormat10Data? GetFormat10()
    {
        return format10;
    }

    internal SfntCmapGroupedData? GetFormat12()
    {
        return format12;
    }

    internal SfntCmapGroupedData? GetFormat13()
    {
        return format13;
    }

    internal SfntCmapFormat14Data? GetFormat14()
    {
        return format14;
    }

    internal int MapCharacterCode(long characterCode)
    {
        if (format == 0)
        {
            System.ArgumentNullException.ThrowIfNull(format0);
            return format0.MapCharacterCode(characterCode);
        }

        if (format == 2)
        {
            System.ArgumentNullException.ThrowIfNull(format2);
            return format2.MapCharacterCode(characterCode);
        }

        if (format == 4)
        {
            System.ArgumentNullException.ThrowIfNull(format4);
            return format4.MapCharacterCode(characterCode);
        }

        if (format == 6)
        {
            System.ArgumentNullException.ThrowIfNull(format6);
            return format6.MapCharacterCode(characterCode);
        }

        if (format == 8)
        {
            System.ArgumentNullException.ThrowIfNull(format8);
            return format8.MapCharacterCode(characterCode);
        }

        if (format == 10)
        {
            System.ArgumentNullException.ThrowIfNull(format10);
            return format10.MapCharacterCode(characterCode);
        }

        if (format == 12)
        {
            System.ArgumentNullException.ThrowIfNull(format12);
            return format12.MapCharacterCode(characterCode);
        }

        if (format == 13)
        {
            System.ArgumentNullException.ThrowIfNull(format13);
            return format13.MapCharacterCode(characterCode);
        }

        return 0;
    }
}

internal class SfntCmapFormat0Data
{
    int[] glyphIds;

    internal SfntCmapFormat0Data(int[] glyphIds)
    {
        this.glyphIds = glyphIds;
    }

    internal int GetEntryCount()
    {
        return glyphIds.Length;
    }

    internal int GetGlyphId(int characterCode)
    {
        if (characterCode < 0 || characterCode >= glyphIds.Length)
        {
            throw new System.ArgumentOutOfRangeException("characterCode");
        }

        return glyphIds[characterCode];
    }

    internal int MapCharacterCode(long characterCode)
    {
        if (characterCode < 0 || characterCode >= glyphIds.Length)
        {
            return 0;
        }

        return glyphIds[(int)characterCode];
    }
}

internal class SfntCmapFormat2SubHeader
{
    int firstCode;
    int entryCount;
    int idDelta;
    int idRangeOffset;
    int[] glyphIds;

    internal SfntCmapFormat2SubHeader(
        int firstCode,
        int entryCount,
        int idDelta,
        int idRangeOffset,
        int[] glyphIds)
    {
        this.firstCode = firstCode;
        this.entryCount = entryCount;
        this.idDelta = idDelta;
        this.idRangeOffset = idRangeOffset;
        this.glyphIds = glyphIds;
    }

    internal int GetFirstCode()
    {
        return firstCode;
    }

    internal int GetEntryCount()
    {
        return entryCount;
    }

    internal int GetIdDelta()
    {
        return idDelta;
    }

    internal int GetIdRangeOffset()
    {
        return idRangeOffset;
    }

    internal int MapLowByte(int lowByte)
    {
        if (lowByte < firstCode || lowByte >= firstCode + entryCount)
        {
            return 0;
        }

        int glyphId = glyphIds[lowByte - firstCode];

        if (glyphId == 0)
        {
            return 0;
        }

        return glyphId + idDelta & 0xFFFF;
    }
}

internal class SfntCmapFormat2Data
{
    int[] subHeaderIndexes;
    SfntCmapFormat2SubHeader[] subHeaders;

    internal SfntCmapFormat2Data(
        int[] subHeaderIndexes,
        SfntCmapFormat2SubHeader[] subHeaders)
    {
        this.subHeaderIndexes = subHeaderIndexes;
        this.subHeaders = subHeaders;
    }

    internal int GetSubHeaderKeyCount()
    {
        return subHeaderIndexes.Length;
    }

    internal int GetSubHeaderIndex(int highByte)
    {
        if (highByte < 0 || highByte >= subHeaderIndexes.Length)
        {
            throw new System.ArgumentOutOfRangeException("highByte");
        }

        return subHeaderIndexes[highByte];
    }

    internal int GetSubHeaderCount()
    {
        return subHeaders.Length;
    }

    internal SfntCmapFormat2SubHeader GetSubHeader(int index)
    {
        if (index < 0 || index >= subHeaders.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return subHeaders[index];
    }

    internal int MapCharacterCode(long characterCode)
    {
        if (characterCode < 0 || characterCode > 0xFFFF)
        {
            return 0;
        }

        if (characterCode <= 0xFF)
        {
            return subHeaders[0].MapLowByte((int)characterCode);
        }

        int highByte = (int)(characterCode >> 8);
        int subHeaderIndex = subHeaderIndexes[highByte];

        if (subHeaderIndex == 0)
        {
            return 0;
        }

        return subHeaders[subHeaderIndex].MapLowByte((int)characterCode & 0xFF);
    }
}

internal class SfntCmapFormat4Segment
{
    int startCode;
    int endCode;
    int idDelta;
    int idRangeOffset;
    int[] glyphIds;

    internal SfntCmapFormat4Segment(
        int startCode,
        int endCode,
        int idDelta,
        int idRangeOffset,
        int[] glyphIds)
    {
        this.startCode = startCode;
        this.endCode = endCode;
        this.idDelta = idDelta;
        this.idRangeOffset = idRangeOffset;
        this.glyphIds = glyphIds;
    }

    internal int GetStartCode()
    {
        return startCode;
    }

    internal int GetEndCode()
    {
        return endCode;
    }

    internal int GetIdDelta()
    {
        return idDelta;
    }

    internal int GetIdRangeOffset()
    {
        return idRangeOffset;
    }

    internal int MapCharacterCode(long characterCode)
    {
        if (characterCode < startCode || characterCode > endCode)
        {
            return 0;
        }

        if (idRangeOffset == 0)
        {
            return (int)characterCode + idDelta & 0xFFFF;
        }

        int glyphId = glyphIds[(int)characterCode - startCode];

        if (glyphId == 0)
        {
            return 0;
        }

        return glyphId + idDelta & 0xFFFF;
    }
}

internal class SfntCmapFormat4Data
{
    int searchRange;
    int entrySelector;
    int rangeShift;
    SfntCmapFormat4Segment[] segments;

    internal SfntCmapFormat4Data(
        int searchRange,
        int entrySelector,
        int rangeShift,
        SfntCmapFormat4Segment[] segments)
    {
        this.searchRange = searchRange;
        this.entrySelector = entrySelector;
        this.rangeShift = rangeShift;
        this.segments = segments;
    }

    internal int GetSearchRange()
    {
        return searchRange;
    }

    internal int GetEntrySelector()
    {
        return entrySelector;
    }

    internal int GetRangeShift()
    {
        return rangeShift;
    }

    internal int GetSegmentCount()
    {
        return segments.Length;
    }

    internal SfntCmapFormat4Segment GetSegment(int index)
    {
        if (index < 0 || index >= segments.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return segments[index];
    }

    internal int MapCharacterCode(long characterCode)
    {
        if (characterCode < 0 || characterCode > 0xFFFF)
        {
            return 0;
        }

        int low = 0;
        int high = segments.Length - 1;

        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            SfntCmapFormat4Segment segment = segments[middle];

            if (characterCode > segment.GetEndCode())
            {
                low = middle + 1;
            }
            else if (characterCode < segment.GetStartCode())
            {
                high = middle - 1;
            }
            else
            {
                return segment.MapCharacterCode(characterCode);
            }
        }

        return 0;
    }
}

internal class SfntCmapFormat6Data
{
    int firstCode;
    int[] glyphIds;

    internal SfntCmapFormat6Data(int firstCode, int[] glyphIds)
    {
        this.firstCode = firstCode;
        this.glyphIds = glyphIds;
    }

    internal int GetFirstCode()
    {
        return firstCode;
    }

    internal int GetEntryCount()
    {
        return glyphIds.Length;
    }

    internal int GetGlyphId(int index)
    {
        if (index < 0 || index >= glyphIds.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return glyphIds[index];
    }

    internal int MapCharacterCode(long characterCode)
    {
        if (characterCode < firstCode ||
            characterCode >= firstCode + glyphIds.Length)
        {
            return 0;
        }

        return glyphIds[(int)characterCode - firstCode];
    }
}

internal class SfntCmapGroup
{
    long startCharacterCode;
    long endCharacterCode;
    int startGlyphId;

    internal SfntCmapGroup(
        long startCharacterCode,
        long endCharacterCode,
        int startGlyphId)
    {
        this.startCharacterCode = startCharacterCode;
        this.endCharacterCode = endCharacterCode;
        this.startGlyphId = startGlyphId;
    }

    internal long GetStartCharacterCode()
    {
        return startCharacterCode;
    }

    internal long GetEndCharacterCode()
    {
        return endCharacterCode;
    }

    internal int GetStartGlyphId()
    {
        return startGlyphId;
    }
}

internal class SfntCmapGroupedData
{
    bool constantGlyph;
    SfntCmapGroup[] groups;

    internal SfntCmapGroupedData(bool constantGlyph, SfntCmapGroup[] groups)
    {
        this.constantGlyph = constantGlyph;
        this.groups = groups;
    }

    internal bool UsesConstantGlyphs()
    {
        return constantGlyph;
    }

    internal int GetGroupCount()
    {
        return groups.Length;
    }

    internal SfntCmapGroup GetGroup(int index)
    {
        if (index < 0 || index >= groups.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return groups[index];
    }

    internal int MapCharacterCode(long characterCode)
    {
        if (characterCode < 0 || characterCode > uint.MaxValue)
        {
            return 0;
        }

        int low = 0;
        int high = groups.Length - 1;

        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            SfntCmapGroup group = groups[middle];

            if (characterCode < group.GetStartCharacterCode())
            {
                high = middle - 1;
            }
            else if (characterCode > group.GetEndCharacterCode())
            {
                low = middle + 1;
            }
            else if (constantGlyph)
            {
                return group.GetStartGlyphId();
            }
            else
            {
                return group.GetStartGlyphId() +
                    (int)(characterCode - group.GetStartCharacterCode());
            }
        }

        return 0;
    }
}

internal class SfntCmapFormat8Data
{
    byte[] is32;
    SfntCmapGroupedData groups;

    internal SfntCmapFormat8Data(byte[] is32, SfntCmapGroupedData groups)
    {
        this.is32 = is32;
        this.groups = groups;
    }

    internal bool Is32BitLead(int codeUnit)
    {
        if (codeUnit < 0 || codeUnit > 0xFFFF)
        {
            throw new System.ArgumentOutOfRangeException("codeUnit");
        }

        int mask = 1 << (7 - (codeUnit & 7));
        return (is32[codeUnit >> 3] & mask) != 0;
    }

    internal int GetGroupCount()
    {
        return groups.GetGroupCount();
    }

    internal SfntCmapGroup GetGroup(int index)
    {
        return groups.GetGroup(index);
    }

    internal int MapCharacterCode(long characterCode)
    {
        long encodedCode = characterCode;

        if (characterCode >= 0x10000 && characterCode <= 0x10FFFF)
        {
            long scalarOffset = characterCode - 0x10000;
            long highSurrogate = 0xD800 + (scalarOffset >> 10);
            long lowSurrogate = 0xDC00 + (scalarOffset & 0x3FF);
            encodedCode = highSurrogate << 16 | lowSurrogate;
        }
        else if (characterCode < 0 || characterCode > uint.MaxValue ||
            characterCode >= 0xD800 && characterCode <= 0xDFFF)
        {
            return 0;
        }

        return groups.MapCharacterCode(encodedCode);
    }
}

internal class SfntCmapFormat10Data
{
    long startCharacterCode;
    int[] glyphIds;

    internal SfntCmapFormat10Data(long startCharacterCode, int[] glyphIds)
    {
        this.startCharacterCode = startCharacterCode;
        this.glyphIds = glyphIds;
    }

    internal long GetStartCharacterCode()
    {
        return startCharacterCode;
    }

    internal int GetEntryCount()
    {
        return glyphIds.Length;
    }

    internal int GetGlyphId(int index)
    {
        if (index < 0 || index >= glyphIds.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return glyphIds[index];
    }

    internal int MapCharacterCode(long characterCode)
    {
        if (characterCode < startCharacterCode ||
            characterCode >= startCharacterCode + glyphIds.Length)
        {
            return 0;
        }

        return glyphIds[(int)(characterCode - startCharacterCode)];
    }
}

internal class SfntCmapTable
{
    int version;
    SfntCmapEncodingRecord[] encodingRecords;
    SfntCmapMapping mapping;

    internal SfntCmapTable(
        int version,
        SfntCmapEncodingRecord[] encodingRecords,
        SfntCmapMapping mapping)
    {
        this.version = version;
        this.encodingRecords = encodingRecords;
        this.mapping = mapping;
    }

    internal int GetVersion()
    {
        return version;
    }

    internal int GetEncodingRecordCount()
    {
        return encodingRecords.Length;
    }

    internal SfntCmapEncodingRecord GetEncodingRecord(int index)
    {
        if (index < 0 || index >= encodingRecords.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return encodingRecords[index];
    }

    internal SfntCmapMapping GetMapping()
    {
        return mapping;
    }
}

internal static class SfntCmapTableParser
{
    const int HeaderLength = 4;
    const int EncodingRecordLength = 8;
    const int Format0Length = 262;
    const int Format2KeysLength = 512;
    const int Format2SubHeaderLength = 8;
    const int Format8BitArrayLength = 8192;
    const int GroupLength = 12;
    const int Format14HeaderLength = 10;
    const int VariationSelectorRecordLength = 11;
    const int DefaultVariationRangeLength = 4;
    const int NonDefaultVariationMappingLength = 5;
    static readonly SfntTableParser parser = new SfntTableParser(
        FontTags.Cmap,
        0,
        0,
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

    internal static SfntCmapTable? FindTable(SfntTableCache cache)
    {
        SfntParsedTableView? view = FindView(cache);

        if (view == null || !view.HasParsedData())
        {
            return null;
        }

        return (SfntCmapTable)view.GetParsedData();
    }

    static SfntTableVersion ReadVersion(SfntTableData table)
    {
        BigEndianReader reader = new BigEndianReader(table.GetDataSlice());
        int version = reader.ReadUInt16();
        return version == 0 ?
            new SfntTableVersion(0, 0) :
            new SfntTableVersion(1, 0);
    }

    static object Parse(
        SfntTableData table,
        SfntTableVersion version,
        SfntTableVersionSupport support,
        FontLimits limits,
        SfntTableCache cache)
    {
        FontDataSlice tableData = table.GetDataSlice();

        if (tableData.GetLength() < HeaderLength)
        {
            throw FontFormatException.Create("The cmap table header is truncated.");
        }

        BigEndianReader reader = new BigEndianReader(tableData);
        int encodedVersion = reader.ReadUInt16();
        int recordCount = reader.ReadUInt16();

        if (encodedVersion != 0)
        {
            throw FontFormatException.Create("The cmap table version is not supported.");
        }

        if (recordCount > limits.GetMaxTablesPerFace())
        {
            throw FontLimitException.Create(
                "The cmap encoding record count exceeds the configured table limit.");
        }

        long encodingRecordsEnd = HeaderLength +
            recordCount * (long)EncodingRecordLength;

        if (encodingRecordsEnd > tableData.GetLength())
        {
            throw FontFormatException.Create("The cmap encoding record array is truncated.");
        }

        SfntMaxpTable? maxp = SfntMaxpTableParser.FindTable(cache);

        if (maxp == null)
        {
            throw FontFormatException.Create(
                "The cmap table requires a supported maxp table.");
        }

        int[] platformIds = new int[recordCount];
        int[] encodingIds = new int[recordCount];
        long[] offsets = new long[recordCount];
        int index = 0;

        while (index < recordCount)
        {
            platformIds[index] = reader.ReadUInt16();
            encodingIds[index] = reader.ReadUInt16();
            offsets[index] = reader.ReadOffset32();

            if (offsets[index] < encodingRecordsEnd ||
                offsets[index] > tableData.GetLength() - 2L)
            {
                throw FontFormatException.Create(
                    "A cmap encoding record has an invalid subtable offset.");
            }

            index += 1;
        }

        SfntCmapEncodingRecord[] records =
            new SfntCmapEncodingRecord[recordCount];
        long[] distinctOffsets = new long[recordCount];
        SfntCmapSubtable[] distinctSubtables =
            new SfntCmapSubtable[recordCount];
        int distinctCount = 0;
        index = 0;

        while (index < recordCount)
        {
            int existingIndex = FindOffset(
                distinctOffsets,
                distinctCount,
                offsets[index]);
            SfntCmapSubtable subtable;

            if (existingIndex >= 0)
            {
                subtable = distinctSubtables[existingIndex];
            }
            else
            {
                subtable = ParseSubtable(
                    tableData,
                    (int)offsets[index],
                    maxp.GetGlyphCount(),
                    limits);
                ValidateNoOverlap(
                    offsets[index],
                    subtable.GetLength(),
                    distinctOffsets,
                    distinctSubtables,
                    distinctCount);
                distinctOffsets[distinctCount] = offsets[index];
                distinctSubtables[distinctCount] = subtable;
                distinctCount += 1;
            }

            ValidateEncoding(
                platformIds[index],
                encodingIds[index],
                subtable.GetFormat());
            ValidateLanguage(platformIds[index], subtable.GetLanguage());
            records[index] = new SfntCmapEncodingRecord(
                platformIds[index],
                encodingIds[index],
                offsets[index],
                subtable);
            ValidateRecordOrder(records, index);
            index += 1;
        }

        SfntCmapMapping mapping = SfntCmapMappingBuilder.Build(
            records,
            maxp.GetGlyphCount(),
            limits);
        return new SfntCmapTable(encodedVersion, records, mapping);
    }

    static SfntCmapSubtable ParseSubtable(
        FontDataSlice tableData,
        int offset,
        int glyphCount,
        FontLimits limits)
    {
        FontDataSlice available = tableData.GetSlice(
            offset,
            tableData.GetLength() - offset);
        BigEndianReader headerReader = new BigEndianReader(available);
        int format = headerReader.ReadUInt16();

        if (format != 0 && format != 2 && format != 4 && format != 6 &&
            format != 8 && format != 10 && format != 12 && format != 13 &&
            format != 14)
        {
            throw FontFormatException.Create("The cmap subtable format is not supported.");
        }

        int length;
        long language;
        bool extendedHeader = format == 8 || format == 10 ||
            format == 12 || format == 13;

        if (format == 14)
        {
            long encodedLength = headerReader.ReadUInt32();

            if (encodedLength > int.MaxValue)
            {
                throw FontFormatException.Create("A cmap subtable length is invalid.");
            }

            length = (int)encodedLength;
            language = 0;
        }
        else if (extendedHeader)
        {
            if (headerReader.ReadUInt16() != 0)
            {
                throw FontFormatException.Create(
                    "A cmap subtable reserved field is not zero.");
            }

            long encodedLength = headerReader.ReadUInt32();

            if (encodedLength > int.MaxValue)
            {
                throw FontFormatException.Create("A cmap subtable length is invalid.");
            }

            length = (int)encodedLength;
            language = headerReader.ReadUInt32();
        }
        else
        {
            length = headerReader.ReadUInt16();
            language = headerReader.ReadUInt16();
        }

        int minimumHeaderLength = format == 14 ?
            Format14HeaderLength :
            extendedHeader ? 12 : 6;

        if (length < minimumHeaderLength || length > available.GetLength())
        {
            throw FontFormatException.Create("A cmap subtable length is invalid.");
        }

        FontDataSlice subtableData = available.GetSlice(0, length);
        BigEndianReader reader = new BigEndianReader(subtableData);
        reader.ReadUInt16();

        if (format == 14)
        {
            reader.ReadUInt32();
        }
        else if (extendedHeader)
        {
            reader.ReadUInt16();
            reader.ReadUInt32();
            reader.ReadUInt32();
        }
        else
        {
            reader.ReadUInt16();
            reader.ReadUInt16();
        }

        SfntCmapFormat0Data? format0 = null;
        SfntCmapFormat2Data? format2 = null;
        SfntCmapFormat4Data? format4 = null;
        SfntCmapFormat6Data? format6 = null;
        SfntCmapFormat8Data? format8 = null;
        SfntCmapFormat10Data? format10 = null;
        SfntCmapGroupedData? format12 = null;
        SfntCmapGroupedData? format13 = null;
        SfntCmapFormat14Data? format14 = null;

        if (format == 0)
        {
            format0 = ParseFormat0(length, reader, glyphCount, limits);
        }
        else if (format == 2)
        {
            format2 = ParseFormat2(
                subtableData,
                length,
                reader,
                glyphCount,
                limits);
        }
        else if (format == 4)
        {
            format4 = ParseFormat4(
                subtableData,
                length,
                reader,
                glyphCount,
                limits);
        }
        else if (format == 6)
        {
            format6 = ParseFormat6(length, reader, glyphCount, limits);
        }
        else if (format == 8)
        {
            format8 = ParseFormat8(length, reader, glyphCount, limits);
        }
        else if (format == 10)
        {
            format10 = ParseFormat10(length, reader, glyphCount, limits);
        }
        else if (format == 12)
        {
            format12 = ParseGroupedFormat(
                12,
                length,
                reader,
                false,
                glyphCount,
                limits);
        }
        else if (format == 13)
        {
            format13 = ParseGroupedFormat(
                13,
                length,
                reader,
                true,
                glyphCount,
                limits);
        }
        else
        {
            format14 = ParseFormat14(
                subtableData,
                length,
                reader,
                glyphCount,
                limits);
        }

        return new SfntCmapSubtable(
            format,
            length,
            language,
            format0,
            format2,
            format4,
            format6,
            format8,
            format10,
            format12,
            format13,
            format14);
    }

    static SfntCmapFormat0Data ParseFormat0(
        int length,
        BigEndianReader reader,
        int glyphCount,
        FontLimits limits)
    {
        if (length != Format0Length)
        {
            throw FontFormatException.Create("The cmap format 0 length is invalid.");
        }

        ValidateCoverageCount(256, limits);
        int[] glyphIds = new int[256];
        int index = 0;

        while (index < glyphIds.Length)
        {
            int glyphId = reader.ReadUInt8();
            ValidateGlyphId(glyphId, glyphCount);
            glyphIds[index] = glyphId;
            index += 1;
        }

        return new SfntCmapFormat0Data(glyphIds);
    }

    static SfntCmapFormat2Data ParseFormat2(
        FontDataSlice subtableData,
        int length,
        BigEndianReader reader,
        int glyphCount,
        FontLimits limits)
    {
        int minimumLength = 6 + Format2KeysLength + Format2SubHeaderLength;

        if (length < minimumLength || (length & 1) != 0)
        {
            throw FontFormatException.Create("The cmap format 2 length is invalid.");
        }

        int[] subHeaderIndexes = new int[256];
        int maximumSubHeaderIndex = 0;
        int index = 0;

        while (index < subHeaderIndexes.Length)
        {
            int key = reader.ReadUInt16();

            if ((key & 7) != 0)
            {
                throw FontFormatException.Create(
                    "A cmap format 2 subHeaderKey is not divisible by eight.");
            }

            int subHeaderIndex = key / Format2SubHeaderLength;
            subHeaderIndexes[index] = subHeaderIndex;

            if (subHeaderIndex > maximumSubHeaderIndex)
            {
                maximumSubHeaderIndex = subHeaderIndex;
            }

            index += 1;
        }

        if (subHeaderIndexes[0] != 0)
        {
            throw FontFormatException.Create(
                "The cmap format 2 single-byte key must select subheader zero.");
        }

        int subHeaderCount = maximumSubHeaderIndex + 1;
        long glyphArrayOffset = 6L + Format2KeysLength +
            subHeaderCount * (long)Format2SubHeaderLength;

        if (glyphArrayOffset > length)
        {
            throw FontFormatException.Create("The cmap format 2 subheaders are truncated.");
        }

        int[] firstCodes = new int[subHeaderCount];
        int[] entryCounts = new int[subHeaderCount];
        int[] deltas = new int[subHeaderCount];
        int[] rangeOffsets = new int[subHeaderCount];
        long totalEntries = 0;
        index = 0;

        while (index < subHeaderCount)
        {
            int firstCode = reader.ReadUInt16();
            int entryCount = reader.ReadUInt16();
            int delta = reader.ReadInt16();
            int rangeOffset = reader.ReadUInt16();

            if (firstCode > 255 || firstCode + (long)entryCount > 256)
            {
                throw FontFormatException.Create(
                    "A cmap format 2 subheader range is invalid.");
            }

            if ((rangeOffset & 1) != 0)
            {
                throw FontFormatException.Create(
                    "A cmap format 2 idRangeOffset is not aligned.");
            }

            firstCodes[index] = firstCode;
            entryCounts[index] = entryCount;
            deltas[index] = delta;
            rangeOffsets[index] = rangeOffset;
            totalEntries += entryCount;
            index += 1;
        }

        ValidateCoverageCount(totalEntries, limits);
        SfntCmapFormat2SubHeader[] subHeaders =
            new SfntCmapFormat2SubHeader[subHeaderCount];
        index = 0;

        while (index < subHeaderCount)
        {
            long rangeOffsetWord = 6L + Format2KeysLength +
                index * (long)Format2SubHeaderLength + 6;
            long glyphOffset = rangeOffsetWord + rangeOffsets[index];
            long glyphEnd = glyphOffset + entryCounts[index] * 2L;

            if (entryCounts[index] != 0 &&
                (glyphOffset < glyphArrayOffset || glyphEnd > length))
            {
                throw FontFormatException.Create(
                    "A cmap format 2 glyph sub-array is outside the subtable.");
            }

            int[] glyphIds = new int[entryCounts[index]];
            int glyphIndex = 0;

            BigEndianReader glyphReader = new BigEndianReader(
                subtableData.GetSlice(
                    entryCounts[index] == 0 ? 0 : (int)glyphOffset,
                    entryCounts[index] * 2));

            while (glyphIndex < glyphIds.Length)
            {
                int glyphId = glyphReader.ReadUInt16();

                if (glyphId != 0)
                {
                    ValidateGlyphId(glyphId + deltas[index] & 0xFFFF, glyphCount);
                }

                glyphIds[glyphIndex] = glyphId;
                glyphIndex += 1;
            }

            subHeaders[index] = new SfntCmapFormat2SubHeader(
                firstCodes[index],
                entryCounts[index],
                deltas[index],
                rangeOffsets[index],
                glyphIds);
            index += 1;
        }

        return new SfntCmapFormat2Data(subHeaderIndexes, subHeaders);
    }

    static SfntCmapFormat4Data ParseFormat4(
        FontDataSlice subtableData,
        int length,
        BigEndianReader reader,
        int glyphCount,
        FontLimits limits)
    {
        if (length < 22 || (length & 1) != 0)
        {
            throw FontFormatException.Create("The cmap format 4 length is invalid.");
        }

        int segmentCountX2 = reader.ReadUInt16();

        if (segmentCountX2 == 0 || (segmentCountX2 & 1) != 0)
        {
            throw FontFormatException.Create("The cmap format 4 segment count is invalid.");
        }

        int segmentCount = segmentCountX2 / 2;
        int searchRange = reader.ReadUInt16();
        int entrySelector = reader.ReadUInt16();
        int rangeShift = reader.ReadUInt16();
        ValidateFormat4SearchFields(
            segmentCount,
            searchRange,
            entrySelector,
            rangeShift);

        long minimumLength = 16L + segmentCount * 8L;

        if (minimumLength > length)
        {
            throw FontFormatException.Create("The cmap format 4 segment arrays are truncated.");
        }

        int[] endCodes = new int[segmentCount];
        int[] startCodes = new int[segmentCount];
        int[] deltas = new int[segmentCount];
        int[] rangeOffsets = new int[segmentCount];
        int index = 0;

        while (index < segmentCount)
        {
            endCodes[index] = reader.ReadUInt16();

            if (index != 0 && endCodes[index] <= endCodes[index - 1])
            {
                throw FontFormatException.Create(
                    "The cmap format 4 end codes are not strictly sorted.");
            }

            index += 1;
        }

        if (reader.ReadUInt16() != 0)
        {
            throw FontFormatException.Create("The cmap format 4 reserved pad is not zero.");
        }

        index = 0;

        while (index < segmentCount)
        {
            startCodes[index] = reader.ReadUInt16();
            index += 1;
        }

        index = 0;

        while (index < segmentCount)
        {
            deltas[index] = reader.ReadInt16();
            index += 1;
        }

        index = 0;

        while (index < segmentCount)
        {
            rangeOffsets[index] = reader.ReadUInt16();

            if ((rangeOffsets[index] & 1) != 0)
            {
                throw FontFormatException.Create(
                    "A cmap format 4 idRangeOffset is not aligned.");
            }

            index += 1;
        }

        long coverageCount = 0;
        index = 0;

        while (index < segmentCount)
        {
            if (startCodes[index] > endCodes[index])
            {
                throw FontFormatException.Create(
                    "A cmap format 4 segment has an inverted range.");
            }

            if (index != 0 && startCodes[index] <= endCodes[index - 1])
            {
                throw FontFormatException.Create(
                    "The cmap format 4 segments overlap or are not sorted.");
            }

            coverageCount += endCodes[index] - startCodes[index] + 1L;
            index += 1;
        }

        if (startCodes[segmentCount - 1] != 0xFFFF ||
            endCodes[segmentCount - 1] != 0xFFFF)
        {
            throw FontFormatException.Create(
                "The cmap format 4 final segment is not the required sentinel.");
        }

        ValidateCoverageCount(coverageCount, limits);
        SfntCmapFormat4Segment[] segments =
            new SfntCmapFormat4Segment[segmentCount];
        long glyphArrayOffset = minimumLength;
        index = 0;

        while (index < segmentCount)
        {
            int codeCount = endCodes[index] - startCodes[index] + 1;
            int[] glyphIds = new int[0];

            if (rangeOffsets[index] == 0)
            {
                int characterCode = startCodes[index];

                while (characterCode <= endCodes[index])
                {
                    ValidateGlyphId(characterCode + deltas[index] & 0xFFFF, glyphCount);
                    characterCode += 1;
                }
            }
            else
            {
                long rangeOffsetWord = 16L + segmentCount * 6L + index * 2L;
                long glyphOffset = rangeOffsetWord + rangeOffsets[index];
                long glyphEnd = glyphOffset + codeCount * 2L;

                if (glyphOffset < glyphArrayOffset || glyphEnd > length)
                {
                    throw FontFormatException.Create(
                        "A cmap format 4 glyph range is outside the subtable.");
                }

                BigEndianReader glyphReader = new BigEndianReader(
                    subtableData.GetSlice((int)glyphOffset, codeCount * 2));
                glyphIds = new int[codeCount];
                int glyphIndex = 0;

                while (glyphIndex < glyphIds.Length)
                {
                    int glyphId = glyphReader.ReadUInt16();

                    if (glyphId != 0)
                    {
                        ValidateGlyphId(
                            glyphId + deltas[index] & 0xFFFF,
                            glyphCount);
                    }

                    glyphIds[glyphIndex] = glyphId;
                    glyphIndex += 1;
                }
            }

            segments[index] = new SfntCmapFormat4Segment(
                startCodes[index],
                endCodes[index],
                deltas[index],
                rangeOffsets[index],
                glyphIds);
            index += 1;
        }

        return new SfntCmapFormat4Data(
            searchRange,
            entrySelector,
            rangeShift,
            segments);
    }

    static SfntCmapFormat6Data ParseFormat6(
        int length,
        BigEndianReader reader,
        int glyphCount,
        FontLimits limits)
    {
        if (length < 10 || (length & 1) != 0)
        {
            throw FontFormatException.Create("The cmap format 6 length is invalid.");
        }

        int firstCode = reader.ReadUInt16();
        int entryCount = reader.ReadUInt16();

        if (firstCode + (long)entryCount > 0x10000L ||
            length != 10L + entryCount * 2L)
        {
            throw FontFormatException.Create("The cmap format 6 range or length is invalid.");
        }

        ValidateCoverageCount(entryCount, limits);
        int[] glyphIds = new int[entryCount];
        int index = 0;

        while (index < entryCount)
        {
            int glyphId = reader.ReadUInt16();
            ValidateGlyphId(glyphId, glyphCount);
            glyphIds[index] = glyphId;
            index += 1;
        }

        return new SfntCmapFormat6Data(firstCode, glyphIds);
    }

    static SfntCmapFormat8Data ParseFormat8(
        int length,
        BigEndianReader reader,
        int glyphCount,
        FontLimits limits)
    {
        int minimumLength = 12 + Format8BitArrayLength + 4;

        if (length < minimumLength)
        {
            throw FontFormatException.Create("The cmap format 8 length is invalid.");
        }

        byte[] is32 = new byte[Format8BitArrayLength];
        int index = 0;

        while (index < is32.Length)
        {
            is32[index] = reader.ReadUInt8();
            index += 1;
        }

        long groupCount = reader.ReadUInt32();
        ValidateGroupLength(8, length, minimumLength, groupCount, limits);
        SfntCmapGroupedData groups = ReadGroups(
            reader,
            (int)groupCount,
            false,
            glyphCount);
        SfntCmapFormat8Data format = new SfntCmapFormat8Data(is32, groups);
        ValidateFormat8Groups(format);
        return format;
    }

    static SfntCmapFormat10Data ParseFormat10(
        int length,
        BigEndianReader reader,
        int glyphCount,
        FontLimits limits)
    {
        if (length < 20 || (length & 1) != 0)
        {
            throw FontFormatException.Create("The cmap format 10 length is invalid.");
        }

        long startCharacterCode = reader.ReadUInt32();
        long entryCount = reader.ReadUInt32();

        if (startCharacterCode + entryCount > 0x100000000L ||
            length != 20L + entryCount * 2L)
        {
            throw FontFormatException.Create(
                "The cmap format 10 range or length is invalid.");
        }

        ValidateCoverageCount(entryCount, limits);
        int[] glyphIds = new int[(int)entryCount];
        int index = 0;

        while (index < glyphIds.Length)
        {
            int glyphId = reader.ReadUInt16();
            ValidateGlyphId(glyphId, glyphCount);
            glyphIds[index] = glyphId;
            index += 1;
        }

        return new SfntCmapFormat10Data(startCharacterCode, glyphIds);
    }

    static SfntCmapGroupedData ParseGroupedFormat(
        int format,
        int length,
        BigEndianReader reader,
        bool constantGlyph,
        int glyphCount,
        FontLimits limits)
    {
        int minimumLength = 16;

        if (length < minimumLength)
        {
            throw FontFormatException.Create(
                "A grouped cmap subtable length is invalid.");
        }

        long groupCount = reader.ReadUInt32();
        ValidateGroupLength(format, length, minimumLength, groupCount, limits);
        return ReadGroups(
            reader,
            (int)groupCount,
            constantGlyph,
            glyphCount);
    }

    static SfntCmapFormat14Data ParseFormat14(
        FontDataSlice subtableData,
        int length,
        BigEndianReader reader,
        int glyphCount,
        FontLimits limits)
    {
        long selectorCount = reader.ReadUInt32();
        long selectorRecordsEnd = Format14HeaderLength +
            selectorCount * VariationSelectorRecordLength;
        ValidateCoverageCount(selectorCount, limits);

        if (selectorRecordsEnd > length)
        {
            throw FontFormatException.Create(
                "The cmap format 14 selector records are truncated.");
        }

        int[] variationSelectors = new int[(int)selectorCount];
        long[] defaultOffsets = new long[(int)selectorCount];
        long[] nonDefaultOffsets = new long[(int)selectorCount];
        int index = 0;

        while (index < variationSelectors.Length)
        {
            int variationSelector = (int)reader.ReadUInt24();

            if (!IsVariationSelector(variationSelector))
            {
                throw FontFormatException.Create(
                    "A cmap format 14 variation selector is invalid.");
            }

            if (index != 0 && variationSelector <= variationSelectors[index - 1])
            {
                throw FontFormatException.Create(
                    "The cmap format 14 variation selectors are not strictly sorted.");
            }

            variationSelectors[index] = variationSelector;
            defaultOffsets[index] = reader.ReadOffset32();
            nonDefaultOffsets[index] = reader.ReadOffset32();
            ValidateFormat14Offset(
                defaultOffsets[index],
                selectorRecordsEnd,
                length);
            ValidateFormat14Offset(
                nonDefaultOffsets[index],
                selectorRecordsEnd,
                length);
            index += 1;
        }

        SfntCmapVariationSelectorRecord[] records =
            new SfntCmapVariationSelectorRecord[(int)selectorCount];
        long coverageCount = selectorCount;
        index = 0;

        while (index < records.Length)
        {
            SfntCmapVariationDefaultRange[] defaultRanges =
                new SfntCmapVariationDefaultRange[0];
            SfntCmapVariationMapping[] nonDefaultMappings =
                new SfntCmapVariationMapping[0];

            if (defaultOffsets[index] != 0)
            {
                defaultRanges = ParseDefaultVariationRanges(
                    subtableData,
                    length,
                    (int)defaultOffsets[index],
                    coverageCount,
                    limits);
                coverageCount += defaultRanges.Length;
            }

            if (nonDefaultOffsets[index] != 0)
            {
                nonDefaultMappings = ParseNonDefaultVariationMappings(
                    subtableData,
                    length,
                    (int)nonDefaultOffsets[index],
                    coverageCount,
                    glyphCount,
                    limits);
                coverageCount += nonDefaultMappings.Length;
            }

            ValidateVariationPartitions(defaultRanges, nonDefaultMappings);
            records[index] = new SfntCmapVariationSelectorRecord(
                variationSelectors[index],
                defaultRanges,
                nonDefaultMappings);
            index += 1;
        }

        return new SfntCmapFormat14Data(records);
    }

    static SfntCmapVariationDefaultRange[] ParseDefaultVariationRanges(
        FontDataSlice subtableData,
        int length,
        int offset,
        long coverageCount,
        FontLimits limits)
    {
        BigEndianReader reader = new BigEndianReader(
            subtableData.GetSlice(offset, length - offset));
        long rangeCount = reader.ReadUInt32();
        ValidateCoverageCount(coverageCount + rangeCount, limits);

        if (4L + rangeCount * DefaultVariationRangeLength > length - offset)
        {
            throw FontFormatException.Create(
                "A cmap format 14 default variation table is truncated.");
        }

        SfntCmapVariationDefaultRange[] ranges =
            new SfntCmapVariationDefaultRange[(int)rangeCount];
        int previousEnd = -1;
        int index = 0;

        while (index < ranges.Length)
        {
            int startUnicodeValue = (int)reader.ReadUInt24();
            int additionalCount = reader.ReadUInt8();
            int endUnicodeValue = startUnicodeValue + additionalCount;
            ValidateUnicodeScalarRange(startUnicodeValue, endUnicodeValue);

            if (index != 0 && startUnicodeValue <= previousEnd)
            {
                throw FontFormatException.Create(
                    "The cmap format 14 default variation ranges overlap or are not sorted.");
            }

            ranges[index] = new SfntCmapVariationDefaultRange(
                startUnicodeValue,
                additionalCount);
            previousEnd = endUnicodeValue;
            index += 1;
        }

        return ranges;
    }

    static SfntCmapVariationMapping[] ParseNonDefaultVariationMappings(
        FontDataSlice subtableData,
        int length,
        int offset,
        long coverageCount,
        int glyphCount,
        FontLimits limits)
    {
        BigEndianReader reader = new BigEndianReader(
            subtableData.GetSlice(offset, length - offset));
        long mappingCount = reader.ReadUInt32();
        ValidateCoverageCount(coverageCount + mappingCount, limits);

        if (4L + mappingCount * NonDefaultVariationMappingLength >
            length - offset)
        {
            throw FontFormatException.Create(
                "A cmap format 14 non-default variation table is truncated.");
        }

        SfntCmapVariationMapping[] mappings =
            new SfntCmapVariationMapping[(int)mappingCount];
        int previousUnicodeValue = -1;
        int index = 0;

        while (index < mappings.Length)
        {
            int unicodeValue = (int)reader.ReadUInt24();
            int glyphId = reader.ReadUInt16();
            ValidateUnicodeScalarRange(unicodeValue, unicodeValue);
            ValidateGlyphId(glyphId, glyphCount);

            if (index != 0 && unicodeValue <= previousUnicodeValue)
            {
                throw FontFormatException.Create(
                    "The cmap format 14 non-default mappings are not strictly sorted.");
            }

            mappings[index] = new SfntCmapVariationMapping(
                unicodeValue,
                glyphId);
            previousUnicodeValue = unicodeValue;
            index += 1;
        }

        return mappings;
    }

    static void ValidateVariationPartitions(
        SfntCmapVariationDefaultRange[] defaultRanges,
        SfntCmapVariationMapping[] nonDefaultMappings)
    {
        int rangeIndex = 0;
        int mappingIndex = 0;

        while (rangeIndex < defaultRanges.Length &&
            mappingIndex < nonDefaultMappings.Length)
        {
            SfntCmapVariationDefaultRange range = defaultRanges[rangeIndex];
            int unicodeValue = nonDefaultMappings[mappingIndex].GetUnicodeValue();

            if (unicodeValue < range.GetStartUnicodeValue())
            {
                mappingIndex += 1;
            }
            else if (unicodeValue > range.GetEndUnicodeValue())
            {
                rangeIndex += 1;
            }
            else
            {
                throw FontFormatException.Create(
                    "A cmap format 14 sequence is both default and non-default.");
            }
        }
    }

    static void ValidateFormat14Offset(
        long offset,
        long selectorRecordsEnd,
        int length)
    {
        if (offset != 0 &&
            (offset < selectorRecordsEnd || offset > length - 4L))
        {
            throw FontFormatException.Create(
                "A cmap format 14 variation table offset is invalid.");
        }
    }

    static void ValidateUnicodeScalarRange(int start, int end)
    {
        if (!IsUnicodeScalar(start) || !IsUnicodeScalar(end) ||
            start <= 0xDFFF && end >= 0xD800)
        {
            throw FontFormatException.Create(
                "A cmap format 14 Unicode value is not a scalar value.");
        }
    }

    static bool IsUnicodeScalar(int value)
    {
        return value >= 0 && value <= 0x10FFFF &&
            (value < 0xD800 || value > 0xDFFF);
    }

    static bool IsVariationSelector(int value)
    {
        return value >= 0x180B && value <= 0x180D ||
            value == 0x180F ||
            value >= 0xFE00 && value <= 0xFE0F ||
            value >= 0xE0100 && value <= 0xE01EF;
    }

    static SfntCmapGroupedData ReadGroups(
        BigEndianReader reader,
        int groupCount,
        bool constantGlyph,
        int glyphCount)
    {
        SfntCmapGroup[] groups = new SfntCmapGroup[groupCount];
        long previousEnd = -1;
        int index = 0;

        while (index < groupCount)
        {
            long startCharacterCode = reader.ReadUInt32();
            long endCharacterCode = reader.ReadUInt32();
            long encodedGlyphId = reader.ReadUInt32();

            if (startCharacterCode > endCharacterCode)
            {
                throw FontFormatException.Create(
                    "A cmap character group has an inverted range.");
            }

            if (index != 0 && startCharacterCode <= previousEnd)
            {
                throw FontFormatException.Create(
                    "The cmap character groups overlap or are not sorted.");
            }

            long finalGlyphId = encodedGlyphId;

            if (!constantGlyph)
            {
                finalGlyphId += endCharacterCode - startCharacterCode;
            }

            if (encodedGlyphId >= glyphCount || finalGlyphId >= glyphCount)
            {
                throw FontFormatException.Create(
                    "A cmap character group references a glyph outside maxp.");
            }

            groups[index] = new SfntCmapGroup(
                startCharacterCode,
                endCharacterCode,
                (int)encodedGlyphId);
            previousEnd = endCharacterCode;
            index += 1;
        }

        return new SfntCmapGroupedData(constantGlyph, groups);
    }

    static void ValidateGroupLength(
        int format,
        int length,
        int minimumLength,
        long groupCount,
        FontLimits limits)
    {
        ValidateCoverageCount(groupCount, limits);

        if (length != minimumLength + groupCount * GroupLength)
        {
            throw FontFormatException.Create(
                "The cmap format " + format.ToString(
                    System.Globalization.CultureInfo.InvariantCulture) +
                " group array length is invalid.");
        }
    }

    static void ValidateFormat8Groups(SfntCmapFormat8Data format)
    {
        int[] bitCounts = new int[0x10001];
        int codeUnit = 0;

        while (codeUnit <= 0xFFFF)
        {
            int count = bitCounts[codeUnit];

            if (format.Is32BitLead(codeUnit))
            {
                count += 1;
            }

            bitCounts[codeUnit + 1] = count;
            codeUnit += 1;
        }

        int index = 0;

        while (index < format.GetGroupCount())
        {
            SfntCmapGroup group = format.GetGroup(index);
            long start = group.GetStartCharacterCode();
            long end = group.GetEndCharacterCode();

            if (end <= 0xFFFF)
            {
                if (CountFormat8Bits(bitCounts, (int)start, (int)end) != 0)
                {
                    throw FontFormatException.Create(
                        "A cmap format 8 group conflicts with its is32 array.");
                }
            }
            else
            {
                int startHigh = (int)(start >> 16);
                int endHigh = (int)(end >> 16);
                int startLow = (int)(start & 0xFFFF);
                int endLow = (int)(end & 0xFFFF);

                if (start <= 0xFFFF ||
                    !AreFormat8BitsSet(bitCounts, startHigh, endHigh))
                {
                    throw FontFormatException.Create(
                        "A cmap format 8 group conflicts with its is32 array.");
                }

                if (startHigh == endHigh)
                {
                    if (!AreFormat8BitsSet(bitCounts, startLow, endLow))
                    {
                        throw FontFormatException.Create(
                            "A cmap format 8 group conflicts with its is32 array.");
                    }
                }
                else if (!AreFormat8BitsSet(bitCounts, startLow, 0xFFFF) ||
                    !AreFormat8BitsSet(bitCounts, 0, endLow) ||
                    endHigh > startHigh + 1 && bitCounts[0x10000] != 0x10000)
                {
                    throw FontFormatException.Create(
                        "A cmap format 8 group conflicts with its is32 array.");
                }
            }

            index += 1;
        }
    }

    static int CountFormat8Bits(int[] bitCounts, int start, int end)
    {
        return bitCounts[end + 1] - bitCounts[start];
    }

    static bool AreFormat8BitsSet(int[] bitCounts, int start, int end)
    {
        return CountFormat8Bits(bitCounts, start, end) == end - start + 1;
    }

    static void ValidateFormat4SearchFields(
        int segmentCount,
        int searchRange,
        int entrySelector,
        int rangeShift)
    {
        int powerOfTwo = 1;
        int expectedEntrySelector = 0;

        while (powerOfTwo <= segmentCount / 2)
        {
            powerOfTwo *= 2;
            expectedEntrySelector += 1;
        }

        int expectedSearchRange = powerOfTwo * 2;
        int expectedRangeShift = segmentCount * 2 - expectedSearchRange;

        if (searchRange != expectedSearchRange ||
            entrySelector != expectedEntrySelector ||
            rangeShift != expectedRangeShift)
        {
            throw FontFormatException.Create(
                "The cmap format 4 binary-search fields are invalid.");
        }
    }

    static void ValidateGlyphId(int glyphId, int glyphCount)
    {
        if (glyphId < 0 || glyphId >= glyphCount)
        {
            throw FontFormatException.Create(
                "A cmap mapping references a glyph outside the maxp glyph range.");
        }
    }

    static void ValidateCoverageCount(long count, FontLimits limits)
    {
        if (count > limits.GetMaxCoverageEntries())
        {
            throw FontLimitException.Create(
                "A cmap subtable exceeds the configured coverage-entry limit.");
        }
    }

    static int FindOffset(long[] offsets, int count, long offset)
    {
        int index = 0;

        while (index < count)
        {
            if (offsets[index] == offset)
            {
                return index;
            }

            index += 1;
        }

        return -1;
    }

    static void ValidateNoOverlap(
        long offset,
        int length,
        long[] offsets,
        SfntCmapSubtable[] subtables,
        int count)
    {
        long end = offset + length;
        int index = 0;

        while (index < count)
        {
            long existingEnd = offsets[index] + subtables[index].GetLength();

            if (offset < existingEnd && offsets[index] < end)
            {
                throw FontFormatException.Create("Two cmap subtables overlap.");
            }

            index += 1;
        }
    }

    static void ValidateLanguage(int platformId, long language)
    {
        if (platformId != 1 && language != 0)
        {
            throw FontFormatException.Create(
                "A non-Macintosh cmap subtable has a nonzero language.");
        }
    }

    static void ValidateEncoding(int platformId, int encodingId, int format)
    {
        if ((format == 14 && (platformId != 0 || encodingId != 5)) ||
            (format != 14 && platformId == 0 && encodingId == 5))
        {
            throw FontFormatException.Create(
                "A cmap format 14 subtable must use Unicode variation encoding.");
        }
    }

    static void ValidateRecordOrder(
        SfntCmapEncodingRecord[] records,
        int index)
    {
        if (index == 0)
        {
            return;
        }

        SfntCmapEncodingRecord previous = records[index - 1];
        SfntCmapEncodingRecord current = records[index];
        int previousPlatform = previous.GetPlatformId();
        int currentPlatform = current.GetPlatformId();
        int previousEncoding = previous.GetEncodingId();
        int currentEncoding = current.GetEncodingId();

        if (currentPlatform < previousPlatform ||
            currentPlatform == previousPlatform &&
            currentEncoding < previousEncoding ||
            currentPlatform == previousPlatform &&
            currentEncoding == previousEncoding &&
            current.GetSubtable().GetLanguage() <=
            previous.GetSubtable().GetLanguage())
        {
            throw FontFormatException.Create(
                "The cmap encoding records are unsorted or duplicated.");
        }
    }
}
