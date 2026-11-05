namespace ttf;

internal class SfntCpalColor
{
    int blue;
    int green;
    int red;
    int alpha;

    internal SfntCpalColor(int blue, int green, int red, int alpha)
    {
        ValidateComponent(blue, "blue");
        ValidateComponent(green, "green");
        ValidateComponent(red, "red");
        ValidateComponent(alpha, "alpha");
        this.blue = blue;
        this.green = green;
        this.red = red;
        this.alpha = alpha;
    }

    internal int GetBlue()
    {
        return blue;
    }

    internal int GetGreen()
    {
        return green;
    }

    internal int GetRed()
    {
        return red;
    }

    internal int GetAlpha()
    {
        return alpha;
    }

    static void ValidateComponent(int value, string parameterName)
    {
        if (value < 0 || value > 255)
        {
            throw new System.ArgumentOutOfRangeException(parameterName);
        }
    }
}

internal class SfntCpalPalette
{
    const uint UsableWithLightBackground = 0x00000001U;
    const uint UsableWithDarkBackground = 0x00000002U;
    int index;
    int firstColorRecordIndex;
    int entryCount;
    SfntCpalColor[] colorRecords;
    bool hasType;
    uint typeFlags;
    bool hasLabel;
    int labelNameId;
    SfntLocalizedName? label;

    internal SfntCpalPalette(
        int index,
        int firstColorRecordIndex,
        int entryCount,
        SfntCpalColor[] colorRecords,
        bool hasType,
        uint typeFlags,
        bool hasLabel,
        int labelNameId,
        SfntLocalizedName? label)
    {
        this.index = index;
        this.firstColorRecordIndex = firstColorRecordIndex;
        this.entryCount = entryCount;
        this.colorRecords = colorRecords;
        this.hasType = hasType;
        this.typeFlags = typeFlags;
        this.hasLabel = hasLabel;
        this.labelNameId = labelNameId;
        this.label = label;
    }

    internal int GetIndex()
    {
        return index;
    }

    internal int GetFirstColorRecordIndex()
    {
        return firstColorRecordIndex;
    }

    internal int GetEntryCount()
    {
        return entryCount;
    }

    internal SfntCpalColor GetColor(int entryIndex)
    {
        if (entryIndex < 0 || entryIndex >= entryCount)
        {
            throw new System.ArgumentOutOfRangeException("entryIndex");
        }

        return colorRecords[firstColorRecordIndex + entryIndex];
    }

    internal bool HasType()
    {
        return hasType;
    }

    internal uint GetTypeFlags()
    {
        if (!hasType)
        {
            throw new System.InvalidOperationException(
                "This CPAL table does not provide palette types.");
        }

        return typeFlags;
    }

    internal bool IsUsableWithLightBackground()
    {
        return hasType && (typeFlags & UsableWithLightBackground) != 0;
    }

    internal bool IsUsableWithDarkBackground()
    {
        return hasType && (typeFlags & UsableWithDarkBackground) != 0;
    }

    internal bool HasLabel()
    {
        return hasLabel;
    }

    internal int GetLabelNameId()
    {
        if (!hasLabel)
        {
            throw new System.InvalidOperationException(
                "This CPAL palette does not provide a label.");
        }

        return labelNameId;
    }

    internal SfntLocalizedName? GetLabel()
    {
        return label;
    }
}

internal class SfntCpalTable
{
    const int ForegroundPaletteEntryIndex = 0xFFFF;
    int version;
    SfntCpalColor[] colorRecords;
    SfntCpalPalette[] palettes;
    int paletteEntryCount;
    bool hasPaletteTypes;
    bool hasPaletteLabels;
    bool hasPaletteEntryLabels;
    int[] paletteEntryLabelNameIds;
    SfntLocalizedName?[] paletteEntryLabels;

    internal SfntCpalTable(
        int version,
        SfntCpalColor[] colorRecords,
        SfntCpalPalette[] palettes,
        int paletteEntryCount,
        bool hasPaletteTypes,
        bool hasPaletteLabels,
        bool hasPaletteEntryLabels,
        int[] paletteEntryLabelNameIds,
        SfntLocalizedName?[] paletteEntryLabels)
    {
        this.version = version;
        this.colorRecords = colorRecords;
        this.palettes = palettes;
        this.paletteEntryCount = paletteEntryCount;
        this.hasPaletteTypes = hasPaletteTypes;
        this.hasPaletteLabels = hasPaletteLabels;
        this.hasPaletteEntryLabels = hasPaletteEntryLabels;
        this.paletteEntryLabelNameIds = paletteEntryLabelNameIds;
        this.paletteEntryLabels = paletteEntryLabels;
    }

