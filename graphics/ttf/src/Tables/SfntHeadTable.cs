namespace ttf;

internal enum SfntLocaFormat
{
    Short = 0,
    Long = 1
}

internal class SfntHeadTable
{
    int majorVersion;
    int minorVersion;
    int fontRevision;
    uint checksumAdjustment;
    uint magicNumber;
    int flags;
    int unitsPerEm;
    long createdSeconds;
    System.DateTime created;
    long modifiedSeconds;
    System.DateTime modified;
    int xMin;
    int yMin;
    int xMax;
    int yMax;
    int macStyle;
    int lowestRecommendedPpem;
    int fontDirectionHint;
    SfntLocaFormat locaFormat;
    int glyphDataFormat;
    int extensionLength;

    internal SfntHeadTable(
        int majorVersion,
        int minorVersion,
        int fontRevision,
        uint checksumAdjustment,
        uint magicNumber,
        int flags,
        int unitsPerEm,
        long createdSeconds,
        System.DateTime created,
        long modifiedSeconds,
        System.DateTime modified,
        int xMin,
        int yMin,
        int xMax,
        int yMax,
        int macStyle,
        int lowestRecommendedPpem,
        int fontDirectionHint,
        SfntLocaFormat locaFormat,
        int glyphDataFormat,
        int extensionLength)
    {
        this.majorVersion = majorVersion;
        this.minorVersion = minorVersion;
        this.fontRevision = fontRevision;
        this.checksumAdjustment = checksumAdjustment;
        this.magicNumber = magicNumber;
        this.flags = flags;
        this.unitsPerEm = unitsPerEm;
        this.createdSeconds = createdSeconds;
        this.created = created;
        this.modifiedSeconds = modifiedSeconds;
        this.modified = modified;
        this.xMin = xMin;
        this.yMin = yMin;
        this.xMax = xMax;
        this.yMax = yMax;
        this.macStyle = macStyle;
        this.lowestRecommendedPpem = lowestRecommendedPpem;
        this.fontDirectionHint = fontDirectionHint;
        this.locaFormat = locaFormat;
        this.glyphDataFormat = glyphDataFormat;
        this.extensionLength = extensionLength;
    }

    internal int GetMajorVersion()
    {
        return majorVersion;
    }

    internal int GetMinorVersion()
    {
        return minorVersion;
    }

    internal int GetFontRevisionRaw()
    {
        return fontRevision;
    }

    internal double GetFontRevision()
    {
        return fontRevision / 65536.0;
    }

    internal uint GetChecksumAdjustment()
    {
        return checksumAdjustment;
    }

    internal uint GetMagicNumber()
    {
        return magicNumber;
    }

    internal int GetFlags()
    {
        return flags;
    }

    internal bool HasFlag(int bitIndex)
    {
        if (bitIndex < 0 || bitIndex > 15)
        {
            throw new System.ArgumentOutOfRangeException("bitIndex");
        }

        return (flags & 1 << bitIndex) != 0;
    }

    internal int GetUnitsPerEm()
    {
        return unitsPerEm;
    }

    internal long GetCreatedSeconds()
    {
        return createdSeconds;
    }

    internal System.DateTime GetCreated()
    {
        return created;
    }

    internal long GetModifiedSeconds()
    {
        return modifiedSeconds;
    }

    internal System.DateTime GetModified()
    {
        return modified;
    }

    internal int GetXMin()
    {
        return xMin;
    }

    internal int GetYMin()
    {
        return yMin;
    }

    internal int GetXMax()
    {
        return xMax;
    }

    internal int GetYMax()
    {
        return yMax;
    }

    internal int GetMacStyle()
    {
        return macStyle;
    }

    internal bool HasMacStyle(int bitIndex)
    {
        if (bitIndex < 0 || bitIndex > 6)
        {
            throw new System.ArgumentOutOfRangeException("bitIndex");
        }

        return (macStyle & 1 << bitIndex) != 0;
    }

    internal int GetLowestRecommendedPpem()
    {
        return lowestRecommendedPpem;
    }

    internal int GetFontDirectionHint()
    {
        return fontDirectionHint;
    }

    internal SfntLocaFormat GetLocaFormat()
    {
        return locaFormat;
    }

    internal int GetGlyphDataFormat()
    {
        return glyphDataFormat;
    }

    internal int GetExtensionLength()
    {
        return extensionLength;
    }
}

