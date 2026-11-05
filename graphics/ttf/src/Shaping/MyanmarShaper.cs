namespace ttf;

internal enum MyanmarDottedCirclePolicy
{
    InsertForBrokenClusters = 0,
    PreserveBrokenClusters = 1
}

internal enum MyanmarShaperStageKind
{
    LocalizedForms = 0,
    CompositionAndDecomposition = 1,
    Reordering = 2,
    RephForms = 3,
    PrebaseForms = 4,
    BelowbaseForms = 5,
    PostbaseForms = 6,
    PresentationForms = 7,
    UserSubstitution = 8,
    Kerning = 9,
    Distances = 10,
    MarkPositioning = 11,
    UserPositioning = 12
}

internal enum MyanmarShapingPosition
{
    PreMatra = 2,
    PreConsonant = 3,
    BaseConsonant = 4,
    AfterMain = 5,
    BeforeSubjoined = 7,
    BelowConsonant = 8,
    AfterSubjoined = 9
}

internal class MyanmarShaperPreparation : ITextShapingBarrier
{
    ShapingBuffer preparedBuffer;
    MyanmarSyllableMap syllableMap;
    int[] sourceIndexes;
    MyanmarShapingPosition[] sourcePositions;
    int[] baseSourceIndexes;
    bool[] kinziValues;
    int dottedCircleCount;
    bool reorderingApplied;

    internal MyanmarShaperPreparation(
        ShapingBuffer preparedBuffer,
        MyanmarSyllableMap syllableMap,
        int[] sourceIndexes,
        MyanmarShapingPosition[] sourcePositions,
        int[] baseSourceIndexes,
        bool[] kinziValues,
        int dottedCircleCount)
    {
        this.preparedBuffer = preparedBuffer;
        this.syllableMap = syllableMap;
        this.sourceIndexes = sourceIndexes;
        this.sourcePositions = sourcePositions;
        this.baseSourceIndexes = baseSourceIndexes;
        this.kinziValues = kinziValues;
        this.dottedCircleCount = dottedCircleCount;
    }

    internal MyanmarSyllableMap GetSyllableMap()
    {
        return syllableMap;
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

    internal MyanmarSyllabicCategory GetCategory(int index)
    {
        return syllableMap.GetCategory(GetSourceIndex(index));
    }

    internal MyanmarShapingPosition GetPosition(int index)
    {
        ValidateIndex(index);
        return sourcePositions[sourceIndexes[index]];
    }

    internal int GetBaseSourceIndex(int syllableIndex)
    {
        ValidateSyllableIndex(syllableIndex);
        return baseSourceIndexes[syllableIndex];
    }

    internal bool HasKinzi(int syllableIndex)
    {
        ValidateSyllableIndex(syllableIndex);
        return kinziValues[syllableIndex];
    }

    internal bool IsReorderingApplied()
    {
        return reorderingApplied;
    }

    /// <summary>Applies the prepared Myanmar reordering barrier.</summary>
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
                "Myanmar reordering has already been applied.");
        }

        if (!object.ReferenceEquals(buffer, preparedBuffer) ||
            buffer.GetCount() != sourceIndexes.Length)
        {
            throw new System.ArgumentException(
                "The shaping buffer no longer matches the Myanmar preparation.");
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

    void ValidateSyllableIndex(int syllableIndex)
    {
        if (syllableIndex < 0 ||
            syllableIndex >= baseSourceIndexes.Length)
        {
            throw new System.ArgumentOutOfRangeException("syllableIndex");
        }
    }
}

internal class MyanmarShaperFeatureStage
{
    MyanmarShaperStageKind kind;
    DefaultShaperTableKind tableKind;
    SfntLayoutFeaturePlan featurePlan;
    int textLength;
    bool reorderingBarrier;

    internal MyanmarShaperFeatureStage(
        MyanmarShaperStageKind kind,
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

    internal MyanmarShaperStageKind GetKind()
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
            MyanmarShaper.ScriptTag,
            languageTag,
            selections,
            textLength);
    }
}

internal class MyanmarShaperPlan
{
    MyanmarShaperPreparation preparation;
    MyanmarShaperFeatureStage[] stages;

    internal MyanmarShaperPlan(
        MyanmarShaperPreparation preparation,
        MyanmarShaperFeatureStage[] stages)
    {
        this.preparation = preparation;
        this.stages = stages;
    }

    internal int GetScriptTag()
    {
        return MyanmarShaper.ScriptTag;
    }

