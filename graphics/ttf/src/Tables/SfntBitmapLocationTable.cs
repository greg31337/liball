namespace ttf;

internal class SfntBitmapGlyphData
{
    int glyphId;
    int imageFormat;
    bool supported;
    bool png;
    int dataOffset;
    int dataLength;
    SfntBitmapGlyphMetrics? metrics;
    byte[] payload;
    SfntBitmapComponent[] components;

    internal SfntBitmapGlyphData(
        int glyphId,
        int imageFormat,
        bool supported,
        bool png,
        int dataOffset,
        int dataLength,
        SfntBitmapGlyphMetrics? metrics,
        byte[] payload,
        SfntBitmapComponent[] components)
    {
        this.glyphId = glyphId;
        this.imageFormat = imageFormat;
        this.supported = supported;
        this.png = png;
        this.dataOffset = dataOffset;
        this.dataLength = dataLength;
        this.metrics = metrics;
        this.payload = payload;
        this.components = components;
    }

    internal int GetGlyphId()
    {
        return glyphId;
    }

    internal int GetImageFormat()
    {
        return imageFormat;
    }

    internal bool IsSupported()
    {
        return supported;
    }

    internal bool IsPng()
    {
        return png;
    }

    internal int GetDataOffset()
    {
        return dataOffset;
    }

    internal int GetDataLength()
    {
        return dataLength;
    }

    internal bool HasMetrics()
    {
        return metrics != null;
    }

    internal SfntBitmapGlyphMetrics? GetMetrics()
    {
        return metrics;
    }

    internal int GetPayloadLength()
    {
        return payload.Length;
    }

    internal byte[] CopyPayload()
    {
        return payload.AsSpan().ToArray();
    }

    internal int GetComponentCount()
    {
        return components.Length;
    }

    internal SfntBitmapComponent GetComponent(int index)
    {
        if (index < 0 || index >= components.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return components[index];
    }
}

internal class SfntBitmapStrike
{
    bool color;
    int startGlyphId;
    int endGlyphId;
    int pixelsPerEmX;
    int pixelsPerEmY;
    int bitDepth;
    bool horizontalMetrics;
    SfntBitmapLineMetrics horizontalLineMetrics;
    SfntBitmapLineMetrics verticalLineMetrics;
    SfntBitmapGlyphData[] glyphs;
    int maximumComponentDepth;

    internal SfntBitmapStrike(
        bool color,
        int startGlyphId,
        int endGlyphId,
        int pixelsPerEmX,
        int pixelsPerEmY,
        int bitDepth,
        bool horizontalMetrics,
        SfntBitmapLineMetrics horizontalLineMetrics,
        SfntBitmapLineMetrics verticalLineMetrics,
        SfntBitmapGlyphData[] glyphs,
        int maximumComponentDepth)
    {
        this.color = color;
        this.startGlyphId = startGlyphId;
        this.endGlyphId = endGlyphId;
        this.pixelsPerEmX = pixelsPerEmX;
        this.pixelsPerEmY = pixelsPerEmY;
        this.bitDepth = bitDepth;
        this.horizontalMetrics = horizontalMetrics;
        this.horizontalLineMetrics = horizontalLineMetrics;
        this.verticalLineMetrics = verticalLineMetrics;
        this.glyphs = glyphs;
        this.maximumComponentDepth = maximumComponentDepth;
        ValidateComponents();
    }

    internal bool IsColor()
    {
        return color;
    }

    internal int GetStartGlyphId()
    {
        return startGlyphId;
    }

    internal int GetEndGlyphId()
    {
        return endGlyphId;
    }

    internal int GetPixelsPerEmX()
    {
        return pixelsPerEmX;
    }

    internal int GetPixelsPerEmY()
    {
        return pixelsPerEmY;
    }

    internal int GetBitDepth()
    {
        return bitDepth;
    }

    internal bool UsesHorizontalSmallMetrics()
    {
        return horizontalMetrics;
    }

    internal SfntBitmapLineMetrics GetHorizontalLineMetrics()
    {
        return horizontalLineMetrics;
    }

    internal SfntBitmapLineMetrics GetVerticalLineMetrics()
    {
        return verticalLineMetrics;
    }

    internal bool HasGlyph(int glyphId)
    {
        ValidateGlyphId(glyphId);
        return glyphs[glyphId] != null;
    }

    internal SfntBitmapGlyphData GetGlyph(int glyphId)
    {
        ValidateGlyphId(glyphId);
        return glyphs[glyphId];
    }

