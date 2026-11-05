namespace ttf;

internal class SfntAvarAxisValueMap
{
    double fromCoordinate;
    double toCoordinate;

    internal SfntAvarAxisValueMap(
        double fromCoordinate,
        double toCoordinate)
    {
        this.fromCoordinate = fromCoordinate;
        this.toCoordinate = toCoordinate;
    }

    internal double GetFromCoordinate()
    {
        return fromCoordinate;
    }

    internal double GetToCoordinate()
    {
        return toCoordinate;
    }
}

internal class SfntAvarSegmentMap
{
    int axisTag;
    SfntAvarAxisValueMap[] valueMaps;
    bool hasRequiredMappings;

    internal SfntAvarSegmentMap(
        int axisTag,
        SfntAvarAxisValueMap[] valueMaps,
        bool hasRequiredMappings)
    {
        this.axisTag = axisTag;
        this.valueMaps = valueMaps;
        this.hasRequiredMappings = hasRequiredMappings;
    }

    internal int GetAxisTag()
    {
        return axisTag;
    }

    internal int GetValueMapCount()
    {
        return valueMaps.Length;
    }

    internal SfntAvarAxisValueMap GetValueMap(int index)
    {
        if (index < 0 || index >= valueMaps.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return valueMaps[index];
    }

    internal bool HasRequiredMappings()
    {
        return hasRequiredMappings;
    }
}

internal class SfntAvarTable
{
    int majorVersion;
    int minorVersion;
    SfntAvarSegmentMap[] segmentMaps;
    int axisIndexMapOffset;
    int variationStoreOffset;
    SfntDeltaSetIndexMap? axisIndexMap;
    SfntItemVariationStore? variationStore;

    internal SfntAvarTable(
        int majorVersion,
        int minorVersion,
        SfntAvarSegmentMap[] segmentMaps,
        int axisIndexMapOffset,
        int variationStoreOffset,
        SfntDeltaSetIndexMap? axisIndexMap,
        SfntItemVariationStore? variationStore)
    {
        this.majorVersion = majorVersion;
        this.minorVersion = minorVersion;
        this.segmentMaps = segmentMaps;
        this.axisIndexMapOffset = axisIndexMapOffset;
        this.variationStoreOffset = variationStoreOffset;
        this.axisIndexMap = axisIndexMap;
        this.variationStore = variationStore;
    }

    internal int GetMajorVersion()
    {
        return majorVersion;
    }

    internal int GetMinorVersion()
    {
        return minorVersion;
    }

    internal int GetSegmentMapCount()
    {
        return segmentMaps.Length;
    }

