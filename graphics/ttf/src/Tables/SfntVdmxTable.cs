namespace ttf;

internal class SfntVdmxRecord
{
    int pixelHeight;
    int maximumY;
    int minimumY;

    internal SfntVdmxRecord(int pixelHeight, int maximumY, int minimumY)
    {
        this.pixelHeight = pixelHeight;
        this.maximumY = maximumY;
        this.minimumY = minimumY;
    }

    internal int GetPixelHeight()
    {
        return pixelHeight;
    }

    internal int GetMaximumY()
    {
        return maximumY;
    }

    internal int GetMinimumY()
    {
        return minimumY;
    }
}

internal class SfntVdmxGroup
{
    int offset;
    int length;
    int startingPixelHeight;
    int endingPixelHeight;
    SfntVdmxRecord[] records;

    internal SfntVdmxGroup(
        int offset,
        int length,
        int startingPixelHeight,
        int endingPixelHeight,
        SfntVdmxRecord[] records)
    {
        this.offset = offset;
        this.length = length;
        this.startingPixelHeight = startingPixelHeight;
        this.endingPixelHeight = endingPixelHeight;
        this.records = records;
    }

    internal int GetOffset()
    {
        return offset;
    }

    internal int GetLength()
    {
        return length;
    }

    internal int GetStartingPixelHeight()
    {
        return startingPixelHeight;
    }

    internal int GetEndingPixelHeight()
    {
        return endingPixelHeight;
    }

    internal int GetRecordCount()
    {
        return records.Length;
    }

    internal SfntVdmxRecord GetRecord(int index)
    {
        return records[index];
    }

    internal SfntVdmxRecord? FindRecord(int pixelHeight)
    {
        if (pixelHeight < 0 || pixelHeight > 0xFFFF)
        {
            throw new System.ArgumentOutOfRangeException("pixelHeight");
        }

        int low = 0;
        int high = records.Length - 1;

        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            int candidate = records[middle].GetPixelHeight();

            if (candidate == pixelHeight)
            {
                return records[middle];
            }

            if (candidate < pixelHeight)
            {
                low = middle + 1;
            }
            else
            {
                high = middle - 1;
            }
        }

        return null;
    }
}

internal class SfntVdmxRatio
{
    int characterSet;
    int xRatio;
    int startingYRatio;
    int endingYRatio;
    SfntVdmxGroup group;

    internal SfntVdmxRatio(
        int characterSet,
        int xRatio,
        int startingYRatio,
        int endingYRatio,
        SfntVdmxGroup group)
    {
        this.characterSet = characterSet;
        this.xRatio = xRatio;
        this.startingYRatio = startingYRatio;
        this.endingYRatio = endingYRatio;
        this.group = group;
    }

    internal int GetCharacterSet()
    {
        return characterSet;
    }

    internal int GetXRatio()
    {
        return xRatio;
    }

    internal int GetStartingYRatio()
    {
        return startingYRatio;
    }

    internal int GetEndingYRatio()
    {
        return endingYRatio;
    }

    internal bool IsDefaultRatio()
    {
        return xRatio == 0 && startingYRatio == 0 && endingYRatio == 0;
    }

    internal SfntVdmxGroup GetGroup()
    {
        return group;
    }

    internal bool Matches(int deviceXRatio, int deviceYRatio)
    {
        if (deviceXRatio <= 0)
        {
            throw new System.ArgumentOutOfRangeException("deviceXRatio");
        }

        if (deviceYRatio <= 0)
        {
            throw new System.ArgumentOutOfRangeException("deviceYRatio");
        }

        if (IsDefaultRatio())
        {
            return true;
        }

        long scaledY = (long)deviceYRatio * xRatio;
        long minimumY = (long)deviceXRatio * startingYRatio;
        long maximumY = (long)deviceXRatio * endingYRatio;
        return scaledY >= minimumY && scaledY <= maximumY;
    }
}

internal class SfntVdmxTable
{
    int version;
    SfntVdmxGroup[] groups;
    SfntVdmxRatio[] ratios;

    internal SfntVdmxTable(
        int version,
        SfntVdmxGroup[] groups,
        SfntVdmxRatio[] ratios)
    {
        this.version = version;
        this.groups = groups;
        this.ratios = ratios;
    }

    internal int GetVersion()
    {
        return version;
    }

    internal int GetGroupCount()
    {
        return groups.Length;
    }

    internal SfntVdmxGroup GetGroup(int index)
    {
        return groups[index];
    }

    internal int GetRatioCount()
    {
        return ratios.Length;
    }

    internal SfntVdmxRatio GetRatio(int index)
    {
        return ratios[index];
    }