    void ValidateComponents()
    {
        bool[] active = new bool[glyphs.Length];
        int glyphId = 0;

        while (glyphId < glyphs.Length)
        {
            if (glyphs[glyphId] != null &&
                glyphs[glyphId].GetComponentCount() > 0)
            {
                ValidateComponentGlyph(glyphId, 1, active);
            }

            glyphId += 1;
        }
    }

    void ValidateComponentGlyph(int glyphId, int depth, bool[] active)
    {
        if (depth > maximumComponentDepth)
        {
            throw FontLimitException.Create(
                "An embedded bitmap composite exceeds the configured component-depth limit.");
        }

        if (active[glyphId])
        {
            throw FontFormatException.Create(
                "An embedded bitmap composite contains a glyph cycle.");
        }

        active[glyphId] = true;
        SfntBitmapGlyphData glyph = glyphs[glyphId];
        int componentIndex = 0;

        while (componentIndex < glyph.GetComponentCount())
        {
            int componentGlyphId = glyph.GetComponent(componentIndex).GetGlyphId();

            if (componentGlyphId < 0 || componentGlyphId >= glyphs.Length ||
                glyphs[componentGlyphId] == null)
            {
                throw FontFormatException.Create(
                    "An embedded bitmap composite references a missing glyph.");
            }

            if (glyphs[componentGlyphId].GetComponentCount() > 0)
            {
                ValidateComponentGlyph(componentGlyphId, depth + 1, active);
            }

            componentIndex += 1;
        }

        active[glyphId] = false;
    }

    void ValidateGlyphId(int glyphId)
    {
        if (glyphId < 0 || glyphId >= glyphs.Length)
        {
            throw new System.ArgumentOutOfRangeException("glyphId");
        }
    }
}

internal class SfntBitmapLocationTable
{
    int majorVersion;
    bool color;
    SfntBitmapStrike[] strikes;

    internal SfntBitmapLocationTable(
        int majorVersion,
        bool color,
        SfntBitmapStrike[] strikes)
    {
        this.majorVersion = majorVersion;
        this.color = color;
        this.strikes = strikes;
    }

    internal int GetMajorVersion()
    {
        return majorVersion;
    }

    internal bool IsColor()
    {
        return color;
    }

    internal int GetStrikeCount()
    {
        return strikes.Length;
    }

    internal SfntBitmapStrike GetStrike(int index)
    {
        if (index < 0 || index >= strikes.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return strikes[index];
    }

    internal SfntBitmapStrike? FindStrike(int pixelsPerEmX, int pixelsPerEmY)
    {
        int index = 0;

        while (index < strikes.Length)
        {
            if (strikes[index].GetPixelsPerEmX() == pixelsPerEmX &&
                strikes[index].GetPixelsPerEmY() == pixelsPerEmY)
            {
                return strikes[index];
            }

            index += 1;
        }

        return null;
    }
}

internal class SfntBitmapGlyphLocator
{
    int glyphId;
    int imageFormat;
    int offset;
    int length;
    SfntBitmapGlyphMetrics? sharedMetrics;

    internal SfntBitmapGlyphLocator(
        int glyphId,
        int imageFormat,
        int offset,
        int length,
        SfntBitmapGlyphMetrics? sharedMetrics)
    {
        this.glyphId = glyphId;
        this.imageFormat = imageFormat;
        this.offset = offset;
        this.length = length;
        this.sharedMetrics = sharedMetrics;
    }

    internal int GetGlyphId()
    {
        return glyphId;
    }

    internal int GetImageFormat()
    {
        return imageFormat;
    }

    internal int GetOffset()
    {
        return offset;
    }

    internal int GetLength()
    {
        return length;
    }

    internal SfntBitmapGlyphMetrics? GetSharedMetrics()
    {
        return sharedMetrics;
    }
}

internal class SfntBitmapSizeData
{
    int listOffset;
    int listSize;
    int subtableCount;
    SfntBitmapLineMetrics horizontalLineMetrics;
    SfntBitmapLineMetrics verticalLineMetrics;
    int startGlyphId;
    int endGlyphId;
    int pixelsPerEmX;
    int pixelsPerEmY;
    int bitDepth;
    bool horizontalMetrics;

    internal SfntBitmapSizeData(
        int listOffset,
        int listSize,
        int subtableCount,
        SfntBitmapLineMetrics horizontalLineMetrics,
        SfntBitmapLineMetrics verticalLineMetrics,
        int startGlyphId,
        int endGlyphId,
        int pixelsPerEmX,
        int pixelsPerEmY,
        int bitDepth,
        bool horizontalMetrics)
    {
        this.listOffset = listOffset;
        this.listSize = listSize;
        this.subtableCount = subtableCount;
        this.horizontalLineMetrics = horizontalLineMetrics;
        this.verticalLineMetrics = verticalLineMetrics;
        this.startGlyphId = startGlyphId;
        this.endGlyphId = endGlyphId;
        this.pixelsPerEmX = pixelsPerEmX;
        this.pixelsPerEmY = pixelsPerEmY;
        this.bitDepth = bitDepth;
        this.horizontalMetrics = horizontalMetrics;
    }

