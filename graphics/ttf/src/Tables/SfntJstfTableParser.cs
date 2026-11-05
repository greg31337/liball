namespace ttf;

internal class SfntJstfParserContext
{
    const int ScriptHeaderLength = 6;
    const int PriorityLength = 20;
    FontDataSlice tableData;
    FontLimits limits;
    int glyphCount;
    int substitutionLookupCount;
    int positioningLookupCount;
    SfntItemVariationStore? variationStore;
    int parsedEntries;

    internal SfntJstfParserContext(
        FontDataSlice tableData,
        FontLimits limits,
        int glyphCount,
        int substitutionLookupCount,
        int positioningLookupCount,
        SfntItemVariationStore? variationStore)
    {
        this.tableData = tableData;
        this.limits = limits;
        this.glyphCount = glyphCount;
        this.substitutionLookupCount = substitutionLookupCount;
        this.positioningLookupCount = positioningLookupCount;
        this.variationStore = variationStore;
        parsedEntries = 0;
    }

    internal SfntJstfTable Parse()
    {
        const int RecordLength = 6;
        BigEndianReader reader = CreateReader(
            0,
            6,
            "The JSTF header is truncated.");
        int majorVersion = reader.ReadUInt16();
        int minorVersion = reader.ReadUInt16();
        int scriptCount = reader.ReadUInt16();

        if (majorVersion != 1)
        {
            throw FontFormatException.Create(
                "The JSTF table version is not supported.");
        }

        AddEntries(scriptCount);
        int headerLength = CheckedFontMath.GetRangeEnd(
            6,
            scriptCount,
            RecordLength);
        reader = CreateReader(
            0,
            headerLength,
            "The JSTF script-record array is truncated.");
        reader.ReadUInt16();
        reader.ReadUInt16();
        reader.ReadUInt16();
        int[] tags = new int[scriptCount];
        int[] offsets = new int[scriptCount];
        uint previousTag = 0;
        int index = 0;

        while (index < scriptCount)
        {
            int tag = reader.ReadTag();
            ValidateTag(tag, "A JSTF script tag is invalid.");

            uint encodedTag = CheckedFontMath.ReinterpretInt32AsUInt32(tag);

            if (index > 0 && encodedTag <= previousTag)
            {
                throw FontFormatException.Create(
                    "JSTF script records are not strictly ordered.");
            }

            tags[index] = tag;
            offsets[index] = reader.ReadUInt16();
            previousTag = encodedTag;
            index += 1;
        }

        SfntJstfScript[] scripts = new SfntJstfScript[scriptCount];
        index = 0;

        while (index < scriptCount)
        {
            int scriptOffset = ResolveRequiredOffset(
                0,
                offsets[index],
                headerLength,
                ScriptHeaderLength,
                "A JSTF script offset is invalid.");
            scripts[index] = ParseScript(scriptOffset, tags[index]);
            index += 1;
        }

        return new SfntJstfTable(
            majorVersion,
            minorVersion,
            scripts);
    }

