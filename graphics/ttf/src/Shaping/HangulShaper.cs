namespace ttf;

internal enum HangulJamoFeature
{
    None = 0,
    Leading = 1,
    Vowel = 2,
    Trailing = 3
}

internal enum HangulShaperStageKind
{
    LocalizedForms = 0,
    CompositionAndDecomposition = 1,
    LeadingJamoForms = 2,
    VowelJamoForms = 3,
    TrailingJamoForms = 4,
    Ligatures = 5,
    UserSubstitution = 6,
    Kerning = 7,
    MarkPositioning = 8,
    UserPositioning = 9
}

internal class HangulGlyphCoverage
{
    int[] supportedCodePoints;
    int[] zeroWidthCodePoints;

    internal HangulGlyphCoverage(
        int[] supportedCodePoints,
        int[] zeroWidthCodePoints)
    {
        if (supportedCodePoints == null)
        {
            throw new System.ArgumentNullException("supportedCodePoints");
        }

        if (zeroWidthCodePoints == null)
        {
            throw new System.ArgumentNullException("zeroWidthCodePoints");
        }

        this.supportedCodePoints = CopyAndValidate(
            supportedCodePoints,
            "supportedCodePoints");
        this.zeroWidthCodePoints = CopyAndValidate(
            zeroWidthCodePoints,
            "zeroWidthCodePoints");
        int index = 0;
        while (index < this.zeroWidthCodePoints.Length)
        {
            if (!HasGlyph(this.zeroWidthCodePoints[index]))
            {
                throw new System.ArgumentException(
                    "Every zero-width character must have a supported glyph.");
            }

            index++;
        }
    }

    internal bool HasGlyph(int codePoint)
    {
        return HasCodePoint(supportedCodePoints, codePoint);
    }

    internal bool IsZeroWidth(int codePoint)
    {
        return HasCodePoint(zeroWidthCodePoints, codePoint);
    }

    static int[] CopyAndValidate(int[] values, string parameterName)
    {
        int[] result = new int[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            if (!System.Text.Rune.IsValid(values[index]))
            {
                throw new System.ArgumentOutOfRangeException(parameterName);
            }

            result[index] = values[index];
            index++;
        }

        return result;
    }

    static bool HasCodePoint(int[] values, int codePoint)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == codePoint)
            {
                return true;
            }

            index++;
        }

        return false;
    }
}

internal class HangulProcessingCounters
{
    int composedSyllableCount;
    int decomposedSyllableCount;
    int reorderedToneCount;
    int zeroWidthToneCount;
    int insertedDottedCircleCount;

    internal void AddComposedSyllable()
    {
        composedSyllableCount++;
    }

    internal void AddDecomposedSyllable()
    {
        decomposedSyllableCount++;
    }

    internal void AddReorderedTone()
    {
        reorderedToneCount++;
    }

    internal void AddZeroWidthTone()
    {
        zeroWidthToneCount++;
    }

    internal void AddInsertedDottedCircle()
    {
        insertedDottedCircleCount++;
    }

    internal int GetComposedSyllableCount()
    {
        return composedSyllableCount;
    }

    internal int GetDecomposedSyllableCount()
    {
        return decomposedSyllableCount;
    }

    internal int GetReorderedToneCount()
    {
        return reorderedToneCount;
    }

    internal int GetZeroWidthToneCount()
    {
        return zeroWidthToneCount;
    }

    internal int GetInsertedDottedCircleCount()
    {
        return insertedDottedCircleCount;
    }
}

internal class HangulShaperPreparation
{
    ShapingBuffer preparedBuffer;
    HangulJamoFeature[] featureValues;
    bool[] contextualAlternatesValues;
    HangulProcessingCounters counters;

    internal HangulShaperPreparation(
        ShapingBuffer preparedBuffer,
        HangulJamoFeature[] featureValues,
        bool[] contextualAlternatesValues,
        HangulProcessingCounters counters)
    {
        this.preparedBuffer = preparedBuffer;
        this.featureValues = featureValues;
        this.contextualAlternatesValues = contextualAlternatesValues;
        this.counters = counters;
    }

    internal ShapingBuffer GetBuffer()
    {
        return preparedBuffer;
    }

    internal int GetCount()
    {
        return featureValues.Length;
    }

    internal HangulJamoFeature GetJamoFeature(int index)
    {
        ValidateIndex(index);
        return featureValues[index];
    }

