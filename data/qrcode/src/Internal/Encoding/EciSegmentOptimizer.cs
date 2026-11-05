using System.Collections.Generic;

namespace qrcode;

internal class EciSegmentOptimizer
{
    static readonly int[] BuiltInAssignments = new int[]
    {
        QrEciAssignments.Iso8859_1,
        1,
        QrEciAssignments.ShiftJis,
        QrEciAssignments.Utf8,
        QrEciAssignments.Utf16BigEndian,
        QrEciAssignments.UsAscii,
        QrEciAssignments.Gb2312,
        QrEciAssignments.Utf16LittleEndian,
        QrEciAssignments.Utf32BigEndian,
        QrEciAssignments.Utf32LittleEndian
    };

    internal static QrSegment[] OptimizeText(
        string text,
        QrSymbolKind symbolKind,
        int version,
        int[] assignmentPriorities,
        int maximumSegments,
        int maximumInputBytes)
    {
        if (text == null)
        {
            throw new QrInputException("The text to optimize cannot be null.", "text");
        }

        if (assignmentPriorities == null)
        {
            throw new QrInputException("The ECI priority list cannot be null.", "assignmentPriorities");
        }

        SegmentValidator.ValidateSegments(
            new QrSegment[0],
            symbolKind,
            version,
            maximumSegments,
            maximumInputBytes,
            false);
        if (text.Length > maximumInputBytes)
        {
            throw new QrResourceLimitException(
                "The text exceeds the configured input byte limit.",
                "MaximumInputBytes",
                text.Length,
                maximumInputBytes);
        }

        int[] assignments = NormalizeAssignments(assignmentPriorities);
        if (text.Length == 0)
        {
            return new QrSegment[0];
        }

        int stateCount = assignments.Length;
        int initialState = FindAssignmentIndex(assignments, QrEciAssignments.Iso8859_1);
        int modeIndicatorWidth = GetModeIndicatorWidth(symbolKind, version);
        bool supportsEci = symbolKind == QrSymbolKind.Model2 || symbolKind == QrSymbolKind.rMQR;
        int[] countWidths = new int[4];
        int[] maximumCounts = new int[4];
        for (int modeIndex = 0; modeIndex < 4; modeIndex++)
        {
            countWidths[modeIndex] = GetCharacterCountWidth(symbolKind, version, modeIndex);
            if (countWidths[modeIndex] > 0)
            {
                maximumCounts[modeIndex] = (1 << countWidths[modeIndex]) - 1;
            }
        }

        int boundaryCount = CheckedDimensions.CalculateSum(
            text.Length,
            1,
            int.MaxValue,
            "EciOptimizationBoundaries");
        int stateBoundaryCount = CheckedDimensions.CalculateProduct(
            stateCount,
            boundaryCount,
            int.MaxValue,
            "EciOptimizationStates");
        int[] bestBits = new int[stateBoundaryCount];
        int[] bestPreferenceScores = new int[stateBoundaryCount];
        int[] bestSegmentCounts = new int[stateBoundaryCount];
        int[] previousPositions = new int[stateBoundaryCount];
        int[] previousStates = new int[stateBoundaryCount];
        int[] selectedModes = new int[stateBoundaryCount];
        int[] bytePrefixCounts = new int[stateBoundaryCount];
        for (int offset = 0; offset < stateBoundaryCount; offset++)
        {
            bestBits[offset] = int.MaxValue;
            bestPreferenceScores[offset] = int.MaxValue;
            bestSegmentCounts[offset] = int.MaxValue;
            previousPositions[offset] = -1;
            previousStates[offset] = -1;
            selectedModes[offset] = -1;
        }

        int initialOffset = GetStateOffset(initialState, 0, boundaryCount);
        bestBits[initialOffset] = 0;
        bestPreferenceScores[initialOffset] = 0;
        bestSegmentCounts[initialOffset] = 0;

        PriorityQueue<int, decimal>[][] numericQueues = CreateGroupedStateQueues(stateCount, 3);
        PriorityQueue<int, decimal>[][] alphanumericQueues = CreateGroupedStateQueues(stateCount, 2);
        PriorityQueue<int, decimal>[] byteQueues = CreateStateQueues(stateCount);
        PriorityQueue<int, decimal>[] kanjiQueues = CreateStateQueues(stateCount);
        int preferenceFactor = CheckedDimensions.CalculateSum(
            CheckedDimensions.CalculateProduct(
                boundaryCount,
                stateCount,
                int.MaxValue - 1,
                "EciPreferenceFactor"),
            1,
            int.MaxValue,
            "EciPreferenceFactor");
        int segmentFactor = CheckedDimensions.CalculateSum(
            CheckedDimensions.CalculateProduct(
                boundaryCount,
                2,
                int.MaxValue - 1,
                "EciSegmentFactor"),
            1,
            int.MaxValue,
            "EciSegmentFactor");

        int index = 0;
        while (index < text.Length)
        {
            int scalarLength = GetScalarLength(text, index);
            int end = index + scalarLength;
            bool numeric = scalarLength == 1 && IsNumericCharacter(text[index]);
            bool alphanumeric = scalarLength == 1
                && QrCharacterSets.Alphanumeric.IndexOf(text[index]) >= 0;
            bool kanji = scalarLength == 1 && KanjiEncoder.IsEncodableCharacter(text[index]);

            for (int state = 0; state < stateCount; state++)
            {
                int prefixOffset = GetStateOffset(state, index, boundaryCount);
                int endOffset = GetStateOffset(state, end, boundaryCount);
                int byteWidth = GetEncodedScalarByteWidth(
                    text,
                    index,
                    scalarLength,
                    assignments[state]);
                bytePrefixCounts[endOffset] = bytePrefixCounts[prefixOffset] + byteWidth;
                if (scalarLength == 2)
                {
                    int middleOffset = GetStateOffset(state, index + 1, boundaryCount);
                    bytePrefixCounts[middleOffset] = bytePrefixCounts[prefixOffset];
                }

                UpdateNumericState(
                    state,
                    index,
                    end,
                    numeric,
                    modeIndicatorWidth,
                    countWidths[0],
                    maximumCounts[0],
                    boundaryCount,
                    preferenceFactor,
                    segmentFactor,
                    numericQueues[state],
                    bestBits,
                    bestPreferenceScores,
                    bestSegmentCounts,
                    previousPositions,
                    previousStates,
                    selectedModes);
                UpdateAlphanumericState(
                    state,
                    index,
                    end,
                    alphanumeric,
                    modeIndicatorWidth,
                    countWidths[1],
                    maximumCounts[1],
                    boundaryCount,
                    preferenceFactor,
                    segmentFactor,
                    alphanumericQueues[state],
                    bestBits,
                    bestPreferenceScores,
                    bestSegmentCounts,
                    previousPositions,
                    previousStates,
                    selectedModes);
                UpdateKanjiState(
                    state,
                    index,
                    end,
                    kanji,
                    modeIndicatorWidth,
                    countWidths[3],
                    maximumCounts[3],
                    boundaryCount,
                    preferenceFactor,
                    segmentFactor,
                    kanjiQueues[state],
                    bestBits,
                    bestPreferenceScores,
                    bestSegmentCounts,
                    previousPositions,
                    previousStates,
                    selectedModes);

                if (byteWidth == 0 || countWidths[2] == 0)
                {
                    byteQueues[state].Clear();
                }
                else
                {
                    AddByteCandidate(
                        state,
                        index,
                        assignments,
                        supportsEci,
                        modeIndicatorWidth,
                        countWidths[2],
                        boundaryCount,
                        preferenceFactor,
                        segmentFactor,
                        bytePrefixCounts,
                        bestBits,
                        bestPreferenceScores,
                        bestSegmentCounts,
                        byteQueues[state]);
                    RemoveExpiredByteCandidates(
                        state,
                        end,
                        maximumCounts[2],
                        stateCount,
                        boundaryCount,
                        bytePrefixCounts,
                        byteQueues[state]);
                    SelectByteCandidate(
                        state,
                        end,
                        modeIndicatorWidth,
                        modeIndicatorWidth + countWidths[2],
                        boundaryCount,
                        assignments,
                        bytePrefixCounts,
                        bestBits,
                        bestPreferenceScores,
                        bestSegmentCounts,
                        previousPositions,
                        previousStates,
                        selectedModes,
                        byteQueues[state]);
                }
            }

            index = end;
        }

        int finalState = SelectFinalState(
            text.Length,
            stateCount,
            boundaryCount,
            bestBits,
            bestPreferenceScores,
            bestSegmentCounts);
        if (finalState < 0)
        {
            throw new QrInputException(
                "The text cannot be represented by the selected QR profile and ECI encodings.",
                "text");
        }

        int finalOffset = GetStateOffset(finalState, text.Length, boundaryCount);
        if (bestSegmentCounts[finalOffset] > maximumSegments)
        {
            throw new QrResourceLimitException(
                "The optimized segment count exceeds the configured limit.",
                "MaximumSegments",
                bestSegmentCounts[finalOffset],
                maximumSegments);
        }

        QrSegment[] segments = BuildSegments(
            text,
            text.Length,
            finalState,
            boundaryCount,
            assignments,
            bestSegmentCounts[finalOffset],
            previousPositions,
            previousStates,
            selectedModes);
        SegmentValidator.ValidateSegments(
            segments,
            symbolKind,
            version,
            maximumSegments,
            maximumInputBytes,
            false);
        return segments;
    }

