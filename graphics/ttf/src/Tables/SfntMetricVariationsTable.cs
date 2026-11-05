namespace ttf;

internal class SfntMetricVariationsTable
{
    int tag;
    int majorVersion;
    int minorVersion;
    int itemVariationStoreOffset;
    int advanceMappingOffset;
    int leadingBearingMappingOffset;
    int trailingBearingMappingOffset;
    int verticalOriginMappingOffset;
    int glyphCount;
    SfntItemVariationStore variationStore;
    SfntDeltaSetIndexMap? advanceMapping;
    SfntDeltaSetIndexMap? leadingBearingMapping;
    SfntDeltaSetIndexMap? trailingBearingMapping;
    SfntDeltaSetIndexMap? verticalOriginMapping;

    internal SfntMetricVariationsTable(
        int tag,
        int majorVersion,
        int minorVersion,
        int itemVariationStoreOffset,
        int advanceMappingOffset,
        int leadingBearingMappingOffset,
        int trailingBearingMappingOffset,
        int verticalOriginMappingOffset,
        int glyphCount,
        SfntItemVariationStore variationStore,
        SfntDeltaSetIndexMap? advanceMapping,
        SfntDeltaSetIndexMap? leadingBearingMapping,
        SfntDeltaSetIndexMap? trailingBearingMapping,
        SfntDeltaSetIndexMap? verticalOriginMapping)
    {
        this.tag = tag;
        this.majorVersion = majorVersion;
        this.minorVersion = minorVersion;
        this.itemVariationStoreOffset = itemVariationStoreOffset;
        this.advanceMappingOffset = advanceMappingOffset;
        this.leadingBearingMappingOffset = leadingBearingMappingOffset;
        this.trailingBearingMappingOffset = trailingBearingMappingOffset;
        this.verticalOriginMappingOffset = verticalOriginMappingOffset;
        this.glyphCount = glyphCount;
        this.variationStore = variationStore;
        this.advanceMapping = advanceMapping;
        this.leadingBearingMapping = leadingBearingMapping;
        this.trailingBearingMapping = trailingBearingMapping;
        this.verticalOriginMapping = verticalOriginMapping;
    }

    internal int GetTag()
    {
        return tag;
    }

    internal int GetMajorVersion()
    {
        return majorVersion;
    }

    internal int GetMinorVersion()
    {
        return minorVersion;
    }

    internal int GetItemVariationStoreOffset()
    {
        return itemVariationStoreOffset;
    }

    internal int GetAdvanceMappingOffset()
    {
        return advanceMappingOffset;
    }

    internal int GetLeadingBearingMappingOffset()
    {
        return leadingBearingMappingOffset;
    }

    internal int GetTrailingBearingMappingOffset()
    {
        return trailingBearingMappingOffset;
    }

    internal int GetVerticalOriginMappingOffset()
    {
        return verticalOriginMappingOffset;
    }

    internal SfntItemVariationStore GetVariationStore()
    {
        return variationStore;
    }

    internal SfntDeltaSetIndexMap? GetAdvanceMapping()
    {
        return advanceMapping;
    }

    internal SfntDeltaSetIndexMap? GetLeadingBearingMapping()
    {
        return leadingBearingMapping;
    }

    internal SfntDeltaSetIndexMap? GetTrailingBearingMapping()
    {
        return trailingBearingMapping;
    }

    internal SfntDeltaSetIndexMap? GetVerticalOriginMapping()
    {
        return verticalOriginMapping;
    }

    internal SfntMetricVariationsLocation CreateLocation(
        double[] normalizedCoordinates)
    {
        return new SfntMetricVariationsLocation(
            this,
            variationStore.CreateLocation(normalizedCoordinates));
    }

    internal SfntDeltaSetIndex ResolveAdvanceIndex(int glyphId)
    {
        ValidateGlyphId(glyphId);
        return advanceMapping == null ?
            new SfntDeltaSetIndex(0, glyphId) :
            advanceMapping.Resolve(glyphId);
    }

    internal SfntDeltaSetIndex? ResolveLeadingBearingIndex(int glyphId)
    {
        ValidateGlyphId(glyphId);
        return leadingBearingMapping == null ? null :
            leadingBearingMapping.Resolve(glyphId);
    }

    internal SfntDeltaSetIndex? ResolveTrailingBearingIndex(int glyphId)
    {
        ValidateGlyphId(glyphId);
        return trailingBearingMapping == null ? null :
            trailingBearingMapping.Resolve(glyphId);
    }

    internal SfntDeltaSetIndex? ResolveVerticalOriginIndex(int glyphId)
    {
        ValidateGlyphId(glyphId);
        return verticalOriginMapping == null ? null :
            verticalOriginMapping.Resolve(glyphId);
    }

    void ValidateGlyphId(int glyphId)
    {
        if (glyphId < 0 || glyphId >= glyphCount)
        {
            throw new System.ArgumentOutOfRangeException("glyphId");
        }
    }
}