    internal bool IsContextualAlternatesEnabled(int index)
    {
        ValidateIndex(index);
        return contextualAlternatesValues[index];
    }

    internal int GetComposedSyllableCount()
    {
        return counters.GetComposedSyllableCount();
    }

    internal int GetDecomposedSyllableCount()
    {
        return counters.GetDecomposedSyllableCount();
    }

    internal int GetReorderedToneCount()
    {
        return counters.GetReorderedToneCount();
    }

    internal int GetZeroWidthToneCount()
    {
        return counters.GetZeroWidthToneCount();
    }

    internal int GetInsertedDottedCircleCount()
    {
        return counters.GetInsertedDottedCircleCount();
    }

    void ValidateIndex(int index)
    {
        if (index < 0 || index >= featureValues.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }
    }
}

internal class HangulShaperFeatureStage
{
    HangulShaperStageKind kind;
    DefaultShaperTableKind tableKind;
    SfntLayoutFeaturePlan featurePlan;
    int textLength;

    internal HangulShaperFeatureStage(
        HangulShaperStageKind kind,
        DefaultShaperTableKind tableKind,
        SfntLayoutFeaturePlan featurePlan,
        int textLength)
    {
        this.kind = kind;
        this.tableKind = tableKind;
        this.featurePlan = featurePlan;
        this.textLength = textLength;
    }

    internal HangulShaperStageKind GetKind()
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
            HangulShaper.ScriptTag,
            languageTag,
            selections,
            textLength);
    }
}

internal class HangulShaperPlan
{
    HangulShaperPreparation preparation;
    HangulShaperFeatureStage[] stages;

    internal HangulShaperPlan(
        HangulShaperPreparation preparation,
        HangulShaperFeatureStage[] stages)
    {
        this.preparation = preparation;
        this.stages = stages;
    }

    internal int GetScriptTag()
    {
        return HangulShaper.ScriptTag;
    }

    internal HangulShaperPreparation GetPreparation()
    {
        return preparation;
    }

    internal int GetStageCount()
    {
        return stages.Length;
    }

    internal HangulShaperFeatureStage GetStage(int index)
    {
        if (index < 0 || index >= stages.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return stages[index];
    }

    internal HangulShaperFeatureStage? FindStage(HangulShaperStageKind kind)
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

    static void ValidateStageKind(HangulShaperStageKind kind)
    {
        if (kind < HangulShaperStageKind.LocalizedForms ||
            kind > HangulShaperStageKind.UserPositioning)
        {
            throw new System.ArgumentOutOfRangeException("kind");
        }
    }
}

internal static class HangulShaper
{
    internal const int ScriptTag = 0x68616E67;

    const int LeadingBase = 0x1100;
    const int VowelBase = 0x1161;
    const int TrailingBase = 0x11A7;
    const int LeadingCount = 19;
    const int VowelCount = 21;
    const int TrailingCount = 28;
    const int SyllableBase = 0xAC00;
    const int SyllableBlockCount = VowelCount * TrailingCount;
    const int SyllableCount = LeadingCount * SyllableBlockCount;
    const int DottedCircle = 0x25CC;

    const int CharacterCompositionTag = 0x63636D70;
    const int ContextualAlternatesTag = 0x63616C74;
    const int ContextualLigaturesTag = 0x636C6967;
    const int KerningTag = 0x6B65726E;
    const int LeadingJamoFormsTag = 0x6C6A6D6F;
    const int LigaturesTag = 0x6C696761;
    const int LocalizedFormsTag = 0x6C6F636C;
    const int MarkPositioningTag = 0x6D61726B;
    const int MarkToMarkPositioningTag = 0x6D6B6D6B;
    const int RequiredLigaturesTag = 0x726C6967;
    const int TrailingJamoFormsTag = 0x746A6D6F;
    const int VowelJamoFormsTag = 0x766A6D6F;

    static readonly int[] BuiltInFeatureTags = new int[]
    {
        LocalizedFormsTag,
        CharacterCompositionTag,
        LeadingJamoFormsTag,
        VowelJamoFormsTag,
        TrailingJamoFormsTag,
        RequiredLigaturesTag,
        LigaturesTag,
        ContextualLigaturesTag,
        ContextualAlternatesTag,
        KerningTag,
        MarkPositioningTag,
        MarkToMarkPositioningTag
    };

