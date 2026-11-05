namespace ttf;

internal class CffHeader
{
    int majorVersion;
    int minorVersion;
    int headerSize;
    int absoluteOffsetSize;
    byte[] extensionData;

    internal CffHeader(
        int majorVersion,
        int minorVersion,
        int headerSize,
        int absoluteOffsetSize,
        byte[] extensionData)
    {
        this.majorVersion = majorVersion;
        this.minorVersion = minorVersion;
        this.headerSize = headerSize;
        this.absoluteOffsetSize = absoluteOffsetSize;
        this.extensionData = extensionData;
    }

    internal int GetMajorVersion()
    {
        return majorVersion;
    }

    internal int GetMinorVersion()
    {
        return minorVersion;
    }

    internal int GetHeaderSize()
    {
        return headerSize;
    }

    internal int GetAbsoluteOffsetSize()
    {
        return absoluteOffsetSize;
    }

    internal int GetExtensionLength()
    {
        return extensionData.Length;
    }

    internal byte[] CopyExtensionData()
    {
        return extensionData.AsSpan().ToArray();
    }
}

internal class CffIndex
{
    FontDataSlice source;
    int startOffset;
    int length;
    int count;
    int offsetSize;
    int[] offsets;
    int dataOffset;
    bool minimalOffsetSize;
    bool emptyObjects;

    internal CffIndex(
        FontDataSlice source,
        int startOffset,
        int length,
        int count,
        int offsetSize,
        int[] offsets,
        int dataOffset,
        bool minimalOffsetSize,
        bool emptyObjects)
    {
        this.source = source;
        this.startOffset = startOffset;
        this.length = length;
        this.count = count;
        this.offsetSize = offsetSize;
        this.offsets = offsets;
        this.dataOffset = dataOffset;
        this.minimalOffsetSize = minimalOffsetSize;
        this.emptyObjects = emptyObjects;
    }

    internal int GetStartOffset()
    {
        return startOffset;
    }

    internal int GetLength()
    {
        return length;
    }

    internal int GetNextOffset()
    {
        return CheckedFontMath.AddOffset(startOffset, length);
    }

    internal int GetCount()
    {
        return count;
    }

    internal bool IsEmpty()
    {
        return count == 0;
    }

    internal int GetOffsetSize()
    {
        return offsetSize;
    }

    internal int GetDataOffset()
    {
        return dataOffset;
    }

    internal bool UsesMinimalOffsetSize()
    {
        return minimalOffsetSize;
    }

    internal bool IsCanonical()
    {
        return count == 0 || minimalOffsetSize;
    }

    internal bool HasEmptyObjects()
    {
        return emptyObjects;
    }

    internal int GetEncodedOffset(int index)
    {
        if (index < 0 || index >= offsets.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return offsets[index];
    }

    internal int GetObjectLength(int index)
    {
        ValidateObjectIndex(index);
        return offsets[index + 1] - offsets[index];
    }

    internal FontDataSlice GetObject(int index)
    {
        ValidateObjectIndex(index);
        int objectOffset = CheckedFontMath.AddOffset(
            dataOffset,
            offsets[index] - 1);
        return source.GetSlice(objectOffset, GetObjectLength(index));
    }

    internal byte[] CopyObject(int index)
    {
        return GetObject(index).CopyBytes();
    }

    void ValidateObjectIndex(int index)
    {
        if (index < 0 || index >= count)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }
    }
}

internal static class CffIndexParser
{
    internal static CffIndex Parse(FontDataSlice data, int startOffset)
    {
        return Parse(data, startOffset, 65535);
    }

    internal static CffIndex Parse(
        FontDataSlice data,
        int startOffset,
        int maximumCount)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        if (startOffset < 0 || startOffset > data.GetLength())
        {
            throw new System.ArgumentOutOfRangeException("startOffset");
        }

        if (maximumCount <= 0)
        {
            throw new System.ArgumentOutOfRangeException("maximumCount");
        }

