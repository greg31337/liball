namespace ttf;

internal class SfntGdefParserContext
{
    FontDataSlice tableData;
    int glyphCount;
    FontLimits limits;

    internal SfntGdefParserContext(
        FontDataSlice tableData,
        int glyphCount,
        FontLimits limits)
    {
        this.tableData = tableData;
        this.glyphCount = glyphCount;
        this.limits = limits;
    }

    internal SfntClassDefinitionTable ParseClassDefinition(
        int offset,
        int maximumClassValue,
        string message)
    {
        SfntClassDefinitionTable classes =
            SfntLayoutGlyphClassParser.ParseClassDefinition(
                tableData,
                offset,
                glyphCount,
                limits);

        if (classes.GetMaximumClassValue() > maximumClassValue)
        {
            throw FontFormatException.Create(message);
        }

        return classes;
    }

    internal SfntGdefAttachmentList ParseAttachmentList(int offset)
    {
        BigEndianReader reader = CreateReader(
            offset,
            4,
            "The GDEF AttachList header is truncated.");
        int coverageOffset = reader.ReadUInt16();
        int glyphCountInList = reader.ReadUInt16();
        ValidateCount(glyphCountInList);
        int headerLength = CheckedFontMath.GetRangeEnd(
            4,
            glyphCountInList,
            2);
        reader = CreateReader(
            offset,
            headerLength,
            "The GDEF AttachList offset array is truncated.");
        reader.ReadUInt16();
        reader.ReadUInt16();
        int coverageTableOffset = ResolveRequiredOffset(
            offset,
            coverageOffset,
            headerLength,
            2,
            "The GDEF AttachList Coverage offset is invalid.");
        int[] attachmentOffsets = new int[glyphCountInList];
        int index = 0;

        while (index < attachmentOffsets.Length)
        {
            attachmentOffsets[index] = reader.ReadUInt16();
            index += 1;
        }

        SfntCoverageTable coverage =
            SfntLayoutGlyphClassParser.ParseCoverage(
                tableData,
                coverageTableOffset,
                glyphCount,
                limits);

        if (coverage.GetCoveredGlyphCount() != glyphCountInList)
        {
            throw FontFormatException.Create(
                "The GDEF AttachList glyph count does not match its Coverage table.");
        }

        SfntGdefAttachmentPoint[] points =
            new SfntGdefAttachmentPoint[glyphCountInList];
        index = 0;

        while (index < points.Length)
        {
            points[index] = ParseAttachmentPoint(ResolveRequiredOffset(
                offset,
                attachmentOffsets[index],
                headerLength,
                2,
                "A GDEF AttachPoint offset is invalid."));
            index += 1;
        }

        return new SfntGdefAttachmentList(coverage, points);
    }

    internal SfntGdefLigatureCaretList ParseLigatureCaretList(int offset)
    {
        BigEndianReader reader = CreateReader(
            offset,
            4,
            "The GDEF LigCaretList header is truncated.");
        int coverageOffset = reader.ReadUInt16();
        int glyphCountInList = reader.ReadUInt16();
        ValidateCount(glyphCountInList);
        int headerLength = CheckedFontMath.GetRangeEnd(
            4,
            glyphCountInList,
            2);
        reader = CreateReader(
            offset,
            headerLength,
            "The GDEF LigCaretList offset array is truncated.");
        reader.ReadUInt16();
        reader.ReadUInt16();
        int coverageTableOffset = ResolveRequiredOffset(
            offset,
            coverageOffset,
            headerLength,
            2,
            "The GDEF LigCaretList Coverage offset is invalid.");
        int[] glyphOffsets = new int[glyphCountInList];
        int index = 0;

        while (index < glyphOffsets.Length)
        {
            glyphOffsets[index] = reader.ReadUInt16();
            index += 1;
        }

        SfntCoverageTable coverage =
            SfntLayoutGlyphClassParser.ParseCoverage(
                tableData,
                coverageTableOffset,
                glyphCount,
                limits);

        if (coverage.GetCoveredGlyphCount() != glyphCountInList)
        {
            throw FontFormatException.Create(
                "The GDEF LigCaretList glyph count does not match its Coverage table.");
        }

        SfntGdefLigatureGlyph[] glyphs =
            new SfntGdefLigatureGlyph[glyphCountInList];
        index = 0;

        while (index < glyphs.Length)
        {
            glyphs[index] = ParseLigatureGlyph(ResolveRequiredOffset(
                offset,
                glyphOffsets[index],
                headerLength,
                2,
                "A GDEF LigGlyph offset is invalid."));
            index += 1;
        }

        return new SfntGdefLigatureCaretList(coverage, glyphs);
    }

