namespace ttf;

internal enum ThaiLaoScript
{
    Thai = 0,
    Lao = 1
}

internal enum ThaiLaoShaperStageKind
{
    LocalizedForms = 0,
    CompositionAndDecomposition = 1,
    Ligatures = 2,
    UserSubstitution = 3,
    Kerning = 4,
    MarkPositioning = 5,
    UserPositioning = 6
}

internal class ThaiLaoShaperPreparation
{
    ShapingBuffer preparedBuffer;
    ThaiLaoScript script;
    int[] sourceIndexes;
    bool[] nikhahitFromSaraAmSources;
    int decompositionCount;
    bool legacyFallbackAttempted;
    ThaiLegacyPuaResult? legacyResult;
    bool reorderingApplied;

    internal ThaiLaoShaperPreparation(
        ShapingBuffer preparedBuffer,
        ThaiLaoScript script,
        int[] sourceIndexes,
        bool[] nikhahitFromSaraAmSources,
        int decompositionCount,
        bool legacyFallbackAttempted,
        ThaiLegacyPuaResult? legacyResult)
    {
        this.preparedBuffer = preparedBuffer;
        this.script = script;
        this.sourceIndexes = sourceIndexes;
        this.nikhahitFromSaraAmSources = nikhahitFromSaraAmSources;
        this.decompositionCount = decompositionCount;
        this.legacyFallbackAttempted = legacyFallbackAttempted;
        this.legacyResult = legacyResult;
    }

    internal ThaiLaoScript GetScript()
    {
        return script;
    }

    internal int GetDecompositionCount()
    {
        return decompositionCount;
    }

    internal bool WasLegacyFallbackAttempted()
    {
        return legacyFallbackAttempted;
    }

    internal int GetLegacyPuaReplacementCount()
    {
        return legacyResult == null ?
            0 : legacyResult.GetReplacementCount();
    }

    internal int GetSourceIndex(int index)
    {
        ValidateIndex(index);
        return sourceIndexes[index];
    }

    internal int GetCount()
    {
        return sourceIndexes.Length;
    }