        FontDataSlice remaining = data.GetSlice(
            startOffset,
            data.GetLength() - startOffset);
        BigEndianReader reader = new BigEndianReader(remaining);
        int count = reader.ReadUInt16();

        if (count > maximumCount)
        {
            throw FontLimitException.Create(
                "A CFF INDEX exceeds its configured entry limit.");
        }

        if (count == 0)
        {
            return new CffIndex(
                data,
                startOffset,
                2,
                0,
                0,
                new int[0],
                CheckedFontMath.AddOffset(startOffset, 2),
                true,
                false);
        }

        int offsetSize = reader.ReadUInt8();

        if (offsetSize < 1 || offsetSize > 4)
        {
            throw FontFormatException.Create(
                "A CFF INDEX has an invalid offSize value.");
        }

        int offsetCount = CheckedFontMath.AddCounts(count, 1);
        int offsetBytes = CheckedFontMath.MultiplyCount(
            offsetCount,
            offsetSize);

        if (reader.GetRemainingLength() < offsetBytes)
        {
            throw FontFormatException.Create(
                "A CFF INDEX offset array is truncated.");
        }

        int[] offsets = new int[offsetCount];
        bool emptyObjects = false;
        int index = 0;

        while (index < offsetCount)
        {
            uint encoded = ReadOffset(reader, offsetSize);

            if (encoded > int.MaxValue)
            {
                throw FontFormatException.Create(
                    "A CFF INDEX offset exceeds the supported range.");
            }

            offsets[index] = (int)encoded;

            if (index == 0 && offsets[index] != 1)
            {
                throw FontFormatException.Create(
                    "A CFF INDEX must begin with the one-based offset 1.");
            }

            if (index != 0)
            {
                if (offsets[index] < offsets[index - 1])
                {
                    throw FontFormatException.Create(
                        "CFF INDEX offsets are not monotonic.");
                }

                if (offsets[index] == offsets[index - 1])
                {
                    emptyObjects = true;
                }
            }

            index += 1;
        }

        int dataLength = offsets[offsetCount - 1] - 1;

        if (dataLength > reader.GetRemainingLength())
        {
            throw FontFormatException.Create(
                "A CFF INDEX object extends outside the bounded data.");
        }

        int headerLength = CheckedFontMath.AddOffset(3, offsetBytes);
        int length = CheckedFontMath.AddOffset(headerLength, dataLength);
        int dataOffset = CheckedFontMath.AddOffset(startOffset, headerLength);
        int minimumOffsetSize = GetMinimumOffsetSize(
            offsets[offsetCount - 1]);
        return new CffIndex(
            data,
            startOffset,
            length,
            count,
            offsetSize,
            offsets,
            dataOffset,
            offsetSize == minimumOffsetSize,
            emptyObjects);
    }

    static uint ReadOffset(BigEndianReader reader, int offsetSize)
    {
        if (offsetSize == 1)
        {
            return reader.ReadUInt8();
        }

        if (offsetSize == 2)
        {
            return reader.ReadUInt16();
        }

        if (offsetSize == 3)
        {
            return reader.ReadUInt24();
        }

        return reader.ReadUInt32();
    }

    static int GetMinimumOffsetSize(int maximumOffset)
    {
        if (maximumOffset <= 0xFF)
        {
            return 1;
        }

        if (maximumOffset <= 0xFFFF)
        {
            return 2;
        }

        if (maximumOffset <= 0xFFFFFF)
        {
            return 3;
        }

        return 4;
    }
}

internal class SfntCffTable
{
    CffHeader header;
    CffIndex nameIndex;
    CffIndex topDictionaryIndex;
    CffIndex stringIndex;
    CffIndex globalSubroutineIndex;
    FontDataSlice data;
    int remainingDataOffset;
    FontLimits limits;
    CffStringTable? stringTable;
    CffTopDictionary? topDictionary;
    CffPrivateDictionary? privateDictionary;
    CffIndex? charStringsIndex;
    CffCharset? charset;
    CffEncoding? encoding;
    CffFontDictionaryArray? fontDictionaryArray;
    CffFontDictionarySelect? fontDictionarySelect;
    CffCidMetadata? cidMetadata;
    CffGlyph[]? normalizedGlyphs;