    internal static HangulShaperPreparation Prepare(
        ShapingBuffer buffer,
        HangulGlyphCoverage coverage)
    {
        if (buffer == null)
        {
            throw new System.ArgumentNullException("buffer");
        }

        if (coverage == null)
        {
            throw new System.ArgumentNullException("coverage");
        }

        System.Collections.Generic.List<HangulJamoFeature> featureValues =
            new System.Collections.Generic.List<HangulJamoFeature>();
        int initialIndex = 0;
        while (initialIndex < buffer.GetCount())
        {
            featureValues.Add(HangulJamoFeature.None);
            initialIndex++;
        }

        HangulProcessingCounters counters = new HangulProcessingCounters();
        Process(buffer, coverage, featureValues, counters);
        HangulJamoFeature[] features =
            new HangulJamoFeature[featureValues.Count];
        featureValues.CopyTo(features, 0);
        bool[] contextualAlternates = new bool[buffer.GetCount()];
        int index = 0;
        while (index < contextualAlternates.Length)
        {
            int codePoint = buffer.GetScalarValue(index);
            contextualAlternates[index] = !IsLeading(codePoint) &&
                !IsVowel(codePoint) && !IsTrailing(codePoint);
            index++;
        }

        return new HangulShaperPreparation(
            buffer,
            features,
            contextualAlternates,
            counters);
    }

    internal static HangulShaperPlan BuildPlan(
        HangulShaperPreparation preparation,
        SfntUserFeature[] userFeatures)
    {
        if (preparation == null)
        {
            throw new System.ArgumentNullException("preparation");
        }

        ValidateUserFeatures(userFeatures);
        int textLength = preparation.GetCount();
        HangulShaperFeatureStage[] stages =
            new HangulShaperFeatureStage[10];
        stages[0] = CreateGlobalStage(
            HangulShaperStageKind.LocalizedForms,
            DefaultShaperTableKind.Substitution,
            new int[] { LocalizedFormsTag },
            new int[0],
            userFeatures,
            textLength);
        stages[1] = CreateGlobalStage(
            HangulShaperStageKind.CompositionAndDecomposition,
            DefaultShaperTableKind.Substitution,
            new int[] { CharacterCompositionTag },
            new int[0],
            userFeatures,
            textLength);
        stages[2] = CreateSelectiveStage(
            HangulShaperStageKind.LeadingJamoForms,
            LeadingJamoFormsTag,
            preparation,
            HangulJamoFeature.Leading,
            textLength);
        stages[3] = CreateSelectiveStage(
            HangulShaperStageKind.VowelJamoForms,
            VowelJamoFormsTag,
            preparation,
            HangulJamoFeature.Vowel,
            textLength);
        stages[4] = CreateSelectiveStage(
            HangulShaperStageKind.TrailingJamoForms,
            TrailingJamoFormsTag,
            preparation,
            HangulJamoFeature.Trailing,
            textLength);
        stages[5] = CreateLigatureStage(
            preparation,
            userFeatures,
            textLength);
        SfntUserFeature[] customFeatures =
            CopyCustomFeatures(userFeatures);
        stages[6] = CreatePolicyStage(
            HangulShaperStageKind.UserSubstitution,
            DefaultShaperTableKind.Substitution,
            new int[0],
            new int[0],
            new int[0],
            customFeatures,
            textLength);
        stages[7] = CreateGlobalStage(
            HangulShaperStageKind.Kerning,
            DefaultShaperTableKind.Positioning,
            new int[0],
            new int[] { KerningTag },
            userFeatures,
            textLength);
        stages[8] = CreateGlobalStage(
            HangulShaperStageKind.MarkPositioning,
            DefaultShaperTableKind.Positioning,
            new int[0],
            new int[] { MarkPositioningTag, MarkToMarkPositioningTag },
            userFeatures,
            textLength);
        stages[9] = CreatePolicyStage(
            HangulShaperStageKind.UserPositioning,
            DefaultShaperTableKind.Positioning,
            new int[0],
            new int[0],
            new int[0],
            customFeatures,
            textLength);
        return new HangulShaperPlan(preparation, stages);
    }

