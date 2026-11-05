namespace ttf;

internal class SfntGlyfRecord
{
    int glyphId;
    int offset;
    FontDataSlice data;

    internal SfntGlyfRecord(int glyphId, int offset, FontDataSlice data)
    {
        if (glyphId < 0)
        {
            throw new System.ArgumentOutOfRangeException("glyphId");
        }

        if (offset < 0)
        {
            throw new System.ArgumentOutOfRangeException("offset");
        }

        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        this.glyphId = glyphId;
        this.offset = offset;
        this.data = data;
    }

    internal int GetGlyphId()
    {
        return glyphId;
    }

    internal int GetOffset()
    {
        return offset;
    }

    internal int GetLength()
    {
        return data.GetLength();
    }

    internal bool IsEmpty()
    {
        return data.GetLength() == 0;
    }

    internal byte[] CopyData()
    {
        return data.CopyBytes();
    }

    internal FontDataSlice GetDataSlice()
    {
        return data;
    }
}

internal class SfntLocaTable
{
    SfntLocaFormat format;
    int[] offsets;
    SfntGlyfRecord[] glyphRecords;
    int glyfLength;
    int emptyGlyphCount;
    SfntMaxpTable maxp;
    FontLimits limits;
    SfntSimpleGlyph?[] simpleGlyphs;
    bool[] simpleGlyphsParsed;
    SfntCompositeGlyph?[] compositeGlyphs;
    bool[] compositeGlyphsParsed;
    SfntResolvedGlyph[] resolvedGlyphs;
    bool[] resolvedGlyphsParsed;
    object glyphParseSync;

    internal SfntLocaTable(
        SfntLocaFormat format,
        int[] offsets,
        SfntGlyfRecord[] glyphRecords,
        int glyfLength,
        int emptyGlyphCount,
        SfntMaxpTable maxp,
        FontLimits limits)
    {
        this.format = format;
        this.offsets = offsets;
        this.glyphRecords = glyphRecords;
        this.glyfLength = glyfLength;
        this.emptyGlyphCount = emptyGlyphCount;
        this.maxp = maxp;
        this.limits = limits;
        simpleGlyphs = new SfntSimpleGlyph[glyphRecords.Length];
        simpleGlyphsParsed = new bool[glyphRecords.Length];
        compositeGlyphs = new SfntCompositeGlyph[glyphRecords.Length];
        compositeGlyphsParsed = new bool[glyphRecords.Length];
        resolvedGlyphs = new SfntResolvedGlyph[glyphRecords.Length];
        resolvedGlyphsParsed = new bool[glyphRecords.Length];
        glyphParseSync = new object();
    }

    internal SfntLocaFormat GetFormat()
    {
        return format;
    }

    internal int GetGlyphCount()
    {
        return glyphRecords.Length;
    }

    internal int GetOffsetCount()
    {
        return offsets.Length;
    }

