namespace ttf;

internal enum UniversalJoiningForm
{
    None = 0,
    Isolated = 1,
    Initial = 2,
    Medial = 3,
    Final = 4
}

internal enum UniversalShaperStageKind
{
    SyllableSetup = 0,
    LocalizedForms = 1,
    CompositionAndDecomposition = 2,
    NuktaForms = 3,
    AkhandForms = 4,
    RephForms = 5,
    PrebaseForms = 6,
    BasicForms = 7,
    Reordering = 8,
    TopographicalForms = 9,
    OtherForms = 10,
    UserSubstitution = 11,
    Kerning = 12,
    MarkPositioning = 13,
    UserPositioning = 14
}

internal class UniversalShaperPreparation : ITextShapingBarrier
{
    ShapingBuffer preparedBuffer;
    UniversalSyllableMap syllableMap;
    int[] sourceIndexes;
    UniversalJoiningForm[] joiningForms;
    bool reorderingApplied;

    internal UniversalShaperPreparation(
        ShapingBuffer preparedBuffer,
        UniversalSyllableMap syllableMap,
        int[] sourceIndexes,
        UniversalJoiningForm[] joiningForms)
    {
        this.preparedBuffer = preparedBuffer;
        this.syllableMap = syllableMap;
        this.sourceIndexes = sourceIndexes;
        this.joiningForms = joiningForms;
    }

    internal UniversalSyllableMap GetSyllableMap()
    {
        return syllableMap;
    }

    internal int GetCount()
    {
        return sourceIndexes.Length;
    }

    internal int GetSourceIndex(int index)
    {
        ValidateIndex(index);
        return sourceIndexes[index];
    }

    internal UniversalCategory GetCategory(int index)
    {
        return syllableMap.GetCategory(GetSourceIndex(index));
    }

    internal UniversalJoiningForm GetJoiningForm(int index)
    {
        return joiningForms[GetSourceIndex(index)];
    }

    internal bool IsReorderingApplied()
    {
        return reorderingApplied;
    }

    internal void RecordRephSubstitution(int index)
    {
        ValidateMutableIndex(index);
        syllableMap.SetCategory(index, UniversalCategory.Repha);
    }

    internal void RecordPrebaseSubstitution(int index)
    {
        ValidateMutableIndex(index);
        syllableMap.SetCategory(index, UniversalCategory.VowelPre);
    }

