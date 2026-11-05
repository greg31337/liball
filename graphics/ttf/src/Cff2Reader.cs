namespace ttf;

internal class Cff2Header
{
    int majorVersion;
    int minorVersion;
    int headerSize;
    int topDictionarySize;
    byte[] extensionData;

    internal Cff2Header(
        int majorVersion,
        int minorVersion,
        int headerSize,
        int topDictionarySize,
        byte[] extensionData)
    {
        this.majorVersion = majorVersion;
        this.minorVersion = minorVersion;
        this.headerSize = headerSize;
        this.topDictionarySize = topDictionarySize;
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

    internal int GetTopDictionarySize()
    {
        return topDictionarySize;
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

internal class Cff2Index
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

    internal Cff2Index(
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

    internal int GetStartOffset() { return startOffset; }
    internal int GetLength() { return length; }
    internal int GetNextOffset() { return CheckedFontMath.AddOffset(startOffset, length); }
    internal int GetCount() { return count; }
    internal bool IsEmpty() { return count == 0; }
    internal int GetOffsetSize() { return offsetSize; }
    internal int GetDataOffset() { return dataOffset; }
    internal bool UsesMinimalOffsetSize() { return minimalOffsetSize; }
    internal bool IsCanonical() { return count == 0 || minimalOffsetSize; }
    internal bool HasEmptyObjects() { return emptyObjects; }

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
        int offset = CheckedFontMath.AddOffset(dataOffset, offsets[index] - 1);
        return source.GetSlice(offset, GetObjectLength(index));
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

internal static class Cff2IndexParser
{
    internal static Cff2Index Parse(
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

        if (maximumCount < 0)
        {
            throw new System.ArgumentOutOfRangeException("maximumCount");
        }

        BigEndianReader reader = new BigEndianReader(data.GetSlice(
            startOffset,
            data.GetLength() - startOffset));
        uint encodedCount = reader.ReadUInt32();

        if (encodedCount > int.MaxValue || encodedCount > (uint)maximumCount)
        {
            throw FontLimitException.Create(
                "A CFF2 INDEX exceeds the configured object limit.");
        }

        int count = (int)encodedCount;

        if (count == 0)
        {
            return new Cff2Index(
                data,
                startOffset,
                4,
                0,
                0,
                new int[0],
                CheckedFontMath.AddOffset(startOffset, 4),
                true,
                false);
        }

        int offsetSize = reader.ReadUInt8();

        if (offsetSize < 1 || offsetSize > 4)
        {
            throw FontFormatException.Create(
                "A CFF2 INDEX has an invalid offSize value.");
        }

        int offsetCount = CheckedFontMath.AddCounts(count, 1);
        int offsetBytes = CheckedFontMath.MultiplyCount(offsetCount, offsetSize);

        if (reader.GetRemainingLength() < offsetBytes)
        {
            throw FontFormatException.Create(
                "A CFF2 INDEX offset array is truncated.");
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
                    "A CFF2 INDEX offset exceeds the supported range.");
            }

            offsets[index] = (int)encoded;

            if (index == 0 && offsets[index] != 1)
            {
                throw FontFormatException.Create(
                    "A CFF2 INDEX must begin with the one-based offset 1.");
            }

            if (index != 0)
            {
                if (offsets[index] < offsets[index - 1])
                {
                    throw FontFormatException.Create(
                        "CFF2 INDEX offsets are not monotonic.");
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
                "A CFF2 INDEX object extends outside the bounded data.");
        }

        int headerLength = CheckedFontMath.AddOffset(5, offsetBytes);
        int length = CheckedFontMath.AddOffset(headerLength, dataLength);
        int dataOffset = CheckedFontMath.AddOffset(startOffset, headerLength);
        return new Cff2Index(
            data,
            startOffset,
            length,
            count,
            offsetSize,
            offsets,
            dataOffset,
            offsetSize == GetMinimumOffsetSize(offsets[offsetCount - 1]),
            emptyObjects);
    }

    static uint ReadOffset(BigEndianReader reader, int offsetSize)
    {
        if (offsetSize == 1) { return reader.ReadUInt8(); }
        if (offsetSize == 2) { return reader.ReadUInt16(); }
        if (offsetSize == 3) { return reader.ReadUInt24(); }
        return reader.ReadUInt32();
    }

    static int GetMinimumOffsetSize(int maximumOffset)
    {
        if (maximumOffset <= 0xFF) { return 1; }
        if (maximumOffset <= 0xFFFF) { return 2; }
        if (maximumOffset <= 0xFFFFFF) { return 3; }
        return 4;
    }
}

internal class Cff2VariationStoreReference
{
    int offset;
    int length;
    FontDataSlice data;

    internal Cff2VariationStoreReference(int offset, int length, FontDataSlice data)
    {
        this.offset = offset;
        this.length = length;
        this.data = data;
    }

    internal int GetOffset() { return offset; }
    internal int GetLength() { return length; }
    internal FontDataSlice GetData() { return data; }
    internal byte[] CopyData() { return data.CopyBytes(); }
}

internal class SfntCff2Table
{
    Cff2Header header;
    Cff2TopDictionary topDictionary;
    Cff2Index globalSubroutineIndex;
    FontDataSlice data;
    int fixedDataEnd;
    FontLimits limits;
    Cff2Index? charStringsIndex;
    Cff2FontDictionaryArray? fontDictionaryArray;
    Cff2FontDictionarySelect? fontDictionarySelect;
    Cff2VariationStoreReference? variationStore;
    Cff2VariationStore? variationData;

    internal SfntCff2Table(
        Cff2Header header,
        Cff2TopDictionary topDictionary,
        Cff2Index globalSubroutineIndex,
        FontDataSlice data,
        int fixedDataEnd,
        FontLimits limits)
    {
        this.header = header;
        this.topDictionary = topDictionary;
        this.globalSubroutineIndex = globalSubroutineIndex;
        this.data = data;
        this.fixedDataEnd = fixedDataEnd;
        this.limits = limits;
    }

    internal Cff2Header GetHeader() { return header; }
    internal Cff2TopDictionary GetTopDictionary() { return topDictionary; }
    internal Cff2Index GetGlobalSubroutineIndex() { return globalSubroutineIndex; }
    internal int GetFixedDataEnd() { return fixedDataEnd; }

    internal Cff2Index GetCharStringsIndex()
    {
        lock (this)
        {
            if (charStringsIndex == null)
            {
                int offset = topDictionary.GetCharStringsOffset();
                ValidateReferencedOffset(offset, "CharStrings");
                Cff2Index parsed = Cff2IndexParser.Parse(
                    data,
                    offset,
                    limits.GetMaxGlyphsPerFace());

                if (parsed.GetCount() == 0)
                {
                    throw FontFormatException.Create(
                        "A CFF2 CharStrings INDEX must contain at least .notdef.");
                }

                charStringsIndex = parsed;
            }

            return charStringsIndex;
        }
    }

    internal Cff2FontDictionaryArray GetFontDictionaryArray()
    {
        lock (this)
        {
            if (fontDictionaryArray == null)
            {
                int offset = topDictionary.GetFontDictionaryArrayOffset();
                ValidateReferencedOffset(offset, "FDArray");
                Cff2FontDictionaryArray parsed =
                    Cff2FontDictionaryArrayParser.Parse(
                        data,
                        offset,
                        limits.GetMaxGlyphsPerFace());

                if (parsed.GetCount() == 1 &&
                    topDictionary.HasFontDictionarySelect())
                {
                    throw FontFormatException.Create(
                        "A one-Font-DICT CFF2 font must omit FDSelect.");
                }

                if (parsed.GetCount() > 1 &&
                    !topDictionary.HasFontDictionarySelect())
                {
                    throw FontFormatException.Create(
                        "A multi-Font-DICT CFF2 font must contain FDSelect.");
                }

                ValidatePrivateOffsets(parsed);
                ValidatePrivateVariationReferences(parsed);
                fontDictionaryArray = parsed;
            }

            return fontDictionaryArray;
        }
    }

    internal Cff2FontDictionarySelect? GetFontDictionarySelect()
    {
        Cff2FontDictionaryArray array = GetFontDictionaryArray();

        if (array.GetCount() == 1)
        {
            return null;
        }

        lock (this)
        {
            if (fontDictionarySelect == null)
            {
                int offset = topDictionary.GetFontDictionarySelectOffset();
                ValidateReferencedOffset(offset, "FDSelect");
                fontDictionarySelect =
                    Cff2FontDictionarySelectParser.Parse(
                        data,
                        offset,
                        GetCharStringsIndex().GetCount(),
                        array.GetCount());
            }

            return fontDictionarySelect;
        }
    }

    internal Cff2FontDictionaryRecord GetFontDictionaryForGlyph(int glyphIndex)
    {
        Cff2Index charStrings = GetCharStringsIndex();

        if (glyphIndex < 0 || glyphIndex >= charStrings.GetCount())
        {
            throw new System.ArgumentOutOfRangeException("glyphIndex");
        }

        Cff2FontDictionaryArray array = GetFontDictionaryArray();
        int index = 0;
        if (array.GetCount() != 1)
        {
            Cff2FontDictionarySelect? select = GetFontDictionarySelect();
            System.ArgumentNullException.ThrowIfNull(select);
            index = select.GetFontDictionaryIndex(glyphIndex);
        }
        return array.GetRecord(index);
    }

    internal int GetFontDictionaryIndexForGlyph(int glyphIndex)
    {
        Cff2Index charStrings = GetCharStringsIndex();

        if (glyphIndex < 0 || glyphIndex >= charStrings.GetCount())
        {
            throw new System.ArgumentOutOfRangeException("glyphIndex");
        }

        Cff2FontDictionaryArray array = GetFontDictionaryArray();
        if (array.GetCount() == 1)
        {
            return 0;
        }
        Cff2FontDictionarySelect? select = GetFontDictionarySelect();
        System.ArgumentNullException.ThrowIfNull(select);
        return select.GetFontDictionaryIndex(glyphIndex);
    }

    internal Cff2VariationStoreReference? GetVariationStore()
    {
        if (!topDictionary.HasVariationStore())
        {
            return null;
        }

        lock (this)
        {
            if (variationStore == null)
            {
                int offset = topDictionary.GetVariationStoreOffset();
                ValidateReferencedOffset(offset, "VariationStore");
                FontDataSlice remaining = data.GetSlice(
                    offset,
                    data.GetLength() - offset);
                BigEndianReader reader = new BigEndianReader(remaining);
                int length = reader.ReadUInt16();

                if (length > reader.GetRemainingLength())
                {
                    throw FontFormatException.Create(
                        "A CFF2 VariationStore extends outside the bounded table.");
                }

                variationStore = new Cff2VariationStoreReference(
                    offset,
                    length,
                    remaining.GetSlice(2, length));
            }

            return variationStore;
        }
    }

    internal Cff2VariationStore? GetVariationData()
    {
        Cff2VariationStoreReference? reference = GetVariationStore();

        if (reference == null)
        {
            return null;
        }

        lock (this)
        {
            if (variationData == null)
            {
                variationData = Cff2VariationStoreParser.Parse(
                    reference,
                    limits.GetMaxVariationRegions(),
                    limits.GetMaxCoverageEntries());
            }

            return variationData;
        }
    }

    internal Cff2PrivateDictionary GetEvaluatedPrivateDictionary(
        int fontDictionaryIndex,
        double[] normalizedCoordinates)
    {
        Cff2FontDictionaryRecord record =
            GetFontDictionaryArray().GetRecord(fontDictionaryIndex);
        return record.GetPrivateDictionary().Evaluate(
            GetVariationData(),
            normalizedCoordinates);
    }

    internal Type2CharStringResult ExecuteCharString(
        int glyphIndex,
        double[]? normalizedCoordinates)
    {
        Cff2FontDictionaryArray array = GetFontDictionaryArray();
        Cff2FontDictionarySelect? select = GetFontDictionarySelect();
        int fontDictionaryIndex = 0;
        if (array.GetCount() != 1)
        {
            System.ArgumentNullException.ThrowIfNull(select);
            fontDictionaryIndex = select.GetFontDictionaryIndex(glyphIndex);
        }
        Cff2PrivateDictionary privateDictionary =
            array.GetRecord(fontDictionaryIndex).GetPrivateDictionary();
        Cff2VariationStore? store = GetVariationData();

        if (store == null)
        {
            if (normalizedCoordinates == null ||
                normalizedCoordinates.Length != 0)
            {
                throw new System.ArgumentException(
                    "A non-variable CFF2 font requires no normalized coordinates.",
                    "normalizedCoordinates");
            }
        }
        else
        {
            store.ValidateCoordinates(normalizedCoordinates);
        }

        return Type2CharStringExecutor.ExecuteCff2(
            GetCharStringsIndex().GetObject(glyphIndex),
            privateDictionary.GetVariationStoreDataIndex(),
            GetLocalSubroutineIndex(fontDictionaryIndex),
            globalSubroutineIndex,
            store,
            normalizedCoordinates,
            limits.GetMaxCffSubroutineDepth(),
            Type2CharStringExecutor.GetDefaultMaximumOperationCount());
    }

    internal CffGlyph GetNormalizedGlyph(
        int glyphIndex,
        double[] normalizedCoordinates)
    {
        return CffGlyphNormalizer.Normalize(
            this,
            glyphIndex,
            normalizedCoordinates,
            limits);
    }

    internal Cff2Index? GetLocalSubroutineIndex(int fontDictionaryIndex)
    {
        Cff2FontDictionaryRecord record =
            GetFontDictionaryArray().GetRecord(fontDictionaryIndex);
        Cff2PrivateDictionary privateDictionary = record.GetPrivateDictionary();

        if (!privateDictionary.HasLocalSubroutines())
        {
            return null;
        }

        Cff2FontDictionary fontDictionary = record.GetFontDictionary();
        int relativeOffset = privateDictionary.GetLocalSubroutinesOffset();

        if (relativeOffset < fontDictionary.GetPrivateDictionarySize())
        {
            throw FontFormatException.Create(
                "A CFF2 local Subrs INDEX overlaps its Private DICT.");
        }

        int offset = CheckedFontMath.AddOffset(
            fontDictionary.GetPrivateDictionaryOffset(),
            relativeOffset);
        ValidateReferencedOffset(offset, "local Subrs");
        return Cff2IndexParser.Parse(
            data,
            offset,
            limits.GetMaxGlyphsPerFace());
    }

    void ValidatePrivateVariationReferences(Cff2FontDictionaryArray array)
    {
        bool hasVariationStore = topDictionary.HasVariationStore();
        int index = 0;

        while (index < array.GetCount())
        {
            Cff2PrivateDictionary dictionary =
                array.GetRecord(index).GetPrivateDictionary();

            if ((dictionary.HasVariationStoreDataIndex() ||
                dictionary.HasBlend()) && !hasVariationStore)
            {
                throw FontFormatException.Create(
                    "A CFF2 Private DICT references variations without a VariationStore.");
            }

            index += 1;
        }
    }

    void ValidatePrivateOffsets(Cff2FontDictionaryArray array)
    {
        int index = 0;

        while (index < array.GetCount())
        {
            Cff2FontDictionary dictionary =
                array.GetRecord(index).GetFontDictionary();

            if (dictionary.GetPrivateDictionarySize() != 0)
            {
                ValidateReferencedOffset(
                    dictionary.GetPrivateDictionaryOffset(),
                    "Private DICT");
            }

            index += 1;
        }
    }

    void ValidateReferencedOffset(int offset, string description)
    {
        if (offset < fixedDataEnd || offset >= data.GetLength())
        {
            throw FontFormatException.Create(
                $"The CFF2 {description} offset is outside the referenced-data region.");
        }
    }
}

internal static class SfntCff2TableParser
{
    const int RequiredHeaderLength = 5;
    static readonly SfntTableParser parser = new SfntTableParser(
        FontTags.Cff2,
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

    internal static SfntCff2Table? FindTable(SfntTableCache cache)
    {
        SfntParsedTableView? view = FindView(cache);

        if (view == null || !view.HasParsedData())
        {
            return null;
        }

        return (SfntCff2Table)view.GetParsedData();
    }

    static SfntTableVersion ReadVersion(SfntTableData table)
    {
        BigEndianReader reader = new BigEndianReader(table.GetDataSlice());
        return new SfntTableVersion(reader.ReadUInt8(), reader.ReadUInt8());
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
            throw FontFormatException.Create("The CFF2 header is truncated.");
        }

        BigEndianReader reader = new BigEndianReader(data);
        int majorVersion = reader.ReadUInt8();
        int minorVersion = reader.ReadUInt8();
        int headerSize = reader.ReadUInt8();
        int topDictionarySize = reader.ReadUInt16();

        if (majorVersion != 2)
        {
            throw FontFormatException.Create(
                "The CFF2 table does not contain a version 2 header.");
        }

        if (headerSize < RequiredHeaderLength || headerSize > data.GetLength())
        {
            throw FontFormatException.Create(
                "The CFF2 header size is outside the bounded table.");
        }

        if (topDictionarySize == 0)
        {
            throw FontFormatException.Create("The CFF2 Top DICT must not be empty.");
        }

        int topDictionaryEnd = CheckedFontMath.AddOffset(
            headerSize,
            topDictionarySize);

        if (topDictionaryEnd > data.GetLength())
        {
            throw FontFormatException.Create(
                "The CFF2 Top DICT extends outside the bounded table.");
        }

        Cff2Header header = new Cff2Header(
            majorVersion,
            minorVersion,
            headerSize,
            topDictionarySize,
            data.GetSlice(
                RequiredHeaderLength,
                headerSize - RequiredHeaderLength).CopyBytes());
        Cff2TopDictionary topDictionary = Cff2TopDictionary.Parse(
            data.GetSlice(headerSize, topDictionarySize));
        Cff2Index globalSubroutineIndex = Cff2IndexParser.Parse(
            data,
            topDictionaryEnd,
            limits.GetMaxGlyphsPerFace());
        return new SfntCff2Table(
            header,
            topDictionary,
            globalSubroutineIndex,
            data,
            globalSubroutineIndex.GetNextOffset(),
            limits);
    }
}