    internal int GetOffset(int index)
    {
        if (index < 0 || index >= offsets.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return offsets[index];
    }

    internal SfntGlyfRecord GetGlyphRecord(int glyphId)
    {
        if (glyphId < 0 || glyphId >= glyphRecords.Length)
        {
            throw new System.ArgumentOutOfRangeException("glyphId");
        }

        return glyphRecords[glyphId];
    }

    internal int GetGlyfLength()
    {
        return glyfLength;
    }

    internal int GetEmptyGlyphCount()
    {
        return emptyGlyphCount;
    }

    internal SfntSimpleGlyph? GetSimpleGlyph(int glyphId)
    {
        if (glyphId < 0 || glyphId >= glyphRecords.Length)
        {
            throw new System.ArgumentOutOfRangeException("glyphId");
        }

        lock (glyphParseSync)
        {
            if (!simpleGlyphsParsed[glyphId])
            {
                simpleGlyphs[glyphId] = SfntSimpleGlyphParser.Parse(
                    glyphRecords[glyphId],
                    maxp,
                    limits);
                simpleGlyphsParsed[glyphId] = true;
            }

            return simpleGlyphs[glyphId];
        }
    }

    internal SfntCompositeGlyph? GetCompositeGlyph(int glyphId)
    {
        if (glyphId < 0 || glyphId >= glyphRecords.Length)
        {
            throw new System.ArgumentOutOfRangeException("glyphId");
        }

        lock (glyphParseSync)
        {
            if (!compositeGlyphsParsed[glyphId])
            {
                compositeGlyphs[glyphId] = SfntCompositeGlyphParser.Parse(
                    glyphRecords[glyphId],
                    maxp,
                    limits);
                compositeGlyphsParsed[glyphId] = true;
            }

            return compositeGlyphs[glyphId];
        }
    }

    internal SfntResolvedGlyph GetResolvedGlyph(int glyphId)
    {
        if (glyphId < 0 || glyphId >= glyphRecords.Length)
        {
            throw new System.ArgumentOutOfRangeException("glyphId");
        }

        lock (glyphParseSync)
        {
            if (!resolvedGlyphsParsed[glyphId])
            {
                resolvedGlyphs[glyphId] = SfntCompositeResolver.Resolve(
                    this,
                    glyphId,
                    maxp,
                    limits);
                resolvedGlyphsParsed[glyphId] = true;
            }

            return resolvedGlyphs[glyphId];
        }
    }

    internal SfntBoundsComparison GetGlyphBoundsComparison(
        int glyphId,
        SfntBoundsPolicy policy)
    {
        SfntGlyfRecord record = GetGlyphRecord(glyphId);
        SfntGlyphHeader? header = SfntGlyphHeaderParser.Parse(record);
        return SfntPathBoundsCalculator.CompareGlyph(
            GetResolvedGlyph(glyphId),
            header,
            policy);
    }

    internal SfntBoundsComparison GetGlobalBoundsComparison(
        SfntHeadTable head,
        SfntBoundsPolicy policy)
    {
        return SfntPathBoundsCalculator.CompareFont(this, head, policy);
    }
}

internal static class SfntLocaTableParser
{
    static readonly SfntTableParser parser = new SfntTableParser(
        FontTags.Loca,
        0,
        0,
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

    internal static SfntLocaTable? FindTable(SfntTableCache cache)
    {
        SfntParsedTableView? view = FindView(cache);

        if (view == null || !view.HasParsedData())
        {
            return null;
        }

        return (SfntLocaTable)view.GetParsedData();
    }

    static SfntTableVersion ReadVersion(SfntTableData table)
    {
        return new SfntTableVersion(0, 0);
    }

    static object Parse(
        SfntTableData table,
        SfntTableVersion version,
        SfntTableVersionSupport support,
        FontLimits limits,
        SfntTableCache cache)
    {
        SfntHeadTable? head = SfntHeadTableParser.FindTable(cache);
        SfntMaxpTable? maxp = SfntMaxpTableParser.FindTable(cache);
        SfntTableData? glyf = cache.FindRawTable(FontTags.Glyf);

        if (head == null || maxp == null || glyf == null)
        {
            throw FontFormatException.Create(
                "The loca table requires supported head and maxp tables and a bounded glyf table.");
        }

        if (maxp.GetKind() != SfntMaxpKind.TrueType)
        {
            throw FontFormatException.Create(
                "The loca table requires the TrueType maxp form.");
        }

        int glyphCount = maxp.GetGlyphCount();
        SfntLocaFormat format = head.GetLocaFormat();
        int entrySize = format == SfntLocaFormat.Short ? 2 : 4;
        int expectedLength = CheckedFontMath.MultiplyCount(glyphCount + 1, entrySize);

        if (table.GetLength() != expectedLength)
        {
            throw FontFormatException.Create(
                "The loca table length is inconsistent with maxp and head.");
        }

        int[] offsets = ReadOffsets(table, glyphCount, format);
        FontDataSlice glyfData = glyf.GetDataSlice();

        if (offsets[glyphCount] > glyfData.GetLength())
        {
            throw FontFormatException.Create(
                "The loca table points outside the bounded glyf table.");
        }

        SfntGlyfRecord[] glyphRecords = new SfntGlyfRecord[glyphCount];
        int emptyGlyphCount = 0;
        int glyphId = 0;

        while (glyphId < glyphCount)
        {
            int glyphOffset = offsets[glyphId];
            int glyphLength = offsets[glyphId + 1] - glyphOffset;
            FontDataSlice glyphData = glyfData.GetSlice(glyphOffset, glyphLength);
            glyphRecords[glyphId] = new SfntGlyfRecord(
                glyphId,
                glyphOffset,
                glyphData);

            if (glyphLength == 0)
            {
                emptyGlyphCount += 1;
            }

            glyphId += 1;
        }

        return new SfntLocaTable(
            format,
            offsets,
            glyphRecords,
            glyfData.GetLength(),
            emptyGlyphCount,
            maxp,
            limits);
    }

    static int[] ReadOffsets(
        SfntTableData table,
        int glyphCount,
        SfntLocaFormat format)
    {
        BigEndianReader reader = new BigEndianReader(table.GetDataSlice());
        int[] offsets = new int[glyphCount + 1];
        int index = 0;

        while (index < offsets.Length)
        {
            uint encodedOffset = format == SfntLocaFormat.Short
                ? reader.ReadUInt16()
                : reader.ReadUInt32();
            long decodedOffset = format == SfntLocaFormat.Short
                ? encodedOffset * 2L
                : encodedOffset;

            if (decodedOffset > int.MaxValue)
            {
                throw FontFormatException.Create(
                    "A loca offset exceeds the supported range.");
            }

            offsets[index] = (int)decodedOffset;

            if (index != 0 && offsets[index] < offsets[index - 1])
            {
                throw FontFormatException.Create(
                    "The loca offsets are not monotonic.");
            }

            index += 1;
        }

        return offsets;
    }
}
