namespace ttf;

internal enum SfntSbixGraphicKind
{
    Unsupported = 0,
    Jpeg = 1,
    Png = 2,
    Tiff = 3,
    Duplicate = 4
}

internal class SfntSbixGlyphData
{
    int glyphId;
    int originOffsetX;
    int originOffsetY;
    int graphicTypeTag;
    SfntSbixGraphicKind graphicKind;
    byte[] data;
    int duplicateGlyphId;

    internal SfntSbixGlyphData(
        int glyphId,
        int originOffsetX,
        int originOffsetY,
        int graphicTypeTag,
        SfntSbixGraphicKind graphicKind,
        byte[] data,
        int duplicateGlyphId)
    {
        this.glyphId = glyphId;
        this.originOffsetX = originOffsetX;
        this.originOffsetY = originOffsetY;
        this.graphicTypeTag = graphicTypeTag;
        this.graphicKind = graphicKind;
        this.data = data;
        this.duplicateGlyphId = duplicateGlyphId;
    }

    internal int GetGlyphId()
    {
        return glyphId;
    }

    internal int GetOriginOffsetX()
    {
        return originOffsetX;
    }

    internal int GetOriginOffsetY()
    {
        return originOffsetY;
    }

    internal int GetGraphicTypeTag()
    {
        return graphicTypeTag;
    }

    internal SfntSbixGraphicKind GetGraphicKind()
    {
        return graphicKind;
    }

    internal bool IsDuplicate()
    {
        return graphicKind == SfntSbixGraphicKind.Duplicate;
    }

    internal int GetDuplicateGlyphId()
    {
        if (!IsDuplicate())
        {
            throw new System.InvalidOperationException(
                "This sbix glyph is not a duplicate reference.");
        }

        return duplicateGlyphId;
    }

    internal int GetDataLength()
    {
        return data.Length;
    }

    internal byte[] CopyData()
    {
        return data.AsSpan().ToArray();
    }
}

internal class SfntSbixGlyphImage
{
    int glyphId;
    int sourceGlyphId;
    int originOffsetX;
    int originOffsetY;
    int graphicTypeTag;
    SfntSbixGraphicKind graphicKind;
    byte[] data;

    internal SfntSbixGlyphImage(
        int glyphId,
        int sourceGlyphId,
        int originOffsetX,
        int originOffsetY,
        int graphicTypeTag,
        SfntSbixGraphicKind graphicKind,
        byte[] data)
    {
        this.glyphId = glyphId;
        this.sourceGlyphId = sourceGlyphId;
        this.originOffsetX = originOffsetX;
        this.originOffsetY = originOffsetY;
        this.graphicTypeTag = graphicTypeTag;
        this.graphicKind = graphicKind;
        this.data = data;
    }

    internal int GetGlyphId()
    {
        return glyphId;
    }

    internal int GetSourceGlyphId()
    {
        return sourceGlyphId;
    }

    internal int GetOriginOffsetX()
    {
        return originOffsetX;
    }

    internal int GetOriginOffsetY()
    {
        return originOffsetY;
    }

    internal int GetGraphicTypeTag()
    {
        return graphicTypeTag;
    }

    internal SfntSbixGraphicKind GetGraphicKind()
    {
        return graphicKind;
    }

    internal int GetDataLength()
    {
        return data.Length;
    }

    internal byte[] CopyData()
    {
        return data.AsSpan().ToArray();
    }
}

internal class SfntSbixStrike
{
    int pixelsPerEm;
    int pixelsPerInch;
    int maximumDuplicateDepth;
    SfntSbixGlyphData[] glyphs;

    internal SfntSbixStrike(
        int pixelsPerEm,
        int pixelsPerInch,
        int maximumDuplicateDepth,
        SfntSbixGlyphData[] glyphs)
    {
        this.pixelsPerEm = pixelsPerEm;
        this.pixelsPerInch = pixelsPerInch;
        this.maximumDuplicateDepth = maximumDuplicateDepth;
        this.glyphs = glyphs;
        ValidateDuplicates();
    }

    internal int GetPixelsPerEm()
    {
        return pixelsPerEm;
    }

    internal int GetPixelsPerInch()
    {
        return pixelsPerInch;
    }

    internal int GetGlyphCount()
    {
        return glyphs.Length;
    }

    internal bool HasGlyphData(int glyphId)
    {
        ValidateGlyphId(glyphId);
        return glyphs[glyphId] != null;
    }

    internal SfntSbixGlyphData GetGlyphData(int glyphId)
    {
        ValidateGlyphId(glyphId);
        return glyphs[glyphId];
    }

    internal SfntSbixGlyphImage? ResolveGlyphImage(int glyphId)
    {
        ValidateGlyphId(glyphId);
        SfntSbixGlyphData requested = glyphs[glyphId];

        if (requested == null)
        {
            return null;
        }

        SfntSbixGlyphData source = requested;
        int depth = 0;

        while (source.IsDuplicate())
        {
            depth += 1;

            if (depth > maximumDuplicateDepth)
            {
                throw FontLimitException.Create(
                    "An sbix duplicate chain exceeds the configured component-depth limit.");
            }

            source = glyphs[source.GetDuplicateGlyphId()];
        }

        return new SfntSbixGlyphImage(
            glyphId,
            source.GetGlyphId(),
            requested.GetOriginOffsetX(),
            requested.GetOriginOffsetY(),
            source.GetGraphicTypeTag(),
            source.GetGraphicKind(),
            source.CopyData());
    }