    /// <summary>Applies the prepared universal-shaper reordering barrier.</summary>
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
                "USE reordering has already been applied.");
        }

        if (!object.ReferenceEquals(buffer, preparedBuffer) ||
            buffer.GetCount() != sourceIndexes.Length)
        {
            throw new System.ArgumentException(
                "The shaping buffer no longer matches the USE preparation.");
        }

        int syllableIndex = 0;
        while (syllableIndex < syllableMap.GetSyllableCount())
        {
            UniversalSyllableType type =
                syllableMap.GetSyllableType(syllableIndex);
            if (NeedsReordering(type))
            {
                ReorderSyllable(
                    syllableMap.GetSyllableStart(syllableIndex),
                    syllableMap.GetSyllableEnd(syllableIndex));
            }

            syllableIndex++;
        }

        buffer.Reorder(sourceIndexes);
        syllableIndex = 0;
        while (syllableIndex < syllableMap.GetSyllableCount())
        {
            int start = syllableMap.GetSyllableStart(syllableIndex);
            int end = syllableMap.GetSyllableEnd(syllableIndex);
            int index = start;
            bool changed = false;
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

    void ReorderSyllable(int start, int end)
    {
        if (GetLogicalCategory(start) == UniversalCategory.Repha &&
            end - start > 1)
        {
            int target = end - 1;
            int index = start + 1;
            while (index < end)
            {
                UniversalCategory category = GetLogicalCategory(index);
                if (IsPostBase(category) || IsHalant(category))
                {
                    target = index - 1;
                    break;
                }

                index++;
            }

            Move(start, target);
        }

        int insertion = start;
        int current = start;
        while (current < end)
        {
            UniversalCategory category = GetLogicalCategory(current);
            if (IsHalant(category))
            {
                insertion = current + 1;
            }
            else if (category == UniversalCategory.VowelPre &&
                insertion < current)
            {
                Move(current, insertion);
                insertion++;
            }

            current++;
        }
    }

    UniversalCategory GetLogicalCategory(int outputIndex)
    {
        return syllableMap.GetCategory(sourceIndexes[outputIndex]);
    }

    void Move(int current, int target)
    {
        if (current == target)
        {
            return;
        }

        int value = sourceIndexes[current];
        if (current > target)
        {
            int index = current;
            while (index > target)
            {
                sourceIndexes[index] = sourceIndexes[index - 1];
                index--;
            }
        }
        else
        {
            int index = current;
            while (index < target)
            {
                sourceIndexes[index] = sourceIndexes[index + 1];
                index++;
            }
        }

        sourceIndexes[target] = value;
    }

    static bool NeedsReordering(UniversalSyllableType type)
    {
        return type == UniversalSyllableType.ViramaTerminated ||
            type == UniversalSyllableType.SakotTerminated ||
            type == UniversalSyllableType.Standard ||
            type == UniversalSyllableType.Symbol ||
            type == UniversalSyllableType.Broken;
    }

    static bool IsHalant(UniversalCategory category)
    {
        return category == UniversalCategory.Halant ||
            category == UniversalCategory.HalantOrVowelModifier ||
            category == UniversalCategory.InvisibleStacker;
    }

    static bool IsPostBase(UniversalCategory category)
    {
        return category == UniversalCategory.ConsonantFinalAbove ||
            category == UniversalCategory.ConsonantFinalBelow ||
            category == UniversalCategory.ConsonantFinalPost ||
            category == UniversalCategory.ConsonantFinalModifierAbove ||
            category == UniversalCategory.ConsonantFinalModifierBelow ||
            category == UniversalCategory.ConsonantFinalModifierPost ||
            category == UniversalCategory.ConsonantMedialAbove ||
            category == UniversalCategory.ConsonantMedialBelow ||
            category == UniversalCategory.ConsonantMedialPost ||
            category == UniversalCategory.ConsonantMedialPre ||
            category == UniversalCategory.VowelAbove ||
            category == UniversalCategory.VowelBelow ||
            category == UniversalCategory.VowelPost ||
            category == UniversalCategory.VowelPre ||
            category == UniversalCategory.VowelModifierAbove ||
            category == UniversalCategory.VowelModifierBelow ||
            category == UniversalCategory.VowelModifierPost ||
            category == UniversalCategory.VowelModifierPre;
    }

    void ValidateMutableIndex(int index)
    {
        ValidateIndex(index);
        if (reorderingApplied)
        {
            throw new System.InvalidOperationException(
                "USE substitution results cannot be recorded after reordering.");
        }
    }

    void ValidateIndex(int index)
    {
        if (index < 0 || index >= sourceIndexes.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }
    }
}

internal class UniversalShaperFeatureStage
{
    UniversalShaperStageKind kind;
    DefaultShaperTableKind tableKind;
    SfntLayoutFeaturePlan featurePlan;
    int scriptTag;
    int textLength;
    bool barrier;

    internal UniversalShaperFeatureStage(
        UniversalShaperStageKind kind,
        DefaultShaperTableKind tableKind,
        SfntLayoutFeaturePlan featurePlan,
        int scriptTag,
        int textLength,
        bool barrier)
    {
        this.kind = kind;
        this.tableKind = tableKind;
        this.featurePlan = featurePlan;
        this.scriptTag = scriptTag;
        this.textLength = textLength;
        this.barrier = barrier;
    }

    internal UniversalShaperStageKind GetKind()
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
        return barrier;
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
            scriptTag,
            languageTag,
            selections,
            textLength);
    }
}

internal class UniversalShaperPlan
{
    int scriptTag;
    UniversalShaperPreparation preparation;
    UniversalShaperFeatureStage[] stages;

    internal UniversalShaperPlan(
        int scriptTag,
        UniversalShaperPreparation preparation,
        UniversalShaperFeatureStage[] stages)
    {
        this.scriptTag = scriptTag;
        this.preparation = preparation;
        this.stages = stages;
    }