    internal MyanmarShaperPreparation GetPreparation()
    {
        return preparation;
    }

    internal int GetStageCount()
    {
        return stages.Length;
    }

    internal MyanmarShaperFeatureStage GetStage(int index)
    {
        if (index < 0 || index >= stages.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return stages[index];
    }

    internal MyanmarShaperFeatureStage? FindStage(
        MyanmarShaperStageKind kind)
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

    static void ValidateStageKind(MyanmarShaperStageKind kind)
    {
        if (kind < MyanmarShaperStageKind.LocalizedForms ||
            kind > MyanmarShaperStageKind.UserPositioning)
        {
            throw new System.ArgumentOutOfRangeException("kind");
        }
    }
}

internal static class MyanmarShaper
{
    internal const int ScriptTag = 0x6D796D32;

    const int AbovebaseSubstitutionsTag = 0x61627673;
    const int CharacterCompositionTag = 0x63636D70;
    const int BelowbaseFormsTag = 0x626C7766;
    const int BelowbaseSubstitutionsTag = 0x626C7773;
    const int DistancesTag = 0x64697374;
    const int KerningTag = 0x6B65726E;
    const int LocalizedFormsTag = 0x6C6F636C;
    const int MarkPositioningTag = 0x6D61726B;
    const int MarkToMarkPositioningTag = 0x6D6B6D6B;
    const int PostbaseFormsTag = 0x70737466;
    const int PostbaseSubstitutionsTag = 0x70737473;
    const int PrebaseFormsTag = 0x70726566;
    const int PresentationFormsTag = 0x70726573;
    const int RephFormsTag = 0x72706866;

    static readonly int[] BuiltInFeatureTags = new int[]
    {
        LocalizedFormsTag,
        CharacterCompositionTag,
        RephFormsTag,
        PrebaseFormsTag,
        BelowbaseFormsTag,
        PostbaseFormsTag,
        PresentationFormsTag,
        AbovebaseSubstitutionsTag,
        BelowbaseSubstitutionsTag,
        PostbaseSubstitutionsTag,
        DistancesTag,
        MarkPositioningTag,
        MarkToMarkPositioningTag,
        KerningTag
    };

    internal static MyanmarShaperPreparation Prepare(
        ShapingBuffer buffer,
        UnicodeData unicodeData,
        MyanmarDottedCirclePolicy dottedCirclePolicy,
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

        MyanmarSyllableMap map = MyanmarSyllableRecognizer.Recognize(
            buffer,
            unicodeData,
            maximumSyllableCount);
        int dottedCircleCount = 0;
        if (dottedCirclePolicy ==
            MyanmarDottedCirclePolicy.InsertForBrokenClusters)
        {
            dottedCircleCount = InsertDottedCircles(buffer, map);
            if (dottedCircleCount > 0)
            {
                map = ExpandMapForDottedCircles(buffer, map);
            }
        }

        int[] sourceIndexes = CreateIdentity(buffer.GetCount());
        MyanmarShapingPosition[] positions =
            new MyanmarShapingPosition[buffer.GetCount()];
        int[] baseIndexes = new int[map.GetSyllableCount()];
        bool[] kinziValues = new bool[map.GetSyllableCount()];
        InitializeBaseIndexes(baseIndexes);
        BuildReordering(
            map,
            sourceIndexes,
            positions,
            baseIndexes,
            kinziValues);
        return new MyanmarShaperPreparation(
            buffer,
            map,
            sourceIndexes,
            positions,
            baseIndexes,
            kinziValues,
            dottedCircleCount);
    }