    static int[] NormalizeAssignments(int[] priorities)
    {
        int capacity = CheckedDimensions.CalculateSum(
            priorities.Length,
            BuiltInAssignments.Length,
            int.MaxValue,
            "EciAssignmentPriorities");
        int[] normalized = new int[capacity];
        int count = 0;
        for (int index = 0; index < priorities.Length; index++)
        {
            int assignment = priorities[index];
            if (assignment == -1)
            {
                assignment = QrEciAssignments.Iso8859_1;
            }

            EciEncoder.ValidateAssignment(assignment);
            if (!EciTextTranscoder.IsSupportedAssignment(assignment))
            {
                throw new QrUnsupportedFeatureException(
                    "An ECI priority has no built-in text transcoder.",
                    QrFailureReason.UnsupportedTextEncoding,
                    "ECI",
                    assignment);
            }

            if (FindAssignmentIndexWithin(normalized, count, assignment) < 0)
            {
                normalized[count] = assignment;
                count++;
            }
        }

        for (int index = 0; index < BuiltInAssignments.Length; index++)
        {
            int assignment = BuiltInAssignments[index];
            if (FindAssignmentIndexWithin(normalized, count, assignment) < 0)
            {
                normalized[count] = assignment;
                count++;
            }
        }

        int[] assignments = new int[count];
        for (int index = 0; index < count; index++)
        {
            assignments[index] = normalized[index];
        }

        return assignments;
    }

