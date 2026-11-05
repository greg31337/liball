namespace ttf;

internal class IndicShaperPreparation
{
    ShapingBuffer preparedBuffer;
    IndicShapingScript script;
    IndicShapingModel model;
    IndicSyllableMap syllableMap;
    int[] baseSourceIndexes;
    int[] rephLengths;
    int[] prebaseStarts;
    int[] initialSourceIndexes;
    int[] finalSourceIndexes;
    bool initialReorderingApplied;
    bool finalReorderingApplied;

    internal IndicShaperPreparation(
        ShapingBuffer preparedBuffer,
        IndicShapingScript script,
        IndicShapingModel model,
        IndicSyllableMap syllableMap,
        int[] baseSourceIndexes,
        int[] rephLengths,
        int[] prebaseStarts,
        int[] initialSourceIndexes,
        int[] finalSourceIndexes)
    {
        this.preparedBuffer = preparedBuffer;
        this.script = script;
        this.model = model;
        this.syllableMap = syllableMap;
        this.baseSourceIndexes = baseSourceIndexes;
        this.rephLengths = rephLengths;
        this.prebaseStarts = prebaseStarts;
        this.initialSourceIndexes = initialSourceIndexes;
        this.finalSourceIndexes = finalSourceIndexes;
    }

    internal IndicShapingScript GetScript()
    {
        return script;
    }

    internal IndicShapingModel GetModel()
    {
        return model;
    }

    internal IndicSyllableMap GetSyllableMap()
    {
        return syllableMap;
    }

    internal int GetSyllableCount()
    {
        return syllableMap.GetSyllableCount();
    }

    internal int GetBaseSourceIndex(int syllableIndex)
    {
        ValidateSyllableIndex(syllableIndex);
        return baseSourceIndexes[syllableIndex];
    }

    internal int GetInitialBaseIndex(int syllableIndex)
    {
        ValidateSyllableIndex(syllableIndex);
        return FindTargetIndex(
            initialSourceIndexes,
            baseSourceIndexes[syllableIndex]);
    }

    internal int GetFinalBaseIndex(int syllableIndex)
    {
        ValidateSyllableIndex(syllableIndex);
        return FindTargetIndex(
            finalSourceIndexes,
            baseSourceIndexes[syllableIndex]);
    }

    internal bool HasReph(int syllableIndex)
    {
        ValidateSyllableIndex(syllableIndex);
        return rephLengths[syllableIndex] > 0;
    }

    internal int GetRephLength(int syllableIndex)
    {
        ValidateSyllableIndex(syllableIndex);
        return rephLengths[syllableIndex];
    }

    internal bool HasPrebaseConsonant(int syllableIndex)
    {
        ValidateSyllableIndex(syllableIndex);
        return prebaseStarts[syllableIndex] >= 0;
    }

    internal int GetPrebaseSourceStart(int syllableIndex)
    {
        ValidateSyllableIndex(syllableIndex);
        return prebaseStarts[syllableIndex];
    }

    internal int GetInitialSourceIndex(int index)
    {
        ValidateRuneIndex(index);
        return initialSourceIndexes[index];
    }

    internal int GetFinalSourceIndex(int index)
    {
        ValidateRuneIndex(index);
        return finalSourceIndexes[index];
    }

    internal IndicSyllabicCategory GetInitialCategory(int index)
    {
        return syllableMap.GetCategory(GetInitialSourceIndex(index));
    }

    internal IndicSyllabicCategory GetFinalCategory(int index)
    {
        return syllableMap.GetCategory(GetFinalSourceIndex(index));
    }

    internal IndicPositionalCategory GetInitialPosition(int index)
    {
        return syllableMap.GetPosition(GetInitialSourceIndex(index));
    }

    internal IndicPositionalCategory GetFinalPosition(int index)
    {
        return syllableMap.GetPosition(GetFinalSourceIndex(index));
    }

    internal int GetInitialSyllableIndex(int index)
    {
        return syllableMap.GetRuneSyllableIndex(
            GetInitialSourceIndex(index));
    }