    static void Process(
        ShapingBuffer buffer,
        HangulGlyphCoverage coverage,
        System.Collections.Generic.List<HangulJamoFeature> featureValues,
        HangulProcessingCounters counters)
    {
        int lastSyllableStart = 0;
        int lastSyllableEnd = 0;
        int index = 0;
        while (index < buffer.GetCount())
        {
            int codePoint = buffer.GetScalarValue(index);
            if (IsTone(codePoint))
            {
                ProcessTone(
                    buffer,
                    coverage,
                    featureValues,
                    counters,
                    index,
                    lastSyllableStart,
                    lastSyllableEnd);
                index += GetToneOutputLength(
                    coverage,
                    codePoint,
                    lastSyllableStart,
                    lastSyllableEnd,
                    index);
                lastSyllableStart = index;
                lastSyllableEnd = index;
                continue;
            }

            int syllableLength = ProcessLeadingSequence(
                buffer,
                coverage,
                featureValues,
                counters,
                index);
            if (syllableLength == 0)
            {
                syllableLength = ProcessCombinedSyllable(
                    buffer,
                    coverage,
                    featureValues,
                    counters,
                    index);
            }

            if (syllableLength > 0)
            {
                lastSyllableStart = index;
                lastSyllableEnd = index + syllableLength;
                index = lastSyllableEnd;
            }
            else
            {
                lastSyllableStart = index;
                lastSyllableEnd = index;
                index++;
            }
        }
    }

    static int ProcessLeadingSequence(
        ShapingBuffer buffer,
        HangulGlyphCoverage coverage,
        System.Collections.Generic.List<HangulJamoFeature> featureValues,
        HangulProcessingCounters counters,
        int index)
    {
        int leading = buffer.GetScalarValue(index);
        if (!IsLeading(leading) || index + 1 >= buffer.GetCount())
        {
            return 0;
        }

        int vowel = buffer.GetScalarValue(index + 1);
        if (!IsVowel(vowel))
        {
            return 0;
        }

        int trailing = 0;
        if (index + 2 < buffer.GetCount() && IsTrailing(
            buffer.GetScalarValue(index + 2)))
        {
            trailing = buffer.GetScalarValue(index + 2);
        }

        int length = trailing == 0 ? 2 : 3;
        MarkUnsafe(buffer, index, index + length);
        if (IsCombiningLeading(leading) && IsCombiningVowel(vowel) &&
            (trailing == 0 || IsCombiningTrailing(trailing)))
        {
            int trailingIndex = trailing == 0 ?
                0 : trailing - TrailingBase;
            int syllable = SyllableBase +
                (leading - LeadingBase) * SyllableBlockCount +
                (vowel - VowelBase) * TrailingCount + trailingIndex;
            if (coverage.HasGlyph(syllable))
            {
                ComposeRange(
                    buffer,
                    featureValues,
                    index,
                    length,
                    syllable);
                counters.AddComposedSyllable();
                return 1;
            }
        }

        featureValues[index] = HangulJamoFeature.Leading;
        featureValues[index + 1] = HangulJamoFeature.Vowel;
        if (trailing != 0)
        {
            featureValues[index + 2] = HangulJamoFeature.Trailing;
        }

        buffer.MergeClusters(index, index + length);
        return length;
    }

