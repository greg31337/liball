namespace ttf;

internal class SfntLayoutParserContext
{
    FontDataSlice tableData;
    FontLimits limits;
    int inspectedEntries;

    internal SfntLayoutParserContext(
        FontDataSlice tableData,
        FontLimits limits)
    {
        this.tableData = tableData;
        this.limits = limits;
        inspectedEntries = 0;
    }

    internal SfntLayoutTable Parse()
    {
        BigEndianReader reader = CreateReader(
            0,
            10,
            "A layout table header is truncated.");
        int majorVersion = reader.ReadUInt16();
        int minorVersion = reader.ReadUInt16();

        if (majorVersion != 1 || (minorVersion != 0 && minorVersion != 1))
        {
            throw FontFormatException.Create(
                "The layout table version is not supported.");
        }

        int headerLength = minorVersion == 1 ? 14 : 10;
        reader = CreateReader(
            0,
            headerLength,
            "A layout table header is truncated.");
        reader.ReadUInt16();
        reader.ReadUInt16();
        int scriptListOffset = reader.ReadUInt16();
        int featureListOffset = reader.ReadUInt16();
        int lookupListOffset = reader.ReadUInt16();
        int featureVariationsOffset = 0;

        if (minorVersion == 1)
        {
            featureVariationsOffset = ReadUInt32Offset(reader);

            if (featureVariationsOffset != 0)
            {
                ResolveOffset(
                    0,
                    featureVariationsOffset,
                    headerLength,
                    8,
                    "A layout FeatureVariations offset is invalid.");
            }
        }

        int scriptListTableOffset = ResolveRequiredOffset(
            0,
            scriptListOffset,
            headerLength,
            2,
            "A layout ScriptList offset is invalid.");
        int featureListTableOffset = ResolveRequiredOffset(
            0,
            featureListOffset,
            headerLength,
            2,
            "A layout FeatureList offset is invalid.");
        int lookupListTableOffset = ResolveRequiredOffset(
            0,
            lookupListOffset,
            headerLength,
            2,
            "A layout LookupList offset is invalid.");
        SfntLayoutScriptList scriptList = ParseScriptList(
            scriptListTableOffset);
        SfntLayoutFeatureList featureList = ParseFeatureList(
            featureListTableOffset);
        SfntLayoutLookupList lookupList = ParseLookupList(
            lookupListTableOffset);
        ValidateFeatureReferences(scriptList, featureList.GetFeatureCount());
        ValidateLookupReferences(featureList, lookupList.GetLookupCount());
        return new SfntLayoutTable(
            majorVersion,
            minorVersion,
            featureVariationsOffset,
            scriptList,
            featureList,
            lookupList,
            tableData);
    }

    SfntLayoutScriptList ParseScriptList(int offset)
    {
        const int RecordLength = 6;
        BigEndianReader reader = CreateReader(
            offset,
            2,
            "A layout ScriptList header is truncated.");
        int count = reader.ReadUInt16();
        AddEntries(count);
        int headerLength = CheckedFontMath.GetRangeEnd(
            2,
            count,
            RecordLength);
        reader = CreateReader(
            offset,
            headerLength,
            "A layout ScriptRecord array is truncated.");
        reader.ReadUInt16();
        int[] tags = new int[count];
        int[] offsets = new int[count];
        uint previousTag = 0;
        int index = 0;

        while (index < count)
        {
            int tag = reader.ReadTag();
            ValidateTag(tag, "A layout script tag is invalid.");

            uint encodedTag = CheckedFontMath.ReinterpretInt32AsUInt32(tag);

            if (index > 0 && encodedTag <= previousTag)
            {
                throw FontFormatException.Create(
                    "Layout ScriptRecords are not strictly ordered.");
            }

            tags[index] = tag;
            offsets[index] = reader.ReadUInt16();
            previousTag = encodedTag;
            index += 1;
        }

        SfntLayoutScript[] scripts = new SfntLayoutScript[count];
        index = 0;

        while (index < count)
        {
            int scriptOffset = ResolveRequiredOffset(
                offset,
                offsets[index],
                headerLength,
                4,
                "A layout Script offset is invalid.");
            scripts[index] = ParseScript(scriptOffset, tags[index]);
            index += 1;
        }

        return new SfntLayoutScriptList(scripts);
    }

