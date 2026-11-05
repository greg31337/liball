namespace ttf;

internal enum BidiParagraphDirection
{
    Auto = 0,
    LeftToRight = 1,
    RightToLeft = 2
}

internal enum BidiClass
{
    L = 0,
    R = 1,
    AL = 2,
    EN = 3,
    ES = 4,
    ET = 5,
    AN = 6,
    CS = 7,
    NSM = 8,
    BN = 9,
    B = 10,
    S = 11,
    WS = 12,
    ON = 13,
    LRE = 14,
    LRO = 15,
    RLE = 16,
    RLO = 17,
    PDF = 18,
    LRI = 19,
    RLI = 20,
    FSI = 21,
    PDI = 22
}

internal class BidiResult
{
    int defaultParagraphLevel;
    int[] paragraphLevels;
    int[] levels;
    BidiClass[] resolvedClasses;
    bool[] removed;
    int[] visualToLogical;
    int[] logicalToVisual;
    int[] visualCodePoints;
    int[] utf16Starts;
    int[] utf16Lengths;
    int sourceLength;
    int[] runVisualStarts;
    int[] runVisualLengths;
    int[] runLevels;

    internal BidiResult(
        int defaultParagraphLevel,
        int[] paragraphLevels,
        int[] levels,
        BidiClass[] resolvedClasses,
        bool[] removed,
        int[] visualToLogical,
        int[] logicalToVisual,
        int[] visualCodePoints,
        int[] utf16Starts,
        int[] utf16Lengths,
        int sourceLength,
        int[] runVisualStarts,
        int[] runVisualLengths,
        int[] runLevels)
    {
        this.defaultParagraphLevel = defaultParagraphLevel;
        this.paragraphLevels = paragraphLevels;
        this.levels = levels;
        this.resolvedClasses = resolvedClasses;
        this.removed = removed;
        this.visualToLogical = visualToLogical;
        this.logicalToVisual = logicalToVisual;
        this.visualCodePoints = visualCodePoints;
        this.utf16Starts = utf16Starts;
        this.utf16Lengths = utf16Lengths;
        this.sourceLength = sourceLength;
        this.runVisualStarts = runVisualStarts;
        this.runVisualLengths = runVisualLengths;
        this.runLevels = runLevels;
    }

    internal int GetRuneCount()
    {
        return levels.Length;
    }

    internal int GetParagraphLevel()
    {
        return defaultParagraphLevel;
    }

    internal int GetParagraphLevelAtIndex(int logicalIndex)
    {
        ValidateLogicalIndex(logicalIndex);
        return paragraphLevels[logicalIndex];
    }

    internal int GetLevel(int logicalIndex)
    {
        ValidateLogicalIndex(logicalIndex);
        return levels[logicalIndex];
    }

    internal BidiClass GetResolvedClass(int logicalIndex)
    {
        ValidateLogicalIndex(logicalIndex);
        return resolvedClasses[logicalIndex];
    }

    internal bool IsRemovedFromVisualOrder(int logicalIndex)
    {
        ValidateLogicalIndex(logicalIndex);
        return removed[logicalIndex];
    }

    internal int GetVisualRuneCount()
    {
        return visualToLogical.Length;
    }

    internal int GetLogicalIndex(int visualIndex)
    {
        ValidateVisualIndex(visualIndex);
        return visualToLogical[visualIndex];
    }

    internal int GetVisualIndex(int logicalIndex)
    {
        ValidateLogicalIndex(logicalIndex);
        return logicalToVisual[logicalIndex];
    }

    internal int GetVisualCodePoint(int visualIndex)
    {
        ValidateVisualIndex(visualIndex);
        return visualCodePoints[visualIndex];
    }

    internal int GetVisualUtf16Start(int visualIndex)
    {
        int logicalIndex = GetLogicalIndex(visualIndex);
        return utf16Starts[logicalIndex];
    }

    internal int GetVisualUtf16Length(int visualIndex)
    {
        int logicalIndex = GetLogicalIndex(visualIndex);
        return utf16Lengths[logicalIndex];
    }

    internal int GetVisualUtf16End(int visualIndex)
    {
        return GetVisualUtf16Start(visualIndex) +
            GetVisualUtf16Length(visualIndex);
    }

    internal int GetSourceLength()
    {
        return sourceLength;
    }

    internal int GetRunCount()
    {
        return runVisualStarts.Length;
    }

    internal int GetRunVisualStart(int runIndex)
    {
        ValidateRunIndex(runIndex);
        return runVisualStarts[runIndex];
    }

    internal int GetRunVisualLength(int runIndex)
    {
        ValidateRunIndex(runIndex);
        return runVisualLengths[runIndex];
    }

    internal int GetRunVisualEnd(int runIndex)
    {
        return GetRunVisualStart(runIndex) + GetRunVisualLength(runIndex);
    }

    internal int GetRunLevel(int runIndex)
    {
        ValidateRunIndex(runIndex);
        return runLevels[runIndex];
    }

    internal bool IsRunRightToLeft(int runIndex)
    {
        return (GetRunLevel(runIndex) & 1) != 0;
    }

    void ValidateLogicalIndex(int logicalIndex)
    {
        if (logicalIndex < 0 || logicalIndex >= levels.Length)
        {
            throw new System.ArgumentOutOfRangeException("logicalIndex");
        }
    }

    void ValidateVisualIndex(int visualIndex)
    {
        if (visualIndex < 0 || visualIndex >= visualToLogical.Length)
        {
            throw new System.ArgumentOutOfRangeException("visualIndex");
        }
    }

    void ValidateRunIndex(int runIndex)
    {
        if (runIndex < 0 || runIndex >= runVisualStarts.Length)
        {
            throw new System.ArgumentOutOfRangeException("runIndex");
        }
    }
}

