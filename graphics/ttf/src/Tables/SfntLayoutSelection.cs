namespace ttf;

internal class SfntUserFeature
{
    int featureTag;
    int value;
    int start;
    int end;

    internal SfntUserFeature(
        int featureTag,
        int value,
        int start,
        int end)
    {
        if (value < 0)
        {
            throw new System.ArgumentOutOfRangeException("value");
        }

        if (start < 0 || end < start)
        {
            throw new System.ArgumentOutOfRangeException("start");
        }

        this.featureTag = featureTag;
        this.value = value;
        this.start = start;
        this.end = end;
    }

    internal int GetFeatureTag()
    {
        return featureTag;
    }

    internal int GetValue()
    {
        return value;
    }

    internal int GetStart()
    {
        return start;
    }

    internal int GetEnd()
    {
        return end;
    }
}

internal class SfntLayoutFeatureRange
{
    int start;
    int end;
    int value;

    internal SfntLayoutFeatureRange(int start, int end, int value)
    {
        this.start = start;
        this.end = end;
        this.value = value;
    }

    internal int GetStart()
    {
        return start;
    }

    internal int GetEnd()
    {
        return end;
    }

    internal int GetValue()
    {
        return value;
    }
}

internal class SfntLayoutLookupSelection
{
    int lookupIndex;
    int textLength;
    SfntLayoutFeatureRange[] ranges;

    internal SfntLayoutLookupSelection(
        int lookupIndex,
        int textLength,
        SfntLayoutFeatureRange[] ranges)
    {
        this.lookupIndex = lookupIndex;
        this.textLength = textLength;
        this.ranges = ranges;
    }

    internal int GetLookupIndex()
    {
        return lookupIndex;
    }

    internal int GetRangeCount()
    {
        return ranges.Length;
    }

    internal int GetTextLength()
    {
        return textLength;
    }

    internal SfntLayoutFeatureRange GetRange(int index)
    {
        return ranges[index];
    }

    internal int GetValueAt(int textIndex)
    {
        if (textIndex < 0 || textIndex >= textLength)
        {
            throw new System.ArgumentOutOfRangeException("textIndex");
        }

        int low = 0;
        int high = ranges.Length - 1;

        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            SfntLayoutFeatureRange range = ranges[middle];

            if (textIndex < range.GetStart())
            {
                high = middle - 1;
            }
            else if (textIndex >= range.GetEnd())
            {
                low = middle + 1;
            }
            else
            {
                return range.GetValue();
            }
        }

        return 0;
    }
}

internal class SfntLayoutSelection
{
    SfntLayoutFeatureList featureList;
    SfntLayoutScript? script;
    SfntLayoutLanguageSystem? languageSystem;
    bool usedDefaultScript;
    bool usedDefaultLanguageSystem;
    bool[] availableFeatures;
    SfntLayoutLookupSelection[] lookups;

    internal SfntLayoutSelection(
        SfntLayoutFeatureList featureList,
        SfntLayoutScript? script,
        SfntLayoutLanguageSystem? languageSystem,
        bool usedDefaultScript,
        bool usedDefaultLanguageSystem,
        bool[] availableFeatures,
        SfntLayoutLookupSelection[] lookups)
    {
        this.featureList = featureList;
        this.script = script;
        this.languageSystem = languageSystem;
        this.usedDefaultScript = usedDefaultScript;
        this.usedDefaultLanguageSystem = usedDefaultLanguageSystem;
        this.availableFeatures = availableFeatures;
        this.lookups = lookups;
    }

    internal bool HasScript()
    {
        return script != null;
    }

    internal SfntLayoutScript? GetScript()
    {
        return script;
    }

    internal bool HasLanguageSystem()
    {
        return languageSystem != null;
    }

    internal SfntLayoutLanguageSystem? GetLanguageSystem()
    {
        return languageSystem;
    }

    internal bool UsedDefaultScript()
    {
        return usedDefaultScript;
    }

    internal bool UsedDefaultLanguageSystem()
    {
        return usedDefaultLanguageSystem;
    }