    static int FindAssignmentIndexWithin(int[] assignments, int count, int assignment)
    {
        for (int index = 0; index < count; index++)
        {
            if (assignments[index] == assignment)
            {
                return index;
            }
        }

        return -1;
    }

    static int FindAssignmentIndex(int[] assignments, int assignment)
    {
        return FindAssignmentIndexWithin(assignments, assignments.Length, assignment);
    }

    static PriorityQueue<int, decimal>[][] CreateGroupedStateQueues(
        int stateCount,
        int groupCount)
    {
        PriorityQueue<int, decimal>[][] queues = new PriorityQueue<int, decimal>[stateCount][];
        for (int state = 0; state < stateCount; state++)
        {
            queues[state] = new PriorityQueue<int, decimal>[groupCount];
            for (int group = 0; group < groupCount; group++)
            {
                queues[state][group] = new PriorityQueue<int, decimal>();
            }
        }

        return queues;
    }

    static PriorityQueue<int, decimal>[] CreateStateQueues(int stateCount)
    {
        PriorityQueue<int, decimal>[] queues = new PriorityQueue<int, decimal>[stateCount];
        for (int state = 0; state < stateCount; state++)
        {
            queues[state] = new PriorityQueue<int, decimal>();
        }

        return queues;
    }

    static void UpdateNumericState(
        int state,
        int start,
        int end,
        bool eligible,
        int modeIndicatorWidth,
        int countWidth,
        int maximumCount,
        int boundaryCount,
        int preferenceFactor,
        int segmentFactor,
        PriorityQueue<int, decimal>[] queues,
        int[] bestBits,
        int[] bestPreferenceScores,
        int[] bestSegmentCounts,
        int[] previousPositions,
        int[] previousStates,
        int[] selectedModes)
    {
        if (!eligible)
        {
            ClearGroupedQueues(queues);
            return;
        }

        if (countWidth == 0)
        {
            return;
        }

        int startOffset = GetStateOffset(state, start, boundaryCount);
        if (bestBits[startOffset] != int.MaxValue)
        {
            int residue = start % 3;
            int completeGroups = (start - residue) / 3;
            int key = bestBits[startOffset]
                + modeIndicatorWidth
                + countWidth
                - completeGroups * 10;
            decimal priority = CreatePriority(
                key,
                bestPreferenceScores[startOffset],
                bestSegmentCounts[startOffset] + 1,
                preferenceFactor,
                segmentFactor);
            queues[residue].Enqueue(start, priority);
        }

        RemoveExpiredGroupedCandidates(queues, end - maximumCount);
        for (int queueIndex = 0; queueIndex < queues.Length; queueIndex++)
        {
            if (queues[queueIndex].Count > 0)
            {
                int candidateStart = queues[queueIndex].Peek();
                int candidateOffset = GetStateOffset(state, candidateStart, boundaryCount);
                int bits = bestBits[candidateOffset]
                    + modeIndicatorWidth
                    + countWidth
                    + NumericEncoder.CalculateBitLength(end - candidateStart);
                RecordStateCandidate(
                    state,
                    end,
                    candidateStart,
                    state,
                    0,
                    bits,
                    bestPreferenceScores[candidateOffset],
                    bestSegmentCounts[candidateOffset] + 1,
                    boundaryCount,
                    bestBits,
                    bestPreferenceScores,
                    bestSegmentCounts,
                    previousPositions,
                    previousStates,
                    selectedModes);
            }
        }
    }