    static int ProcessCombinedSyllable(
        ShapingBuffer buffer,
        HangulGlyphCoverage coverage,
        System.Collections.Generic.List<HangulJamoFeature> featureValues,
        HangulProcessingCounters counters,
        int index)
    {
        int syllable = buffer.GetScalarValue(index);
        if (!IsCombinedSyllable(syllable))
        {
            return 0;
        }

        bool hasSyllableGlyph = coverage.HasGlyph(syllable);
        int leadingIndex = (syllable - SyllableBase) /
            SyllableBlockCount;
        int remainder = (syllable - SyllableBase) %
            SyllableBlockCount;
        int vowelIndex = remainder / TrailingCount;
        int trailingIndex = remainder % TrailingCount;
        bool hasFollowingTrailing = trailingIndex == 0 &&
            index + 1 < buffer.GetCount() &&
            IsTrailing(buffer.GetScalarValue(index + 1));
        bool combiningFollowingTrailing = hasFollowingTrailing &&
            IsCombiningTrailing(buffer.GetScalarValue(index + 1));
        if (combiningFollowingTrailing)
        {
            int newTrailingIndex = buffer.GetScalarValue(index + 1) -
                TrailingBase;
            int combined = syllable + newTrailingIndex;
            if (coverage.HasGlyph(combined))
            {
                MarkUnsafe(buffer, index, index + 2);
                ComposeRange(
                    buffer,
                    featureValues,
                    index,
                    2,
                    combined);
                counters.AddComposedSyllable();
                return 1;
            }

            MarkUnsafe(buffer, index, index + 2);
        }

        bool shouldDecompose = !hasSyllableGlyph ||
            (trailingIndex == 0 && hasFollowingTrailing);
        if (shouldDecompose)
        {
            int leading = LeadingBase + leadingIndex;
            int vowel = VowelBase + vowelIndex;
            int trailing = TrailingBase + trailingIndex;
            bool componentsAvailable = coverage.HasGlyph(leading) &&
                coverage.HasGlyph(vowel) &&
                (trailingIndex == 0 || coverage.HasGlyph(trailing));
            if (componentsAvailable)
            {
                bool includeFollowingTrailing = hasSyllableGlyph &&
                    trailingIndex == 0 && hasFollowingTrailing;
                int outputLength = trailingIndex == 0 ? 2 : 3;
                DecomposeSyllable(
                    buffer,
                    featureValues,
                    index,
                    leading,
                    vowel,
                    trailingIndex == 0 ? 0 : trailing);
                if (includeFollowingTrailing)
                {
                    featureValues[index + 2] =
                        HangulJamoFeature.Trailing;
                    outputLength++;
                }

                MarkUnsafe(buffer, index, index + outputLength);
                buffer.MergeClusters(index, index + outputLength);
                counters.AddDecomposedSyllable();
                return outputLength;
            }

            if (hasFollowingTrailing)
            {
                MarkUnsafe(buffer, index, index + 2);
            }
        }

        return hasSyllableGlyph ? 1 : 0;
    }

    static void ComposeRange(
        ShapingBuffer buffer,
        System.Collections.Generic.List<HangulJamoFeature> featureValues,
        int index,
        int length,
        int syllable)
    {
        buffer.MergeClusters(index, index + length);
        buffer.SetRune(index, new System.Text.Rune(syllable));
        buffer.ClearGlyphId(index);
        featureValues[index] = HangulJamoFeature.None;
        int remaining = length - 1;
        while (remaining > 0)
        {
            buffer.Remove(index + 1);
            featureValues.RemoveAt(index + 1);
            remaining--;
        }
    }

    static void DecomposeSyllable(
        ShapingBuffer buffer,
        System.Collections.Generic.List<HangulJamoFeature> featureValues,
        int index,
        int leading,
        int vowel,
        int trailing)
    {
        buffer.SetRune(index, new System.Text.Rune(leading));
        buffer.ClearGlyphId(index);
        featureValues[index] = HangulJamoFeature.Leading;
        buffer.InsertRune(
            index + 1,
            new System.Text.Rune(vowel),
            index);
        featureValues.Insert(index + 1, HangulJamoFeature.Vowel);
        if (trailing != 0)
        {
            buffer.InsertRune(
                index + 2,
                new System.Text.Rune(trailing),
                index);
            featureValues.Insert(index + 2, HangulJamoFeature.Trailing);
        }
    }

    static void ProcessTone(
        ShapingBuffer buffer,
        HangulGlyphCoverage coverage,
        System.Collections.Generic.List<HangulJamoFeature> featureValues,
        HangulProcessingCounters counters,
        int index,
        int lastSyllableStart,
        int lastSyllableEnd)
    {
        int tone = buffer.GetScalarValue(index);
        bool zeroWidth = coverage.IsZeroWidth(tone);
        if (lastSyllableStart < lastSyllableEnd &&
            lastSyllableEnd == index)
        {
            MarkUnsafe(buffer, lastSyllableStart, index + 1);
            if (zeroWidth)
            {
                counters.AddZeroWidthTone();
                return;
            }

            buffer.MergeClusters(lastSyllableStart, index + 1);
            MoveItem(buffer, featureValues, index, lastSyllableStart);
            counters.AddReorderedTone();
            return;
        }

        if (!coverage.HasGlyph(DottedCircle))
        {
            return;
        }

        if (zeroWidth)
        {
            buffer.InsertRune(
                index,
                new System.Text.Rune(DottedCircle),
                index);
            featureValues.Insert(index, HangulJamoFeature.None);
            counters.AddZeroWidthTone();
        }
        else
        {
            buffer.InsertRune(
                index + 1,
                new System.Text.Rune(DottedCircle),
                index);
            featureValues.Insert(index + 1, HangulJamoFeature.None);
        }

        MarkUnsafe(buffer, index, index + 2);
        buffer.MergeClusters(index, index + 2);
        counters.AddInsertedDottedCircle();
    }