    internal static MyanmarShaperPlan BuildPlan(
        MyanmarShaperPreparation preparation,
        SfntUserFeature[] userFeatures)
    {
        if (preparation == null)
        {
            throw new System.ArgumentNullException("preparation");
        }

        ValidateUserFeatures(userFeatures);
        int textLength = preparation.GetSyllableMap().GetRuneCount();
        MyanmarShaperFeatureStage[] stages =
            new MyanmarShaperFeatureStage[13];
        stages[0] = CreateGlobalStage(
            MyanmarShaperStageKind.LocalizedForms,
            DefaultShaperTableKind.Substitution,
            new int[] { LocalizedFormsTag },
            userFeatures,
            textLength);
        stages[1] = CreateGlobalStage(
            MyanmarShaperStageKind.CompositionAndDecomposition,
            DefaultShaperTableKind.Substitution,
            new int[] { CharacterCompositionTag },
            userFeatures,
            textLength);
        stages[2] = CreateBarrierStage(
            MyanmarShaperStageKind.Reordering,
            textLength);
        stages[3] = CreateGlobalStage(
            MyanmarShaperStageKind.RephForms,
            DefaultShaperTableKind.Substitution,
            new int[] { RephFormsTag },
            userFeatures,
            textLength);
        stages[4] = CreateGlobalStage(
            MyanmarShaperStageKind.PrebaseForms,
            DefaultShaperTableKind.Substitution,
            new int[] { PrebaseFormsTag },
            userFeatures,
            textLength);
        stages[5] = CreateGlobalStage(
            MyanmarShaperStageKind.BelowbaseForms,
            DefaultShaperTableKind.Substitution,
            new int[] { BelowbaseFormsTag },
            userFeatures,
            textLength);
        stages[6] = CreateGlobalStage(
            MyanmarShaperStageKind.PostbaseForms,
            DefaultShaperTableKind.Substitution,
            new int[] { PostbaseFormsTag },
            userFeatures,
            textLength);
        stages[7] = CreateGlobalStage(
            MyanmarShaperStageKind.PresentationForms,
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
        SfntUserFeature[] customFeatures =
            CopyCustomFeatures(userFeatures);
        stages[8] = CreatePolicyStage(
            MyanmarShaperStageKind.UserSubstitution,
            DefaultShaperTableKind.Substitution,
            new int[0],
            customFeatures,
            textLength,
            false);
        stages[9] = CreateGlobalStage(
            MyanmarShaperStageKind.Kerning,
            DefaultShaperTableKind.Positioning,
            new int[] { KerningTag },
            userFeatures,
            textLength);
        stages[10] = CreateGlobalStage(
            MyanmarShaperStageKind.Distances,
            DefaultShaperTableKind.Positioning,
            new int[] { DistancesTag },
            userFeatures,
            textLength);
        stages[11] = CreateGlobalStage(
            MyanmarShaperStageKind.MarkPositioning,
            DefaultShaperTableKind.Positioning,
            new int[] { MarkPositioningTag, MarkToMarkPositioningTag },
            userFeatures,
            textLength);
        stages[12] = CreatePolicyStage(
            MyanmarShaperStageKind.UserPositioning,
            DefaultShaperTableKind.Positioning,
            new int[0],
            customFeatures,
            textLength,
            false);
        return new MyanmarShaperPlan(preparation, stages);
    }

    static int InsertDottedCircles(
        ShapingBuffer buffer,
        MyanmarSyllableMap map)
    {
        int count = 0;
        int syllableIndex = map.GetSyllableCount() - 1;
        while (syllableIndex >= 0)
        {
            if (map.GetSyllableType(syllableIndex) ==
                MyanmarSyllableType.Broken)
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

    static MyanmarSyllableMap ExpandMapForDottedCircles(
        ShapingBuffer buffer,
        MyanmarSyllableMap map)
    {
        int newRuneCount = buffer.GetCount();
        MyanmarSyllabicCategory[] categories =
            new MyanmarSyllabicCategory[newRuneCount];
        int[] runeSyllableIndexes = new int[newRuneCount];
        int syllableCount = map.GetSyllableCount();
        int[] starts = new int[syllableCount];
        int[] lengths = new int[syllableCount];
        MyanmarSyllableType[] types =
            new MyanmarSyllableType[syllableCount];
        int offset = 0;
        int syllableIndex = 0;
        while (syllableIndex < syllableCount)
        {
            int oldStart = map.GetSyllableStart(syllableIndex);
            int oldLength = map.GetSyllableLength(syllableIndex);
            bool broken = map.GetSyllableType(syllableIndex) ==
                MyanmarSyllableType.Broken;
            int newStart = oldStart + offset;
            starts[syllableIndex] = newStart;
            lengths[syllableIndex] = oldLength + (broken ? 1 : 0);
            types[syllableIndex] = map.GetSyllableType(syllableIndex);
            int target = newStart;
            if (broken)
            {
                categories[target] =
                    MyanmarSyllabicCategory.DottedCircle;
                runeSyllableIndexes[target] = syllableIndex;
                target++;
                offset++;
            }

            int source = oldStart;
            int sourceEnd = oldStart + oldLength;
            while (source < sourceEnd)
            {
                categories[target] = map.GetCategory(source);
                runeSyllableIndexes[target] = syllableIndex;
                source++;
                target++;
            }

            syllableIndex++;
        }

        MyanmarSyllableMap expanded = new MyanmarSyllableMap(
            categories,
            runeSyllableIndexes,
            starts,
            lengths,
            types,
            map.HasBrokenSyllable());
        ApplySyllables(buffer, expanded);
        return expanded;
    }

    static void ApplySyllables(
        ShapingBuffer buffer,
        MyanmarSyllableMap map)
    {
        int serial = 1;
        int syllableIndex = 0;
        while (syllableIndex < map.GetSyllableCount())
        {
            int value = serial * 16 +
                (int)map.GetSyllableType(syllableIndex);
            int index = map.GetSyllableStart(syllableIndex);
            int end = map.GetSyllableEnd(syllableIndex);
            while (index < end)
            {
                buffer.SetSyllable(index, value);
                index++;
            }

            serial++;
            if (serial == 16)
            {
                serial = 1;
            }

            syllableIndex++;
        }
    }

    static void BuildReordering(
        MyanmarSyllableMap map,
        int[] sourceIndexes,
        MyanmarShapingPosition[] positions,
        int[] baseIndexes,
        bool[] kinziValues)
    {
        int syllableIndex = 0;
        while (syllableIndex < map.GetSyllableCount())
        {
            if (map.GetSyllableType(syllableIndex) !=
                MyanmarSyllableType.NonMyanmar)
            {
                int start = map.GetSyllableStart(syllableIndex);
                int end = map.GetSyllableEnd(syllableIndex);
                bool hasKinzi = HasKinzi(map, start, end);
                int baseIndex = FindBase(map, start, end, hasKinzi);
                kinziValues[syllableIndex] = hasKinzi;
                baseIndexes[syllableIndex] = baseIndex;
                AssignPositions(
                    map,
                    start,
                    end,
                    baseIndex,
                    hasKinzi,
                    positions);
                SortSyllable(
                    map,
                    start,
                    end,
                    positions,
                    sourceIndexes);
            }

            syllableIndex++;
        }
    }

    static bool HasKinzi(
        MyanmarSyllableMap map,
        int start,
        int end)
    {
        return start + 3 <= end &&
            map.GetCategory(start) == MyanmarSyllabicCategory.Ra &&
            map.GetCategory(start + 1) == MyanmarSyllabicCategory.Asat &&
            map.GetCategory(start + 2) == MyanmarSyllabicCategory.Halant;
    }

    static int FindBase(
        MyanmarSyllableMap map,
        int start,
        int end,
        bool hasKinzi)
    {
        int limit = hasKinzi ? start + 3 : start;
        int baseIndex = hasKinzi ? start : limit;
        int index = limit;
        while (index < end)
        {
            if (IsReorderingConsonant(map.GetCategory(index)))
            {
                return index;
            }

            index++;
        }

        return baseIndex;
    }

    static bool IsReorderingConsonant(MyanmarSyllabicCategory category)
    {
        return category == MyanmarSyllabicCategory.Consonant ||
            category == MyanmarSyllabicCategory.ConsonantWithStacker ||
            category == MyanmarSyllabicCategory.Ra ||
            category == MyanmarSyllabicCategory.IndependentVowel ||
            category == MyanmarSyllabicCategory.Placeholder ||
            category == MyanmarSyllabicCategory.DottedCircle;
    }

    static void AssignPositions(
        MyanmarSyllableMap map,
        int start,
        int end,
        int baseIndex,
        bool hasKinzi,
        MyanmarShapingPosition[] positions)
    {
        int kinziEnd = start + (hasKinzi ? 3 : 0);
        int index = start;
        while (index < kinziEnd)
        {
            positions[index] = MyanmarShapingPosition.AfterMain;
            index++;
        }

        while (index < baseIndex)
        {
            positions[index] = MyanmarShapingPosition.PreConsonant;
            index++;
        }

        if (index < end)
        {
            positions[index] = MyanmarShapingPosition.BaseConsonant;
            index++;
        }

        MyanmarShapingPosition position =
            MyanmarShapingPosition.AfterMain;
        while (index < end)
        {
            MyanmarSyllabicCategory category = map.GetCategory(index);
            if (category == MyanmarSyllabicCategory.MedialRa)
            {
                positions[index] = MyanmarShapingPosition.PreConsonant;
            }
            else if (category == MyanmarSyllabicCategory.VowelPre)
            {
                positions[index] = MyanmarShapingPosition.PreMatra;
            }
            else if (category ==
                MyanmarSyllabicCategory.VariationSelector)
            {
                positions[index] = positions[index - 1];
            }
            else if (position == MyanmarShapingPosition.AfterMain &&
                category == MyanmarSyllabicCategory.VowelBelow)
            {
                position = MyanmarShapingPosition.BelowConsonant;
                positions[index] = position;
            }
            else if (position == MyanmarShapingPosition.BelowConsonant &&
                category == MyanmarSyllabicCategory.A)
            {
                positions[index] = MyanmarShapingPosition.BeforeSubjoined;
            }
            else if (position == MyanmarShapingPosition.BelowConsonant &&
                category == MyanmarSyllabicCategory.VowelBelow)
            {
                positions[index] = position;
            }
            else if (position == MyanmarShapingPosition.BelowConsonant &&
                category != MyanmarSyllabicCategory.A)
            {
                position = MyanmarShapingPosition.AfterSubjoined;
                positions[index] = position;
            }
            else
            {
                positions[index] = position;
            }

            index++;
        }
    }

    static void SortSyllable(
        MyanmarSyllableMap map,
        int start,
        int end,
        MyanmarShapingPosition[] positions,
        int[] sourceIndexes)
    {
        System.Collections.Generic.List<int> order =
            new System.Collections.Generic.List<int>();
        int source = start;
        while (source < end)
        {
            int target = order.Count;
            while (target > 0 &&
                positions[order[target - 1]] > positions[source])
            {
                target--;
            }

            order.Insert(target, source);
            source++;
        }

        ReverseLeftMatras(map, order, positions);
        int index = 0;
        while (index < order.Count)
        {
            sourceIndexes[start + index] = order[index];
            index++;
        }
    }

    static void ReverseLeftMatras(
        MyanmarSyllableMap map,
        System.Collections.Generic.List<int> order,
        MyanmarShapingPosition[] positions)
    {
        int first = -1;
        int last = -1;
        int index = 0;
        while (index < order.Count)
        {
            if (positions[order[index]] ==
                MyanmarShapingPosition.PreMatra)
            {
                if (first < 0)
                {
                    first = index;
                }

                last = index;
            }

            index++;
        }

        if (first < 0 || first >= last)
        {
            return;
        }

        ReverseRange(order, first, last + 1);
        int groupStart = first;
        index = first;
        while (index <= last)
        {
            if (map.GetCategory(order[index]) ==
                MyanmarSyllabicCategory.VowelPre)
            {
                ReverseRange(order, groupStart, index + 1);
                groupStart = index + 1;
            }

            index++;
        }
    }

    static void ReverseRange(
        System.Collections.Generic.List<int> values,
        int start,
        int end)
    {
        int left = start;
        int right = end - 1;
        while (left < right)
        {
            int value = values[left];
            values[left] = values[right];
            values[right] = value;
            left++;
            right--;
        }
    }

    static void InitializeBaseIndexes(int[] baseIndexes)
    {
        int index = 0;
        while (index < baseIndexes.Length)
        {
            baseIndexes[index] = -1;
            index++;
        }
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

    static MyanmarShaperFeatureStage CreateGlobalStage(
        MyanmarShaperStageKind kind,
        DefaultShaperTableKind tableKind,
        int[] tags,
        SfntUserFeature[] userFeatures,
        int textLength)
    {
        return CreatePolicyStage(
            kind,
            tableKind,
            tags,
            CopyMatchingFeatures(userFeatures, tags),
            textLength,
            false);
    }

    static MyanmarShaperFeatureStage CreateBarrierStage(
        MyanmarShaperStageKind kind,
        int textLength)
    {
        return CreatePolicyStage(
            kind,
            DefaultShaperTableKind.Substitution,
            new int[0],
            new SfntUserFeature[0],
            textLength,
            true);
    }

    static MyanmarShaperFeatureStage CreatePolicyStage(
        MyanmarShaperStageKind kind,
        DefaultShaperTableKind tableKind,
        int[] mandatoryTags,
        SfntUserFeature[] features,
        int textLength,
        bool barrier)
    {
        SfntLayoutFeaturePlan plan = SfntLayoutFeaturePlanBuilder.Build(
            mandatoryTags,
            new int[0],
            new int[0],
            features,
            textLength);
        return new MyanmarShaperFeatureStage(
            kind,
            tableKind,
            plan,
            textLength,
            barrier);
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
        MyanmarDottedCirclePolicy policy)
    {
        if (policy != MyanmarDottedCirclePolicy.InsertForBrokenClusters &&
            policy != MyanmarDottedCirclePolicy.PreserveBrokenClusters)
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