    internal bool HasFeature(int featureTag)
    {
        int index = 0;

        while (index < availableFeatures.Length)
        {
            if (availableFeatures[index] &&
                featureList.GetFeature(index).GetFeatureTag() == featureTag)
            {
                return true;
            }

            index += 1;
        }

        return false;
    }

    internal int GetLookupCount()
    {
        return lookups.Length;
    }

    internal SfntLayoutLookupSelection GetLookup(int index)
    {
        return lookups[index];
    }

    internal SfntLayoutLookupSelection? FindLookup(int lookupIndex)
    {
        int low = 0;
        int high = lookups.Length - 1;

        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            int candidate = lookups[middle].GetLookupIndex();

            if (candidate == lookupIndex)
            {
                return lookups[middle];
            }

            if (candidate < lookupIndex)
            {
                low = middle + 1;
            }
            else
            {
                high = middle - 1;
            }
        }

        return null;
    }
}

internal static class SfntLayoutSelector
{
    const int DefaultScriptTag = 0x44464C54;

    internal static SfntLayoutSelection Select(
        SfntLayoutTable table,
        int scriptTag,
        int languageTag,
        SfntUserFeature[] userFeatures,
        int textLength)
    {
        return CreateSelection(
            table,
            scriptTag,
            languageTag,
            userFeatures,
            textLength);
    }

    internal static SfntLayoutSelection CreateSelection(
        SfntLayoutTable table,
        int scriptTag,
        int languageTag,
        SfntUserFeature[] userFeatures,
        int textLength)
    {
        if (table == null)
        {
            throw new System.ArgumentNullException("table");
        }

        if (userFeatures == null)
        {
            throw new System.ArgumentNullException("userFeatures");
        }

        if (textLength < 0)
        {
            throw new System.ArgumentOutOfRangeException("textLength");
        }

        ValidateUserFeatures(userFeatures);
        SfntLayoutScriptList scriptList = table.GetScriptList();
        SfntLayoutScript? script = scriptList.FindScript(scriptTag);
        bool usedDefaultScript = false;

        if (script == null)
        {
            script = scriptList.FindScript(DefaultScriptTag);
            usedDefaultScript = script != null;
        }

        SfntLayoutFeatureList featureList = table.GetFeatureList();
        bool[] availableFeatures = new bool[featureList.GetFeatureCount()];

        if (script == null)
        {
            return new SfntLayoutSelection(
                featureList,
                null,
                null,
                false,
                false,
                availableFeatures,
                new SfntLayoutLookupSelection[0]);
        }

        SfntLayoutLanguageSystem? languageSystem = languageTag == 0 ?
            script.GetDefaultLanguageSystem() :
            script.FindLanguageSystem(languageTag);
        bool usedDefaultLanguageSystem = languageTag == 0;

        if (languageSystem == null && languageTag != 0)
        {
            languageSystem = script.GetDefaultLanguageSystem();
            usedDefaultLanguageSystem = languageSystem != null;
        }

        if (languageSystem == null)
        {
            return new SfntLayoutSelection(
                featureList,
                script,
                null,
                usedDefaultScript,
                false,
                availableFeatures,
                new SfntLayoutLookupSelection[0]);
        }

        MarkAvailableFeatures(languageSystem, availableFeatures);
        SfntLayoutLookupSelection[] lookups = SelectLookups(
            table,
            languageSystem,
            availableFeatures,
            userFeatures,
            textLength);
        return new SfntLayoutSelection(
            featureList,
            script,
            languageSystem,
            usedDefaultScript,
            usedDefaultLanguageSystem,
            availableFeatures,
            lookups);
    }

    static void ValidateUserFeatures(SfntUserFeature[] userFeatures)
    {
        int index = 0;

        while (index < userFeatures.Length)
        {
            if (userFeatures[index] == null)
            {
                throw new System.ArgumentException(
                    "A user feature cannot be null.");
            }

            index += 1;
        }
    }

    static void MarkAvailableFeatures(
        SfntLayoutLanguageSystem languageSystem,
        bool[] availableFeatures)
    {
        if (languageSystem.HasRequiredFeature())
        {
            availableFeatures[languageSystem.GetRequiredFeatureIndex()] =
                true;
        }

        int index = 0;

        while (index < languageSystem.GetFeatureIndexCount())
        {
            availableFeatures[languageSystem.GetFeatureIndex(index)] = true;
            index += 1;
        }
    }