    SfntLayoutScript ParseScript(int offset, int scriptTag)
    {
        const int RecordLength = 6;
        BigEndianReader reader = CreateReader(
            offset,
            4,
            "A layout Script header is truncated.");
        int defaultLanguageOffset = reader.ReadUInt16();
        int languageCount = reader.ReadUInt16();
        AddEntries(languageCount);
        int headerLength = CheckedFontMath.GetRangeEnd(
            4,
            languageCount,
            RecordLength);
        reader = CreateReader(
            offset,
            headerLength,
            "A layout LangSysRecord array is truncated.");
        reader.ReadUInt16();
        reader.ReadUInt16();
        int[] languageTags = new int[languageCount];
        int[] languageOffsets = new int[languageCount];
        uint previousTag = 0;
        int index = 0;

        while (index < languageCount)
        {
            int tag = reader.ReadTag();
            ValidateTag(tag, "A layout language-system tag is invalid.");

            uint encodedTag = CheckedFontMath.ReinterpretInt32AsUInt32(tag);

            if (index > 0 && encodedTag <= previousTag)
            {
                throw FontFormatException.Create(
                    "Layout LangSysRecords are not strictly ordered.");
            }

            languageTags[index] = tag;
            languageOffsets[index] = reader.ReadUInt16();
            previousTag = encodedTag;
            index += 1;
        }

        SfntLayoutLanguageSystem? defaultLanguage = null;

        if (defaultLanguageOffset != 0)
        {
            int defaultOffset = ResolveOffset(
                offset,
                defaultLanguageOffset,
                headerLength,
                6,
                "A layout default LangSys offset is invalid.");
            defaultLanguage = ParseLanguageSystem(defaultOffset, 0);
        }

        SfntLayoutLanguageSystem[] languages =
            new SfntLayoutLanguageSystem[languageCount];
        index = 0;

        while (index < languageCount)
        {
            int languageOffset = ResolveRequiredOffset(
                offset,
                languageOffsets[index],
                headerLength,
                6,
                "A layout LangSys offset is invalid.");
            languages[index] = ParseLanguageSystem(
                languageOffset,
                languageTags[index]);
            index += 1;
        }

        if (scriptTag == 0x44464C54 &&
            (defaultLanguage == null || languages.Length != 0))
        {
            throw FontFormatException.Create(
                "The DFLT layout script must contain only a default LangSys.");
        }

        return new SfntLayoutScript(
            scriptTag,
            defaultLanguage,
            languages);
    }

    SfntLayoutLanguageSystem ParseLanguageSystem(
        int offset,
        int languageTag)
    {
        BigEndianReader reader = CreateReader(
            offset,
            6,
            "A layout LangSys table is truncated.");
        int lookupOrderOffset = reader.ReadUInt16();
        int requiredFeatureIndex = reader.ReadUInt16();
        int featureCount = reader.ReadUInt16();
        AddEntries(featureCount);
        int length = CheckedFontMath.GetRangeEnd(6, featureCount, 2);
        reader = CreateReader(
            offset,
            length,
            "A layout LangSys feature-index array is truncated.");
        reader.ReadUInt16();
        reader.ReadUInt16();
        reader.ReadUInt16();

        if (lookupOrderOffset != 0)
        {
            throw FontFormatException.Create(
                "A layout LangSys LookupOrder offset is not zero.");
        }

        int[] featureIndices = new int[featureCount];
        int index = 0;

        while (index < featureCount)
        {
            featureIndices[index] = reader.ReadUInt16();
            index += 1;
        }

        return new SfntLayoutLanguageSystem(
            languageTag,
            requiredFeatureIndex,
            featureIndices);
    }

