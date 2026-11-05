namespace ttf;

internal class SfntMetaDataMap
{
    int tag;
    long dataOffset;
    int dataLength;
    FontDataSlice data;

    internal SfntMetaDataMap(
        int tag,
        long dataOffset,
        int dataLength,
        FontDataSlice data)
    {
        this.tag = tag;
        this.dataOffset = dataOffset;
        this.dataLength = dataLength;
        this.data = data;
    }

    internal int GetTag()
    {
        return tag;
    }

    internal long GetDataOffset()
    {
        return dataOffset;
    }

    internal int GetDataLength()
    {
        return dataLength;
    }

    internal byte[] CopyData()
    {
        return data.CopyBytes();
    }
}

internal class SfntMetaTable
{
    uint version;
    uint flags;
    SfntMetaDataMap[] dataMaps;

    internal SfntMetaTable(
        uint version,
        uint flags,
        SfntMetaDataMap[] dataMaps)
    {
        this.version = version;
        this.flags = flags;
        this.dataMaps = dataMaps;
    }

    internal uint GetVersion()
    {
        return version;
    }

    internal uint GetFlags()
    {
        return flags;
    }

    internal int GetDataMapCount()
    {
        return dataMaps.Length;
    }

    internal SfntMetaDataMap GetDataMap(int index)
    {
        return dataMaps[index];
    }

    internal SfntMetaDataMap? FindFirstDataMap(int tag)
    {
        int index = 0;

        while (index < dataMaps.Length)
        {
            if (dataMaps[index].GetTag() == tag)
            {
                return dataMaps[index];
            }

            index += 1;
        }

        return null;
    }
}

internal static class SfntMetaTableParser
{
    const uint CurrentVersion = 1;
    const int HeaderLength = 16;
    const int DataMapLength = 12;
    static readonly SfntTableParser parser = new SfntTableParser(
        FontTags.Meta,
        1,
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

    internal static SfntMetaTable? FindTable(SfntTableCache cache)
    {
        SfntParsedTableView? view = FindView(cache);

        if (view == null || !view.HasParsedData())
        {
            return null;
        }

        return (SfntMetaTable)view.GetParsedData();
    }

    static SfntTableVersion ReadVersion(SfntTableData table)
    {
        BigEndianReader reader = new BigEndianReader(table.GetDataSlice());
        uint version = reader.ReadUInt32();
        return new SfntTableVersion(
            version > int.MaxValue ? int.MaxValue : (int)version,
            0);
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
            throw FontFormatException.Create("The meta table header is truncated.");
        }

        BigEndianReader reader = new BigEndianReader(tableData);
        uint encodedVersion = reader.ReadUInt32();
        uint flags = reader.ReadUInt32();
        uint reserved = reader.ReadUInt32();
        uint encodedCount = reader.ReadUInt32();

        if (encodedVersion != CurrentVersion)
        {
            throw FontFormatException.Create("The meta table version is not supported.");
        }

        if (flags != 0 || reserved != 0)
        {
            throw FontFormatException.Create("A reserved meta header field is nonzero.");
        }

        if (encodedCount > int.MaxValue ||
            encodedCount > (uint)limits.GetMaxCoverageEntries())
        {
            throw FontLimitException.Create(
                "The meta data-map count exceeds the configured coverage limit.");
        }

        int count = (int)encodedCount;
        int recordsLength = CheckedFontMath.MultiplyCount(count, DataMapLength);
        int dataStart = CheckedFontMath.AddOffset(HeaderLength, recordsLength);

        if (dataStart > tableData.GetLength())
        {
            throw FontFormatException.Create("The meta data-map array is truncated.");
        }

        SfntMetaDataMap[] maps = new SfntMetaDataMap[count];
        int index = 0;

        while (index < count)
        {
            int tag = reader.ReadTag();
            ValidateTag(tag);
            uint encodedOffset = reader.ReadUInt32();
            uint encodedLength = reader.ReadUInt32();

            if (encodedOffset > int.MaxValue || encodedLength > int.MaxValue)
            {
                throw FontFormatException.Create("A meta data region is too large.");
            }

            int offset = (int)encodedOffset;
            int length = (int)encodedLength;

            if (offset < dataStart || offset > tableData.GetLength() - length)
            {
                throw FontFormatException.Create("A meta data region is out of bounds.");
            }

            maps[index] = new SfntMetaDataMap(
                tag,
                encodedOffset,
                length,
                tableData.GetSlice(offset, length));
            index += 1;
        }

        return new SfntMetaTable(encodedVersion, flags, maps);
    }

    static void ValidateTag(int tag)
    {
        uint value = CheckedFontMath.ReinterpretInt32AsUInt32(tag);
        int index = 0;
        bool foundSpace = false;

        while (index < 4)
        {
            int character = (int)((value >> (24 - index * 8)) & 0xFFU);
            bool letter = (character >= 0x41 && character <= 0x5A) ||
                (character >= 0x61 && character <= 0x7A);
            bool digit = character >= 0x30 && character <= 0x39;

            if (index == 0 && !letter)
            {
                throw FontFormatException.Create("A meta data-map tag is invalid.");
            }

            if (character == 0x20)
            {
                foundSpace = true;
            }
            else if (!letter && !digit)
            {
                throw FontFormatException.Create("A meta data-map tag is invalid.");
            }
            else if (foundSpace)
            {
                throw FontFormatException.Create("A meta data-map tag has an embedded space.");
            }

            index += 1;
        }
    }
}
