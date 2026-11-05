namespace ttf;

internal enum KhmerDottedCirclePolicy
{
    InsertForBrokenClusters = 0,
    PreserveBrokenClusters = 1
}

internal enum KhmerShaperStageKind
{
    Reordering = 0,
    LocalizedForms = 1,
    CompositionAndDecomposition = 2,
    BasicForms = 3,
    PresentationForms = 4,
    ContextualLigatures = 5,
    UserSubstitution = 6,
    Distances = 7,
    MarkPositioning = 8,
    Kerning = 9,
    UserPositioning = 10
}

internal class KhmerShaperPreparation : ITextShapingBarrier
{
    ShapingBuffer preparedBuffer;
    KhmerSyllableMap syllableMap;
    int[] sourceIndexes;
    bool[] prebaseFormValues;
    bool[] belowbaseFormValues;
    bool[] abovebaseFormValues;
    bool[] postbaseFormValues;
    bool[] consonantAfterRoValues;
    int splitVowelCount;
    int dottedCircleCount;
    bool reorderingApplied;

    internal KhmerShaperPreparation(
        ShapingBuffer preparedBuffer,
        KhmerSyllableMap syllableMap,
        int[] sourceIndexes,
        bool[] prebaseFormValues,
        bool[] belowbaseFormValues,
        bool[] abovebaseFormValues,
        bool[] postbaseFormValues,
        bool[] consonantAfterRoValues,
        int splitVowelCount,
        int dottedCircleCount)
    {
        this.preparedBuffer = preparedBuffer;
        this.syllableMap = syllableMap;
        this.sourceIndexes = sourceIndexes;
        this.prebaseFormValues = prebaseFormValues;
        this.belowbaseFormValues = belowbaseFormValues;
        this.abovebaseFormValues = abovebaseFormValues;
        this.postbaseFormValues = postbaseFormValues;
        this.consonantAfterRoValues = consonantAfterRoValues;
        this.splitVowelCount = splitVowelCount;
        this.dottedCircleCount = dottedCircleCount;
    }

    internal KhmerSyllableMap GetSyllableMap()
    {
        return syllableMap;
    }

    internal int GetSplitVowelCount()
    {
        return splitVowelCount;
    }

    internal int GetInsertedDottedCircleCount()
    {
        return dottedCircleCount;
    }

    internal int GetSourceIndex(int index)
    {
        ValidateIndex(index);
        return sourceIndexes[index];
    }

    internal KhmerSyllabicCategory GetCategory(int index)
    {
        return syllableMap.GetCategory(GetSourceIndex(index));
    }

    internal bool IsReorderingApplied()
    {
        return reorderingApplied;
    }

    internal bool IsPrebaseFormEnabled(int index)
    {
        ValidateIndex(index);
        return prebaseFormValues[index];
    }

    internal bool IsBelowbaseFormEnabled(int index)
    {
        ValidateIndex(index);
        return belowbaseFormValues[index];
    }

    internal bool IsAbovebaseFormEnabled(int index)
    {
        ValidateIndex(index);
        return abovebaseFormValues[index];
    }

    internal bool IsPostbaseFormEnabled(int index)
    {
        ValidateIndex(index);
        return postbaseFormValues[index];
    }

    internal bool IsConsonantAfterRoEnabled(int index)
    {
        ValidateIndex(index);
        return consonantAfterRoValues[index];
    }

    /// <summary>Applies the prepared Khmer reordering barrier.</summary>
    /// <param name="buffer">The shaping buffer to reorder.</param>
    public void Apply(ShapingBuffer buffer)
    {
        ApplyReordering(buffer);
    }

