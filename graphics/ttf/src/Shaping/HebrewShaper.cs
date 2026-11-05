namespace ttf;

internal enum HebrewShaperStageKind
{
    CompositionAndDecomposition = 0,
    DiscretionaryLigatures = 1,
    UserSubstitution = 2,
    Kerning = 3,
    MarkPositioning = 4,
    UserPositioning = 5
}

internal class HebrewShaperPreparation
{
    int presentationCompositionCount;
    int markReorderCount;

    internal HebrewShaperPreparation(
        int presentationCompositionCount,
        int markReorderCount)
    {
        this.presentationCompositionCount = presentationCompositionCount;
        this.markReorderCount = markReorderCount;
    }

    internal int GetPresentationCompositionCount()
    {
        return presentationCompositionCount;
    }

    internal int GetMarkReorderCount()
    {
        return markReorderCount;
    }
}

internal class HebrewShaperFeatureStage
{
    HebrewShaperStageKind kind;
    DefaultShaperTableKind tableKind;
    SfntLayoutFeaturePlan featurePlan;
    int textLength;

    internal HebrewShaperFeatureStage(
        HebrewShaperStageKind kind,
        DefaultShaperTableKind tableKind,
        SfntLayoutFeaturePlan featurePlan,
        int textLength)
    {
        this.kind = kind;
        this.tableKind = tableKind;
        this.featurePlan = featurePlan;
        this.textLength = textLength;
    }

    internal HebrewShaperStageKind GetKind()
    {
        return kind;
    }

    internal DefaultShaperTableKind GetTableKind()
    {
        return tableKind;
    }

    internal SfntLayoutFeaturePlan GetFeaturePlan()
    {
        return featurePlan;
    }

    internal SfntLayoutSelection CreateSelection(
        SfntLayoutTable table,
        int scriptTag,
        int languageTag)
    {
        if (table == null)
        {
            throw new System.ArgumentNullException("table");
        }

        SfntUserFeature[] selections =
            new SfntUserFeature[featurePlan.GetSelectionCount()];
        int index = 0;
        while (index < selections.Length)
        {
            selections[index] = featurePlan.GetSelection(index);
            index++;
        }

        return SfntLayoutSelector.CreateSelection(
            table,
            scriptTag,
            languageTag,
            selections,
            textLength);
    }
}

internal class HebrewShaperPlan
{
    HebrewShaperFeatureStage[] stages;

    internal HebrewShaperPlan(HebrewShaperFeatureStage[] stages)
    {
        this.stages = stages;
    }

    internal int GetStageCount()
    {
        return stages.Length;
    }

    internal HebrewShaperFeatureStage GetStage(int index)
    {
        if (index < 0 || index >= stages.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return stages[index];
    }

    internal HebrewShaperFeatureStage? FindStage(
        HebrewShaperStageKind kind)
    {
        ValidateStageKind(kind);
        int index = 0;
        while (index < stages.Length)
        {
            if (stages[index].GetKind() == kind)
            {
                return stages[index];
            }

            index++;
        }

        return null;
    }

    static void ValidateStageKind(HebrewShaperStageKind kind)
    {
        if (kind < HebrewShaperStageKind.CompositionAndDecomposition ||
            kind > HebrewShaperStageKind.UserPositioning)
        {
            throw new System.ArgumentOutOfRangeException("kind");
        }
    }
}

internal static class HebrewShaper
{
    const int CharacterCompositionTag = 0x63636D70;
    const int DiscretionaryLigaturesTag = 0x646C6967;
    const int KerningTag = 0x6B65726E;
    const int MarkPositioningTag = 0x6D61726B;
    const int MarkToMarkPositioningTag = 0x6D6B6D6B;
    const int FirstHebrewLetter = 0x05D0;
    const int LastHebrewLetter = 0x05EA;
    const int Hiriq = 0x05B4;
    const int Patah = 0x05B7;
    const int Qamats = 0x05B8;
    const int Holam = 0x05B9;
    const int Dagesh = 0x05BC;
    const int Rafe = 0x05BF;
    const int ShinDot = 0x05C1;
    const int SinDot = 0x05C2;

    static readonly int[] DageshForms = new int[]
    {
        0xFB30,
        0xFB31,
        0xFB32,
        0xFB33,
        0xFB34,
        0xFB35,
        0xFB36,
        0,
        0xFB38,
        0xFB39,
        0xFB3A,
        0xFB3B,
        0xFB3C,
        0,
        0xFB3E,
        0,
        0xFB40,
        0xFB41,
        0,
        0xFB43,
        0xFB44,
        0,
        0xFB46,
        0xFB47,
        0xFB48,
        0xFB49,
        0xFB4A
    };

    static readonly int[] BuiltInFeatureTags = new int[]
    {
        CharacterCompositionTag,
        DiscretionaryLigaturesTag,
        KerningTag,
        MarkPositioningTag,
        MarkToMarkPositioningTag
    };