    internal int GetListOffset() { return listOffset; }
    internal int GetListSize() { return listSize; }
    internal int GetSubtableCount() { return subtableCount; }
    internal SfntBitmapLineMetrics GetHorizontalLineMetrics()
    {
        return horizontalLineMetrics;
    }
    internal SfntBitmapLineMetrics GetVerticalLineMetrics()
    {
        return verticalLineMetrics;
    }
    internal int GetStartGlyphId() { return startGlyphId; }
    internal int GetEndGlyphId() { return endGlyphId; }
    internal int GetPixelsPerEmX() { return pixelsPerEmX; }
    internal int GetPixelsPerEmY() { return pixelsPerEmY; }
    internal int GetBitDepth() { return bitDepth; }
    internal bool UsesHorizontalMetrics() { return horizontalMetrics; }
}

internal class SfntBitmapParsingBudget
{
    int maximumEntries;
    long entries;

    internal SfntBitmapParsingBudget(FontLimits limits)
    {
        maximumEntries = limits.GetMaxCoverageEntries();
    }

    internal void AddEntries(long count)
    {
        entries += count;

        if (count < 0 || entries > maximumEntries)
        {
            throw FontLimitException.Create(
                "Embedded bitmap structures exceed the configured coverage-entry limit.");
        }
    }
}

internal static class SfntEblcTableParser
{
    static readonly SfntTableParser parser = new SfntTableParser(
        FontTags.Eblc,
        2,
        2,
        0,
        ReadVersion,
        Parse);

    internal static SfntTableParser GetParser() { return parser; }
    internal static SfntParsedTableView? FindView(SfntTableCache cache)
    {
        if (cache == null) { throw new System.ArgumentNullException("cache"); }
        return cache.FindParsedTable(parser);
    }
    internal static SfntBitmapLocationTable? FindTable(SfntTableCache cache)
    {
        SfntParsedTableView? view = FindView(cache);
        if (view == null || !view.HasParsedData()) { return null; }
        return (SfntBitmapLocationTable)view.GetParsedData();
    }
    static SfntTableVersion ReadVersion(SfntTableData table)
    {
        return SfntBitmapLocationTableParser.ReadVersion(table);
    }
    static object Parse(
        SfntTableData table,
        SfntTableVersion version,
        SfntTableVersionSupport support,
        FontLimits limits,
        SfntTableCache cache)
    {
        SfntBitmapDataTable? dataTable = SfntEbdtTableParser.FindTable(cache);
        return SfntBitmapLocationTableParser.Parse(
            table, dataTable, 2, false, limits, cache);
    }
}

internal static class SfntCblcTableParser
{
    static readonly SfntTableParser parser = new SfntTableParser(
        FontTags.Cblc,
        3,
        3,
        0,
        ReadVersion,
        Parse);

    internal static SfntTableParser GetParser() { return parser; }
    internal static SfntParsedTableView? FindView(SfntTableCache cache)
    {
        if (cache == null) { throw new System.ArgumentNullException("cache"); }
        return cache.FindParsedTable(parser);
    }
    internal static SfntBitmapLocationTable? FindTable(SfntTableCache cache)
    {
        SfntParsedTableView? view = FindView(cache);
        if (view == null || !view.HasParsedData()) { return null; }
        return (SfntBitmapLocationTable)view.GetParsedData();
    }
    static SfntTableVersion ReadVersion(SfntTableData table)
    {
        return SfntBitmapLocationTableParser.ReadVersion(table);
    }
    static object Parse(
        SfntTableData table,
        SfntTableVersion version,
        SfntTableVersionSupport support,
        FontLimits limits,
        SfntTableCache cache)
    {
        SfntBitmapDataTable? dataTable = SfntCbdtTableParser.FindTable(cache);
        return SfntBitmapLocationTableParser.Parse(
            table, dataTable, 3, true, limits, cache);
    }
}

internal static class SfntBitmapLocationTableParser
{
    const int HeaderLength = 8;
    const int BitmapSizeLength = 48;
    const int IndexRecordLength = 8;

    internal static SfntTableVersion ReadVersion(SfntTableData table)
    {
        BigEndianReader reader = new BigEndianReader(table.GetDataSlice());
        return new SfntTableVersion(reader.ReadUInt16(), reader.ReadUInt16());
    }