    internal void ApplyReordering(ShapingBuffer buffer)
    {
        if (buffer == null)
        {
            throw new System.ArgumentNullException("buffer");
        }

        if (reorderingApplied)
        {
            throw new System.InvalidOperationException(
                "Khmer reordering has already been applied.");
        }

        if (!object.ReferenceEquals(buffer, preparedBuffer) ||
            buffer.GetCount() != sourceIndexes.Length)
        {
            throw new System.ArgumentException(
                "The shaping buffer no longer matches the Khmer preparation.");
        }

        int[] permutation = new int[sourceIndexes.Length];
        System.Array.Copy(sourceIndexes, permutation, sourceIndexes.Length);
        buffer.Reorder(permutation);
        int syllableIndex = 0;
        while (syllableIndex < syllableMap.GetSyllableCount())
        {
            int start = syllableMap.GetSyllableStart(syllableIndex);
            int end = syllableMap.GetSyllableEnd(syllableIndex);
            bool changed = false;
            int index = start;
            while (index < end)
            {
                changed = changed || sourceIndexes[index] != index;
                buffer.SetUnsafeToBreak(index, true);
                index++;
            }

            if (changed)
            {
                buffer.MergeClusters(start, end);
            }

            syllableIndex++;
        }

        reorderingApplied = true;
    }

    void ValidateIndex(int index)
    {
        if (index < 0 || index >= sourceIndexes.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }
    }
}

internal class KhmerShaperFeatureStage
{
    KhmerShaperStageKind kind;
    DefaultShaperTableKind tableKind;
    SfntLayoutFeaturePlan featurePlan;
    int textLength;
    bool reorderingBarrier;

    internal KhmerShaperFeatureStage(
        KhmerShaperStageKind kind,
        DefaultShaperTableKind tableKind,
        SfntLayoutFeaturePlan featurePlan,
        int textLength,
        bool reorderingBarrier)
    {
        this.kind = kind;
        this.tableKind = tableKind;
        this.featurePlan = featurePlan;
        this.textLength = textLength;
        this.reorderingBarrier = reorderingBarrier;
    }

    internal KhmerShaperStageKind GetKind()
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

    internal bool IsReorderingBarrier()
    {
        return reorderingBarrier;
    }

    internal SfntLayoutSelection CreateSelection(
        SfntLayoutTable table,
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
            KhmerShaper.ScriptTag,
            languageTag,
            selections,
            textLength);
    }
}

internal class KhmerShaperPlan
{
    KhmerShaperPreparation preparation;
    KhmerShaperFeatureStage[] stages;

    internal KhmerShaperPlan(
        KhmerShaperPreparation preparation,
        KhmerShaperFeatureStage[] stages)
    {
        this.preparation = preparation;
        this.stages = stages;
    }

    internal int GetScriptTag()
    {
        return KhmerShaper.ScriptTag;
    }

    internal KhmerShaperPreparation GetPreparation()
    {
        return preparation;
    }

    internal int GetStageCount()
    {
        return stages.Length;
    }

    internal KhmerShaperFeatureStage GetStage(int index)
    {
        if (index < 0 || index >= stages.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return stages[index];
    }

    internal KhmerShaperFeatureStage? FindStage(KhmerShaperStageKind kind)
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

    static void ValidateStageKind(KhmerShaperStageKind kind)
    {
        if (kind < KhmerShaperStageKind.Reordering ||
            kind > KhmerShaperStageKind.UserPositioning)
        {
            throw new System.ArgumentOutOfRangeException("kind");
        }
    }
}

internal static class KhmerShaper
{
    internal const int ScriptTag = 0x6B686D72;

    const int AbovebaseFormsTag = 0x61627666;
    const int AbovebaseSubstitutionsTag = 0x61627673;
    const int AboveMarkPositioningTag = 0x6162766D;
    const int BelowbaseFormsTag = 0x626C7766;
    const int BelowbaseSubstitutionsTag = 0x626C7773;
    const int BelowMarkPositioningTag = 0x626C776D;
    const int CharacterCompositionTag = 0x63636D70;
    const int ConsonantAfterRoTag = 0x63666172;
    const int ContextualLigaturesTag = 0x636C6967;
    const int DistancesTag = 0x64697374;
    const int KerningTag = 0x6B65726E;
    const int LocalizedFormsTag = 0x6C6F636C;
    const int MarkPositioningTag = 0x6D61726B;
    const int MarkToMarkPositioningTag = 0x6D6B6D6B;
    const int PostbaseFormsTag = 0x70737466;
    const int PostbaseSubstitutionsTag = 0x70737473;
    const int PrebaseFormsTag = 0x70726566;
    const int PresentationFormsTag = 0x70726573;

