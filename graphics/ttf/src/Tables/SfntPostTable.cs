namespace ttf;

internal enum SfntPostFormat
{
    Version1 = 0,
    Version2 = 1,
    Version25 = 2,
    Version3 = 3,
    Version4 = 4
}

internal class SfntPostHeader
{
    uint version;
    int italicAngle;
    int underlinePosition;
    int underlineThickness;
    uint fixedPitch;
    uint minimumType42Memory;
    uint maximumType42Memory;
    uint minimumType1Memory;
    uint maximumType1Memory;

    internal SfntPostHeader(
        uint version,
        int italicAngle,
        int underlinePosition,
        int underlineThickness,
        uint fixedPitch,
        uint minimumType42Memory,
        uint maximumType42Memory,
        uint minimumType1Memory,
        uint maximumType1Memory)
    {
        this.version = version;
        this.italicAngle = italicAngle;
        this.underlinePosition = underlinePosition;
        this.underlineThickness = underlineThickness;
        this.fixedPitch = fixedPitch;
        this.minimumType42Memory = minimumType42Memory;
        this.maximumType42Memory = maximumType42Memory;
        this.minimumType1Memory = minimumType1Memory;
        this.maximumType1Memory = maximumType1Memory;
    }

    internal uint GetVersion()
    {
        return version;
    }

    internal int GetItalicAngle()
    {
        return italicAngle;
    }

    internal int GetUnderlinePosition()
    {
        return underlinePosition;
    }

    internal int GetUnderlineThickness()
    {
        return underlineThickness;
    }

    internal uint GetFixedPitch()
    {
        return fixedPitch;
    }

    internal uint GetMinimumType42Memory()
    {
        return minimumType42Memory;
    }

    internal uint GetMaximumType42Memory()
    {
        return maximumType42Memory;
    }

    internal uint GetMinimumType1Memory()
    {
        return minimumType1Memory;
    }

    internal uint GetMaximumType1Memory()
    {
        return maximumType1Memory;
    }
}

internal class SfntPostGlyphName
{
    int glyphId;
    int nameIndex;
    string name;
    bool provided;
    bool validNameIndex;
    bool validName;
    bool standardName;
    bool customName;

    internal SfntPostGlyphName(
        int glyphId,
        int nameIndex,
        string name,
        bool provided,
        bool validNameIndex,
        bool validName,
        bool standardName,
        bool customName)
    {
        this.glyphId = glyphId;
        this.nameIndex = nameIndex;
        this.name = name;
        this.provided = provided;
        this.validNameIndex = validNameIndex;
        this.validName = validName;
        this.standardName = standardName;
        this.customName = customName;
    }

    internal int GetGlyphId()
    {
        return glyphId;
    }

    internal int GetNameIndex()
    {
        return nameIndex;
    }

    internal string GetName()
    {
        return name;
    }

    internal bool IsProvided()
    {
        return provided;
    }

    internal bool IsSynthetic()
    {
        return !provided;
    }

    internal bool HasValidNameIndex()
    {
        return validNameIndex;
    }

    internal bool HasValidName()
    {
        return provided && validNameIndex && validName;
    }

    internal bool IsStandardName()
    {
        return standardName;
    }

    internal bool IsCustomName()
    {
        return customName;
    }
}

internal class SfntPostTable
{
    SfntPostHeader header;
    SfntPostFormat format;
    SfntPostGlyphName[] glyphNames;
    string[] customNames;
    int invalidNameIndexCount;
    int invalidGlyphNameCount;
    int providedGlyphNameCount;

    internal SfntPostTable(
        SfntPostHeader header,
        SfntPostFormat format,
        SfntPostGlyphName[] glyphNames,
        string[] customNames,
        int invalidNameIndexCount,
        int invalidGlyphNameCount,
        int providedGlyphNameCount)
    {
        this.header = header;
        this.format = format;
        this.glyphNames = glyphNames;
        this.customNames = customNames;
        this.invalidNameIndexCount = invalidNameIndexCount;
        this.invalidGlyphNameCount = invalidGlyphNameCount;
        this.providedGlyphNameCount = providedGlyphNameCount;
    }

