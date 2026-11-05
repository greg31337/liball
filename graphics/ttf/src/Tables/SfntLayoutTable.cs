namespace ttf;

internal class SfntLayoutLanguageSystem
{
    int languageTag;
    int requiredFeatureIndex;
    int[] featureIndices;

    internal SfntLayoutLanguageSystem(
        int languageTag,
        int requiredFeatureIndex,
        int[] featureIndices)
    {
        this.languageTag = languageTag;
        this.requiredFeatureIndex = requiredFeatureIndex;
        this.featureIndices = featureIndices;
    }

    internal int GetLanguageTag()
    {
        return languageTag;
    }

    internal bool HasRequiredFeature()
    {
        return requiredFeatureIndex != 0xFFFF;
    }

    internal int GetRequiredFeatureIndex()
    {
        return requiredFeatureIndex;
    }

    internal int GetFeatureIndexCount()
    {
        return featureIndices.Length;
    }

    internal int GetFeatureIndex(int index)
    {
        return featureIndices[index];
    }
}

internal class SfntLayoutScript
{
    int scriptTag;
    SfntLayoutLanguageSystem? defaultLanguageSystem;
    SfntLayoutLanguageSystem[] languageSystems;

    internal SfntLayoutScript(
        int scriptTag,
        SfntLayoutLanguageSystem? defaultLanguageSystem,
        SfntLayoutLanguageSystem[] languageSystems)
    {
        this.scriptTag = scriptTag;
        this.defaultLanguageSystem = defaultLanguageSystem;
        this.languageSystems = languageSystems;
    }

    internal int GetScriptTag()
    {
        return scriptTag;
    }

    internal SfntLayoutLanguageSystem? GetDefaultLanguageSystem()
    {
        return defaultLanguageSystem;
    }

    internal int GetLanguageSystemCount()
    {
        return languageSystems.Length;
    }

    internal SfntLayoutLanguageSystem GetLanguageSystem(int index)
    {
        return languageSystems[index];
    }