    void ValidateDuplicates()
    {
        int[] visits = new int[glyphs.Length];
        int generation = 1;
        int glyphId = 0;

        while (glyphId < glyphs.Length)
        {
            SfntSbixGlyphData current = glyphs[glyphId];
            int depth = 0;

            while (current != null && current.IsDuplicate())
            {
                int currentGlyphId = current.GetGlyphId();

                if (visits[currentGlyphId] == generation)
                {
                    throw FontFormatException.Create(
                        "The sbix table contains a duplicate-glyph cycle.");
                }

                visits[currentGlyphId] = generation;
                depth += 1;

                if (depth > maximumDuplicateDepth)
                {
                    throw FontLimitException.Create(
                        "An sbix duplicate chain exceeds the configured component-depth limit.");
                }

                current = glyphs[current.GetDuplicateGlyphId()];

                if (current == null)
                {
                    throw FontFormatException.Create(
                        "An sbix duplicate references a glyph without bitmap data.");
                }
            }

            generation += 1;
            glyphId += 1;
        }
    }

    void ValidateGlyphId(int glyphId)
    {
        if (glyphId < 0 || glyphId >= glyphs.Length)
        {
            throw new System.ArgumentOutOfRangeException("glyphId");
        }
    }
}

internal class SfntSbixTable
{
    int flags;
    SfntSbixStrike[] strikes;

    internal SfntSbixTable(int flags, SfntSbixStrike[] strikes)
    {
        this.flags = flags;
        this.strikes = strikes;
    }

    internal int GetVersion()
    {
        return 1;
    }

    internal int GetFlags()
    {
        return flags;
    }

    internal bool DrawOutlines()
    {
        return (flags & 0x0002) != 0;
    }

    internal int GetStrikeCount()
    {
        return strikes.Length;
    }

