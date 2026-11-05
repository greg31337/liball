namespace ttf;

internal class SfntCmapVariationDefaultRange
{
    int startUnicodeValue;
    int additionalCount;

    internal SfntCmapVariationDefaultRange(
        int startUnicodeValue,
        int additionalCount)
    {
        this.startUnicodeValue = startUnicodeValue;
        this.additionalCount = additionalCount;
    }

    internal int GetStartUnicodeValue()
    {
        return startUnicodeValue;
    }

    internal int GetAdditionalCount()
    {
        return additionalCount;
    }

    internal int GetEndUnicodeValue()
    {
        return startUnicodeValue + additionalCount;
    }

    internal bool ContainsUnicodeValue(int unicodeValue)
    {
        return unicodeValue >= startUnicodeValue &&
            unicodeValue <= startUnicodeValue + additionalCount;
    }
}

internal class SfntCmapVariationMapping
{
    int unicodeValue;
    int glyphId;

    internal SfntCmapVariationMapping(int unicodeValue, int glyphId)
    {
        this.unicodeValue = unicodeValue;
        this.glyphId = glyphId;
    }

    internal int GetUnicodeValue()
    {
        return unicodeValue;
    }

    internal int GetGlyphId()
    {
        return glyphId;
    }
}

internal class SfntCmapVariationSelectorRecord
{
    int variationSelector;
    SfntCmapVariationDefaultRange[] defaultRanges;
    SfntCmapVariationMapping[] nonDefaultMappings;

    internal SfntCmapVariationSelectorRecord(
        int variationSelector,
        SfntCmapVariationDefaultRange[] defaultRanges,
        SfntCmapVariationMapping[] nonDefaultMappings)
    {
        this.variationSelector = variationSelector;
        this.defaultRanges = defaultRanges;
        this.nonDefaultMappings = nonDefaultMappings;
    }

    internal int GetVariationSelector()
    {
        return variationSelector;
    }

    internal int GetDefaultRangeCount()
    {
        return defaultRanges.Length;
    }

    internal SfntCmapVariationDefaultRange GetDefaultRange(int index)
    {
        if (index < 0 || index >= defaultRanges.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return defaultRanges[index];
    }

    internal int GetNonDefaultMappingCount()
    {
        return nonDefaultMappings.Length;
    }

    internal SfntCmapVariationMapping GetNonDefaultMapping(int index)
    {
        if (index < 0 || index >= nonDefaultMappings.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return nonDefaultMappings[index];
    }

    internal bool IsDefaultUnicodeValue(int unicodeValue)
    {
        int low = 0;
        int high = defaultRanges.Length - 1;

        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            SfntCmapVariationDefaultRange range = defaultRanges[middle];

            if (unicodeValue < range.GetStartUnicodeValue())
            {
                high = middle - 1;
            }
            else if (unicodeValue > range.GetEndUnicodeValue())
            {
                low = middle + 1;
            }
            else
            {
                return true;
            }
        }

        return false;
    }

    internal SfntCmapVariationMapping? FindNonDefaultMapping(int unicodeValue)
    {
        int low = 0;
        int high = nonDefaultMappings.Length - 1;

        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            SfntCmapVariationMapping mapping = nonDefaultMappings[middle];

            if (unicodeValue < mapping.GetUnicodeValue())
            {
                high = middle - 1;
            }
            else if (unicodeValue > mapping.GetUnicodeValue())
            {
                low = middle + 1;
            }
            else
            {
                return mapping;
            }
        }

        return null;
    }
}

internal class SfntCmapFormat14Data
{
    SfntCmapVariationSelectorRecord[] selectorRecords;

    internal SfntCmapFormat14Data(
        SfntCmapVariationSelectorRecord[] selectorRecords)
    {
        this.selectorRecords = selectorRecords;
    }

    internal int GetSelectorRecordCount()
    {
        return selectorRecords.Length;
    }

    internal SfntCmapVariationSelectorRecord GetSelectorRecord(int index)
    {
        if (index < 0 || index >= selectorRecords.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return selectorRecords[index];
    }

    internal SfntCmapVariationSelectorRecord? FindSelectorRecord(
        int variationSelector)
    {
        int low = 0;
        int high = selectorRecords.Length - 1;

        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            SfntCmapVariationSelectorRecord record = selectorRecords[middle];

            if (variationSelector < record.GetVariationSelector())
            {
                high = middle - 1;
            }
            else if (variationSelector > record.GetVariationSelector())
            {
                low = middle + 1;
            }
            else
            {
                return record;
            }
        }

        return null;
    }

    internal bool IsDefaultVariation(int unicodeValue, int variationSelector)
    {
        SfntCmapVariationSelectorRecord? record =
            FindSelectorRecord(variationSelector);

        if (record == null)
        {
            return false;
        }

        return record.IsDefaultUnicodeValue(unicodeValue);
    }

    internal int MapNonDefaultVariation(
        int unicodeValue,
        int variationSelector)
    {
        SfntCmapVariationSelectorRecord? record =
            FindSelectorRecord(variationSelector);

        if (record == null)
        {
            return 0;
        }

        SfntCmapVariationMapping? mapping =
            record.FindNonDefaultMapping(unicodeValue);

        if (mapping == null)
        {
            return 0;
        }

        return mapping.GetGlyphId();
    }
}