internal static class BidiAlgorithm
{
    const int MaximumDepth = 125;
    const int BracketStackCapacity = 63;
    const string RuneLimitMessage =
        "Bidirectional resolution exceeds the configured rune limit.";
    const string RunLimitMessage =
        "Bidirectional resolution exceeds the configured visual run limit.";

    internal static BidiResult Resolve(
        RuneBuffer input,
        UnicodeData unicodeData,
        BidiParagraphDirection paragraphDirection,
        int maximumRuneCount,
        int maximumRunCount)
    {
        ValidateArguments(
            input,
            unicodeData,
            paragraphDirection,
            maximumRuneCount,
            maximumRunCount);

        int count = input.GetCount();
        BidiClass[] originalTypes = new BidiClass[count];
        BidiClass[] types = new BidiClass[count];
        int[] levels = new int[count];
        int[] paragraphLevels = new int[count];
        bool[] removed = new bool[count];
        int[] matchingIsolates = CreateFilledArray(count, -1);
        int[] matchingInitiators = CreateFilledArray(count, -1);
        int[] paragraphIds = new int[count];
        int[] utf16Starts = new int[count];
        int[] utf16Lengths = new int[count];

        int index = 0;
        while (index < count)
        {
            int codePoint = input.GetScalarValue(index);
            BidiClass bidiClass = ParseBidiClass(unicodeData.GetPropertyName(
                UnicodeDataTags.Bidi,
                codePoint));
            originalTypes[index] = bidiClass;
            types[index] = bidiClass;
            removed[index] = IsRemovedType(bidiClass);
            utf16Starts[index] = input.GetUtf16Start(index);
            utf16Lengths[index] = input.GetUtf16Length(index);
            index++;
        }

        int paragraphStart = 0;
        int paragraphId = 0;
        int firstParagraphLevel = DirectionLevel(paragraphDirection);
        while (paragraphStart < count)
        {
            int paragraphEnd = FindParagraphEnd(originalTypes, paragraphStart);
            MatchIsolates(
                originalTypes,
                paragraphStart,
                paragraphEnd,
                matchingIsolates,
                matchingInitiators);
            int paragraphLevel = ResolveParagraphLevel(
                originalTypes,
                matchingIsolates,
                paragraphStart,
                paragraphEnd,
                paragraphDirection);
            if (paragraphStart == 0)
            {
                firstParagraphLevel = paragraphLevel;
            }

            index = paragraphStart;
            while (index < paragraphEnd)
            {
                paragraphLevels[index] = paragraphLevel;
                paragraphIds[index] = paragraphId;
                index++;
            }

            ResolveParagraph(
                input,
                unicodeData,
                originalTypes,
                types,
                levels,
                matchingIsolates,
                matchingInitiators,
                paragraphStart,
                paragraphEnd,
                paragraphLevel);
            paragraphStart = paragraphEnd;
            paragraphId++;
        }

        int visualCount = CountVisualCharacters(removed);
        int[] visualToLogical = new int[visualCount];
        ReorderParagraphs(
            originalTypes,
            levels,
            removed,
            visualToLogical);
        int[] logicalToVisual = CreateFilledArray(count, -1);
        int[] visualCodePoints = new int[visualCount];
        index = 0;
        while (index < visualCount)
        {
            int logicalIndex = visualToLogical[index];
            logicalToVisual[logicalIndex] = index;
            int codePoint = input.GetScalarValue(logicalIndex);
            if ((levels[logicalIndex] & 1) != 0)
            {
                int mirrored = unicodeData.GetMapping(
                    UnicodeDataTags.Mirror,
                    codePoint);
                if (mirrored >= 0)
                {
                    codePoint = mirrored;
                }
            }

            visualCodePoints[index] = codePoint;
            index++;
        }

        int runCount = CountVisualRuns(
            visualToLogical,
            levels,
            paragraphIds);
        if (runCount > maximumRunCount)
        {
            throw FontLimitException.Create(RunLimitMessage);
        }

        int[] runVisualStarts = new int[runCount];
        int[] runVisualLengths = new int[runCount];
        int[] runLevels = new int[runCount];
        FillVisualRuns(
            visualToLogical,
            levels,
            paragraphIds,
            runVisualStarts,
            runVisualLengths,
            runLevels);

        return new BidiResult(
            firstParagraphLevel,
            paragraphLevels,
            levels,
            types,
            removed,
            visualToLogical,
            logicalToVisual,
            visualCodePoints,
            utf16Starts,
            utf16Lengths,
            input.GetSourceLength(),
            runVisualStarts,
            runVisualLengths,
            runLevels);
    }

    static void ValidateArguments(
        RuneBuffer input,
        UnicodeData unicodeData,
        BidiParagraphDirection paragraphDirection,
        int maximumRuneCount,
        int maximumRunCount)
    {
        if (input == null)
        {
            throw new System.ArgumentNullException("input");
        }

        if (unicodeData == null)
        {
            throw new System.ArgumentNullException("unicodeData");
        }

        if (paragraphDirection != BidiParagraphDirection.Auto &&
            paragraphDirection != BidiParagraphDirection.LeftToRight &&
            paragraphDirection != BidiParagraphDirection.RightToLeft)
        {
            throw new System.ArgumentOutOfRangeException("paragraphDirection");
        }

        if (maximumRuneCount < 0)
        {
            throw new System.ArgumentOutOfRangeException("maximumRuneCount");
        }

        if (maximumRunCount < 0)
        {
            throw new System.ArgumentOutOfRangeException("maximumRunCount");
        }

        if (input.GetCount() > maximumRuneCount)
        {
            throw FontLimitException.Create(RuneLimitMessage);
        }
    }

