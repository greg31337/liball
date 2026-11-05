namespace ttf;

internal class SfntGaspRange
{
    int maximumPpem;
    int behavior;

    internal SfntGaspRange(int maximumPpem, int behavior)
    {
        this.maximumPpem = maximumPpem;
        this.behavior = behavior;
    }

    internal int GetMaximumPpem()
    {
        return maximumPpem;
    }

    internal int GetBehavior()
    {
        return behavior;
    }

    internal bool UsesGridFitting()
    {
        return (behavior & 0x0001) != 0;
    }

    internal bool UsesGrayscaleRendering()
    {
        return (behavior & 0x0002) != 0;
    }

    internal bool UsesSymmetricGridFitting()
    {
        return (behavior & 0x0004) != 0;
    }

    internal bool UsesSymmetricSmoothing()
    {
        return (behavior & 0x0008) != 0;
    }
}

internal class SfntGaspTable
{
    int version;
    SfntGaspRange[] ranges;

    internal SfntGaspTable(int version, SfntGaspRange[] ranges)
    {
        this.version = version;
        this.ranges = ranges;
    }

    internal int GetVersion()
    {
        return version;
    }

    internal int GetRangeCount()
    {
        return ranges.Length;
    }

    internal SfntGaspRange GetRange(int index)
    {
        return ranges[index];
    }

    internal SfntGaspRange? FindRange(int ppem)
    {
        if (ppem < 0 || ppem > 0xFFFF)
        {
            throw new System.ArgumentOutOfRangeException("ppem");
        }

        int low = 0;
        int high = ranges.Length - 1;

        while (low <= high)
        {
            int middle = low + (high - low) / 2;

            if (ppem <= ranges[middle].GetMaximumPpem())
            {
                if (middle == 0 ||
                    ppem > ranges[middle - 1].GetMaximumPpem())
                {
                    return ranges[middle];
                }

                high = middle - 1;
            }
            else
            {
                low = middle + 1;
            }
        }

        return null;
    }
}

internal static class SfntGaspTableParser
{
    const int HeaderLength = 4;
    const int RangeLength = 4;
    static readonly SfntTableParser parser = new SfntTableParser(
        FontTags.Gasp,
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

    internal static SfntGaspTable? FindTable(SfntTableCache cache)
    {
        SfntParsedTableView? view = FindView(cache);

        if (view == null || !view.HasParsedData())
        {
            return null;
        }

        return (SfntGaspTable)view.GetParsedData();
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
            throw FontFormatException.Create("The gasp table header is truncated.");
        }

        BigEndianReader reader = new BigEndianReader(tableData);
        int encodedVersion = reader.ReadUInt16();
        int rangeCount = reader.ReadUInt16();

        if (encodedVersion != 0 && encodedVersion != 1)
        {
            throw FontFormatException.Create("The gasp table version is not supported.");
        }

        if (rangeCount == 0)
        {
            throw FontFormatException.Create("The gasp table declares no ranges.");
        }

        if (rangeCount > limits.GetMaxCoverageEntries())
        {
            throw FontLimitException.Create(
                "The gasp range count exceeds the configured coverage limit.");
        }

        int expectedLength = CheckedFontMath.GetRangeEnd(
            HeaderLength,
            rangeCount,
            RangeLength);

        if (tableData.GetLength() != expectedLength)
        {
            throw FontFormatException.Create("The gasp table length is invalid.");
        }

        SfntGaspRange[] ranges = new SfntGaspRange[rangeCount];
        int previousMaximum = -1;
        int index = 0;

        while (index < rangeCount)
        {
            int maximumPpem = reader.ReadUInt16();
            int behavior = reader.ReadUInt16();

            if (maximumPpem <= previousMaximum)
            {
                throw FontFormatException.Create(
                    "The gasp ranges are not strictly ordered.");
            }

            if ((behavior & 0xFFF0) != 0 ||
                (encodedVersion == 0 && (behavior & 0x000C) != 0))
            {
                throw FontFormatException.Create(
                    "A gasp range has behavior flags invalid for its version.");
            }

            ranges[index] = new SfntGaspRange(maximumPpem, behavior);
            previousMaximum = maximumPpem;
            index += 1;
        }

        if (previousMaximum != 0xFFFF)
        {
            throw FontFormatException.Create(
                "The final gasp range does not use the required sentinel.");
        }

        return new SfntGaspTable(encodedVersion, ranges);
    }
}