    static readonly int[] BuiltInFeatureTags = new int[]
    {
        LocalizedFormsTag,
        CharacterCompositionTag,
        PrebaseFormsTag,
        BelowbaseFormsTag,
        AbovebaseFormsTag,
        PostbaseFormsTag,
        ConsonantAfterRoTag,
        PresentationFormsTag,
        AbovebaseSubstitutionsTag,
        BelowbaseSubstitutionsTag,
        PostbaseSubstitutionsTag,
        ContextualLigaturesTag,
        DistancesTag,
        AboveMarkPositioningTag,
        BelowMarkPositioningTag,
        KerningTag,
        MarkPositioningTag,
        MarkToMarkPositioningTag
    };

    internal static KhmerShaperPreparation Prepare(
        ShapingBuffer buffer,
        UnicodeData unicodeData,
        KhmerDottedCirclePolicy dottedCirclePolicy,
        int maximumSyllableCount)
    {
        if (buffer == null)
        {
            throw new System.ArgumentNullException("buffer");
        }

        if (unicodeData == null)
        {
            throw new System.ArgumentNullException("unicodeData");
        }

        ValidateDottedCirclePolicy(dottedCirclePolicy);
        if (maximumSyllableCount < 0)
        {
            throw new System.ArgumentOutOfRangeException(
                "maximumSyllableCount");
        }

        int splitVowelCount = DecomposeSplitVowels(buffer);
        KhmerSyllableMap map = KhmerSyllableRecognizer.Recognize(
            buffer,
            unicodeData,
            maximumSyllableCount);
        int dottedCircleCount = 0;
        if (dottedCirclePolicy ==
            KhmerDottedCirclePolicy.InsertForBrokenClusters)
        {
            dottedCircleCount = InsertDottedCircles(buffer, map);
            if (dottedCircleCount > 0)
            {
                map = KhmerSyllableRecognizer.Recognize(
                    buffer,
                    unicodeData,
                    maximumSyllableCount);
            }
        }

        int[] sourceIndexes = CreateIdentity(buffer.GetCount());
        bool[] prebaseSources = new bool[buffer.GetCount()];
        bool[] cfarSources = new bool[buffer.GetCount()];
        BuildReordering(map, sourceIndexes, prebaseSources, cfarSources);
        bool[] prefValues = MapValues(sourceIndexes, prebaseSources);
        bool[] cfarValues = MapValues(sourceIndexes, cfarSources);
        bool[] postbaseSources = CreatePostbaseSourceValues(map);
        bool[] postbaseValues = MapValues(
            sourceIndexes,
            postbaseSources);
        return new KhmerShaperPreparation(
            buffer,
            map,
            sourceIndexes,
            prefValues,
            CopyBooleans(postbaseValues),
            CopyBooleans(postbaseValues),
            CopyBooleans(postbaseValues),
            cfarValues,
            splitVowelCount,
            dottedCircleCount);
    }