    internal SfntCffTable(
        CffHeader header,
        CffIndex nameIndex,
        CffIndex topDictionaryIndex,
        CffIndex stringIndex,
        CffIndex globalSubroutineIndex,
        FontDataSlice data,
        int remainingDataOffset,
        FontLimits limits)
    {
        this.header = header;
        this.nameIndex = nameIndex;
        this.topDictionaryIndex = topDictionaryIndex;
        this.stringIndex = stringIndex;
        this.globalSubroutineIndex = globalSubroutineIndex;
        this.data = data;
        this.remainingDataOffset = remainingDataOffset;
        this.limits = limits;
    }

    internal CffHeader GetHeader()
    {
        return header;
    }

    internal CffIndex GetNameIndex()
    {
        return nameIndex;
    }

    internal CffIndex GetTopDictionaryIndex()
    {
        return topDictionaryIndex;
    }

    internal CffIndex GetStringIndex()
    {
        return stringIndex;
    }

    internal CffIndex GetGlobalSubroutineIndex()
    {
        return globalSubroutineIndex;
    }

    internal int GetRemainingDataOffset()
    {
        return remainingDataOffset;
    }

    internal int GetRemainingDataLength()
    {
        return data.GetLength() - remainingDataOffset;
    }

    internal FontDataSlice GetRemainingData()
    {
        return data.GetSlice(
            remainingDataOffset,
            GetRemainingDataLength());
    }

    internal CffStringTable GetStringTable()
    {
        lock (this)
        {
            if (stringTable == null)
            {
                stringTable = new CffStringTable(stringIndex);
            }

            return stringTable;
        }
    }

    internal CffTopDictionary GetTopDictionary()
    {
        lock (this)
        {
            if (topDictionary == null)
            {
                topDictionary = CffTopDictionary.Parse(
                    topDictionaryIndex.GetObject(0),
                    GetStringTable());
            }

            return topDictionary;
        }
    }

    internal CffPrivateDictionary? GetPrivateDictionary()
    {
        CffTopDictionary top = GetTopDictionary();

        if (!top.HasPrivateDictionary())
        {
            return null;
        }

        lock (this)
        {
            if (privateDictionary == null)
            {
                int size = top.GetPrivateDictionarySize();
                int offset = top.GetPrivateDictionaryOffset();
                int end = CheckedFontMath.AddOffset(offset, size);

                if (end > data.GetLength())
                {
                    throw FontFormatException.Create(
                        "A CFF Private DICT extends outside the bounded table.");
                }

                privateDictionary = CffPrivateDictionary.Parse(
                    data.GetSlice(offset, size));
            }

            return privateDictionary;
        }
    }

    internal CffIndex GetCharStringsIndex()
    {
        lock (this)
        {
            if (charStringsIndex == null)
            {
                CffIndex parsed = CffIndexParser.Parse(
                    data,
                    GetTopDictionary().GetCharStringsOffset(),
                    limits.GetMaxGlyphsPerFace());

                if (parsed.GetCount() == 0)
                {
                    throw FontFormatException.Create(
                        "A CFF CharStrings INDEX must contain at least .notdef.");
                }

                if (parsed.GetCount() > limits.GetMaxGlyphsPerFace())
                {
                    throw FontLimitException.Create(
                        "The CFF CharStrings INDEX exceeds the configured glyph limit.");
                }

                charStringsIndex = parsed;
            }

            return charStringsIndex;
        }
    }

    internal CffCharset GetCharset()
    {
        lock (this)
        {
            if (charset == null)
            {
                CffTopDictionary top = GetTopDictionary();
                charset = CffCharsetParser.Parse(
                    data,
                    top.GetCharsetOffset(),
                    GetCharStringsIndex().GetCount(),
                    top.IsCidKeyed(),
                    GetStringTable());
            }

            return charset;
        }
    }

