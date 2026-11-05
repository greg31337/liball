namespace ttf;

internal class SfntOs2Table
{
    int version;
    int averageCharacterWidth;
    int weightClass;
    int widthClass;
    int embeddingFlags;
    int subscriptXSize;
    int subscriptYSize;
    int subscriptXOffset;
    int subscriptYOffset;
    int superscriptXSize;
    int superscriptYSize;
    int superscriptXOffset;
    int superscriptYOffset;
    int strikeoutSize;
    int strikeoutPosition;
    int familyClassValue;
    byte[] panose;
    uint[] unicodeRanges;
    uint vendorId;
    int selectionFlags;
    int firstCharacterIndex;
    int lastCharacterIndex;
    bool hasTypographicMetrics;
    int typographicAscender;
    int typographicDescender;
    int typographicLineGap;
    int windowsAscent;
    int windowsDescent;
    uint[] codePageRanges;
    bool hasVersion2Metrics;
    int xHeight;
    int capHeight;
    int defaultCharacter;
    int breakCharacter;
    int maximumContext;
    bool hasOpticalPointSizes;
    int lowerOpticalPointSize;
    int upperOpticalPointSize;
    int extensionLength;
    SfntEmbeddingRightsReport embeddingRights;

    internal SfntOs2Table(
        int version,
        int averageCharacterWidth,
        int weightClass,
        int widthClass,
        int embeddingFlags,
        int subscriptXSize,
        int subscriptYSize,
        int subscriptXOffset,
        int subscriptYOffset,
        int superscriptXSize,
        int superscriptYSize,
        int superscriptXOffset,
        int superscriptYOffset,
        int strikeoutSize,
        int strikeoutPosition,
        int familyClassValue,
        byte[] panose,
        uint[] unicodeRanges,
        uint vendorId,
        int selectionFlags,
        int firstCharacterIndex,
        int lastCharacterIndex,
        bool hasTypographicMetrics,
        int typographicAscender,
        int typographicDescender,
        int typographicLineGap,
        int windowsAscent,
        int windowsDescent,
        uint[] codePageRanges,
        bool hasVersion2Metrics,
        int xHeight,
        int capHeight,
        int defaultCharacter,
        int breakCharacter,
        int maximumContext,
        bool hasOpticalPointSizes,
        int lowerOpticalPointSize,
        int upperOpticalPointSize,
        int extensionLength)
    {
        this.version = version;
        this.averageCharacterWidth = averageCharacterWidth;
        this.weightClass = weightClass;
        this.widthClass = widthClass;
        this.embeddingFlags = embeddingFlags;
        this.subscriptXSize = subscriptXSize;
        this.subscriptYSize = subscriptYSize;
        this.subscriptXOffset = subscriptXOffset;
        this.subscriptYOffset = subscriptYOffset;
        this.superscriptXSize = superscriptXSize;
        this.superscriptYSize = superscriptYSize;
        this.superscriptXOffset = superscriptXOffset;
        this.superscriptYOffset = superscriptYOffset;
        this.strikeoutSize = strikeoutSize;
        this.strikeoutPosition = strikeoutPosition;
        this.familyClassValue = familyClassValue;
        this.panose = panose;
        this.unicodeRanges = unicodeRanges;
        this.vendorId = vendorId;
        this.selectionFlags = selectionFlags;
        this.firstCharacterIndex = firstCharacterIndex;
        this.lastCharacterIndex = lastCharacterIndex;
        this.hasTypographicMetrics = hasTypographicMetrics;
        this.typographicAscender = typographicAscender;
        this.typographicDescender = typographicDescender;
        this.typographicLineGap = typographicLineGap;
        this.windowsAscent = windowsAscent;
        this.windowsDescent = windowsDescent;
        this.codePageRanges = codePageRanges;
        this.hasVersion2Metrics = hasVersion2Metrics;
        this.xHeight = xHeight;
        this.capHeight = capHeight;
        this.defaultCharacter = defaultCharacter;
        this.breakCharacter = breakCharacter;
        this.maximumContext = maximumContext;
        this.hasOpticalPointSizes = hasOpticalPointSizes;
        this.lowerOpticalPointSize = lowerOpticalPointSize;
        this.upperOpticalPointSize = upperOpticalPointSize;
        this.extensionLength = extensionLength;
        embeddingRights = SfntEmbeddingRightsInterpreter.Interpret(
            version,
            embeddingFlags);
    }