    internal SfntGdefMarkGlyphSets ParseMarkGlyphSets(int offset)
    {
        BigEndianReader reader = CreateReader(
            offset,
            4,
            "The GDEF MarkGlyphSetsDef header is truncated.");
        int format = reader.ReadUInt16();
        int setCount = reader.ReadUInt16();

        if (format != 1)
        {
            throw FontFormatException.Create(
                "The GDEF MarkGlyphSetsDef format is not supported.");
        }

        ValidateCount(setCount);
        int headerLength = CheckedFontMath.GetRangeEnd(4, setCount, 4);
        reader = CreateReader(
            offset,
            headerLength,
            "The GDEF MarkGlyphSetsDef offset array is truncated.");
        reader.ReadUInt16();
        reader.ReadUInt16();
        SfntCoverageTable[] coverages = new SfntCoverageTable[setCount];
        int index = 0;

        while (index < coverages.Length)
        {
            long encodedOffset = reader.ReadUInt32();
            int coverageOffset = ResolveRequiredOffset32(
                offset,
                encodedOffset,
                headerLength,
                2,
                "A GDEF mark-set Coverage offset is invalid.");
            coverages[index] = SfntLayoutGlyphClassParser.ParseCoverage(
                tableData,
                coverageOffset,
                glyphCount,
                limits);
            index += 1;
        }

        return new SfntGdefMarkGlyphSets(format, coverages);
    }

    SfntGdefAttachmentPoint ParseAttachmentPoint(int offset)
    {
        BigEndianReader reader = CreateReader(
            offset,
            2,
            "A GDEF AttachPoint header is truncated.");
        int pointCount = reader.ReadUInt16();
        ValidateCount(pointCount);
        int length = CheckedFontMath.GetRangeEnd(2, pointCount, 2);
        reader = CreateReader(
            offset,
            length,
            "A GDEF AttachPoint array is truncated.");
        reader.ReadUInt16();
        int[] pointIndices = new int[pointCount];
        int previousPointIndex = -1;
        int index = 0;

        while (index < pointIndices.Length)
        {
            int pointIndex = reader.ReadUInt16();

            if (pointIndex <= previousPointIndex)
            {
                throw FontFormatException.Create(
                    "A GDEF AttachPoint array is not strictly ordered.");
            }

            pointIndices[index] = pointIndex;
            previousPointIndex = pointIndex;
            index += 1;
        }

        return new SfntGdefAttachmentPoint(pointIndices);
    }

    SfntGdefLigatureGlyph ParseLigatureGlyph(int offset)
    {
        BigEndianReader reader = CreateReader(
            offset,
            2,
            "A GDEF LigGlyph header is truncated.");
        int caretCount = reader.ReadUInt16();
        ValidateCount(caretCount);
        int headerLength = CheckedFontMath.GetRangeEnd(2, caretCount, 2);
        reader = CreateReader(
            offset,
            headerLength,
            "A GDEF LigGlyph offset array is truncated.");
        reader.ReadUInt16();
        int[] caretOffsets = new int[caretCount];
        int index = 0;

        while (index < caretOffsets.Length)
        {
            caretOffsets[index] = reader.ReadUInt16();
            index += 1;
        }

        SfntGdefCaretValue[] carets = new SfntGdefCaretValue[caretCount];
        index = 0;

        while (index < carets.Length)
        {
            carets[index] = ParseCaretValue(ResolveRequiredOffset(
                offset,
                caretOffsets[index],
                headerLength,
                4,
                "A GDEF CaretValue offset is invalid."));
            index += 1;
        }

        return new SfntGdefLigatureGlyph(carets);
    }

    SfntGdefCaretValue ParseCaretValue(int offset)
    {
        BigEndianReader reader = CreateReader(
            offset,
            4,
            "A GDEF CaretValue table is truncated.");
        int format = reader.ReadUInt16();

        if (format == 1)
        {
            return new SfntGdefCaretValue(
                format,
                reader.ReadInt16(),
                0,
                0,
                0,
                null);
        }

        if (format == 2)
        {
            return new SfntGdefCaretValue(
                format,
                0,
                reader.ReadUInt16(),
                0,
                0,
                null);
        }

        if (format == 3)
        {
            reader = CreateReader(
                offset,
                6,
                "A format 3 GDEF CaretValue table is truncated.");
            reader.ReadUInt16();
            int coordinate = reader.ReadInt16();
            int adjustmentOffset = reader.ReadUInt16();
            int adjustmentTableOffset = adjustmentOffset == 0 ? 0 :
                ResolveRequiredOffset(
                    offset,
                    adjustmentOffset,
                    6,
                    6,
                    "A GDEF CaretValue Device or VariationIndex offset is invalid.");
            SfntDeviceOrVariationIndex? adjustment = adjustmentOffset == 0 ?
                null : SfntLayoutPrimitiveParser.ParseDeviceOrVariationIndex(
                    tableData,
                    adjustmentTableOffset,
                    limits);
            return new SfntGdefCaretValue(
                format,
                coordinate,
                0,
                adjustmentOffset,
                adjustmentTableOffset,
                adjustment);
        }

        throw FontFormatException.Create(
            "The GDEF CaretValue format is not supported.");
    }

