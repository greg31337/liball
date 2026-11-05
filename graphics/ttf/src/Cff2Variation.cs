namespace ttf;

internal class Cff2VariationData
{
    int[] regionIndexes;

    internal Cff2VariationData(int[] regionIndexes)
    {
        this.regionIndexes = regionIndexes;
    }

    internal int GetRegionCount() { return regionIndexes.Length; }

    internal int GetRegionIndex(int index)
    {
        if (index < 0 || index >= regionIndexes.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return regionIndexes[index];
    }
}

internal class Cff2VariationStore
{
    SfntVariationRegion[] regions;
    Cff2VariationData[] variationData;
    int axisCount;

    internal Cff2VariationStore(
        SfntVariationRegion[] regions,
        Cff2VariationData[] variationData,
        int axisCount)
    {
        this.regions = regions;
        this.variationData = variationData;
        this.axisCount = axisCount;
    }

    internal int GetAxisCount() { return axisCount; }
    internal int GetRegionCount() { return regions.Length; }
    internal int GetVariationDataCount() { return variationData.Length; }

    internal SfntVariationRegion GetRegion(int index)
    {
        if (index < 0 || index >= regions.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return regions[index];
    }

    internal Cff2VariationData GetVariationData(int index)
    {
        if (index < 0 || index >= variationData.Length)
        {
            throw FontFormatException.Create(
                "A CFF2 vsindex references missing variation data.");
        }

        if (variationData[index] == null)
        {
            throw FontFormatException.Create(
                "A CFF2 vsindex references a null variation-data offset.");
        }

        return variationData[index];
    }

    internal double[] CopyRegionScalars(int dataIndex, double[] coordinates)
    {
        return CreateLocation(coordinates).CopyRegionScalars(dataIndex);
    }

    internal Cff2VariationLocation CreateLocation(double[]? coordinates)
    {
        return new Cff2VariationLocation(
            this,
            SfntVariationRegionLocation.Create(
                regions,
                axisCount,
                coordinates));
    }

    internal void ValidateCoordinates(double[]? coordinates)
    {
        CreateLocation(coordinates);
    }
}

internal class Cff2VariationLocation
{
    Cff2VariationStore store;
    SfntVariationRegionLocation regionLocation;

    internal Cff2VariationLocation(
        Cff2VariationStore store,
        SfntVariationRegionLocation regionLocation)
    {
        if (store == null)
        {
            throw new System.ArgumentNullException("store");
        }

        if (regionLocation == null)
        {
            throw new System.ArgumentNullException("regionLocation");
        }

        this.store = store;
        this.regionLocation = regionLocation;
    }

    internal double[] CopyRegionScalars(int dataIndex)
    {
        Cff2VariationData selected = store.GetVariationData(dataIndex);
        double[] scalars = new double[selected.GetRegionCount()];
        int index = 0;

        while (index < scalars.Length)
        {
            scalars[index] = regionLocation.GetRegionScalar(
                selected.GetRegionIndex(index));
            index += 1;
        }

        return scalars;
    }
}

internal static class Cff2VariationStoreParser
{
    internal static Cff2VariationStore Parse(
        Cff2VariationStoreReference reference,
        int maximumRegions)
    {
        return Parse(reference, maximumRegions, int.MaxValue);
    }