internal class SfntMetricVariationsLocation
{
    SfntMetricVariationsTable table;
    SfntVariationLocation location;

    internal SfntMetricVariationsLocation(
        SfntMetricVariationsTable table,
        SfntVariationLocation location)
    {
        this.table = table;
        this.location = location;
    }

    internal double CalculateAdvanceDelta(int glyphId)
    {
        return location.CalculateDelta(table.ResolveAdvanceIndex(glyphId));
    }

    internal double CalculateLeadingBearingDelta(int glyphId)
    {
        return CalculateOptionalDelta(
            table.ResolveLeadingBearingIndex(glyphId));
    }

    internal bool HasLeadingBearingVariations()
    {
        return table.GetLeadingBearingMapping() != null;
    }

    internal double CalculateTrailingBearingDelta(int glyphId)
    {
        return CalculateOptionalDelta(
            table.ResolveTrailingBearingIndex(glyphId));
    }

    internal double CalculateVerticalOriginDelta(int glyphId)
    {
        return CalculateOptionalDelta(
            table.ResolveVerticalOriginIndex(glyphId));
    }

    internal bool HasVerticalOriginVariations()
    {
        return table.GetVerticalOriginMapping() != null;
    }

    double CalculateOptionalDelta(SfntDeltaSetIndex? index)
    {
        return index == null ? 0 : location.CalculateDelta(index);
    }
}

internal static class SfntMetricVariationsTableParser
{
    const int HvarHeaderLength = 20;
    const int VvarHeaderLength = 24;
    static readonly SfntTableParser hvarParser = new SfntTableParser(
        FontTags.Hvar,
        1,
        1,
        0,
        ReadVersion,
        ParseHvar);
    static readonly SfntTableParser vvarParser = new SfntTableParser(
        FontTags.Vvar,
        1,
        1,
        0,
        ReadVersion,
        ParseVvar);

    internal static SfntTableParser GetHvarParser()
    {
        return hvarParser;
    }

    internal static SfntTableParser GetVvarParser()
    {
        return vvarParser;
    }

    internal static SfntParsedTableView? FindHvarView(SfntTableCache cache)
    {
        return FindView(cache, hvarParser);
    }

    internal static SfntMetricVariationsTable? FindHvarTable(
        SfntTableCache cache)
    {
        return FindTable(cache, hvarParser);
    }

    internal static SfntParsedTableView? FindVvarView(SfntTableCache cache)
    {
        return FindView(cache, vvarParser);
    }

    internal static SfntMetricVariationsTable? FindVvarTable(
        SfntTableCache cache)
    {
        return FindTable(cache, vvarParser);
    }

    static SfntParsedTableView? FindView(
        SfntTableCache cache,
        SfntTableParser parser)
    {
        if (cache == null)
        {
            throw new System.ArgumentNullException("cache");
        }

        return cache.FindParsedTable(parser);
    }

    static SfntMetricVariationsTable? FindTable(
        SfntTableCache cache,
        SfntTableParser parser)
    {
        SfntParsedTableView? view = FindView(cache, parser);

        if (view == null || !view.HasParsedData())
        {
            return null;
        }

        return (SfntMetricVariationsTable)view.GetParsedData();
    }

    static SfntTableVersion ReadVersion(SfntTableData table)
    {
        BigEndianReader reader = new BigEndianReader(table.GetDataSlice());
        return new SfntTableVersion(reader.ReadUInt16(), reader.ReadUInt16());
    }

    static object ParseHvar(
        SfntTableData table,
        SfntTableVersion version,
        SfntTableVersionSupport support,
        FontLimits limits,
        SfntTableCache cache)
    {
        return Parse(table, version, limits, cache, false);
    }

    static object ParseVvar(
        SfntTableData table,
        SfntTableVersion version,
        SfntTableVersionSupport support,
        FontLimits limits,
        SfntTableCache cache)
    {
        return Parse(table, version, limits, cache, true);
    }