    BigEndianReader CreateReader(
        int offset,
        int requiredLength,
        string message)
    {
        if (offset < 0 ||
            requiredLength < 0 ||
            offset > tableData.GetLength() - requiredLength)
        {
            throw FontFormatException.Create(message);
        }

        return new BigEndianReader(tableData.GetSlice(
            offset,
            tableData.GetLength() - offset));
    }

    int ResolveRequiredOffset(
        int parentOffset,
        int relativeOffset,
        int minimumRelativeOffset,
        int requiredLength,
        string message)
    {
        if (relativeOffset < minimumRelativeOffset)
        {
            throw FontFormatException.Create(message);
        }

        int absoluteOffset = CheckedFontMath.AddOffset(
            parentOffset,
            relativeOffset);

        if (absoluteOffset > tableData.GetLength() - requiredLength)
        {
            throw FontFormatException.Create(message);
        }

        return absoluteOffset;
    }

    int ResolveRequiredOffset32(
        int parentOffset,
        long relativeOffset,
        int minimumRelativeOffset,
        int requiredLength,
        string message)
    {
        if (relativeOffset < minimumRelativeOffset ||
            relativeOffset > int.MaxValue)
        {
            throw FontFormatException.Create(message);
        }

        return ResolveRequiredOffset(
            parentOffset,
            (int)relativeOffset,
            minimumRelativeOffset,
            requiredLength,
            message);
    }

    void ValidateCount(int count)
    {
        if (count > limits.GetMaxCoverageEntries())
        {
            throw FontLimitException.Create(
                "GDEF data exceeds the configured coverage-entry limit.");
        }
    }
}