    internal int GetVersion()
    {
        return version;
    }

    internal int GetAverageCharacterWidth()
    {
        return averageCharacterWidth;
    }

    internal int GetWeightClass()
    {
        return weightClass;
    }

    internal int GetWidthClass()
    {
        return widthClass;
    }

    internal int GetEmbeddingFlags()
    {
        return embeddingFlags;
    }

    internal bool HasEmbeddingFlag(int bitIndex)
    {
        ValidateBitIndex(bitIndex, 15);
        return (embeddingFlags & 1 << bitIndex) != 0;
    }

    internal int GetSubscriptXSize()
    {
        return subscriptXSize;
    }

    internal int GetSubscriptYSize()
    {
        return subscriptYSize;
    }

    internal int GetSubscriptXOffset()
    {
        return subscriptXOffset;
    }

    internal int GetSubscriptYOffset()
    {
        return subscriptYOffset;
    }

    internal int GetSuperscriptXSize()
    {
        return superscriptXSize;
    }

    internal int GetSuperscriptYSize()
    {
        return superscriptYSize;
    }

    internal int GetSuperscriptXOffset()
    {
        return superscriptXOffset;
    }

    internal int GetSuperscriptYOffset()
    {
        return superscriptYOffset;
    }

    internal int GetStrikeoutSize()
    {
        return strikeoutSize;
    }

    internal int GetStrikeoutPosition()
    {
        return strikeoutPosition;
    }

    internal int GetFamilyClassValue()
    {
        return familyClassValue;
    }

    internal int GetFamilyClass()
    {
        return familyClassValue >> 8 & 0xFF;
    }

    internal int GetFamilySubclass()
    {
        return familyClassValue & 0xFF;
    }

