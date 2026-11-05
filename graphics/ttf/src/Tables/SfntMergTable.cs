namespace ttf;

internal class SfntMergClassRange
{
    int startGlyphId;
    int endGlyphId;
    int classId;

    internal SfntMergClassRange(int startGlyphId, int endGlyphId, int classId)
    {
        this.startGlyphId = startGlyphId;
        this.endGlyphId = endGlyphId;
        this.classId = classId;
    }

    internal int GetStartGlyphId() { return startGlyphId; }
    internal int GetEndGlyphId() { return endGlyphId; }
    internal int GetClassId() { return classId; }
}

internal class SfntMergClassDefinition
{
    int format;
    int startGlyphId;
    int[] classValues;
    SfntMergClassRange[] ranges;
    int dataOffset;
    int dataLength;

    internal SfntMergClassDefinition(
        int format,
        int startGlyphId,
        int[] classValues,
        SfntMergClassRange[] ranges,
        int dataOffset,
        int dataLength)
    {
        this.format = format;
        this.startGlyphId = startGlyphId;
        this.classValues = classValues;
        this.ranges = ranges;
        this.dataOffset = dataOffset;
        this.dataLength = dataLength;
    }

    internal int GetFormat() { return format; }
    internal int GetDataOffset() { return dataOffset; }
    internal int GetDataLength() { return dataLength; }
    internal int GetClassValueCount() { return classValues.Length; }
    internal int GetRangeCount() { return ranges.Length; }
    internal SfntMergClassRange GetRange(int index) { return ranges[index]; }

    internal int GetFirstGlyphId()
    {
        if (classValues.Length != 0) { return startGlyphId; }
        if (ranges.Length != 0) { return ranges[0].GetStartGlyphId(); }
        return -1;
    }

    internal int GetLastGlyphId()
    {
        if (classValues.Length != 0)
        {
            return startGlyphId + classValues.Length - 1;
        }

        if (ranges.Length != 0)
        {
            return ranges[ranges.Length - 1].GetEndGlyphId();
        }

        return -1;
    }

    internal int GetClassForGlyph(int glyphId)
    {
        if (format == 1)
        {
            int index = glyphId - startGlyphId;
            if (index >= 0 && index < classValues.Length) { return classValues[index]; }
            return 0;
        }

        int rangeIndex = 0;
        while (rangeIndex < ranges.Length)
        {
            SfntMergClassRange range = ranges[rangeIndex];
            if (glyphId < range.GetStartGlyphId()) { return 0; }
            if (glyphId <= range.GetEndGlyphId()) { return range.GetClassId(); }
            rangeIndex += 1;
        }

        return 0;
    }
}

internal class SfntMergEntry
{
    int behavior;

    internal SfntMergEntry(int behavior) { this.behavior = behavior; }
    internal int GetBehavior() { return behavior; }
    internal bool MergesLeftToRight() { return (behavior & 0x01) != 0; }
    internal bool GroupsLeftToRight() { return (behavior & 0x02) != 0; }
    internal bool IsSubordinateLeftToRight() { return (behavior & 0x04) != 0; }
    internal bool MergesRightToLeft() { return (behavior & 0x10) != 0; }
    internal bool GroupsRightToLeft() { return (behavior & 0x20) != 0; }
    internal bool IsSubordinateRightToLeft() { return (behavior & 0x40) != 0; }
}

internal class SfntMergTable
{
    int version;
    int mergeClassCount;
    SfntMergClassDefinition[] classDefinitions;
    SfntMergEntry[] entries;

    internal SfntMergTable(
        int version,
        int mergeClassCount,
        SfntMergClassDefinition[] classDefinitions,
        SfntMergEntry[] entries)
    {
        this.version = version;
        this.mergeClassCount = mergeClassCount;
        this.classDefinitions = classDefinitions;
        this.entries = entries;
    }

    internal int GetVersion() { return version; }
    internal int GetMergeClassCount() { return mergeClassCount; }
    internal int GetClassDefinitionCount() { return classDefinitions.Length; }
    internal SfntMergClassDefinition GetClassDefinition(int index) { return classDefinitions[index]; }
    internal SfntMergEntry GetEntry(int leftClassId, int rightClassId)
    {
        if (leftClassId < 0 || leftClassId >= mergeClassCount)
        {
            throw new System.ArgumentOutOfRangeException("leftClassId");
        }
        if (rightClassId < 0 || rightClassId >= mergeClassCount)
        {
            throw new System.ArgumentOutOfRangeException("rightClassId");
        }
        return entries[leftClassId * mergeClassCount + rightClassId];
    }

    internal int GetClassForGlyph(int glyphId)
    {
        int index = 0;
        while (index < classDefinitions.Length)
        {
            int classId = classDefinitions[index].GetClassForGlyph(glyphId);
            if (classId != 0) { return classId; }
            index += 1;
        }
        return 0;
    }
}

