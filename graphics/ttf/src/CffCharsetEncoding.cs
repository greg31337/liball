namespace ttf;

internal static class CffPredefinedData
{
    static readonly int[] expertCharset = new int[]
    {
        0, 1, 229, 230, 231, 232, 233, 234,
        235, 236, 237, 238, 13, 14, 15, 99,
        239, 240, 241, 242, 243, 244, 245, 246,
        247, 248, 27, 28, 249, 250, 251, 252,
        253, 254, 255, 256, 257, 258, 259, 260,
        261, 262, 263, 264, 265, 266, 109, 110,
        267, 268, 269, 270, 271, 272, 273, 274,
        275, 276, 277, 278, 279, 280, 281, 282,
        283, 284, 285, 286, 287, 288, 289, 290,
        291, 292, 293, 294, 295, 296, 297, 298,
        299, 300, 301, 302, 303, 304, 305, 306,
        307, 308, 309, 310, 311, 312, 313, 314,
        315, 316, 317, 318, 158, 155, 163, 319,
        320, 321, 322, 323, 324, 325, 326, 150,
        164, 169, 327, 328, 329, 330, 331, 332,
        333, 334, 335, 336, 337, 338, 339, 340,
        341, 342, 343, 344, 345, 346, 347, 348,
        349, 350, 351, 352, 353, 354, 355, 356,
        357, 358, 359, 360, 361, 362, 363, 364,
        365, 366, 367, 368, 369, 370, 371, 372,
        373, 374, 375, 376, 377, 378
    };
    static readonly int[] expertSubsetCharset = new int[]
    {
        0, 1, 231, 232, 235, 236, 237, 238,
        13, 14, 15, 99, 239, 240, 241, 242,
        243, 244, 245, 246, 247, 248, 27, 28,
        249, 250, 251, 253, 254, 255, 256, 257,
        258, 259, 260, 261, 262, 263, 264, 265,
        266, 109, 110, 267, 268, 269, 270, 272,
        300, 301, 302, 305, 314, 315, 158, 155,
        163, 320, 321, 322, 323, 324, 325, 326,
        150, 164, 169, 327, 328, 329, 330, 331,
        332, 333, 334, 335, 336, 337, 338, 339,
        340, 341, 342, 343, 344, 345, 346
    };
    static readonly int[] standardEncoding = new int[]
    {
        0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0,
        1, 2, 3, 4, 5, 6, 7, 8,
        9, 10, 11, 12, 13, 14, 15, 16,
        17, 18, 19, 20, 21, 22, 23, 24,
        25, 26, 27, 28, 29, 30, 31, 32,
        33, 34, 35, 36, 37, 38, 39, 40,
        41, 42, 43, 44, 45, 46, 47, 48,
        49, 50, 51, 52, 53, 54, 55, 56,
        57, 58, 59, 60, 61, 62, 63, 64,
        65, 66, 67, 68, 69, 70, 71, 72,
        73, 74, 75, 76, 77, 78, 79, 80,
        81, 82, 83, 84, 85, 86, 87, 88,
        89, 90, 91, 92, 93, 94, 95, 0,
        0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0,
        0, 96, 97, 98, 99, 100, 101, 102,
        103, 104, 105, 106, 107, 108, 109, 110,
        0, 111, 112, 113, 114, 0, 115, 116,
        117, 118, 119, 120, 121, 122, 0, 123,
        0, 124, 125, 126, 127, 128, 129, 130,
        131, 0, 132, 133, 0, 134, 135, 136,
        137, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0,
        0, 138, 0, 139, 0, 0, 0, 0,
        140, 141, 142, 143, 0, 0, 0, 0,
        0, 144, 0, 0, 0, 145, 0, 0,
        146, 147, 148, 149, 0, 0, 0, 0
    };
    static readonly int[] expertEncoding = new int[]
    {
        0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0,
        1, 229, 230, 0, 231, 232, 233, 234,
        235, 236, 237, 238, 13, 14, 15, 99,
        239, 240, 241, 242, 243, 244, 245, 246,
        247, 248, 27, 28, 249, 250, 251, 252,
        0, 253, 254, 255, 256, 257, 0, 0,
        0, 258, 0, 0, 259, 260, 261, 262,
        0, 0, 263, 264, 265, 0, 266, 109,
        110, 267, 268, 269, 0, 270, 271, 272,
        273, 274, 275, 276, 277, 278, 279, 280,
        281, 282, 283, 284, 285, 286, 287, 288,
        289, 290, 291, 292, 293, 294, 295, 296,
        297, 298, 299, 300, 301, 302, 303, 0,
        0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0,
        0, 304, 305, 306, 0, 0, 307, 308,
        309, 310, 311, 0, 312, 0, 0, 312,
        0, 0, 314, 315, 0, 0, 316, 317,
        318, 0, 0, 0, 158, 155, 163, 319,
        320, 321, 322, 323, 324, 325, 0, 0,
        326, 150, 164, 169, 327, 328, 329, 330,
        331, 332, 333, 334, 335, 336, 337, 338,
        339, 340, 341, 342, 343, 344, 345, 346,
        347, 348, 349, 350, 351, 352, 353, 354,
        355, 356, 357, 358, 359, 360, 361, 362,
        363, 364, 365, 366, 367, 368, 369, 370,
        371, 372, 373, 374, 375, 376, 377, 378
    };