    internal bool IsReorderingApplied()
    {
        return reorderingApplied;
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
                "Thai/Lao preprocessing has already been applied.");
        }

        if (!object.ReferenceEquals(buffer, preparedBuffer) ||
            buffer.GetCount() != sourceIndexes.Length)
        {
            throw new System.ArgumentException(
                "The shaping buffer no longer matches the Thai/Lao preparation.");
        }

        int[] permutation = new int[sourceIndexes.Length];
        System.Array.Copy(sourceIndexes, permutation, sourceIndexes.Length);
        buffer.Reorder(permutation);
        MarkAndMergeCombiningSequences(buffer);
        MergeSaraAmClusters(buffer);
        ApplyLegacyReplacements(buffer);
        reorderingApplied = true;
    }

    void MarkAndMergeCombiningSequences(ShapingBuffer buffer)
    {
        int baseIndex = -1;
        int previousSource = -1;
        bool reordered = false;
        int index = 0;
        while (index < buffer.GetCount())
        {
            int codePoint = buffer.GetScalarValue(index);
            if (ThaiLaoShaper.IsCombiningMark(codePoint))
            {
                if (baseIndex < 0)
                {
                    baseIndex = index;
                }

                int unsafeIndex = baseIndex;
                while (unsafeIndex <= index)
                {
                    buffer.SetUnsafeToBreak(unsafeIndex, true);
                    unsafeIndex++;
                }

                if (previousSource >= 0 &&
                    sourceIndexes[index] < previousSource)
                {
                    reordered = true;
                }
            }
            else
            {
                if (reordered && baseIndex >= 0 && index > baseIndex)
                {
                    buffer.MergeClusters(baseIndex, index);
                }

                baseIndex = index;
                reordered = false;
            }

            previousSource = sourceIndexes[index];
            index++;
        }

        if (reordered && baseIndex >= 0 && buffer.GetCount() > baseIndex)
        {
            buffer.MergeClusters(baseIndex, buffer.GetCount());
        }
    }

    void MergeSaraAmClusters(ShapingBuffer buffer)
    {
        int source = 0;
        while (source < nikhahitFromSaraAmSources.Length)
        {
            if (!nikhahitFromSaraAmSources[source])
            {
                source++;
                continue;
            }

            int nikhahitIndex = FindTargetIndex(source);
            int saraAaIndex = FindTargetIndex(source + 1);
            int start = nikhahitIndex;
            if (start > 0 && ThaiLaoShaper.IsBaseCharacter(
                buffer.GetScalarValue(start - 1),
                script))
            {
                start--;
            }

            int end = saraAaIndex + 1;
            if (end > start)
            {
                buffer.MergeClusters(start, end);
                int index = start;
                while (index < end)
                {
                    buffer.SetUnsafeToBreak(index, true);
                    index++;
                }
            }

            source++;
        }
    }

    void ApplyLegacyReplacements(ShapingBuffer buffer)
    {
        if (legacyResult == null)
        {
            return;
        }

        int index = 0;
        while (index < legacyResult.GetCount())
        {
            int unsafeStart = legacyResult.GetUnsafeStart(index);
            if (unsafeStart >= 0)
            {
                int unsafeIndex = unsafeStart;
                while (unsafeIndex <= index)
                {
                    buffer.SetUnsafeToBreak(unsafeIndex, true);
                    unsafeIndex++;
                }
            }

            if (legacyResult.HasReplacement(index))
            {
                buffer.SetRune(
                    index,
                    new System.Text.Rune(
                        legacyResult.GetReplacement(index)));
            }

            index++;
        }
    }

    int FindTargetIndex(int sourceIndex)
    {
        int index = 0;
        while (index < sourceIndexes.Length)
        {
            if (sourceIndexes[index] == sourceIndex)
            {
                return index;
            }

            index++;
        }

        throw new System.InvalidOperationException(
            "A Thai/Lao preprocessing source index was lost.");
    }

    void ValidateIndex(int index)
    {
        if (index < 0 || index >= sourceIndexes.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }
    }
}

internal class ThaiLaoShaperFeatureStage
{
    ThaiLaoShaperStageKind kind;
    DefaultShaperTableKind tableKind;
    SfntLayoutFeaturePlan featurePlan;
    int scriptTag;
    int textLength;

    internal ThaiLaoShaperFeatureStage(
        ThaiLaoShaperStageKind kind,
        DefaultShaperTableKind tableKind,
        SfntLayoutFeaturePlan featurePlan,
        int scriptTag,
        int textLength)
    {
        this.kind = kind;
        this.tableKind = tableKind;
        this.featurePlan = featurePlan;
        this.scriptTag = scriptTag;
        this.textLength = textLength;
    }

    internal ThaiLaoShaperStageKind GetKind()
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

internal class ThaiLaoShaperPlan
{
    ThaiLaoScript script;
    int scriptTag;
    ThaiLaoShaperPreparation preparation;
    ThaiLaoShaperFeatureStage[] stages;

    internal ThaiLaoShaperPlan(
        ThaiLaoScript script,
        int scriptTag,
        ThaiLaoShaperPreparation preparation,
        ThaiLaoShaperFeatureStage[] stages)
    {
        this.script = script;
        this.scriptTag = scriptTag;
        this.preparation = preparation;
        this.stages = stages;
    }

    internal ThaiLaoScript GetScript()
    {
        return script;
    }

    internal int GetScriptTag()
    {
        return scriptTag;
    }

    internal ThaiLaoShaperPreparation GetPreparation()
    {
        return preparation;
    }

    internal int GetStageCount()
    {
        return stages.Length;
    }