    static SfntLayoutLookupSelection[] SelectLookups(
        SfntLayoutTable table,
        SfntLayoutLanguageSystem languageSystem,
        bool[] availableFeatures,
        SfntUserFeature[] userFeatures,
        int textLength)
    {
        System.Collections.Generic.List<SfntLayoutLookupSelection>
            selected = new System.Collections.Generic.List<
                SfntLayoutLookupSelection>();
        int lookupIndex = 0;

        while (lookupIndex < table.GetLookupList().GetLookupCount())
        {
            bool required = RequiredFeatureReferencesLookup(
                table.GetFeatureList(),
                languageSystem,
                lookupIndex);
            SfntLayoutFeatureRange[] ranges = BuildLookupRanges(
                table.GetFeatureList(),
                availableFeatures,
                languageSystem,
                lookupIndex,
                required,
                userFeatures,
                textLength);

            if (ranges.Length > 0)
            {
                selected.Add(new SfntLayoutLookupSelection(
                    lookupIndex,
                    textLength,
                    ranges));
            }

            lookupIndex += 1;
        }

        SfntLayoutLookupSelection[] result =
            new SfntLayoutLookupSelection[selected.Count];
        selected.CopyTo(result, 0);
        return result;
    }

    static bool RequiredFeatureReferencesLookup(
        SfntLayoutFeatureList featureList,
        SfntLayoutLanguageSystem languageSystem,
        int lookupIndex)
    {
        if (!languageSystem.HasRequiredFeature())
        {
            return false;
        }

        return FeatureReferencesLookup(
            featureList.GetFeature(
                languageSystem.GetRequiredFeatureIndex()),
            lookupIndex);
    }

    static bool FeatureReferencesLookup(
        SfntLayoutFeature feature,
        int lookupIndex)
    {
        int index = 0;

        while (index < feature.GetLookupIndexCount())
        {
            if (feature.GetLookupIndex(index) == lookupIndex)
            {
                return true;
            }

            index += 1;
        }

        return false;
    }

    static SfntLayoutFeatureRange[] BuildLookupRanges(
        SfntLayoutFeatureList featureList,
        bool[] availableFeatures,
        SfntLayoutLanguageSystem languageSystem,
        int lookupIndex,
        bool required,
        SfntUserFeature[] userFeatures,
        int textLength)
    {
        if (textLength == 0)
        {
            return new SfntLayoutFeatureRange[0];
        }

        long capacity = 2L + userFeatures.Length * 2L;

        if (capacity > int.MaxValue)
        {
            throw new System.ArgumentException(
                "Too many user feature ranges were supplied.");
        }

        int[] boundaries = new int[(int)capacity];
        int boundaryCount = 2;
        boundaries[0] = 0;
        boundaries[1] = textLength;
        int settingIndex = 0;

        while (settingIndex < userFeatures.Length)
        {
            SfntUserFeature setting = userFeatures[settingIndex];

            if (SettingCanAffectLookup(
                featureList,
                availableFeatures,
                languageSystem,
                lookupIndex,
                setting.GetFeatureTag()))
            {
                int start = System.Math.Min(setting.GetStart(), textLength);
                int end = System.Math.Min(setting.GetEnd(), textLength);

                if (start < end)
                {
                    boundaries[boundaryCount] = start;
                    boundaries[boundaryCount + 1] = end;
                    boundaryCount += 2;
                }
            }

            settingIndex += 1;
        }

        System.Array.Sort(boundaries, 0, boundaryCount);
        boundaryCount = RemoveDuplicateBoundaries(
            boundaries,
            boundaryCount);
        System.Collections.Generic.List<SfntLayoutFeatureRange> ranges =
            new System.Collections.Generic.List<SfntLayoutFeatureRange>();
        int boundaryIndex = 0;

        while (boundaryIndex + 1 < boundaryCount)
        {
            int start = boundaries[boundaryIndex];
            int end = boundaries[boundaryIndex + 1];
            int value = required ? 1 : FindLookupValue(
                featureList,
                availableFeatures,
                languageSystem,
                lookupIndex,
                userFeatures,
                start);

            if (value > 0)
            {
                AddOrExtendRange(ranges, start, end, value);
            }

            boundaryIndex += 1;
        }

        SfntLayoutFeatureRange[] result =
            new SfntLayoutFeatureRange[ranges.Count];
        ranges.CopyTo(result, 0);
        return result;
    }

