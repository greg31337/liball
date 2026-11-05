namespace ttf;

internal class SfntBaseParserContext
{
    FontDataSlice tableData;
    FontLimits limits;
    int glyphCount;
    int inspectedEntries;

    internal SfntBaseParserContext(
        FontDataSlice tableData,
        FontLimits limits,
        int glyphCount)
    {
        this.tableData = tableData;
        this.limits = limits;
        this.glyphCount = glyphCount;
        inspectedEntries = 0;
    }

    internal SfntBaseAxis ParseAxis(int offset, bool horizontal)
    {
        const int HeaderLength = 4;
        BigEndianReader reader = CreateReader(offset, HeaderLength,
            "A BASE Axis table is truncated.");
        int tagListOffset = reader.ReadUInt16();
        int scriptListOffset = reader.ReadUInt16();
        int[] baselineTags = tagListOffset == 0 ?
            new int[0] :
            ParseTagList(ResolveOffset(
                offset,
                tagListOffset,
                HeaderLength,
                2,
                "A BASE tag-list offset is invalid."));

        if (scriptListOffset == 0)
        {
            throw FontFormatException.Create("A BASE Axis table has no script list.");
        }

        int scriptListAbsoluteOffset = ResolveOffset(
            offset,
            scriptListOffset,
            HeaderLength,
            2,
            "A BASE script-list offset is invalid.");
        SfntBaseScript[] scripts = ParseScriptList(
            scriptListAbsoluteOffset,
            baselineTags.Length);
        return new SfntBaseAxis(horizontal, baselineTags, scripts);
    }

    int[] ParseTagList(int offset)
    {
        BigEndianReader reader = CreateReader(offset, 2,
            "The BASE tag-list header is truncated.");
        int count = reader.ReadUInt16();
        AddEntries(count);
        int byteLength = CheckedFontMath.GetRangeEnd(2, count, 4);
        reader = CreateReader(offset, byteLength,
            "The BASE baseline-tag array is truncated.");
        reader.ReadUInt16();
        int[] tags = new int[count];
        uint previous = 0;
        int index = 0;

        while (index < count)
        {
            int tag = reader.ReadTag();
            ValidateTag(tag, "A BASE baseline tag is invalid.");

            uint encodedTag = CheckedFontMath.ReinterpretInt32AsUInt32(tag);

            if (index > 0 && encodedTag <= previous)
            {
                throw FontFormatException.Create(
                    "The BASE baseline tags are not strictly ordered.");
            }

            tags[index] = tag;
            previous = encodedTag;
            index += 1;
        }

        return tags;
    }

    SfntBaseScript[] ParseScriptList(int offset, int baselineTagCount)
    {
        const int RecordLength = 6;
        BigEndianReader reader = CreateReader(offset, 2,
            "The BASE script-list header is truncated.");
        int count = reader.ReadUInt16();
        AddEntries(count);
        int headerLength = CheckedFontMath.GetRangeEnd(2, count, RecordLength);
        reader = CreateReader(offset, headerLength,
            "The BASE script-record array is truncated.");
        reader.ReadUInt16();
        int[] tags = new int[count];
        int[] offsets = new int[count];
        uint previous = 0;
        int index = 0;

        while (index < count)
        {
            int tag = reader.ReadTag();
            ValidateTag(tag, "A BASE script tag is invalid.");

            uint encodedTag = CheckedFontMath.ReinterpretInt32AsUInt32(tag);

            if (index > 0 && encodedTag <= previous)
            {
                throw FontFormatException.Create(
                    "The BASE script records are not strictly ordered.");
            }

            tags[index] = tag;
            offsets[index] = reader.ReadUInt16();

            if (offsets[index] == 0)
            {
                throw FontFormatException.Create(
                    "A BASE script record has a null script offset.");
            }

            previous = encodedTag;
            index += 1;
        }

        SfntBaseScript[] scripts = new SfntBaseScript[count];
        index = 0;

        while (index < count)
        {
            int scriptOffset = ResolveOffset(
                offset,
                offsets[index],
                headerLength,
                6,
                "A BASE script offset is invalid.");
            scripts[index] = ParseScript(
                scriptOffset,
                tags[index],
                baselineTagCount);
            index += 1;
        }

        return scripts;
    }