    internal int GetPanoseValue(int index)
    {
        if (index < 0 || index >= panose.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return panose[index];
    }

    internal uint GetUnicodeRange(int index)
    {
        if (index < 0 || index >= unicodeRanges.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return unicodeRanges[index];
    }

    internal bool HasUnicodeRange(int bitIndex)
    {
        ValidateBitIndex(bitIndex, 127);
        int wordIndex = bitIndex / 32;
        int wordBit = bitIndex % 32;
        return (unicodeRanges[wordIndex] & 1U << wordBit) != 0;
    }

    internal uint GetVendorId()
    {
        return vendorId;
    }

    internal int GetSelectionFlags()
    {
        return selectionFlags;
    }

    internal bool HasSelectionFlag(int bitIndex)
    {
        ValidateBitIndex(bitIndex, 15);
        return (selectionFlags & 1 << bitIndex) != 0;
    }

    internal bool UsesTypographicMetrics()
    {
        return HasSelectionFlag(7);
    }

    internal int GetFirstCharacterIndex()
    {
        return firstCharacterIndex;
    }

    internal int GetLastCharacterIndex()
    {
        return lastCharacterIndex;
    }

    internal bool HasTypographicMetrics()
    {
        return hasTypographicMetrics;
    }

    internal int GetTypographicAscender()
    {
        EnsureTypographicMetrics();
        return typographicAscender;
    }

    internal int GetTypographicDescender()
    {
        EnsureTypographicMetrics();
        return typographicDescender;
    }

    internal int GetTypographicLineGap()
    {
        EnsureTypographicMetrics();
        return typographicLineGap;
    }

    internal int GetWindowsAscent()
    {
        EnsureTypographicMetrics();
        return windowsAscent;
    }

    internal int GetWindowsDescent()
    {
        EnsureTypographicMetrics();
        return windowsDescent;
    }

    internal bool HasCodePageRanges()
    {
        return codePageRanges.Length != 0;
    }

    internal uint GetCodePageRange(int index)
    {
        EnsureCodePageRanges();

        if (index < 0 || index >= codePageRanges.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return codePageRanges[index];
    }

    internal bool HasCodePageRange(int bitIndex)
    {
        EnsureCodePageRanges();
        ValidateBitIndex(bitIndex, 63);
        int wordIndex = bitIndex / 32;
        int wordBit = bitIndex % 32;
        return (codePageRanges[wordIndex] & 1U << wordBit) != 0;
    }

    internal bool HasVersion2Metrics()
    {
        return hasVersion2Metrics;
    }

    internal int GetXHeight()
    {
        EnsureVersion2Metrics();
        return xHeight;
    }

    internal int GetCapHeight()
    {
        EnsureVersion2Metrics();
        return capHeight;
    }

    internal int GetDefaultCharacter()
    {
        EnsureVersion2Metrics();
        return defaultCharacter;
    }

    internal int GetBreakCharacter()
    {
        EnsureVersion2Metrics();
        return breakCharacter;
    }

    internal int GetMaximumContext()
    {
        EnsureVersion2Metrics();
        return maximumContext;
    }

    internal bool HasOpticalPointSizes()
    {
        return hasOpticalPointSizes;
    }

    internal int GetLowerOpticalPointSize()
    {
        EnsureOpticalPointSizes();
        return lowerOpticalPointSize;
    }

    internal int GetUpperOpticalPointSize()
    {
        EnsureOpticalPointSizes();
        return upperOpticalPointSize;
    }

    internal double GetLowerOpticalPointSizeInPoints()
    {
        return GetLowerOpticalPointSize() / 20.0;
    }

    internal double GetUpperOpticalPointSizeInPoints()
    {
        int value = GetUpperOpticalPointSize();

        if (value == 0xFFFF)
        {
            return double.PositiveInfinity;
        }

        return value / 20.0;
    }

    internal int GetExtensionLength()
    {
        return extensionLength;
    }

    internal SfntEmbeddingRightsReport GetEmbeddingRights()
    {
        return embeddingRights;
    }

    internal void VerifyHeadStyle(SfntHeadTable head)
    {
        if (head == null)
        {
            throw new System.ArgumentNullException("head");
        }

        bool italic = HasSelectionFlag(0);
        bool bold = HasSelectionFlag(5);

        if (italic != head.HasMacStyle(1) ||
            bold != head.HasMacStyle(0))
        {
            throw FontFormatException.Create(
                "The OS/2 selection style flags do not match head.macStyle.");
        }
    }

    void EnsureTypographicMetrics()
    {
        if (!hasTypographicMetrics)
        {
            throw new System.InvalidOperationException(
                "The shortened version 0 OS/2 table has no typographic metrics.");
        }
    }

    void EnsureCodePageRanges()
    {
        if (codePageRanges.Length == 0)
        {
            throw new System.InvalidOperationException(
                "This OS/2 version has no code-page range fields.");
        }
    }

    void EnsureVersion2Metrics()
    {
        if (!hasVersion2Metrics)
        {
            throw new System.InvalidOperationException(
                "This OS/2 version has no version 2 metric fields.");
        }
    }

    void EnsureOpticalPointSizes()
    {
        if (!hasOpticalPointSizes)
        {
            throw new System.InvalidOperationException(
                "This OS/2 version has no optical point-size fields.");
        }
    }

    static void ValidateBitIndex(int bitIndex, int maximum)
    {
        if (bitIndex < 0 || bitIndex > maximum)
        {
            throw new System.ArgumentOutOfRangeException("bitIndex");
        }
    }
}

internal static class SfntOs2TableParser
{
    const int LegacyVersion0Length = 68;
    const int Version0Length = 78;
    const int Version1Length = 86;
    const int Version2Length = 96;
    const int Version5Length = 100;
    const int MaximumPublishedVersion = 5;
    const uint ReservedUnicodeRange4Mask = 0xF8000000U;
    const uint ReservedCodePageRange1Mask = 0x1FC0FE00U;
    const uint ReservedCodePageRange2Mask = 0x0000FFFFU;
    static readonly SfntTableParser parser = new SfntTableParser(
        FontTags.Os2,
        0,
        0,
        MaximumPublishedVersion,
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

    internal static SfntOs2Table? FindTable(SfntTableCache cache)
    {
        SfntParsedTableView? view = FindView(cache);

        if (view == null || !view.HasParsedData())
        {
            return null;
        }

        return (SfntOs2Table)view.GetParsedData();
    }

    static SfntTableVersion ReadVersion(SfntTableData table)
    {
        BigEndianReader reader = new BigEndianReader(table.GetDataSlice());
        return new SfntTableVersion(0, reader.ReadUInt16());
    }

    static object Parse(
        SfntTableData table,
        SfntTableVersion tableVersion,
        SfntTableVersionSupport support,
        FontLimits limits,
        SfntTableCache cache)
    {
        int version = tableVersion.GetMinorVersion();
        int tableLength = table.GetLength();
        int requiredLength = GetRequiredLength(version);
        bool legacyVersion0 = version == 0 && tableLength == LegacyVersion0Length;

        if (tableLength < requiredLength && !legacyVersion0)
        {
            throw FontFormatException.Create("The OS/2 table length is invalid for its version.");
        }

        if (version == 0 &&
            tableLength > LegacyVersion0Length &&
            tableLength < Version0Length)
        {
            throw FontFormatException.Create("The version 0 OS/2 table has a partial metric suffix.");
        }

        BigEndianReader reader = new BigEndianReader(table.GetDataSlice());
        int encodedVersion = reader.ReadUInt16();
        int averageCharacterWidth = reader.ReadInt16();
        int weightClass = reader.ReadUInt16();
        int widthClass = reader.ReadUInt16();
        int embeddingFlags = reader.ReadUInt16();
        int subscriptXSize = reader.ReadInt16();
        int subscriptYSize = reader.ReadInt16();
        int subscriptXOffset = reader.ReadInt16();
        int subscriptYOffset = reader.ReadInt16();
        int superscriptXSize = reader.ReadInt16();
        int superscriptYSize = reader.ReadInt16();
        int superscriptXOffset = reader.ReadInt16();
        int superscriptYOffset = reader.ReadInt16();
        int strikeoutSize = reader.ReadInt16();
        int strikeoutPosition = reader.ReadInt16();
        int familyClassValue = reader.ReadInt16();
        byte[] panose = new byte[10];
        int panoseIndex = 0;

        while (panoseIndex < panose.Length)
        {
            panose[panoseIndex] = reader.ReadUInt8();
            panoseIndex += 1;
        }

        uint[] unicodeRanges = new uint[4];
        int unicodeRangeIndex = 0;

        while (unicodeRangeIndex < unicodeRanges.Length)
        {
            unicodeRanges[unicodeRangeIndex] = reader.ReadUInt32();
            unicodeRangeIndex += 1;
        }

        uint vendorId = reader.ReadUInt32();
        int selectionFlags = reader.ReadUInt16();
        int firstCharacterIndex = reader.ReadUInt16();
        int lastCharacterIndex = reader.ReadUInt16();
        bool hasTypographicMetrics = !legacyVersion0;
        int typographicAscender = 0;
        int typographicDescender = 0;
        int typographicLineGap = 0;
        int windowsAscent = 0;
        int windowsDescent = 0;

        if (hasTypographicMetrics)
        {
            typographicAscender = reader.ReadInt16();
            typographicDescender = reader.ReadInt16();
            typographicLineGap = reader.ReadInt16();
            windowsAscent = reader.ReadUInt16();
            windowsDescent = reader.ReadUInt16();
        }

        uint[] codePageRanges = new uint[0];

        if (version >= 1)
        {
            codePageRanges = new uint[]
            {
                reader.ReadUInt32(),
                reader.ReadUInt32()
            };
        }

        bool hasVersion2Metrics = version >= 2;
        int xHeight = 0;
        int capHeight = 0;
        int defaultCharacter = 0;
        int breakCharacter = 0;
        int maximumContext = 0;

        if (hasVersion2Metrics)
        {
            xHeight = reader.ReadInt16();
            capHeight = reader.ReadInt16();
            defaultCharacter = reader.ReadUInt16();
            breakCharacter = reader.ReadUInt16();
            maximumContext = reader.ReadUInt16();
        }

        bool hasOpticalPointSizes = version >= 5;
        int lowerOpticalPointSize = 0;
        int upperOpticalPointSize = 0;

        if (hasOpticalPointSizes)
        {
            lowerOpticalPointSize = reader.ReadUInt16();
            upperOpticalPointSize = reader.ReadUInt16();
        }

        ValidateFields(
            encodedVersion,
            weightClass,
            widthClass,
            embeddingFlags,
            unicodeRanges,
            selectionFlags,
            firstCharacterIndex,
            lastCharacterIndex,
            codePageRanges,
            hasOpticalPointSizes,
            lowerOpticalPointSize,
            upperOpticalPointSize);

        int parsedLength = legacyVersion0
            ? LegacyVersion0Length
            : requiredLength;
        return new SfntOs2Table(
            encodedVersion,
            averageCharacterWidth,
            weightClass,
            widthClass,
            embeddingFlags,
            subscriptXSize,
            subscriptYSize,
            subscriptXOffset,
            subscriptYOffset,
            superscriptXSize,
            superscriptYSize,
            superscriptXOffset,
            superscriptYOffset,
            strikeoutSize,
            strikeoutPosition,
            familyClassValue,
            panose,
            unicodeRanges,
            vendorId,
            selectionFlags,
            firstCharacterIndex,
            lastCharacterIndex,
            hasTypographicMetrics,
            typographicAscender,
            typographicDescender,
            typographicLineGap,
            windowsAscent,
            windowsDescent,
            codePageRanges,
            hasVersion2Metrics,
            xHeight,
            capHeight,
            defaultCharacter,
            breakCharacter,
            maximumContext,
            hasOpticalPointSizes,
            lowerOpticalPointSize,
            upperOpticalPointSize,
            tableLength - parsedLength);
    }

    static int GetRequiredLength(int version)
    {
        if (version == 0)
        {
            return Version0Length;
        }

        if (version == 1)
        {
            return Version1Length;
        }

        if (version < 5)
        {
            return Version2Length;
        }

        return Version5Length;
    }

    static void ValidateFields(
        int version,
        int weightClass,
        int widthClass,
        int embeddingFlags,
        uint[] unicodeRanges,
        int selectionFlags,
        int firstCharacterIndex,
        int lastCharacterIndex,
        uint[] codePageRanges,
        bool hasOpticalPointSizes,
        int lowerOpticalPointSize,
        int upperOpticalPointSize)
    {
        if (weightClass < 1 || weightClass > 1000)
        {
            throw FontFormatException.Create("The OS/2 weight class is outside 1 through 1000.");
        }

        if (widthClass < 1 || widthClass > 9)
        {
            throw FontFormatException.Create("The OS/2 width class is outside 1 through 9.");
        }

        if ((unicodeRanges[3] & ReservedUnicodeRange4Mask) != 0)
        {
            throw FontFormatException.Create("A reserved OS/2 Unicode-range bit is set.");
        }

        ValidateEmbeddingFlags(version, embeddingFlags);
        ValidateSelectionFlags(version, selectionFlags);

        if (firstCharacterIndex > lastCharacterIndex)
        {
            throw FontFormatException.Create(
                "The OS/2 first character index exceeds the last character index.");
        }

        if (codePageRanges.Length != 0)
        {
            uint firstReservedMask = ReservedCodePageRange1Mask;

            if (version == 1)
            {
                firstReservedMask |= 0x00000100U;
            }

            if ((codePageRanges[0] & firstReservedMask) != 0 ||
                (codePageRanges[1] & ReservedCodePageRange2Mask) != 0)
            {
                throw FontFormatException.Create("A reserved OS/2 code-page-range bit is set.");
            }
        }

        if (hasOpticalPointSizes &&
            (upperOpticalPointSize < 2 ||
            lowerOpticalPointSize >= upperOpticalPointSize))
        {
            throw FontFormatException.Create("The OS/2 optical point-size range is invalid.");
        }
    }

    static void ValidateEmbeddingFlags(int version, int embeddingFlags)
    {
        if ((embeddingFlags & 1) != 0)
        {
            throw FontFormatException.Create("Reserved OS/2 embedding bit 0 is set.");
        }

        if (version >= 2 && (embeddingFlags & 0xFCF0) != 0)
        {
            throw FontFormatException.Create("A reserved OS/2 embedding flag is set.");
        }

        int permissionFlags = embeddingFlags & 0x000E;

        if (version >= 3 &&
            permissionFlags != 0 &&
            permissionFlags != 2 &&
            permissionFlags != 4 &&
            permissionFlags != 8)
        {
            throw FontFormatException.Create(
                "The OS/2 embedding permission flags are not mutually exclusive.");
        }
    }

    static void ValidateSelectionFlags(int version, int selectionFlags)
    {
        int reservedMask = version < 4 ? 0xFF80 : 0xFC00;

        if ((selectionFlags & reservedMask) != 0)
        {
            throw FontFormatException.Create("A reserved OS/2 selection flag is set.");
        }

        bool regular = (selectionFlags & 1 << 6) != 0;
        bool italic = (selectionFlags & 1) != 0;
        bool bold = (selectionFlags & 1 << 5) != 0;

        if (regular && (italic || bold))
        {
            throw FontFormatException.Create(
                "The OS/2 regular selection flag conflicts with italic or bold.");
        }
    }
}