    SfntLayoutFeatureList ParseFeatureList(int offset)
    {
        const int RecordLength = 6;
        BigEndianReader reader = CreateReader(
            offset,
            2,
            "A layout FeatureList header is truncated.");
        int count = reader.ReadUInt16();
        AddEntries(count);
        int headerLength = CheckedFontMath.GetRangeEnd(
            2,
            count,
            RecordLength);
        reader = CreateReader(
            offset,
            headerLength,
            "A layout FeatureRecord array is truncated.");
        reader.ReadUInt16();
        int[] tags = new int[count];
        int[] offsets = new int[count];
        uint previousTag = 0;
        int index = 0;

        while (index < count)
        {
            int tag = reader.ReadTag();
            ValidateTag(tag, "A layout feature tag is invalid.");

            uint encodedTag = CheckedFontMath.ReinterpretInt32AsUInt32(tag);

            if (index > 0 && encodedTag < previousTag)
            {
                throw FontFormatException.Create(
                    "Layout FeatureRecords are not ordered by tag.");
            }

            tags[index] = tag;
            offsets[index] = reader.ReadUInt16();
            previousTag = encodedTag;
            index += 1;
        }

        SfntLayoutFeature[] features = new SfntLayoutFeature[count];
        index = 0;

        while (index < count)
        {
            int featureOffset = ResolveRequiredOffset(
                offset,
                offsets[index],
                headerLength,
                4,
                "A layout Feature offset is invalid.");
            features[index] = ParseFeature(featureOffset, tags[index]);
            index += 1;
        }

        return new SfntLayoutFeatureList(features);
    }

    SfntLayoutFeature ParseFeature(int offset, int featureTag)
    {
        BigEndianReader reader = CreateReader(
            offset,
            4,
            "A layout Feature table is truncated.");
        int featureParamsOffset = reader.ReadUInt16();
        int lookupCount = reader.ReadUInt16();
        AddEntries(lookupCount);
        int headerLength = CheckedFontMath.GetRangeEnd(4, lookupCount, 2);
        reader = CreateReader(
            offset,
            headerLength,
            "A layout Feature lookup-index array is truncated.");
        reader.ReadUInt16();
        reader.ReadUInt16();
        int[] lookupIndices = new int[lookupCount];
        int index = 0;

        while (index < lookupCount)
        {
            lookupIndices[index] = reader.ReadUInt16();
            index += 1;
        }

        int featureParamsTableOffset = 0;

        if (featureParamsOffset != 0)
        {
            featureParamsTableOffset = ResolveOffset(
                offset,
                featureParamsOffset,
                headerLength,
                2,
                "A layout FeatureParams offset is invalid.");
        }

        return new SfntLayoutFeature(
            featureTag,
            featureParamsOffset,
            featureParamsTableOffset,
            lookupIndices);
    }

    internal SfntLayoutLookupList ParseLookupList(int offset)
    {
        BigEndianReader reader = CreateReader(
            offset,
            2,
            "A layout LookupList header is truncated.");
        int count = reader.ReadUInt16();

        if (count > limits.GetMaxLookupsPerTable())
        {
            throw FontLimitException.Create(
                "A layout table exceeds the configured lookup limit.");
        }

        AddEntries(count);
        int headerLength = CheckedFontMath.GetRangeEnd(2, count, 2);
        reader = CreateReader(
            offset,
            headerLength,
            "A layout Lookup offset array is truncated.");
        reader.ReadUInt16();
        int[] offsets = new int[count];
        int index = 0;

        while (index < count)
        {
            offsets[index] = reader.ReadUInt16();
            index += 1;
        }

        SfntLayoutLookup[] lookups = new SfntLayoutLookup[count];
        index = 0;

        while (index < count)
        {
            int lookupOffset = ResolveRequiredOffset(
                offset,
                offsets[index],
                headerLength,
                6,
                "A layout Lookup offset is invalid.");
            lookups[index] = ParseLookup(lookupOffset);
            index += 1;
        }

        return new SfntLayoutLookupList(lookups);
    }

