namespace ttf;

internal class SfntVariationParsingBudget
{
    int maximumEntries;
    int entries;

    internal SfntVariationParsingBudget(int maximumEntries)
    {
        if (maximumEntries <= 0)
        {
            throw new System.ArgumentOutOfRangeException("maximumEntries");
        }

        this.maximumEntries = maximumEntries;
        entries = 0;
    }

    internal void AddEntries(int count)
    {
        if (count < 0)
        {
            throw new System.ArgumentOutOfRangeException("count");
        }

        if (count > maximumEntries - entries)
        {
            throw FontLimitException.Create(
                "Variation data exceeds the configured coverage-entry limit.");
        }

        entries += count;
    }
}

internal class SfntDeltaSetIndex
{
    int outerIndex;
    int innerIndex;

    internal SfntDeltaSetIndex(int outerIndex, int innerIndex)
    {
        if (outerIndex < 0 || outerIndex > 0xFFFF)
        {
            throw new System.ArgumentOutOfRangeException("outerIndex");
        }

        if (innerIndex < 0 || innerIndex > 0xFFFF)
        {
            throw new System.ArgumentOutOfRangeException("innerIndex");
        }

        this.outerIndex = outerIndex;
        this.innerIndex = innerIndex;
    }

    internal int GetOuterIndex()
    {
        return outerIndex;
    }

    internal int GetInnerIndex()
    {
        return innerIndex;
    }

    internal bool HasVariationData()
    {
        return outerIndex != 0xFFFF || innerIndex != 0xFFFF;
    }
}

internal class SfntVariationIndex
{
    SfntDeltaSetIndex deltaSetIndex;

    internal SfntVariationIndex(int outerIndex, int innerIndex)
    {
        deltaSetIndex = new SfntDeltaSetIndex(outerIndex, innerIndex);
    }

    internal int GetOuterIndex()
    {
        return deltaSetIndex.GetOuterIndex();
    }

    internal int GetInnerIndex()
    {
        return deltaSetIndex.GetInnerIndex();
    }

    internal SfntDeltaSetIndex GetDeltaSetIndex()
    {
        return deltaSetIndex;
    }

    internal double CalculateDelta(SfntVariationLocation location)
    {
        if (location == null)
        {
            throw new System.ArgumentNullException("location");
        }

        return location.CalculateDelta(deltaSetIndex);
    }
}

internal class SfntDeltaSetIndexMap
{
    int offset;
    int format;
    int entryFormat;
    int entrySize;
    int innerIndexBitCount;
    SfntDeltaSetIndex[] entries;

    internal SfntDeltaSetIndexMap(
        int offset,
        int format,
        int entryFormat,
        int entrySize,
        int innerIndexBitCount,
        SfntDeltaSetIndex[] entries)
    {
        this.offset = offset;
        this.format = format;
        this.entryFormat = entryFormat;
        this.entrySize = entrySize;
        this.innerIndexBitCount = innerIndexBitCount;
        this.entries = entries;
    }

    internal int GetOffset()
    {
        return offset;
    }

    internal int GetFormat()
    {
        return format;
    }

    internal int GetEntryFormat()
    {
        return entryFormat;
    }

    internal int GetEntrySize()
    {
        return entrySize;
    }

    internal int GetInnerIndexBitCount()
    {
        return innerIndexBitCount;
    }

    internal int GetEntryCount()
    {
        return entries.Length;
    }