    SfntBaseScript ParseScript(
        int offset,
        int scriptTag,
        int baselineTagCount)
    {
        const int RecordLength = 6;
        BigEndianReader reader = CreateReader(offset, 6,
            "A BASE script header is truncated.");
        int valuesOffset = reader.ReadUInt16();
        int defaultMinMaxOffset = reader.ReadUInt16();
        int languageCount = reader.ReadUInt16();
        AddEntries(languageCount);
        int headerLength = CheckedFontMath.GetRangeEnd(
            6,
            languageCount,
            RecordLength);
        reader = CreateReader(offset, headerLength,
            "A BASE language-system record array is truncated.");
        reader.ReadUInt16();
        reader.ReadUInt16();
        reader.ReadUInt16();
        int[] languageTags = new int[languageCount];
        int[] minMaxOffsets = new int[languageCount];
        uint previous = 0;
        int index = 0;

        while (index < languageCount)
        {
            int tag = reader.ReadTag();
            ValidateTag(tag, "A BASE language-system tag is invalid.");

            uint encodedTag = CheckedFontMath.ReinterpretInt32AsUInt32(tag);

            if (index > 0 && encodedTag <= previous)
            {
                throw FontFormatException.Create(
                    "The BASE language-system records are not strictly ordered.");
            }

            languageTags[index] = tag;
            minMaxOffsets[index] = reader.ReadUInt16();

            if (minMaxOffsets[index] == 0)
            {
                throw FontFormatException.Create(
                    "A BASE language-system record has a null MinMax offset.");
            }

            previous = encodedTag;
            index += 1;
        }

        if (baselineTagCount == 0 && valuesOffset != 0)
        {
            throw FontFormatException.Create(
                "A BASE script's values do not match its baseline tag list.");
        }

        SfntBaseValues? values = valuesOffset == 0 ?
            null :
            ParseValues(
                ResolveOffset(
                    offset,
                    valuesOffset,
                    headerLength,
                    4,
                    "A BASE values offset is invalid."),
                baselineTagCount);
        SfntBaseMinMax? defaultMinMax = defaultMinMaxOffset == 0 ?
            null :
            ParseMinMax(ResolveOffset(
                offset,
                defaultMinMaxOffset,
                headerLength,
                6,
                "A BASE default MinMax offset is invalid."));
        SfntBaseLanguageSystem[] languages =
            new SfntBaseLanguageSystem[languageCount];
        index = 0;

        while (index < languageCount)
        {
            int minMaxOffset = ResolveOffset(
                offset,
                minMaxOffsets[index],
                headerLength,
                6,
                "A BASE language MinMax offset is invalid.");
            languages[index] = new SfntBaseLanguageSystem(
                languageTags[index],
                ParseMinMax(minMaxOffset));
            index += 1;
        }

        return new SfntBaseScript(
            scriptTag,
            values,
            defaultMinMax,
            languages);
    }

    SfntBaseValues ParseValues(int offset, int baselineTagCount)
    {
        BigEndianReader reader = CreateReader(offset, 4,
            "A BASE values header is truncated.");
        int defaultBaselineIndex = reader.ReadUInt16();
        int count = reader.ReadUInt16();
        AddEntries(count);
        int headerLength = CheckedFontMath.GetRangeEnd(4, count, 2);

        if (count != baselineTagCount)
        {
            throw FontFormatException.Create(
                "A BASE coordinate count does not match the baseline tag count.");
        }

        if (count == 0 || defaultBaselineIndex >= count)
        {
            throw FontFormatException.Create(
                "A BASE default baseline index is invalid.");
        }

        reader = CreateReader(offset, headerLength,
            "A BASE coordinate-offset array is truncated.");
        reader.ReadUInt16();
        reader.ReadUInt16();
        int[] offsets = new int[count];
        int index = 0;

        while (index < count)
        {
            offsets[index] = reader.ReadUInt16();

            if (offsets[index] == 0)
            {
                throw FontFormatException.Create(
                    "A BASE coordinate offset is null.");
            }

            index += 1;
        }

        SfntBaseCoordinate[] coordinates = new SfntBaseCoordinate[count];
        index = 0;

        while (index < count)
        {
            int coordinateOffset = ResolveOffset(
                offset,
                offsets[index],
                headerLength,
                4,
                "A BASE coordinate offset is invalid.");
            coordinates[index] = ParseCoordinate(coordinateOffset);
            index += 1;
        }

        return new SfntBaseValues(defaultBaselineIndex, coordinates);
    }

