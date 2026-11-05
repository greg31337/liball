namespace ttf;

internal class SfntPcltTable
{
    int majorVersion;
    int minorVersion;
    uint fontNumber;
    int pitch;
    int xHeight;
    int style;
    int typeFamily;
    int capHeight;
    int symbolSet;
    int[] typeface;
    byte[] characterComplement;
    byte[] fileName;
    int strokeWeight;
    int widthType;
    int serifStyle;

    internal SfntPcltTable(
        int majorVersion,
        int minorVersion,
        uint fontNumber,
        int pitch,
        int xHeight,
        int style,
        int typeFamily,
        int capHeight,
        int symbolSet,
        int[] typeface,
        byte[] characterComplement,
        byte[] fileName,
        int strokeWeight,
        int widthType,
        int serifStyle)
    {
        this.majorVersion = majorVersion;
        this.minorVersion = minorVersion;
        this.fontNumber = fontNumber;
        this.pitch = pitch;
        this.xHeight = xHeight;
        this.style = style;
        this.typeFamily = typeFamily;
        this.capHeight = capHeight;
        this.symbolSet = symbolSet;
        this.typeface = typeface;
        this.characterComplement = characterComplement;
        this.fileName = fileName;
        this.strokeWeight = strokeWeight;
        this.widthType = widthType;
        this.serifStyle = serifStyle;
    }

    internal int GetMajorVersion() { return majorVersion; }
    internal int GetMinorVersion() { return minorVersion; }
    internal uint GetFontNumber() { return fontNumber; }
    internal int GetPitch() { return pitch; }
    internal int GetXHeight() { return xHeight; }
    internal int GetStyle() { return style; }
    internal int GetTypeFamily() { return typeFamily; }
    internal int GetCapHeight() { return capHeight; }
    internal int GetSymbolSet() { return symbolSet; }
    internal int GetTypefaceByte(int index) { return typeface[index]; }
    internal byte[] CopyCharacterComplement() { return characterComplement.AsSpan().ToArray(); }
    internal byte[] CopyFileName() { return fileName.AsSpan().ToArray(); }
    internal int GetStrokeWeight() { return strokeWeight; }
    internal int GetWidthType() { return widthType; }
    internal int GetSerifStyle() { return serifStyle; }
}

internal static class SfntPcltTableParser
{
    const int TableLength = 54;
    static readonly SfntTableParser parser = new SfntTableParser(
        FontTags.Pclt,
        1,
        1,
        0,
        ReadVersion,
        Parse);

    internal static SfntTableParser GetParser() { return parser; }

    internal static SfntParsedTableView? FindView(SfntTableCache cache)
    {
        if (cache == null) { throw new System.ArgumentNullException("cache"); }
        return cache.FindParsedTable(parser);
    }

    internal static SfntPcltTable? FindTable(SfntTableCache cache)
    {
        SfntParsedTableView? view = FindView(cache);
        if (view == null || !view.HasParsedData()) { return null; }
        return (SfntPcltTable)view.GetParsedData();
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
        if (data.GetLength() != TableLength)
        {
            throw FontFormatException.Create("The PCLT table length is invalid.");
        }

        BigEndianReader reader = new BigEndianReader(data);
        int majorVersion = reader.ReadUInt16();
        int minorVersion = reader.ReadUInt16();
        if (majorVersion != 1 || minorVersion != 0)
        {
            throw FontFormatException.Create("The PCLT table version is not supported.");
        }

        uint fontNumber = reader.ReadUInt32();
        int pitch = reader.ReadUInt16();
        int xHeight = reader.ReadUInt16();
        int style = reader.ReadUInt16();
        int typeFamily = reader.ReadUInt16();
        int capHeight = reader.ReadUInt16();
        int symbolSet = reader.ReadUInt16();
        int[] typeface = new int[16];
        int index = 0;
        while (index < typeface.Length)
        {
            typeface[index] = reader.ReadInt8();
            index += 1;
        }

        byte[] characterComplement = new byte[8];
        index = 0;
        while (index < characterComplement.Length)
        {
            characterComplement[index] = reader.ReadUInt8();
            index += 1;
        }

        byte[] fileName = new byte[6];
        index = 0;
        while (index < fileName.Length)
        {
            fileName[index] = reader.ReadUInt8();
            index += 1;
        }

        int strokeWeight = reader.ReadInt8();
        int widthType = reader.ReadInt8();
        int serifStyle = reader.ReadUInt8();
        int reserved = reader.ReadUInt8();
        if (reserved != 0)
        {
            throw FontFormatException.Create("The reserved PCLT byte is nonzero.");
        }

        return new SfntPcltTable(
            majorVersion,
            minorVersion,
            fontNumber,
            pitch,
            xHeight,
            style,
            typeFamily,
            capHeight,
            symbolSet,
            typeface,
            characterComplement,
            fileName,
            strokeWeight,
            widthType,
            serifStyle);
    }
}