    static int GetToneOutputLength(
        HangulGlyphCoverage coverage,
        int tone,
        int lastSyllableStart,
        int lastSyllableEnd,
        int toneIndex)
    {
        if (lastSyllableStart < lastSyllableEnd &&
            lastSyllableEnd == toneIndex)
        {
            return 1;
        }

        return coverage.HasGlyph(DottedCircle) ? 2 : 1;
    }

    static void MoveItem(
        ShapingBuffer buffer,
        System.Collections.Generic.List<HangulJamoFeature> featureValues,
        int current,
        int target)
    {
        int count = buffer.GetCount();
        int[] permutation = new int[count];
        int index = 0;
        while (index < count)
        {
            permutation[index] = index;
            index++;
        }

        int source = permutation[current];
        index = current;
        while (index > target)
        {
            permutation[index] = permutation[index - 1];
            index--;
        }

        permutation[target] = source;
        buffer.Reorder(permutation);
        HangulJamoFeature feature = featureValues[current];
        featureValues.RemoveAt(current);
        featureValues.Insert(target, feature);
    }

    static void MarkUnsafe(ShapingBuffer buffer, int start, int end)
    {
        int index = start;
        while (index < end)
        {
            buffer.SetUnsafeToBreak(index, true);
            index++;
        }
    }

    static HangulShaperFeatureStage CreateSelectiveStage(
        HangulShaperStageKind kind,
        int tag,
        HangulShaperPreparation preparation,
        HangulJamoFeature feature,
        int textLength)
    {
        SfntUserFeature[] ranges = CreateJamoRanges(
            tag,
            preparation,
            feature);
        return CreatePolicyStage(
            kind,
            DefaultShaperTableKind.Substitution,
            new int[0],
            new int[0],
            new int[] { tag },
            ranges,
            textLength);
    }

    static HangulShaperFeatureStage CreateLigatureStage(
        HangulShaperPreparation preparation,
        SfntUserFeature[] userFeatures,
        int textLength)
    {
        System.Collections.Generic.List<SfntUserFeature> features =
            new System.Collections.Generic.List<SfntUserFeature>();
        AddMatchingFeatures(
            features,
            userFeatures,
            new int[]
            {
                RequiredLigaturesTag,
                LigaturesTag,
                ContextualLigaturesTag,
                ContextualAlternatesTag
            });
        int index = 0;
        while (index < textLength)
        {
            if (preparation.IsContextualAlternatesEnabled(index))
            {
                index++;
                continue;
            }

            int start = index;
            while (index < textLength &&
                !preparation.IsContextualAlternatesEnabled(index))
            {
                index++;
            }

            features.Add(new SfntUserFeature(
                ContextualAlternatesTag,
                0,
                start,
                index));
        }

        SfntUserFeature[] featureArray =
            new SfntUserFeature[features.Count];
        features.CopyTo(featureArray, 0);
        return CreatePolicyStage(
            HangulShaperStageKind.Ligatures,
            DefaultShaperTableKind.Substitution,
            new int[] { RequiredLigaturesTag },
            new int[]
            {
                LigaturesTag,
                ContextualLigaturesTag,
                ContextualAlternatesTag
            },
            new int[0],
            featureArray,
            textLength);
    }

    static HangulShaperFeatureStage CreateGlobalStage(
        HangulShaperStageKind kind,
        DefaultShaperTableKind tableKind,
        int[] mandatoryTags,
        int[] defaultTags,
        SfntUserFeature[] userFeatures,
        int textLength)
    {
        int[] tags = CombineTags(mandatoryTags, defaultTags);
        return CreatePolicyStage(
            kind,
            tableKind,
            mandatoryTags,
            defaultTags,
            new int[0],
            CopyMatchingFeatures(userFeatures, tags),
            textLength);
    }