    internal int GetVersion()
    {
        return version;
    }

    internal int GetPaletteEntryCount()
    {
        return paletteEntryCount;
    }

    internal int GetPaletteCount()
    {
        return palettes.Length;
    }

    internal SfntCpalPalette GetPalette(int index)
    {
        if (index < 0 || index >= palettes.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return palettes[index];
    }

    internal SfntCpalPalette GetDefaultPalette()
    {
        return palettes[0];
    }

    internal int GetColorRecordCount()
    {
        return colorRecords.Length;
    }

    internal SfntCpalColor GetColorRecord(int index)
    {
        if (index < 0 || index >= colorRecords.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return colorRecords[index];
    }

    internal bool HasPaletteTypes()
    {
        return hasPaletteTypes;
    }

    internal bool HasPaletteLabels()
    {
        return hasPaletteLabels;
    }

    internal bool HasPaletteEntryLabels()
    {
        return hasPaletteEntryLabels;
    }

    internal bool HasPaletteEntryLabel(int entryIndex)
    {
        ValidateEntryIndex(entryIndex);
        return paletteEntryLabelNameIds[entryIndex] != ForegroundPaletteEntryIndex;
    }

    internal int GetPaletteEntryLabelNameId(int entryIndex)
    {
        ValidateEntryIndex(entryIndex);

        if (!HasPaletteEntryLabel(entryIndex))
        {
            throw new System.InvalidOperationException(
                "This CPAL palette entry does not provide a label.");
        }

        return paletteEntryLabelNameIds[entryIndex];
    }

    internal SfntLocalizedName? GetPaletteEntryLabel(int entryIndex)
    {
        ValidateEntryIndex(entryIndex);
        return paletteEntryLabels[entryIndex];
    }

    internal int GetForegroundPaletteEntryIndex()
    {
        return ForegroundPaletteEntryIndex;
    }

    internal bool IsForegroundPaletteEntry(int paletteEntryIndex)
    {
        return paletteEntryIndex == ForegroundPaletteEntryIndex;
    }

    internal SfntCpalColor ResolveColor(
        int paletteIndex,
        int paletteEntryIndex,
        SfntCpalColor foregroundColor)
    {
        SfntCpalPalette palette = GetPalette(paletteIndex);

        if (IsForegroundPaletteEntry(paletteEntryIndex))
        {
            if (foregroundColor == null)
            {
                throw new System.ArgumentNullException("foregroundColor");
            }

            return foregroundColor;
        }

        return palette.GetColor(paletteEntryIndex);
    }

    void ValidateEntryIndex(int entryIndex)
    {
        if (entryIndex < 0 || entryIndex >= paletteEntryCount)
        {
            throw new System.ArgumentOutOfRangeException("entryIndex");
        }
    }
}

internal static class SfntCpalTableParser
{
    const int BaseHeaderLength = 12;
    const int VersionOneOffsetsLength = 12;
    const int ColorRecordLength = 4;
    const uint KnownPaletteTypeFlags = 0x00000003U;
    const int NoNameId = 0xFFFF;
    static readonly SfntTableParser parser = new SfntTableParser(
        FontTags.Cpal,
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

    internal static SfntCpalTable? FindTable(SfntTableCache cache)
    {
        SfntParsedTableView? view = FindView(cache);

        if (view == null || !view.HasParsedData())
        {
            return null;
        }

        return (SfntCpalTable)view.GetParsedData();
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

        if (tableData.GetLength() < BaseHeaderLength)
        {
            throw FontFormatException.Create("The CPAL table header is truncated.");
        }

        BigEndianReader reader = new BigEndianReader(tableData);
        int encodedVersion = reader.ReadUInt16();
        int paletteEntryCount = reader.ReadUInt16();
        int paletteCount = reader.ReadUInt16();
        int colorRecordCount = reader.ReadUInt16();
        long colorRecordsOffset = reader.ReadOffset32();

        if (encodedVersion != version.GetMajorVersion())
        {
            throw FontFormatException.Create("The CPAL table version is inconsistent.");
        }

        ValidateCounts(
            paletteEntryCount,
            paletteCount,
            colorRecordCount,
            limits);

        int headerLength = CheckedFontMath.AddOffset(
            BaseHeaderLength,
            CheckedFontMath.MultiplyCount(paletteCount, 2));

        if (encodedVersion == 1)
        {
            headerLength = CheckedFontMath.AddOffset(
                headerLength,
                VersionOneOffsetsLength);
        }

        if (tableData.GetLength() < headerLength)
        {
            throw FontFormatException.Create("The CPAL table header is truncated.");
        }

        int[] colorRecordIndices = new int[paletteCount];
        int index = 0;

        while (index < colorRecordIndices.Length)
        {
            int firstColorRecordIndex = reader.ReadUInt16();
            long endIndex = (long)firstColorRecordIndex + paletteEntryCount;

            if (endIndex > colorRecordCount)
            {
                throw FontFormatException.Create(
                    "A CPAL palette references missing color records.");
            }

            colorRecordIndices[index] = firstColorRecordIndex;
            index += 1;
        }

        long paletteTypesOffset = 0;
        long paletteLabelsOffset = 0;
        long paletteEntryLabelsOffset = 0;

        if (encodedVersion == 1)
        {
            paletteTypesOffset = reader.ReadOffset32();
            paletteLabelsOffset = reader.ReadOffset32();
            paletteEntryLabelsOffset = reader.ReadOffset32();
        }

        ValidateArrayOffset(
            tableData,
            colorRecordsOffset,
            colorRecordCount,
            ColorRecordLength,
            headerLength,
            "The CPAL color-records array offset is invalid.");
        ValidateOptionalArrayOffset(
            tableData,
            paletteTypesOffset,
            paletteCount,
            4,
            headerLength,
            "The CPAL palette-types array offset is invalid.");
        ValidateOptionalArrayOffset(
            tableData,
            paletteLabelsOffset,
            paletteCount,
            2,
            headerLength,
            "The CPAL palette-labels array offset is invalid.");
        ValidateOptionalArrayOffset(
            tableData,
            paletteEntryLabelsOffset,
            paletteEntryCount,
            2,
            headerLength,
            "The CPAL palette-entry-labels array offset is invalid.");

        SfntCpalColor[] colorRecords = ParseColors(
            tableData,
            colorRecordsOffset,
            colorRecordCount);
        uint[] paletteTypes = ParsePaletteTypes(
            tableData,
            paletteTypesOffset,
            paletteCount);
        int[] paletteLabelNameIds = ParseNameIds(
            tableData,
            paletteLabelsOffset,
            paletteCount);
        int[] paletteEntryLabelNameIds = ParseNameIds(
            tableData,
            paletteEntryLabelsOffset,
            paletteEntryCount);
        SfntNameTable? nameTable = null;

        if (NeedsNameTable(paletteLabelNameIds) ||
            NeedsNameTable(paletteEntryLabelNameIds))
        {
            nameTable = SfntNameTableParser.FindTable(cache);
        }

        SfntLocalizedName?[] paletteLabels = ResolveNames(
            paletteLabelNameIds,
            nameTable,
            "A CPAL palette label cannot be resolved.");
        SfntLocalizedName?[] paletteEntryLabels = ResolveNames(
            paletteEntryLabelNameIds,
            nameTable,
            "A CPAL palette-entry label cannot be resolved.");
        SfntCpalPalette[] palettes = new SfntCpalPalette[paletteCount];
        bool hasPaletteTypes = paletteTypesOffset != 0;
        bool hasPaletteLabels = paletteLabelsOffset != 0;
        bool hasPaletteEntryLabels = paletteEntryLabelsOffset != 0;
        index = 0;

        while (index < palettes.Length)
        {
            bool hasLabel = paletteLabelNameIds[index] != NoNameId;
            palettes[index] = new SfntCpalPalette(
                index,
                colorRecordIndices[index],
                paletteEntryCount,
                colorRecords,
                hasPaletteTypes,
                paletteTypes[index],
                hasLabel,
                paletteLabelNameIds[index],
                paletteLabels[index]);
            index += 1;
        }

        return new SfntCpalTable(
            encodedVersion,
            colorRecords,
            palettes,
            paletteEntryCount,
            hasPaletteTypes,
            hasPaletteLabels,
            hasPaletteEntryLabels,
            paletteEntryLabelNameIds,
            paletteEntryLabels);
    }

    static void ValidateCounts(
        int paletteEntryCount,
        int paletteCount,
        int colorRecordCount,
        FontLimits limits)
    {
        if (paletteEntryCount == 0 || paletteCount == 0 || colorRecordCount == 0)
        {
            throw FontFormatException.Create(
                "A CPAL table must contain palettes, entries, and color records.");
        }

        int limit = limits.GetMaxCoverageEntries();

        if (paletteEntryCount > limit || paletteCount > limit ||
            colorRecordCount > limit)
        {
            throw FontLimitException.Create(
                "A CPAL array count exceeds the configured coverage-entry limit.");
        }
    }

    static SfntCpalColor[] ParseColors(
        FontDataSlice tableData,
        long offset,
        int count)
    {
        int length = CheckedFontMath.MultiplyCount(count, ColorRecordLength);
        BigEndianReader reader = new BigEndianReader(
            tableData.GetSlice((int)offset, length));
        SfntCpalColor[] colors = new SfntCpalColor[count];
        int index = 0;

        while (index < colors.Length)
        {
            colors[index] = new SfntCpalColor(
                reader.ReadUInt8(),
                reader.ReadUInt8(),
                reader.ReadUInt8(),
                reader.ReadUInt8());
            index += 1;
        }

        return colors;
    }

    static uint[] ParsePaletteTypes(
        FontDataSlice tableData,
        long offset,
        int count)
    {
        uint[] types = new uint[count];

        if (offset == 0)
        {
            return types;
        }

        int length = CheckedFontMath.MultiplyCount(count, 4);
        BigEndianReader reader = new BigEndianReader(
            tableData.GetSlice((int)offset, length));
        int index = 0;

        while (index < types.Length)
        {
            uint type = reader.ReadUInt32();

            if ((type & ~KnownPaletteTypeFlags) != 0)
            {
                throw FontFormatException.Create(
                    "A CPAL palette type has nonzero reserved flags.");
            }

            types[index] = type;
            index += 1;
        }

        return types;
    }

    static int[] ParseNameIds(
        FontDataSlice tableData,
        long offset,
        int count)
    {
        int[] nameIds = new int[count];

        if (offset == 0)
        {
            int emptyIndex = 0;

            while (emptyIndex < nameIds.Length)
            {
                nameIds[emptyIndex] = NoNameId;
                emptyIndex += 1;
            }

            return nameIds;
        }

        int length = CheckedFontMath.MultiplyCount(count, 2);
        BigEndianReader reader = new BigEndianReader(
            tableData.GetSlice((int)offset, length));
        int index = 0;

        while (index < nameIds.Length)
        {
            nameIds[index] = reader.ReadUInt16();
            index += 1;
        }

        return nameIds;
    }

    static SfntLocalizedName?[] ResolveNames(
        int[] nameIds,
        SfntNameTable? nameTable,
        string message)
    {
        SfntLocalizedName?[] names = new SfntLocalizedName[nameIds.Length];
        int index = 0;

        while (index < names.Length)
        {
            if (nameIds[index] != NoNameId)
            {
                if (nameTable == null)
                {
                    throw FontFormatException.Create(message);
                }

                names[index] = nameTable.SelectDefaultName(nameIds[index]);

                if (names[index] == null)
                {
                    throw FontFormatException.Create(message);
                }
            }

            index += 1;
        }

        return names;
    }

    static bool NeedsNameTable(int[] nameIds)
    {
        int index = 0;

        while (index < nameIds.Length)
        {
            if (nameIds[index] != NoNameId)
            {
                return true;
            }

            index += 1;
        }

        return false;
    }

    static void ValidateOptionalArrayOffset(
        FontDataSlice tableData,
        long offset,
        int count,
        int elementLength,
        int headerLength,
        string message)
    {
        if (offset != 0)
        {
            ValidateArrayOffset(
                tableData,
                offset,
                count,
                elementLength,
                headerLength,
                message);
        }
    }

    static void ValidateArrayOffset(
        FontDataSlice tableData,
        long offset,
        int count,
        int elementLength,
        int headerLength,
        string message)
    {
        long length = (long)count * elementLength;

        if (offset < headerLength || offset > int.MaxValue ||
            length > int.MaxValue || offset > tableData.GetLength() - length)
        {
            throw FontFormatException.Create(message);
        }
    }
}