    internal static KhmerShaperPlan BuildPlan(
        KhmerShaperPreparation preparation,
        SfntUserFeature[] userFeatures)
    {
        if (preparation == null)
        {
            throw new System.ArgumentNullException("preparation");
        }

        ValidateUserFeatures(userFeatures);
        int textLength = preparation.GetSyllableMap().GetRuneCount();
        KhmerShaperFeatureStage[] stages =
            new KhmerShaperFeatureStage[11];
        stages[0] = CreateBarrierStage(
            KhmerShaperStageKind.Reordering,
            textLength);
        stages[1] = CreateGlobalStage(
            KhmerShaperStageKind.LocalizedForms,
            DefaultShaperTableKind.Substitution,
            new int[] { LocalizedFormsTag },
            userFeatures,
            textLength);
        stages[2] = CreateGlobalStage(
            KhmerShaperStageKind.CompositionAndDecomposition,
            DefaultShaperTableKind.Substitution,
            new int[] { CharacterCompositionTag },
            userFeatures,
            textLength);
        stages[3] = CreateBasicStage(preparation, textLength);
        stages[4] = CreateGlobalStage(
            KhmerShaperStageKind.PresentationForms,
            DefaultShaperTableKind.Substitution,
            new int[]
            {
                PresentationFormsTag,
                AbovebaseSubstitutionsTag,
                BelowbaseSubstitutionsTag,
                PostbaseSubstitutionsTag
            },
            userFeatures,
            textLength);
        stages[5] = CreateGlobalStage(
            KhmerShaperStageKind.ContextualLigatures,
            DefaultShaperTableKind.Substitution,
            new int[] { ContextualLigaturesTag },
            userFeatures,
            textLength);
        SfntUserFeature[] customFeatures =
            CopyCustomFeatures(userFeatures);
        stages[6] = CreatePolicyStage(
            KhmerShaperStageKind.UserSubstitution,
            DefaultShaperTableKind.Substitution,
            new int[0],
            new int[0],
            customFeatures,
            textLength,
            false);
        stages[7] = CreateGlobalStage(
            KhmerShaperStageKind.Distances,
            DefaultShaperTableKind.Positioning,
            new int[] { DistancesTag },
            userFeatures,
            textLength);
        stages[8] = CreateGlobalStage(
            KhmerShaperStageKind.MarkPositioning,
            DefaultShaperTableKind.Positioning,
            new int[]
            {
                AboveMarkPositioningTag,
                BelowMarkPositioningTag,
                MarkPositioningTag,
                MarkToMarkPositioningTag
            },
            userFeatures,
            textLength);
        stages[9] = CreateGlobalStage(
            KhmerShaperStageKind.Kerning,
            DefaultShaperTableKind.Positioning,
            new int[] { KerningTag },
            userFeatures,
            textLength);
        stages[10] = CreatePolicyStage(
            KhmerShaperStageKind.UserPositioning,
            DefaultShaperTableKind.Positioning,
            new int[0],
            new int[0],
            customFeatures,
            textLength,
            false);
        return new KhmerShaperPlan(preparation, stages);
    }

    static int DecomposeSplitVowels(ShapingBuffer buffer)
    {
        int count = 0;
        int index = 0;
        while (index < buffer.GetCount())
        {
            int codePoint = buffer.GetScalarValue(index);
            if (codePoint == 0x17BE || codePoint == 0x17BF ||
                codePoint == 0x17C0 || codePoint == 0x17C4 ||
                codePoint == 0x17C5)
            {
                buffer.InsertRune(
                    index,
                    new System.Text.Rune(0x17C1),
                    index);
                count++;
                index += 2;
            }
            else
            {
                index++;
            }
        }

        return count;
    }

    static int InsertDottedCircles(
        ShapingBuffer buffer,
        KhmerSyllableMap map)
    {
        int count = 0;
        int syllableIndex = map.GetSyllableCount() - 1;
        while (syllableIndex >= 0)
        {
            if (map.GetSyllableType(syllableIndex) ==
                KhmerSyllableType.Broken)
            {
                int start = map.GetSyllableStart(syllableIndex);
                buffer.InsertRune(
                    start,
                    new System.Text.Rune(0x25CC),
                    start);
                count++;
            }

            syllableIndex--;
        }

        return count;
    }

    static void BuildReordering(
        KhmerSyllableMap map,
        int[] sourceIndexes,
        bool[] prebaseSources,
        bool[] cfarSources)
    {
        int syllableIndex = 0;
        while (syllableIndex < map.GetSyllableCount())
        {
            KhmerSyllableType type = map.GetSyllableType(syllableIndex);
            if (type != KhmerSyllableType.NonKhmer)
            {
                int start = map.GetSyllableStart(syllableIndex);
                int end = map.GetSyllableEnd(syllableIndex);
                System.Collections.Generic.List<int> order =
                    new System.Collections.Generic.List<int>();
                int index = start;
                while (index < end)
                {
                    order.Add(index);
                    index++;
                }

                ReorderCoengRa(
                    map,
                    order,
                    start,
                    end,
                    prebaseSources,
                    cfarSources);
                ReorderPreVowels(map, order, start, end);
                index = 0;
                while (index < order.Count)
                {
                    sourceIndexes[start + index] = order[index];
                    index++;
                }
            }

            syllableIndex++;
        }
    }