    internal ThaiLaoShaperFeatureStage GetStage(int index)
    {
        if (index < 0 || index >= stages.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return stages[index];
    }

    internal ThaiLaoShaperFeatureStage? FindStage(
        ThaiLaoShaperStageKind kind)
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

    static void ValidateStageKind(ThaiLaoShaperStageKind kind)
    {
        if (kind < ThaiLaoShaperStageKind.LocalizedForms ||
            kind > ThaiLaoShaperStageKind.UserPositioning)
        {
            throw new System.ArgumentOutOfRangeException("kind");
        }
    }
}

internal static class ThaiLaoShaper
{
    const int ThaiScriptTag = 0x74686169;
    const int LaoScriptTag = 0x6C616F20;
    const int LocalizedFormsTag = 0x6C6F636C;
    const int CharacterCompositionTag = 0x63636D70;
    const int RequiredLigaturesTag = 0x726C6967;
    const int LigaturesTag = 0x6C696761;
    const int ContextualLigaturesTag = 0x636C6967;
    const int KerningTag = 0x6B65726E;
    const int MarkPositioningTag = 0x6D61726B;
    const int MarkToMarkPositioningTag = 0x6D6B6D6B;

    static readonly int[] BuiltInFeatureTags = new int[]
    {
        LocalizedFormsTag,
        CharacterCompositionTag,
        RequiredLigaturesTag,
        LigaturesTag,
        ContextualLigaturesTag,
        KerningTag,
        MarkPositioningTag,
        MarkToMarkPositioningTag
    };

    internal static ThaiLaoShaperPreparation Prepare(
        ShapingBuffer buffer,
        ThaiLaoScript script,
        ThaiLegacyPuaPolicy legacyPuaPolicy,
        bool hasScriptSubstitution,
        ThaiLegacyPuaCoverage legacyPuaCoverage)
    {
        if (buffer == null)
        {
            throw new System.ArgumentNullException("buffer");
        }

        if (legacyPuaCoverage == null)
        {
            throw new System.ArgumentNullException("legacyPuaCoverage");
        }

        ValidateScript(script);
        ValidateLegacyPuaPolicy(legacyPuaPolicy);
        System.Collections.Generic.List<int> insertedNikhahitIndexes =
            new System.Collections.Generic.List<int>();
        int decompositionCount = DecomposeSaraAm(
            buffer,
            script,
            insertedNikhahitIndexes);
        bool[] nikhahitSources = new bool[buffer.GetCount()];
        int index = 0;
        while (index < insertedNikhahitIndexes.Count)
        {
            nikhahitSources[insertedNikhahitIndexes[index]] = true;
            index++;
        }

        int[] sourceIndexes = CreateIdentity(buffer.GetCount());
        if (script == ThaiLaoScript.Thai)
        {
            ReorderThaiBelowMarks(buffer, sourceIndexes);
        }

        ReorderSaraAmNikhahit(
            buffer,
            sourceIndexes,
            nikhahitSources);
        bool fallbackAttempted = script == ThaiLaoScript.Thai &&
            legacyPuaPolicy ==
                ThaiLegacyPuaPolicy.WhenOpenTypeSubstitutionUnavailable &&
            !hasScriptSubstitution;
        ThaiLegacyPuaResult? legacyResult = null;
        if (fallbackAttempted)
        {
            legacyResult = ThaiLegacyPuaShaper.Prepare(
                buffer,
                sourceIndexes,
                legacyPuaCoverage);
        }

        return new ThaiLaoShaperPreparation(
            buffer,
            script,
            sourceIndexes,
            nikhahitSources,
            decompositionCount,
            fallbackAttempted,
            legacyResult);
    }