    internal int GetScriptTag()
    {
        return scriptTag;
    }

    internal UniversalShaperPreparation GetPreparation()
    {
        return preparation;
    }

    internal int GetStageCount()
    {
        return stages.Length;
    }

    internal UniversalShaperFeatureStage GetStage(int index)
    {
        if (index < 0 || index >= stages.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return stages[index];
    }
}

internal static class UniversalShaper
{
    static readonly int[] BuiltInTags = new int[]
    {
        0x6C6F636C, 0x63636D70, 0x6E756B74, 0x616B686E,
        0x72706866, 0x70726566, 0x726B7266, 0x61627666,
        0x626C7766, 0x68616C66, 0x70737466, 0x76617475,
        0x636A6374, 0x69736F6C, 0x696E6974, 0x6D656469,
        0x66696E61, 0x61627673, 0x626C7773, 0x68616C6E,
        0x70726573, 0x70737473, 0x6B65726E, 0x6D61726B,
        0x6D6B6D6B
    };

    internal static UniversalShaperPreparation Prepare(
        ShapingBuffer buffer,
        UnicodeData unicodeData,
        int maximumSyllableCount)
    {
        UniversalSyllableMap map = UniversalSyllableRecognizer.Recognize(
            buffer,
            unicodeData,
            maximumSyllableCount);
        int[] sourceIndexes = new int[buffer.GetCount()];
        int index = 0;
        while (index < sourceIndexes.Length)
        {
            sourceIndexes[index] = index;
            buffer.SetUnsafeToBreak(index, true);
            index++;
        }

        UniversalJoiningForm[] forms = CreateJoiningForms(map);
        return new UniversalShaperPreparation(
            buffer,
            map,
            sourceIndexes,
            forms);
    }

    internal static UniversalShaperPlan BuildPlan(
        UniversalShaperPreparation preparation,
        int scriptTag,
        SfntUserFeature[] userFeatures)
    {
        if (preparation == null)
        {
            throw new System.ArgumentNullException("preparation");
        }

        if (scriptTag < 0)
        {
            throw new System.ArgumentOutOfRangeException("scriptTag");
        }

        ValidateFeatures(userFeatures);
        int count = preparation.GetCount();
        UniversalShaperFeatureStage[] stages =
            new UniversalShaperFeatureStage[15];
        stages[0] = Stage(UniversalShaperStageKind.SyllableSetup,
            DefaultShaperTableKind.Substitution, new int[0], new int[0],
            new SfntUserFeature[0], scriptTag, count, true);
        stages[1] = Global(UniversalShaperStageKind.LocalizedForms,
            new int[] { 0x6C6F636C }, userFeatures, scriptTag, count);
        stages[2] = Global(
            UniversalShaperStageKind.CompositionAndDecomposition,
            new int[] { 0x63636D70 }, userFeatures, scriptTag, count);
        stages[3] = Global(UniversalShaperStageKind.NuktaForms,
            new int[] { 0x6E756B74 }, userFeatures, scriptTag, count);
        stages[4] = Global(UniversalShaperStageKind.AkhandForms,
            new int[] { 0x616B686E }, userFeatures, scriptTag, count);
        stages[5] = Stage(UniversalShaperStageKind.RephForms,
            DefaultShaperTableKind.Substitution, new int[0],
            new int[] { 0x72706866 }, CreateRephRanges(preparation),
            scriptTag, count, true);
        stages[6] = Stage(UniversalShaperStageKind.PrebaseForms,
            DefaultShaperTableKind.Substitution,
            new int[] { 0x70726566 }, new int[0],
            CopyMatching(userFeatures, new int[] { 0x70726566 }),
            scriptTag, count, true);
        stages[7] = Global(UniversalShaperStageKind.BasicForms,
            new int[] { 0x726B7266, 0x61627666, 0x626C7766,
                0x68616C66, 0x70737466, 0x76617475, 0x636A6374 },
            userFeatures, scriptTag, count);
        stages[8] = Stage(UniversalShaperStageKind.Reordering,
            DefaultShaperTableKind.Substitution, new int[0], new int[0],
            new SfntUserFeature[0], scriptTag, count, true);
        stages[9] = Stage(UniversalShaperStageKind.TopographicalForms,
            DefaultShaperTableKind.Substitution, new int[0],
            new int[] { 0x69736F6C, 0x696E6974, 0x6D656469, 0x66696E61 },
            CreateTopographicalRanges(preparation), scriptTag, count, false);
        stages[10] = Global(UniversalShaperStageKind.OtherForms,
            new int[] { 0x61627673, 0x626C7773, 0x68616C6E,
                0x70726573, 0x70737473 }, userFeatures, scriptTag, count);
        SfntUserFeature[] custom = CopyCustomFeatures(userFeatures);
        stages[11] = Stage(UniversalShaperStageKind.UserSubstitution,
            DefaultShaperTableKind.Substitution, new int[0], new int[0],
            custom, scriptTag, count, false);
        stages[12] = DefaultStage(UniversalShaperStageKind.Kerning,
            DefaultShaperTableKind.Positioning,
            new int[] { 0x6B65726E }, userFeatures,
            scriptTag, count);
        stages[13] = Stage(UniversalShaperStageKind.MarkPositioning,
            DefaultShaperTableKind.Positioning,
            new int[] { 0x6D61726B, 0x6D6B6D6B }, new int[0],
            CopyMatching(userFeatures,
                new int[] { 0x6D61726B, 0x6D6B6D6B }),
            scriptTag, count, false);
        stages[14] = Stage(UniversalShaperStageKind.UserPositioning,
            DefaultShaperTableKind.Positioning, new int[0], new int[0],
            custom, scriptTag, count, false);
        return new UniversalShaperPlan(scriptTag, preparation, stages);
    }