    static void ReorderCoengRa(
        KhmerSyllableMap map,
        System.Collections.Generic.List<int> order,
        int start,
        int end,
        bool[] prebaseSources,
        bool[] cfarSources)
    {
        int coengCount = 0;
        int sourceIndex = start + 1;
        while (sourceIndex + 1 < end)
        {
            if (map.GetCategory(sourceIndex) ==
                KhmerSyllabicCategory.Coeng)
            {
                coengCount++;
                if (coengCount <= 3 && map.GetCategory(sourceIndex + 1) ==
                    KhmerSyllabicCategory.Ra)
                {
                    RemoveValue(order, sourceIndex);
                    RemoveValue(order, sourceIndex + 1);
                    order.Insert(0, sourceIndex + 1);
                    order.Insert(0, sourceIndex);
                    prebaseSources[sourceIndex] = true;
                    prebaseSources[sourceIndex + 1] = true;
                    int following = sourceIndex + 2;
                    while (following < end)
                    {
                        cfarSources[following] = true;
                        following++;
                    }

                    return;
                }
            }

            sourceIndex++;
        }
    }

    static void ReorderPreVowels(
        KhmerSyllableMap map,
        System.Collections.Generic.List<int> order,
        int start,
        int end)
    {
        int sourceIndex = start + 1;
        while (sourceIndex < end)
        {
            if (map.GetCategory(sourceIndex) ==
                KhmerSyllabicCategory.VowelPre)
            {
                RemoveValue(order, sourceIndex);
                order.Insert(0, sourceIndex);
            }

            sourceIndex++;
        }
    }

    static bool[] CreatePostbaseSourceValues(KhmerSyllableMap map)
    {
        bool[] values = new bool[map.GetRuneCount()];
        int syllableIndex = 0;
        while (syllableIndex < map.GetSyllableCount())
        {
            if (map.GetSyllableType(syllableIndex) !=
                KhmerSyllableType.NonKhmer)
            {
                int index = map.GetSyllableStart(syllableIndex) + 1;
                int end = map.GetSyllableEnd(syllableIndex);
                while (index < end)
                {
                    values[index] = true;
                    index++;
                }
            }

            syllableIndex++;
        }

        return values;
    }

    static KhmerShaperFeatureStage CreateBasicStage(
        KhmerShaperPreparation preparation,
        int textLength)
    {
        System.Collections.Generic.List<SfntUserFeature> ranges =
            new System.Collections.Generic.List<SfntUserFeature>();
        AddRanges(ranges, PrebaseFormsTag, preparation, 0);
        AddRanges(ranges, BelowbaseFormsTag, preparation, 1);
        AddRanges(ranges, AbovebaseFormsTag, preparation, 2);
        AddRanges(ranges, PostbaseFormsTag, preparation, 3);
        AddRanges(ranges, ConsonantAfterRoTag, preparation, 4);
        SfntUserFeature[] features = new SfntUserFeature[ranges.Count];
        ranges.CopyTo(features, 0);
        return CreatePolicyStage(
            KhmerShaperStageKind.BasicForms,
            DefaultShaperTableKind.Substitution,
            new int[0],
            new int[]
            {
                PrebaseFormsTag,
                BelowbaseFormsTag,
                AbovebaseFormsTag,
                PostbaseFormsTag,
                ConsonantAfterRoTag
            },
            features,
            textLength,
            false);
    }

    static void AddRanges(
        System.Collections.Generic.List<SfntUserFeature> ranges,
        int tag,
        KhmerShaperPreparation preparation,
        int kind)
    {
        int count = preparation.GetSyllableMap().GetRuneCount();
        int index = 0;
        while (index < count)
        {
            if (!GetFeatureValue(preparation, index, kind))
            {
                index++;
                continue;
            }

            int start = index;
            while (index < count &&
                GetFeatureValue(preparation, index, kind))
            {
                index++;
            }

            ranges.Add(new SfntUserFeature(tag, 1, start, index));
        }
    }

