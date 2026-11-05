namespace ttf;

internal class SfntBaseCoordinate
{
    int format;
    int coordinate;
    int referenceGlyphId;
    int contourPointIndex;
    int deviceOffset;
    int deviceAbsoluteOffset;
    SfntDeviceOrVariationIndex? adjustment;

    internal SfntBaseCoordinate(
        int format,
        int coordinate,
        int referenceGlyphId,
        int contourPointIndex,
        int deviceOffset,
        int deviceAbsoluteOffset,
        SfntDeviceOrVariationIndex? adjustment)
    {
        this.format = format;
        this.coordinate = coordinate;
        this.referenceGlyphId = referenceGlyphId;
        this.contourPointIndex = contourPointIndex;
        this.deviceOffset = deviceOffset;
        this.deviceAbsoluteOffset = deviceAbsoluteOffset;
        this.adjustment = adjustment;
    }

    internal int GetFormat()
    {
        return format;
    }

    internal int GetCoordinate()
    {
        return coordinate;
    }

    internal bool HasContourPointReference()
    {
        return format == 2;
    }

    internal int GetReferenceGlyphId()
    {
        return referenceGlyphId;
    }

    internal int GetContourPointIndex()
    {
        return contourPointIndex;
    }

    internal bool HasDeviceOrVariationIndex()
    {
        return format == 3 && deviceOffset != 0;
    }

    internal int GetDeviceOffset()
    {
        return deviceOffset;
    }

    internal int GetDeviceAbsoluteOffset()
    {
        return deviceAbsoluteOffset;
    }

    internal SfntDeviceOrVariationIndex? GetAdjustment()
    {
        return adjustment;
    }
}

internal class SfntBaseFeatureMinMax
{
    int featureTag;
    SfntBaseCoordinate? minimum;
    SfntBaseCoordinate? maximum;

    internal SfntBaseFeatureMinMax(
        int featureTag,
        SfntBaseCoordinate? minimum,
        SfntBaseCoordinate? maximum)
    {
        this.featureTag = featureTag;
        this.minimum = minimum;
        this.maximum = maximum;
    }

    internal int GetFeatureTag()
    {
        return featureTag;
    }

    internal SfntBaseCoordinate? GetMinimum()
    {
        return minimum;
    }

    internal SfntBaseCoordinate? GetMaximum()
    {
        return maximum;
    }
}

internal class SfntBaseMinMax
{
    SfntBaseCoordinate? minimum;
    SfntBaseCoordinate? maximum;
    SfntBaseFeatureMinMax[] features;

    internal SfntBaseMinMax(
        SfntBaseCoordinate? minimum,
        SfntBaseCoordinate? maximum,
        SfntBaseFeatureMinMax[] features)
    {
        this.minimum = minimum;
        this.maximum = maximum;
        this.features = features;
    }

    internal SfntBaseCoordinate? GetMinimum()
    {
        return minimum;
    }

    internal SfntBaseCoordinate? GetMaximum()
    {
        return maximum;
    }

    internal int GetFeatureCount()
    {
        return features.Length;
    }

    internal SfntBaseFeatureMinMax GetFeature(int index)
    {
        return features[index];
    }

    internal SfntBaseFeatureMinMax? FindFeature(int featureTag)
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

internal class SfntBaseValues
{
    int defaultBaselineIndex;
    SfntBaseCoordinate[] coordinates;

    internal SfntBaseValues(
        int defaultBaselineIndex,
        SfntBaseCoordinate[] coordinates)
    {
        this.defaultBaselineIndex = defaultBaselineIndex;
        this.coordinates = coordinates;
    }

    internal int GetDefaultBaselineIndex()
    {
        return defaultBaselineIndex;
    }

    internal int GetCoordinateCount()
    {
        return coordinates.Length;
    }

    internal SfntBaseCoordinate GetCoordinate(int index)
    {
        return coordinates[index];
    }
}

internal class SfntBaseLanguageSystem
{
    int languageTag;
    SfntBaseMinMax minMax;

    internal SfntBaseLanguageSystem(int languageTag, SfntBaseMinMax minMax)
    {
        this.languageTag = languageTag;
        this.minMax = minMax;
    }

    internal int GetLanguageTag()
    {
        return languageTag;
    }

    internal SfntBaseMinMax GetMinMax()
    {
        return minMax;
    }
}

internal class SfntBaseScript
{
    int scriptTag;
    SfntBaseValues? values;
    SfntBaseMinMax? defaultMinMax;
    SfntBaseLanguageSystem[] languageSystems;

    internal SfntBaseScript(
        int scriptTag,
        SfntBaseValues? values,
        SfntBaseMinMax? defaultMinMax,
        SfntBaseLanguageSystem[] languageSystems)
    {
        this.scriptTag = scriptTag;
        this.values = values;
        this.defaultMinMax = defaultMinMax;
        this.languageSystems = languageSystems;
    }