    static int DirectionLevel(BidiParagraphDirection direction)
    {
        return direction == BidiParagraphDirection.RightToLeft ? 1 : 0;
    }

    static int FindParagraphEnd(BidiClass[] originalTypes, int start)
    {
        int index = start;
        while (index < originalTypes.Length)
        {
            if (originalTypes[index] == BidiClass.B)
            {
                return index + 1;
            }

            index++;
        }

        return originalTypes.Length;
    }

    static void MatchIsolates(
        BidiClass[] originalTypes,
        int start,
        int end,
        int[] matchingIsolates,
        int[] matchingInitiators)
    {
        int[] stack = new int[end - start];
        int stackCount = 0;
        int index = start;
        while (index < end)
        {
            BidiClass bidiClass = originalTypes[index];
            if (IsIsolateInitiator(bidiClass))
            {
                stack[stackCount] = index;
                stackCount++;
            }
            else if (bidiClass == BidiClass.PDI && stackCount != 0)
            {
                stackCount--;
                int initiator = stack[stackCount];
                matchingIsolates[initiator] = index;
                matchingInitiators[index] = initiator;
            }

            index++;
        }
    }

    static int ResolveParagraphLevel(
        BidiClass[] originalTypes,
        int[] matchingIsolates,
        int start,
        int end,
        BidiParagraphDirection direction)
    {
        if (direction != BidiParagraphDirection.Auto)
        {
            return DirectionLevel(direction);
        }

        return ResolveAutoLevel(originalTypes, matchingIsolates, start, end);
    }

    static int ResolveAutoLevel(
        BidiClass[] originalTypes,
        int[] matchingIsolates,
        int start,
        int end)
    {
        int index = start;
        while (index < end)
        {
            BidiClass bidiClass = originalTypes[index];
            if (bidiClass == BidiClass.L)
            {
                return 0;
            }

            if (bidiClass == BidiClass.R || bidiClass == BidiClass.AL)
            {
                return 1;
            }

            if (IsIsolateInitiator(bidiClass))
            {
                int matching = matchingIsolates[index];
                if (matching < 0 || matching >= end)
                {
                    return 0;
                }

                index = matching + 1;
            }
            else
            {
                index++;
            }
        }

        return 0;
    }

    static void ResolveParagraph(
        RuneBuffer input,
        UnicodeData unicodeData,
        BidiClass[] originalTypes,
        BidiClass[] types,
        int[] levels,
        int[] matchingIsolates,
        int[] matchingInitiators,
        int start,
        int end,
        int paragraphLevel)
    {
        ResolveExplicit(
            originalTypes,
            types,
            levels,
            matchingIsolates,
            start,
            end,
            paragraphLevel);
        ResolveIsolatingRunSequences(
            input,
            unicodeData,
            originalTypes,
            types,
            levels,
            matchingIsolates,
            matchingInitiators,
            start,
            end,
            paragraphLevel);
        ResetLineLevels(
            originalTypes,
            levels,
            start,
            end,
            paragraphLevel);
    }

    static void ResolveExplicit(
        BidiClass[] originalTypes,
        BidiClass[] types,
        int[] levels,
        int[] matchingIsolates,
        int start,
        int end,
        int paragraphLevel)
    {
        int[] stackLevels = new int[MaximumDepth + 2];
        int[] stackOverrides = new int[MaximumDepth + 2];
        bool[] stackIsolates = new bool[MaximumDepth + 2];
        int stackCount = 1;
        int overflowIsolateCount = 0;
        int overflowEmbeddingCount = 0;
        int validIsolateCount = 0;
        stackLevels[0] = paragraphLevel;
        stackOverrides[0] = 0;
        stackIsolates[0] = false;

        int index = start;
        while (index < end)
        {
            BidiClass originalType = originalTypes[index];
            int currentLevel = stackLevels[stackCount - 1];
            int currentOverride = stackOverrides[stackCount - 1];
            levels[index] = currentLevel;

            if (originalType == BidiClass.RLE ||
                originalType == BidiClass.RLO ||
                originalType == BidiClass.LRE ||
                originalType == BidiClass.LRO)
            {
                int newLevel = originalType == BidiClass.RLE ||
                    originalType == BidiClass.RLO ?
                    LeastGreaterOdd(currentLevel) :
                    LeastGreaterEven(currentLevel);
                if (newLevel <= MaximumDepth &&
                    overflowIsolateCount == 0 &&
                    overflowEmbeddingCount == 0)
                {
                    stackLevels[stackCount] = newLevel;
                    if (originalType == BidiClass.RLO)
                    {
                        stackOverrides[stackCount] = 2;
                    }
                    else if (originalType == BidiClass.LRO)
                    {
                        stackOverrides[stackCount] = 1;
                    }
                    else
                    {
                        stackOverrides[stackCount] = 0;
                    }

                    stackIsolates[stackCount] = false;
                    stackCount++;
                }
                else if (overflowIsolateCount == 0)
                {
                    overflowEmbeddingCount++;
                }
            }
            else if (IsIsolateInitiator(originalType))
            {
                if (currentOverride == 1)
                {
                    types[index] = BidiClass.L;
                }
                else if (currentOverride == 2)
                {
                    types[index] = BidiClass.R;
                }

                BidiClass isolateType = originalType;
                if (originalType == BidiClass.FSI)
                {
                    int isolateEnd = matchingIsolates[index];
                    if (isolateEnd < 0 || isolateEnd > end)
                    {
                        isolateEnd = end;
                    }

                    int isolateLevel = ResolveAutoLevel(
                        originalTypes,
                        matchingIsolates,
                        index + 1,
                        isolateEnd);
                    isolateType = isolateLevel == 1 ?
                        BidiClass.RLI : BidiClass.LRI;
                }

                int newLevel = isolateType == BidiClass.RLI ?
                    LeastGreaterOdd(currentLevel) :
                    LeastGreaterEven(currentLevel);
                if (newLevel <= MaximumDepth &&
                    overflowIsolateCount == 0 &&
                    overflowEmbeddingCount == 0)
                {
                    validIsolateCount++;
                    stackLevels[stackCount] = newLevel;
                    stackOverrides[stackCount] = 0;
                    stackIsolates[stackCount] = true;
                    stackCount++;
                }
                else
                {
                    overflowIsolateCount++;
                }
            }
            else if (originalType == BidiClass.PDI)
            {
                if (overflowIsolateCount > 0)
                {
                    overflowIsolateCount--;
                }
                else if (validIsolateCount > 0)
                {
                    overflowEmbeddingCount = 0;
                    while (!stackIsolates[stackCount - 1])
                    {
                        stackCount--;
                    }

                    stackCount--;
                    validIsolateCount--;
                }

                currentLevel = stackLevels[stackCount - 1];
                currentOverride = stackOverrides[stackCount - 1];
                levels[index] = currentLevel;
                if (currentOverride == 1)
                {
                    types[index] = BidiClass.L;
                }
                else if (currentOverride == 2)
                {
                    types[index] = BidiClass.R;
                }
            }
            else if (originalType == BidiClass.PDF)
            {
                if (overflowIsolateCount == 0)
                {
                    if (overflowEmbeddingCount > 0)
                    {
                        overflowEmbeddingCount--;
                    }
                    else if (!stackIsolates[stackCount - 1] &&
                        stackCount >= 2)
                    {
                        stackCount--;
                    }
                }
            }
            else if (originalType == BidiClass.B)
            {
                levels[index] = paragraphLevel;
            }
            else if (originalType != BidiClass.BN)
            {
                if (currentOverride == 1)
                {
                    types[index] = BidiClass.L;
                }
                else if (currentOverride == 2)
                {
                    types[index] = BidiClass.R;
                }
            }

            index++;
        }
    }

