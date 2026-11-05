namespace ttf;

internal class SfntDirectory
{
    long directoryOffset;
    uint scalerType;
    int storedSearchRange;
    int storedEntrySelector;
    int storedRangeShift;
    int searchRange;
    int entrySelector;
    int rangeShift;
    SfntTableRecord[] records;

    internal SfntDirectory(
        long directoryOffset,
        uint scalerType,
        int storedSearchRange,
        int storedEntrySelector,
        int storedRangeShift,
        int searchRange,
        int entrySelector,
        int rangeShift,
        SfntTableRecord[] records)
    {
        this.directoryOffset = directoryOffset;
        this.scalerType = scalerType;
        this.storedSearchRange = storedSearchRange;
        this.storedEntrySelector = storedEntrySelector;
        this.storedRangeShift = storedRangeShift;
        this.searchRange = searchRange;
        this.entrySelector = entrySelector;
        this.rangeShift = rangeShift;
        this.records = records;
    }

    internal long GetDirectoryOffset()
    {
        return directoryOffset;
    }

    internal uint GetScalerType()
    {
        return scalerType;
    }

    internal int GetTableCount()
    {
        return records.Length;
    }

    internal int GetStoredSearchRange()
    {
        return storedSearchRange;
    }

    internal int GetStoredEntrySelector()
    {
        return storedEntrySelector;
    }

    internal int GetStoredRangeShift()
    {
        return storedRangeShift;
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

    internal SfntTableRecord GetTableRecord(int index)
    {
        if (index < 0 || index >= records.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return records[index];
    }
}

internal class SfntTableRecord
{
    int tag;
    uint checksum;
    long offset;
    long length;

    internal SfntTableRecord(int tag, uint checksum, long offset, long length)
    {
        this.tag = tag;
        this.checksum = checksum;
        this.offset = offset;
        this.length = length;
    }

    internal int GetTag()
    {
        return tag;
    }

    internal uint GetChecksum()
    {
        return checksum;
    }

    internal long GetOffset()
    {
        return offset;
    }

    internal long GetLength()
    {
        return length;
    }
}