    internal static HebrewShaperPreparation Prepare(
        ShapingBuffer buffer,
        UnicodeData unicodeData,
        bool hasMarkPositioning)
    {
        if (buffer == null)
        {
            throw new System.ArgumentNullException("buffer");
        }

        if (unicodeData == null)
        {
            throw new System.ArgumentNullException("unicodeData");
        }

        int compositionCount = 0;
        if (!hasMarkPositioning)
        {
            compositionCount = ComposePresentationForms(buffer);
        }

        int reorderCount = ReorderMarks(buffer, unicodeData);
        return new HebrewShaperPreparation(
            compositionCount,
            reorderCount);
    }

    internal static HebrewShaperPlan BuildPlan(
        ShapingBuffer buffer,
        SfntUserFeature[] userFeatures)
    {
        if (buffer == null)
        {
            throw new System.ArgumentNullException("buffer");
        }

        ValidateUserFeatures(userFeatures);
        int textLength = buffer.GetCount();
        HebrewShaperFeatureStage[] stages =
            new HebrewShaperFeatureStage[6];
        stages[0] = CreateStage(
            HebrewShaperStageKind.CompositionAndDecomposition,
            DefaultShaperTableKind.Substitution,
            new int[] { CharacterCompositionTag },
            new int[0],
            new int[0],
            CopyMatchingFeatures(
                userFeatures,
                new int[] { CharacterCompositionTag }),
            textLength);
        stages[1] = CreateStage(
            HebrewShaperStageKind.DiscretionaryLigatures,
            DefaultShaperTableKind.Substitution,
            new int[0],
            new int[0],
            new int[] { DiscretionaryLigaturesTag },
            CopyMatchingFeatures(
                userFeatures,
                new int[] { DiscretionaryLigaturesTag }),
            textLength);
        SfntUserFeature[] customFeatures =
            CopyCustomFeatures(userFeatures);
        stages[2] = CreateStage(
            HebrewShaperStageKind.UserSubstitution,
            DefaultShaperTableKind.Substitution,
            new int[0],
            new int[0],
            new int[0],
            customFeatures,
            textLength);
        stages[3] = CreateStage(
            HebrewShaperStageKind.Kerning,
            DefaultShaperTableKind.Positioning,
            new int[0],
            new int[] { KerningTag },
            new int[0],
            CopyMatchingFeatures(
                userFeatures,
                new int[] { KerningTag }),
            textLength);
        stages[4] = CreateStage(
            HebrewShaperStageKind.MarkPositioning,
            DefaultShaperTableKind.Positioning,
            new int[] { MarkPositioningTag },
            new int[] { MarkToMarkPositioningTag },
            new int[0],
            CopyMatchingFeatures(
                userFeatures,
                new int[]
                {
                    MarkPositioningTag,
                    MarkToMarkPositioningTag
                }),
            textLength);
        stages[5] = CreateStage(
            HebrewShaperStageKind.UserPositioning,
            DefaultShaperTableKind.Positioning,
            new int[0],
            new int[0],
            new int[0],
            customFeatures,
            textLength);
        return new HebrewShaperPlan(stages);
    }

    static int ComposePresentationForms(ShapingBuffer buffer)
    {
        int compositionCount = 0;
        int index = 0;
        while (index + 1 < buffer.GetCount())
        {
            int composed = FindPresentationForm(
                buffer.GetScalarValue(index),
                buffer.GetScalarValue(index + 1));
            if (composed == 0)
            {
                index++;
                continue;
            }

            buffer.MergeClusters(index, index + 2);
            buffer.SetRune(index, new System.Text.Rune(composed));
            buffer.ClearGlyphId(index);
            buffer.Remove(index + 1);
            compositionCount++;
        }

        return compositionCount;
    }

    static int FindPresentationForm(int first, int second)
    {
        if (second == Hiriq && first == 0x05D9)
        {
            return 0xFB1D;
        }

        if (second == Patah)
        {
            if (first == 0x05F2)
            {
                return 0xFB1F;
            }

            if (first == 0x05D0)
            {
                return 0xFB2E;
            }
        }

        if (second == Qamats && first == 0x05D0)
        {
            return 0xFB2F;
        }

        if (second == Holam && first == 0x05D5)
        {
            return 0xFB4B;
        }

        if (second == Dagesh)
        {
            if (first >= FirstHebrewLetter &&
                first <= LastHebrewLetter)
            {
                return DageshForms[first - FirstHebrewLetter];
            }

            if (first == 0xFB2A)
            {
                return 0xFB2C;
            }

            if (first == 0xFB2B)
            {
                return 0xFB2D;
            }
        }

        if (second == Rafe)
        {
            if (first == 0x05D1)
            {
                return 0xFB4C;
            }

            if (first == 0x05DB)
            {
                return 0xFB4D;
            }

            if (first == 0x05E4)
            {
                return 0xFB4E;
            }
        }

        if (second == ShinDot)
        {
            if (first == 0x05E9)
            {
                return 0xFB2A;
            }

            if (first == 0xFB49)
            {
                return 0xFB2C;
            }
        }

        if (second == SinDot)
        {
            if (first == 0x05E9)
            {
                return 0xFB2B;
            }

            if (first == 0xFB49)
            {
                return 0xFB2D;
            }
        }

        return 0;
    }