    static UniversalJoiningForm[] CreateJoiningForms(
        UniversalSyllableMap map)
    {
        UniversalJoiningForm[] result =
            new UniversalJoiningForm[map.GetRuneCount()];
        int runStart = -1;
        int syllableIndex = 0;
        while (syllableIndex <= map.GetSyllableCount())
        {
            bool joins = syllableIndex < map.GetSyllableCount() &&
                map.GetSyllableType(syllableIndex) !=
                    UniversalSyllableType.Hieroglyph &&
                map.GetSyllableType(syllableIndex) !=
                    UniversalSyllableType.NonCluster;
            if (joins && runStart < 0)
            {
                runStart = syllableIndex;
            }

            if (!joins && runStart >= 0)
            {
                ApplyJoiningRun(map, result, runStart, syllableIndex);
                runStart = -1;
            }

            syllableIndex++;
        }

        return result;
    }

    static void ApplyJoiningRun(
        UniversalSyllableMap map,
        UniversalJoiningForm[] values,
        int start,
        int end)
    {
        int syllableIndex = start;
        while (syllableIndex < end)
        {
            UniversalJoiningForm form = UniversalJoiningForm.Medial;
            if (end - start == 1)
            {
                form = UniversalJoiningForm.Isolated;
            }
            else if (syllableIndex == start)
            {
                form = UniversalJoiningForm.Initial;
            }
            else if (syllableIndex == end - 1)
            {
                form = UniversalJoiningForm.Final;
            }

            int index = map.GetSyllableStart(syllableIndex);
            int limit = map.GetSyllableEnd(syllableIndex);
            while (index < limit)
            {
                values[index] = form;
                index++;
            }

            syllableIndex++;
        }
    }

    static UniversalShaperFeatureStage Global(
        UniversalShaperStageKind kind,
        int[] tags,
        SfntUserFeature[] users,
        int scriptTag,
        int count)
    {
        return Stage(kind, DefaultShaperTableKind.Substitution, tags,
            new int[0], CopyMatching(users, tags), scriptTag, count, false);
    }

    static UniversalShaperFeatureStage Stage(
        UniversalShaperStageKind kind,
        DefaultShaperTableKind tableKind,
        int[] mandatory,
        int[] discretionary,
        SfntUserFeature[] users,
        int scriptTag,
        int count,
        bool barrier)
    {
        return new UniversalShaperFeatureStage(kind, tableKind,
            SfntLayoutFeaturePlanBuilder.Build(mandatory, new int[0],
                discretionary, users, count),
            scriptTag, count, barrier);
    }

