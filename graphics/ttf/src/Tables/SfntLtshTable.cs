namespace ttf;

internal class SfntLtshTable
{
    int version;
    int[] linearThresholds;

    internal SfntLtshTable(int version, int[] linearThresholds)
    {
        this.version = version;
        this.linearThresholds = linearThresholds;
    }

    internal int GetVersion()
    {
        return version;
    }

    internal int GetGlyphCount()
    {
        return linearThresholds.Length;
    }

    internal int GetLinearThreshold(int glyphId)
    {
        return linearThresholds[glyphId];
    }

    internal bool IsLinearAtPpem(int glyphId, int ppem)
    {
        if (ppem < 0)
        {
            throw new System.ArgumentOutOfRangeException("ppem");
        }

        return ppem >= linearThresholds[glyphId];
    }
}

internal static class SfntLtshTableParser
{
    const int HeaderLength = 4;
    static readonly SfntTableParser parser = new SfntTableParser(
        FontTags.Ltsh,
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

    internal static SfntLtshTable? FindTable(SfntTableCache cache)
    {
        SfntParsedTableView? view = FindView(cache);

        if (view == null || !view.HasParsedData())
        {
            return null;
        }

        return (SfntLtshTable)view.GetParsedData();
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
            throw FontFormatException.Create("The LTSH table header is truncated.");
        }

        SfntMaxpTable? maxp = SfntMaxpTableParser.FindTable(cache);

        if (maxp == null)
        {
            throw FontFormatException.Create(
                "The LTSH table requires a supported maxp table.");
        }

        BigEndianReader reader = new BigEndianReader(tableData);
        int encodedVersion = reader.ReadUInt16();
        int glyphCount = reader.ReadUInt16();

        if (encodedVersion != 0)
        {
            throw FontFormatException.Create("The LTSH table version is not supported.");
        }

        if (glyphCount != maxp.GetGlyphCount())
        {
            throw FontFormatException.Create(
                "The LTSH glyph count does not match the maxp table.");
        }

        int expectedLength = CheckedFontMath.AddCounts(HeaderLength, glyphCount);

        if (tableData.GetLength() != expectedLength)
        {
            throw FontFormatException.Create("The LTSH table length is invalid.");
        }

        int[] thresholds = new int[glyphCount];
        int glyphId = 0;

        while (glyphId < glyphCount)
        {
            thresholds[glyphId] = reader.ReadUInt8();
            glyphId += 1;
        }

        return new SfntLtshTable(encodedVersion, thresholds);
    }
}