    internal uint GetVersionRaw()
    {
        return header.GetVersion();
    }

    internal double GetVersion()
    {
        if (format == SfntPostFormat.Version25)
        {
            return 2.5;
        }

        if (format == SfntPostFormat.Version1)
        {
            return 1.0;
        }

        if (format == SfntPostFormat.Version2)
        {
            return 2.0;
        }

        if (format == SfntPostFormat.Version3)
        {
            return 3.0;
        }

        return 4.0;
    }

    internal SfntPostFormat GetFormat()
    {
        return format;
    }

    internal int GetItalicAngleRaw()
    {
        return header.GetItalicAngle();
    }

    internal double GetItalicAngle()
    {
        return header.GetItalicAngle() / 65536.0;
    }

    internal int GetUnderlinePosition()
    {
        return header.GetUnderlinePosition();
    }

    internal int GetUnderlineThickness()
    {
        return header.GetUnderlineThickness();
    }

    internal uint GetFixedPitchRaw()
    {
        return header.GetFixedPitch();
    }

    internal bool IsFixedPitch()
    {
        return header.GetFixedPitch() != 0;
    }

    internal uint GetMinimumType42Memory()
    {
        return header.GetMinimumType42Memory();
    }

    internal uint GetMaximumType42Memory()
    {
        return header.GetMaximumType42Memory();
    }

    internal uint GetMinimumType1Memory()
    {
        return header.GetMinimumType1Memory();
    }

    internal uint GetMaximumType1Memory()
    {
        return header.GetMaximumType1Memory();
    }

    internal int GetGlyphCount()
    {
        return glyphNames.Length;
    }

    internal bool ProvidesGlyphNames()
    {
        return providedGlyphNameCount != 0;
    }

    internal int GetProvidedGlyphNameCount()
    {
        return providedGlyphNameCount;
    }

    internal SfntPostGlyphName GetGlyphNameRecord(int glyphId)
    {
        if (glyphId < 0 || glyphId >= glyphNames.Length)
        {
            throw new System.ArgumentOutOfRangeException("glyphId");
        }

        return glyphNames[glyphId];
    }

    internal string GetGlyphName(int glyphId)
    {
        return GetGlyphNameRecord(glyphId).GetName();
    }

    internal int GetCustomNameCount()
    {
        return customNames.Length;
    }

    internal string GetCustomName(int index)
    {
        if (index < 0 || index >= customNames.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return customNames[index];
    }

    internal int GetInvalidNameIndexCount()
    {
        return invalidNameIndexCount;
    }

    internal int GetInvalidGlyphNameCount()
    {
        return invalidGlyphNameCount;
    }
}

internal class SfntPostCustomNameData
{
    string[] names;
    bool[] validNames;

    internal SfntPostCustomNameData(string[] names, bool[] validNames)
    {
        this.names = names;
        this.validNames = validNames;
    }

    internal int GetCount()
    {
        return names.Length;
    }

    internal string[] GetNames()
    {
        return names;
    }

    internal string GetName(int index)
    {
        return names[index];
    }