    static UniversalShaperFeatureStage DefaultStage(
        UniversalShaperStageKind kind,
        DefaultShaperTableKind tableKind,
        int[] tags,
        SfntUserFeature[] users,
        int scriptTag,
        int count)
    {
        return new UniversalShaperFeatureStage(kind, tableKind,
            SfntLayoutFeaturePlanBuilder.Build(new int[0], tags,
                new int[0], CopyMatching(users, tags), count),
            scriptTag, count, false);
    }

    static SfntUserFeature[] CreateRephRanges(
        UniversalShaperPreparation preparation)
    {
        System.Collections.Generic.List<SfntUserFeature> result =
            new System.Collections.Generic.List<SfntUserFeature>();
        UniversalSyllableMap map = preparation.GetSyllableMap();
        int syllableIndex = 0;
        while (syllableIndex < map.GetSyllableCount())
        {
            int start = map.GetSyllableStart(syllableIndex);
            int limit = map.GetCategory(start) == UniversalCategory.Repha ?
                start + 1 : System.Math.Min(start + 3,
                    map.GetSyllableEnd(syllableIndex));
            result.Add(new SfntUserFeature(0x72706866, 1, start, limit));
            syllableIndex++;
        }

        return CopyFeatures(result);
    }

    static SfntUserFeature[] CreateTopographicalRanges(
        UniversalShaperPreparation preparation)
    {
        System.Collections.Generic.List<SfntUserFeature> result =
            new System.Collections.Generic.List<SfntUserFeature>();
        int index = 0;
        while (index < preparation.GetCount())
        {
            UniversalJoiningForm form = preparation.GetJoiningForm(index);
            if (form == UniversalJoiningForm.None)
            {
                index++;
                continue;
            }

            int start = index;
            while (index < preparation.GetCount() &&
                preparation.GetJoiningForm(index) == form)
            {
                index++;
            }

            result.Add(new SfntUserFeature(FormTag(form), 1, start, index));
        }

        return CopyFeatures(result);
    }

    static int FormTag(UniversalJoiningForm form)
    {
        if (form == UniversalJoiningForm.Isolated)
        {
            return 0x69736F6C;
        }

        if (form == UniversalJoiningForm.Initial)
        {
            return 0x696E6974;
        }

        if (form == UniversalJoiningForm.Medial)
        {
            return 0x6D656469;
        }

        return 0x66696E61;
    }

    static SfntUserFeature[] CopyCustomFeatures(SfntUserFeature[] values)
    {
        System.Collections.Generic.List<SfntUserFeature> result =
            new System.Collections.Generic.List<SfntUserFeature>();
        int index = 0;
        while (index < values.Length)
        {
            if (!HasTag(BuiltInTags, values[index].GetFeatureTag()))
            {
                result.Add(values[index]);
            }

            index++;
        }

        return CopyFeatures(result);
    }

    static SfntUserFeature[] CopyMatching(
        SfntUserFeature[] values,
        int[] tags)
    {
        System.Collections.Generic.List<SfntUserFeature> result =
            new System.Collections.Generic.List<SfntUserFeature>();
        int index = 0;
        while (index < values.Length)
        {
            if (HasTag(tags, values[index].GetFeatureTag()))
            {
                result.Add(values[index]);
            }

            index++;
        }

        return CopyFeatures(result);
    }

    static SfntUserFeature[] CopyFeatures(
        System.Collections.Generic.List<SfntUserFeature> values)
    {
        SfntUserFeature[] result = new SfntUserFeature[values.Count];
        values.CopyTo(result, 0);
        return result;
    }

    static bool HasTag(int[] tags, int tag)
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

    static void ValidateFeatures(SfntUserFeature[] values)
    {
        if (values == null)
        {
            throw new System.ArgumentNullException("userFeatures");
        }

        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == null)
            {
                throw new System.ArgumentException(
                    "A user feature cannot be null.");
            }

            index++;
        }
    }
}