    internal SfntAvarSegmentMap GetSegmentMap(int index)
    {
        if (index < 0 || index >= segmentMaps.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return segmentMaps[index];
    }

    internal int GetAxisIndexMapOffset()
    {
        return axisIndexMapOffset;
    }

    internal int GetVariationStoreOffset()
    {
        return variationStoreOffset;
    }

    internal SfntDeltaSetIndexMap? GetAxisIndexMap()
    {
        return axisIndexMap;
    }

    internal SfntItemVariationStore? GetVariationStore()
    {
        return variationStore;
    }

    internal SfntDeltaSetIndex GetVariationIndexForAxis(int axisIndex)
    {
        if (axisIndex < 0 || axisIndex > 0xFFFF)
        {
            throw new System.ArgumentOutOfRangeException("axisIndex");
        }

        return axisIndexMap == null ?
            new SfntDeltaSetIndex(0, axisIndex) :
            axisIndexMap.Resolve(axisIndex);
    }
}

internal static class SfntAvarTableParser
{
    const int HeaderLength = 8;
    const int VersionTwoTailLength = 8;
    static readonly SfntTableParser parser = new SfntTableParser(
        FontTags.Avar,
        1,
        2,
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

    internal static SfntAvarTable? FindTable(SfntTableCache cache)
    {
        SfntParsedTableView? view = FindView(cache);

        if (view == null || !view.HasParsedData())
        {
            return null;
        }

        return (SfntAvarTable)view.GetParsedData();
    }

    static SfntTableVersion ReadVersion(SfntTableData table)
    {
        BigEndianReader reader = new BigEndianReader(table.GetDataSlice());
        return new SfntTableVersion(reader.ReadUInt16(), reader.ReadUInt16());
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
            throw FontFormatException.Create("The avar table header is truncated.");
        }

        BigEndianReader reader = new BigEndianReader(tableData);
        int majorVersion = reader.ReadUInt16();
        int minorVersion = reader.ReadUInt16();
        int reserved = reader.ReadUInt16();
        int segmentMapCount = reader.ReadUInt16();

        if (majorVersion != version.GetMajorVersion() ||
            minorVersion != version.GetMinorVersion())
        {
            throw FontFormatException.Create("The avar table version is inconsistent.");
        }

        if (majorVersion != 1 && majorVersion != 2)
        {
            throw FontFormatException.Create("The avar table version is not supported.");
        }

        if (reserved != 0)
        {
            throw FontFormatException.Create("The avar reserved field is nonzero.");
        }

        SfntFvarTable? fvar = SfntFvarTableParser.FindTable(cache);

        if (fvar == null)
        {
            throw FontFormatException.Create(
                "The avar table requires a supported fvar table.");
        }

        int axisCount = fvar.GetAxisCount();

        if (majorVersion == 1 && segmentMapCount != axisCount ||
            majorVersion == 2 &&
            segmentMapCount != 0 && segmentMapCount != axisCount)
        {
            throw FontFormatException.Create(
                "The avar segment-map count does not match fvar.");
        }

        SfntVariationParsingBudget budget = new SfntVariationParsingBudget(
            limits.GetMaxCoverageEntries());
        budget.AddEntries(segmentMapCount);
        SfntAvarSegmentMap[] segmentMaps = ParseSegmentMaps(
            reader,
            fvar,
            segmentMapCount,
            budget);
        int axisIndexMapOffset = 0;
        int variationStoreOffset = 0;
        SfntDeltaSetIndexMap? axisIndexMap = null;
        SfntItemVariationStore? variationStore = null;

        if (majorVersion == 2)
        {
            if (reader.GetRemainingLength() < VersionTwoTailLength)
            {
                throw FontFormatException.Create("The avar version 2 tail is truncated.");
            }

            int tailEnd = CheckedFontMath.AddOffset(
                reader.GetPosition(),
                VersionTwoTailLength);
            axisIndexMapOffset = ReadTableOffset(
                reader,
                tableData,
                tailEnd,
                4,
                "The avar axis-index map offset is invalid.");
            variationStoreOffset = ReadTableOffset(
                reader,
                tableData,
                tailEnd,
                8,
                "The avar variation-store offset is invalid.");

            if (axisIndexMapOffset != 0 &&
                axisIndexMapOffset == variationStoreOffset)
            {
                throw FontFormatException.Create(
                    "The avar version 2 structures have the same offset.");
            }

            if (axisIndexMapOffset != 0)
            {
                axisIndexMap = SfntDeltaSetIndexMapParser.Parse(
                    tableData,
                    axisIndexMapOffset,
                    tailEnd,
                    budget);
            }

            if (variationStoreOffset != 0)
            {
                variationStore = SfntItemVariationStoreParser.Parse(
                    tableData,
                    variationStoreOffset,
                    tailEnd,
                    axisCount,
                    limits,
                    budget);
                ValidateVariationIndexes(
                    axisIndexMap,
                    variationStore,
                    axisCount);
            }
        }

        return new SfntAvarTable(
            majorVersion,
            minorVersion,
            segmentMaps,
            axisIndexMapOffset,
            variationStoreOffset,
            axisIndexMap,
            variationStore);
    }