    SfntJstfScript ParseScript(int offset, int scriptTag)
    {
        const int RecordLength = 6;
        BigEndianReader reader = CreateReader(
            offset,
            ScriptHeaderLength,
            "A JSTF script header is truncated.");
        int extenderOffset = reader.ReadUInt16();
        int defaultLanguageOffset = reader.ReadUInt16();
        int languageCount = reader.ReadUInt16();
        AddEntries(languageCount);
        int headerLength = CheckedFontMath.GetRangeEnd(
            ScriptHeaderLength,
            languageCount,
            RecordLength);
        reader = CreateReader(
            offset,
            headerLength,
            "A JSTF language-system record array is truncated.");
        reader.ReadUInt16();
        reader.ReadUInt16();
        reader.ReadUInt16();
        int[] languageTags = new int[languageCount];
        int[] languageOffsets = new int[languageCount];
        uint previousTag = 0;
        int index = 0;

        while (index < languageCount)
        {
            int tag = reader.ReadTag();
            ValidateTag(tag, "A JSTF language-system tag is invalid.");

            uint encodedTag = CheckedFontMath.ReinterpretInt32AsUInt32(tag);

            if (index > 0 && encodedTag <= previousTag)
            {
                throw FontFormatException.Create(
                    "JSTF language-system records are not strictly ordered.");
            }

            languageTags[index] = tag;
            languageOffsets[index] = reader.ReadUInt16();
            previousTag = encodedTag;
            index += 1;
        }

        int[] extenders = extenderOffset == 0 ? new int[0] :
            ParseExtenders(ResolveOffset(
                offset,
                extenderOffset,
                headerLength,
                2,
                "A JSTF extender-glyph offset is invalid."));
        SfntJstfLanguageSystem? defaultLanguage =
            defaultLanguageOffset == 0 ? null :
            ParseLanguageSystem(
                ResolveOffset(
                    offset,
                    defaultLanguageOffset,
                    headerLength,
                    2,
                    "A JSTF default language-system offset is invalid."),
                0);
        SfntJstfLanguageSystem[] languages =
            new SfntJstfLanguageSystem[languageCount];
        index = 0;

        while (index < languageCount)
        {
            int languageOffset = ResolveRequiredOffset(
                offset,
                languageOffsets[index],
                headerLength,
                2,
                "A JSTF language-system offset is invalid.");
            languages[index] = ParseLanguageSystem(
                languageOffset,
                languageTags[index]);
            index += 1;
        }

        return new SfntJstfScript(
            scriptTag,
            extenders,
            defaultLanguage,
            languages);
    }

    int[] ParseExtenders(int offset)
    {
        BigEndianReader reader = CreateReader(
            offset,
            2,
            "A JSTF extender-glyph table is truncated.");
        int count = reader.ReadUInt16();
        AddEntries(count);
        int length = CheckedFontMath.GetRangeEnd(2, count, 2);
        reader = CreateReader(
            offset,
            length,
            "A JSTF extender-glyph array is truncated.");
        reader.ReadUInt16();
        int[] glyphIds = new int[count];
        int previousGlyphId = -1;
        int index = 0;

        while (index < count)
        {
            int glyphId = reader.ReadUInt16();

            if (glyphId >= glyphCount)
            {
                throw FontFormatException.Create(
                    "A JSTF extender glyph ID is invalid.");
            }

            if (glyphId <= previousGlyphId)
            {
                throw FontFormatException.Create(
                    "JSTF extender glyph IDs are not strictly ordered.");
            }

            glyphIds[index] = glyphId;
            previousGlyphId = glyphId;
            index += 1;
        }

        return glyphIds;
    }

    SfntJstfLanguageSystem ParseLanguageSystem(
        int offset,
        int languageTag)
    {
        BigEndianReader reader = CreateReader(
            offset,
            2,
            "A JSTF language-system table is truncated.");
        int priorityCount = reader.ReadUInt16();
        AddEntries(priorityCount);
        int headerLength = CheckedFontMath.GetRangeEnd(
            2,
            priorityCount,
            2);
        reader = CreateReader(
            offset,
            headerLength,
            "A JSTF priority-offset array is truncated.");
        reader.ReadUInt16();
        int[] offsets = new int[priorityCount];
        int index = 0;

        while (index < priorityCount)
        {
            offsets[index] = reader.ReadUInt16();
            index += 1;
        }

        SfntJstfPriority[] priorities =
            new SfntJstfPriority[priorityCount];
        index = 0;

        while (index < priorityCount)
        {
            int priorityOffset = ResolveRequiredOffset(
                offset,
                offsets[index],
                headerLength,
                PriorityLength,
                "A JSTF priority offset is invalid.");
            priorities[index] = ParsePriority(priorityOffset);
            index += 1;
        }

        return new SfntJstfLanguageSystem(languageTag, priorities);
    }