    SfntBaseMinMax ParseMinMax(int offset)
    {
        const int RecordLength = 8;
        BigEndianReader reader = CreateReader(offset, 6,
            "A BASE MinMax header is truncated.");
        int minimumOffset = reader.ReadUInt16();
        int maximumOffset = reader.ReadUInt16();
        int featureCount = reader.ReadUInt16();
        AddEntries(featureCount);
        int headerLength = CheckedFontMath.GetRangeEnd(
            6,
            featureCount,
            RecordLength);
        reader = CreateReader(offset, headerLength,
            "A BASE feature MinMax record array is truncated.");
        reader.ReadUInt16();
        reader.ReadUInt16();
        reader.ReadUInt16();
        int[] tags = new int[featureCount];
        int[] minimumOffsets = new int[featureCount];
        int[] maximumOffsets = new int[featureCount];
        uint previous = 0;
        int index = 0;

        while (index < featureCount)
        {
            int tag = reader.ReadTag();
            ValidateTag(tag, "A BASE feature tag is invalid.");

            uint encodedTag = CheckedFontMath.ReinterpretInt32AsUInt32(tag);

            if (index > 0 && encodedTag <= previous)
            {
                throw FontFormatException.Create(
                    "The BASE feature MinMax records are not strictly ordered.");
            }

            tags[index] = tag;
            minimumOffsets[index] = reader.ReadUInt16();
            maximumOffsets[index] = reader.ReadUInt16();
            previous = encodedTag;
            index += 1;
        }

        SfntBaseCoordinate? minimum = ParseOptionalCoordinate(
            offset,
            minimumOffset,
            headerLength,
            "A BASE minimum-coordinate offset is invalid.");
        SfntBaseCoordinate? maximum = ParseOptionalCoordinate(
            offset,
            maximumOffset,
            headerLength,
            "A BASE maximum-coordinate offset is invalid.");
        SfntBaseFeatureMinMax[] features =
            new SfntBaseFeatureMinMax[featureCount];
        index = 0;

        while (index < featureCount)
        {
            features[index] = new SfntBaseFeatureMinMax(
                tags[index],
                ParseOptionalCoordinate(
                    offset,
                    minimumOffsets[index],
                    headerLength,
                    "A BASE feature minimum-coordinate offset is invalid."),
                ParseOptionalCoordinate(
                    offset,
                    maximumOffsets[index],
                    headerLength,
                    "A BASE feature maximum-coordinate offset is invalid."));
            index += 1;
        }

        return new SfntBaseMinMax(minimum, maximum, features);
    }

    SfntBaseCoordinate? ParseOptionalCoordinate(
        int baseOffset,
        int relativeOffset,
        int minimumRelativeOffset,
        string message)
    {
        if (relativeOffset == 0)
        {
            return null;
        }

        return ParseCoordinate(ResolveOffset(
            baseOffset,
            relativeOffset,
            minimumRelativeOffset,
            4,
            message));
    }

    SfntBaseCoordinate ParseCoordinate(int offset)
    {
        BigEndianReader reader = CreateReader(offset, 4,
            "A BASE coordinate table is truncated.");
        int format = reader.ReadUInt16();
        int coordinate = reader.ReadInt16();

        if (format == 1)
        {
            return new SfntBaseCoordinate(
                format,
                coordinate,
                0,
                0,
                0,
                0,
                null);
        }

        if (format == 2)
        {
            reader = CreateReader(offset, 8,
                "A BASE format 2 coordinate table is truncated.");
            reader.ReadUInt16();
            reader.ReadInt16();
            int referenceGlyphId = reader.ReadUInt16();
            int contourPointIndex = reader.ReadUInt16();

            if (referenceGlyphId >= glyphCount)
            {
                throw FontFormatException.Create(
                    "A BASE coordinate references an invalid glyph.");
            }

            return new SfntBaseCoordinate(
                format,
                coordinate,
                referenceGlyphId,
                contourPointIndex,
                0,
                0,
                null);
        }

        if (format == 3)
        {
            reader = CreateReader(offset, 6,
                "A BASE format 3 coordinate table is truncated.");
            reader.ReadUInt16();
            reader.ReadInt16();
            int deviceOffset = reader.ReadUInt16();
            int deviceAbsoluteOffset = 0;
            SfntDeviceOrVariationIndex? adjustment = null;

            if (deviceOffset != 0)
            {
                deviceAbsoluteOffset = ResolveOffset(
                    offset,
                    deviceOffset,
                    6,
                    6,
                    "A BASE Device or VariationIndex offset is invalid.");
                adjustment =
                    SfntLayoutPrimitiveParser.ParseDeviceOrVariationIndex(
                        tableData,
                        deviceAbsoluteOffset,
                        limits);
            }

            return new SfntBaseCoordinate(
                format,
                coordinate,
                0,
                0,
                deviceOffset,
                deviceAbsoluteOffset,
                adjustment);
        }

        throw FontFormatException.Create("A BASE coordinate format is not supported.");
    }