internal static class SfntMergTableParser
{
    const int HeaderLength = 10;
    static readonly SfntTableParser parser = new SfntTableParser(
        FontTags.Merg, 0, 0, 0, ReadVersion, Parse);

    internal static SfntTableParser GetParser() { return parser; }

    internal static SfntParsedTableView? FindView(SfntTableCache cache)
    {
        if (cache == null) { throw new System.ArgumentNullException("cache"); }
        return cache.FindParsedTable(parser);
    }

    internal static SfntMergTable? FindTable(SfntTableCache cache)
    {
        SfntParsedTableView? view = FindView(cache);
        if (view == null || !view.HasParsedData()) { return null; }
        return (SfntMergTable)view.GetParsedData();
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
        FontDataSlice data = table.GetDataSlice();
        if (data.GetLength() < HeaderLength)
        {
            throw FontFormatException.Create("The MERG table header is truncated.");
        }

        SfntMaxpTable? maxp = SfntMaxpTableParser.FindTable(cache);
        if (maxp == null)
        {
            throw FontFormatException.Create("The MERG table requires a supported maxp table.");
        }

        BigEndianReader reader = new BigEndianReader(data);
        int encodedVersion = reader.ReadUInt16();
        int classCount = reader.ReadUInt16();
        int matrixOffset = reader.ReadOffset16();
        int definitionCount = reader.ReadUInt16();
        int offsetsOffset = reader.ReadOffset16();
        if (encodedVersion != 0)
        {
            throw FontFormatException.Create("The MERG table version is not supported.");
        }

        int matrixLength = CheckedFontMath.MultiplyCount(classCount, classCount);
        if (matrixLength > limits.GetMaxCoverageEntries())
        {
            throw FontLimitException.Create("The MERG matrix exceeds the configured coverage limit.");
        }
        ValidateRegion(data.GetLength(), matrixOffset, matrixLength, HeaderLength);

        if (definitionCount > limits.GetMaxCoverageEntries())
        {
            throw FontLimitException.Create("The MERG class-definition count exceeds the configured coverage limit.");
        }

        int offsetsLength = CheckedFontMath.MultiplyCount(definitionCount, 2);
        if (definitionCount == 0)
        {
            if (offsetsOffset != 0)
            {
                throw FontFormatException.Create("An empty MERG class-definition array has a nonzero offset.");
            }
        }
        else
        {
            ValidateRegion(data.GetLength(), offsetsOffset, offsetsLength, HeaderLength);
        }

        if (RegionsOverlap(matrixOffset, matrixLength, offsetsOffset, offsetsLength))
        {
            throw FontFormatException.Create("MERG table regions overlap.");
        }

        int[] definitionOffsets = new int[definitionCount];
        if (definitionCount != 0)
        {
            BigEndianReader offsetsReader = new BigEndianReader(data.GetSlice(offsetsOffset, offsetsLength));
            int offsetIndex = 0;
            while (offsetIndex < definitionCount)
            {
                definitionOffsets[offsetIndex] = offsetsReader.ReadOffset16();
                offsetIndex += 1;
            }
        }

        SfntMergClassDefinition[] definitions = new SfntMergClassDefinition[definitionCount];
        int inspectedGlyphs = 0;
        int previousLastGlyph = -1;
        int index = 0;
        while (index < definitionCount)
        {
            SfntMergClassDefinition definition = ParseDefinition(
                data,
                definitionOffsets[index],
                maxp.GetGlyphCount(),
                limits,
                inspectedGlyphs);
            int firstGlyph = definition.GetFirstGlyphId();
            if (firstGlyph >= 0 && firstGlyph <= previousLastGlyph)
            {
                throw FontFormatException.Create("MERG class definitions contain duplicate or unordered glyphs.");
            }
            if (firstGlyph >= 0)
            {
                previousLastGlyph = definition.GetLastGlyphId();
                inspectedGlyphs += GetReferencedGlyphCount(definition);
            }

            if (RegionsOverlap(matrixOffset, matrixLength, definition.GetDataOffset(), definition.GetDataLength()) ||
                RegionsOverlap(offsetsOffset, offsetsLength, definition.GetDataOffset(), definition.GetDataLength()))
            {
                throw FontFormatException.Create("MERG table regions overlap.");
            }

            int prior = 0;
            while (prior < index)
            {
                if (RegionsOverlap(
                    definitions[prior].GetDataOffset(),
                    definitions[prior].GetDataLength(),
                    definition.GetDataOffset(),
                    definition.GetDataLength()))
                {
                    throw FontFormatException.Create("MERG class definitions overlap.");
                }
                prior += 1;
            }

            definitions[index] = definition;
            index += 1;
        }

        SfntMergEntry[] entries = new SfntMergEntry[matrixLength];
        BigEndianReader matrixReader = new BigEndianReader(data.GetSlice(matrixOffset, matrixLength));
        index = 0;
        while (index < entries.Length)
        {
            int behavior = matrixReader.ReadUInt8();
            if ((behavior & 0x88) != 0)
            {
                throw FontFormatException.Create("A MERG matrix entry uses reserved behavior bits.");
            }
            entries[index] = new SfntMergEntry(behavior);
            index += 1;
        }

        return new SfntMergTable(encodedVersion, classCount, definitions, entries);
    }