    static void UpdateAlphanumericState(
        int state,
        int start,
        int end,
        bool eligible,
        int modeIndicatorWidth,
        int countWidth,
        int maximumCount,
        int boundaryCount,
        int preferenceFactor,
        int segmentFactor,
        PriorityQueue<int, decimal>[] queues,
        int[] bestBits,
        int[] bestPreferenceScores,
        int[] bestSegmentCounts,
        int[] previousPositions,
        int[] previousStates,
        int[] selectedModes)
    {
        if (!eligible)
        {
            ClearGroupedQueues(queues);
            return;
        }

        if (countWidth == 0)
        {
            return;
        }

        int startOffset = GetStateOffset(state, start, boundaryCount);
        if (bestBits[startOffset] != int.MaxValue)
        {
            int residue = start % 2;
            int completeGroups = (start - residue) / 2;
            int key = bestBits[startOffset]
                + modeIndicatorWidth
                + countWidth
                - completeGroups * 11;
            decimal priority = CreatePriority(
                key,
                bestPreferenceScores[startOffset],
                bestSegmentCounts[startOffset] + 1,
                preferenceFactor,
                segmentFactor);
            queues[residue].Enqueue(start, priority);
        }

        RemoveExpiredGroupedCandidates(queues, end - maximumCount);
        for (int queueIndex = 0; queueIndex < queues.Length; queueIndex++)
        {
            if (queues[queueIndex].Count > 0)
            {
                int candidateStart = queues[queueIndex].Peek();
                int candidateOffset = GetStateOffset(state, candidateStart, boundaryCount);
                int bits = bestBits[candidateOffset]
                    + modeIndicatorWidth
                    + countWidth
                    + AlphanumericEncoder.CalculateBitLength(end - candidateStart);
                RecordStateCandidate(
                    state,
                    end,
                    candidateStart,
                    state,
                    1,
                    bits,
                    bestPreferenceScores[candidateOffset],
                    bestSegmentCounts[candidateOffset] + 1,
                    boundaryCount,
                    bestBits,
                    bestPreferenceScores,
                    bestSegmentCounts,
                    previousPositions,
                    previousStates,
                    selectedModes);
            }
        }
    }