    static SfntMetricVariationsTable Parse(
        SfntTableData table,
        SfntTableVersion version,
        FontLimits limits,
        SfntTableCache cache,
        bool vertical)
    {
        int headerLength = vertical ? VvarHeaderLength : HvarHeaderLength;
        FontDataSlice tableData = table.GetDataSlice();

        if (tableData.GetLength() < headerLength)
        {
            throw FontFormatException.Create(
                "A metric variations table header is truncated.");
        }

        BigEndianReader reader = new BigEndianReader(tableData);
        int majorVersion = reader.ReadUInt16();
        int minorVersion = reader.ReadUInt16();

        if (majorVersion != version.GetMajorVersion() ||
            minorVersion != version.GetMinorVersion())
        {
            throw FontFormatException.Create(
                "A metric variations table version is inconsistent.");
        }

        int storeOffset = ReadOffset(
            reader,
            false,
            tableData,
            headerLength,
            "The metric variation-store offset is invalid.");
        int advanceOffset = ReadOffset(
            reader,
            true,
            tableData,
            headerLength,
            "The metric advance-map offset is invalid.");
        int leadingOffset = ReadOffset(
            reader,
            true,
            tableData,
            headerLength,
            "The metric leading-bearing-map offset is invalid.");
        int trailingOffset = ReadOffset(
            reader,
            true,
            tableData,
            headerLength,
            "The metric trailing-bearing-map offset is invalid.");
        int originOffset = vertical ? ReadOffset(
            reader,
            true,
            tableData,
            headerLength,
            "The vertical-origin-map offset is invalid.") : 0;
        SfntFvarTable? fvar = SfntFvarTableParser.FindTable(cache);

        if (fvar == null)
        {
            throw FontFormatException.Create(
                "A metric variations table requires a supported fvar table.");
        }

        SfntMaxpTable? maxp = SfntMaxpTableParser.FindTable(cache);

        if (maxp == null)
        {
            throw FontFormatException.Create(
                "A metric variations table requires a supported maxp table.");
        }

        if (vertical)
        {
            if (SfntVmtxTableParser.FindTable(cache) == null)
            {
                throw FontFormatException.Create(
                    "The VVAR table requires supported vhea and vmtx tables.");
            }
        }
        else if (SfntHmtxTableParser.FindTable(cache) == null)
        {
            throw FontFormatException.Create(
                "The HVAR table requires supported hhea and hmtx tables.");
        }

        if ((leadingOffset == 0) != (trailingOffset == 0))
        {
            throw FontFormatException.Create(
                "Metric side-bearing maps must be supplied as a pair.");
        }

        SfntVariationParsingBudget budget = new SfntVariationParsingBudget(
            limits.GetMaxCoverageEntries());
        SfntItemVariationStore store = SfntItemVariationStoreParser.Parse(
            tableData,
            storeOffset,
            headerLength,
            fvar.GetAxisCount(),
            limits,
            budget);
        SfntDeltaSetIndexMap? advanceMap = ParseMap(
            tableData,
            advanceOffset,
            headerLength,
            maxp.GetGlyphCount(),
            store,
            budget);
        SfntDeltaSetIndexMap? leadingMap = ParseMap(
            tableData,
            leadingOffset,
            headerLength,
            maxp.GetGlyphCount(),
            store,
            budget);
        SfntDeltaSetIndexMap? trailingMap = ParseMap(
            tableData,
            trailingOffset,
            headerLength,
            maxp.GetGlyphCount(),
            store,
            budget);
        SfntDeltaSetIndexMap? originMap = ParseMap(
            tableData,
            originOffset,
            headerLength,
            maxp.GetGlyphCount(),
            store,
            budget);

        if (advanceMap == null && maxp.GetGlyphCount() > 0)
        {
            store.ValidateDeltaSetIndex(new SfntDeltaSetIndex(
                0,
                maxp.GetGlyphCount() - 1));
        }

        return new SfntMetricVariationsTable(
            vertical ? FontTags.Vvar : FontTags.Hvar,
            majorVersion,
            minorVersion,
            storeOffset,
            advanceOffset,
            leadingOffset,
            trailingOffset,
            originOffset,
            maxp.GetGlyphCount(),
            store,
            advanceMap,
            leadingMap,
            trailingMap,
            originMap);
    }

    static SfntDeltaSetIndexMap? ParseMap(
        FontDataSlice tableData,
        int offset,
        int minimumOffset,
        int glyphCount,
        SfntItemVariationStore store,
        SfntVariationParsingBudget budget)
    {
        if (offset == 0)
        {
            return null;
        }

        SfntDeltaSetIndexMap map = SfntDeltaSetIndexMapParser.Parse(
            tableData,
            offset,
            minimumOffset,
            budget);

        if (map.GetFormat() != 0)
        {
            throw FontFormatException.Create(
                "HVAR and VVAR require DeltaSetIndexMap format 0.");
        }

        if (map.GetEntryCount() == 0 || map.GetEntryCount() > glyphCount)
        {
            throw FontFormatException.Create(
                "A metric delta-set index map has an invalid entry count.");
        }

        int index = 0;

        while (index < map.GetEntryCount())
        {
            store.ValidateDeltaSetIndex(map.GetEntry(index));
            index += 1;
        }

        return map;
    }

    static int ReadOffset(
        BigEndianReader reader,
        bool nullable,
        FontDataSlice tableData,
        int minimumOffset,
        string message)
    {
        long encoded = reader.ReadOffset32();

        if (encoded == 0 && nullable)
        {
            return 0;
        }

        if (encoded > int.MaxValue ||
            encoded < minimumOffset ||
            encoded > tableData.GetLength() - 4)
        {
            throw FontFormatException.Create(message);
        }

        return (int)encoded;
    }
}