    SfntJstfPriority ParsePriority(int offset)
    {
        BigEndianReader reader = CreateReader(
            offset,
            PriorityLength,
            "A JSTF priority table is truncated.");
        int gsubShrinkEnable = reader.ReadUInt16();
        int gsubShrinkDisable = reader.ReadUInt16();
        int gposShrinkEnable = reader.ReadUInt16();
        int gposShrinkDisable = reader.ReadUInt16();
        int shrinkMaximum = reader.ReadUInt16();
        int gsubExtendEnable = reader.ReadUInt16();
        int gsubExtendDisable = reader.ReadUInt16();
        int gposExtendEnable = reader.ReadUInt16();
        int gposExtendDisable = reader.ReadUInt16();
        int extendMaximum = reader.ReadUInt16();
        SfntJstfOpportunity shrinkage = new SfntJstfOpportunity(
            true,
            ParseOptionalLookupList(
                offset,
                gsubShrinkEnable,
                substitutionLookupCount),
            ParseOptionalLookupList(
                offset,
                gsubShrinkDisable,
                substitutionLookupCount),
            ParseOptionalLookupList(
                offset,
                gposShrinkEnable,
                positioningLookupCount),
            ParseOptionalLookupList(
                offset,
                gposShrinkDisable,
                positioningLookupCount),
            ParseOptionalMaximum(offset, shrinkMaximum));
        SfntJstfOpportunity extension = new SfntJstfOpportunity(
            false,
            ParseOptionalLookupList(
                offset,
                gsubExtendEnable,
                substitutionLookupCount),
            ParseOptionalLookupList(
                offset,
                gsubExtendDisable,
                substitutionLookupCount),
            ParseOptionalLookupList(
                offset,
                gposExtendEnable,
                positioningLookupCount),
            ParseOptionalLookupList(
                offset,
                gposExtendDisable,
                positioningLookupCount),
            ParseOptionalMaximum(offset, extendMaximum));
        return new SfntJstfPriority(shrinkage, extension);
    }

    SfntJstfLookupList? ParseOptionalLookupList(
        int priorityOffset,
        int relativeOffset,
        int availableLookupCount)
    {
        if (relativeOffset == 0)
        {
            return null;
        }

        int offset = ResolveOffset(
            priorityOffset,
            relativeOffset,
            PriorityLength,
            2,
            "A JSTF modification-list offset is invalid.");
        BigEndianReader reader = CreateReader(
            offset,
            2,
            "A JSTF modification list is truncated.");
        int count = reader.ReadUInt16();
        AddEntries(count);
        int length = CheckedFontMath.GetRangeEnd(2, count, 2);
        reader = CreateReader(
            offset,
            length,
            "A JSTF modification-list index array is truncated.");
        reader.ReadUInt16();
        int[] lookupIndices = new int[count];
        int previousIndex = -1;
        int index = 0;

        while (index < count)
        {
            int lookupIndex = reader.ReadUInt16();

            if (lookupIndex >= availableLookupCount)
            {
                throw FontFormatException.Create(
                    "A JSTF modification list references an invalid lookup.");
            }

            if (lookupIndex <= previousIndex)
            {
                throw FontFormatException.Create(
                    "JSTF modification-list indices are not strictly ordered.");
            }

            lookupIndices[index] = lookupIndex;
            previousIndex = lookupIndex;
            index += 1;
        }

        return new SfntJstfLookupList(lookupIndices);
    }