    static void UpdateKanjiState(
        int state,
        int start,
        int end,
        bool eligible,
        int modeIndicatorWidth,
        int countWidth,
        int maximumCount,
        int boundaryCount,
        int preferenceFactor,
        int segmentFactor,
        PriorityQueue<int, decimal> queue,
        int[] bestBits,
        int[] bestPreferenceScores,
        int[] bestSegmentCounts,
        int[] previousPositions,
        int[] previousStates,
        int[] selectedModes)
    {
        if (!eligible)
        {
            queue.Clear();
            return;
        }

        if (countWidth == 0)
        {
            return;
        }

        int startOffset = GetStateOffset(state, start, boundaryCount);
        if (bestBits[startOffset] != int.MaxValue)
        {
            int key = bestBits[startOffset]
                + modeIndicatorWidth
                + countWidth
                - start * 13;
            decimal priority = CreatePriority(
                key,
                bestPreferenceScores[startOffset],
                bestSegmentCounts[startOffset] + 1,
                preferenceFactor,
                segmentFactor);
            queue.Enqueue(start, priority);
        }

        RemoveExpiredCandidates(queue, end - maximumCount);
        if (queue.Count > 0)
        {
            int candidateStart = queue.Peek();
            int candidateOffset = GetStateOffset(state, candidateStart, boundaryCount);
            int bits = bestBits[candidateOffset]
                + modeIndicatorWidth
                + countWidth
                + (end - candidateStart) * 13;
            RecordStateCandidate(
                state,
                end,
                candidateStart,
                state,
                3,
                bits,
                bestPreferenceScores[candidateOffset],
                bestSegmentCounts[candidateOffset] + 1,
                boundaryCount,
                bestBits,
                bestPreferenceScores,
                bestSegmentCounts,
                previousPositions,
                previousStates,
                selectedModes);
        }
    }

    static void AddByteCandidate(
        int targetState,
        int start,
        int[] assignments,
        bool supportsEci,
        int modeIndicatorWidth,
        int countWidth,
        int boundaryCount,
        int preferenceFactor,
        int segmentFactor,
        int[] bytePrefixCounts,
        int[] bestBits,
        int[] bestPreferenceScores,
        int[] bestSegmentCounts,
        PriorityQueue<int, decimal> queue)
    {
        int selectedPreviousState = -1;
        int selectedKey = int.MaxValue;
        int selectedPreference = int.MaxValue;
        int selectedSegments = int.MaxValue;
        int targetPrefixOffset = GetStateOffset(targetState, start, boundaryCount);
        for (int previousState = 0; previousState < assignments.Length; previousState++)
        {
            int previousOffset = GetStateOffset(previousState, start, boundaryCount);
            if (bestBits[previousOffset] == int.MaxValue)
            {
                continue;
            }

            bool switches = previousState != targetState;
            if (switches && !supportsEci)
            {
                continue;
            }

            int switchBits = 0;
            int addedSegments = 1;
            if (switches)
            {
                switchBits = modeIndicatorWidth
                    + EciEncoder.CalculateBitLength(assignments[targetState]);
                addedSegments++;
            }

            int key = bestBits[previousOffset]
                + switchBits
                + modeIndicatorWidth
                + countWidth
                - bytePrefixCounts[targetPrefixOffset] * 8;
            int preference = bestPreferenceScores[previousOffset] + targetState;
            int segments = bestSegmentCounts[previousOffset] + addedSegments;
            if (IsBetterCandidate(
                key,
                preference,
                segments,
                selectedKey,
                selectedPreference,
                selectedSegments))
            {
                selectedPreviousState = previousState;
                selectedKey = key;
                selectedPreference = preference;
                selectedSegments = segments;
            }
        }

        if (selectedPreviousState >= 0)
        {
            int element = start * assignments.Length + selectedPreviousState;
            decimal priority = CreatePriority(
                selectedKey,
                selectedPreference,
                selectedSegments,
                preferenceFactor,
                segmentFactor);
            queue.Enqueue(element, priority);
        }
    }

    static void RemoveExpiredByteCandidates(
        int targetState,
        int end,
        int maximumByteCount,
        int stateCount,
        int boundaryCount,
        int[] bytePrefixCounts,
        PriorityQueue<int, decimal> queue)
    {
        int endOffset = GetStateOffset(targetState, end, boundaryCount);
        while (queue.Count > 0)
        {
            int start = queue.Peek() / stateCount;
            int startOffset = GetStateOffset(targetState, start, boundaryCount);
            if (bytePrefixCounts[endOffset] - bytePrefixCounts[startOffset] <= maximumByteCount)
            {
                return;
            }

            queue.Dequeue();
        }
    }

