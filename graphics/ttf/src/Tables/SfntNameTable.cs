namespace ttf;

internal enum SfntNameEncodingKind
{
    Unknown = 0,
    Utf16BigEndian = 1,
    MacintoshRoman = 2,
    MacintoshLegacy = 3,
    WindowsCodePage936 = 4,
    WindowsCodePage950 = 5,
    WindowsCodePage949 = 6,
    WindowsCodePage932 = 7
}

internal class SfntNameRecord
{
    int platformId;
    int encodingId;
    int languageId;
    int nameId;
    int length;
    int stringOffset;
    SfntNameEncodingKind encodingKind;
    bool usesLanguageTag;
    bool validLanguage;
    bool validString;
    bool duplicateKey;
    FontDataSlice stringData;

    internal SfntNameRecord(
        int platformId,
        int encodingId,
        int languageId,
        int nameId,
        int length,
        int stringOffset,
        SfntNameEncodingKind encodingKind,
        bool usesLanguageTag,
        bool validLanguage,
        bool validString,
        bool duplicateKey,
        FontDataSlice stringData)
    {
        this.platformId = platformId;
        this.encodingId = encodingId;
        this.languageId = languageId;
        this.nameId = nameId;
        this.length = length;
        this.stringOffset = stringOffset;
        this.encodingKind = encodingKind;
        this.usesLanguageTag = usesLanguageTag;
        this.validLanguage = validLanguage;
        this.validString = validString;
        this.duplicateKey = duplicateKey;
        this.stringData = stringData;
    }

    internal int GetPlatformId()
    {
        return platformId;
    }

    internal int GetEncodingId()
    {
        return encodingId;
    }

    internal int GetLanguageId()
    {
        return languageId;
    }

    internal int GetNameId()
    {
        return nameId;
    }

    internal int GetLength()
    {
        return length;
    }

    internal int GetStringOffset()
    {
        return stringOffset;
    }

    internal SfntNameEncodingKind GetEncodingKind()
    {
        return encodingKind;
    }

    internal bool IsUnicodeEncoding()
    {
        return encodingKind == SfntNameEncodingKind.Utf16BigEndian;
    }

    internal bool HasSupportedEncoding()
    {
        return SfntNameDecoder.IsEncodingSupported(this);
    }

    internal bool UsesLanguageTag()
    {
        return usesLanguageTag;
    }

    internal int GetLanguageTagIndex()
    {
        if (!UsesLanguageTag())
        {
            throw new System.InvalidOperationException(
                "This name record does not reference a language tag.");
        }

        return languageId - 0x8000;
    }

    internal bool HasValidLanguage()
    {
        return validLanguage;
    }

    internal bool HasValidString()
    {
        return validString;
    }

    internal bool IsDuplicateKey()
    {
        return duplicateKey;
    }

    internal byte[] CopyStringBytes()
    {
        return stringData.CopyBytes();
    }

    internal FontDataSlice GetStringData()
    {
        return stringData;
    }
}

internal class SfntLanguageTagRecord
{
    int languageId;
    int length;
    int stringOffset;
    string? languageTag;
    bool valid;
    FontDataSlice stringData;

    internal SfntLanguageTagRecord(
        int languageId,
        int length,
        int stringOffset,
        string? languageTag,
        bool valid,
        FontDataSlice stringData)
    {
        this.languageId = languageId;
        this.length = length;
        this.stringOffset = stringOffset;
        this.languageTag = languageTag;
        this.valid = valid;
        this.stringData = stringData;
    }

    internal int GetLanguageId()
    {
        return languageId;
    }

    internal int GetLength()
    {
        return length;
    }

    internal int GetStringOffset()
    {
        return stringOffset;
    }

    internal string? GetLanguageTag()
    {
        return languageTag;
    }

    internal bool IsValid()
    {
        return valid;
    }

    internal byte[] CopyStringBytes()
    {
        return stringData.CopyBytes();
    }
}

internal class SfntNameTable
{
    int version;
    int storageOffset;
    int storageLength;
    SfntNameRecord[] records;
    SfntLanguageTagRecord[] languageTags;
    int duplicateRecordCount;
    int invalidRecordCount;
    int invalidLanguageTagCount;
    SfntDecodedName[] decodedNames;

