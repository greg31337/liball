namespace ttf;

internal class SfntVheaTable
{
    int majorVersion;
    int minorVersion;
    int ascender;
    int descender;
    int lineGap;
    int advanceHeightMaximum;
    int minimumTopSideBearing;
    int minimumBottomSideBearing;
    int maximumYExtent;
    int caretSlopeRise;
    int caretSlopeRun;
    int caretOffset;
    int metricDataFormat;
    int metricCount;
    int extensionLength;

    internal SfntVheaTable(
        int majorVersion,
        int minorVersion,
        int ascender,
        int descender,
        int lineGap,
        int advanceHeightMaximum,
        int minimumTopSideBearing,
        int minimumBottomSideBearing,
        int maximumYExtent,
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
        this.advanceHeightMaximum = advanceHeightMaximum;
        this.minimumTopSideBearing = minimumTopSideBearing;
        this.minimumBottomSideBearing = minimumBottomSideBearing;
        this.maximumYExtent = maximumYExtent;
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

    internal bool UsesTypographicFieldDefinitions()
    {
        return minorVersion >= 0x1000;
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

    internal int GetAdvanceHeightMaximum()
    {
        return advanceHeightMaximum;
    }

    internal int GetMinimumTopSideBearing()
    {
        return minimumTopSideBearing;
    }

    internal int GetMinimumBottomSideBearing()
    {
        return minimumBottomSideBearing;
    }

    internal int GetMaximumYExtent()
    {
        return maximumYExtent;
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

internal static class SfntVheaTableParser
{
    const int RequiredLength = 36;
    const int Version11Minor = 0x1000;
    static readonly SfntTableParser parser = new SfntTableParser(
        FontTags.Vhea,
        1,
        1,
        Version11Minor,
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

    internal static SfntVheaTable? FindTable(SfntTableCache cache)
    {
        SfntParsedTableView? view = FindView(cache);

        if (view == null || !view.HasParsedData())
        {
            return null;
        }

        return (SfntVheaTable)view.GetParsedData();
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
            throw FontFormatException.Create("The vhea table is truncated.");
        }

        int minorVersion = version.GetMinorVersion();

        if (support == SfntTableVersionSupport.Supported &&
            minorVersion != 0 &&
            minorVersion != Version11Minor)
        {
            throw FontFormatException.Create("The vhea table version is invalid.");
        }

        if (support == SfntTableVersionSupport.Supported &&
            tableLength != RequiredLength)
        {
            throw FontFormatException.Create("The supported vhea table length is invalid.");
        }

        BigEndianReader reader = new BigEndianReader(table.GetDataSlice());
        int majorVersion = reader.ReadUInt16();
        minorVersion = reader.ReadUInt16();
        int ascender = reader.ReadInt16();
        int descender = reader.ReadInt16();
        int lineGap = reader.ReadInt16();
        int advanceHeightMaximum = reader.ReadUInt16();
        int minimumTopSideBearing = reader.ReadInt16();
        int minimumBottomSideBearing = reader.ReadInt16();
        int maximumYExtent = reader.ReadInt16();
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
            throw FontFormatException.Create("A reserved vhea field is nonzero.");
        }

        if (metricDataFormat != 0)
        {
            throw FontFormatException.Create("The vhea metricDataFormat value is invalid.");
        }

        if (metricCount == 0)
        {
            throw FontFormatException.Create("The vhea table declares no vertical metrics.");
        }

        return new SfntVheaTable(
            majorVersion,
            minorVersion,
            ascender,
            descender,
            lineGap,
            advanceHeightMaximum,
            minimumTopSideBearing,
            minimumBottomSideBearing,
            maximumYExtent,
            caretSlopeRise,
            caretSlopeRun,
            caretOffset,
            metricDataFormat,
            metricCount,
            tableLength - RequiredLength);
    }
}

internal class SfntVmtxTable
{
    int glyphCount;
    int metricCount;
    int[] advanceHeights;
    int[] metricTopSideBearings;
    int[] trailingTopSideBearings;

    internal SfntVmtxTable(
        int glyphCount,
        int metricCount,
        int[] advanceHeights,
        int[] metricTopSideBearings,
        int[] trailingTopSideBearings)
    {
        this.glyphCount = glyphCount;
        this.metricCount = metricCount;
        this.advanceHeights = advanceHeights;
        this.metricTopSideBearings = metricTopSideBearings;
        this.trailingTopSideBearings = trailingTopSideBearings;
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
        return trailingTopSideBearings.Length;
    }

    internal int GetAdvanceHeight(int glyphId)
    {
        ValidateGlyphId(glyphId);
        int metricIndex = glyphId < metricCount ? glyphId : metricCount - 1;
        return advanceHeights[metricIndex];
    }

    internal int GetTopSideBearing(int glyphId)
    {
        ValidateGlyphId(glyphId);

        if (glyphId < metricCount)
        {
            return metricTopSideBearings[glyphId];
        }

        return trailingTopSideBearings[glyphId - metricCount];
    }

    internal bool UsesSharedAdvanceHeight(int glyphId)
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

internal static class SfntVmtxTableParser
{
    static readonly SfntTableParser parser = new SfntTableParser(
        FontTags.Vmtx,
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

    internal static SfntVmtxTable? FindTable(SfntTableCache cache)
    {
        SfntParsedTableView? view = FindView(cache);

        if (view == null || !view.HasParsedData())
        {
            return null;
        }

        return (SfntVmtxTable)view.GetParsedData();
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
        SfntVheaTable? vhea = SfntVheaTableParser.FindTable(cache);
        SfntMaxpTable? maxp = SfntMaxpTableParser.FindTable(cache);

        if (vhea == null || maxp == null)
        {
            throw FontFormatException.Create(
                "The vmtx table requires supported vhea and maxp tables.");
        }

        int glyphCount = maxp.GetGlyphCount();
        int metricCount = vhea.GetMetricCount();

        if (metricCount > glyphCount)
        {
            throw FontFormatException.Create(
                "The vhea vertical metric count exceeds the maxp glyph count.");
        }

        long expectedLength = metricCount * 4L +
            (glyphCount - metricCount) * 2L;

        if (table.GetLength() != expectedLength)
        {
            throw FontFormatException.Create(
                "The vmtx table length is inconsistent with vhea and maxp.");
        }

        BigEndianReader reader = new BigEndianReader(table.GetDataSlice());
        int[] advanceHeights = new int[metricCount];
        int[] metricBearings = new int[metricCount];
        int metricIndex = 0;
        int maximumAdvanceHeight = 0;

        while (metricIndex < metricCount)
        {
            int advanceHeight = reader.ReadUInt16();
            advanceHeights[metricIndex] = advanceHeight;
            metricBearings[metricIndex] = reader.ReadInt16();

            if (advanceHeight > maximumAdvanceHeight)
            {
                maximumAdvanceHeight = advanceHeight;
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

        if (maximumAdvanceHeight != vhea.GetAdvanceHeightMaximum())
        {
            throw FontFormatException.Create(
                "The vhea maximum advance height does not match vmtx.");
        }

        return new SfntVmtxTable(
            glyphCount,
            metricCount,
            advanceHeights,
            metricBearings,
            trailingBearings);
    }
}

internal class SfntVorgTable
{
    int majorVersion;
    int minorVersion;
    int defaultVerticalOriginY;
    int[] glyphIds;
    int[] verticalOriginsY;
    int extensionLength;
    int glyphCount;

    internal SfntVorgTable(
        int majorVersion,
        int minorVersion,
        int defaultVerticalOriginY,
        int[] glyphIds,
        int[] verticalOriginsY,
        int extensionLength,
        int glyphCount)
    {
        this.majorVersion = majorVersion;
        this.minorVersion = minorVersion;
        this.defaultVerticalOriginY = defaultVerticalOriginY;
        this.glyphIds = glyphIds;
        this.verticalOriginsY = verticalOriginsY;
        this.extensionLength = extensionLength;
        this.glyphCount = glyphCount;
    }

    internal int GetMajorVersion()
    {
        return majorVersion;
    }

    internal int GetMinorVersion()
    {
        return minorVersion;
    }

    internal int GetDefaultVerticalOriginY()
    {
        return defaultVerticalOriginY;
    }

    internal int GetMetricCount()
    {
        return glyphIds.Length;
    }

    internal int GetMetricGlyphId(int index)
    {
        ValidateMetricIndex(index);
        return glyphIds[index];
    }

    internal int GetMetricVerticalOriginY(int index)
    {
        ValidateMetricIndex(index);
        return verticalOriginsY[index];
    }

    internal bool HasGlyphOverride(int glyphId)
    {
        ValidateGlyphId(glyphId);
        return FindGlyphIndex(glyphId) >= 0;
    }

    internal int GetVerticalOriginY(int glyphId)
    {
        ValidateGlyphId(glyphId);
        int index = FindGlyphIndex(glyphId);

        if (index < 0)
        {
            return defaultVerticalOriginY;
        }

        return verticalOriginsY[index];
    }

    internal int GetExtensionLength()
    {
        return extensionLength;
    }

    int FindGlyphIndex(int glyphId)
    {
        int low = 0;
        int high = glyphIds.Length - 1;

        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            int candidate = glyphIds[middle];

            if (candidate == glyphId)
            {
                return middle;
            }

            if (candidate < glyphId)
            {
                low = middle + 1;
            }
            else
            {
                high = middle - 1;
            }
        }

        return -1;
    }

    void ValidateMetricIndex(int index)
    {
        if (index < 0 || index >= glyphIds.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }
    }

    void ValidateGlyphId(int glyphId)
    {
        if (glyphId < 0 || glyphId >= glyphCount)
        {
            throw new System.ArgumentOutOfRangeException("glyphId");
        }
    }
}

internal static class SfntVorgTableParser
{
    const int HeaderLength = 8;
    const int RecordLength = 4;
    static readonly SfntTableParser parser = new SfntTableParser(
        FontTags.Vorg,
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

    internal static SfntVorgTable? FindTable(SfntTableCache cache)
    {
        SfntParsedTableView? view = FindView(cache);

        if (view == null || !view.HasParsedData())
        {
            return null;
        }

        return (SfntVorgTable)view.GetParsedData();
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
        if (table.GetLength() < HeaderLength)
        {
            throw FontFormatException.Create("The VORG table is truncated.");
        }

        SfntMaxpTable? maxp = SfntMaxpTableParser.FindTable(cache);

        if (maxp == null)
        {
            throw FontFormatException.Create("The VORG table requires a supported maxp table.");
        }

        BigEndianReader reader = new BigEndianReader(table.GetDataSlice());
        int majorVersion = reader.ReadUInt16();
        int minorVersion = reader.ReadUInt16();
        int defaultVerticalOriginY = reader.ReadInt16();
        int metricCount = reader.ReadUInt16();
        long requiredLength = HeaderLength + metricCount * (long)RecordLength;

        if (requiredLength > table.GetLength())
        {
            throw FontFormatException.Create("The VORG metric array is truncated.");
        }

        if (support == SfntTableVersionSupport.Supported &&
            requiredLength != table.GetLength())
        {
            throw FontFormatException.Create("The VORG version 1.0 table length is invalid.");
        }

        if (metricCount > maxp.GetGlyphCount())
        {
            throw FontFormatException.Create(
                "The VORG metric count exceeds the maxp glyph count.");
        }

        int[] glyphIds = new int[metricCount];
        int[] verticalOriginsY = new int[metricCount];
        int index = 0;
        int previousGlyphId = -1;

        while (index < metricCount)
        {
            int glyphId = reader.ReadUInt16();

            if (glyphId <= previousGlyphId)
            {
                throw FontFormatException.Create(
                    "The VORG metrics are not sorted by unique glyph ID.");
            }

            if (glyphId >= maxp.GetGlyphCount())
            {
                throw FontFormatException.Create(
                    "A VORG metric glyph ID exceeds the maxp glyph count.");
            }

            glyphIds[index] = glyphId;
            verticalOriginsY[index] = reader.ReadInt16();
            previousGlyphId = glyphId;
            index += 1;
        }

        return new SfntVorgTable(
            majorVersion,
            minorVersion,
            defaultVerticalOriginY,
            glyphIds,
            verticalOriginsY,
            table.GetLength() - (int)requiredLength,
            maxp.GetGlyphCount());
    }
}

internal class SfntVerticalMetrics
{
    int glyphCount;
    SfntVmtxTable? metrics;
    SfntVorgTable? origins;
    int fallbackAdvanceHeight;
    int fallbackVerticalOriginY;

    internal SfntVerticalMetrics(
        int glyphCount,
        SfntVmtxTable? metrics,
        SfntVorgTable? origins,
        int fallbackAdvanceHeight,
        int fallbackVerticalOriginY)
    {
        this.glyphCount = glyphCount;
        this.metrics = metrics;
        this.origins = origins;
        this.fallbackAdvanceHeight = fallbackAdvanceHeight;
        this.fallbackVerticalOriginY = fallbackVerticalOriginY;
    }

    internal int GetGlyphCount()
    {
        return glyphCount;
    }

    internal bool UsesSynthesizedAdvanceHeights()
    {
        return metrics == null;
    }

    internal bool UsesVorgOrigins()
    {
        return origins != null;
    }

    internal int GetAdvanceHeight(int glyphId)
    {
        ValidateGlyphId(glyphId);

        if (metrics == null)
        {
            return fallbackAdvanceHeight;
        }

        return metrics.GetAdvanceHeight(glyphId);
    }

    internal int GetTopSideBearing(int glyphId, int glyphYMaximum)
    {
        ValidateGlyphId(glyphId);

        if (origins != null)
        {
            return origins.GetVerticalOriginY(glyphId) - glyphYMaximum;
        }

        if (metrics != null)
        {
            return metrics.GetTopSideBearing(glyphId);
        }

        return fallbackVerticalOriginY - glyphYMaximum;
    }

    internal int GetVerticalOriginY(int glyphId, int glyphYMaximum)
    {
        ValidateGlyphId(glyphId);

        if (origins != null)
        {
            return origins.GetVerticalOriginY(glyphId);
        }

        if (metrics != null)
        {
            return glyphYMaximum + metrics.GetTopSideBearing(glyphId);
        }

        return fallbackVerticalOriginY;
    }

    void ValidateGlyphId(int glyphId)
    {
        if (glyphId < 0 || glyphId >= glyphCount)
        {
            throw new System.ArgumentOutOfRangeException("glyphId");
        }
    }
}

internal static class SfntVerticalMetricsResolver
{
    internal static SfntVerticalMetrics Create(
        SfntTableCache cache,
        FontOutlineKind outlineKind)
    {
        if (cache == null)
        {
            throw new System.ArgumentNullException("cache");
        }

        SfntMaxpTable? maxp = SfntMaxpTableParser.FindTable(cache);
        SfntHeadTable? head = SfntHeadTableParser.FindTable(cache);

        if (maxp == null || head == null)
        {
            throw FontFormatException.Create(
                "Vertical metric resolution requires supported head and maxp tables.");
        }

        bool hasVhea = cache.FindRawTable(FontTags.Vhea) != null;
        bool hasVmtx = cache.FindRawTable(FontTags.Vmtx) != null;

        if (hasVhea != hasVmtx)
        {
            throw FontFormatException.Create(
                "The vhea and vmtx tables must either both be present or both be absent.");
        }

        SfntVmtxTable? metrics = null;

        if (hasVmtx)
        {
            metrics = SfntVmtxTableParser.FindTable(cache);

            if (metrics == null)
            {
                throw FontFormatException.Create(
                    "The vertical metric tables use an unsupported version.");
            }
        }

        SfntVorgTable? origins = null;

        if (outlineKind == FontOutlineKind.Cff1 ||
            outlineKind == FontOutlineKind.Cff2)
        {
            origins = SfntVorgTableParser.FindTable(cache);
        }

        return new SfntVerticalMetrics(
            maxp.GetGlyphCount(),
            metrics,
            origins,
            head.GetUnitsPerEm(),
            head.GetYMax());
    }
}
