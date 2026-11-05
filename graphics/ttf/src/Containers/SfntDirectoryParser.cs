namespace ttf;

internal static class SfntDirectoryParser
{
    internal static SfntDirectory Parse(FontDataSlice data)
    {
        return ParseCore(data, ushort.MaxValue);
    }

    internal static SfntDirectory Parse(FontDataSlice data, FontLimits limits)
    {
        if (limits == null)
        {
            throw new System.ArgumentNullException("limits");
        }

        return ParseCore(data, limits.GetMaxTablesPerFace());
    }

    static SfntDirectory ParseCore(FontDataSlice data, int maximumTableCount)
    {
        BigEndianReader reader = new BigEndianReader(data);
        long directoryOffset = reader.GetAbsolutePosition();
        uint scalerType = reader.ReadUInt32();

        EnsureSupportedScalerType(scalerType);

        int tableCount = reader.ReadUInt16();

        if (tableCount > maximumTableCount)
        {
            throw FontLimitException.Create(
                "The SFNT table count exceeds the configured per-face limit.");
        }

        int storedSearchRange = reader.ReadUInt16();
        int storedEntrySelector = reader.ReadUInt16();
        int storedRangeShift = reader.ReadUInt16();
        int searchRange = CalculateSearchRange(tableCount);
        int entrySelector = CalculateEntrySelector(tableCount);
        int rangeShift = tableCount * 16 - searchRange;
        SfntTableRecord[] records = new SfntTableRecord[tableCount];
        int index = 0;

        while (index < tableCount)
        {
            int tag = reader.ReadTag();
            uint checksum = reader.ReadUInt32();
            long offset = reader.ReadOffset32();
            long length = reader.ReadOffset32();
            records[index] = new SfntTableRecord(tag, checksum, offset, length);
            index += 1;
        }

        return new SfntDirectory(
            directoryOffset,
            scalerType,
            storedSearchRange,
            storedEntrySelector,
            storedRangeShift,
            searchRange,
            entrySelector,
            rangeShift,
            records);
    }

    static void EnsureSupportedScalerType(uint scalerType)
    {
        if (scalerType != 0x00010000U &&
            scalerType != 0x4F54544FU &&
            scalerType != 0x74727565U &&
            scalerType != 0x74797031U)
        {
            throw FontFormatException.Create("The SFNT scaler type is not supported.");
        }
    }

    static int CalculateSearchRange(int tableCount)
    {
        int maximumPowerOfTwo = 0;

        if (tableCount > 0)
        {
            maximumPowerOfTwo = 1;

            while (maximumPowerOfTwo <= tableCount / 2)
            {
                maximumPowerOfTwo *= 2;
            }
        }

        return maximumPowerOfTwo * 16;
    }

    static int CalculateEntrySelector(int tableCount)
    {
        int maximumPowerOfTwo = 1;
        int entrySelector = 0;

        while (maximumPowerOfTwo <= tableCount / 2)
        {
            maximumPowerOfTwo *= 2;
            entrySelector += 1;
        }

        return entrySelector;
    }
}
