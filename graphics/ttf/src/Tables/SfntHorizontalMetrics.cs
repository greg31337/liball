namespace ttf;

internal class SfntHheaTable
{
    int majorVersion;
    int minorVersion;
    int ascender;
    int descender;
    int lineGap;
    int advanceWidthMaximum;
    int minimumLeftSideBearing;
    int minimumRightSideBearing;
    int maximumXExtent;
    int caretSlopeRise;
    int caretSlopeRun;
    int caretOffset;
    int metricDataFormat;
    int metricCount;
    int extensionLength;

    internal SfntHheaTable(
        int majorVersion,
        int minorVersion,
        int ascender,
        int descender,
        int lineGap,
        int advanceWidthMaximum,
        int minimumLeftSideBearing,
        int minimumRightSideBearing,
        int maximumXExtent,
        int caretSlopeRise,
        int caretSlopeRun,
        int caretOffset,
        int metricDataFormat,
        int metricCount,
        int extensionLength)
    {
        this.majorVersion = majorVersion;
        this.minorVersion = minorVersion;
        this.ascender = ascender;
        this.descender = descender;
        this.lineGap = lineGap;
        this.advanceWidthMaximum = advanceWidthMaximum;
        this.minimumLeftSideBearing = minimumLeftSideBearing;
        this.minimumRightSideBearing = minimumRightSideBearing;
        this.maximumXExtent = maximumXExtent;
        this.caretSlopeRise = caretSlopeRise;
        this.caretSlopeRun = caretSlopeRun;
        this.caretOffset = caretOffset;
        this.metricDataFormat = metricDataFormat;
        this.metricCount = metricCount;
        this.extensionLength = extensionLength;
    }

    internal int GetMajorVersion()
    {
        return majorVersion;
    }

    internal int GetMinorVersion()
    {
        return minorVersion;
    }

    internal int GetAscender()
    {
        return ascender;
    }

    internal int GetDescender()
    {
        return descender;
    }

    internal int GetLineGap()
    {
        return lineGap;
    }

    internal int GetAdvanceWidthMaximum()
    {
        return advanceWidthMaximum;
    }

    internal int GetMinimumLeftSideBearing()
    {
        return minimumLeftSideBearing;
    }

    internal int GetMinimumRightSideBearing()
    {
        return minimumRightSideBearing;
    }

    internal int GetMaximumXExtent()
    {
        return maximumXExtent;
    }

    internal int GetCaretSlopeRise()
    {
        return caretSlopeRise;
    }

    internal int GetCaretSlopeRun()
    {
        return caretSlopeRun;
    }

    internal int GetCaretOffset()
    {
        return caretOffset;
    }

    internal int GetMetricDataFormat()
    {
        return metricDataFormat;
    }

    internal int GetMetricCount()
    {
        return metricCount;
    }

    internal int GetExtensionLength()
    {
        return extensionLength;
    }
}

internal static class SfntHheaTableParser
{
    const int RequiredLength = 36;
    static readonly SfntTableParser parser = new SfntTableParser(
        FontTags.Hhea,
        1,
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

    internal static SfntHheaTable? FindTable(SfntTableCache cache)
    {
        SfntParsedTableView? view = FindView(cache);

        if (view == null || !view.HasParsedData())
        {
            return null;
        }

        return (SfntHheaTable)view.GetParsedData();
    }

    static SfntTableVersion ReadVersion(SfntTableData table)
    {
        BigEndianReader reader = new BigEndianReader(table.GetDataSlice());
        return new SfntTableVersion(
            reader.ReadUInt16(),
            reader.ReadUInt16());
    }

    static object Parse(
        SfntTableData table,
        SfntTableVersion version,
        SfntTableVersionSupport support,
        FontLimits limits,
        SfntTableCache cache)
    {
        int tableLength = table.GetLength();

        if (tableLength < RequiredLength)
        {
            throw FontFormatException.Create("The hhea table is truncated.");
        }

        if (support == SfntTableVersionSupport.Supported &&
            tableLength != RequiredLength)
        {
            throw FontFormatException.Create("The hhea version 1.0 table length is invalid.");
        }

        BigEndianReader reader = new BigEndianReader(table.GetDataSlice());
        int majorVersion = reader.ReadUInt16();
        int minorVersion = reader.ReadUInt16();
        int ascender = reader.ReadInt16();
        int descender = reader.ReadInt16();
        int lineGap = reader.ReadInt16();
        int advanceWidthMaximum = reader.ReadUInt16();
        int minimumLeftSideBearing = reader.ReadInt16();
        int minimumRightSideBearing = reader.ReadInt16();
        int maximumXExtent = reader.ReadInt16();
        int caretSlopeRise = reader.ReadInt16();
        int caretSlopeRun = reader.ReadInt16();
        int caretOffset = reader.ReadInt16();
        int reserved1 = reader.ReadInt16();
        int reserved2 = reader.ReadInt16();
        int reserved3 = reader.ReadInt16();
        int reserved4 = reader.ReadInt16();
        int metricDataFormat = reader.ReadInt16();
        int metricCount = reader.ReadUInt16();

        if (reserved1 != 0 ||
            reserved2 != 0 ||
            reserved3 != 0 ||
            reserved4 != 0)
        {
            throw FontFormatException.Create("A reserved hhea field is nonzero.");
        }

        if (metricDataFormat != 0)
        {
            throw FontFormatException.Create("The hhea metricDataFormat value is invalid.");
        }

        if (metricCount == 0)
        {
            throw FontFormatException.Create("The hhea table declares no horizontal metrics.");
        }

        return new SfntHheaTable(
            majorVersion,
            minorVersion,
            ascender,
            descender,
            lineGap,
            advanceWidthMaximum,
            minimumLeftSideBearing,
            minimumRightSideBearing,
            maximumXExtent,
            caretSlopeRise,
            caretSlopeRun,
            caretOffset,
            metricDataFormat,
            metricCount,
            tableLength - RequiredLength);
    }
}

internal class SfntHmtxTable
{
    int glyphCount;
    int metricCount;
    int[] advanceWidths;
    int[] metricLeftSideBearings;
    int[] trailingLeftSideBearings;