    static void SelectByteCandidate(
        int targetState,
        int end,
        int modeIndicatorWidth,
        int headerBitCount,
        int boundaryCount,
        int[] assignments,
        int[] bytePrefixCounts,
        int[] bestBits,
        int[] bestPreferenceScores,
        int[] bestSegmentCounts,
        int[] previousPositions,
        int[] previousStates,
        int[] selectedModes,
        PriorityQueue<int, decimal> queue)
    {
        if (queue.Count == 0)
        {
            return;
        }

        int element = queue.Peek();
        int start = element / assignments.Length;
        int previousState = element % assignments.Length;
        int previousOffset = GetStateOffset(previousState, start, boundaryCount);
        int startPrefixOffset = GetStateOffset(targetState, start, boundaryCount);
        int endPrefixOffset = GetStateOffset(targetState, end, boundaryCount);
        bool switches = previousState != targetState;
        int switchBits = 0;
        int addedSegments = 1;
        if (switches)
        {
            switchBits = modeIndicatorWidth
                + EciEncoder.CalculateBitLength(assignments[targetState]);
            addedSegments++;
        }

        int byteCount = bytePrefixCounts[endPrefixOffset] - bytePrefixCounts[startPrefixOffset];
        int bits = bestBits[previousOffset]
            + switchBits
            + headerBitCount
            + byteCount * 8;
        RecordStateCandidate(
            targetState,
            end,
            start,
            previousState,
            2,
            bits,
            bestPreferenceScores[previousOffset] + targetState,
            bestSegmentCounts[previousOffset] + addedSegments,
            boundaryCount,
            bestBits,
            bestPreferenceScores,
            bestSegmentCounts,
            previousPositions,
            previousStates,
            selectedModes);
    }

    static void ClearGroupedQueues(PriorityQueue<int, decimal>[] queues)
    {
        for (int index = 0; index < queues.Length; index++)
        {
            queues[index].Clear();
        }
    }

    static void RemoveExpiredGroupedCandidates(
        PriorityQueue<int, decimal>[] queues,
        int minimumStart)
    {
        for (int index = 0; index < queues.Length; index++)
        {
            RemoveExpiredCandidates(queues[index], minimumStart);
        }
    }

    static void RemoveExpiredCandidates(
        PriorityQueue<int, decimal> queue,
        int minimumStart)
    {
        while (queue.Count > 0 && queue.Peek() < minimumStart)
        {
            queue.Dequeue();
        }
    }

    static decimal CreatePriority(
        int key,
        int preferenceScore,
        int segmentCount,
        int preferenceFactor,
        int segmentFactor)
    {
        return ((decimal)key * preferenceFactor + preferenceScore)
            * segmentFactor
            + segmentCount;
    }

    static bool IsBetterCandidate(
        int bits,
        int preferenceScore,
        int segmentCount,
        int existingBits,
        int existingPreferenceScore,
        int existingSegmentCount)
    {
        return bits < existingBits
            || bits == existingBits && preferenceScore < existingPreferenceScore
            || bits == existingBits
                && preferenceScore == existingPreferenceScore
                && segmentCount < existingSegmentCount;
    }

    static void RecordStateCandidate(
        int state,
        int end,
        int start,
        int previousState,
        int modeIndex,
        int bits,
        int preferenceScore,
        int segmentCount,
        int boundaryCount,
        int[] bestBits,
        int[] bestPreferenceScores,
        int[] bestSegmentCounts,
        int[] previousPositions,
        int[] previousStates,
        int[] selectedModes)
    {
        int offset = GetStateOffset(state, end, boundaryCount);
        bool better = IsBetterCandidate(
            bits,
            preferenceScore,
            segmentCount,
            bestBits[offset],
            bestPreferenceScores[offset],
            bestSegmentCounts[offset]);
        if (!better
            && bits == bestBits[offset]
            && preferenceScore == bestPreferenceScores[offset]
            && segmentCount == bestSegmentCounts[offset])
        {
            better = selectedModes[offset] < 0
                || modeIndex < selectedModes[offset]
                || modeIndex == selectedModes[offset]
                    && previousState < previousStates[offset]
                || modeIndex == selectedModes[offset]
                    && previousState == previousStates[offset]
                    && start < previousPositions[offset];
        }

        if (better)
        {
            bestBits[offset] = bits;
            bestPreferenceScores[offset] = preferenceScore;
            bestSegmentCounts[offset] = segmentCount;
            previousPositions[offset] = start;
            previousStates[offset] = previousState;
            selectedModes[offset] = modeIndex;
        }
    }