    internal SfntSbixStrike GetStrike(int index)
    {
        if (index < 0 || index >= strikes.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return strikes[index];
    }
}

internal static class SfntSbixTableParser
{
    const int HeaderLength = 8;
    const int StrikeHeaderLength = 4;
    const int GlyphHeaderLength = 8;
    const int JpegTag = 0x6A706720;
    const int PngTag = 0x706E6720;
    const int TiffTag = 0x74696666;
    const int DuplicateTag = 0x64757065;
    static readonly SfntTableParser parser = new SfntTableParser(
        FontTags.Sbix,
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

    internal static SfntSbixTable? FindTable(SfntTableCache cache)
    {
        SfntParsedTableView? view = FindView(cache);

        if (view == null || !view.HasParsedData())
        {
            return null;
        }

        return (SfntSbixTable)view.GetParsedData();
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

        if (tableData.GetLength() < HeaderLength)
        {
            throw FontFormatException.Create("The sbix table header is truncated.");
        }

        BigEndianReader reader = new BigEndianReader(tableData);
        int encodedVersion = reader.ReadUInt16();
        int flags = reader.ReadUInt16();
        uint strikeCountValue = reader.ReadUInt32();

        if (encodedVersion != 1)
        {
            throw FontFormatException.Create("The sbix table version is not supported.");
        }

        if ((flags & 0x0001) == 0 || (flags & 0xFFFC) != 0)
        {
            throw FontFormatException.Create("The sbix table flags are invalid.");
        }

        if (strikeCountValue > int.MaxValue ||
            strikeCountValue > limits.GetMaxCoverageEntries())
        {
            throw FontLimitException.Create(
                "The sbix strike count exceeds the configured coverage-entry limit.");
        }

        int strikeCount = (int)strikeCountValue;
        int headerLength = CheckedFontMath.AddOffset(
            HeaderLength,
            CheckedFontMath.MultiplyCount(strikeCount, 4));

        if (headerLength > tableData.GetLength())
        {
            throw FontFormatException.Create("The sbix strike-offset array is truncated.");
        }

        SfntMaxpTable? maxp = SfntMaxpTableParser.FindTable(cache);

        if (maxp == null)
        {
            throw FontFormatException.Create(
                "The sbix table requires a parsed maxp table.");
        }

        int glyphCount = maxp.GetGlyphCount();
        long entryCount = strikeCount +
            (long)strikeCount * (glyphCount + 1);

        if (entryCount > limits.GetMaxCoverageEntries())
        {
            throw FontLimitException.Create(
                "The sbix strike glyph-offset arrays exceed the configured coverage-entry limit.");
        }

        int[] strikeOffsets = new int[strikeCount];
        int index = 0;

        while (index < strikeOffsets.Length)
        {
            uint offsetValue = reader.ReadUInt32();

            if (offsetValue > int.MaxValue)
            {
                throw FontFormatException.Create("An sbix strike offset is invalid.");
            }

            int offset = (int)offsetValue;

            if (offset < headerLength || offset >= tableData.GetLength() ||
                (index > 0 && offset <= strikeOffsets[index - 1]))
            {
                throw FontFormatException.Create(
                    "The sbix strike offsets are invalid or unsorted.");
            }

            strikeOffsets[index] = offset;
            index += 1;
        }

        SfntSbixStrike[] strikes = new SfntSbixStrike[strikeCount];
        index = 0;

        while (index < strikes.Length)
        {
            int strikeOffset = strikeOffsets[index];
            int strikeEnd = index + 1 < strikes.Length ?
                strikeOffsets[index + 1] : tableData.GetLength();
            strikes[index] = ParseStrike(
                tableData.GetSlice(strikeOffset, strikeEnd - strikeOffset),
                glyphCount,
                limits.GetMaxComponentDepth());
            index += 1;
        }

        return new SfntSbixTable(flags, strikes);
    }

    static SfntSbixStrike ParseStrike(
        FontDataSlice strikeData,
        int glyphCount,
        int maximumDuplicateDepth)
    {
        int glyphOffsetsLength = CheckedFontMath.MultiplyCount(
            glyphCount + 1,
            4);
        int strikeHeaderLength = CheckedFontMath.AddOffset(
            StrikeHeaderLength,
            glyphOffsetsLength);

        if (strikeHeaderLength > strikeData.GetLength())
        {
            throw FontFormatException.Create("An sbix strike header is truncated.");
        }

        BigEndianReader reader = new BigEndianReader(strikeData);
        int pixelsPerEm = reader.ReadUInt16();
        int pixelsPerInch = reader.ReadUInt16();
        int[] glyphOffsets = new int[glyphCount + 1];
        int index = 0;

        while (index < glyphOffsets.Length)
        {
            uint offsetValue = reader.ReadUInt32();

            if (offsetValue > int.MaxValue)
            {
                throw FontFormatException.Create(
                    "An sbix glyph-data offset is invalid.");
            }

            int offset = (int)offsetValue;

            if (offset < strikeHeaderLength ||
                offset > strikeData.GetLength() ||
                (index > 0 && offset < glyphOffsets[index - 1]))
            {
                throw FontFormatException.Create(
                    "The sbix glyph-data offsets are invalid or unsorted.");
            }

            glyphOffsets[index] = offset;
            index += 1;
        }

        SfntSbixGlyphData[] glyphs = new SfntSbixGlyphData[glyphCount];
        index = 0;

        while (index < glyphs.Length)
        {
            int glyphLength = glyphOffsets[index + 1] - glyphOffsets[index];

            if (glyphLength > 0)
            {
                glyphs[index] = ParseGlyph(
                    strikeData.GetSlice(glyphOffsets[index], glyphLength),
                    index,
                    glyphCount);
            }

            index += 1;
        }

        return new SfntSbixStrike(
            pixelsPerEm,
            pixelsPerInch,
            maximumDuplicateDepth,
            glyphs);
    }

    static SfntSbixGlyphData ParseGlyph(
        FontDataSlice glyphData,
        int glyphId,
        int glyphCount)
    {
        if (glyphData.GetLength() < GlyphHeaderLength)
        {
            throw FontFormatException.Create("An sbix glyph-data record is truncated.");
        }

        BigEndianReader reader = new BigEndianReader(glyphData);
        int originOffsetX = reader.ReadInt16();
        int originOffsetY = reader.ReadInt16();
        int graphicTypeTag = reader.ReadTag();
        int dataLength = glyphData.GetLength() - GlyphHeaderLength;
        byte[] data = glyphData.GetSlice(
            GlyphHeaderLength,
            dataLength).CopyBytes();
        SfntSbixGraphicKind graphicKind = GetGraphicKind(graphicTypeTag);
        int duplicateGlyphId = -1;

        if (graphicKind == SfntSbixGraphicKind.Duplicate)
        {
            if (dataLength != 2)
            {
                throw FontFormatException.Create(
                    "An sbix duplicate record must contain one glyph ID.");
            }

            duplicateGlyphId = (data[0] << 8) | data[1];

            if (duplicateGlyphId >= glyphCount)
            {
                throw FontFormatException.Create(
                    "An sbix duplicate references a missing glyph.");
            }
        }
        else if (dataLength == 0)
        {
            throw FontFormatException.Create(
                "An sbix image record has no graphic payload.");
        }

        return new SfntSbixGlyphData(
            glyphId,
            originOffsetX,
            originOffsetY,
            graphicTypeTag,
            graphicKind,
            data,
            duplicateGlyphId);
    }

    static SfntSbixGraphicKind GetGraphicKind(int tag)
    {
        if (tag == JpegTag)
        {
            return SfntSbixGraphicKind.Jpeg;
        }

        if (tag == PngTag)
        {
            return SfntSbixGraphicKind.Png;
        }

        if (tag == TiffTag)
        {
            return SfntSbixGraphicKind.Tiff;
        }

        if (tag == DuplicateTag)
        {
            return SfntSbixGraphicKind.Duplicate;
        }

        return SfntSbixGraphicKind.Unsupported;
    }
}