    internal static int GetCharsetCount(int predefinedValue)
    {
        if (predefinedValue == 0)
        {
            return 229;
        }

        if (predefinedValue == 1)
        {
            return expertCharset.Length;
        }

        if (predefinedValue == 2)
        {
            return expertSubsetCharset.Length;
        }

        throw new System.ArgumentOutOfRangeException("predefinedValue");
    }

    internal static int GetCharsetIdentifier(
        int predefinedValue,
        int glyphIndex)
    {
        int count = GetCharsetCount(predefinedValue);

        if (glyphIndex < 0 || glyphIndex >= count)
        {
            throw new System.ArgumentOutOfRangeException("glyphIndex");
        }

        if (predefinedValue == 0)
        {
            return glyphIndex;
        }

        if (predefinedValue == 1)
        {
            return expertCharset[glyphIndex];
        }

        return expertSubsetCharset[glyphIndex];
    }

    internal static int GetEncodingSid(
        int predefinedValue,
        int code)
    {
        if (predefinedValue < 0 || predefinedValue > 1)
        {
            throw new System.ArgumentOutOfRangeException("predefinedValue");
        }

        if (code < 0 || code > 255)
        {
            throw new System.ArgumentOutOfRangeException("code");
        }

        return predefinedValue == 0 ?
            standardEncoding[code] : expertEncoding[code];
    }
}

internal class CffCharset
{
    int predefinedValue;
    int format;
    bool cidKeyed;
    int[] identifiers;
    CffStringTable strings;
    int encodedLength;

    internal CffCharset(
        int predefinedValue,
        int format,
        bool cidKeyed,
        int[] identifiers,
        CffStringTable strings,
        int encodedLength)
    {
        this.predefinedValue = predefinedValue;
        this.format = format;
        this.cidKeyed = cidKeyed;
        this.identifiers = identifiers;
        this.strings = strings;
        this.encodedLength = encodedLength;
    }

    internal int GetGlyphCount()
    {
        return identifiers.Length;
    }

    internal bool IsCidKeyed()
    {
        return cidKeyed;
    }

    internal bool IsPredefined()
    {
        return predefinedValue >= 0;
    }

    internal int GetPredefinedValue()
    {
        return predefinedValue;
    }

    internal int GetFormat()
    {
        return format;
    }

    internal int GetEncodedLength()
    {
        return encodedLength;
    }

    internal int GetIdentifier(int glyphIndex)
    {
        if (glyphIndex < 0 || glyphIndex >= identifiers.Length)
        {
            throw new System.ArgumentOutOfRangeException("glyphIndex");
        }

        return identifiers[glyphIndex];
    }

    internal string GetName(int glyphIndex)
    {
        if (cidKeyed)
        {
            throw FontFormatException.Create(
                "A CID-keyed CFF charset identifies glyphs by CID, not SID.");
        }

        return strings.GetString(GetIdentifier(glyphIndex));
    }

    internal int FindGlyphIndex(int identifier)
    {
        int glyphIndex = 0;

        while (glyphIndex < identifiers.Length)
        {
            if (identifiers[glyphIndex] == identifier)
            {
                return glyphIndex;
            }

            glyphIndex += 1;
        }

        return -1;
    }
}