    internal SfntDeltaSetIndex GetEntry(int index)
    {
        if (index < 0 || index >= entries.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return entries[index];
    }

    internal SfntDeltaSetIndex Resolve(long index)
    {
        if (index < 0 || index > uint.MaxValue)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        if (entries.Length == 0)
        {
            return new SfntDeltaSetIndex(
                (int)(index >> 16),
                (int)(index & 0xFFFF));
        }

        int selectedIndex = index < entries.Length ?
            (int)index : entries.Length - 1;
        return entries[selectedIndex];
    }
}

internal static class SfntDeltaSetIndexMapParser
{
    internal static SfntDeltaSetIndexMap Parse(
        FontDataSlice tableData,
        int offset,
        int minimumOffset,
        SfntVariationParsingBudget budget)
    {
        if (tableData == null)
        {
            throw new System.ArgumentNullException("tableData");
        }

        if (budget == null)
        {
            throw new System.ArgumentNullException("budget");
        }

        if (minimumOffset < 0)
        {
            throw new System.ArgumentOutOfRangeException("minimumOffset");
        }

        if (offset < minimumOffset || offset > tableData.GetLength() - 4)
        {
            throw FontFormatException.Create(
                "A DeltaSetIndexMap offset is invalid.");
        }

        BigEndianReader reader = new BigEndianReader(
            tableData.GetSlice(offset, tableData.GetLength() - offset));
        int format = reader.ReadUInt8();
        int entryFormat = reader.ReadUInt8();

        if (format != 0 && format != 1)
        {
            throw FontFormatException.Create(
                "A DeltaSetIndexMap format is not supported.");
        }

        if ((entryFormat & 0xC0) != 0)
        {
            throw FontFormatException.Create(
                "A DeltaSetIndexMap reserved entry-format bit is nonzero.");
        }

        int entrySize = ((entryFormat & 0x30) >> 4) + 1;
        int innerIndexBitCount = (entryFormat & 0x0F) + 1;

        if (innerIndexBitCount > entrySize * 8)
        {
            throw FontFormatException.Create(
                "A DeltaSetIndexMap entry format is invalid.");
        }

        uint encodedCount = format == 0 ?
            reader.ReadUInt16() : reader.ReadUInt32();

        if (encodedCount > int.MaxValue)
        {
            throw FontLimitException.Create(
                "A DeltaSetIndexMap entry count is too large.");
        }

        int count = (int)encodedCount;
        budget.AddEntries(count);
        int requiredLength = CheckedFontMath.MultiplyCount(count, entrySize);

        if (requiredLength > reader.GetRemainingLength())
        {
            throw FontFormatException.Create(
                "A DeltaSetIndexMap entry array is truncated.");
        }

        SfntDeltaSetIndex[] entries = new SfntDeltaSetIndex[count];
        uint innerMask = (1U << innerIndexBitCount) - 1U;
        int index = 0;

        while (index < entries.Length)
        {
            uint encoded = ReadEntry(reader, entrySize);
            int outerIndex = (int)(encoded >> innerIndexBitCount);
            int innerIndex = (int)(encoded & innerMask);

            if (outerIndex > 0xFFFF)
            {
                throw FontFormatException.Create(
                    "A DeltaSetIndexMap outer index is too large.");
            }

            entries[index] = new SfntDeltaSetIndex(outerIndex, innerIndex);
            index += 1;
        }

        return new SfntDeltaSetIndexMap(
            offset,
            format,
            entryFormat,
            entrySize,
            innerIndexBitCount,
            entries);
    }

    static uint ReadEntry(BigEndianReader reader, int entrySize)
    {
        if (entrySize == 1)
        {
            return reader.ReadUInt8();
        }

        if (entrySize == 2)
        {
            return reader.ReadUInt16();
        }

        if (entrySize == 3)
        {
            return reader.ReadUInt24();
        }

        return reader.ReadUInt32();
    }
}

internal class SfntVariationRegionAxis
{
    double start;
    double peak;
    double end;

    internal SfntVariationRegionAxis(double start, double peak, double end)
    {
        if (start < -1 || start > 1 ||
            peak < -1 || peak > 1 ||
            end < -1 || end > 1 ||
            start > peak || peak > end ||
            (peak != 0 && start < 0 && end > 0))
        {
            throw FontFormatException.Create(
                "A variation-region axis has invalid coordinates.");
        }

        this.start = start;
        this.peak = peak;
        this.end = end;
    }

    internal double GetStart()
    {
        return start;
    }

    internal double GetPeak()
    {
        return peak;
    }

    internal double GetEnd()
    {
        return end;
    }
}

internal class SfntVariationRegion
{
    SfntVariationRegionAxis[] axes;

    internal SfntVariationRegion(SfntVariationRegionAxis[] axes)
    {
        this.axes = axes;
    }

    internal int GetAxisCount()
    {
        return axes.Length;
    }

    internal SfntVariationRegionAxis GetAxis(int index)
    {
        if (index < 0 || index >= axes.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return axes[index];
    }
}

internal class SfntItemVariationData
{
    int itemCount;
    int wordDeltaCount;
    bool usesLongWords;
    int[] regionIndexes;
    int[][] deltaSets;