    static int LeastGreaterOdd(int level)
    {
        return (level + 1) | 1;
    }

    static int LeastGreaterEven(int level)
    {
        return (level + 2) & -2;
    }

    static void ResolveIsolatingRunSequences(
        RuneBuffer input,
        UnicodeData unicodeData,
        BidiClass[] originalTypes,
        BidiClass[] types,
        int[] levels,
        int[] matchingIsolates,
        int[] matchingInitiators,
        int start,
        int end,
        int paragraphLevel)
    {
        int activeCount = CountActive(originalTypes, start, end);
        if (activeCount == 0)
        {
            return;
        }

        int[] explicitLevels = new int[levels.Length];
        System.Array.Copy(levels, explicitLevels, levels.Length);
        BidiClass[] explicitTypes = new BidiClass[types.Length];
        System.Array.Copy(types, explicitTypes, types.Length);

        int[] active = new int[activeCount];
        int[] activePosition = CreateFilledArray(types.Length, -1);
        int activeIndex = 0;
        int logicalIndex = start;
        while (logicalIndex < end)
        {
            if (!IsRemovedType(originalTypes[logicalIndex]))
            {
                active[activeIndex] = logicalIndex;
                activePosition[logicalIndex] = activeIndex;
                activeIndex++;
            }

            logicalIndex++;
        }

        int runCount = CountLevelRuns(active, explicitLevels);
        int[] runStarts = new int[runCount];
        int[] runLengths = new int[runCount];
        int[] logicalToRun = CreateFilledArray(types.Length, -1);
        FillLevelRuns(
            active,
            explicitLevels,
            runStarts,
            runLengths,
            logicalToRun);

        int runIndex = 0;
        while (runIndex < runCount)
        {
            int firstLogical = active[runStarts[runIndex]];
            if (explicitTypes[firstLogical] != BidiClass.PDI ||
                matchingInitiators[firstLogical] < 0)
            {
                int[] sequence = new int[activeCount];
                int sequenceCount = 0;
                int currentRun = runIndex;
                bool following = true;
                while (following)
                {
                    int runStart = runStarts[currentRun];
                    int runEnd = runStart + runLengths[currentRun];
                    int position = runStart;
                    while (position < runEnd)
                    {
                        sequence[sequenceCount] = active[position];
                        sequenceCount++;
                        position++;
                    }

                    int lastLogical = active[runEnd - 1];
                    if (IsIsolateInitiator(explicitTypes[lastLogical]) &&
                        matchingIsolates[lastLogical] >= 0)
                    {
                        currentRun = logicalToRun[
                            matchingIsolates[lastLogical]];
                    }
                    else
                    {
                        following = false;
                    }
                }

                int precedingPosition = runStarts[runIndex] - 1;
                int precedingLevel = precedingPosition < 0 ?
                    paragraphLevel : explicitLevels[active[precedingPosition]];
                int sos = StrongFromLevel(System.Math.Max(
                    explicitLevels[sequence[0]],
                    precedingLevel));
                int lastSequenceLogical = sequence[sequenceCount - 1];
                int followingPosition = activePosition[lastSequenceLogical] + 1;
                int followingLevel = followingPosition >= activeCount ||
                    (IsIsolateInitiator(explicitTypes[lastSequenceLogical]) &&
                    matchingIsolates[lastSequenceLogical] < 0) ?
                    paragraphLevel : explicitLevels[active[followingPosition]];
                int eos = StrongFromLevel(System.Math.Max(
                    explicitLevels[lastSequenceLogical],
                    followingLevel));
                ResolveSequence(
                    input,
                    unicodeData,
                    originalTypes,
                    types,
                    levels,
                    sequence,
                    sequenceCount,
                    sos,
                    eos);
            }

            runIndex++;
        }
    }