    internal static SfntBitmapLocationTable Parse(
        SfntTableData table,
        SfntBitmapDataTable? dataTable,
        int expectedMajorVersion,
        bool color,
        FontLimits limits,
        SfntTableCache cache)
    {
        if (dataTable == null)
        {
            throw FontFormatException.Create(
                "An embedded bitmap location table requires its paired data table.");
        }

        FontDataSlice tableData = table.GetDataSlice();
        if (tableData.GetLength() < HeaderLength)
        {
            throw FontFormatException.Create(
                "An embedded bitmap location table header is truncated.");
        }

        BigEndianReader reader = new BigEndianReader(tableData);
        int majorVersion = reader.ReadUInt16();
        int minorVersion = reader.ReadUInt16();
        uint strikeCountValue = reader.ReadUInt32();

        if (majorVersion != expectedMajorVersion || minorVersion != 0)
        {
            throw FontFormatException.Create(
                "An embedded bitmap location table version is not supported.");
        }

        if (strikeCountValue > int.MaxValue)
        {
            throw FontLimitException.Create("The embedded bitmap strike count is too large.");
        }

        int strikeCount = (int)strikeCountValue;
        int sizesLength = CheckedFontMath.MultiplyCount(
            strikeCount, BitmapSizeLength);
        int requiredLength = CheckedFontMath.AddOffset(HeaderLength, sizesLength);
        if (requiredLength > tableData.GetLength())
        {
            throw FontFormatException.Create(
                "The embedded bitmap size-record array is truncated.");
        }

        SfntMaxpTable? maxp = SfntMaxpTableParser.FindTable(cache);
        if (maxp == null)
        {
            throw FontFormatException.Create(
                "An embedded bitmap location table requires a parsed maxp table.");
        }

        SfntBitmapParsingBudget budget = new SfntBitmapParsingBudget(limits);
        budget.AddEntries(strikeCount);
        budget.AddEntries((long)strikeCount * maxp.GetGlyphCount());
        SfntBitmapSizeData[] sizes = new SfntBitmapSizeData[strikeCount];
        int index = 0;
        while (index < sizes.Length)
        {
            sizes[index] = ReadBitmapSize(
                reader,
                tableData.GetLength(),
                requiredLength,
                maxp.GetGlyphCount(),
                color,
                limits);
            index += 1;
        }

        SfntBitmapStrike[] strikes = new SfntBitmapStrike[strikeCount];
        index = 0;
        while (index < strikes.Length)
        {
            strikes[index] = ParseStrike(
                tableData,
                dataTable,
                sizes[index],
                maxp.GetGlyphCount(),
                color,
                limits,
                budget);
            index += 1;
        }

        return new SfntBitmapLocationTable(majorVersion, color, strikes);
    }

    static SfntBitmapSizeData ReadBitmapSize(
        BigEndianReader reader,
        int tableLength,
        int minimumListOffset,
        int glyphCount,
        bool color,
        FontLimits limits)
    {
        int listOffset = ConvertUInt32(reader.ReadUInt32(), "bitmap index-list offset");
        int listSize = ConvertUInt32(reader.ReadUInt32(), "bitmap index-list size");
        int subtableCount = ConvertUInt32(reader.ReadUInt32(), "bitmap index count");
        uint colorReference = reader.ReadUInt32();
        SfntBitmapLineMetrics horizontal = ReadLineMetrics(reader);
        SfntBitmapLineMetrics vertical = ReadLineMetrics(reader);
        int startGlyphId = reader.ReadUInt16();
        int endGlyphId = reader.ReadUInt16();
        int pixelsPerEmX = reader.ReadUInt8();
        int pixelsPerEmY = reader.ReadUInt8();
        int bitDepth = reader.ReadUInt8();
        int flags = reader.ReadUInt8();

        if (colorReference != 0)
        {
            throw FontFormatException.Create(
                "An embedded bitmap color-reference field is not zero.");
        }

        if (startGlyphId > endGlyphId || endGlyphId >= glyphCount)
        {
            throw FontFormatException.Create(
                "An embedded bitmap strike glyph range is invalid.");
        }

        if (flags != 1 && flags != 2)
        {
            throw FontFormatException.Create("Embedded bitmap strike flags are invalid.");
        }

        if (!IsValidBitDepth(bitDepth, color))
        {
            throw FontFormatException.Create("An embedded bitmap bit depth is invalid.");
        }

        if (subtableCount > limits.GetMaxCoverageEntries())
        {
            throw FontLimitException.Create(
                "The embedded bitmap index count exceeds the configured limit.");
        }

        long listEnd = (long)listOffset + listSize;
        if (listSize < subtableCount * (long)IndexRecordLength ||
            listOffset < minimumListOffset || listEnd > tableLength)
        {
            throw FontFormatException.Create(
                "An embedded bitmap index-subtable list is outside the location table.");
        }

        return new SfntBitmapSizeData(
            listOffset, listSize, subtableCount, horizontal, vertical,
            startGlyphId, endGlyphId, pixelsPerEmX, pixelsPerEmY,
            bitDepth, flags == 1);
    }