internal static class SfntGdefTableParser
{
    const int Version10HeaderLength = 12;
    const int Version12HeaderLength = 14;
    const int Version13HeaderLength = 18;
    static readonly SfntTableParser parser = new SfntTableParser(
        FontTags.Gdef,
        1,
        1,
        3,
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

    internal static SfntGdefTable? FindTable(SfntTableCache cache)
    {
        SfntParsedTableView? view = FindView(cache);

        if (view == null || !view.HasParsedData())
        {
            return null;
        }

        return (SfntGdefTable)view.GetParsedData();
    }

    static SfntTableVersion ReadVersion(SfntTableData table)
    {
        BigEndianReader reader = new BigEndianReader(table.GetDataSlice());
        return new SfntTableVersion(
            reader.ReadUInt16(),
            reader.ReadUInt16());
    }

    static object Parse(
        SfntTableData table,
        SfntTableVersion version,
        SfntTableVersionSupport support,
        FontLimits limits,
        SfntTableCache cache)
    {
        FontDataSlice tableData = table.GetDataSlice();
        int minorVersion = version.GetMinorVersion();
        int headerLength = GetHeaderLength(minorVersion);

        if (tableData.GetLength() < headerLength)
        {
            throw FontFormatException.Create("The GDEF table header is truncated.");
        }

        SfntMaxpTable? maxp = SfntMaxpTableParser.FindTable(cache);

        if (maxp == null)
        {
            throw FontFormatException.Create(
                "The GDEF table requires a supported maxp table.");
        }

        BigEndianReader reader = new BigEndianReader(tableData);
        int majorVersion = reader.ReadUInt16();
        minorVersion = reader.ReadUInt16();
        int glyphClassOffset = reader.ReadUInt16();
        int attachmentListOffset = reader.ReadUInt16();
        int ligatureCaretListOffset = reader.ReadUInt16();
        int markAttachmentClassOffset = reader.ReadUInt16();
        int markGlyphSetsOffset = minorVersion >= 2 ?
            reader.ReadUInt16() : 0;
        long itemVariationStoreOffset = minorVersion >= 3 ?
            reader.ReadUInt32() : 0;
        SfntGdefParserContext context = new SfntGdefParserContext(
            tableData,
            maxp.GetGlyphCount(),
            limits);
        SfntClassDefinitionTable? glyphClasses = glyphClassOffset == 0 ?
            null : context.ParseClassDefinition(
                ResolveTopLevelOffset(
                    tableData,
                    glyphClassOffset,
                    headerLength,
                    2,
                    "The GDEF GlyphClassDef offset is invalid."),
                4,
                "A GDEF glyph class value is invalid.");
        SfntGdefAttachmentList? attachmentList = attachmentListOffset == 0 ?
            null : context.ParseAttachmentList(ResolveTopLevelOffset(
                tableData,
                attachmentListOffset,
                headerLength,
                4,
                "The GDEF AttachList offset is invalid."));
        SfntGdefLigatureCaretList? ligatureCaretList =
            ligatureCaretListOffset == 0 ? null :
            context.ParseLigatureCaretList(ResolveTopLevelOffset(
                tableData,
                ligatureCaretListOffset,
                headerLength,
                4,
                "The GDEF LigCaretList offset is invalid."));
        SfntClassDefinitionTable? markAttachmentClasses =
            markAttachmentClassOffset == 0 ? null :
            context.ParseClassDefinition(
                ResolveTopLevelOffset(
                    tableData,
                    markAttachmentClassOffset,
                    headerLength,
                    2,
                    "The GDEF MarkAttachClassDef offset is invalid."),
                255,
                "A GDEF mark attachment class value is invalid.");
        SfntGdefMarkGlyphSets? markGlyphSets = markGlyphSetsOffset == 0 ?
            null : context.ParseMarkGlyphSets(ResolveTopLevelOffset(
                tableData,
                markGlyphSetsOffset,
                headerLength,
                4,
                "The GDEF MarkGlyphSetsDef offset is invalid."));
        SfntItemVariationStore? itemVariationStore = null;

        if (itemVariationStoreOffset != 0)
        {
            SfntFvarTable? fvar = SfntFvarTableParser.FindTable(cache);

            if (fvar == null)
            {
                throw FontFormatException.Create(
                    "The GDEF ItemVariationStore requires a supported fvar table.");
            }

            int storeOffset = ResolveTopLevelOffset32(
                tableData,
                itemVariationStoreOffset,
                headerLength,
                8,
                "The GDEF ItemVariationStore offset is invalid.");
            itemVariationStore = SfntItemVariationStoreParser.Parse(
                tableData,
                storeOffset,
                headerLength,
                fvar.GetAxisCount(),
                limits,
                new SfntVariationParsingBudget(
                    limits.GetMaxCoverageEntries()));
        }

        ValidateCaretVariationIndexes(
            ligatureCaretList,
            itemVariationStore);

        return new SfntGdefTable(
            majorVersion,
            minorVersion,
            glyphClasses,
            attachmentList,
            ligatureCaretList,
            markAttachmentClasses,
            markGlyphSets,
            itemVariationStore);
    }

    static void ValidateCaretVariationIndexes(
        SfntGdefLigatureCaretList? ligatureCaretList,
        SfntItemVariationStore? itemVariationStore)
    {
        if (ligatureCaretList == null)
        {
            return;
        }

        int glyphIndex = 0;

        while (glyphIndex < ligatureCaretList.GetGlyphCount())
        {
            SfntGdefLigatureGlyph glyph =
                ligatureCaretList.GetLigatureGlyph(glyphIndex);
            int caretIndex = 0;

            while (caretIndex < glyph.GetCaretCount())
            {
                SfntDeviceOrVariationIndex? adjustment =
                    glyph.GetCaretValue(caretIndex).GetAdjustment();

                if (adjustment != null && adjustment.IsVariationIndex())
                {
                    if (itemVariationStore == null)
                    {
                        throw FontFormatException.Create(
                            "A GDEF caret VariationIndex requires an ItemVariationStore.");
                    }

                    SfntVariationIndex? variationIndex = adjustment.GetVariationIndex();
                    System.ArgumentNullException.ThrowIfNull(variationIndex);
                    itemVariationStore.ValidateDeltaSetIndex(
                        variationIndex.GetDeltaSetIndex());
                }

                caretIndex += 1;
            }

            glyphIndex += 1;
        }
    }

    static int GetHeaderLength(int minorVersion)
    {
        if (minorVersion == 0)
        {
            return Version10HeaderLength;
        }

        if (minorVersion == 2)
        {
            return Version12HeaderLength;
        }

        if (minorVersion >= 3)
        {
            return Version13HeaderLength;
        }

        throw FontFormatException.Create("The GDEF table version is not supported.");
    }

    static int ResolveTopLevelOffset(
        FontDataSlice tableData,
        int offset,
        int minimumOffset,
        int requiredLength,
        string message)
    {
        if (offset < minimumOffset ||
            offset > tableData.GetLength() - requiredLength)
        {
            throw FontFormatException.Create(message);
        }

        return offset;
    }

    static int ResolveTopLevelOffset32(
        FontDataSlice tableData,
        long offset,
        int minimumOffset,
        int requiredLength,
        string message)
    {
        if (offset > int.MaxValue)
        {
            throw FontFormatException.Create(message);
        }

        return ResolveTopLevelOffset(
            tableData,
            (int)offset,
            minimumOffset,
            requiredLength,
            message);
    }
}