    internal static Cff2VariationStore Parse(
        Cff2VariationStoreReference reference,
        int maximumRegions,
        int maximumEntries)
    {
        if (reference == null)
        {
            throw new System.ArgumentNullException("reference");
        }

        if (maximumRegions <= 0)
        {
            throw new System.ArgumentOutOfRangeException("maximumRegions");
        }

        if (maximumEntries <= 0)
        {
            throw new System.ArgumentOutOfRangeException("maximumEntries");
        }

        FontDataSlice data = reference.GetData();
        SfntVariationParsingBudget budget =
            new SfntVariationParsingBudget(maximumEntries);
        BigEndianReader reader = new BigEndianReader(data);

        if (reader.ReadUInt16() != 1)
        {
            throw FontFormatException.Create(
                "A CFF2 ItemVariationStore has an unsupported format.");
        }

        int regionListOffset = ReadOffset(reader);
        int dataCount = reader.ReadUInt16();
        budget.AddEntries(dataCount);
        int[] dataOffsets = new int[dataCount];
        int index = 0;

        while (index < dataOffsets.Length)
        {
            dataOffsets[index] = ReadOffset(reader);
            index += 1;
        }

        int headerEnd = reader.GetPosition();

        if (regionListOffset < headerEnd || regionListOffset >= data.GetLength())
        {
            throw FontFormatException.Create(
                "A CFF2 variation-region-list offset is invalid.");
        }

        BigEndianReader regionReader = CreateOffsetReader(data, regionListOffset);
        int axisCount = regionReader.ReadUInt16();
        int regionCount = regionReader.ReadUInt16();

        if (axisCount == 0)
        {
            throw FontFormatException.Create(
                "A CFF2 VariationStore must contain at least one axis.");
        }

        if (regionCount >= 32768 || regionCount > maximumRegions)
        {
            throw FontLimitException.Create(
                "A CFF2 VariationStore exceeds its variation-region limit.");
        }

        int coordinateCount = CheckedFontMath.MultiplyCount(
            axisCount,
            regionCount);
        budget.AddEntries(coordinateCount);
        int coordinateBytes = CheckedFontMath.MultiplyCount(coordinateCount, 6);

        if (coordinateBytes > regionReader.GetRemainingLength())
        {
            throw FontFormatException.Create(
                "A CFF2 variation region list is truncated.");
        }

        SfntVariationRegion[] regions = new SfntVariationRegion[regionCount];
        index = 0;

        while (index < regions.Length)
        {
            SfntVariationRegionAxis[] axes =
                new SfntVariationRegionAxis[axisCount];
            int axisIndex = 0;

            while (axisIndex < axes.Length)
            {
                axes[axisIndex] = new SfntVariationRegionAxis(
                    regionReader.ReadF2Dot14(),
                    regionReader.ReadF2Dot14(),
                    regionReader.ReadF2Dot14());
                axisIndex += 1;
            }

            regions[index] = new SfntVariationRegion(axes);
            index += 1;
        }

        Cff2VariationData[] variationData =
            new Cff2VariationData[dataOffsets.Length];
        index = 0;

        while (index < variationData.Length)
        {
            if (dataOffsets[index] != 0)
            {
                variationData[index] = ParseVariationData(
                    data,
                    dataOffsets[index],
                    headerEnd,
                    regionCount,
                    maximumRegions,
                    budget);
            }

            index += 1;
        }

        return new Cff2VariationStore(regions, variationData, axisCount);
    }

    static Cff2VariationData ParseVariationData(
        FontDataSlice data,
        int offset,
        int headerEnd,
        int regionCount,
        int maximumRegions,
        SfntVariationParsingBudget budget)
    {
        if (offset < headerEnd || offset >= data.GetLength())
        {
            throw FontFormatException.Create(
                "A CFF2 ItemVariationData offset is invalid.");
        }

        BigEndianReader reader = CreateOffsetReader(data, offset);
        int itemCount = reader.ReadUInt16();
        int wordDeltaCount = reader.ReadUInt16();
        int selectedRegionCount = reader.ReadUInt16();

        if (itemCount != 0 || wordDeltaCount != 0)
        {
            throw FontFormatException.Create(
                "CFF2 ItemVariationData must omit embedded delta sets.");
        }

        if (selectedRegionCount > maximumRegions)
        {
            throw FontLimitException.Create(
                "CFF2 ItemVariationData exceeds its region-index limit.");
        }

        budget.AddEntries(selectedRegionCount);

        int requiredBytes = CheckedFontMath.MultiplyCount(
            selectedRegionCount,
            2);

        if (requiredBytes > reader.GetRemainingLength())
        {
            throw FontFormatException.Create(
                "A CFF2 ItemVariationData region-index array is truncated.");
        }

        int[] regionIndexes = new int[selectedRegionCount];
        int index = 0;

        while (index < regionIndexes.Length)
        {
            regionIndexes[index] = reader.ReadUInt16();

            if (regionIndexes[index] >= regionCount)
            {
                throw FontFormatException.Create(
                    "CFF2 ItemVariationData references a missing region.");
            }

            index += 1;
        }

        return new Cff2VariationData(regionIndexes);
    }

    static BigEndianReader CreateOffsetReader(FontDataSlice data, int offset)
    {
        return new BigEndianReader(data.GetSlice(
            offset,
            data.GetLength() - offset));
    }

    static int ReadOffset(BigEndianReader reader)
    {
        uint value = reader.ReadUInt32();

        if (value > int.MaxValue)
        {
            throw FontFormatException.Create(
                "A CFF2 variation offset exceeds the supported range.");
        }

        return (int)value;
    }
}