    BigEndianReader CreateReader(int offset, int requiredLength, string message)
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

        int absoluteOffset = CheckedFontMath.AddOffset(baseOffset, relativeOffset);

        if (absoluteOffset > tableData.GetLength() - requiredLength)
        {
            throw FontFormatException.Create(message);
        }

        return absoluteOffset;
    }

    void AddEntries(int count)
    {
        if (count > limits.GetMaxCoverageEntries() - inspectedEntries)
        {
            throw FontLimitException.Create(
                "The BASE table exceeds the configured coverage-entry limit.");
        }

        inspectedEntries += count;
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

internal static class SfntBaseTableParser
{
    const int Version10HeaderLength = 8;
    const int Version11HeaderLength = 12;
    static readonly SfntTableParser parser = new SfntTableParser(
        FontTags.Base,
        1,
        1,
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

    internal static SfntBaseTable? FindTable(SfntTableCache cache)
    {
        SfntParsedTableView? view = FindView(cache);

        if (view == null || !view.HasParsedData())
        {
            return null;
        }

        return (SfntBaseTable)view.GetParsedData();
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
        int headerLength = minorVersion == 0 ?
            Version10HeaderLength :
            Version11HeaderLength;

        if (tableData.GetLength() < headerLength)
        {
            throw FontFormatException.Create("The BASE table header is truncated.");
        }

        if (version.GetMajorVersion() != 1)
        {
            throw FontFormatException.Create("The BASE table version is not supported.");
        }

        SfntMaxpTable? maxp = SfntMaxpTableParser.FindTable(cache);

        if (maxp == null)
        {
            throw FontFormatException.Create(
                "The BASE table requires a supported maxp table.");
        }

        BigEndianReader reader = new BigEndianReader(tableData);
        int majorVersion = reader.ReadUInt16();
        minorVersion = reader.ReadUInt16();
        int horizontalOffset = reader.ReadUInt16();
        int verticalOffset = reader.ReadUInt16();
        long itemVariationStoreOffset = minorVersion == 0 ?
            0 : reader.ReadUInt32();

        if (itemVariationStoreOffset != 0 &&
            (itemVariationStoreOffset < headerLength ||
            itemVariationStoreOffset > tableData.GetLength() - 2L))
        {
            throw FontFormatException.Create(
                "The BASE ItemVariationStore offset is invalid.");
        }

        SfntBaseParserContext context = new SfntBaseParserContext(
            tableData,
            limits,
            maxp.GetGlyphCount());
        SfntBaseAxis? horizontalAxis = horizontalOffset == 0 ?
            null : context.ParseAxis(
                ResolveAxisOffset(
                    tableData,
                    horizontalOffset,
                    headerLength),
                true);
        SfntBaseAxis? verticalAxis = verticalOffset == 0 ?
            null : context.ParseAxis(
                ResolveAxisOffset(
                    tableData,
                    verticalOffset,
                    headerLength),
                false);
        return new SfntBaseTable(
            majorVersion,
            minorVersion,
            itemVariationStoreOffset,
            horizontalAxis,
            verticalAxis);
    }

    static int ResolveAxisOffset(
        FontDataSlice tableData,
        int offset,
        int headerLength)
    {
        if (offset < headerLength || offset > tableData.GetLength() - 4)
        {
            throw FontFormatException.Create("A BASE Axis offset is invalid.");
        }

        return offset;
    }
}