    internal CffEncoding? GetEncoding()
    {
        CffTopDictionary top = GetTopDictionary();

        if (top.IsCidKeyed())
        {
            if (top.HasEncoding())
            {
                throw FontFormatException.Create(
                    "A CID-keyed CFF font must not define an Encoding operator.");
            }

            return null;
        }

        lock (this)
        {
            if (encoding == null)
            {
                encoding = CffEncodingParser.Parse(
                    data,
                    top.GetEncodingOffset(),
                    GetCharset());
            }

            return encoding;
        }
    }

    internal CffFontDictionaryArray? GetFontDictionaryArray()
    {
        CffTopDictionary top = GetTopDictionary();

        if (!top.IsCidKeyed())
        {
            return null;
        }

        lock (this)
        {
            if (fontDictionaryArray == null)
            {
                fontDictionaryArray = CffFontDictionaryArrayParser.Parse(
                    data,
                    top.GetFontDictionaryArrayOffset(),
                    GetStringTable());
            }

            return fontDictionaryArray;
        }
    }

    internal CffFontDictionarySelect? GetFontDictionarySelect()
    {
        CffTopDictionary top = GetTopDictionary();

        if (!top.IsCidKeyed())
        {
            return null;
        }

        lock (this)
        {
            if (fontDictionarySelect == null)
            {
                CffFontDictionaryArray? fontDictionaries = GetFontDictionaryArray();
                System.ArgumentNullException.ThrowIfNull(fontDictionaries);
                fontDictionarySelect = CffFontDictionarySelectParser.Parse(
                    data,
                    top.GetFontDictionarySelectOffset(),
                    GetCharStringsIndex().GetCount(),
                    fontDictionaries.GetCount());
            }

            return fontDictionarySelect;
        }
    }

    internal int GetFontDictionaryIndexForGlyph(int glyphIndex)
    {
        CffIndex charStrings = GetCharStringsIndex();

        if (glyphIndex < 0 || glyphIndex >= charStrings.GetCount())
        {
            throw new System.ArgumentOutOfRangeException("glyphIndex");
        }

        if (!GetTopDictionary().IsCidKeyed())
        {
            return 0;
        }

        CffFontDictionarySelect? fontDictionarySelect = GetFontDictionarySelect();
        System.ArgumentNullException.ThrowIfNull(fontDictionarySelect);
        return fontDictionarySelect
            .GetFontDictionaryIndex(glyphIndex);
    }

    internal int GetFontDictionaryCountForSubsetting()
    {
        if (!GetTopDictionary().IsCidKeyed())
        {
            return 1;
        }
        CffFontDictionaryArray? array = GetFontDictionaryArray();
        System.ArgumentNullException.ThrowIfNull(array);
        return array.GetCount();
    }

    internal bool HasFontDictionariesForSubsetting()
    {
        return GetTopDictionary().IsCidKeyed();
    }

    internal CffIndex? GetLocalSubroutineIndexForSubsetting(
        int fontDictionaryIndex)
    {
        CffTopDictionary top = GetTopDictionary();
        CffPrivateDictionary? privateValues;
        int privateOffset;
        int privateSize;

        if (top.IsCidKeyed())
        {
            CffFontDictionaryArray? fontDictionaries = GetFontDictionaryArray();
            System.ArgumentNullException.ThrowIfNull(fontDictionaries);
            CffFontDictionaryRecord record = fontDictionaries
                .GetRecord(fontDictionaryIndex);
            privateValues = record.GetPrivateDictionary();
            privateOffset = record.GetDictionary()
                .GetPrivateDictionaryOffset();
            privateSize = record.GetDictionary()
                .GetPrivateDictionarySize();
        }
        else
        {
            if (fontDictionaryIndex != 0)
            {
                throw new System.ArgumentOutOfRangeException(
                    "fontDictionaryIndex");
            }

            privateValues = GetPrivateDictionary();
            privateOffset = top.GetPrivateDictionaryOffset();
            privateSize = top.GetPrivateDictionarySize();
        }

        if (privateValues == null)
        {
            return null;
        }

        return GetLocalSubroutineIndex(
            privateValues,
            privateOffset,
            privateSize);
    }

