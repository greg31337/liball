namespace ttf;

internal enum SfntLayoutFeatureCategory
{
    Mandatory = 0,
    Default = 1,
    Discretionary = 2
}

internal class SfntLayoutFeaturePlanEntry
{
    int featureTag;
    SfntLayoutFeatureCategory category;
    int textLength;
    SfntLayoutFeatureRange[] ranges;

    internal SfntLayoutFeaturePlanEntry(
        int featureTag,
        SfntLayoutFeatureCategory category,
        int textLength,
        SfntLayoutFeatureRange[] ranges)
    {
        this.featureTag = featureTag;
        this.category = category;
        this.textLength = textLength;
        this.ranges = ranges;
    }

    internal int GetFeatureTag()
    {
        return featureTag;
    }

    internal SfntLayoutFeatureCategory GetCategory()
    {
        return category;
    }

    internal int GetRangeCount()
    {
        return ranges.Length;
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

internal class SfntLayoutFeaturePlan
{
    SfntLayoutFeaturePlanEntry[] entries;
    SfntUserFeature[] selections;
    int textLength;

    internal SfntLayoutFeaturePlan(
        SfntLayoutFeaturePlanEntry[] entries,
        SfntUserFeature[] selections,
        int textLength)
    {
        this.entries = entries;
        this.selections = selections;
        this.textLength = textLength;
    }

    internal int GetEntryCount()
    {
        return entries.Length;
    }

    internal SfntLayoutFeaturePlanEntry GetEntry(int index)
    {
        return entries[index];
    }

    internal SfntLayoutFeaturePlanEntry? FindEntry(int featureTag)
    {
        int index = 0;

        while (index < entries.Length)
        {
            if (entries[index].GetFeatureTag() == featureTag)
            {
                return entries[index];
            }

            index += 1;
        }

        return null;
    }

    internal int GetSelectionCount()
    {
        return selections.Length;
    }

    internal int GetTextLength()
    {
        return textLength;
    }

    internal SfntUserFeature GetSelection(int index)
    {
        return selections[index];
    }

    internal SfntLayoutSelection Select(
        SfntLayoutTable table,
        int scriptTag,
        int languageTag)
    {
        return SfntLayoutSelector.CreateSelection(
            table,
            scriptTag,
            languageTag,
            selections,
            textLength);
    }
}

internal static class SfntLayoutFeaturePlanBuilder
{
    internal static SfntLayoutFeaturePlan Build(
        int[] mandatoryFeatureTags,
        int[] defaultFeatureTags,
        int[] discretionaryFeatureTags,
        SfntUserFeature[] userFeatures,
        int textLength)
    {
        if (mandatoryFeatureTags == null)
        {
            throw new System.ArgumentNullException(
                "mandatoryFeatureTags");
        }

        if (defaultFeatureTags == null)
        {
            throw new System.ArgumentNullException("defaultFeatureTags");
        }

        if (discretionaryFeatureTags == null)
        {
            throw new System.ArgumentNullException(
                "discretionaryFeatureTags");
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
        System.Collections.Generic.List<int> tags =
            new System.Collections.Generic.List<int>();
        System.Collections.Generic.List<SfntLayoutFeatureCategory>
            categories = new System.Collections.Generic.List<
                SfntLayoutFeatureCategory>();
        AddFeatureTags(
            tags,
            categories,
            mandatoryFeatureTags,
            SfntLayoutFeatureCategory.Mandatory);
        AddFeatureTags(
            tags,
            categories,
            defaultFeatureTags,
            SfntLayoutFeatureCategory.Default);
        AddFeatureTags(
            tags,
            categories,
            discretionaryFeatureTags,
            SfntLayoutFeatureCategory.Discretionary);
        AddUserFeatureTags(tags, categories, userFeatures);
        SfntLayoutFeaturePlanEntry[] entries =
            new SfntLayoutFeaturePlanEntry[tags.Count];
        System.Collections.Generic.List<SfntUserFeature> selections =
            new System.Collections.Generic.List<SfntUserFeature>();
        int index = 0;

        while (index < tags.Count)
        {
            SfntLayoutFeatureRange[] ranges = BuildRanges(
                tags[index],
                categories[index],
                userFeatures,
                textLength);
            entries[index] = new SfntLayoutFeaturePlanEntry(
                tags[index],
                categories[index],
                textLength,
                ranges);
            AddSelections(selections, tags[index], ranges);
            index += 1;
        }

        SfntUserFeature[] selectedFeatures =
            new SfntUserFeature[selections.Count];
        selections.CopyTo(selectedFeatures, 0);
        return new SfntLayoutFeaturePlan(
            entries,
            selectedFeatures,
            textLength);
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

    static void AddFeatureTags(
        System.Collections.Generic.List<int> tags,
        System.Collections.Generic.List<SfntLayoutFeatureCategory>
            categories,
        int[] featureTags,
        SfntLayoutFeatureCategory category)
    {
        int index = 0;

        while (index < featureTags.Length)
        {
            if (FindTag(tags, featureTags[index]) < 0)
            {
                tags.Add(featureTags[index]);
                categories.Add(category);
            }

            index += 1;
        }
    }

    static void AddUserFeatureTags(
        System.Collections.Generic.List<int> tags,
        System.Collections.Generic.List<SfntLayoutFeatureCategory>
            categories,
        SfntUserFeature[] userFeatures)
    {
        int index = 0;

        while (index < userFeatures.Length)
        {
            int featureTag = userFeatures[index].GetFeatureTag();

            if (FindTag(tags, featureTag) < 0)
            {
                tags.Add(featureTag);
                categories.Add(SfntLayoutFeatureCategory.Discretionary);
            }

            index += 1;
        }
    }

    static int FindTag(
        System.Collections.Generic.List<int> tags,
        int featureTag)
    {
        int index = 0;

        while (index < tags.Count)
        {
            if (tags[index] == featureTag)
            {
                return index;
            }

            index += 1;
        }

        return -1;
    }

    static SfntLayoutFeatureRange[] BuildRanges(
        int featureTag,
        SfntLayoutFeatureCategory category,
        SfntUserFeature[] userFeatures,
        int textLength)
    {
        if (textLength == 0)
        {
            return new SfntLayoutFeatureRange[0];
        }

        if (category == SfntLayoutFeatureCategory.Mandatory)
        {
            return new SfntLayoutFeatureRange[]
            {
                new SfntLayoutFeatureRange(0, textLength, 1)
            };
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
        int index = 0;

        while (index < userFeatures.Length)
        {
            SfntUserFeature feature = userFeatures[index];

            if (feature.GetFeatureTag() == featureTag)
            {
                int start = System.Math.Min(feature.GetStart(), textLength);
                int end = System.Math.Min(feature.GetEnd(), textLength);

                if (start < end)
                {
                    boundaries[boundaryCount] = start;
                    boundaries[boundaryCount + 1] = end;
                    boundaryCount += 2;
                }
            }

            index += 1;
        }

        System.Array.Sort(boundaries, 0, boundaryCount);
        boundaryCount = RemoveDuplicateBoundaries(
            boundaries,
            boundaryCount);
        int defaultValue = category ==
            SfntLayoutFeatureCategory.Default ? 1 : 0;
        System.Collections.Generic.List<SfntLayoutFeatureRange> ranges =
            new System.Collections.Generic.List<SfntLayoutFeatureRange>();
        index = 0;

        while (index + 1 < boundaryCount)
        {
            int start = boundaries[index];
            int end = boundaries[index + 1];
            int value = FindValue(
                featureTag,
                userFeatures,
                start,
                defaultValue);

            if (value > 0)
            {
                AddOrExtendRange(ranges, start, end, value);
            }

            index += 1;
        }

        SfntLayoutFeatureRange[] result =
            new SfntLayoutFeatureRange[ranges.Count];
        ranges.CopyTo(result, 0);
        return result;
    }

    static int RemoveDuplicateBoundaries(int[] boundaries, int count)
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

    static int FindValue(
        int featureTag,
        SfntUserFeature[] userFeatures,
        int textIndex,
        int defaultValue)
    {
        int index = userFeatures.Length - 1;

        while (index >= 0)
        {
            SfntUserFeature feature = userFeatures[index];

            if (feature.GetFeatureTag() == featureTag &&
                textIndex >= feature.GetStart() &&
                textIndex < feature.GetEnd())
            {
                return feature.GetValue();
            }

            index -= 1;
        }

        return defaultValue;
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

    static void AddSelections(
        System.Collections.Generic.List<SfntUserFeature> selections,
        int featureTag,
        SfntLayoutFeatureRange[] ranges)
    {
        int index = 0;

        while (index < ranges.Length)
        {
            SfntLayoutFeatureRange range = ranges[index];
            selections.Add(new SfntUserFeature(
                featureTag,
                range.GetValue(),
                range.GetStart(),
                range.GetEnd()));
            index += 1;
        }
    }
}