    static SfntBitmapStrike ParseStrike(
        FontDataSlice tableData,
        SfntBitmapDataTable dataTable,
        SfntBitmapSizeData size,
        int glyphCount,
        bool color,
        FontLimits limits,
        SfntBitmapParsingBudget budget)
    {
        FontDataSlice listData = tableData.GetSlice(
            size.GetListOffset(), size.GetListSize());
        BigEndianReader reader = new BigEndianReader(listData);
        SfntBitmapGlyphLocator[] locators =
            new SfntBitmapGlyphLocator[glyphCount];
        int previousLastGlyphId = -1;
        int index = 0;
        budget.AddEntries(size.GetSubtableCount());

        while (index < size.GetSubtableCount())
        {
            int firstGlyphId = reader.ReadUInt16();
            int lastGlyphId = reader.ReadUInt16();
            int subtableOffset = ConvertUInt32(
                reader.ReadUInt32(), "bitmap index-subtable offset");

            if (firstGlyphId > lastGlyphId ||
                firstGlyphId <= previousLastGlyphId ||
                firstGlyphId < size.GetStartGlyphId() ||
                lastGlyphId > size.GetEndGlyphId())
            {
                throw FontFormatException.Create(
                    "Embedded bitmap index-subtable glyph ranges are invalid.");
            }

            long subtableEnd = (long)subtableOffset + 8;
            if (subtableOffset < size.GetSubtableCount() * IndexRecordLength ||
                subtableEnd > size.GetListSize() ||
                ((size.GetListOffset() + subtableOffset) & 3) != 0)
            {
                throw FontFormatException.Create(
                    "An embedded bitmap index subtable offset is invalid.");
            }

            ParseIndexSubtable(
                listData,
                subtableOffset,
                firstGlyphId,
                lastGlyphId,
                locators,
                dataTable,
                size.UsesHorizontalMetrics(),
                budget);
            previousLastGlyphId = lastGlyphId;
            index += 1;
        }

        SfntBitmapGlyphData[] glyphs = new SfntBitmapGlyphData[glyphCount];
        index = 0;
        while (index < glyphs.Length)
        {
            if (locators[index] != null)
            {
                glyphs[index] = ParseGlyphData(
                    dataTable,
                    locators[index],
                    size.UsesHorizontalMetrics(),
                    color,
                    glyphCount,
                    budget);
            }
            index += 1;
        }

        return new SfntBitmapStrike(
            color,
            size.GetStartGlyphId(),
            size.GetEndGlyphId(),
            size.GetPixelsPerEmX(),
            size.GetPixelsPerEmY(),
            size.GetBitDepth(),
            size.UsesHorizontalMetrics(),
            size.GetHorizontalLineMetrics(),
            size.GetVerticalLineMetrics(),
            glyphs,
            limits.GetMaxComponentDepth());
    }

    static void ParseIndexSubtable(
        FontDataSlice listData,
        int subtableOffset,
        int firstGlyphId,
        int lastGlyphId,
        SfntBitmapGlyphLocator[] locators,
        SfntBitmapDataTable dataTable,
        bool horizontalMetrics,
        SfntBitmapParsingBudget budget)
    {
        FontDataSlice subtableData = listData.GetSlice(
            subtableOffset, listData.GetLength() - subtableOffset);
        BigEndianReader reader = new BigEndianReader(subtableData);
        int indexFormat = reader.ReadUInt16();
        int imageFormat = reader.ReadUInt16();
        int imageDataOffset = ConvertUInt32(
            reader.ReadUInt32(), "bitmap image-data offset");

        if (imageDataOffset < 4 || imageDataOffset > dataTable.GetLength())
        {
            throw FontFormatException.Create(
                "An embedded bitmap image-data offset is invalid.");
        }

        if (indexFormat == 1 || indexFormat == 3)
        {
            ParseVariableOffsets(
                reader, indexFormat, imageFormat, imageDataOffset,
                firstGlyphId, lastGlyphId, locators, dataTable, budget);
            return;
        }

        if (indexFormat == 2)
        {
            int imageSize = ConvertUInt32(reader.ReadUInt32(), "bitmap image size");
            SfntBitmapGlyphMetrics metrics = ReadBigMetrics(reader);
            ParseConstantRange(
                imageFormat, imageDataOffset, imageSize, firstGlyphId,
                lastGlyphId, locators, dataTable, metrics, budget);
            return;
        }

        if (indexFormat == 4)
        {
            ParseSparseVariable(
                reader, imageFormat, imageDataOffset, firstGlyphId,
                lastGlyphId, locators, dataTable, budget);
            return;
        }

        if (indexFormat == 5)
        {
            int imageSize = ConvertUInt32(reader.ReadUInt32(), "bitmap image size");
            SfntBitmapGlyphMetrics metrics = ReadBigMetrics(reader);
            ParseSparseConstant(
                reader, imageFormat, imageDataOffset, imageSize,
                firstGlyphId, lastGlyphId, locators, dataTable,
                metrics, budget);
            return;
        }

        throw FontFormatException.Create(
            "An embedded bitmap index-subtable format is not supported.");
    }