    static SfntAvarSegmentMap[] ParseSegmentMaps(
        BigEndianReader reader,
        SfntFvarTable fvar,
        int count,
        SfntVariationParsingBudget budget)
    {
        SfntAvarSegmentMap[] segmentMaps = new SfntAvarSegmentMap[count];
        int axisIndex = 0;

        while (axisIndex < segmentMaps.Length)
        {
            if (reader.GetRemainingLength() < 2)
            {
                throw FontFormatException.Create("An avar segment map is truncated.");
            }

            int valueMapCount = reader.ReadUInt16();
            budget.AddEntries(valueMapCount);
            int requiredLength = CheckedFontMath.MultiplyCount(
                valueMapCount,
                4);

            if (requiredLength > reader.GetRemainingLength())
            {
                throw FontFormatException.Create(
                    "An avar axis-value map array is truncated.");
            }

            SfntAvarAxisValueMap[] valueMaps =
                new SfntAvarAxisValueMap[valueMapCount];
            double previousFrom = -2;
            double previousTo = -2;
            bool hasNegative = false;
            bool hasZero = false;
            bool hasPositive = false;
            int valueIndex = 0;

            while (valueIndex < valueMaps.Length)
            {
                double fromCoordinate = reader.ReadF2Dot14();
                double toCoordinate = reader.ReadF2Dot14();

                if (fromCoordinate < -1 || fromCoordinate > 1 ||
                    toCoordinate < -1 || toCoordinate > 1)
                {
                    throw FontFormatException.Create(
                        "An avar axis-value mapping is outside the normalized range.");
                }

                if (valueIndex != 0 &&
                    (fromCoordinate <= previousFrom ||
                    toCoordinate < previousTo))
                {
                    throw FontFormatException.Create(
                        "An avar axis-value map is not monotonic.");
                }

                if (fromCoordinate == -1 && toCoordinate == -1)
                {
                    hasNegative = true;
                }

                if (fromCoordinate == 0 && toCoordinate == 0)
                {
                    hasZero = true;
                }

                if (fromCoordinate == 1 && toCoordinate == 1)
                {
                    hasPositive = true;
                }

                valueMaps[valueIndex] = new SfntAvarAxisValueMap(
                    fromCoordinate,
                    toCoordinate);
                previousFrom = fromCoordinate;
                previousTo = toCoordinate;
                valueIndex += 1;
            }

            segmentMaps[axisIndex] = new SfntAvarSegmentMap(
                fvar.GetAxis(axisIndex).GetTag(),
                valueMaps,
                valueMapCount == 0 ||
                hasNegative && hasZero && hasPositive);
            axisIndex += 1;
        }

        return segmentMaps;
    }

    static int ReadTableOffset(
        BigEndianReader reader,
        FontDataSlice tableData,
        int minimumOffset,
        int requiredLength,
        string message)
    {
        long encodedOffset = reader.ReadOffset32();

        if (encodedOffset == 0)
        {
            return 0;
        }

        if (encodedOffset > int.MaxValue ||
            encodedOffset < minimumOffset ||
            encodedOffset > tableData.GetLength() - requiredLength)
        {
            throw FontFormatException.Create(message);
        }

        return (int)encodedOffset;
    }

    static void ValidateVariationIndexes(
        SfntDeltaSetIndexMap? axisIndexMap,
        SfntItemVariationStore variationStore,
        int axisCount)
    {
        int axisIndex = 0;

        while (axisIndex < axisCount)
        {
            SfntDeltaSetIndex variationIndex = axisIndexMap == null ?
                new SfntDeltaSetIndex(0, axisIndex) :
                axisIndexMap.Resolve(axisIndex);
            ValidateVariationIndex(variationIndex, variationStore);
            axisIndex += 1;
        }
    }

    static void ValidateVariationIndex(
        SfntDeltaSetIndex variationIndex,
        SfntItemVariationStore variationStore)
    {
        variationStore.ValidateDeltaSetIndex(variationIndex);
    }
}