    static HangulShaperFeatureStage CreatePolicyStage(
        HangulShaperStageKind kind,
        DefaultShaperTableKind tableKind,
        int[] mandatoryTags,
        int[] defaultTags,
        int[] discretionaryTags,
        SfntUserFeature[] features,
        int textLength)
    {
        SfntLayoutFeaturePlan plan = SfntLayoutFeaturePlanBuilder.Build(
            mandatoryTags,
            defaultTags,
            discretionaryTags,
            features,
            textLength);
        return new HangulShaperFeatureStage(
            kind,
            tableKind,
            plan,
            textLength);
    }

    static SfntUserFeature[] CreateJamoRanges(
        int tag,
        HangulShaperPreparation preparation,
        HangulJamoFeature feature)
    {
        System.Collections.Generic.List<SfntUserFeature> ranges =
            new System.Collections.Generic.List<SfntUserFeature>();
        int index = 0;
        while (index < preparation.GetCount())
        {
            if (preparation.GetJamoFeature(index) != feature)
            {
                index++;
                continue;
            }

            int start = index;
            while (index < preparation.GetCount() &&
                preparation.GetJamoFeature(index) == feature)
            {
                index++;
            }

            ranges.Add(new SfntUserFeature(tag, 1, start, index));
        }

        SfntUserFeature[] result = new SfntUserFeature[ranges.Count];
        ranges.CopyTo(result, 0);
        return result;
    }

    static void AddMatchingFeatures(
        System.Collections.Generic.List<SfntUserFeature> destination,
        SfntUserFeature[] features,
        int[] tags)
    {
        int index = 0;
        while (index < features.Length)
        {
            if (HasTag(tags, features[index].GetFeatureTag()))
            {
                destination.Add(features[index]);
            }

            index++;
        }
    }

    static SfntUserFeature[] CopyMatchingFeatures(
        SfntUserFeature[] features,
        int[] tags)
    {
        System.Collections.Generic.List<SfntUserFeature> result =
            new System.Collections.Generic.List<SfntUserFeature>();
        AddMatchingFeatures(result, features, tags);
        SfntUserFeature[] values = new SfntUserFeature[result.Count];
        result.CopyTo(values, 0);
        return values;
    }

    static SfntUserFeature[] CopyCustomFeatures(
        SfntUserFeature[] features)
    {
        System.Collections.Generic.List<SfntUserFeature> result =
            new System.Collections.Generic.List<SfntUserFeature>();
        int index = 0;
        while (index < features.Length)
        {
            if (!HasTag(
                BuiltInFeatureTags,
                features[index].GetFeatureTag()))
            {
                result.Add(features[index]);
            }

            index++;
        }

        SfntUserFeature[] values = new SfntUserFeature[result.Count];
        result.CopyTo(values, 0);
        return values;
    }

    static int[] CombineTags(int[] first, int[] second)
    {
        int[] result = new int[first.Length + second.Length];
        System.Array.Copy(first, result, first.Length);
        System.Array.Copy(second, 0, result, first.Length, second.Length);
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

    static bool IsCombiningLeading(int codePoint)
    {
        return codePoint >= LeadingBase &&
            codePoint < LeadingBase + LeadingCount;
    }

    static bool IsCombiningVowel(int codePoint)
    {
        return codePoint >= VowelBase &&
            codePoint < VowelBase + VowelCount;
    }

    static bool IsCombiningTrailing(int codePoint)
    {
        return codePoint > TrailingBase &&
            codePoint < TrailingBase + TrailingCount;
    }

    static bool IsCombinedSyllable(int codePoint)
    {
        return codePoint >= SyllableBase &&
            codePoint < SyllableBase + SyllableCount;
    }

    static bool IsLeading(int codePoint)
    {
        return (codePoint >= 0x1100 && codePoint <= 0x115F) ||
            (codePoint >= 0xA960 && codePoint <= 0xA97C);
    }

    static bool IsVowel(int codePoint)
    {
        return (codePoint >= 0x1160 && codePoint <= 0x11A7) ||
            (codePoint >= 0xD7B0 && codePoint <= 0xD7C6);
    }

    static bool IsTrailing(int codePoint)
    {
        return (codePoint >= 0x11A8 && codePoint <= 0x11FF) ||
            (codePoint >= 0xD7CB && codePoint <= 0xD7FB);
    }

    static bool IsTone(int codePoint)
    {
        return codePoint == 0x302E || codePoint == 0x302F;
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