    internal bool IsValid(int index)
    {
        return validNames[index];
    }
}

internal static class SfntPostTableParser
{
    const int HeaderLength = 32;
    const uint Version1 = 0x00010000U;
    const uint Version2 = 0x00020000U;
    const uint Version25 = 0x00025000U;
    const uint Version3 = 0x00030000U;
    const uint Version4 = 0x00040000U;
    static readonly SfntTableParser parser = new SfntTableParser(
        FontTags.Post,
        1,
        4,
        5,
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

    internal static SfntPostTable? FindTable(SfntTableCache cache)
    {
        SfntParsedTableView? view = FindView(cache);

        if (view == null || !view.HasParsedData())
        {
            return null;
        }

        return (SfntPostTable)view.GetParsedData();
    }

    static SfntTableVersion ReadVersion(SfntTableData table)
    {
        BigEndianReader reader = new BigEndianReader(table.GetDataSlice());
        uint version = reader.ReadUInt32();

        if (version == Version1)
        {
            return new SfntTableVersion(1, 0);
        }

        if (version == Version2)
        {
            return new SfntTableVersion(2, 0);
        }

        if (version == Version25)
        {
            return new SfntTableVersion(2, 5);
        }

        if (version == Version3)
        {
            return new SfntTableVersion(3, 0);
        }

        if (version == Version4)
        {
            return new SfntTableVersion(4, 0);
        }

        return new SfntTableVersion(5, 0);
    }

    static object Parse(
        SfntTableData table,
        SfntTableVersion version,
        SfntTableVersionSupport support,
        FontLimits limits,
        SfntTableCache cache)
    {
        if (table.GetLength() < HeaderLength)
        {
            throw FontFormatException.Create("The post table header is truncated.");
        }

        BigEndianReader reader = new BigEndianReader(table.GetDataSlice());
        SfntPostHeader header = ReadHeader(reader);
        SfntMaxpTable? maxp = SfntMaxpTableParser.FindTable(cache);

        if (maxp == null)
        {
            throw FontFormatException.Create(
                "The post table requires a supported maxp table.");
        }

        int glyphCount = maxp.GetGlyphCount();
        uint encodedVersion = header.GetVersion();

        if (encodedVersion == Version1)
        {
            return ParseVersion1(table, header, glyphCount);
        }

        if (encodedVersion == Version2)
        {
            return ParseVersion2(header, reader, glyphCount, limits);
        }

        if (encodedVersion == Version25)
        {
            return ParseVersion25(table, header, reader, glyphCount);
        }

        if (encodedVersion == Version3)
        {
            return ParseVersion3(table, header, glyphCount);
        }

        if (encodedVersion == Version4)
        {
            return ParseVersion4(table, header, reader, glyphCount);
        }

        throw FontFormatException.Create("The post table version is not supported.");
    }

    static SfntPostHeader ReadHeader(BigEndianReader reader)
    {
        return new SfntPostHeader(
            reader.ReadUInt32(),
            reader.ReadInt32(),
            reader.ReadInt16(),
            reader.ReadInt16(),
            reader.ReadUInt32(),
            reader.ReadUInt32(),
            reader.ReadUInt32(),
            reader.ReadUInt32(),
            reader.ReadUInt32());
    }

    static SfntPostTable ParseVersion1(
        SfntTableData table,
        SfntPostHeader header,
        int glyphCount)
    {
        if (table.GetLength() != HeaderLength)
        {
            throw FontFormatException.Create("The post version 1 table length is invalid.");
        }

        if (glyphCount != SfntPostStandardNames.GetCount())
        {
            throw FontFormatException.Create(
                "The post version 1 table requires exactly 258 glyphs.");
        }

        SfntPostGlyphName[] records = new SfntPostGlyphName[glyphCount];
        int glyphId = 0;

        while (glyphId < glyphCount)
        {
            records[glyphId] = CreateProvidedName(
                glyphId,
                glyphId,
                SfntPostStandardNames.GetName(glyphId),
                true,
                false);
            glyphId += 1;
        }

        return new SfntPostTable(
            header,
            SfntPostFormat.Version1,
            records,
            new string[0],
            0,
            0,
            glyphCount);
    }

    static SfntPostTable ParseVersion2(
        SfntPostHeader header,
        BigEndianReader reader,
        int maxpGlyphCount,
        FontLimits limits)
    {
        int glyphCount = reader.ReadUInt16();

        if (glyphCount != maxpGlyphCount)
        {
            throw FontFormatException.Create(
                "The post version 2 glyph count does not match maxp.");
        }

        int[] nameIndices = new int[glyphCount];
        int maximumCustomIndex = -1;
        int glyphId = 0;

        while (glyphId < glyphCount)
        {
            int nameIndex = reader.ReadUInt16();
            nameIndices[glyphId] = nameIndex;

            if (nameIndex >= 258 && nameIndex - 258 > maximumCustomIndex)
            {
                maximumCustomIndex = nameIndex - 258;
            }

            glyphId += 1;
        }

        int expectedCustomNameCount = maximumCustomIndex + 1;
        SfntPostCustomNameData customNames = ReadCustomNames(
            reader,
            expectedCustomNameCount,
            limits);

        if (customNames.GetCount() == expectedCustomNameCount &&
            reader.GetRemainingLength() != 0)
        {
            throw FontFormatException.Create(
                "The post version 2 string data contains trailing bytes.");
        }

        SfntPostGlyphName[] records = new SfntPostGlyphName[glyphCount];
        int invalidIndexCount = 0;
        int invalidGlyphNameCount = 0;
        int providedCount = 0;
        glyphId = 0;

        while (glyphId < glyphCount)
        {
            int nameIndex = nameIndices[glyphId];

            if (nameIndex < 258)
            {
                records[glyphId] = CreateProvidedName(
                    glyphId,
                    nameIndex,
                    SfntPostStandardNames.GetName(nameIndex),
                    true,
                    false);
                providedCount += 1;
            }
            else
            {
                int customIndex = nameIndex - 258;

                if (customIndex >= customNames.GetCount())
                {
                    records[glyphId] = CreateSyntheticName(
                        glyphId,
                        nameIndex,
                        false,
                        false);
                    invalidIndexCount += 1;
                    invalidGlyphNameCount += 1;
                }
                else if (!customNames.IsValid(customIndex))
                {
                    records[glyphId] = CreateSyntheticName(
                        glyphId,
                        nameIndex,
                        true,
                        false);
                    invalidGlyphNameCount += 1;
                }
                else
                {
                    records[glyphId] = CreateProvidedName(
                        glyphId,
                        nameIndex,
                        customNames.GetName(customIndex),
                        false,
                        true);
                    providedCount += 1;
                }
            }

            glyphId += 1;
        }

        return new SfntPostTable(
            header,
            SfntPostFormat.Version2,
            records,
            customNames.GetNames(),
            invalidIndexCount,
            invalidGlyphNameCount,
            providedCount);
    }

    static SfntPostTable ParseVersion25(
        SfntTableData table,
        SfntPostHeader header,
        BigEndianReader reader,
        int maxpGlyphCount)
    {
        int glyphCount = reader.ReadUInt16();

        if (glyphCount != maxpGlyphCount)
        {
            throw FontFormatException.Create(
                "The post version 2.5 glyph count does not match maxp.");
        }

        if (table.GetLength() != HeaderLength + 2 + glyphCount)
        {
            throw FontFormatException.Create(
                "The post version 2.5 table length is invalid.");
        }

        SfntPostGlyphName[] records = new SfntPostGlyphName[glyphCount];
        int invalidIndexCount = 0;
        int providedCount = 0;
        int glyphId = 0;

        while (glyphId < glyphCount)
        {
            int nameIndex = glyphId + reader.ReadInt8();

            if (nameIndex < 0 || nameIndex >= SfntPostStandardNames.GetCount())
            {
                records[glyphId] = CreateSyntheticName(
                    glyphId,
                    nameIndex,
                    false,
                    false);
                invalidIndexCount += 1;
            }
            else
            {
                records[glyphId] = CreateProvidedName(
                    glyphId,
                    nameIndex,
                    SfntPostStandardNames.GetName(nameIndex),
                    true,
                    false);
                providedCount += 1;
            }

            glyphId += 1;
        }

        return new SfntPostTable(
            header,
            SfntPostFormat.Version25,
            records,
            new string[0],
            invalidIndexCount,
            invalidIndexCount,
            providedCount);
    }

    static SfntPostTable ParseVersion3(
        SfntTableData table,
        SfntPostHeader header,
        int glyphCount)
    {
        if (table.GetLength() != HeaderLength)
        {
            throw FontFormatException.Create("The post version 3 table length is invalid.");
        }

        return new SfntPostTable(
            header,
            SfntPostFormat.Version3,
            CreateSyntheticNames(glyphCount),
            new string[0],
            0,
            0,
            0);
    }

    static SfntPostTable ParseVersion4(
        SfntTableData table,
        SfntPostHeader header,
        BigEndianReader reader,
        int glyphCount)
    {
        if (table.GetLength() != HeaderLength + glyphCount * 2L)
        {
            throw FontFormatException.Create("The post version 4 table length is invalid.");
        }

        SfntPostGlyphName[] records = new SfntPostGlyphName[glyphCount];
        int providedCount = 0;
        int glyphId = 0;

        while (glyphId < glyphCount)
        {
            int characterCode = reader.ReadUInt16();

            if (characterCode == 0xFFFF)
            {
                records[glyphId] = CreateSyntheticName(
                    glyphId,
                    characterCode,
                    true,
                    true);
            }
            else
            {
                string name = "a" + characterCode.ToString(
                    "X4",
                    System.Globalization.CultureInfo.InvariantCulture);
                records[glyphId] = CreateProvidedName(
                    glyphId,
                    characterCode,
                    name,
                    false,
                    false);
                providedCount += 1;
            }

            glyphId += 1;
        }

        return new SfntPostTable(
            header,
            SfntPostFormat.Version4,
            records,
            new string[0],
            0,
            0,
            providedCount);
    }

    static SfntPostCustomNameData ReadCustomNames(
        BigEndianReader reader,
        int expectedCount,
        FontLimits limits)
    {
        System.Collections.Generic.List<string> names =
            new System.Collections.Generic.List<string>();
        System.Collections.Generic.List<bool> validNames =
            new System.Collections.Generic.List<bool>();

        while (names.Count < expectedCount && reader.GetRemainingLength() != 0)
        {
            int length = reader.ReadUInt8();

            if (length > limits.GetMaxStringBytes())
            {
                throw FontLimitException.Create(
                    "A post glyph name exceeds the configured string limit.");
            }

            if (length > reader.GetRemainingLength())
            {
                throw FontFormatException.Create(
                    "A post Pascal string is truncated.");
            }

            char[] characters = new char[length];
            bool valid = length != 0 && length <= 63;
            int index = 0;

            while (index < length)
            {
                int character = reader.ReadUInt8();
                characters[index] = (char)character;

                if (!IsValidGlyphNameCharacter(character))
                {
                    valid = false;
                }

                index += 1;
            }

            names.Add(new string(characters));
            validNames.Add(valid);
        }

        return new SfntPostCustomNameData(
            names.ToArray(),
            validNames.ToArray());
    }

    static bool IsValidGlyphNameCharacter(int character)
    {
        return character >= 'A' && character <= 'Z' ||
            character >= 'a' && character <= 'z' ||
            character >= '0' && character <= '9' ||
            character == '.' || character == '_';
    }

    static SfntPostGlyphName CreateProvidedName(
        int glyphId,
        int nameIndex,
        string name,
        bool standardName,
        bool customName)
    {
        return new SfntPostGlyphName(
            glyphId,
            nameIndex,
            name,
            true,
            true,
            true,
            standardName,
            customName);
    }

    static SfntPostGlyphName CreateSyntheticName(
        int glyphId,
        int nameIndex,
        bool validNameIndex,
        bool validName)
    {
        return new SfntPostGlyphName(
            glyphId,
            nameIndex,
            CreateSyntheticName(glyphId),
            false,
            validNameIndex,
            validName,
            false,
            false);
    }

    static SfntPostGlyphName[] CreateSyntheticNames(int glyphCount)
    {
        SfntPostGlyphName[] names = new SfntPostGlyphName[glyphCount];
        int glyphId = 0;

        while (glyphId < glyphCount)
        {
            names[glyphId] = CreateSyntheticName(
                glyphId,
                -1,
                true,
                true);
            glyphId += 1;
        }

        return names;
    }

    static string CreateSyntheticName(int glyphId)
    {
        return "gid" + glyphId.ToString(
            "D4",
            System.Globalization.CultureInfo.InvariantCulture);
    }
}