    SfntJstfMaximum? ParseOptionalMaximum(
        int priorityOffset,
        int relativeOffset)
    {
        if (relativeOffset == 0)
        {
            return null;
        }

        int offset = ResolveOffset(
            priorityOffset,
            relativeOffset,
            PriorityLength,
            2,
            "A JSTF maximum offset is invalid.");
        SfntLayoutLookupList lookupList =
            new SfntLayoutParserContext(tableData, limits)
                .ParseLookupList(offset);
        int lookupCount = lookupList.GetLookupCount();

        if (lookupCount > limits.GetMaxLookupsPerTable())
        {
            throw FontLimitException.Create(
                "A JSTF maximum exceeds the configured lookup limit.");
        }

        AddEntries(lookupCount);
        SfntLayoutTable layoutTable = new SfntLayoutTable(
            1,
            0,
            0,
            new SfntLayoutScriptList(new SfntLayoutScript[0]),
            new SfntLayoutFeatureList(new SfntLayoutFeature[0]),
            lookupList,
            tableData);
        SfntGposTable positioning = new SfntGposParserContext(
            tableData,
            glyphCount,
            limits,
            variationStore).Parse(layoutTable);
        int index = 0;

        while (index < positioning.GetLookupCount())
        {
            int lookupType = positioning.GetLookup(index).GetLookupType();

            if (lookupType == 7 || lookupType == 8)
            {
                throw FontFormatException.Create(
                    "A JSTF maximum contains a contextual positioning lookup.");
            }

            index += 1;
        }

        return new SfntJstfMaximum(positioning);
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
        int baseOffset,
        int relativeOffset,
        int minimumRelativeOffset,
        int requiredLength,
        string message)
    {
        if (relativeOffset == 0)
        {
            throw FontFormatException.Create(message);
        }

        return ResolveOffset(
            baseOffset,
            relativeOffset,
            minimumRelativeOffset,
            requiredLength,
            message);
    }

    int ResolveOffset(
        int baseOffset,
        int relativeOffset,
        int minimumRelativeOffset,
        int requiredLength,
        string message)
    {
        if (relativeOffset < minimumRelativeOffset)
        {
            throw FontFormatException.Create(message);
        }

        int offset = CheckedFontMath.AddOffset(
            baseOffset,
            relativeOffset);

        if (offset > tableData.GetLength() - requiredLength)
        {
            throw FontFormatException.Create(message);
        }

        return offset;
    }

    void AddEntries(int count)
    {
        if (count > limits.GetMaxCoverageEntries() - parsedEntries)
        {
            throw FontLimitException.Create(
                "The JSTF table exceeds the configured coverage-entry limit.");
        }

        parsedEntries += count;
    }

    static void ValidateTag(int tag, string message)
    {
        uint value = CheckedFontMath.ReinterpretInt32AsUInt32(tag);
        int shift = 24;

        while (shift >= 0)
        {
            int character = (int)((value >> shift) & 0xFFU);

            if (character < 0x20 || character > 0x7E)
            {
                throw FontFormatException.Create(message);
            }

            shift -= 8;
        }
    }
}

internal static class SfntJstfTableParser
{
    static readonly SfntTableParser parser = new SfntTableParser(
        FontTags.Jstf,
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

    internal static SfntJstfTable? FindTable(SfntTableCache cache)
    {
        SfntParsedTableView? view = FindView(cache);

        if (view == null || !view.HasParsedData())
        {
            return null;
        }

        return (SfntJstfTable)view.GetParsedData();
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
        SfntMaxpTable? maxp = SfntMaxpTableParser.FindTable(cache);

        if (maxp == null)
        {
            throw FontFormatException.Create(
                "The JSTF table requires a supported maxp table.");
        }

        SfntGsubTable? gsub = SfntGsubTableParser.FindTable(cache);
        SfntGposTable? gpos = SfntGposTableParser.FindTable(cache);
        SfntGdefTable? gdef = SfntGdefTableParser.FindTable(cache);
        int substitutionLookupCount = gsub == null ? 0 :
            gsub.GetLookupCount();
        int positioningLookupCount = gpos == null ? 0 :
            gpos.GetLookupCount();
        SfntItemVariationStore? variationStore = gdef == null ? null :
            gdef.GetItemVariationStore();
        return new SfntJstfParserContext(
            table.GetDataSlice(),
            limits,
            maxp.GetGlyphCount(),
            substitutionLookupCount,
            positioningLookupCount,
            variationStore).Parse();
    }
}