    static bool GetFeatureValue(
        KhmerShaperPreparation preparation,
        int index,
        int kind)
    {
        if (kind == 0)
        {
            return preparation.IsPrebaseFormEnabled(index);
        }

        if (kind == 1)
        {
            return preparation.IsBelowbaseFormEnabled(index);
        }

        if (kind == 2)
        {
            return preparation.IsAbovebaseFormEnabled(index);
        }

        if (kind == 3)
        {
            return preparation.IsPostbaseFormEnabled(index);
        }

        return preparation.IsConsonantAfterRoEnabled(index);
    }

    static KhmerShaperFeatureStage CreateGlobalStage(
        KhmerShaperStageKind kind,
        DefaultShaperTableKind tableKind,
        int[] tags,
        SfntUserFeature[] userFeatures,
        int textLength)
    {
        return CreatePolicyStage(
            kind,
            tableKind,
            tags,
            new int[0],
            CopyMatchingFeatures(userFeatures, tags),
            textLength,
            false);
    }

    static KhmerShaperFeatureStage CreateBarrierStage(
        KhmerShaperStageKind kind,
        int textLength)
    {
        return CreatePolicyStage(
            kind,
            DefaultShaperTableKind.Substitution,
            new int[0],
            new int[0],
            new SfntUserFeature[0],
            textLength,
            true);
    }

    static KhmerShaperFeatureStage CreatePolicyStage(
        KhmerShaperStageKind kind,
        DefaultShaperTableKind tableKind,
        int[] mandatoryTags,
        int[] discretionaryTags,
        SfntUserFeature[] features,
        int textLength,
        bool barrier)
    {
        SfntLayoutFeaturePlan plan = SfntLayoutFeaturePlanBuilder.Build(
            mandatoryTags,
            new int[0],
            discretionaryTags,
            features,
            textLength);
        return new KhmerShaperFeatureStage(
            kind,
            tableKind,
            plan,
            textLength,
            barrier);
    }

    static bool[] MapValues(int[] sourceIndexes, bool[] sourceValues)
    {
        bool[] result = new bool[sourceIndexes.Length];
        int index = 0;
        while (index < sourceIndexes.Length)
        {
            result[index] = sourceValues[sourceIndexes[index]];
            index++;
        }

        return result;
    }

    static bool[] CopyBooleans(bool[] values)
    {
        bool[] result = new bool[values.Length];
        System.Array.Copy(values, result, values.Length);
        return result;
    }

    static int[] CreateIdentity(int count)
    {
        int[] result = new int[count];
        int index = 0;
        while (index < count)
        {
            result[index] = index;
            index++;
        }

        return result;
    }

    static void RemoveValue(
        System.Collections.Generic.List<int> values,
        int value)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (values[index] == value)
            {
                values.RemoveAt(index);
                return;
            }

            index++;
        }
    }

    static SfntUserFeature[] CopyMatchingFeatures(
        SfntUserFeature[] features,
        int[] tags)
    {
        int count = CountMatchingFeatures(features, tags, true);
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
        int count = CountMatchingFeatures(
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

    static int CountMatchingFeatures(
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

    static void ValidateDottedCirclePolicy(
        KhmerDottedCirclePolicy policy)
    {
        if (policy != KhmerDottedCirclePolicy.InsertForBrokenClusters &&
            policy != KhmerDottedCirclePolicy.PreserveBrokenClusters)
        {
            throw new System.ArgumentOutOfRangeException(
                "dottedCirclePolicy");
        }
    }

    static void ValidateUserFeatures(SfntUserFeature[] features)
    {
        if (features == null)
        {
            throw new System.ArgumentNullException("userFeatures");
        }

        int index = 0;
        while (index < features.Length)
        {
            if (features[index] == null)
            {
                throw new System.ArgumentException(
                    "A user feature cannot be null.");
            }

            index++;
        }
    }
}