internal static class SfntHeadTableParser
{
    const int RequiredLength = 54;
    const uint RequiredMagicNumber = 0x5F0F3CF5U;
    const int AllowedFlagsMask = 0x781F;
    const int AllowedMacStyleMask = 0x007F;
    static readonly SfntTableParser parser = new SfntTableParser(
        FontTags.Head,
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

    internal static SfntHeadTable? FindTable(SfntTableCache cache)
    {
        SfntParsedTableView? view = FindView(cache);

        if (view == null || !view.HasParsedData())
        {
            return null;
        }

        return (SfntHeadTable)view.GetParsedData();
    }

    static SfntTableVersion ReadVersion(SfntTableData table)
    {
        BigEndianReader reader = new BigEndianReader(table.GetDataSlice());
        return new SfntTableVersion(
            reader.ReadUInt16(),
            reader.ReadUInt16());
    }

    static object Parse(
        SfntTableData table,
        SfntTableVersion version,
        SfntTableVersionSupport support,
        FontLimits limits,
        SfntTableCache cache)
    {
        int tableLength = table.GetLength();

        if (tableLength < RequiredLength)
        {
            throw FontFormatException.Create("The head table is truncated.");
        }

        if (support == SfntTableVersionSupport.Supported &&
            tableLength != RequiredLength)
        {
            throw FontFormatException.Create("The head version 1.0 table length is invalid.");
        }

        BigEndianReader reader = new BigEndianReader(table.GetDataSlice());
        int majorVersion = reader.ReadUInt16();
        int minorVersion = reader.ReadUInt16();
        int fontRevision = reader.ReadInt32();
        uint checksumAdjustment = reader.ReadUInt32();
        uint magicNumber = reader.ReadUInt32();
        int flags = reader.ReadUInt16();
        int unitsPerEm = reader.ReadUInt16();
        long createdSeconds = reader.ReadLongDateTimeSeconds();
        System.DateTime created = ConvertTimestamp(createdSeconds);
        long modifiedSeconds = reader.ReadLongDateTimeSeconds();
        System.DateTime modified = ConvertTimestamp(modifiedSeconds);
        int xMin = reader.ReadInt16();
        int yMin = reader.ReadInt16();
        int xMax = reader.ReadInt16();
        int yMax = reader.ReadInt16();
        int macStyle = reader.ReadUInt16();
        int lowestRecommendedPpem = reader.ReadUInt16();
        int fontDirectionHint = reader.ReadInt16();
        int indexToLocFormat = reader.ReadInt16();
        int glyphDataFormat = reader.ReadInt16();

        ValidateFields(
            magicNumber,
            flags,
            unitsPerEm,
            xMin,
            yMin,
            xMax,
            yMax,
            macStyle,
            fontDirectionHint,
            indexToLocFormat,
            glyphDataFormat);

        return new SfntHeadTable(
            majorVersion,
            minorVersion,
            fontRevision,
            checksumAdjustment,
            magicNumber,
            flags,
            unitsPerEm,
            createdSeconds,
            created,
            modifiedSeconds,
            modified,
            xMin,
            yMin,
            xMax,
            yMax,
            macStyle,
            lowestRecommendedPpem,
            fontDirectionHint,
            (SfntLocaFormat)indexToLocFormat,
            glyphDataFormat,
            tableLength - RequiredLength);
    }

    static void ValidateFields(
        uint magicNumber,
        int flags,
        int unitsPerEm,
        int xMin,
        int yMin,
        int xMax,
        int yMax,
        int macStyle,
        int fontDirectionHint,
        int indexToLocFormat,
        int glyphDataFormat)
    {
        if (magicNumber != RequiredMagicNumber)
        {
            throw FontFormatException.Create("The head magicNumber value is invalid.");
        }

        if ((flags & ~AllowedFlagsMask) != 0)
        {
            throw FontFormatException.Create("The head flags field sets a reserved bit.");
        }

        if (unitsPerEm < 16 || unitsPerEm > 16384)
        {
            throw FontFormatException.Create("The head unitsPerEm value is outside the valid range.");
        }

        if (xMin > xMax || yMin > yMax)
        {
            throw FontFormatException.Create("The head global glyph bounds are invalid.");
        }

        if ((macStyle & ~AllowedMacStyleMask) != 0)
        {
            throw FontFormatException.Create("The head macStyle field sets a reserved bit.");
        }

        if (fontDirectionHint < -2 || fontDirectionHint > 2)
        {
            throw FontFormatException.Create("The head fontDirectionHint value is invalid.");
        }

        if (indexToLocFormat != 0 && indexToLocFormat != 1)
        {
            throw FontFormatException.Create("The head indexToLocFormat value is invalid.");
        }

        if (glyphDataFormat != 0)
        {
            throw FontFormatException.Create("The head glyphDataFormat value is invalid.");
        }
    }

    static System.DateTime ConvertTimestamp(long seconds)
    {
        if (seconds < -60052752000L || seconds > 255485145599L)
        {
            throw FontFormatException.Create(
                "A head LONGDATETIME value is outside the supported UTC date range.");
        }

        long ticks = 600527520000000000L + seconds * 10000000L;
        return new System.DateTime(ticks, System.DateTimeKind.Utc);
    }
}