    static int CountActive(BidiClass[] originalTypes, int start, int end)
    {
        int count = 0;
        int index = start;
        while (index < end)
        {
            if (!IsRemovedType(originalTypes[index]))
            {
                count++;
            }

            index++;
        }

        return count;
    }

    static int CountLevelRuns(int[] active, int[] levels)
    {
        if (active.Length == 0)
        {
            return 0;
        }

        int count = 1;
        int index = 1;
        while (index < active.Length)
        {
            if (levels[active[index]] != levels[active[index - 1]])
            {
                count++;
            }

            index++;
        }

        return count;
    }

    static void FillLevelRuns(
        int[] active,
        int[] levels,
        int[] runStarts,
        int[] runLengths,
        int[] logicalToRun)
    {
        int runIndex = 0;
        int runStart = 0;
        int index = 0;
        while (index <= active.Length)
        {
            if (index == active.Length ||
                (index > runStart &&
                levels[active[index]] != levels[active[index - 1]]))
            {
                runStarts[runIndex] = runStart;
                runLengths[runIndex] = index - runStart;
                int position = runStart;
                while (position < index)
                {
                    logicalToRun[active[position]] = runIndex;
                    position++;
                }

                runIndex++;
                runStart = index;
            }

            index++;
        }
    }

    static void ResolveSequence(
        RuneBuffer input,
        UnicodeData unicodeData,
        BidiClass[] originalTypes,
        BidiClass[] types,
        int[] levels,
        int[] sequence,
        int sequenceCount,
        int sos,
        int eos)
    {
        ResolveNonspacingMarks(types, sequence, sequenceCount, sos);
        ResolveEuropeanNumbers(types, sequence, sequenceCount, sos);
        ResolveArabicLetters(types, sequence, sequenceCount);
        ResolveNumberSeparators(types, sequence, sequenceCount);
        ResolveEuropeanTerminators(types, sequence, sequenceCount);
        ResolveRemainingSeparators(types, sequence, sequenceCount);
        ResolveEuropeanNumbersFromLeft(types, sequence, sequenceCount, sos);
        ResolveBrackets(
            input,
            unicodeData,
            originalTypes,
            types,
            levels,
            sequence,
            sequenceCount,
            sos);
        ResolveNeutrals(types, levels, sequence, sequenceCount, sos, eos);
        ResolveImplicitLevels(types, levels, sequence, sequenceCount);
    }

    static void ResolveNonspacingMarks(
        BidiClass[] types,
        int[] sequence,
        int count,
        int sos)
    {
        BidiClass previous = sos == 0 ? BidiClass.L : BidiClass.R;
        int index = 0;
        while (index < count)
        {
            int logical = sequence[index];
            if (types[logical] == BidiClass.NSM)
            {
                types[logical] = IsIsolateInitiator(previous) ||
                    previous == BidiClass.PDI ? BidiClass.ON : previous;
            }

            previous = types[logical];
            index++;
        }
    }

    static void ResolveEuropeanNumbers(
        BidiClass[] types,
        int[] sequence,
        int count,
        int sos)
    {
        BidiClass previousStrong = sos == 0 ? BidiClass.L : BidiClass.R;
        int index = 0;
        while (index < count)
        {
            int logical = sequence[index];
            BidiClass bidiClass = types[logical];
            if (bidiClass == BidiClass.EN && previousStrong == BidiClass.AL)
            {
                types[logical] = BidiClass.AN;
            }

            if (bidiClass == BidiClass.L ||
                bidiClass == BidiClass.R ||
                bidiClass == BidiClass.AL)
            {
                previousStrong = bidiClass;
            }

            index++;
        }
    }

    static void ResolveArabicLetters(
        BidiClass[] types,
        int[] sequence,
        int count)
    {
        int index = 0;
        while (index < count)
        {
            int logical = sequence[index];
            if (types[logical] == BidiClass.AL)
            {
                types[logical] = BidiClass.R;
            }

            index++;
        }
    }

    static void ResolveNumberSeparators(
        BidiClass[] types,
        int[] sequence,
        int count)
    {
        int index = 1;
        while (index + 1 < count)
        {
            int logical = sequence[index];
            BidiClass current = types[logical];
            BidiClass before = types[sequence[index - 1]];
            BidiClass after = types[sequence[index + 1]];
            if (current == BidiClass.ES &&
                before == BidiClass.EN && after == BidiClass.EN)
            {
                types[logical] = BidiClass.EN;
            }
            else if (current == BidiClass.CS &&
                before == after &&
                (before == BidiClass.EN || before == BidiClass.AN))
            {
                types[logical] = before;
            }

            index++;
        }
    }

    static void ResolveEuropeanTerminators(
        BidiClass[] types,
        int[] sequence,
        int count)
    {
        int index = 0;
        while (index < count)
        {
            if (types[sequence[index]] != BidiClass.ET)
            {
                index++;
                continue;
            }

            int start = index;
            while (index < count &&
                types[sequence[index]] == BidiClass.ET)
            {
                index++;
            }

            bool adjacent = start > 0 &&
                types[sequence[start - 1]] == BidiClass.EN;
            if (index < count &&
                types[sequence[index]] == BidiClass.EN)
            {
                adjacent = true;
            }

            if (adjacent)
            {
                int position = start;
                while (position < index)
                {
                    types[sequence[position]] = BidiClass.EN;
                    position++;
                }
            }
        }
    }