    internal SfntItemVariationData(
        int itemCount,
        int wordDeltaCount,
        bool usesLongWords,
        int[] regionIndexes,
        int[][] deltaSets)
    {
        this.itemCount = itemCount;
        this.wordDeltaCount = wordDeltaCount;
        this.usesLongWords = usesLongWords;
        this.regionIndexes = regionIndexes;
        this.deltaSets = deltaSets;
    }

    internal int GetItemCount()
    {
        return itemCount;
    }

    internal int GetWordDeltaCount()
    {
        return wordDeltaCount;
    }

    internal bool UsesLongWords()
    {
        return usesLongWords;
    }

    internal int GetRegionIndexCount()
    {
        return regionIndexes.Length;
    }

    internal int GetRegionIndex(int index)
    {
        if (index < 0 || index >= regionIndexes.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return regionIndexes[index];
    }

    internal int GetDelta(int itemIndex, int regionIndex)
    {
        if (itemIndex < 0 || itemIndex >= deltaSets.Length)
        {
            throw new System.ArgumentOutOfRangeException("itemIndex");
        }

        if (regionIndex < 0 || regionIndex >= regionIndexes.Length)
        {
            throw new System.ArgumentOutOfRangeException("regionIndex");
        }

        return deltaSets[itemIndex][regionIndex];
    }
}

internal class SfntItemVariationStore
{
    int offset;
    int format;
    int axisCount;
    SfntVariationRegion[] regions;
    SfntItemVariationData[] variationData;

    internal SfntItemVariationStore(
        int offset,
        int format,
        int axisCount,
        SfntVariationRegion[] regions,
        SfntItemVariationData[] variationData)
    {
        this.offset = offset;
        this.format = format;
        this.axisCount = axisCount;
        this.regions = regions;
        this.variationData = variationData;
    }

    internal int GetOffset()
    {
        return offset;
    }

    internal int GetFormat()
    {
        return format;
    }

    internal int GetAxisCount()
    {
        return axisCount;
    }

    internal int GetRegionCount()
    {
        return regions.Length;
    }