    internal SfntHmtxTable(
        int glyphCount,
        int metricCount,
        int[] advanceWidths,
        int[] metricLeftSideBearings,
        int[] trailingLeftSideBearings)
    {
        this.glyphCount = glyphCount;
        this.metricCount = metricCount;
        this.advanceWidths = advanceWidths;
        this.metricLeftSideBearings = metricLeftSideBearings;
        this.trailingLeftSideBearings = trailingLeftSideBearings;
    }

    internal int GetGlyphCount()
    {
        return glyphCount;
    }

    internal int GetMetricCount()
    {
        return metricCount;
    }

    internal int GetTrailingBearingCount()
    {
        return trailingLeftSideBearings.Length;
    }

    internal int GetAdvanceWidth(int glyphId)
    {
        ValidateGlyphId(glyphId);
        int metricIndex = glyphId < metricCount ? glyphId : metricCount - 1;
        return advanceWidths[metricIndex];
    }

    internal int GetLeftSideBearing(int glyphId)
    {
        ValidateGlyphId(glyphId);

        if (glyphId < metricCount)
        {
            return metricLeftSideBearings[glyphId];
        }

        return trailingLeftSideBearings[glyphId - metricCount];
    }

    internal bool UsesSharedAdvanceWidth(int glyphId)
    {
        ValidateGlyphId(glyphId);
        return glyphId >= metricCount;
    }

    void ValidateGlyphId(int glyphId)
    {
        if (glyphId < 0 || glyphId >= glyphCount)
        {
            throw new System.ArgumentOutOfRangeException("glyphId");
        }
    }
}

internal static class SfntHmtxTableParser
{
    static readonly SfntTableParser parser = new SfntTableParser(
        FontTags.Hmtx,
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

    internal static SfntHmtxTable? FindTable(SfntTableCache cache)
    {
        SfntParsedTableView? view = FindView(cache);

        if (view == null || !view.HasParsedData())
        {
            return null;
        }

        return (SfntHmtxTable)view.GetParsedData();
    }

    static SfntTableVersion ReadVersion(SfntTableData table)
    {
        return new SfntTableVersion(0, 0);
    }

    static object Parse(
        SfntTableData table,
        SfntTableVersion version,
        SfntTableVersionSupport support,
        FontLimits limits,
        SfntTableCache cache)
    {
        SfntHheaTable? hhea = SfntHheaTableParser.FindTable(cache);
        SfntMaxpTable? maxp = SfntMaxpTableParser.FindTable(cache);

        if (hhea == null || maxp == null)
        {
            throw FontFormatException.Create(
                "The hmtx table requires supported hhea and maxp tables.");
        }

        int glyphCount = maxp.GetGlyphCount();
        int metricCount = hhea.GetMetricCount();

        if (metricCount > glyphCount)
        {
            throw FontFormatException.Create(
                "The hhea horizontal metric count exceeds the maxp glyph count.");
        }

        long expectedLength = metricCount * 4L +
            (glyphCount - metricCount) * 2L;

        if (table.GetLength() != expectedLength)
        {
            throw FontFormatException.Create(
                "The hmtx table length is inconsistent with hhea and maxp.");
        }

        BigEndianReader reader = new BigEndianReader(table.GetDataSlice());
        int[] advanceWidths = new int[metricCount];
        int[] metricBearings = new int[metricCount];
        int metricIndex = 0;
        int maximumAdvanceWidth = 0;

        while (metricIndex < metricCount)
        {
            int advanceWidth = reader.ReadUInt16();
            advanceWidths[metricIndex] = advanceWidth;
            metricBearings[metricIndex] = reader.ReadInt16();

            if (advanceWidth > maximumAdvanceWidth)
            {
                maximumAdvanceWidth = advanceWidth;
            }

            metricIndex += 1;
        }

        int trailingCount = glyphCount - metricCount;
        int[] trailingBearings = new int[trailingCount];
        int trailingIndex = 0;

        while (trailingIndex < trailingCount)
        {
            trailingBearings[trailingIndex] = reader.ReadInt16();
            trailingIndex += 1;
        }

        if (maximumAdvanceWidth != hhea.GetAdvanceWidthMaximum())
        {
            throw FontFormatException.Create(
                "The hhea maximum advance width does not match hmtx.");
        }

        return new SfntHmtxTable(
            glyphCount,
            metricCount,
            advanceWidths,
            metricBearings,
            trailingBearings);
    }
}