    internal CffCidMetadata? GetCidMetadata()
    {
        CffTopDictionary top = GetTopDictionary();

        if (!top.IsCidKeyed())
        {
            return null;
        }

        lock (this)
        {
            if (cidMetadata == null)
            {
                GetEncoding();
                cidMetadata = new CffCidMetadata(
                    top,
                    GetCharset(),
                    GetFontDictionaryArray(),
                    GetFontDictionarySelect());
            }

            return cidMetadata;
        }
    }

    internal Type2CharStringResult ExecuteCharString(int glyphIndex)
    {
        CffIndex charStrings = GetCharStringsIndex();

        if (glyphIndex < 0 || glyphIndex >= charStrings.GetCount())
        {
            throw new System.ArgumentOutOfRangeException("glyphIndex");
        }

        CffTopDictionary top = GetTopDictionary();
        CffPrivateDictionary? privateValues;
        int privateOffset;
        int privateSize;

        if (top.IsCidKeyed())
        {
            CffCidMetadata? cidMetadata = GetCidMetadata();
            System.ArgumentNullException.ThrowIfNull(cidMetadata);
            CffFontDictionaryRecord record = cidMetadata
                .GetFontDictionaryForGlyph(glyphIndex);
            privateValues = record.GetPrivateDictionary();
            privateOffset = record.GetDictionary()
                .GetPrivateDictionaryOffset();
            privateSize = record.GetDictionary()
                .GetPrivateDictionarySize();
        }
        else
        {
            privateValues = GetPrivateDictionary();
            privateOffset = top.GetPrivateDictionaryOffset();
            privateSize = top.GetPrivateDictionarySize();
        }

        System.ArgumentNullException.ThrowIfNull(privateValues);
        return Type2CharStringExecutor.Execute(
            charStrings.GetObject(glyphIndex),
            privateValues.GetNominalWidthX(),
            privateValues.GetDefaultWidthX(),
            privateValues.GetInitialRandomSeed(),
            GetLocalSubroutineIndex(
                privateValues,
                privateOffset,
                privateSize),
            globalSubroutineIndex,
            limits.GetMaxCffSubroutineDepth(),
            Type2CharStringExecutor.GetDefaultMaximumOperationCount());
    }

    internal CffGlyph GetNormalizedGlyph(int glyphIndex)
    {
        CffIndex charStrings = GetCharStringsIndex();

        if (glyphIndex < 0 || glyphIndex >= charStrings.GetCount())
        {
            throw new System.ArgumentOutOfRangeException("glyphIndex");
        }

        lock (this)
        {
            if (normalizedGlyphs == null)
            {
                normalizedGlyphs = new CffGlyph[charStrings.GetCount()];
            }

            if (normalizedGlyphs[glyphIndex] == null)
            {
                normalizedGlyphs[glyphIndex] = CffGlyphNormalizer.Normalize(
                    this,
                    glyphIndex,
                    limits);
            }

            return normalizedGlyphs[glyphIndex];
        }
    }

    CffIndex? GetLocalSubroutineIndex(
        CffPrivateDictionary privateValues,
        int privateOffset,
        int privateSize)
    {
        if (!privateValues.HasLocalSubroutines())
        {
            return null;
        }

        int relativeOffset = privateValues.GetLocalSubroutinesOffset();

        if (relativeOffset < privateSize)
        {
            throw FontFormatException.Create(
                "A CFF local Subrs INDEX overlaps its Private DICT.");
        }

        int offset = CheckedFontMath.AddOffset(
            privateOffset,
            relativeOffset);

        if (offset >= data.GetLength())
        {
            throw FontFormatException.Create(
                "A CFF local Subrs INDEX offset is outside the bounded table.");
        }

        return CffIndexParser.Parse(
            data,
            offset,
            limits.GetMaxCoverageEntries());
    }
}

