namespace ttf;

internal class CffFontDictionaryRecord
{
    CffFontDictionary dictionary;
    CffPrivateDictionary privateDictionary;

    internal CffFontDictionaryRecord(
        CffFontDictionary dictionary,
        CffPrivateDictionary privateDictionary)
    {
        this.dictionary = dictionary;
        this.privateDictionary = privateDictionary;
    }

    internal CffFontDictionary GetDictionary()
    {
        return dictionary;
    }

    internal CffPrivateDictionary GetPrivateDictionary()
    {
        return privateDictionary;
    }
}

internal class CffFontDictionaryArray
{
    CffIndex index;
    CffFontDictionaryRecord[] records;

    internal CffFontDictionaryArray(
        CffIndex index,
        CffFontDictionaryRecord[] records)
    {
        this.index = index;
        this.records = records;
    }

    internal CffIndex GetIndex()
    {
        return index;
    }

    internal int GetCount()
    {
        return records.Length;
    }

    internal CffFontDictionaryRecord GetRecord(int indexValue)
    {
        if (indexValue < 0 || indexValue >= records.Length)
        {
            throw new System.ArgumentOutOfRangeException("indexValue");
        }

        return records[indexValue];
    }
}

internal static class CffFontDictionaryArrayParser
{
    internal static CffFontDictionaryArray Parse(
        FontDataSlice data,
        int offset,
        CffStringTable strings)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        if (strings == null)
        {
            throw new System.ArgumentNullException("strings");
        }

        if (offset < 0 || offset >= data.GetLength())
        {
            throw FontFormatException.Create(
                "A CFF FDArray offset is outside the bounded table.");
        }

        CffIndex index = CffIndexParser.Parse(data, offset, 256);

        if (index.GetCount() == 0 || index.GetCount() > 256)
        {
            throw FontFormatException.Create(
                "A CFF 1 FDArray must contain between 1 and 256 Font DICTs.");
        }

        CffFontDictionaryRecord[] records =
            new CffFontDictionaryRecord[index.GetCount()];
        int recordIndex = 0;

        while (recordIndex < records.Length)
        {
            CffFontDictionary dictionary = CffFontDictionary.Parse(
                index.GetObject(recordIndex),
                strings);
            int privateSize = dictionary.GetPrivateDictionarySize();
            int privateOffset = dictionary.GetPrivateDictionaryOffset();
            int privateEnd = CheckedFontMath.AddOffset(
                privateOffset,
                privateSize);

            if (privateEnd > data.GetLength())
            {
                throw FontFormatException.Create(
                    "A CFF Font DICT Private DICT extends outside the bounded table.");
            }

            CffPrivateDictionary privateDictionary =
                CffPrivateDictionary.Parse(data.GetSlice(
                    privateOffset,
                    privateSize));
            records[recordIndex] = new CffFontDictionaryRecord(
                dictionary,
                privateDictionary);
            recordIndex += 1;
        }

        return new CffFontDictionaryArray(index, records);
    }
}

internal class CffFontDictionarySelect
{
    int format;
    int encodedLength;
    int[] fontDictionaryIndices;

    internal CffFontDictionarySelect(
        int format,
        int encodedLength,
        int[] fontDictionaryIndices)
    {
        this.format = format;
        this.encodedLength = encodedLength;
        this.fontDictionaryIndices = fontDictionaryIndices;
    }

    internal int GetFormat()
    {
        return format;
    }

    internal int GetEncodedLength()
    {
        return encodedLength;
    }

    internal int GetGlyphCount()
    {
        return fontDictionaryIndices.Length;
    }

    internal int GetFontDictionaryIndex(int glyphIndex)
    {
        if (glyphIndex < 0 || glyphIndex >= fontDictionaryIndices.Length)
        {
            throw new System.ArgumentOutOfRangeException("glyphIndex");
        }

        return fontDictionaryIndices[glyphIndex];
    }
}

internal static class CffFontDictionarySelectParser
{
    internal static CffFontDictionarySelect Parse(
        FontDataSlice data,
        int offset,
        int glyphCount,
        int fontDictionaryCount)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        if (offset < 0 || offset >= data.GetLength())
        {
            throw FontFormatException.Create(
                "A CFF FDSelect offset is outside the bounded table.");
        }

        if (glyphCount <= 0 ||
            fontDictionaryCount <= 0 ||
            fontDictionaryCount > 256)
        {
            throw FontFormatException.Create(
                "A CFF FDSelect has invalid glyph or Font DICT counts.");
        }

        BigEndianReader reader = new BigEndianReader(data.GetSlice(
            offset,
            data.GetLength() - offset));
        int format = reader.ReadUInt8();
        int[] indices = new int[glyphCount];

        if (format == 0)
        {
            int glyphIndex = 0;

            while (glyphIndex < glyphCount)
            {
                indices[glyphIndex] = reader.ReadUInt8();
                ValidateFontDictionaryIndex(
                    indices[glyphIndex],
                    fontDictionaryCount);
                glyphIndex += 1;
            }
        }
        else if (format == 3)
        {
            ReadFormatThree(
                reader,
                indices,
                glyphCount,
                fontDictionaryCount);
        }
        else
        {
            throw FontFormatException.Create(
                "A CFF 1 FDSelect has an unsupported format.");
        }