    static void ParseVariableOffsets(
        BigEndianReader reader,
        int indexFormat,
        int imageFormat,
        int imageDataOffset,
        int firstGlyphId,
        int lastGlyphId,
        SfntBitmapGlyphLocator[] locators,
        SfntBitmapDataTable dataTable,
        SfntBitmapParsingBudget budget)
    {
        int count = lastGlyphId - firstGlyphId + 2;
        budget.AddEntries(count);
        long[] offsets = new long[count];
        int index = 0;
        while (index < count)
        {
            offsets[index] = indexFormat == 1 ?
                reader.ReadUInt32() : reader.ReadUInt16();
            if (index > 0 && offsets[index] < offsets[index - 1])
            {
                throw FontFormatException.Create(
                    "Embedded bitmap image offsets are unsorted.");
            }
            index += 1;
        }

        index = 0;
        while (index < count - 1)
        {
            long length = offsets[index + 1] - offsets[index];
            if (length > 0)
            {
                AddLocator(
                    locators,
                    firstGlyphId + index,
                    imageFormat,
                    imageDataOffset + offsets[index],
                    length,
                    null,
                    dataTable);
            }
            index += 1;
        }
    }

    static void ParseConstantRange(
        int imageFormat,
        int imageDataOffset,
        int imageSize,
        int firstGlyphId,
        int lastGlyphId,
        SfntBitmapGlyphLocator[] locators,
        SfntBitmapDataTable dataTable,
        SfntBitmapGlyphMetrics metrics,
        SfntBitmapParsingBudget budget)
    {
        if (imageSize == 0)
        {
            throw FontFormatException.Create("An embedded bitmap image size is zero.");
        }
        int count = lastGlyphId - firstGlyphId + 1;
        budget.AddEntries(count);
        int index = 0;
        while (index < count)
        {
            AddLocator(
                locators,
                firstGlyphId + index,
                imageFormat,
                imageDataOffset + (long)index * imageSize,
                imageSize,
                metrics,
                dataTable);
            index += 1;
        }
    }

    static void ParseSparseVariable(
        BigEndianReader reader,
        int imageFormat,
        int imageDataOffset,
        int firstGlyphId,
        int lastGlyphId,
        SfntBitmapGlyphLocator[] locators,
        SfntBitmapDataTable dataTable,
        SfntBitmapParsingBudget budget)
    {
        int glyphCount = ConvertUInt32(reader.ReadUInt32(), "sparse bitmap glyph count");
        budget.AddEntries((long)glyphCount + 1);
        int[] glyphIds = new int[glyphCount];
        int[] offsets = new int[glyphCount + 1];
        int index = 0;
        while (index <= glyphCount)
        {
            int glyphId = reader.ReadUInt16();
            int offset = reader.ReadUInt16();
            if (index < glyphCount)
            {
                if (glyphId < firstGlyphId || glyphId > lastGlyphId ||
                    (index > 0 && glyphId <= glyphIds[index - 1]))
                {
                    throw FontFormatException.Create(
                        "Sparse embedded bitmap glyph IDs are invalid.");
                }
                glyphIds[index] = glyphId;
            }
            if (index > 0 && offset < offsets[index - 1])
            {
                throw FontFormatException.Create(
                    "Sparse embedded bitmap offsets are unsorted.");
            }
            offsets[index] = offset;
            index += 1;
        }

        index = 0;
        while (index < glyphCount)
        {
            int length = offsets[index + 1] - offsets[index];
            if (length > 0)
            {
                AddLocator(
                    locators, glyphIds[index], imageFormat,
                    imageDataOffset + offsets[index], length, null, dataTable);
            }
            index += 1;
        }
    }

