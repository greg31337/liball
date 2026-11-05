namespace ttf;

internal class SfntBitmapScale
{
    SfntBitmapLineMetrics horizontalLineMetrics;
    SfntBitmapLineMetrics verticalLineMetrics;
    int pixelsPerEmX;
    int pixelsPerEmY;
    int substitutePixelsPerEmX;
    int substitutePixelsPerEmY;

    internal SfntBitmapScale(
        SfntBitmapLineMetrics horizontalLineMetrics,
        SfntBitmapLineMetrics verticalLineMetrics,
        int pixelsPerEmX,
        int pixelsPerEmY,
        int substitutePixelsPerEmX,
        int substitutePixelsPerEmY)
    {
        this.horizontalLineMetrics = horizontalLineMetrics;
        this.verticalLineMetrics = verticalLineMetrics;
        this.pixelsPerEmX = pixelsPerEmX;
        this.pixelsPerEmY = pixelsPerEmY;
        this.substitutePixelsPerEmX = substitutePixelsPerEmX;
        this.substitutePixelsPerEmY = substitutePixelsPerEmY;
    }

    internal SfntBitmapLineMetrics GetHorizontalLineMetrics()
    {
        return horizontalLineMetrics;
    }

    internal SfntBitmapLineMetrics GetVerticalLineMetrics()
    {
        return verticalLineMetrics;
    }

    internal int GetPixelsPerEmX()
    {
        return pixelsPerEmX;
    }

    internal int GetPixelsPerEmY()
    {
        return pixelsPerEmY;
    }

    internal int GetSubstitutePixelsPerEmX()
    {
        return substitutePixelsPerEmX;
    }

    internal int GetSubstitutePixelsPerEmY()
    {
        return substitutePixelsPerEmY;
    }
}

internal class SfntEbscTable
{
    SfntBitmapScale[] scales;

    internal SfntEbscTable(SfntBitmapScale[] scales)
    {
        this.scales = scales;
    }

    internal int GetScaleCount()
    {
        return scales.Length;
    }

    internal SfntBitmapScale GetScale(int index)
    {
        if (index < 0 || index >= scales.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return scales[index];
    }

    internal SfntBitmapScale? FindScale(int pixelsPerEmX, int pixelsPerEmY)
    {
        int index = 0;

        while (index < scales.Length)
        {
            if (scales[index].GetPixelsPerEmX() == pixelsPerEmX &&
                scales[index].GetPixelsPerEmY() == pixelsPerEmY)
            {
                return scales[index];
            }

            index += 1;
        }

        return null;
    }
}

internal static class SfntEbscTableParser
{
    const int HeaderLength = 8;
    const int ScaleLength = 28;
    static readonly SfntTableParser parser = new SfntTableParser(
        FontTags.Ebsc,
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

    internal static SfntEbscTable? FindTable(SfntTableCache cache)
    {
        SfntParsedTableView? view = FindView(cache);

        if (view == null || !view.HasParsedData())
        {
            return null;
        }

        return (SfntEbscTable)view.GetParsedData();
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
        FontDataSlice data = table.GetDataSlice();

        if (data.GetLength() < HeaderLength)
        {
            throw FontFormatException.Create("The EBSC table header is truncated.");
        }

        BigEndianReader reader = new BigEndianReader(data);
        int majorVersion = reader.ReadUInt16();
        int minorVersion = reader.ReadUInt16();
        uint scaleCountValue = reader.ReadUInt32();

        if (majorVersion != 2 || minorVersion != 0)
        {
            throw FontFormatException.Create("The EBSC table version is not supported.");
        }

        if (scaleCountValue > int.MaxValue ||
            scaleCountValue > limits.GetMaxCoverageEntries())
        {
            throw FontLimitException.Create(
                "The EBSC scale count exceeds the configured coverage-entry limit.");
        }

        int scaleCount = (int)scaleCountValue;
        int requiredLength = CheckedFontMath.AddOffset(
            HeaderLength,
            CheckedFontMath.MultiplyCount(scaleCount, ScaleLength));

        if (requiredLength > data.GetLength())
        {
            throw FontFormatException.Create("The EBSC scale array is truncated.");
        }

        SfntBitmapLocationTable? eblc = SfntEblcTableParser.FindTable(cache);

        if (eblc == null)
        {
            throw FontFormatException.Create(
                "The EBSC table requires parsed EBLC and EBDT tables.");
        }

        SfntBitmapScale[] scales = new SfntBitmapScale[scaleCount];
        int index = 0;

        while (index < scales.Length)
        {
            SfntBitmapLineMetrics horizontal =
                SfntBitmapLocationTableParser.ReadLineMetrics(reader);
            SfntBitmapLineMetrics vertical =
                SfntBitmapLocationTableParser.ReadLineMetrics(reader);
            int pixelsPerEmX = reader.ReadUInt8();
            int pixelsPerEmY = reader.ReadUInt8();
            int substitutePixelsPerEmX = reader.ReadUInt8();
            int substitutePixelsPerEmY = reader.ReadUInt8();

            if (pixelsPerEmX == 0 || pixelsPerEmY == 0 ||
                substitutePixelsPerEmX == 0 || substitutePixelsPerEmY == 0)
            {
                throw FontFormatException.Create("An EBSC pixels-per-em value is zero.");
            }

            if (eblc.FindStrike(
                substitutePixelsPerEmX,
                substitutePixelsPerEmY) == null)
            {
                throw FontFormatException.Create(
                    "An EBSC scale references a missing substitute strike.");
            }

            int previousIndex = 0;
            while (previousIndex < index)
            {
                if (scales[previousIndex].GetPixelsPerEmX() == pixelsPerEmX &&
                    scales[previousIndex].GetPixelsPerEmY() == pixelsPerEmY)
                {
                    throw FontFormatException.Create(
                        "The EBSC table contains duplicate target sizes.");
                }
                previousIndex += 1;
            }

            scales[index] = new SfntBitmapScale(
                horizontal,
                vertical,
                pixelsPerEmX,
                pixelsPerEmY,
                substitutePixelsPerEmX,
                substitutePixelsPerEmY);
            index += 1;
        }

        return new SfntEbscTable(scales);
    }
}
