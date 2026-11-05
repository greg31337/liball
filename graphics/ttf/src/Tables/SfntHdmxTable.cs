namespace ttf;

internal class SfntHdmxRecord
{
    int pixelSize;
    int maximumWidth;
    int[] widths;

    internal SfntHdmxRecord(int pixelSize, int maximumWidth, int[] widths)
    {
        this.pixelSize = pixelSize;
        this.maximumWidth = maximumWidth;
        this.widths = widths;
    }

    internal int GetPixelSize()
    {
        return pixelSize;
    }

    internal int GetMaximumWidth()
    {
        return maximumWidth;
    }

    internal int GetGlyphCount()
    {
        return widths.Length;
    }

    internal int GetWidth(int glyphId)
    {
        return widths[glyphId];
    }
}

internal class SfntHdmxTable
{
    int version;
    int deviceRecordSize;
    SfntHdmxRecord[] records;

    internal SfntHdmxTable(
        int version,
        int deviceRecordSize,
        SfntHdmxRecord[] records)
    {
        this.version = version;
        this.deviceRecordSize = deviceRecordSize;
        this.records = records;
    }

    internal int GetVersion()
    {
        return version;
    }

    internal int GetDeviceRecordSize()
    {
        return deviceRecordSize;
    }

    internal int GetRecordCount()
    {
        return records.Length;
    }

    internal SfntHdmxRecord GetRecord(int index)
    {
        return records[index];
    }

    internal SfntHdmxRecord? FindRecord(int pixelSize)
    {
        if (pixelSize < 0 || pixelSize > 0xFF)
        {
            throw new System.ArgumentOutOfRangeException("pixelSize");
        }

        int low = 0;
        int high = records.Length - 1;

        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            int candidate = records[middle].GetPixelSize();

            if (candidate == pixelSize)
            {
                return records[middle];
            }

            if (candidate < pixelSize)
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

internal static class SfntHdmxTableParser
{
    const int HeaderLength = 8;
    static readonly SfntTableParser parser = new SfntTableParser(
        FontTags.Hdmx,
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

    internal static SfntHdmxTable? FindTable(SfntTableCache cache)
    {
        SfntParsedTableView? view = FindView(cache);

        if (view == null || !view.HasParsedData())
        {
            return null;
        }

        return (SfntHdmxTable)view.GetParsedData();
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
            throw FontFormatException.Create("The hdmx table header is truncated.");
        }

        SfntMaxpTable? maxp = SfntMaxpTableParser.FindTable(cache);

        if (maxp == null)
        {
            throw FontFormatException.Create(
                "The hdmx table requires a supported maxp table.");
        }

        BigEndianReader reader = new BigEndianReader(tableData);
        int encodedVersion = reader.ReadUInt16();
        int recordCount = reader.ReadUInt16();
        uint encodedRecordSize = reader.ReadUInt32();

        if (encodedVersion != 0)
        {
            throw FontFormatException.Create("The hdmx table version is not supported.");
        }

        if (recordCount > limits.GetMaxCoverageEntries())
        {
            throw FontLimitException.Create(
                "The hdmx record count exceeds the configured coverage limit.");
        }

        int glyphCount = maxp.GetGlyphCount();
        int expectedRecordSize = CheckedFontMath.AlignFour(
            CheckedFontMath.AddCounts(2, glyphCount));

        if (encodedRecordSize != (uint)expectedRecordSize)
        {
            throw FontFormatException.Create("The hdmx device-record size is invalid.");
        }

        int recordsLength = CheckedFontMath.MultiplyCount(
            recordCount,
            expectedRecordSize);
        int expectedLength = CheckedFontMath.AddOffset(
            HeaderLength,
            recordsLength);

        if (tableData.GetLength() != expectedLength)
        {
            throw FontFormatException.Create("The hdmx table length is invalid.");
        }

        SfntHdmxRecord[] records = new SfntHdmxRecord[recordCount];
        int previousPixelSize = 0;
        int index = 0;

        while (index < recordCount)
        {
            int pixelSize = reader.ReadUInt8();
            int maximumWidth = reader.ReadUInt8();

            if (pixelSize == 0 || pixelSize <= previousPixelSize)
            {
                throw FontFormatException.Create(
                    "The hdmx records are not ordered by positive pixel size.");
            }

            int[] widths = new int[glyphCount];
            int calculatedMaximum = 0;
            int glyphId = 0;

            while (glyphId < glyphCount)
            {
                int width = reader.ReadUInt8();
                widths[glyphId] = width;

                if (width > calculatedMaximum)
                {
                    calculatedMaximum = width;
                }

                glyphId += 1;
            }

            if (maximumWidth != calculatedMaximum)
            {
                throw FontFormatException.Create(
                    "An hdmx maximum width does not match its glyph widths.");
            }

            int padding = expectedRecordSize - glyphCount - 2;

            while (padding > 0)
            {
                if (reader.ReadUInt8() != 0)
                {
                    throw FontFormatException.Create(
                        "An hdmx device record has nonzero padding.");
                }

                padding -= 1;
            }

            records[index] = new SfntHdmxRecord(
                pixelSize,
                maximumWidth,
                widths);
            previousPixelSize = pixelSize;
            index += 1;
        }

        return new SfntHdmxTable(
            encodedVersion,
            expectedRecordSize,
            records);
    }
}