        return new CffFontDictionarySelect(
            format,
            reader.GetPosition(),
            indices);
    }

    static void ReadFormatThree(
        BigEndianReader reader,
        int[] indices,
        int glyphCount,
        int fontDictionaryCount)
    {
        int rangeCount = reader.ReadUInt16();

        if (rangeCount <= 0 || rangeCount > glyphCount)
        {
            throw FontFormatException.Create(
                "A CFF FDSelect format 3 has an invalid range count.");
        }

        int[] firstGlyphs = new int[rangeCount];
        int[] rangeIndices = new int[rangeCount];
        int rangeIndex = 0;

        while (rangeIndex < rangeCount)
        {
            firstGlyphs[rangeIndex] = reader.ReadUInt16();
            rangeIndices[rangeIndex] = reader.ReadUInt8();
            ValidateFontDictionaryIndex(
                rangeIndices[rangeIndex],
                fontDictionaryCount);

            if ((rangeIndex == 0 && firstGlyphs[rangeIndex] != 0) ||
                (rangeIndex != 0 &&
                    firstGlyphs[rangeIndex] <= firstGlyphs[rangeIndex - 1]) ||
                firstGlyphs[rangeIndex] >= glyphCount)
            {
                throw FontFormatException.Create(
                    "CFF FDSelect format 3 ranges are not strictly ordered from glyph zero.");
            }

            rangeIndex += 1;
        }

        int sentinel = reader.ReadUInt16();

        if (sentinel != glyphCount)
        {
            throw FontFormatException.Create(
                "A CFF FDSelect format 3 sentinel must equal the glyph count.");
        }

        rangeIndex = 0;

        while (rangeIndex < rangeCount)
        {
            int end = rangeIndex + 1 < rangeCount ?
                firstGlyphs[rangeIndex + 1] : sentinel;
            int glyphIndex = firstGlyphs[rangeIndex];

            while (glyphIndex < end)
            {
                indices[glyphIndex] = rangeIndices[rangeIndex];
                glyphIndex += 1;
            }

            rangeIndex += 1;
        }
    }

    static void ValidateFontDictionaryIndex(
        int index,
        int fontDictionaryCount)
    {
        if (index < 0 || index >= fontDictionaryCount)
        {
            throw FontFormatException.Create(
                "A CFF FDSelect references a Font DICT outside FDArray.");
        }
    }
}

internal class CffCidMetadata
{
    CffTopDictionary topDictionary;
    CffCharset charset;
    CffFontDictionaryArray fontDictionaries;
    CffFontDictionarySelect fontDictionarySelect;

    internal CffCidMetadata(
        CffTopDictionary topDictionary,
        CffCharset charset,
        CffFontDictionaryArray? fontDictionaries,
        CffFontDictionarySelect? fontDictionarySelect)
    {
        if (topDictionary == null)
        {
            throw new System.ArgumentNullException("topDictionary");
        }

        if (charset == null)
        {
            throw new System.ArgumentNullException("charset");
        }

        if (fontDictionaries == null)
        {
            throw new System.ArgumentNullException("fontDictionaries");
        }

        if (fontDictionarySelect == null)
        {
            throw new System.ArgumentNullException("fontDictionarySelect");
        }

        this.topDictionary = topDictionary;
        this.charset = charset;
        this.fontDictionaries = fontDictionaries;
        this.fontDictionarySelect = fontDictionarySelect;
        Validate();
    }

    internal string GetRegistry()
    {
        return topDictionary.GetRegistry();
    }

    internal string GetOrdering()
    {
        return topDictionary.GetOrdering();
    }

    internal int GetSupplement()
    {
        return topDictionary.GetSupplement();
    }

    internal double GetFontVersion()
    {
        return topDictionary.GetCidFontVersion();
    }

    internal double GetFontRevision()
    {
        return topDictionary.GetCidFontRevision();
    }

    internal int GetFontType()
    {
        return topDictionary.GetCidFontType();
    }

    internal int GetCidCount()
    {
        return topDictionary.GetCidCount();
    }

    internal bool HasUniqueIdentifierBase()
    {
        return topDictionary.HasUniqueIdentifierBase();
    }

    internal int GetUniqueIdentifierBase()
    {
        return topDictionary.GetUniqueIdentifierBase();
    }

    internal CffCharset GetCharset()
    {
        return charset;
    }

    internal CffFontDictionaryArray GetFontDictionaries()
    {
        return fontDictionaries;
    }

    internal CffFontDictionarySelect GetFontDictionarySelect()
    {
        return fontDictionarySelect;
    }

    internal CffFontDictionaryRecord GetFontDictionaryForGlyph(
        int glyphIndex)
    {
        return fontDictionaries.GetRecord(
            fontDictionarySelect.GetFontDictionaryIndex(glyphIndex));
    }

    void Validate()
    {
        if (!topDictionary.IsCidKeyed() || !charset.IsCidKeyed())
        {
            throw FontFormatException.Create(
                "CFF CID metadata requires a CID-keyed Top DICT and charset.");
        }

        int cidCount = topDictionary.GetCidCount();

        if (cidCount <= 0 || cidCount > 65536)
        {
            throw FontFormatException.Create(
                "A CFF CIDCount is outside the supported CID range.");
        }

        int glyphIndex = 0;

        while (glyphIndex < charset.GetGlyphCount())
        {
            if (charset.GetIdentifier(glyphIndex) >= cidCount)
            {
                throw FontFormatException.Create(
                    "A CFF charset CID is not less than CIDCount.");
            }

            glyphIndex += 1;
        }
    }
}