    static int SelectFinalState(
        int end,
        int stateCount,
        int boundaryCount,
        int[] bestBits,
        int[] bestPreferenceScores,
        int[] bestSegmentCounts)
    {
        int selectedState = -1;
        int selectedBits = int.MaxValue;
        int selectedPreference = int.MaxValue;
        int selectedSegments = int.MaxValue;
        for (int state = 0; state < stateCount; state++)
        {
            int offset = GetStateOffset(state, end, boundaryCount);
            if (IsBetterCandidate(
                bestBits[offset],
                bestPreferenceScores[offset],
                bestSegmentCounts[offset],
                selectedBits,
                selectedPreference,
                selectedSegments))
            {
                selectedState = state;
                selectedBits = bestBits[offset];
                selectedPreference = bestPreferenceScores[offset];
                selectedSegments = bestSegmentCounts[offset];
            }
        }

        return selectedState;
    }

    static int GetStateOffset(int state, int position, int boundaryCount)
    {
        return state * boundaryCount + position;
    }

    static QrSegment[] BuildSegments(
        string text,
        int end,
        int finalState,
        int boundaryCount,
        int[] assignments,
        int segmentCount,
        int[] previousPositions,
        int[] previousStates,
        int[] selectedModes)
    {
        QrSegment[] segments = new QrSegment[segmentCount];
        int resultIndex = segmentCount - 1;
        int position = end;
        int state = finalState;
        while (position > 0)
        {
            int offset = GetStateOffset(state, position, boundaryCount);
            int start = previousPositions[offset];
            int previousState = previousStates[offset];
            int modeIndex = selectedModes[offset];
            string segmentText = text.Substring(start, position - start);
            segments[resultIndex] = CreateDataSegment(
                segmentText,
                modeIndex,
                assignments[state]);
            resultIndex--;
            if (modeIndex == 2 && previousState != state)
            {
                segments[resultIndex] = EciEncoder.CreateControlSegment(assignments[state]);
                resultIndex--;
            }

            position = start;
            state = previousState;
        }

        return segments;
    }

    static QrSegment CreateDataSegment(string text, int modeIndex, int assignment)
    {
        if (modeIndex == 0)
        {
            return CreateTextSegment(
                text,
                QrEncodingMode.Numeric,
                EncodeAsciiText(text),
                -1);
        }

        if (modeIndex == 1)
        {
            return CreateTextSegment(
                text,
                QrEncodingMode.Alphanumeric,
                EncodeAsciiText(text),
                -1);
        }

        if (modeIndex == 2)
        {
            return CreateTextSegment(
                text,
                QrEncodingMode.Byte,
                EciTextTranscoder.EncodeText(text, assignment),
                assignment);
        }

        return CreateTextSegment(
            text,
            QrEncodingMode.Kanji,
            EciTextTranscoder.EncodeText(text, QrEciAssignments.ShiftJis),
            -1);
    }

    static QrSegment CreateTextSegment(
        string text,
        QrEncodingMode mode,
        byte[] data,
        int assignment)
    {
        int characterCount = text.Length;
        if (mode == QrEncodingMode.Byte)
        {
            characterCount = data.Length;
        }

        return new QrSegment
        {
            Mode = mode,
            CharacterCount = characterCount,
            Data = data,
            Text = text,
            EciAssignment = assignment
        };
    }

    static byte[] EncodeAsciiText(string text)
    {
        CheckedDimensions.CalculateAllocationBytes(
            text.Length,
            1,
            QrLimits.DefaultMaximumAllocationBytes);
        byte[] data = new byte[text.Length];
        for (int index = 0; index < text.Length; index++)
        {
            data[index] = (byte)text[index];
        }

        return data;
    }