    static void ParseSparseConstant(
        BigEndianReader reader,
        int imageFormat,
        int imageDataOffset,
        int imageSize,
        int firstGlyphId,
        int lastGlyphId,
        SfntBitmapGlyphLocator[] locators,
        SfntBitmapDataTable dataTable,
        SfntBitmapGlyphMetrics metrics,
        SfntBitmapParsingBudget budget)
    {
        if (imageSize == 0)
        {
            throw FontFormatException.Create("An embedded bitmap image size is zero.");
        }
        int glyphCount = ConvertUInt32(reader.ReadUInt32(), "sparse bitmap glyph count");
        budget.AddEntries(glyphCount);
        int previousGlyphId = -1;
        int index = 0;
        while (index < glyphCount)
        {
            int glyphId = reader.ReadUInt16();
            if (glyphId < firstGlyphId || glyphId > lastGlyphId ||
                glyphId <= previousGlyphId)
            {
                throw FontFormatException.Create(
                    "Sparse embedded bitmap glyph IDs are invalid.");
            }
            AddLocator(
                locators, glyphId, imageFormat,
                imageDataOffset + (long)index * imageSize,
                imageSize, metrics, dataTable);
            previousGlyphId = glyphId;
            index += 1;
        }
    }

    static void AddLocator(
        SfntBitmapGlyphLocator[] locators,
        int glyphId,
        int imageFormat,
        long offset,
        long length,
        SfntBitmapGlyphMetrics? metrics,
        SfntBitmapDataTable dataTable)
    {
        long end = offset + length;
        if (offset < 4 || offset > int.MaxValue || length < 0 ||
            length > int.MaxValue || end > dataTable.GetLength())
        {
            throw FontFormatException.Create(
                "An embedded bitmap glyph-data range is outside the data table.");
        }
        if (locators[glyphId] != null)
        {
            throw FontFormatException.Create(
                "An embedded bitmap glyph is indexed more than once in a strike.");
        }
        locators[glyphId] = new SfntBitmapGlyphLocator(
            glyphId, imageFormat, (int)offset, (int)length, metrics);
    }

    static SfntBitmapGlyphData ParseGlyphData(
        SfntBitmapDataTable dataTable,
        SfntBitmapGlyphLocator locator,
        bool horizontalMetrics,
        bool color,
        int glyphCount,
        SfntBitmapParsingBudget budget)
    {
        FontDataSlice glyphData = dataTable.GetSlice(
            locator.GetOffset(), locator.GetLength());
        BigEndianReader reader = new BigEndianReader(glyphData);
        int format = locator.GetImageFormat();
        SfntBitmapGlyphMetrics? metrics = null;
        byte[] payload;
        SfntBitmapComponent[] components = new SfntBitmapComponent[0];
        bool supported = true;
        bool png = false;

        if (format == 1 || format == 2)
        {
            RequireLength(glyphData, 5);
            metrics = ReadSmallMetrics(reader, horizontalMetrics);
            payload = CopyRemaining(glyphData, reader.GetPosition());
        }
        else if (format == 5)
        {
            metrics = RequireSharedMetrics(locator);
            payload = glyphData.CopyBytes();
        }
        else if (format == 6 || format == 7)
        {
            RequireLength(glyphData, 8);
            metrics = ReadBigMetrics(reader);
            payload = CopyRemaining(glyphData, reader.GetPosition());
        }
        else if (format == 8 || format == 9)
        {
            int minimumLength = format == 8 ? 8 : 10;
            RequireLength(glyphData, minimumLength);
            metrics = format == 8 ?
                ReadSmallMetrics(reader, horizontalMetrics) : ReadBigMetrics(reader);
            if (format == 8) { reader.ReadUInt8(); }
            int componentCount = reader.ReadUInt16();
            budget.AddEntries(componentCount);
            components = ReadComponents(reader, componentCount, glyphCount);
            payload = new byte[0];
        }
        else if (color && (format == 17 || format == 18 || format == 19))
        {
            if (format == 17)
            {
                RequireLength(glyphData, 9);
                metrics = ReadSmallMetrics(reader, horizontalMetrics);
            }
            else if (format == 18)
            {
                RequireLength(glyphData, 12);
                metrics = ReadBigMetrics(reader);
            }
            else
            {
                RequireLength(glyphData, 4);
                metrics = RequireSharedMetrics(locator);
            }
            int pngLength = ConvertUInt32(reader.ReadUInt32(), "PNG data length");
            if (pngLength > reader.GetRemainingLength())
            {
                throw FontFormatException.Create("Embedded bitmap PNG data is truncated.");
            }
            payload = glyphData.GetSlice(reader.GetPosition(), pngLength).CopyBytes();
            ValidatePng(payload);
            png = true;
        }
        else
        {
            metrics = locator.GetSharedMetrics();
            payload = glyphData.CopyBytes();
            supported = false;
        }

        return new SfntBitmapGlyphData(
            locator.GetGlyphId(), format, supported, png,
            locator.GetOffset(), locator.GetLength(), metrics,
            payload, components);
    }