    static bool SettingCanAffectLookup(
        SfntLayoutFeatureList featureList,
        bool[] availableFeatures,
        SfntLayoutLanguageSystem languageSystem,
        int lookupIndex,
        int featureTag)
    {
        int requiredIndex = languageSystem.HasRequiredFeature() ?
            languageSystem.GetRequiredFeatureIndex() : -1;
        int featureIndex = 0;

        while (featureIndex < availableFeatures.Length)
        {
            if (availableFeatures[featureIndex] &&
                featureIndex != requiredIndex)
            {
                SfntLayoutFeature feature = featureList.GetFeature(
                    featureIndex);

                if (feature.GetFeatureTag() == featureTag &&
                    FeatureReferencesLookup(feature, lookupIndex))
                {
                    return true;
                }
            }

            featureIndex += 1;
        }

        return false;
    }

    static int RemoveDuplicateBoundaries(
        int[] boundaries,
        int count)
    {
        int writeIndex = 1;
        int readIndex = 1;

        while (readIndex < count)
        {
            if (boundaries[readIndex] != boundaries[writeIndex - 1])
            {
                boundaries[writeIndex] = boundaries[readIndex];
                writeIndex += 1;
            }

            readIndex += 1;
        }

        return writeIndex;
    }

    static int FindLookupValue(
        SfntLayoutFeatureList featureList,
        bool[] availableFeatures,
        SfntLayoutLanguageSystem languageSystem,
        int lookupIndex,
        SfntUserFeature[] userFeatures,
        int textIndex)
    {
        int requiredIndex = languageSystem.HasRequiredFeature() ?
            languageSystem.GetRequiredFeatureIndex() : -1;
        int winningSettingIndex = -1;
        int value = 0;
        int featureIndex = 0;

        while (featureIndex < availableFeatures.Length)
        {
            if (availableFeatures[featureIndex] &&
                featureIndex != requiredIndex)
            {
                SfntLayoutFeature feature = featureList.GetFeature(
                    featureIndex);

                if (FeatureReferencesLookup(feature, lookupIndex))
                {
                    int candidateSettingIndex = FindLastSetting(
                        userFeatures,
                        feature.GetFeatureTag(),
                        textIndex);

                    if (candidateSettingIndex >= 0 &&
                        userFeatures[candidateSettingIndex].GetValue() > 0 &&
                        candidateSettingIndex > winningSettingIndex)
                    {
                        winningSettingIndex = candidateSettingIndex;
                        value = userFeatures[candidateSettingIndex]
                            .GetValue();
                    }
                }
            }

            featureIndex += 1;
        }

        return value;
    }

    static int FindLastSetting(
        SfntUserFeature[] userFeatures,
        int featureTag,
        int textIndex)
    {
        int index = userFeatures.Length - 1;

        while (index >= 0)
        {
            SfntUserFeature setting = userFeatures[index];

            if (setting.GetFeatureTag() == featureTag &&
                textIndex >= setting.GetStart() &&
                textIndex < setting.GetEnd())
            {
                return index;
            }

            index -= 1;
        }

        return -1;
    }

    static void AddOrExtendRange(
        System.Collections.Generic.List<SfntLayoutFeatureRange> ranges,
        int start,
        int end,
        int value)
    {
        if (ranges.Count > 0)
        {
            SfntLayoutFeatureRange previous = ranges[ranges.Count - 1];

            if (previous.GetEnd() == start &&
                previous.GetValue() == value)
            {
                ranges[ranges.Count - 1] = new SfntLayoutFeatureRange(
                    previous.GetStart(),
                    end,
                    value);
                return;
            }
        }

        ranges.Add(new SfntLayoutFeatureRange(start, end, value));
    }
}