    SfntLayoutLookup ParseLookup(int offset)
    {
        BigEndianReader reader = CreateReader(
            offset,
            6,
            "A layout Lookup table is truncated.");
        int lookupType = reader.ReadUInt16();
        int lookupFlag = reader.ReadUInt16();
        int subtableCount = reader.ReadUInt16();

        if (lookupType == 0)
        {
            throw FontFormatException.Create(
                "A layout Lookup type is zero.");
        }

        if ((lookupFlag & 0x00E0) != 0)
        {
            throw FontFormatException.Create(
                "A layout Lookup has nonzero reserved flag bits.");
        }

        if (subtableCount == 0)
        {
            throw FontFormatException.Create(
                "A layout Lookup has no subtables.");
        }

        AddEntries(subtableCount);
        bool usesMarkFilteringSet = (lookupFlag & 0x0010) != 0;
        int headerLength = CheckedFontMath.GetRangeEnd(
            6,
            subtableCount,
            2);

        if (usesMarkFilteringSet)
        {
            headerLength = CheckedFontMath.AddOffset(headerLength, 2);
        }

        reader = CreateReader(
            offset,
            headerLength,
            "A layout Lookup subtable-offset array is truncated.");
        reader.ReadUInt16();
        reader.ReadUInt16();
        reader.ReadUInt16();
        int[] subtableOffsets = new int[subtableCount];
        int[] subtableTableOffsets = new int[subtableCount];
        int index = 0;

        while (index < subtableCount)
        {
            subtableOffsets[index] = reader.ReadUInt16();
            subtableTableOffsets[index] = ResolveRequiredOffset(
                offset,
                subtableOffsets[index],
                headerLength,
                2,
                "A layout Lookup subtable offset is invalid.");
            index += 1;
        }

        int markFilteringSet = usesMarkFilteringSet ?
            reader.ReadUInt16() : 0;
        return new SfntLayoutLookup(
            lookupType,
            lookupFlag,
            subtableOffsets,
            subtableTableOffsets,
            markFilteringSet);
    }

    void ValidateFeatureReferences(
        SfntLayoutScriptList scriptList,
        int featureCount)
    {
        int scriptIndex = 0;

        while (scriptIndex < scriptList.GetScriptCount())
        {
            SfntLayoutScript script = scriptList.GetScript(scriptIndex);
            ValidateLanguageFeatureReferences(
                script.GetDefaultLanguageSystem(),
                featureCount);
            int languageIndex = 0;

            while (languageIndex < script.GetLanguageSystemCount())
            {
                ValidateLanguageFeatureReferences(
                    script.GetLanguageSystem(languageIndex),
                    featureCount);
                languageIndex += 1;
            }

            scriptIndex += 1;
        }
    }

    static void ValidateLanguageFeatureReferences(
        SfntLayoutLanguageSystem? language,
        int featureCount)
    {
        if (language == null)
        {
            return;
        }

        if (language.HasRequiredFeature() &&
            language.GetRequiredFeatureIndex() >= featureCount)
        {
            throw FontFormatException.Create(
                "A layout LangSys required feature index is invalid.");
        }

        int index = 0;

        while (index < language.GetFeatureIndexCount())
        {
            if (language.GetFeatureIndex(index) >= featureCount)
            {
                throw FontFormatException.Create(
                    "A layout LangSys feature index is invalid.");
            }

            index += 1;
        }
    }

    static void ValidateLookupReferences(
        SfntLayoutFeatureList featureList,
        int lookupCount)
    {
        int featureIndex = 0;

        while (featureIndex < featureList.GetFeatureCount())
        {
            SfntLayoutFeature feature =
                featureList.GetFeature(featureIndex);
            int lookupIndex = 0;

            while (lookupIndex < feature.GetLookupIndexCount())
            {
                if (feature.GetLookupIndex(lookupIndex) >= lookupCount)
                {
                    throw FontFormatException.Create(
                        "A layout Feature lookup index is invalid.");
                }

                lookupIndex += 1;
            }

            featureIndex += 1;
        }
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

        int absoluteOffset = CheckedFontMath.AddOffset(
            baseOffset,
            relativeOffset);

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
                "A layout table exceeds the configured entry limit.");
        }

        inspectedEntries += count;
    }

    static int ReadUInt32Offset(BigEndianReader reader)
    {
        uint value = reader.ReadUInt32();

        if (value > int.MaxValue)
        {
            throw FontFormatException.Create(
                "A layout Offset32 value exceeds the supported range.");
        }

        return (int)value;
    }

    static void ValidateTag(int tag, string message)
    {
        uint value = CheckedFontMath.ReinterpretInt32AsUInt32(tag);
        int shift = 24;

        while (shift >= 0)
        {
            int character = (int)(value >> shift) & 0xFF;

            if (character < 0x20 || character > 0x7E)
            {
                throw FontFormatException.Create(message);
            }

            shift -= 8;
        }
    }
}

internal static class SfntLayoutTableParser
{
    internal static SfntLayoutTable Parse(
        FontDataSlice tableData,
        FontLimits limits)
    {
        if (tableData == null)
        {
            throw new System.ArgumentNullException("tableData");
        }

        if (limits == null)
        {
            throw new System.ArgumentNullException("limits");
        }

        return new SfntLayoutParserContext(tableData, limits).Parse();
    }
}