    internal int GetFinalSyllableIndex(int index)
    {
        return syllableMap.GetRuneSyllableIndex(
            GetFinalSourceIndex(index));
    }

    internal bool IsInitialReorderingApplied()
    {
        return initialReorderingApplied;
    }

    internal bool IsFinalReorderingApplied()
    {
        return finalReorderingApplied;
    }

    internal void ApplyInitialReordering(ShapingBuffer buffer)
    {
        if (buffer == null)
        {
            throw new System.ArgumentNullException("buffer");
        }

        if (initialReorderingApplied)
        {
            throw new System.InvalidOperationException(
                "Indic initial reordering has already been applied.");
        }

        ValidateBufferCount(buffer);
        buffer.Reorder(CopyIndexes(initialSourceIndexes));
        MergeChangedSyllables(
            buffer,
            CreateIdentity(initialSourceIndexes.Length),
            initialSourceIndexes);
        initialReorderingApplied = true;
    }

    internal void ApplyFinalReordering(ShapingBuffer buffer)
    {
        if (buffer == null)
        {
            throw new System.ArgumentNullException("buffer");
        }

        if (!initialReorderingApplied)
        {
            throw new System.InvalidOperationException(
                "Indic initial reordering must be applied first.");
        }

        if (finalReorderingApplied)
        {
            throw new System.InvalidOperationException(
                "Indic final reordering has already been applied.");
        }

        ValidateBufferCount(buffer);
        int[] currentIndexes = new int[initialSourceIndexes.Length];
        int index = 0;
        while (index < initialSourceIndexes.Length)
        {
            currentIndexes[initialSourceIndexes[index]] = index;
            index++;
        }

        int[] permutation = new int[finalSourceIndexes.Length];
        index = 0;
        while (index < finalSourceIndexes.Length)
        {
            permutation[index] = currentIndexes[finalSourceIndexes[index]];
            index++;
        }

        buffer.Reorder(permutation);
        MergeChangedSyllables(
            buffer,
            initialSourceIndexes,
            finalSourceIndexes);
        finalReorderingApplied = true;
    }

    void MergeChangedSyllables(
        ShapingBuffer buffer,
        int[] before,
        int[] after)
    {
        int syllableIndex = 0;
        while (syllableIndex < syllableMap.GetSyllableCount())
        {
            int start = syllableMap.GetSyllableStart(syllableIndex);
            int length = syllableMap.GetSyllableLength(syllableIndex);
            int end = start + length;
            bool changed = false;
            int index = start;
            while (index < end && !changed)
            {
                changed = before[index] != after[index];
                index++;
            }

            if (changed)
            {
                buffer.MergeClusters(start, end);
                index = start;
                while (index < end)
                {
                    buffer.SetUnsafeToBreak(index, true);
                    index++;
                }
            }

            syllableIndex++;
        }
    }

    void ValidateBufferCount(ShapingBuffer buffer)
    {
        if (!object.ReferenceEquals(buffer, preparedBuffer) ||
            buffer.GetCount() != initialSourceIndexes.Length)
        {
            throw new System.ArgumentException(
                "The shaping buffer no longer matches the Indic preparation.");
        }
    }

    void ValidateRuneIndex(int index)
    {
        if (index < 0 || index >= initialSourceIndexes.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }
    }

    void ValidateSyllableIndex(int syllableIndex)
    {
        if (syllableIndex < 0 ||
            syllableIndex >= syllableMap.GetSyllableCount())
        {
            throw new System.ArgumentOutOfRangeException("syllableIndex");
        }
    }