    static void ResolveRemainingSeparators(
        BidiClass[] types,
        int[] sequence,
        int count)
    {
        int index = 0;
        while (index < count)
        {
            int logical = sequence[index];
            BidiClass bidiClass = types[logical];
            if (bidiClass == BidiClass.ES ||
                bidiClass == BidiClass.ET ||
                bidiClass == BidiClass.CS)
            {
                types[logical] = BidiClass.ON;
            }

            index++;
        }
    }

    static void ResolveEuropeanNumbersFromLeft(
        BidiClass[] types,
        int[] sequence,
        int count,
        int sos)
    {
        BidiClass previousStrong = sos == 0 ? BidiClass.L : BidiClass.R;
        int index = 0;
        while (index < count)
        {
            int logical = sequence[index];
            BidiClass bidiClass = types[logical];
            if (bidiClass == BidiClass.EN && previousStrong == BidiClass.L)
            {
                types[logical] = BidiClass.L;
            }

            if (bidiClass == BidiClass.L || bidiClass == BidiClass.R)
            {
                previousStrong = bidiClass;
            }

            index++;
        }
    }

    static void ResolveBrackets(
        RuneBuffer input,
        UnicodeData unicodeData,
        BidiClass[] originalTypes,
        BidiClass[] types,
        int[] levels,
        int[] sequence,
        int sequenceCount,
        int sos)
    {
        int[] pairOpenPositions = new int[sequenceCount];
        int[] pairClosePositions = new int[sequenceCount];
        int pairCount = FindBracketPairs(
            input,
            unicodeData,
            types,
            sequence,
            sequenceCount,
            pairOpenPositions,
            pairClosePositions);
        int pairIndex = 0;
        while (pairIndex < pairCount)
        {
            int openPosition = pairOpenPositions[pairIndex];
            int closePosition = pairClosePositions[pairIndex];
            BidiClass embeddingDirection =
                (levels[sequence[openPosition]] & 1) == 0 ?
                BidiClass.L : BidiClass.R;
            BidiClass oppositeDirection =
                embeddingDirection == BidiClass.L ?
                BidiClass.R : BidiClass.L;
            bool foundEmbedding = false;
            bool foundOpposite = false;
            int position = openPosition + 1;
            while (position < closePosition)
            {
                BidiClass strong = StrongForNeutral(types[sequence[position]]);
                if (strong == embeddingDirection)
                {
                    foundEmbedding = true;
                    break;
                }

                if (strong == oppositeDirection)
                {
                    foundOpposite = true;
                }

                position++;
            }

            BidiClass resolved = BidiClass.ON;
            if (foundEmbedding)
            {
                resolved = embeddingDirection;
            }
            else if (foundOpposite)
            {
                BidiClass preceding = sos == 0 ? BidiClass.L : BidiClass.R;
                position = openPosition - 1;
                while (position >= 0)
                {
                    BidiClass strong = StrongForNeutral(
                        types[sequence[position]]);
                    if (strong == BidiClass.L || strong == BidiClass.R)
                    {
                        preceding = strong;
                        break;
                    }

                    position--;
                }

                resolved = preceding == oppositeDirection ?
                    oppositeDirection : embeddingDirection;
            }

            if (resolved != BidiClass.ON)
            {
                SetBracketAndFollowingMarks(
                    originalTypes,
                    types,
                    sequence,
                    sequenceCount,
                    openPosition,
                    resolved);
                SetBracketAndFollowingMarks(
                    originalTypes,
                    types,
                    sequence,
                    sequenceCount,
                    closePosition,
                    resolved);
            }

            pairIndex++;
        }
    }

    static int FindBracketPairs(
        RuneBuffer input,
        UnicodeData unicodeData,
        BidiClass[] types,
        int[] sequence,
        int sequenceCount,
        int[] pairOpenPositions,
        int[] pairClosePositions)
    {
        int[] stackValues = new int[BracketStackCapacity];
        int[] stackPositions = new int[BracketStackCapacity];
        int stackCount = 0;
        int pairCount = 0;
        int position = 0;
        while (position < sequenceCount)
        {
            int logical = sequence[position];
            if (types[logical] == BidiClass.ON)
            {
                int codePoint = input.GetScalarValue(logical);
                int bracketType = unicodeData.GetMappingAuxiliary(
                    UnicodeDataTags.Bracket,
                    codePoint);
                if (bracketType == 1)
                {
                    if (stackCount == BracketStackCapacity)
                    {
                        return 0;
                    }

                    stackValues[stackCount] = unicodeData.GetMapping(
                        UnicodeDataTags.Bracket,
                        codePoint);
                    stackPositions[stackCount] = position;
                    stackCount++;
                }
                else if (bracketType == 2)
                {
                    int stackIndex = stackCount - 1;
                    while (stackIndex >= 0)
                    {
                        if (BracketsMatch(stackValues[stackIndex], codePoint))
                        {
                            pairOpenPositions[pairCount] =
                                stackPositions[stackIndex];
                            pairClosePositions[pairCount] = position;
                            pairCount++;
                            stackCount = stackIndex;
                            break;
                        }

                        stackIndex--;
                    }
                }
            }

            position++;
        }

        SortBracketPairs(pairOpenPositions, pairClosePositions, pairCount);
        return pairCount;
    }

    static bool BracketsMatch(int expected, int actual)
    {
        if (expected == actual)
        {
            return true;
        }

        return (expected == 0x232A && actual == 0x3009) ||
            (expected == 0x3009 && actual == 0x232A);
    }

    static void SortBracketPairs(
        int[] openPositions,
        int[] closePositions,
        int count)
    {
        int index = 1;
        while (index < count)
        {
            int open = openPositions[index];
            int close = closePositions[index];
            int insertion = index - 1;
            while (insertion >= 0 && openPositions[insertion] > open)
            {
                openPositions[insertion + 1] = openPositions[insertion];
                closePositions[insertion + 1] = closePositions[insertion];
                insertion--;
            }

            openPositions[insertion + 1] = open;
            closePositions[insertion + 1] = close;
            index++;
        }
    }