    static int GetModeIndicatorWidth(QrSymbolKind symbolKind, int version)
    {
        if (symbolKind == QrSymbolKind.Micro)
        {
            return MicroVersionTable.GetModeIndicatorBitWidth(version);
        }

        if (symbolKind == QrSymbolKind.rMQR)
        {
            return RectangularMicroVersionTable.ModeIndicatorBitWidth;
        }

        return 4;
    }

    static int GetCharacterCountWidth(
        QrSymbolKind symbolKind,
        int version,
        int modeIndex)
    {
        if (symbolKind == QrSymbolKind.Model2)
        {
            return Model2VersionTable.GetCharacterCountBitWidth(modeIndex, version);
        }

        if (symbolKind == QrSymbolKind.Micro)
        {
            if (!MicroVersionTable.IsModeSupported(version, modeIndex))
            {
                return 0;
            }

            return MicroVersionTable.GetCharacterCountBitWidth(version, modeIndex);
        }

        if (symbolKind == QrSymbolKind.rMQR)
        {
            return RectangularMicroVersionTable.GetCharacterCountBitWidth(version, modeIndex);
        }

        return LegacyModel1Table.GetCharacterCountBitWidth(version, modeIndex);
    }

    static int GetScalarLength(string text, int index)
    {
        char character = text[index];
        if (character >= 0xD800 && character <= 0xDBFF)
        {
            if (index + 1 >= text.Length
                || text[index + 1] < 0xDC00
                || text[index + 1] > 0xDFFF)
            {
                throw new QrInputException("The text contains an unmatched high surrogate.", "text");
            }

            return 2;
        }

        if (character >= 0xDC00 && character <= 0xDFFF)
        {
            throw new QrInputException("The text contains an unmatched low surrogate.", "text");
        }

        return 1;
    }

    static bool IsNumericCharacter(char character)
    {
        return character >= '0' && character <= '9';
    }

    static int GetEncodedScalarByteWidth(
        string text,
        int index,
        int scalarLength,
        int assignment)
    {
        if (assignment == 1 || assignment == QrEciAssignments.Iso8859_1)
        {
            if (scalarLength == 1 && text[index] <= 0xFF)
            {
                return 1;
            }

            return 0;
        }

        if (assignment == QrEciAssignments.UsAscii)
        {
            if (scalarLength == 1 && text[index] <= 0x7F)
            {
                return 1;
            }

            return 0;
        }

        if (assignment == QrEciAssignments.ShiftJis)
        {
            return GetShiftJisScalarByteWidth(text[index], scalarLength);
        }

        if (assignment == QrEciAssignments.Gb2312)
        {
            return GetGb2312ScalarByteWidth(text[index], scalarLength);
        }

        if (assignment == QrEciAssignments.Utf8)
        {
            if (scalarLength == 2)
            {
                return 4;
            }

            if (text[index] <= 0x7F)
            {
                return 1;
            }

            if (text[index] <= 0x7FF)
            {
                return 2;
            }

            return 3;
        }

        if (assignment == QrEciAssignments.Utf16BigEndian
            || assignment == QrEciAssignments.Utf16LittleEndian)
        {
            return scalarLength * 2;
        }

        if (assignment == QrEciAssignments.Utf32BigEndian
            || assignment == QrEciAssignments.Utf32LittleEndian)
        {
            return 4;
        }

        return 0;
    }

    static int GetShiftJisScalarByteWidth(char character, int scalarLength)
    {
        if (scalarLength != 1)
        {
            return 0;
        }

        if (character <= 0x1F || character == 0x7F)
        {
            return 1;
        }

        int encodedValue = ShiftJisTable.GetStandardEncodedValue(character);
        if (encodedValue == ShiftJisTable.NotMapped)
        {
            return 0;
        }

        if (encodedValue <= 0xFF)
        {
            return 1;
        }

        return 2;
    }

    static int GetGb2312ScalarByteWidth(char character, int scalarLength)
    {
        if (scalarLength != 1)
        {
            return 0;
        }

        if (character <= 0x7F)
        {
            return 1;
        }

        if (Gb2312Table.GetStandardEncodedValue(character) == Gb2312Table.NotMapped)
        {
            return 0;
        }

        return 2;
    }
}