    static SfntMergClassDefinition ParseDefinition(
        FontDataSlice data,
        int offset,
        int glyphCount,
        FontLimits limits,
        int inspectedGlyphs)
    {
        ValidateRegion(data.GetLength(), offset, 2, HeaderLength);
        BigEndianReader reader = new BigEndianReader(data.GetSlice(offset, data.GetLength() - offset));
        int format = reader.ReadUInt16();
        if (format == 1)
        {
            if (reader.GetRemainingLength() < 4)
            {
                throw FontFormatException.Create("A MERG format 1 class definition is truncated.");
            }
            int startGlyph = reader.ReadUInt16();
            int valueCount = reader.ReadUInt16();
            int length = CheckedFontMath.AddOffset(6, CheckedFontMath.MultiplyCount(valueCount, 2));
            ValidateRegion(data.GetLength(), offset, length, HeaderLength);
            ValidateGlyphSpan(startGlyph, valueCount, glyphCount);
            if (valueCount > limits.GetMaxCoverageEntries() - inspectedGlyphs)
            {
                throw FontLimitException.Create("MERG glyph coverage exceeds the configured limit.");
            }
            int[] values = new int[valueCount];
            int index = 0;
            while (index < valueCount)
            {
                values[index] = reader.ReadUInt16();
                index += 1;
            }
            return new SfntMergClassDefinition(
                format, startGlyph, values, new SfntMergClassRange[0], offset, length);
        }

        if (format == 2)
        {
            if (reader.GetRemainingLength() < 2)
            {
                throw FontFormatException.Create("A MERG format 2 class definition is truncated.");
            }
            int rangeCount = reader.ReadUInt16();
            int length = CheckedFontMath.AddOffset(4, CheckedFontMath.MultiplyCount(rangeCount, 6));
            ValidateRegion(data.GetLength(), offset, length, HeaderLength);
            if (rangeCount > limits.GetMaxCoverageEntries())
            {
                throw FontLimitException.Create("The MERG range count exceeds the configured coverage limit.");
            }
            SfntMergClassRange[] ranges = new SfntMergClassRange[rangeCount];
            int totalGlyphs = inspectedGlyphs;
            int previousEnd = -1;
            int index = 0;
            while (index < rangeCount)
            {
                int startGlyph = reader.ReadUInt16();
                int endGlyph = reader.ReadUInt16();
                int classId = reader.ReadUInt16();
                if (startGlyph > endGlyph || startGlyph <= previousEnd || endGlyph >= glyphCount)
                {
                    throw FontFormatException.Create("A MERG class range is invalid.");
                }
                int rangeLength = endGlyph - startGlyph + 1;
                if (rangeLength > limits.GetMaxCoverageEntries() - totalGlyphs)
                {
                    throw FontLimitException.Create("MERG glyph coverage exceeds the configured limit.");
                }
                totalGlyphs += rangeLength;
                ranges[index] = new SfntMergClassRange(startGlyph, endGlyph, classId);
                previousEnd = endGlyph;
                index += 1;
            }
            return new SfntMergClassDefinition(
                format, 0, new int[0], ranges, offset, length);
        }

        throw FontFormatException.Create("A MERG class definition format is unsupported.");
    }

    static int GetReferencedGlyphCount(SfntMergClassDefinition definition)
    {
        if (definition.GetFormat() == 1) { return definition.GetClassValueCount(); }
        int count = 0;
        int index = 0;
        while (index < definition.GetRangeCount())
        {
            SfntMergClassRange range = definition.GetRange(index);
            count += range.GetEndGlyphId() - range.GetStartGlyphId() + 1;
            index += 1;
        }
        return count;
    }

    static void ValidateGlyphSpan(int startGlyph, int count, int glyphCount)
    {
        if (count != 0 && (startGlyph >= glyphCount || count > glyphCount - startGlyph))
        {
            throw FontFormatException.Create("A MERG class definition references an invalid glyph.");
        }
    }

    static void ValidateRegion(int tableLength, int offset, int length, int minimumOffset)
    {
        if (offset < minimumOffset || length < 0 || offset > tableLength - length)
        {
            throw FontFormatException.Create("A MERG table region is out of bounds.");
        }
    }

    static bool RegionsOverlap(int firstOffset, int firstLength, int secondOffset, int secondLength)
    {
        if (firstLength == 0 || secondLength == 0) { return false; }
        return firstOffset < secondOffset + secondLength && secondOffset < firstOffset + firstLength;
    }
}