    static int ReorderMarks(
        ShapingBuffer buffer,
        UnicodeData unicodeData)
    {
        int reorderCount = 0;
        int index = 2;
        while (index < buffer.GetCount())
        {
            int firstClass = GetCombiningClass(
                buffer,
                unicodeData,
                index - 2);
            int secondClass = GetCombiningClass(
                buffer,
                unicodeData,
                index - 1);
            int thirdClass = GetCombiningClass(
                buffer,
                unicodeData,
                index);
            bool firstMatches = firstClass == 17 || firstClass == 18;
            bool secondMatches = secondClass == 10 || secondClass == 14;
            bool thirdMatches = thirdClass == 22 || thirdClass == 220;
            if (firstMatches && secondMatches && thirdMatches)
            {
                buffer.MergeClusters(index - 1, index + 1);
                SwapItems(buffer, index - 1, index);
                reorderCount++;
            }

            index++;
        }

        return reorderCount;
    }

    static int GetCombiningClass(
        ShapingBuffer buffer,
        UnicodeData unicodeData,
        int index)
    {
        string value = unicodeData.GetPropertyName(
            UnicodeDataTags.CanonicalCombiningClass,
            buffer.GetScalarValue(index));
        return System.Int32.Parse(
            value,
            System.Globalization.CultureInfo.InvariantCulture);
    }

    static void SwapItems(
        ShapingBuffer buffer,
        int firstIndex,
        int secondIndex)
    {
        int[] sourceIndexes = new int[buffer.GetCount()];
        int index = 0;
        while (index < sourceIndexes.Length)
        {
            sourceIndexes[index] = index;
            index++;
        }

        sourceIndexes[firstIndex] = secondIndex;
        sourceIndexes[secondIndex] = firstIndex;
        buffer.Reorder(sourceIndexes);
    }

    static HebrewShaperFeatureStage CreateStage(
        HebrewShaperStageKind kind,
        DefaultShaperTableKind tableKind,
        int[] mandatoryTags,
        int[] defaultTags,
        int[] discretionaryTags,
        SfntUserFeature[] userFeatures,
        int textLength)
    {
        SfntLayoutFeaturePlan plan = SfntLayoutFeaturePlanBuilder.Build(
            mandatoryTags,
            defaultTags,
            discretionaryTags,
            userFeatures,
            textLength);
        return new HebrewShaperFeatureStage(
            kind,
            tableKind,
            plan,
            textLength);
    }

    static SfntUserFeature[] CopyMatchingFeatures(
        SfntUserFeature[] features,
        int[] tags)
    {
        int count = CountFeatures(features, tags, true);
        SfntUserFeature[] result = new SfntUserFeature[count];
        int index = 0;
        int resultIndex = 0;
        while (index < features.Length)
        {
            if (ContainsTag(tags, features[index].GetFeatureTag()))
            {
                result[resultIndex] = features[index];
                resultIndex++;
            }

            index++;
        }

        return result;
    }

    static SfntUserFeature[] CopyCustomFeatures(
        SfntUserFeature[] features)
    {
        int count = CountFeatures(
            features,
            BuiltInFeatureTags,
            false);
        SfntUserFeature[] result = new SfntUserFeature[count];
        int index = 0;
        int resultIndex = 0;
        while (index < features.Length)
        {
            if (!ContainsTag(
                BuiltInFeatureTags,
                features[index].GetFeatureTag()))
            {
                result[resultIndex] = features[index];
                resultIndex++;
            }

            index++;
        }

        return result;
    }

    static int CountFeatures(
        SfntUserFeature[] features,
        int[] tags,
        bool matching)
    {
        int count = 0;
        int index = 0;
        while (index < features.Length)
        {
            bool contains = ContainsTag(
                tags,
                features[index].GetFeatureTag());
            if (contains == matching)
            {
                count++;
            }

            index++;
        }

        return count;
    }

    static bool ContainsTag(int[] tags, int tag)
    {
        int index = 0;
        while (index < tags.Length)
        {
            if (tags[index] == tag)
            {
                return true;
            }

            index++;
        }

        return false;
    }

    static void ValidateUserFeatures(SfntUserFeature[] userFeatures)
    {
        if (userFeatures == null)
        {
            throw new System.ArgumentNullException("userFeatures");
        }

        int index = 0;
        while (index < userFeatures.Length)
        {
            if (userFeatures[index] == null)
            {
                throw new System.ArgumentException(
                    "A user feature cannot be null.");
            }

            index++;
        }
    }
}