    internal SfntVariationRegion GetRegion(int index)
    {
        if (index < 0 || index >= regions.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return regions[index];
    }

    internal int GetVariationDataCount()
    {
        return variationData.Length;
    }

    internal SfntItemVariationData GetVariationData(int index)
    {
        if (index < 0 || index >= variationData.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return variationData[index];
    }

    internal SfntVariationLocation CreateLocation(double[] coordinates)
    {
        return new SfntVariationLocation(this, coordinates);
    }

    internal void ValidateDeltaSetIndex(SfntDeltaSetIndex index)
    {
        FindVariationData(index);
    }

    internal SfntItemVariationData? FindVariationData(
        SfntDeltaSetIndex index)
    {
        if (index == null)
        {
            throw new System.ArgumentNullException("index");
        }

        if (!index.HasVariationData())
        {
            return null;
        }

        int outerIndex = index.GetOuterIndex();

        if (outerIndex >= variationData.Length)
        {
            throw FontFormatException.Create(
                "A delta-set index references missing variation data.");
        }

        SfntItemVariationData data = variationData[outerIndex];

        if (data == null)
        {
            return null;
        }

        if (index.GetInnerIndex() >= data.GetItemCount())
        {
            throw FontFormatException.Create(
                "A delta-set index references a missing delta set.");
        }

        return data;
    }
}

internal class SfntVariationRegionLocation
{
    double[] regionScalars;

    internal static SfntVariationRegionLocation Create(
        SfntItemVariationStore store,
        double[] coordinates)
    {
        if (store == null)
        {
            throw new System.ArgumentNullException("store");
        }

        SfntVariationRegion[] regions =
            new SfntVariationRegion[store.GetRegionCount()];
        int regionIndex = 0;

        while (regionIndex < regions.Length)
        {
            regions[regionIndex] = store.GetRegion(regionIndex);
            regionIndex += 1;
        }

        return new SfntVariationRegionLocation(
            regions,
            store.GetAxisCount(),
            coordinates);
    }

    internal SfntVariationRegionLocation(
        SfntVariationRegion[] regions,
        int axisCount,
        double[]? coordinates)
    {
        if (regions == null)
        {
            throw new System.ArgumentNullException("regions");
        }

        Initialize(axisCount, regions.Length, coordinates);
        int regionIndex = 0;

        System.ArgumentNullException.ThrowIfNull(regionScalars);
        while (regionIndex < regionScalars.Length)
        {
            SfntVariationRegion region = regions[regionIndex];

            if (region == null || region.GetAxisCount() != axisCount)
            {
                throw new System.ArgumentException(
                    "A variation region does not match the axis count.",
                    "regions");
            }

            regionScalars[regionIndex] = CalculateRegionScalar(
                region,
                coordinates);
            regionIndex += 1;
        }
    }

    internal static SfntVariationRegionLocation Create(
        SfntVariationRegion[] regions,
        int axisCount,
        double[]? coordinates)
    {
        return new SfntVariationRegionLocation(
            regions,
            axisCount,
            coordinates);
    }

    internal int GetRegionCount()
    {
        return regionScalars.Length;
    }

    internal double GetRegionScalar(int index)
    {
        if (index < 0 || index >= regionScalars.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return regionScalars[index];
    }

    void Initialize(
        int axisCount,
        int regionCount,
        double[]? coordinates)
    {
        if (axisCount < 0)
        {
            throw new System.ArgumentOutOfRangeException("axisCount");
        }

        if (regionCount < 0)
        {
            throw new System.ArgumentOutOfRangeException("regionCount");
        }

        if (coordinates == null)
        {
            throw new System.ArgumentNullException("coordinates");
        }

        if (coordinates.Length != axisCount)
        {
            throw new System.ArgumentException(
                "The normalized coordinate count does not match the variation regions.",
                "coordinates");
        }

        int axisIndex = 0;

        while (axisIndex < coordinates.Length)
        {
            if (!double.IsFinite(coordinates[axisIndex]) ||
                coordinates[axisIndex] < -1 || coordinates[axisIndex] > 1)
            {
                throw new System.ArgumentOutOfRangeException("coordinates");
            }

            axisIndex += 1;
        }

        regionScalars = new double[regionCount];
    }

    static double CalculateRegionScalar(
        SfntVariationRegion region,
        double[]? coordinates)
    {
        double scalar = 1;
        int axisIndex = 0;

        System.ArgumentNullException.ThrowIfNull(coordinates);
        while (axisIndex < coordinates.Length && scalar != 0)
        {
            SfntVariationRegionAxis axis = region.GetAxis(axisIndex);
            double peak = axis.GetPeak();

            if (peak != 0)
            {
                double coordinate = coordinates[axisIndex];

                if (coordinate < axis.GetStart() ||
                    coordinate > axis.GetEnd())
                {
                    scalar = 0;
                }
                else if (coordinate != peak)
                {
                    if (coordinate < peak)
                    {
                        scalar *= (coordinate - axis.GetStart()) /
                            (peak - axis.GetStart());
                    }
                    else
                    {
                        scalar *= (axis.GetEnd() - coordinate) /
                            (axis.GetEnd() - peak);
                    }
                }
            }

            axisIndex += 1;
        }

        return scalar;
    }
}

internal class SfntVariationLocation
{
    SfntItemVariationStore store;
    SfntVariationRegionLocation regionLocation;

    internal SfntVariationLocation(
        SfntItemVariationStore store,
        double[] coordinates)
    {
        if (store == null)
        {
            throw new System.ArgumentNullException("store");
        }

        this.store = store;
        regionLocation = SfntVariationRegionLocation.Create(store, coordinates);
    }

    internal double CalculateDelta(SfntDeltaSetIndex index)
    {
        SfntItemVariationData? data = store.FindVariationData(index);

        if (data == null)
        {
            return 0;
        }

        double delta = 0;
        int regionIndex = 0;

        while (regionIndex < data.GetRegionIndexCount())
        {
            delta += data.GetDelta(
                index.GetInnerIndex(),
                regionIndex) * regionLocation.GetRegionScalar(
                    data.GetRegionIndex(regionIndex));
            regionIndex += 1;
        }

        return delta;
    }
}

internal static class SfntItemVariationStoreParser
{
    const int HeaderFixedLength = 8;
    const int VariationDataHeaderLength = 6;

    internal static SfntItemVariationStore Parse(
        FontDataSlice tableData,
        int storeOffset,
        int minimumOffset,
        int expectedAxisCount,
        FontLimits limits,
        SfntVariationParsingBudget budget)
    {
        if (tableData == null)
        {
            throw new System.ArgumentNullException("tableData");
        }

        if (limits == null)
        {
            throw new System.ArgumentNullException("limits");
        }

        if (budget == null)
        {
            throw new System.ArgumentNullException("budget");
        }

        if (minimumOffset < 0)
        {
            throw new System.ArgumentOutOfRangeException("minimumOffset");
        }

        if (expectedAxisCount < 0 || expectedAxisCount > 0xFFFF)
        {
            throw new System.ArgumentOutOfRangeException("expectedAxisCount");
        }

        if (storeOffset < minimumOffset ||
            storeOffset > tableData.GetLength() - HeaderFixedLength)
        {
            throw FontFormatException.Create(
                "An ItemVariationStore offset is invalid.");
        }

        BigEndianReader reader = CreateReader(tableData, storeOffset);
        int format = reader.ReadUInt16();

        if (format != 1)
        {
            throw FontFormatException.Create(
                "An ItemVariationStore format is not supported.");
        }

        long encodedRegionListOffset = reader.ReadOffset32();
        int variationDataCount = reader.ReadUInt16();
        budget.AddEntries(variationDataCount);
        int offsetsLength = CheckedFontMath.MultiplyCount(
            variationDataCount,
            4);
        int headerLength = CheckedFontMath.AddOffset(
            HeaderFixedLength,
            offsetsLength);

        if (offsetsLength > reader.GetRemainingLength())
        {
            throw FontFormatException.Create(
                "An ItemVariationStore data-offset array is truncated.");
        }

        int regionListOffset = ResolveRelativeOffset(
            tableData,
            storeOffset,
            encodedRegionListOffset,
            headerLength,
            4,
            "An ItemVariationStore region-list offset is invalid.");
        int[] variationDataOffsets = new int[variationDataCount];
        int index = 0;

        while (index < variationDataOffsets.Length)
        {
            long encodedOffset = reader.ReadOffset32();

            if (encodedOffset != 0)
            {
                variationDataOffsets[index] = ResolveRelativeOffset(
                    tableData,
                    storeOffset,
                    encodedOffset,
                    headerLength,
                    VariationDataHeaderLength,
                    "An ItemVariationData offset is invalid.");
            }

            index += 1;
        }

        SfntVariationRegion[] regions = ParseRegions(
            tableData,
            regionListOffset,
            expectedAxisCount,
            limits,
            budget);
        SfntItemVariationData[] variationData =
            new SfntItemVariationData[variationDataCount];
        index = 0;

        while (index < variationData.Length)
        {
            int offset = variationDataOffsets[index];

            if (offset != 0)
            {
                int previous = FindPreviousOffset(
                    variationDataOffsets,
                    index,
                    offset);
                variationData[index] = previous < 0 ?
                    ParseVariationData(
                        tableData,
                        offset,
                        regions.Length,
                        budget) :
                    variationData[previous];
            }

            index += 1;
        }

        return new SfntItemVariationStore(
            storeOffset,
            format,
            expectedAxisCount,
            regions,
            variationData);
    }

    static SfntVariationRegion[] ParseRegions(
        FontDataSlice tableData,
        int offset,
        int expectedAxisCount,
        FontLimits limits,
        SfntVariationParsingBudget budget)
    {
        BigEndianReader reader = CreateReader(tableData, offset);
        int axisCount = reader.ReadUInt16();
        int regionCount = reader.ReadUInt16();

        if (axisCount != expectedAxisCount)
        {
            throw FontFormatException.Create(
                "A variation-region list axis count does not match fvar.");
        }

        if (regionCount >= 32768)
        {
            throw FontFormatException.Create(
                "A variation-region list reserved count bit is nonzero.");
        }

        if (regionCount > limits.GetMaxVariationRegions())
        {
            throw FontLimitException.Create(
                "A variation-region list exceeds the configured region limit.");
        }

        int coordinateCount = CheckedFontMath.MultiplyCount(
            axisCount,
            regionCount);
        budget.AddEntries(coordinateCount);
        int coordinateBytes = CheckedFontMath.MultiplyCount(
            coordinateCount,
            6);

        if (coordinateBytes > reader.GetRemainingLength())
        {
            throw FontFormatException.Create(
                "A variation-region list is truncated.");
        }

        SfntVariationRegion[] regions = new SfntVariationRegion[regionCount];
        int regionIndex = 0;

        while (regionIndex < regions.Length)
        {
            SfntVariationRegionAxis[] axes =
                new SfntVariationRegionAxis[axisCount];
            int axisIndex = 0;

            while (axisIndex < axes.Length)
            {
                axes[axisIndex] = new SfntVariationRegionAxis(
                    reader.ReadF2Dot14(),
                    reader.ReadF2Dot14(),
                    reader.ReadF2Dot14());
                axisIndex += 1;
            }

            regions[regionIndex] = new SfntVariationRegion(axes);
            regionIndex += 1;
        }

        return regions;
    }

    static SfntItemVariationData ParseVariationData(
        FontDataSlice tableData,
        int offset,
        int regionCount,
        SfntVariationParsingBudget budget)
    {
        BigEndianReader reader = CreateReader(tableData, offset);
        int itemCount = reader.ReadUInt16();
        int encodedWordDeltaCount = reader.ReadUInt16();
        int regionIndexCount = reader.ReadUInt16();
        bool usesLongWords = (encodedWordDeltaCount & 0x8000) != 0;
        int wordDeltaCount = encodedWordDeltaCount & 0x7FFF;

        if (wordDeltaCount > regionIndexCount)
        {
            throw FontFormatException.Create(
                "An ItemVariationData word-delta count is invalid.");
        }

        budget.AddEntries(regionIndexCount);
        int deltaCount = CheckedFontMath.MultiplyCount(
            itemCount,
            regionIndexCount);
        budget.AddEntries(deltaCount);
        int regionIndexBytes = CheckedFontMath.MultiplyCount(
            regionIndexCount,
            2);
        int rowUnits = CheckedFontMath.AddCounts(
            regionIndexCount,
            wordDeltaCount);
        int rowBytes = usesLongWords ?
            CheckedFontMath.MultiplyCount(rowUnits, 2) : rowUnits;
        int deltaBytes = CheckedFontMath.MultiplyCount(itemCount, rowBytes);
        int requiredBytes = CheckedFontMath.AddOffset(
            regionIndexBytes,
            deltaBytes);

        if (requiredBytes > reader.GetRemainingLength())
        {
            throw FontFormatException.Create(
                "An ItemVariationData subtable is truncated.");
        }

        int[] regionIndexes = new int[regionIndexCount];
        int index = 0;

        while (index < regionIndexes.Length)
        {
            int regionIndex = reader.ReadUInt16();

            if (regionIndex >= regionCount)
            {
                throw FontFormatException.Create(
                    "An ItemVariationData subtable references a missing region.");
            }

            regionIndexes[index] = regionIndex;
            index += 1;
        }

        int[][] deltaSets = new int[itemCount][];
        int itemIndex = 0;

        while (itemIndex < deltaSets.Length)
        {
            int[] deltas = new int[regionIndexCount];
            index = 0;

            while (index < deltas.Length)
            {
                bool word = index < wordDeltaCount;
                deltas[index] = ReadDelta(reader, usesLongWords, word);
                index += 1;
            }

            deltaSets[itemIndex] = deltas;
            itemIndex += 1;
        }

        return new SfntItemVariationData(
            itemCount,
            wordDeltaCount,
            usesLongWords,
            regionIndexes,
            deltaSets);
    }

    static int ReadDelta(
        BigEndianReader reader,
        bool usesLongWords,
        bool word)
    {
        if (usesLongWords)
        {
            return word ? reader.ReadInt32() : reader.ReadInt16();
        }

        return word ? reader.ReadInt16() : reader.ReadInt8();
    }

    static int ResolveRelativeOffset(
        FontDataSlice tableData,
        int baseOffset,
        long encodedOffset,
        int minimumRelativeOffset,
        int requiredLength,
        string message)
    {
        if (encodedOffset > int.MaxValue ||
            encodedOffset < minimumRelativeOffset)
        {
            throw FontFormatException.Create(message);
        }

        int offset = CheckedFontMath.AddOffset(
            baseOffset,
            (int)encodedOffset);

        if (offset > tableData.GetLength() - requiredLength)
        {
            throw FontFormatException.Create(message);
        }

        return offset;
    }

    static int FindPreviousOffset(
        int[] offsets,
        int currentIndex,
        int offset)
    {
        int index = 0;

        while (index < currentIndex)
        {
            if (offsets[index] == offset)
            {
                return index;
            }

            index += 1;
        }

        return -1;
    }

    static BigEndianReader CreateReader(FontDataSlice tableData, int offset)
    {
        return new BigEndianReader(
            tableData.GetSlice(offset, tableData.GetLength() - offset));
    }
}