    internal SfntVdmxRatio? FindRatio(int deviceXRatio, int deviceYRatio)
    {
        int index = 0;

        while (index < ratios.Length)
        {
            if (ratios[index].Matches(deviceXRatio, deviceYRatio))
            {
                return ratios[index];
            }

            index += 1;
        }

        return null;
    }
}

internal static class SfntVdmxTableParser
{
    const int HeaderLength = 6;
    const int RatioLength = 4;
    const int OffsetLength = 2;
    const int GroupHeaderLength = 4;
    const int RecordLength = 6;
    static readonly SfntTableParser parser = new SfntTableParser(
        FontTags.Vdmx,
        0,
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

    internal static SfntVdmxTable? FindTable(SfntTableCache cache)
    {
        SfntParsedTableView? view = FindView(cache);

        if (view == null || !view.HasParsedData())
        {
            return null;
        }

        return (SfntVdmxTable)view.GetParsedData();
    }

    static SfntTableVersion ReadVersion(SfntTableData table)
    {
        BigEndianReader reader = new BigEndianReader(table.GetDataSlice());
        return new SfntTableVersion(reader.ReadUInt16(), 0);
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
            throw FontFormatException.Create("The VDMX table header is truncated.");
        }

        BigEndianReader reader = new BigEndianReader(tableData);
        int encodedVersion = reader.ReadUInt16();
        int groupCount = reader.ReadUInt16();
        int ratioCount = reader.ReadUInt16();

        if (encodedVersion != 0 && encodedVersion != 1)
        {
            throw FontFormatException.Create("The VDMX table version is not supported.");
        }

        if (groupCount == 0 || ratioCount == 0)
        {
            throw FontFormatException.Create("The VDMX table declares no usable groups.");
        }

        if (groupCount > limits.GetMaxTablesPerFace())
        {
            throw FontLimitException.Create(
                "The VDMX group count exceeds the configured table limit.");
        }

        if (ratioCount > limits.GetMaxCoverageEntries())
        {
            throw FontLimitException.Create(
                "The VDMX ratio count exceeds the configured coverage limit.");
        }

        int headerRecordsLength = CheckedFontMath.MultiplyCount(
            ratioCount,
            RatioLength + OffsetLength);
        int completeHeaderLength = CheckedFontMath.AddOffset(
            HeaderLength,
            headerRecordsLength);

        if (completeHeaderLength > tableData.GetLength())
        {
            throw FontFormatException.Create("The VDMX ratio header is truncated.");
        }

        int[] characterSets = new int[ratioCount];
        int[] xRatios = new int[ratioCount];
        int[] startingYRatios = new int[ratioCount];
        int[] endingYRatios = new int[ratioCount];
        int index = 0;

        while (index < ratioCount)
        {
            int characterSet = reader.ReadUInt8();
            int xRatio = reader.ReadUInt8();
            int startingYRatio = reader.ReadUInt8();
            int endingYRatio = reader.ReadUInt8();
            bool defaultRatio = xRatio == 0 &&
                startingYRatio == 0 && endingYRatio == 0;

            if (characterSet > 1)
            {
                throw FontFormatException.Create(
                    "A VDMX ratio has an unsupported character set.");
            }

            if (defaultRatio)
            {
                if (index != ratioCount - 1)
                {
                    throw FontFormatException.Create(
                        "The default VDMX ratio is not the final ratio.");
                }
            }
            else if (xRatio == 0 || startingYRatio == 0 ||
                endingYRatio < startingYRatio)
            {
                throw FontFormatException.Create("A VDMX ratio range is invalid.");
            }

            characterSets[index] = characterSet;
            xRatios[index] = xRatio;
            startingYRatios[index] = startingYRatio;
            endingYRatios[index] = endingYRatio;
            index += 1;
        }

        int[] groupOffsets = new int[ratioCount];
        index = 0;

        while (index < ratioCount)
        {
            groupOffsets[index] = reader.ReadUInt16();

            if (groupOffsets[index] < completeHeaderLength ||
                groupOffsets[index] > tableData.GetLength() - GroupHeaderLength)
            {
                throw FontFormatException.Create("A VDMX group offset is invalid.");
            }

            index += 1;
        }

        int[] distinctOffsets = new int[groupCount];
        SfntVdmxGroup[] groups = new SfntVdmxGroup[groupCount];
        SfntVdmxGroup[] ratioGroups = new SfntVdmxGroup[ratioCount];
        int distinctCount = 0;
        int inspectedRecords = ratioCount;
        index = 0;

        while (index < ratioCount)
        {
            int existingIndex = FindOffset(
                distinctOffsets,
                distinctCount,
                groupOffsets[index]);
            SfntVdmxGroup group;

            if (existingIndex >= 0)
            {
                group = groups[existingIndex];
            }
            else
            {
                if (distinctCount >= groupCount)
                {
                    throw FontFormatException.Create(
                        "The VDMX ratio records reference too many groups.");
                }

                group = ParseGroup(
                    tableData,
                    groupOffsets[index],
                    limits,
                    inspectedRecords);
                inspectedRecords = CheckedFontMath.AddCounts(
                    inspectedRecords,
                    group.GetRecordCount());

                if (inspectedRecords > limits.GetMaxCoverageEntries())
                {
                    throw FontLimitException.Create(
                        "The VDMX records exceed the configured coverage limit.");
                }

                distinctOffsets[distinctCount] = groupOffsets[index];
                groups[distinctCount] = group;
                distinctCount += 1;
            }

            ratioGroups[index] = group;
            index += 1;
        }

        if (distinctCount != groupCount)
        {
            throw FontFormatException.Create(
                "The VDMX group count does not match the referenced groups.");
        }

        ValidateGroupCoverage(tableData.GetLength(), completeHeaderLength, groups);
        SfntVdmxRatio[] ratios = new SfntVdmxRatio[ratioCount];
        index = 0;

        while (index < ratioCount)
        {
            ratios[index] = new SfntVdmxRatio(
                characterSets[index],
                xRatios[index],
                startingYRatios[index],
                endingYRatios[index],
                ratioGroups[index]);
            index += 1;
        }

        return new SfntVdmxTable(encodedVersion, groups, ratios);
    }