    static int FindTargetIndex(int[] indexes, int sourceIndex)
    {
        if (sourceIndex < 0)
        {
            return -1;
        }

        int index = 0;
        while (index < indexes.Length)
        {
            if (indexes[index] == sourceIndex)
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    static int[] CopyIndexes(int[] indexes)
    {
        int[] result = new int[indexes.Length];
        System.Array.Copy(indexes, result, indexes.Length);
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
}

internal static class IndicReorderer
{
    internal static IndicShaperPreparation Prepare(
        ShapingBuffer buffer,
        UnicodeData unicodeData,
        IndicScriptConfiguration configuration,
        IndicShapingModel model,
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

        if (configuration == null)
        {
            throw new System.ArgumentNullException("configuration");
        }

        IndicSyllableMap map = IndicSyllableRecognizer.Recognize(
            buffer,
            unicodeData,
            model,
            maximumSyllableCount);
        int syllableCount = map.GetSyllableCount();
        int[] baseIndexes = new int[syllableCount];
        int[] rephLengths = new int[syllableCount];
        int[] prebaseStarts = new int[syllableCount];
        int index = 0;
        while (index < syllableCount)
        {
            baseIndexes[index] = -1;
            prebaseStarts[index] = -1;
            index++;
        }

        int[] initialIndexes = new int[buffer.GetCount()];
        index = 0;
        while (index < initialIndexes.Length)
        {
            initialIndexes[index] = index;
            index++;
        }

        int syllableIndex = 0;
        while (syllableIndex < syllableCount)
        {
            AnalyzeSyllable(
                map,
                configuration,
                syllableIndex,
                baseIndexes,
                rephLengths,
                prebaseStarts);
            BuildInitialSyllableOrder(
                map,
                syllableIndex,
                baseIndexes[syllableIndex],
                rephLengths[syllableIndex],
                initialIndexes);
            syllableIndex++;
        }

        int[] finalIndexes = new int[initialIndexes.Length];
        System.Array.Copy(
            initialIndexes,
            finalIndexes,
            initialIndexes.Length);
        syllableIndex = 0;
        while (syllableIndex < syllableCount)
        {
            BuildFinalSyllableOrder(
                map,
                configuration,
                syllableIndex,
                baseIndexes[syllableIndex],
                rephLengths[syllableIndex],
                prebaseStarts[syllableIndex],
                finalIndexes);
            syllableIndex++;
        }

        return new IndicShaperPreparation(
            buffer,
            configuration.GetScript(),
            model,
            map,
            baseIndexes,
            rephLengths,
            prebaseStarts,
            initialIndexes,
            finalIndexes);
    }

    static void AnalyzeSyllable(
        IndicSyllableMap map,
        IndicScriptConfiguration configuration,
        int syllableIndex,
        int[] baseIndexes,
        int[] rephLengths,
        int[] prebaseStarts)
    {
        IndicSyllableType type = map.GetSyllableType(syllableIndex);
        if (type == IndicSyllableType.NonIndic ||
            type == IndicSyllableType.Symbol)
        {
            return;
        }

        int start = map.GetSyllableStart(syllableIndex);
        int end = map.GetSyllableEnd(syllableIndex);
        int rephLength = FindRephLength(
            map,
            configuration.GetRephMode(),
            start,
            end);
        int prebaseStart = FindPrebaseStart(
            map,
            start + rephLength,
            end);
        int baseIndex = FindBase(
            map,
            start + rephLength,
            end,
            prebaseStart);
        rephLengths[syllableIndex] = rephLength;
        prebaseStarts[syllableIndex] = prebaseStart;
        baseIndexes[syllableIndex] = baseIndex;
    }

    static int FindRephLength(
        IndicSyllableMap map,
        IndicRephMode mode,
        int start,
        int end)
    {
        int length = 0;
        if (mode == IndicRephMode.Implicit && start + 2 < end &&
            map.GetCategory(start) == IndicSyllabicCategory.Ra &&
            map.GetCategory(start + 1) == IndicSyllabicCategory.Halant &&
            !IsJoiner(map.GetCategory(start + 2)))
        {
            length = 2;
        }
        else if (mode == IndicRephMode.Explicit && start + 3 < end &&
            map.GetCategory(start) == IndicSyllabicCategory.Ra &&
            map.GetCategory(start + 1) == IndicSyllabicCategory.Halant &&
            map.GetCategory(start + 2) == IndicSyllabicCategory.Joiner)
        {
            length = 3;
        }
        else if (mode == IndicRephMode.Logical && start + 1 < end &&
            map.GetCategory(start) == IndicSyllabicCategory.Repha)
        {
            length = 1;
        }

        if (length > 0 && HasBaseCandidate(map, start + length, end))
        {
            return length;
        }

        return 0;
    }

    static int FindPrebaseStart(
        IndicSyllableMap map,
        int start,
        int end)
    {
        int index = end - 2;
        while (index >= start)
        {
            if (map.GetCategory(index) == IndicSyllabicCategory.Halant &&
                map.GetCategory(index + 1) == IndicSyllabicCategory.Ra &&
                HasBaseCandidate(map, start, index))
            {
                return index;
            }

            index--;
        }

        return -1;
    }

    static int FindBase(
        IndicSyllableMap map,
        int start,
        int end,
        int prebaseStart)
    {
        int index = end - 1;
        while (index >= start)
        {
            if ((prebaseStart < 0 || index != prebaseStart + 1) &&
                IsBaseCategory(map.GetCategory(index)))
            {
                return index;
            }

            index--;
        }

        return -1;
    }

    static bool HasBaseCandidate(
        IndicSyllableMap map,
        int start,
        int end)
    {
        int index = start;
        while (index < end)
        {
            if (IsBaseCategory(map.GetCategory(index)))
            {
                return true;
            }

            index++;
        }

        return false;
    }

    static bool IsBaseCategory(IndicSyllabicCategory category)
    {
        return category == IndicSyllabicCategory.Consonant ||
            category == IndicSyllabicCategory.Ra ||
            category == IndicSyllabicCategory.Vowel ||
            category == IndicSyllabicCategory.Placeholder ||
            category == IndicSyllabicCategory.DottedCircle;
    }

    static void BuildInitialSyllableOrder(
        IndicSyllableMap map,
        int syllableIndex,
        int baseIndex,
        int rephLength,
        int[] indexes)
    {
        if (baseIndex < 0)
        {
            return;
        }

        int start = map.GetSyllableStart(syllableIndex);
        int end = map.GetSyllableEnd(syllableIndex);
        System.Collections.Generic.List<int> order =
            new System.Collections.Generic.List<int>();
        int index = start;
        while (index < start + rephLength)
        {
            order.Add(index);
            index++;
        }

        index = end - 1;
        while (index >= start + rephLength)
        {
            if (IsLeftMatra(map, index))
            {
                order.Add(index);
            }

            index--;
        }

        index = start + rephLength;
        while (index < end)
        {
            if (!IsLeftMatra(map, index))
            {
                order.Add(index);
            }

            index++;
        }

        CopyOrder(order, indexes, start);
    }

    static void BuildFinalSyllableOrder(
        IndicSyllableMap map,
        IndicScriptConfiguration configuration,
        int syllableIndex,
        int baseSourceIndex,
        int rephLength,
        int prebaseStart,
        int[] indexes)
    {
        if (baseSourceIndex < 0)
        {
            return;
        }

        int start = map.GetSyllableStart(syllableIndex);
        int end = map.GetSyllableEnd(syllableIndex);
        System.Collections.Generic.List<int> order =
            new System.Collections.Generic.List<int>();
        int index = start;
        while (index < end)
        {
            order.Add(indexes[index]);
            index++;
        }

        RepositionLeftMatras(map, order, baseSourceIndex);
        RepositionReph(
            map,
            order,
            start,
            rephLength,
            baseSourceIndex,
            configuration.GetRephPosition());
        RepositionPrebaseConsonant(
            order,
            prebaseStart,
            baseSourceIndex);
        CopyOrder(order, indexes, start);
    }

    static void RepositionLeftMatras(
        IndicSyllableMap map,
        System.Collections.Generic.List<int> order,
        int baseSourceIndex)
    {
        System.Collections.Generic.List<int> matras =
            new System.Collections.Generic.List<int>();
        int index = 0;
        while (index < order.Count)
        {
            if (IsLeftMatra(map, order[index]))
            {
                matras.Add(order[index]);
                order.RemoveAt(index);
            }
            else
            {
                index++;
            }
        }

        if (matras.Count == 0)
        {
            return;
        }

        int baseIndex = IndexOf(order, baseSourceIndex);
        int target = 0;
        index = baseIndex - 1;
        while (index >= 0)
        {
            IndicSyllabicCategory category =
                map.GetCategory(order[index]);
            if (category == IndicSyllabicCategory.Halant)
            {
                bool followedByJoiner = index + 1 < baseIndex &&
                    map.GetCategory(order[index + 1]) ==
                    IndicSyllabicCategory.Joiner;
                if (!followedByJoiner)
                {
                    target = index + 1;
                    if (target < baseIndex && map.GetCategory(order[target]) ==
                        IndicSyllabicCategory.NonJoiner)
                    {
                        target++;
                    }

                    break;
                }
            }

            index--;
        }

        index = 0;
        while (index < matras.Count)
        {
            order.Insert(target + index, matras[index]);
            index++;
        }
    }

    static void RepositionReph(
        IndicSyllableMap map,
        System.Collections.Generic.List<int> order,
        int logicalStart,
        int rephLength,
        int baseSourceIndex,
        IndicRephPosition position)
    {
        if (rephLength == 0)
        {
            return;
        }

        System.Collections.Generic.List<int> reph =
            new System.Collections.Generic.List<int>();
        int sourceIndex = logicalStart;
        while (sourceIndex < logicalStart + rephLength)
        {
            int currentIndex = IndexOf(order, sourceIndex);
            if (currentIndex >= 0)
            {
                reph.Add(sourceIndex);
                order.RemoveAt(currentIndex);
            }

            sourceIndex++;
        }

        int target = IndexOf(order, baseSourceIndex) + 1;
        if (position == IndicRephPosition.AfterPost)
        {
            while (target < order.Count &&
                IsBeforeTrailingModifier(map.GetCategory(order[target])))
            {
                target++;
            }
        }

        int index = 0;
        while (index < reph.Count)
        {
            order.Insert(target + index, reph[index]);
            index++;
        }
    }

    static bool IsBeforeTrailingModifier(
        IndicSyllabicCategory category)
    {
        return category != IndicSyllabicCategory.SyllableModifier &&
            category != IndicSyllabicCategory.SyllableModifierPost &&
            category != IndicSyllabicCategory.Accent;
    }

    static void RepositionPrebaseConsonant(
        System.Collections.Generic.List<int> order,
        int prebaseStart,
        int baseSourceIndex)
    {
        if (prebaseStart < 0)
        {
            return;
        }

        int halantIndex = IndexOf(order, prebaseStart);
        int raIndex = IndexOf(order, prebaseStart + 1);
        if (halantIndex < 0 || raIndex < 0)
        {
            return;
        }

        int first = halantIndex < raIndex ? halantIndex : raIndex;
        int second = halantIndex < raIndex ? raIndex : halantIndex;
        int firstSource = order[first];
        int secondSource = order[second];
        order.RemoveAt(second);
        order.RemoveAt(first);
        int target = IndexOf(order, baseSourceIndex);
        order.Insert(target, firstSource);
        order.Insert(target + 1, secondSource);
    }

    static bool IsLeftMatra(IndicSyllableMap map, int index)
    {
        IndicSyllabicCategory category = map.GetCategory(index);
        if (category != IndicSyllabicCategory.Matra &&
            category != IndicSyllabicCategory.PostMatra)
        {
            return false;
        }

        IndicPositionalCategory position = map.GetPosition(index);
        return position == IndicPositionalCategory.Left ||
            position == IndicPositionalCategory.LeftAndRight ||
            position == IndicPositionalCategory.TopAndBottomAndLeft ||
            position == IndicPositionalCategory.TopAndLeft ||
            position == IndicPositionalCategory.TopAndLeftAndRight ||
            position == IndicPositionalCategory.VisualOrderLeft;
    }

    static bool IsJoiner(IndicSyllabicCategory category)
    {
        return category == IndicSyllabicCategory.Joiner ||
            category == IndicSyllabicCategory.NonJoiner;
    }

    static int IndexOf(
        System.Collections.Generic.List<int> values,
        int value)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (values[index] == value)
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    static void CopyOrder(
        System.Collections.Generic.List<int> order,
        int[] indexes,
        int start)
    {
        int index = 0;
        while (index < order.Count)
        {
            indexes[start + index] = order[index];
            index++;
        }
    }
}