    internal static ThaiLaoShaperPlan BuildPlan(
        ThaiLaoShaperPreparation preparation,
        SfntUserFeature[] userFeatures)
    {
        if (preparation == null)
        {
            throw new System.ArgumentNullException("preparation");
        }

        ValidateUserFeatures(userFeatures);
        int scriptTag = GetScriptTag(preparation.GetScript());
        int textLength = preparation.GetCount();
        ThaiLaoShaperFeatureStage[] stages =
            new ThaiLaoShaperFeatureStage[7];
        stages[0] = CreateStage(
            ThaiLaoShaperStageKind.LocalizedForms,
            DefaultShaperTableKind.Substitution,
            new int[] { LocalizedFormsTag },
            new int[0],
            CopyMatchingFeatures(
                userFeatures,
                new int[] { LocalizedFormsTag }),
            scriptTag,
            textLength);
        stages[1] = CreateStage(
            ThaiLaoShaperStageKind.CompositionAndDecomposition,
            DefaultShaperTableKind.Substitution,
            new int[] { CharacterCompositionTag },
            new int[0],
            CopyMatchingFeatures(
                userFeatures,
                new int[] { CharacterCompositionTag }),
            scriptTag,
            textLength);
        stages[2] = CreateStage(
            ThaiLaoShaperStageKind.Ligatures,
            DefaultShaperTableKind.Substitution,
            new int[] { RequiredLigaturesTag },
            new int[] { LigaturesTag, ContextualLigaturesTag },
            CopyMatchingFeatures(
                userFeatures,
                new int[]
                {
                    RequiredLigaturesTag,
                    LigaturesTag,
                    ContextualLigaturesTag
                }),
            scriptTag,
            textLength);
        SfntUserFeature[] customFeatures =
            CopyCustomFeatures(userFeatures);
        stages[3] = CreateStage(
            ThaiLaoShaperStageKind.UserSubstitution,
            DefaultShaperTableKind.Substitution,
            new int[0],
            new int[0],
            customFeatures,
            scriptTag,
            textLength);
        stages[4] = CreateStage(
            ThaiLaoShaperStageKind.Kerning,
            DefaultShaperTableKind.Positioning,
            new int[0],
            new int[] { KerningTag },
            CopyMatchingFeatures(
                userFeatures,
                new int[] { KerningTag }),
            scriptTag,
            textLength);
        stages[5] = CreateStage(
            ThaiLaoShaperStageKind.MarkPositioning,
            DefaultShaperTableKind.Positioning,
            new int[] { MarkPositioningTag, MarkToMarkPositioningTag },
            new int[0],
            CopyMatchingFeatures(
                userFeatures,
                new int[] { MarkPositioningTag, MarkToMarkPositioningTag }),
            scriptTag,
            textLength);
        stages[6] = CreateStage(
            ThaiLaoShaperStageKind.UserPositioning,
            DefaultShaperTableKind.Positioning,
            new int[0],
            new int[0],
            customFeatures,
            scriptTag,
            textLength);
        return new ThaiLaoShaperPlan(
            preparation.GetScript(),
            scriptTag,
            preparation,
            stages);
    }

    internal static int GetScriptTag(ThaiLaoScript script)
    {
        ValidateScript(script);
        return script == ThaiLaoScript.Thai ?
            ThaiScriptTag : LaoScriptTag;
    }

    internal static bool IsCombiningMark(int codePoint)
    {
        return codePoint == 0x0E31 ||
            (codePoint >= 0x0E34 && codePoint <= 0x0E3A) ||
            (codePoint >= 0x0E47 && codePoint <= 0x0E4E) ||
            codePoint == 0x0EB1 ||
            (codePoint >= 0x0EB4 && codePoint <= 0x0EBC) ||
            (codePoint >= 0x0EC8 && codePoint <= 0x0ECD);
    }

    internal static bool IsBaseCharacter(
        int codePoint,
        ThaiLaoScript script)
    {
        if (script == ThaiLaoScript.Thai)
        {
            return codePoint >= 0x0E01 && codePoint <= 0x0E2E;
        }

        return codePoint >= 0x0E81 && codePoint <= 0x0EAE;
    }