internal static class CffCharsetParser
{
    internal static CffCharset Parse(
        FontDataSlice data,
        int value,
        int glyphCount,
        bool cidKeyed,
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

        if (glyphCount <= 0)
        {
            throw FontFormatException.Create(
                "A CFF charset requires at least the .notdef glyph.");
        }

        if (value >= 0 && value <= 2)
        {
            if (cidKeyed)
            {
                throw FontFormatException.Create(
                    "A CID-keyed CFF font requires a custom CID charset.");
            }

            return CreatePredefined(value, glyphCount, strings);
        }

        if (value < 3 || value >= data.GetLength())
        {
            throw FontFormatException.Create(
                "A CFF charset offset is outside the bounded table.");
        }

        BigEndianReader reader = new BigEndianReader(data.GetSlice(
            value,
            data.GetLength() - value));
        int format = reader.ReadUInt8();
        int[] identifiers = new int[glyphCount];
        System.Collections.Generic.HashSet<int> seen =
            new System.Collections.Generic.HashSet<int>();
        identifiers[0] = 0;
        seen.Add(0);
        int glyphIndex = 1;

        if (format == 0)
        {
            while (glyphIndex < glyphCount)
            {
                AddIdentifier(
                    identifiers,
                    glyphIndex,
                    reader.ReadUInt16(),
                    cidKeyed,
                    strings,
                    seen);
                glyphIndex += 1;
            }
        }
        else if (format == 1 || format == 2)
        {
            while (glyphIndex < glyphCount)
            {
                int first = reader.ReadUInt16();
                int left = format == 1 ?
                    reader.ReadUInt8() : reader.ReadUInt16();
                int rangeCount = CheckedFontMath.AddCounts(left, 1);

                if (rangeCount > glyphCount - glyphIndex ||
                    left > 65535 - first)
                {
                    throw FontFormatException.Create(
                        "A CFF charset range exceeds its glyph or identifier bounds.");
                }

                int rangeIndex = 0;

                while (rangeIndex < rangeCount)
                {
                    AddIdentifier(
                        identifiers,
                        glyphIndex,
                        first + rangeIndex,
                        cidKeyed,
                        strings,
                        seen);
                    glyphIndex += 1;
                    rangeIndex += 1;
                }
            }
        }
        else
        {
            throw FontFormatException.Create(
                "A CFF charset has an unsupported format.");
        }

        return new CffCharset(
            -1,
            format,
            cidKeyed,
            identifiers,
            strings,
            reader.GetPosition());
    }

    static CffCharset CreatePredefined(
        int value,
        int glyphCount,
        CffStringTable strings)
    {
        if (glyphCount > CffPredefinedData.GetCharsetCount(value))
        {
            throw FontFormatException.Create(
                "A predefined CFF charset does not contain enough glyph identifiers.");
        }

        int[] identifiers = new int[glyphCount];
        int glyphIndex = 0;

        while (glyphIndex < glyphCount)
        {
            identifiers[glyphIndex] = CffPredefinedData.GetCharsetIdentifier(
                value,
                glyphIndex);
            strings.GetString(identifiers[glyphIndex]);
            glyphIndex += 1;
        }

        return new CffCharset(
            value,
            -1,
            false,
            identifiers,
            strings,
            0);
    }

    static void AddIdentifier(
        int[] identifiers,
        int glyphIndex,
        int identifier,
        bool cidKeyed,
        CffStringTable strings,
        System.Collections.Generic.HashSet<int> seen)
    {
        if (!seen.Add(identifier))
        {
            throw FontFormatException.Create(
                "A CFF charset contains a duplicate SID or CID.");
        }

        if (!cidKeyed)
        {
            strings.GetString(identifier);
        }

        identifiers[glyphIndex] = identifier;
    }
}

internal class CffEncoding
{
    int predefinedValue;
    int format;
    int encodedLength;
    int supplementCount;
    int[] glyphIndices;
    int[] identifiers;
    bool[] mapped;
    bool[] supplemental;

    internal CffEncoding(
        int predefinedValue,
        int format,
        int encodedLength,
        int supplementCount,
        int[] glyphIndices,
        int[] identifiers,
        bool[] mapped,
        bool[] supplemental)
    {
        this.predefinedValue = predefinedValue;
        this.format = format;
        this.encodedLength = encodedLength;
        this.supplementCount = supplementCount;
        this.glyphIndices = glyphIndices;
        this.identifiers = identifiers;
        this.mapped = mapped;
        this.supplemental = supplemental;
    }

    internal bool IsPredefined()
    {
        return predefinedValue >= 0;
    }

    internal int GetPredefinedValue()
    {
        return predefinedValue;
    }

    internal int GetFormat()
    {
        return format;
    }

    internal int GetEncodedLength()
    {
        return encodedLength;
    }

    internal int GetSupplementCount()
    {
        return supplementCount;
    }

    internal bool HasCode(int code)
    {
        ValidateCode(code);
        return mapped[code];
    }

    internal bool IsSupplementalCode(int code)
    {
        ValidateCode(code);
        return supplemental[code];
    }

    internal int GetGlyphIndex(int code)
    {
        ValidateCode(code);
        return mapped[code] ? glyphIndices[code] : 0;
    }

    internal int GetIdentifier(int code)
    {
        ValidateCode(code);
        return mapped[code] ? identifiers[code] : 0;
    }

    void ValidateCode(int code)
    {
        if (code < 0 || code > 255)
        {
            throw new System.ArgumentOutOfRangeException("code");
        }
    }
}

