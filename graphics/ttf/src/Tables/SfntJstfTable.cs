namespace ttf;

internal class SfntJstfLookupList
{
    int[] lookupIndices;

    internal SfntJstfLookupList(int[] lookupIndices)
    {
        this.lookupIndices = lookupIndices;
    }

    internal int GetLookupCount()
    {
        return lookupIndices.Length;
    }

    internal int GetLookupIndex(int index)
    {
        if (index < 0 || index >= lookupIndices.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return lookupIndices[index];
    }
}

internal class SfntJstfMaximum
{
    SfntGposTable positioningTable;

    internal SfntJstfMaximum(SfntGposTable positioningTable)
    {
        this.positioningTable = positioningTable;
    }

    internal int GetLookupCount()
    {
        return positioningTable.GetLookupCount();
    }

    internal SfntGposLookup GetLookup(int index)
    {
        return positioningTable.GetLookup(index);
    }
}

internal class SfntJstfOpportunity
{
    bool shrinkage;
    SfntJstfLookupList? substitutionEnable;
    SfntJstfLookupList? substitutionDisable;
    SfntJstfLookupList? positioningEnable;
    SfntJstfLookupList? positioningDisable;
    SfntJstfMaximum? maximum;

    internal SfntJstfOpportunity(
        bool shrinkage,
        SfntJstfLookupList? substitutionEnable,
        SfntJstfLookupList? substitutionDisable,
        SfntJstfLookupList? positioningEnable,
        SfntJstfLookupList? positioningDisable,
        SfntJstfMaximum? maximum)
    {
        this.shrinkage = shrinkage;
        this.substitutionEnable = substitutionEnable;
        this.substitutionDisable = substitutionDisable;
        this.positioningEnable = positioningEnable;
        this.positioningDisable = positioningDisable;
        this.maximum = maximum;
    }

    internal bool IsShrinkage()
    {
        return shrinkage;
    }

    internal bool IsExtension()
    {
        return !shrinkage;
    }

    internal SfntJstfLookupList? GetSubstitutionEnableList()
    {
        return substitutionEnable;
    }

    internal SfntJstfLookupList? GetSubstitutionDisableList()
    {
        return substitutionDisable;
    }

    internal SfntJstfLookupList? GetPositioningEnableList()
    {
        return positioningEnable;
    }

    internal SfntJstfLookupList? GetPositioningDisableList()
    {
        return positioningDisable;
    }

    internal SfntJstfMaximum? GetMaximum()
    {
        return maximum;
    }

    internal bool HasActions()
    {
        return substitutionEnable != null ||
            substitutionDisable != null ||
            positioningEnable != null ||
            positioningDisable != null ||
            maximum != null;
    }
}

internal class SfntJstfPriority
{
    SfntJstfOpportunity shrinkage;
    SfntJstfOpportunity extension;

    internal SfntJstfPriority(
        SfntJstfOpportunity shrinkage,
        SfntJstfOpportunity extension)
    {
        this.shrinkage = shrinkage;
        this.extension = extension;
    }

    internal SfntJstfOpportunity GetShrinkage()
    {
        return shrinkage;
    }

    internal SfntJstfOpportunity GetExtension()
    {
        return extension;
    }
}

internal class SfntJstfLanguageSystem
{
    int languageTag;
    SfntJstfPriority[] priorities;

    internal SfntJstfLanguageSystem(
        int languageTag,
        SfntJstfPriority[] priorities)
    {
        this.languageTag = languageTag;
        this.priorities = priorities;
    }

    internal int GetLanguageTag()
    {
        return languageTag;
    }

    internal int GetPriorityCount()
    {
        return priorities.Length;
    }

    internal SfntJstfPriority GetPriority(int index)
    {
        if (index < 0 || index >= priorities.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return priorities[index];
    }
}

internal class SfntJstfScript
{
    int scriptTag;
    int[] extenderGlyphIds;
    SfntJstfLanguageSystem? defaultLanguageSystem;
    SfntJstfLanguageSystem[] languageSystems;

    internal SfntJstfScript(
        int scriptTag,
        int[] extenderGlyphIds,
        SfntJstfLanguageSystem? defaultLanguageSystem,
        SfntJstfLanguageSystem[] languageSystems)
    {
        this.scriptTag = scriptTag;
        this.extenderGlyphIds = extenderGlyphIds;
        this.defaultLanguageSystem = defaultLanguageSystem;
        this.languageSystems = languageSystems;
    }

    internal int GetScriptTag()
    {
        return scriptTag;
    }

    internal int GetExtenderGlyphCount()
    {
        return extenderGlyphIds.Length;
    }

    internal int GetExtenderGlyphId(int index)
    {
        if (index < 0 || index >= extenderGlyphIds.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return extenderGlyphIds[index];
    }

    internal SfntJstfLanguageSystem? GetDefaultLanguageSystem()
    {
        return defaultLanguageSystem;
    }

    internal int GetLanguageSystemCount()
    {
        return languageSystems.Length;
    }

    internal SfntJstfLanguageSystem GetLanguageSystem(int index)
    {
        if (index < 0 || index >= languageSystems.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return languageSystems[index];
    }

    internal SfntJstfLanguageSystem? FindLanguageSystem(int languageTag)
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

internal class SfntJstfTable
{
    int majorVersion;
    int minorVersion;
    SfntJstfScript[] scripts;

    internal SfntJstfTable(
        int majorVersion,
        int minorVersion,
        SfntJstfScript[] scripts)
    {
        this.majorVersion = majorVersion;
        this.minorVersion = minorVersion;
        this.scripts = scripts;
    }

    internal int GetMajorVersion()
    {
        return majorVersion;
    }

    internal int GetMinorVersion()
    {
        return minorVersion;
    }

    internal int GetScriptCount()
    {
        return scripts.Length;
    }

    internal SfntJstfScript GetScript(int index)
    {
        if (index < 0 || index >= scripts.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return scripts[index];
    }

    internal SfntJstfScript? FindScript(int scriptTag)
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
