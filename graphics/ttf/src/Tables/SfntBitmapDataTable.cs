namespace ttf;

internal class SfntBitmapDataTable
{
    int majorVersion;
    int minorVersion;
    bool color;
    FontDataSlice data;

    internal SfntBitmapDataTable(
        int majorVersion,
        int minorVersion,
        bool color,
        FontDataSlice data)
    {
        this.majorVersion = majorVersion;
        this.minorVersion = minorVersion;
        this.color = color;
        this.data = data;
    }

    internal int GetMajorVersion()
    {
        return majorVersion;
    }

    internal int GetMinorVersion()
    {
        return minorVersion;
    }

    internal bool IsColor()
    {
        return color;
    }

    internal int GetLength()
    {
        return data.GetLength();
    }

    internal FontDataSlice GetSlice(int offset, int length)
    {
        return data.GetSlice(offset, length);
    }
}

internal static class SfntEbdtTableParser
{
    static readonly SfntTableParser parser = new SfntTableParser(
        FontTags.Ebdt,
        2,
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

    internal static SfntBitmapDataTable? FindTable(SfntTableCache cache)
    {
        SfntParsedTableView? view = FindView(cache);

        if (view == null || !view.HasParsedData())
        {
            return null;
        }

        return (SfntBitmapDataTable)view.GetParsedData();
    }

    static SfntTableVersion ReadVersion(SfntTableData table)
    {
        return SfntBitmapDataTableParser.ReadVersion(table);
    }

    static object Parse(
        SfntTableData table,
        SfntTableVersion version,
        SfntTableVersionSupport support,
        FontLimits limits,
        SfntTableCache cache)
    {
        return SfntBitmapDataTableParser.Parse(table, 2, false);
    }
}

internal static class SfntCbdtTableParser
{
    static readonly SfntTableParser parser = new SfntTableParser(
        FontTags.Cbdt,
        3,
        3,
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

    internal static SfntBitmapDataTable? FindTable(SfntTableCache cache)
    {
        SfntParsedTableView? view = FindView(cache);

        if (view == null || !view.HasParsedData())
        {
            return null;
        }

        return (SfntBitmapDataTable)view.GetParsedData();
    }

    static SfntTableVersion ReadVersion(SfntTableData table)
    {
        return SfntBitmapDataTableParser.ReadVersion(table);
    }

    static object Parse(
        SfntTableData table,
        SfntTableVersion version,
        SfntTableVersionSupport support,
        FontLimits limits,
        SfntTableCache cache)
    {
        return SfntBitmapDataTableParser.Parse(table, 3, true);
    }
}

internal static class SfntBitmapDataTableParser
{
    internal static SfntTableVersion ReadVersion(SfntTableData table)
    {
        BigEndianReader reader = new BigEndianReader(table.GetDataSlice());
        return new SfntTableVersion(
            reader.ReadUInt16(),
            reader.ReadUInt16());
    }

    internal static SfntBitmapDataTable Parse(
        SfntTableData table,
        int expectedMajorVersion,
        bool color)
    {
        FontDataSlice data = table.GetDataSlice();

        if (data.GetLength() < 4)
        {
            throw FontFormatException.Create(
                "An embedded bitmap data table header is truncated.");
        }

        BigEndianReader reader = new BigEndianReader(data);
        int majorVersion = reader.ReadUInt16();
        int minorVersion = reader.ReadUInt16();

        if (majorVersion != expectedMajorVersion || minorVersion != 0)
        {
            throw FontFormatException.Create(
                "An embedded bitmap data table version is not supported.");
        }

        return new SfntBitmapDataTable(
            majorVersion,
            minorVersion,
            color,
            data);
    }
}