    static void SetBracketAndFollowingMarks(
        BidiClass[] originalTypes,
        BidiClass[] types,
        int[] sequence,
        int sequenceCount,
        int position,
        BidiClass resolved)
    {
        types[sequence[position]] = resolved;
        position++;
        while (position < sequenceCount &&
            originalTypes[sequence[position]] == BidiClass.NSM)
        {
            types[sequence[position]] = resolved;
            position++;
        }
    }

    static void ResolveNeutrals(
        BidiClass[] types,
        int[] levels,
        int[] sequence,
        int count,
        int sos,
        int eos)
    {
        int index = 0;
        BidiClass before = sos == 0 ? BidiClass.L : BidiClass.R;
        while (index < count)
        {
            int logical = sequence[index];
            if (!IsNeutral(types[logical]))
            {
                BidiClass strong = StrongForNeutral(types[logical]);
                if (strong == BidiClass.L || strong == BidiClass.R)
                {
                    before = strong;
                }

                index++;
                continue;
            }

            int neutralStart = index;
            while (index < count && IsNeutral(types[sequence[index]]))
            {
                index++;
            }

            BidiClass after = index == count ?
                (eos == 0 ? BidiClass.L : BidiClass.R) :
                StrongForNeutral(types[sequence[index]]);
            BidiClass resolved = before == after ? before : BidiClass.ON;
            int position = neutralStart;
            while (position < index)
            {
                int neutralLogical = sequence[position];
                if (resolved == BidiClass.ON)
                {
                    types[neutralLogical] =
                        (levels[neutralLogical] & 1) == 0 ?
                        BidiClass.L : BidiClass.R;
                }
                else
                {
                    types[neutralLogical] = resolved;
                }

                position++;
            }

            before = after;
        }
    }

    static void ResolveImplicitLevels(
        BidiClass[] types,
        int[] levels,
        int[] sequence,
        int count)
    {
        int index = 0;
        while (index < count)
        {
            int logical = sequence[index];
            BidiClass bidiClass = types[logical];
            if ((levels[logical] & 1) == 0)
            {
                if (bidiClass == BidiClass.R)
                {
                    levels[logical]++;
                }
                else if (bidiClass == BidiClass.EN ||
                    bidiClass == BidiClass.AN)
                {
                    levels[logical] += 2;
                }
            }
            else if (bidiClass == BidiClass.L ||
                bidiClass == BidiClass.EN ||
                bidiClass == BidiClass.AN)
            {
                levels[logical]++;
            }

            index++;
        }
    }

    static BidiClass StrongForNeutral(BidiClass bidiClass)
    {
        if (bidiClass == BidiClass.EN || bidiClass == BidiClass.AN)
        {
            return BidiClass.R;
        }

        return bidiClass;
    }

    static int StrongFromLevel(int level)
    {
        return (level & 1) == 0 ? 0 : 1;
    }

    static bool IsNeutral(BidiClass bidiClass)
    {
        return bidiClass == BidiClass.B ||
            bidiClass == BidiClass.S ||
            bidiClass == BidiClass.WS ||
            bidiClass == BidiClass.ON ||
            bidiClass == BidiClass.LRI ||
            bidiClass == BidiClass.RLI ||
            bidiClass == BidiClass.FSI ||
            bidiClass == BidiClass.PDI;
    }

    static void ResetLineLevels(
        BidiClass[] originalTypes,
        int[] levels,
        int start,
        int end,
        int paragraphLevel)
    {
        int index = start;
        while (index < end)
        {
            BidiClass bidiClass = originalTypes[index];
            if (bidiClass == BidiClass.S || bidiClass == BidiClass.B)
            {
                levels[index] = paragraphLevel;
                int preceding = index - 1;
                bool scanning = true;
                while (preceding >= start && scanning)
                {
                    BidiClass precedingType = originalTypes[preceding];
                    if (IsRemovedType(precedingType))
                    {
                        preceding--;
                    }
                    else if (IsLineResetPredecessor(precedingType))
                    {
                        levels[preceding] = paragraphLevel;
                        preceding--;
                    }
                    else
                    {
                        scanning = false;
                    }
                }
            }

            index++;
        }

        index = end - 1;
        bool scanningEnd = true;
        while (index >= start && scanningEnd)
        {
            BidiClass bidiClass = originalTypes[index];
            if (IsRemovedType(bidiClass))
            {
                index--;
            }
            else if (IsLineResetPredecessor(bidiClass))
            {
                levels[index] = paragraphLevel;
                index--;
            }
            else
            {
                scanningEnd = false;
            }
        }
    }

    static bool IsLineResetPredecessor(BidiClass bidiClass)
    {
        return bidiClass == BidiClass.WS ||
            bidiClass == BidiClass.LRI ||
            bidiClass == BidiClass.RLI ||
            bidiClass == BidiClass.FSI ||
            bidiClass == BidiClass.PDI;
    }

    static int CountVisualCharacters(bool[] removed)
    {
        int count = 0;
        int index = 0;
        while (index < removed.Length)
        {
            if (!removed[index])
            {
                count++;
            }

            index++;
        }

        return count;
    }