    static int DecomposeSaraAm(
        ShapingBuffer buffer,
        ThaiLaoScript script,
        System.Collections.Generic.List<int> insertedNikhahitIndexes)
    {
        int saraAm = script == ThaiLaoScript.Thai ? 0x0E33 : 0x0EB3;
        int nikhahit = script == ThaiLaoScript.Thai ? 0x0E4D : 0x0ECD;
        int saraAa = saraAm - 1;
        int count = 0;
        int index = 0;
        while (index < buffer.GetCount())
        {
            if (buffer.GetScalarValue(index) == saraAm)
            {
                buffer.InsertRune(
                    index,
                    new System.Text.Rune(nikhahit),
                    index);
                insertedNikhahitIndexes.Add(index);
                buffer.SetRune(index + 1, new System.Text.Rune(saraAa));
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

    static void ReorderThaiBelowMarks(
        ShapingBuffer buffer,
        int[] sourceIndexes)
    {
        int source = 0;
        while (source < buffer.GetCount())
        {
            int codePoint = buffer.GetScalarValue(source);
            if (codePoint == 0x0E38 || codePoint == 0x0E39)
            {
                int current = FindValue(sourceIndexes, source);
                int target = current;
                while (target > 0 && buffer.GetScalarValue(
                    sourceIndexes[target - 1]) == 0x0E3A)
                {
                    target--;
                }

                MoveValue(sourceIndexes, current, target);
            }

            source++;
        }
    }

    static void ReorderSaraAmNikhahit(
        ShapingBuffer buffer,
        int[] sourceIndexes,
        bool[] nikhahitSources)
    {
        int source = 0;
        while (source < nikhahitSources.Length)
        {
            if (nikhahitSources[source])
            {
                int current = FindValue(sourceIndexes, source);
                int target = current;
                while (target > 0 && IsAboveBaseMark(
                    buffer.GetScalarValue(sourceIndexes[target - 1])))
                {
                    target--;
                }

                MoveValue(sourceIndexes, current, target);
            }

            source++;
        }
    }

    static bool IsAboveBaseMark(int codePoint)
    {
        int thaiCodePoint = codePoint >= 0x0E80 ?
            codePoint - 0x80 : codePoint;
        return thaiCodePoint == 0x0E31 || thaiCodePoint == 0x0E3B ||
            (thaiCodePoint >= 0x0E34 && thaiCodePoint <= 0x0E37) ||
            (thaiCodePoint >= 0x0E47 && thaiCodePoint <= 0x0E4E);
    }

    static int FindValue(int[] values, int value)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == value)
            {
                return index;
            }

            index++;
        }

        throw new System.InvalidOperationException(
            "A Thai/Lao preprocessing source index was lost.");
    }

    static void MoveValue(int[] values, int current, int target)
    {
        if (current == target)
        {
            return;
        }

        int value = values[current];
        int index = current;
        while (index > target)
        {
            values[index] = values[index - 1];
            index--;
        }

        values[target] = value;
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

    static ThaiLaoShaperFeatureStage CreateStage(
        ThaiLaoShaperStageKind kind,
        DefaultShaperTableKind tableKind,
        int[] mandatoryTags,
        int[] defaultTags,
        SfntUserFeature[] userFeatures,
        int scriptTag,
        int textLength)
    {
        SfntLayoutFeaturePlan plan = SfntLayoutFeaturePlanBuilder.Build(
            mandatoryTags,
            defaultTags,
            new int[0],
            userFeatures,
            textLength);
        return new ThaiLaoShaperFeatureStage(
            kind,
            tableKind,
            plan,
            scriptTag,
            textLength);
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

    static void ValidateScript(ThaiLaoScript script)
    {
        if (script != ThaiLaoScript.Thai && script != ThaiLaoScript.Lao)
        {
            throw new System.ArgumentOutOfRangeException("script");
        }
    }

    static void ValidateLegacyPuaPolicy(ThaiLegacyPuaPolicy policy)
    {
        if (policy != ThaiLegacyPuaPolicy.Disabled &&
            policy !=
                ThaiLegacyPuaPolicy.WhenOpenTypeSubstitutionUnavailable)
        {
            throw new System.ArgumentOutOfRangeException("legacyPuaPolicy");
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