internal static class SfntCffTableParser
{
    const int RequiredHeaderLength = 4;
    static readonly SfntTableParser parser = new SfntTableParser(
        FontTags.Cff,
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

    internal static SfntCffTable? FindTable(SfntTableCache cache)
    {
        SfntParsedTableView? view = FindView(cache);

        if (view == null || !view.HasParsedData())
        {
            return null;
        }

        return (SfntCffTable)view.GetParsedData();
    }

    static SfntTableVersion ReadVersion(SfntTableData table)
    {
        BigEndianReader reader = new BigEndianReader(table.GetDataSlice());
        return new SfntTableVersion(
            reader.ReadUInt8(),
            reader.ReadUInt8());
    }

    static object Parse(
        SfntTableData table,
        SfntTableVersion version,
        SfntTableVersionSupport support,
        FontLimits limits,
        SfntTableCache cache)
    {
        FontDataSlice data = table.GetDataSlice();

        if (data.GetLength() < RequiredHeaderLength)
        {
            throw FontFormatException.Create("The CFF header is truncated.");
        }

        BigEndianReader reader = new BigEndianReader(data);
        int majorVersion = reader.ReadUInt8();
        int minorVersion = reader.ReadUInt8();
        int headerSize = reader.ReadUInt8();
        int absoluteOffsetSize = reader.ReadUInt8();

        if (majorVersion != 1)
        {
            throw FontFormatException.Create(
                "The CFF table does not contain a version 1 header.");
        }

        if (headerSize < RequiredHeaderLength ||
            headerSize > data.GetLength())
        {
            throw FontFormatException.Create(
                "The CFF header size is outside the bounded table.");
        }

        if (absoluteOffsetSize < 1 || absoluteOffsetSize > 4)
        {
            throw FontFormatException.Create(
                "The CFF header has an invalid absolute offSize value.");
        }

        byte[] extensionData = data.GetSlice(
            RequiredHeaderLength,
            headerSize - RequiredHeaderLength).CopyBytes();
        CffHeader header = new CffHeader(
            majorVersion,
            minorVersion,
            headerSize,
            absoluteOffsetSize,
            extensionData);
        int maximumIndexCount = limits.GetMaxCoverageEntries();
        CffIndex nameIndex = CffIndexParser.Parse(
            data,
            headerSize,
            maximumIndexCount);
        CffIndex topDictionaryIndex = CffIndexParser.Parse(
            data,
            nameIndex.GetNextOffset(),
            maximumIndexCount);
        CffIndex stringIndex = CffIndexParser.Parse(
            data,
            topDictionaryIndex.GetNextOffset(),
            maximumIndexCount);
        CffIndex globalSubroutineIndex = CffIndexParser.Parse(
            data,
            stringIndex.GetNextOffset(),
            maximumIndexCount);

        if (nameIndex.GetCount() != 1)
        {
            throw FontFormatException.Create(
                "An OpenType CFF Name INDEX must contain exactly one font name.");
        }

        if (topDictionaryIndex.GetCount() != 1)
        {
            throw FontFormatException.Create(
                "An OpenType CFF Top DICT INDEX must contain exactly one dictionary.");
        }

        ValidateStringLengths(
            nameIndex,
            limits.GetMaxStringBytes(),
            "name");
        ValidateStringLengths(
            stringIndex,
            limits.GetMaxStringBytes(),
            "custom string");
        return new SfntCffTable(
            header,
            nameIndex,
            topDictionaryIndex,
            stringIndex,
            globalSubroutineIndex,
            data,
            globalSubroutineIndex.GetNextOffset(),
            limits);
    }

    static void ValidateStringLengths(
        CffIndex index,
        int maximumLength,
        string description)
    {
        int objectIndex = 0;

        while (objectIndex < index.GetCount())
        {
            if (index.GetObjectLength(objectIndex) > maximumLength)
            {
                throw FontLimitException.Create(
                    $"A CFF {description} exceeds the configured string-byte limit.");
            }

            objectIndex += 1;
        }
    }
}