    internal SfntNameTable(
        int version,
        int storageOffset,
        int storageLength,
        SfntNameRecord[] records,
        SfntLanguageTagRecord[] languageTags,
        int duplicateRecordCount,
        int invalidRecordCount,
        int invalidLanguageTagCount)
    {
        this.version = version;
        this.storageOffset = storageOffset;
        this.storageLength = storageLength;
        this.records = records;
        this.languageTags = languageTags;
        this.duplicateRecordCount = duplicateRecordCount;
        this.invalidRecordCount = invalidRecordCount;
        this.invalidLanguageTagCount = invalidLanguageTagCount;
        decodedNames = new SfntDecodedName[records.Length];
    }

    internal int GetVersion()
    {
        return version;
    }

    internal int GetStorageOffset()
    {
        return storageOffset;
    }

    internal int GetStorageLength()
    {
        return storageLength;
    }

    internal int GetRecordCount()
    {
        return records.Length;
    }

    internal SfntNameRecord GetRecord(int index)
    {
        if (index < 0 || index >= records.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return records[index];
    }

    internal SfntDecodedName GetDecodedName(int index)
    {
        SfntNameRecord record = GetRecord(index);

        lock (decodedNames)
        {
            SfntDecodedName decodedName = decodedNames[index];

            if (decodedName == null)
            {
                decodedName = SfntNameDecoder.Decode(record);
                decodedNames[index] = decodedName;
            }

            return decodedName;
        }
    }

    internal SfntLocalizedName? SelectName(int nameId, string languageTag)
    {
        return SfntNameSelector.Find(this, nameId, languageTag);
    }

    internal SfntLocalizedName? SelectDefaultName(int nameId)
    {
        return SfntNameSelector.Find(this, nameId);
    }

    internal SfntPostScriptName? GetPostScriptName()
    {
        return SfntPostScriptNameNormalizer.Find(this);
    }

    internal int GetLanguageTagCount()
    {
        return languageTags.Length;
    }

    internal SfntLanguageTagRecord GetLanguageTagRecord(int index)
    {
        if (index < 0 || index >= languageTags.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return languageTags[index];
    }

    internal string? GetLanguageTagForRecord(int recordIndex)
    {
        SfntNameRecord record = GetRecord(recordIndex);

        if (!record.UsesLanguageTag())
        {
            return null;
        }

        if (!record.HasValidLanguage())
        {
            return null;
        }

        return languageTags[record.GetLanguageTagIndex()].GetLanguageTag();
    }

    internal string? GetResolvedLanguageTagForRecord(int recordIndex)
    {
        return SfntNameLanguageResolver.GetLanguageTag(this, recordIndex);
    }

    internal int GetDuplicateRecordCount()
    {
        return duplicateRecordCount;
    }

    internal int GetInvalidRecordCount()
    {
        return invalidRecordCount;
    }

    internal int GetInvalidLanguageTagCount()
    {
        return invalidLanguageTagCount;
    }
}

internal static class SfntNameTableParser
{
    const int HeaderLength = 6;
    const int NameRecordLength = 12;
    const int LanguageTagRecordLength = 4;
    const int MaximumLanguageTagCount = 0x8000;
    static readonly SfntTableParser parser = new SfntTableParser(
        FontTags.Name,
        0,
        0,
        1,
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

    internal static SfntNameTable? FindTable(SfntTableCache cache)
    {
        SfntParsedTableView? view = FindView(cache);

        if (view == null || !view.HasParsedData())
        {
            return null;
        }

        return (SfntNameTable)view.GetParsedData();
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
        FontDataSlice tableData = table.GetDataSlice();

        if (tableData.GetLength() < HeaderLength)
        {
            throw FontFormatException.Create("The name table header is truncated.");
        }

        BigEndianReader reader = new BigEndianReader(tableData);
        int version = reader.ReadUInt16();
        int recordCount = reader.ReadUInt16();
        int storageOffset = reader.ReadOffset16();

        if (recordCount > limits.GetMaxCoverageEntries())
        {
            throw FontLimitException.Create(
                "The name record count exceeds the configured coverage-entry limit.");
        }

        long nameRecordsEnd = HeaderLength + recordCount * (long)NameRecordLength;

        if (nameRecordsEnd > tableData.GetLength())
        {
            throw FontFormatException.Create("The name record array is truncated.");
        }

        int[] platformIds = new int[recordCount];
        int[] encodingIds = new int[recordCount];
        int[] languageIds = new int[recordCount];
        int[] nameIds = new int[recordCount];
        int[] lengths = new int[recordCount];
        int[] stringOffsets = new int[recordCount];
        int recordIndex = 0;

        while (recordIndex < recordCount)
        {
            platformIds[recordIndex] = reader.ReadUInt16();
            encodingIds[recordIndex] = reader.ReadUInt16();
            languageIds[recordIndex] = reader.ReadUInt16();
            nameIds[recordIndex] = reader.ReadUInt16();
            lengths[recordIndex] = reader.ReadUInt16();
            stringOffsets[recordIndex] = reader.ReadOffset16();
            recordIndex += 1;
        }

        int languageTagCount = 0;
        int[] languageTagLengths = new int[0];
        int[] languageTagOffsets = new int[0];
        long recordDataEnd = nameRecordsEnd;

        if (version >= 1)
        {
            if (reader.GetRemainingLength() < 2)
            {
                throw FontFormatException.Create("The name language-tag count is truncated.");
            }

            languageTagCount = reader.ReadUInt16();

            if (languageTagCount > MaximumLanguageTagCount)
            {
                throw FontFormatException.Create(
                    "The name table declares more language tags than language IDs can address.");
            }

            if (languageTagCount >
                limits.GetMaxCoverageEntries() - recordCount)
            {
                throw FontLimitException.Create(
                    "The name record and language-tag counts exceed the configured coverage-entry limit.");
            }

            recordDataEnd = nameRecordsEnd + 2L +
                languageTagCount * (long)LanguageTagRecordLength;

            if (recordDataEnd > tableData.GetLength())
            {
                throw FontFormatException.Create("The name language-tag array is truncated.");
            }

            languageTagLengths = new int[languageTagCount];
            languageTagOffsets = new int[languageTagCount];
            int languageTagIndex = 0;

            while (languageTagIndex < languageTagCount)
            {
                languageTagLengths[languageTagIndex] = reader.ReadUInt16();
                languageTagOffsets[languageTagIndex] = reader.ReadOffset16();
                languageTagIndex += 1;
            }
        }

        if (storageOffset < recordDataEnd || storageOffset > tableData.GetLength())
        {
            throw FontFormatException.Create("The name string storage offset is invalid.");
        }

        int storageLength = tableData.GetLength() - storageOffset;
        SfntLanguageTagRecord[] languageTags = ParseLanguageTags(
            tableData,
            storageOffset,
            storageLength,
            languageTagLengths,
            languageTagOffsets,
            limits);
        SfntNameRecord[] records = new SfntNameRecord[recordCount];
        int[] recordCounts = ParseNameRecords(
            tableData,
            version,
            storageOffset,
            storageLength,
            platformIds,
            encodingIds,
            languageIds,
            nameIds,
            lengths,
            stringOffsets,
            languageTags,
            limits,
            records);

        return new SfntNameTable(
            version,
            storageOffset,
            storageLength,
            records,
            languageTags,
            recordCounts[0],
            recordCounts[1],
            CountInvalidLanguageTags(languageTags));
    }

    static SfntLanguageTagRecord[] ParseLanguageTags(
        FontDataSlice tableData,
        int storageOffset,
        int storageLength,
        int[] lengths,
        int[] stringOffsets,
        FontLimits limits)
    {
        SfntLanguageTagRecord[] records = new SfntLanguageTagRecord[lengths.Length];
        int index = 0;

        while (index < records.Length)
        {
            ValidateStringRange(
                storageLength,
                stringOffsets[index],
                lengths[index],
                limits);
            FontDataSlice stringData = tableData.GetSlice(
                storageOffset + stringOffsets[index],
                lengths[index]);
            string? languageTag = SfntNameStringValidator.ReadLanguageTagOrNull(stringData);
            records[index] = new SfntLanguageTagRecord(
                0x8000 + index,
                lengths[index],
                stringOffsets[index],
                languageTag,
                languageTag != null,
                stringData);
            index += 1;
        }

        return records;
    }

    static int[] ParseNameRecords(
        FontDataSlice tableData,
        int version,
        int storageOffset,
        int storageLength,
        int[] platformIds,
        int[] encodingIds,
        int[] languageIds,
        int[] nameIds,
        int[] lengths,
        int[] stringOffsets,
        SfntLanguageTagRecord[] languageTags,
        FontLimits limits,
        SfntNameRecord[] records)
    {
        int duplicateRecordCount = 0;
        int invalidRecordCount = 0;
        int index = 0;

        while (index < records.Length)
        {
            ValidateRecordOrder(
                index,
                platformIds,
                encodingIds,
                languageIds,
                nameIds);
            bool validLanguage = IsValidLanguageId(
                version,
                platformIds[index],
                languageIds[index],
                languageTags);
            ValidateStringRange(
                storageLength,
                stringOffsets[index],
                lengths[index],
                limits);
            FontDataSlice stringData = tableData.GetSlice(
                storageOffset + stringOffsets[index],
                lengths[index]);
            SfntNameEncodingKind encodingKind = GetEncodingKind(
                platformIds[index],
                encodingIds[index]);

            bool validString = lengths[index] != 0;

            if (encodingKind == SfntNameEncodingKind.Utf16BigEndian)
            {
                validString = validString &&
                    SfntNameStringValidator.IsValidUtf16BigEndian(stringData);
            }

            if (!validLanguage || !validString)
            {
                invalidRecordCount += 1;
            }

            bool duplicateKey = IsDuplicateKey(
                index,
                platformIds,
                encodingIds,
                languageIds,
                nameIds);

            if (duplicateKey)
            {
                duplicateRecordCount += 1;
            }

            records[index] = new SfntNameRecord(
                platformIds[index],
                encodingIds[index],
                languageIds[index],
                nameIds[index],
                lengths[index],
                stringOffsets[index],
                encodingKind,
                version >= 1 && languageIds[index] >= 0x8000,
                validLanguage,
                validString,
                duplicateKey,
                stringData);
            index += 1;
        }

        return new int[] { duplicateRecordCount, invalidRecordCount };
    }

    static void ValidateStringRange(
        int storageLength,
        int stringOffset,
        int length,
        FontLimits limits)
    {
        if (length > limits.GetMaxStringBytes())
        {
            throw FontLimitException.Create(
                "A name string exceeds the configured encoded-string limit.");
        }

        if (stringOffset > storageLength || length > storageLength - stringOffset)
        {
            throw FontFormatException.Create("A name string is outside the storage area.");
        }
    }

    static bool IsValidLanguageId(
        int version,
        int platformId,
        int languageId,
        SfntLanguageTagRecord[] languageTags)
    {
        if (languageId < 0x8000)
        {
            return true;
        }

        if (version == 0)
        {
            if (platformId < 240 || platformId > 255)
            {
                return false;
            }

            return true;
        }

        int languageTagIndex = languageId - 0x8000;

        if (languageTagIndex >= languageTags.Length)
        {
            return false;
        }

        return languageTags[languageTagIndex].IsValid();
    }

    static int CountInvalidLanguageTags(SfntLanguageTagRecord[] languageTags)
    {
        int count = 0;
        int index = 0;

        while (index < languageTags.Length)
        {
            if (!languageTags[index].IsValid())
            {
                count += 1;
            }

            index += 1;
        }

        return count;
    }

    static void ValidateRecordOrder(
        int index,
        int[] platformIds,
        int[] encodingIds,
        int[] languageIds,
        int[] nameIds)
    {
        if (index == 0)
        {
            return;
        }

        int comparison = CompareKey(
            platformIds[index - 1],
            encodingIds[index - 1],
            languageIds[index - 1],
            nameIds[index - 1],
            platformIds[index],
            encodingIds[index],
            languageIds[index],
            nameIds[index]);

        if (comparison > 0)
        {
            throw FontFormatException.Create("The name records are not in required key order.");
        }
    }

    static bool IsDuplicateKey(
        int index,
        int[] platformIds,
        int[] encodingIds,
        int[] languageIds,
        int[] nameIds)
    {
        if (index == 0)
        {
            return false;
        }

        return CompareKey(
            platformIds[index - 1],
            encodingIds[index - 1],
            languageIds[index - 1],
            nameIds[index - 1],
            platformIds[index],
            encodingIds[index],
            languageIds[index],
            nameIds[index]) == 0;
    }

    static int CompareKey(
        int firstPlatform,
        int firstEncoding,
        int firstLanguage,
        int firstName,
        int secondPlatform,
        int secondEncoding,
        int secondLanguage,
        int secondName)
    {
        if (firstPlatform != secondPlatform)
        {
            return firstPlatform < secondPlatform ? -1 : 1;
        }

        if (firstEncoding != secondEncoding)
        {
            return firstEncoding < secondEncoding ? -1 : 1;
        }

        if (firstLanguage != secondLanguage)
        {
            return firstLanguage < secondLanguage ? -1 : 1;
        }

        if (firstName != secondName)
        {
            return firstName < secondName ? -1 : 1;
        }

        return 0;
    }

    static SfntNameEncodingKind GetEncodingKind(int platformId, int encodingId)
    {
        if (platformId == 0)
        {
            return SfntNameEncodingKind.Utf16BigEndian;
        }

        if (platformId == 1)
        {
            if (encodingId == 0)
            {
                return SfntNameEncodingKind.MacintoshRoman;
            }

            return SfntNameEncodingKind.MacintoshLegacy;
        }

        if (platformId == 3)
        {
            if (encodingId == 2)
            {
                return SfntNameEncodingKind.WindowsCodePage932;
            }

            if (encodingId == 3)
            {
                return SfntNameEncodingKind.WindowsCodePage936;
            }

            if (encodingId == 4)
            {
                return SfntNameEncodingKind.WindowsCodePage950;
            }

            if (encodingId == 5)
            {
                return SfntNameEncodingKind.WindowsCodePage949;
            }

            if (encodingId == 6 || encodingId == 7 || encodingId == 8 ||
                encodingId == 9)
            {
                return SfntNameEncodingKind.Unknown;
            }

            return SfntNameEncodingKind.Utf16BigEndian;
        }

        return SfntNameEncodingKind.Unknown;
    }
}

internal static class SfntNameStringValidator
{
    internal static bool IsValidUtf16BigEndian(FontDataSlice stringData)
    {
        if (stringData == null)
        {
            throw new System.ArgumentNullException("stringData");
        }

        ReadOnlySpan<byte> bytes = stringData.GetMemory().Span;

        if ((bytes.Length & 1) != 0)
        {
            return false;
        }

        int index = 0;

        while (index < bytes.Length)
        {
            int codeUnit = bytes[index] << 8 | bytes[index + 1];

            if (codeUnit >= 0xD800 && codeUnit <= 0xDBFF)
            {
                if (index + 3 >= bytes.Length)
                {
                    return false;
                }

                int lowSurrogate = bytes[index + 2] << 8 | bytes[index + 3];

                if (lowSurrogate < 0xDC00 || lowSurrogate > 0xDFFF)
                {
                    return false;
                }

                index += 4;
                continue;
            }

            if (codeUnit >= 0xDC00 && codeUnit <= 0xDFFF)
            {
                return false;
            }

            index += 2;
        }

        return true;
    }

    internal static string? ReadLanguageTagOrNull(FontDataSlice stringData)
    {
        if (!IsValidUtf16BigEndian(stringData))
        {
            return null;
        }

        ReadOnlySpan<byte> bytes = stringData.GetMemory().Span;

        if (bytes.Length == 0)
        {
            return null;
        }

        char[] characters = new char[bytes.Length / 2];
        int index = 0;
        int subtagLength = 0;

        while (index < characters.Length)
        {
            int codeUnit = bytes[index * 2] << 8 | bytes[index * 2 + 1];
            bool alphaNumeric = codeUnit >= 'A' && codeUnit <= 'Z' ||
                codeUnit >= 'a' && codeUnit <= 'z' ||
                codeUnit >= '0' && codeUnit <= '9';

            if (codeUnit == '-')
            {
                if (subtagLength == 0)
                {
                    return null;
                }

                subtagLength = 0;
            }
            else
            {
                if (!alphaNumeric)
                {
                    return null;
                }

                subtagLength += 1;

                if (subtagLength > 8)
                {
                    return null;
                }
            }

            characters[index] = (char)codeUnit;
            index += 1;
        }

        if (subtagLength == 0)
        {
            return null;
        }

        return new string(characters);
    }
}