    static SfntBitmapComponent[] ReadComponents(
        BigEndianReader reader,
        int count,
        int glyphCount)
    {
        long requiredLength = (long)count * 4;
        if (requiredLength > reader.GetRemainingLength())
        {
            throw FontFormatException.Create(
                "An embedded bitmap component array is truncated.");
        }
        SfntBitmapComponent[] components = new SfntBitmapComponent[count];
        int index = 0;
        while (index < count)
        {
            int glyphId = reader.ReadUInt16();
            int xOffset = reader.ReadInt8();
            int yOffset = reader.ReadInt8();
            if (glyphId >= glyphCount)
            {
                throw FontFormatException.Create(
                    "An embedded bitmap component references a missing glyph.");
            }
            components[index] = new SfntBitmapComponent(
                glyphId, xOffset, yOffset);
            index += 1;
        }
        return components;
    }

    static SfntBitmapGlyphMetrics RequireSharedMetrics(
        SfntBitmapGlyphLocator locator)
    {
        SfntBitmapGlyphMetrics? metrics = locator.GetSharedMetrics();
        if (metrics == null)
        {
            throw FontFormatException.Create(
                "An embedded bitmap image format requires shared index metrics.");
        }
        return metrics;
    }

    internal static SfntBitmapLineMetrics ReadLineMetrics(BigEndianReader reader)
    {
        int ascender = reader.ReadInt8();
        int descender = reader.ReadInt8();
        int widthMax = reader.ReadUInt8();
        int caretSlopeNumerator = reader.ReadInt8();
        int caretSlopeDenominator = reader.ReadInt8();
        int caretOffset = reader.ReadInt8();
        int minOrigin = reader.ReadInt8();
        int minAdvance = reader.ReadInt8();
        int maxBefore = reader.ReadInt8();
        int minAfter = reader.ReadInt8();
        reader.ReadInt8();
        reader.ReadInt8();
        return new SfntBitmapLineMetrics(
            ascender, descender, widthMax, caretSlopeNumerator,
            caretSlopeDenominator, caretOffset, minOrigin, minAdvance,
            maxBefore, minAfter);
    }

    static SfntBitmapGlyphMetrics ReadSmallMetrics(
        BigEndianReader reader,
        bool horizontal)
    {
        return new SfntBitmapGlyphMetrics(
            false, horizontal, reader.ReadUInt8(), reader.ReadUInt8(),
            reader.ReadInt8(), reader.ReadInt8(), reader.ReadUInt8(),
            0, 0, 0);
    }

    static SfntBitmapGlyphMetrics ReadBigMetrics(BigEndianReader reader)
    {
        return new SfntBitmapGlyphMetrics(
            true, true, reader.ReadUInt8(), reader.ReadUInt8(),
            reader.ReadInt8(), reader.ReadInt8(), reader.ReadUInt8(),
            reader.ReadInt8(), reader.ReadInt8(), reader.ReadUInt8());
    }

    static void ValidatePng(byte[] data)
    {
        if (data.Length < 8 || data[0] != 0x89 || data[1] != 0x50 ||
            data[2] != 0x4E || data[3] != 0x47 || data[4] != 0x0D ||
            data[5] != 0x0A || data[6] != 0x1A || data[7] != 0x0A)
        {
            throw FontFormatException.Create(
                "An embedded bitmap PNG payload has an invalid signature.");
        }
    }

    static byte[] CopyRemaining(FontDataSlice data, int offset)
    {
        return data.GetSlice(offset, data.GetLength() - offset).CopyBytes();
    }

    static void RequireLength(FontDataSlice data, int length)
    {
        if (data.GetLength() < length)
        {
            throw FontFormatException.Create(
                "An embedded bitmap glyph-data record is truncated.");
        }
    }

    static int ConvertUInt32(uint value, string fieldName)
    {
        if (value > int.MaxValue)
        {
            throw FontFormatException.Create("An embedded " + fieldName + " is too large.");
        }
        return (int)value;
    }

    static bool IsValidBitDepth(int bitDepth, bool color)
    {
        return bitDepth == 1 || bitDepth == 2 || bitDepth == 4 ||
            bitDepth == 8 || (color && bitDepth == 32);
    }
}