internal static class CffEncodingParser
{
    internal static CffEncoding Parse(
        FontDataSlice data,
        int value,
        CffCharset charset)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        if (charset == null)
        {
            throw new System.ArgumentNullException("charset");
        }

        if (charset.IsCidKeyed())
        {
            throw FontFormatException.Create(
                "A CID-keyed CFF font must not define an Encoding.");
        }

        int[] glyphIndices = new int[256];
        int[] identifiers = new int[256];
        bool[] mapped = new bool[256];
        bool[] supplemental = new bool[256];

        if (value == 0 || value == 1)
        {
            int code = 0;

            while (code < 256)
            {
                int sid = CffPredefinedData.GetEncodingSid(value, code);
                int glyphIndex = charset.FindGlyphIndex(sid);

                if (sid != 0 && glyphIndex > 0)
                {
                    glyphIndices[code] = glyphIndex;
                    identifiers[code] = sid;
                    mapped[code] = true;
                }

                code += 1;
            }

            return new CffEncoding(
                value,
                -1,
                0,
                0,
                glyphIndices,
                identifiers,
                mapped,
                supplemental);
        }

        if (value < 2 || value >= data.GetLength())
        {
            throw FontFormatException.Create(
                "A CFF Encoding offset is outside the bounded table.");
        }

        BigEndianReader reader = new BigEndianReader(data.GetSlice(
            value,
            data.GetLength() - value));
        int encodedFormat = reader.ReadUInt8();
        bool hasSupplements = (encodedFormat & 0x80) != 0;
        int format = encodedFormat & 0x7F;
        int nextGlyphIndex = 1;

        if (format == 0)
        {
            int codeCount = reader.ReadUInt8();

            if (codeCount > charset.GetGlyphCount() - 1)
            {
                throw FontFormatException.Create(
                    "A CFF Encoding format 0 maps more codes than glyphs.");
            }

            int index = 0;

            while (index < codeCount)
            {
                int code = reader.ReadUInt8();
                AddMapping(
                    code,
                    nextGlyphIndex,
                    charset.GetIdentifier(nextGlyphIndex),
                    false,
                    glyphIndices,
                    identifiers,
                    mapped,
                    supplemental);
                nextGlyphIndex += 1;
                index += 1;
            }
        }
        else if (format == 1)
        {
            int rangeCount = reader.ReadUInt8();
            int rangeIndex = 0;

            while (rangeIndex < rangeCount)
            {
                int firstCode = reader.ReadUInt8();
                int left = reader.ReadUInt8();
                int count = left + 1;

                if (left > 255 - firstCode ||
                    count > charset.GetGlyphCount() - nextGlyphIndex)
                {
                    throw FontFormatException.Create(
                        "A CFF Encoding range exceeds its code or glyph bounds.");
                }

                int codeIndex = 0;

                while (codeIndex < count)
                {
                    AddMapping(
                        firstCode + codeIndex,
                        nextGlyphIndex,
                        charset.GetIdentifier(nextGlyphIndex),
                        false,
                        glyphIndices,
                        identifiers,
                        mapped,
                        supplemental);
                    nextGlyphIndex += 1;
                    codeIndex += 1;
                }

                rangeIndex += 1;
            }
        }
        else
        {
            throw FontFormatException.Create(
                "A CFF Encoding has an unsupported format.");
        }

        int supplementCount = 0;

        if (hasSupplements)
        {
            supplementCount = reader.ReadUInt8();
            int supplementIndex = 0;

            while (supplementIndex < supplementCount)
            {
                int code = reader.ReadUInt8();
                int sid = reader.ReadUInt16();
                int glyphIndex = charset.FindGlyphIndex(sid);

                if (glyphIndex <= 0)
                {
                    throw FontFormatException.Create(
                        "A CFF Encoding supplement references a SID outside its charset.");
                }

                AddMapping(
                    code,
                    glyphIndex,
                    sid,
                    true,
                    glyphIndices,
                    identifiers,
                    mapped,
                    supplemental);
                supplementIndex += 1;
            }
        }

        return new CffEncoding(
            -1,
            format,
            reader.GetPosition(),
            supplementCount,
            glyphIndices,
            identifiers,
            mapped,
            supplemental);
    }

    static void AddMapping(
        int code,
        int glyphIndex,
        int identifier,
        bool isSupplemental,
        int[] glyphIndices,
        int[] identifiers,
        bool[] mapped,
        bool[] supplemental)
    {
        if (mapped[code])
        {
            throw FontFormatException.Create(
                "A CFF Encoding maps the same code more than once.");
        }

        glyphIndices[code] = glyphIndex;
        identifiers[code] = identifier;
        mapped[code] = true;
        supplemental[code] = isSupplemental;
    }
}