    static SfntVdmxGroup ParseGroup(
        FontDataSlice tableData,
        int offset,
        FontLimits limits,
        int inspectedRecords)
    {
        BigEndianReader reader = new BigEndianReader(tableData.GetSlice(
            offset,
            tableData.GetLength() - offset));
        int recordCount = reader.ReadUInt16();
        int startingPixelHeight = reader.ReadUInt8();
        int endingPixelHeight = reader.ReadUInt8();

        if (recordCount == 0 || startingPixelHeight > endingPixelHeight)
        {
            throw FontFormatException.Create("A VDMX group header is invalid.");
        }

        if (recordCount > limits.GetMaxCoverageEntries() - inspectedRecords)
        {
            throw FontLimitException.Create(
                "The VDMX records exceed the configured coverage limit.");
        }

        int length = CheckedFontMath.GetRangeEnd(
            GroupHeaderLength,
            recordCount,
            RecordLength);

        if (length > tableData.GetLength() - offset)
        {
            throw FontFormatException.Create("A VDMX group is truncated.");
        }

        SfntVdmxRecord[] records = new SfntVdmxRecord[recordCount];
        int previousPixelHeight = -1;
        int index = 0;

        while (index < recordCount)
        {
            int pixelHeight = reader.ReadUInt16();
            int maximumY = reader.ReadInt16();
            int minimumY = reader.ReadInt16();

            if (pixelHeight <= previousPixelHeight)
            {
                throw FontFormatException.Create(
                    "The VDMX height records are not strictly ordered.");
            }

            if (maximumY < minimumY)
            {
                throw FontFormatException.Create("A VDMX height record is invalid.");
            }

            records[index] = new SfntVdmxRecord(
                pixelHeight,
                maximumY,
                minimumY);
            previousPixelHeight = pixelHeight;
            index += 1;
        }

        return new SfntVdmxGroup(
            offset,
            length,
            startingPixelHeight,
            endingPixelHeight,
            records);
    }

    static int FindOffset(int[] offsets, int count, int offset)
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

    static void ValidateGroupCoverage(
        int tableLength,
        int headerLength,
        SfntVdmxGroup[] groups)
    {
        int expectedOffset = headerLength;
        int selectedCount = 0;

        while (selectedCount < groups.Length)
        {
            SfntVdmxGroup? selected = null;
            int index = 0;

            while (index < groups.Length)
            {
                if (groups[index].GetOffset() == expectedOffset)
                {
                    selected = groups[index];
                    break;
                }

                index += 1;
            }

            if (selected == null)
            {
                throw FontFormatException.Create(
                    "The VDMX groups are overlapping or have gaps.");
            }

            expectedOffset = CheckedFontMath.AddOffset(
                expectedOffset,
                selected.GetLength());
            selectedCount += 1;
        }

        if (expectedOffset != tableLength)
        {
            throw FontFormatException.Create("The VDMX table has trailing data.");
        }
    }
}