    internal int GetScriptTag()
    {
        return scriptTag;
    }

    internal SfntBaseValues? GetValues()
    {
        return values;
    }

    internal SfntBaseMinMax? GetDefaultMinMax()
    {
        return defaultMinMax;
    }

    internal int GetLanguageSystemCount()
    {
        return languageSystems.Length;
    }

    internal SfntBaseLanguageSystem GetLanguageSystem(int index)
    {
        return languageSystems[index];
    }

    internal SfntBaseLanguageSystem? FindLanguageSystem(int languageTag)
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

internal class SfntBaseAxis
{
    bool horizontal;
    int[] baselineTags;
    SfntBaseScript[] scripts;

    internal SfntBaseAxis(
        bool horizontal,
        int[] baselineTags,
        SfntBaseScript[] scripts)
    {
        this.horizontal = horizontal;
        this.baselineTags = baselineTags;
        this.scripts = scripts;
    }

    internal bool IsHorizontal()
    {
        return horizontal;
    }

    internal bool IsVertical()
    {
        return !horizontal;
    }

    internal int GetBaselineTagCount()
    {
        return baselineTags.Length;
    }

    internal int GetBaselineTag(int index)
    {
        return baselineTags[index];
    }

    internal int GetScriptCount()
    {
        return scripts.Length;
    }

    internal SfntBaseScript GetScript(int index)
    {
        return scripts[index];
    }

    internal int FindBaselineTagIndex(int baselineTag)
    {
        int low = 0;
        int high = baselineTags.Length - 1;
        uint target = CheckedFontMath.ReinterpretInt32AsUInt32(baselineTag);

        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            uint candidate = CheckedFontMath.ReinterpretInt32AsUInt32(
                baselineTags[middle]);

            if (candidate == target)
            {
                return middle;
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

        return -1;
    }

    internal SfntBaseScript? FindScript(int scriptTag)
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

    internal SfntBaseCoordinate? FindBaseline(int scriptTag, int baselineTag)
    {
        int baselineIndex = FindBaselineTagIndex(baselineTag);
        SfntBaseScript? script = FindScript(scriptTag);

        if (baselineIndex < 0 || script == null || script.GetValues() == null)
        {
            return null;
        }

        SfntBaseValues? baseValues = script.GetValues();
        System.ArgumentNullException.ThrowIfNull(baseValues);
        return baseValues.GetCoordinate(baselineIndex);
    }

    internal SfntBaseCoordinate? FindDefaultBaseline(int scriptTag)
    {
        SfntBaseScript? script = FindScript(scriptTag);

        if (script == null || script.GetValues() == null)
        {
            return null;
        }

        SfntBaseValues? values = script.GetValues();
        System.ArgumentNullException.ThrowIfNull(values);
        return values.GetCoordinate(values.GetDefaultBaselineIndex());
    }

    internal int FindDefaultBaselineTag(int scriptTag)
    {
        SfntBaseScript? script = FindScript(scriptTag);

        if (script == null || script.GetValues() == null)
        {
            return 0;
        }

        SfntBaseValues? baseValues = script.GetValues();
        System.ArgumentNullException.ThrowIfNull(baseValues);
        return baselineTags[baseValues.GetDefaultBaselineIndex()];
    }
}

internal class SfntBaseTable
{
    int majorVersion;
    int minorVersion;
    long itemVariationStoreOffset;
    SfntBaseAxis? horizontalAxis;
    SfntBaseAxis? verticalAxis;

    internal SfntBaseTable(
        int majorVersion,
        int minorVersion,
        long itemVariationStoreOffset,
        SfntBaseAxis? horizontalAxis,
        SfntBaseAxis? verticalAxis)
    {
        this.majorVersion = majorVersion;
        this.minorVersion = minorVersion;
        this.itemVariationStoreOffset = itemVariationStoreOffset;
        this.horizontalAxis = horizontalAxis;
        this.verticalAxis = verticalAxis;
    }

    internal int GetMajorVersion()
    {
        return majorVersion;
    }

    internal int GetMinorVersion()
    {
        return minorVersion;
    }

    internal long GetItemVariationStoreOffset()
    {
        return itemVariationStoreOffset;
    }

    internal SfntBaseAxis? GetHorizontalAxis()
    {
        return horizontalAxis;
    }

    internal SfntBaseAxis? GetVerticalAxis()
    {
        return verticalAxis;
    }

    internal SfntBaseCoordinate? FindHorizontalBaseline(
        int scriptTag,
        int baselineTag)
    {
        return horizontalAxis == null ?
            null : horizontalAxis.FindBaseline(scriptTag, baselineTag);
    }

    internal SfntBaseCoordinate? FindVerticalBaseline(
        int scriptTag,
        int baselineTag)
    {
        return verticalAxis == null ?
            null : verticalAxis.FindBaseline(scriptTag, baselineTag);
    }
}