    internal SfntLayoutLanguageSystem? FindLanguageSystem(int languageTag)
    {
        int low = 0;
        int high = languageSystems.Length - 1;
        uint target = CheckedFontMath.ReinterpretInt32AsUInt32(languageTag);

        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            uint candidate = CheckedFontMath.ReinterpretInt32AsUInt32(
                languageSystems[middle].GetLanguageTag());

            if (candidate == target)
            {
                return languageSystems[middle];
            }

            if (candidate < target)
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

internal class SfntLayoutScriptList
{
    SfntLayoutScript[] scripts;

    internal SfntLayoutScriptList(SfntLayoutScript[] scripts)
    {
        this.scripts = scripts;
    }

    internal int GetScriptCount()
    {
        return scripts.Length;
    }

    internal SfntLayoutScript GetScript(int index)
    {
        return scripts[index];
    }

    internal SfntLayoutScript? FindScript(int scriptTag)
    {
        int low = 0;
        int high = scripts.Length - 1;
        uint target = CheckedFontMath.ReinterpretInt32AsUInt32(scriptTag);

        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            uint candidate = CheckedFontMath.ReinterpretInt32AsUInt32(
                scripts[middle].GetScriptTag());

            if (candidate == target)
            {
                return scripts[middle];
            }

            if (candidate < target)
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

internal class SfntLayoutFeature
{
    int featureTag;
    int featureParamsOffset;
    int featureParamsTableOffset;
    int[] lookupIndices;

    internal SfntLayoutFeature(
        int featureTag,
        int featureParamsOffset,
        int featureParamsTableOffset,
        int[] lookupIndices)
    {
        this.featureTag = featureTag;
        this.featureParamsOffset = featureParamsOffset;
        this.featureParamsTableOffset = featureParamsTableOffset;
        this.lookupIndices = lookupIndices;
    }

    internal int GetFeatureTag()
    {
        return featureTag;
    }

    internal bool HasFeatureParams()
    {
        return featureParamsOffset != 0;
    }

    internal int GetFeatureParamsOffset()
    {
        return featureParamsOffset;
    }

    internal int GetFeatureParamsTableOffset()
    {
        return featureParamsTableOffset;
    }

    internal int GetLookupIndexCount()
    {
        return lookupIndices.Length;
    }

    internal int GetLookupIndex(int index)
    {
        return lookupIndices[index];
    }
}

internal class SfntLayoutFeatureList
{
    SfntLayoutFeature[] features;

    internal SfntLayoutFeatureList(SfntLayoutFeature[] features)
    {
        this.features = features;
    }

    internal int GetFeatureCount()
    {
        return features.Length;
    }

    internal SfntLayoutFeature GetFeature(int index)
    {
        return features[index];
    }

    internal SfntLayoutFeature? FindFeature(int featureTag)
    {
        int low = 0;
        int high = features.Length - 1;
        uint target = CheckedFontMath.ReinterpretInt32AsUInt32(featureTag);

        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            uint candidate = CheckedFontMath.ReinterpretInt32AsUInt32(
                features[middle].GetFeatureTag());

            if (candidate == target)
            {
                while (middle > 0 &&
                    features[middle - 1].GetFeatureTag() == featureTag)
                {
                    middle -= 1;
                }

                return features[middle];
            }

            if (candidate < target)
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

internal class SfntLayoutLookup
{
    int lookupType;
    int lookupFlag;
    int[] subtableOffsets;
    int[] subtableTableOffsets;
    int markFilteringSet;

    internal SfntLayoutLookup(
        int lookupType,
        int lookupFlag,
        int[] subtableOffsets,
        int[] subtableTableOffsets,
        int markFilteringSet)
    {
        this.lookupType = lookupType;
        this.lookupFlag = lookupFlag;
        this.subtableOffsets = subtableOffsets;
        this.subtableTableOffsets = subtableTableOffsets;
        this.markFilteringSet = markFilteringSet;
    }

    internal int GetLookupType()
    {
        return lookupType;
    }

    internal int GetLookupFlag()
    {
        return lookupFlag;
    }

    internal bool IsRightToLeft()
    {
        return (lookupFlag & 0x0001) != 0;
    }

    internal bool IgnoresBaseGlyphs()
    {
        return (lookupFlag & 0x0002) != 0;
    }

    internal bool IgnoresLigatures()
    {
        return (lookupFlag & 0x0004) != 0;
    }

    internal bool IgnoresMarks()
    {
        return (lookupFlag & 0x0008) != 0;
    }

    internal bool UsesMarkFilteringSet()
    {
        return (lookupFlag & 0x0010) != 0;
    }

    internal int GetMarkAttachmentType()
    {
        return lookupFlag >> 8;
    }

    internal int GetMarkFilteringSet()
    {
        return markFilteringSet;
    }

    internal int GetSubtableCount()
    {
        return subtableOffsets.Length;
    }

    internal int GetSubtableOffset(int index)
    {
        return subtableOffsets[index];
    }

    internal int GetSubtableTableOffset(int index)
    {
        return subtableTableOffsets[index];
    }
}

internal class SfntLayoutLookupList
{
    SfntLayoutLookup[] lookups;

    internal SfntLayoutLookupList(SfntLayoutLookup[] lookups)
    {
        this.lookups = lookups;
    }

    internal int GetLookupCount()
    {
        return lookups.Length;
    }

    internal SfntLayoutLookup GetLookup(int index)
    {
        return lookups[index];
    }
}

internal class SfntLayoutTable
{
    int majorVersion;
    int minorVersion;
    int featureVariationsOffset;
    SfntLayoutScriptList scriptList;
    SfntLayoutFeatureList featureList;
    SfntLayoutLookupList lookupList;
    FontDataSlice data;

    internal SfntLayoutTable(
        int majorVersion,
        int minorVersion,
        int featureVariationsOffset,
        SfntLayoutScriptList scriptList,
        SfntLayoutFeatureList featureList,
        SfntLayoutLookupList lookupList,
        FontDataSlice data)
    {
        this.majorVersion = majorVersion;
        this.minorVersion = minorVersion;
        this.featureVariationsOffset = featureVariationsOffset;
        this.scriptList = scriptList;
        this.featureList = featureList;
        this.lookupList = lookupList;
        this.data = data;
    }

    internal int GetMajorVersion()
    {
        return majorVersion;
    }

    internal int GetMinorVersion()
    {
        return minorVersion;
    }

    internal int GetFeatureVariationsOffset()
    {
        return featureVariationsOffset;
    }

    internal SfntLayoutScriptList GetScriptList()
    {
        return scriptList;
    }

    internal SfntLayoutFeatureList GetFeatureList()
    {
        return featureList;
    }

    internal SfntLayoutLookupList GetLookupList()
    {
        return lookupList;
    }

    internal FontDataSlice GetDataSlice()
    {
        return data;
    }
}