    static void ReorderParagraphs(
        BidiClass[] originalTypes,
        int[] levels,
        bool[] removed,
        int[] visualToLogical)
    {
        int output = 0;
        int paragraphStart = 0;
        while (paragraphStart < originalTypes.Length)
        {
            int paragraphEnd = FindParagraphEnd(originalTypes, paragraphStart);
            int activeCount = 0;
            int index = paragraphStart;
            while (index < paragraphEnd)
            {
                if (!removed[index])
                {
                    activeCount++;
                }

                index++;
            }

            int[] order = new int[activeCount];
            int orderIndex = 0;
            index = paragraphStart;
            while (index < paragraphEnd)
            {
                if (!removed[index])
                {
                    order[orderIndex] = index;
                    orderIndex++;
                }

                index++;
            }

            ReorderLine(order, levels);
            index = 0;
            while (index < order.Length)
            {
                visualToLogical[output] = order[index];
                output++;
                index++;
            }

            paragraphStart = paragraphEnd;
        }
    }

    static void ReorderLine(int[] order, int[] levels)
    {
        int maximum = 0;
        int minimumOdd = MaximumDepth + 2;
        int index = 0;
        while (index < order.Length)
        {
            int level = levels[order[index]];
            if (level > maximum)
            {
                maximum = level;
            }

            if ((level & 1) != 0 && level < minimumOdd)
            {
                minimumOdd = level;
            }

            index++;
        }

        int threshold = maximum;
        while (threshold >= minimumOdd)
        {
            index = 0;
            while (index < order.Length)
            {
                while (index < order.Length &&
                    levels[order[index]] < threshold)
                {
                    index++;
                }

                int reverseStart = index;
                while (index < order.Length &&
                    levels[order[index]] >= threshold)
                {
                    index++;
                }

                Reverse(order, reverseStart, index - 1);
            }

            threshold--;
        }
    }

    static void Reverse(int[] values, int start, int end)
    {
        while (start < end)
        {
            int temporary = values[start];
            values[start] = values[end];
            values[end] = temporary;
            start++;
            end--;
        }
    }

    static int CountVisualRuns(
        int[] visualToLogical,
        int[] levels,
        int[] paragraphIds)
    {
        if (visualToLogical.Length == 0)
        {
            return 0;
        }

        int count = 1;
        int index = 1;
        while (index < visualToLogical.Length)
        {
            int current = visualToLogical[index];
            int previous = visualToLogical[index - 1];
            if (levels[current] != levels[previous] ||
                paragraphIds[current] != paragraphIds[previous])
            {
                count++;
            }

            index++;
        }

        return count;
    }

    static void FillVisualRuns(
        int[] visualToLogical,
        int[] levels,
        int[] paragraphIds,
        int[] runVisualStarts,
        int[] runVisualLengths,
        int[] runLevels)
    {
        int runIndex = 0;
        int runStart = 0;
        int index = 0;
        while (index <= visualToLogical.Length)
        {
            bool boundary = index == visualToLogical.Length;
            if (!boundary && index > runStart)
            {
                int current = visualToLogical[index];
                int previous = visualToLogical[index - 1];
                boundary = levels[current] != levels[previous] ||
                    paragraphIds[current] != paragraphIds[previous];
            }

            if (boundary && index > runStart)
            {
                runVisualStarts[runIndex] = runStart;
                runVisualLengths[runIndex] = index - runStart;
                runLevels[runIndex] = levels[visualToLogical[runStart]];
                runIndex++;
                runStart = index;
            }

            index++;
        }
    }

    static int[] CreateFilledArray(int length, int value)
    {
        int[] result = new int[length];
        int index = 0;
        while (index < length)
        {
            result[index] = value;
            index++;
        }

        return result;
    }

    static bool IsRemovedType(BidiClass bidiClass)
    {
        return bidiClass == BidiClass.RLE ||
            bidiClass == BidiClass.LRE ||
            bidiClass == BidiClass.RLO ||
            bidiClass == BidiClass.LRO ||
            bidiClass == BidiClass.PDF ||
            bidiClass == BidiClass.BN;
    }

    static bool IsIsolateInitiator(BidiClass bidiClass)
    {
        return bidiClass == BidiClass.LRI ||
            bidiClass == BidiClass.RLI ||
            bidiClass == BidiClass.FSI;
    }

    static BidiClass ParseBidiClass(string value)
    {
        if (value == "L")
        {
            return BidiClass.L;
        }

        if (value == "R")
        {
            return BidiClass.R;
        }

        if (value == "AL")
        {
            return BidiClass.AL;
        }

        if (value == "EN")
        {
            return BidiClass.EN;
        }

        if (value == "ES")
        {
            return BidiClass.ES;
        }

        if (value == "ET")
        {
            return BidiClass.ET;
        }

        if (value == "AN")
        {
            return BidiClass.AN;
        }

        if (value == "CS")
        {
            return BidiClass.CS;
        }

        if (value == "NSM")
        {
            return BidiClass.NSM;
        }

        if (value == "BN")
        {
            return BidiClass.BN;
        }

        if (value == "B")
        {
            return BidiClass.B;
        }

        if (value == "S")
        {
            return BidiClass.S;
        }

        if (value == "WS")
        {
            return BidiClass.WS;
        }

        if (value == "ON")
        {
            return BidiClass.ON;
        }

        if (value == "LRE")
        {
            return BidiClass.LRE;
        }

        if (value == "LRO")
        {
            return BidiClass.LRO;
        }

        if (value == "RLE")
        {
            return BidiClass.RLE;
        }

        if (value == "RLO")
        {
            return BidiClass.RLO;
        }

        if (value == "PDF")
        {
            return BidiClass.PDF;
        }

        if (value == "LRI")
        {
            return BidiClass.LRI;
        }

        if (value == "RLI")
        {
            return BidiClass.RLI;
        }

        if (value == "FSI")
        {
            return BidiClass.FSI;
        }

        if (value == "PDI")
        {
            return BidiClass.PDI;
        }

        throw new System.InvalidOperationException(
            "The Unicode bidi class is not recognized: " + value + ".");
    }
}
