using System.Collections.Generic;

namespace qrcode;

internal class SegmentOptimizer
{
    internal static QrSegment[] CreateSingleModeTextSegments(
        string text,
        int byteEciAssignment,
        bool enableHanziCompatibility,
        int maximumInputBytes)
    {
        if (text == null)
        {
            throw new QrInputException("The text to segment cannot be null.", "text");
        }

        ValidateMaximumInputBytes(maximumInputBytes);
        ValidateOptionalEciAssignment(byteEciAssignment);
        ValidateTextCharacters(text);
        if (text.Length == 0)
        {
            return new QrSegment[0];
        }

        QrSegment segment;
        if (IsNumericText(text))
        {
            segment = CreateTextDataSegment(
                text,
                QrEncodingMode.Numeric,
                EncodeAsciiText(text),
                -1);
        }
        else if (IsAlphanumericText(text))
        {
            segment = CreateTextDataSegment(
                text,
                QrEncodingMode.Alphanumeric,
                EncodeAsciiText(text),
                -1);
        }
        else if (IsKanjiText(text))
        {
            segment = CreateTextDataSegment(
                text,
                QrEncodingMode.Kanji,
                EciTextTranscoder.EncodeText(text, QrEciAssignments.ShiftJis),
                -1);
        }
        else if (enableHanziCompatibility && IsHanziText(text))
        {
            segment = HanziEncoder.CreateCompatibilitySegment(text, true);
        }
        else
        {
            segment = CreateByteTextSegment(text, byteEciAssignment);
        }

        ValidateInputByteCount(segment.Data.Length, maximumInputBytes);
        return new QrSegment[] { segment };
    }

    internal static QrSegment CreateSingleModeByteSegment(
        byte[] bytes,
        int eciAssignment,
        int maximumInputBytes)
    {
        if (bytes == null)
        {
            throw new QrInputException("The binary input cannot be null.", "bytes");
        }

        ValidateMaximumInputBytes(maximumInputBytes);
        ValidateInputByteCount(bytes.Length, maximumInputBytes);
        ValidateOptionalEciAssignment(eciAssignment);
        CheckedDimensions.CalculateAllocationBytes(
            bytes.Length,
            1,
            QrLimits.DefaultMaximumAllocationBytes);
        byte[] copy = new byte[bytes.Length];
        for (int index = 0; index < bytes.Length; index++)
        {
            copy[index] = bytes[index];
        }

        return new QrSegment
        {
            Mode = QrEncodingMode.Byte,
            CharacterCount = copy.Length,
            Data = copy,
            EciAssignment = eciAssignment
        };
    }

    internal static QrSegment[] OptimizeTextSegments(
        string text,
        QrSymbolKind symbolKind,
        int version,
        int byteEciAssignment,
        int maximumSegments,
        int maximumInputBytes)
    {
        if (text == null)
        {
            throw new QrInputException("The text to optimize cannot be null.", "text");
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

        if (text.Length == 0)
        {
            return new QrSegment[0];
        }

        int effectiveByteAssignment = GetEffectiveByteAssignment(text, byteEciAssignment);
        int segmentByteAssignment = GetSegmentByteAssignment(text, byteEciAssignment);
        bool byteEncodingSupported = IsByteEncodingSupported(effectiveByteAssignment);
        int modeIndicatorWidth = GetModeIndicatorWidth(symbolKind, version);
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
            "SegmentOptimizationBoundaries");
        int[] bestBits = new int[boundaryCount];
        int[] bestSegmentCounts = new int[boundaryCount];
        int[] previousPositions = new int[boundaryCount];
        int[] selectedModes = new int[boundaryCount];
        int[] bytePrefixCounts = new int[boundaryCount];
        for (int position = 1; position < boundaryCount; position++)
        {
            bestBits[position] = int.MaxValue;
            bestSegmentCounts[position] = int.MaxValue;
            previousPositions[position] = -1;
            selectedModes[position] = -1;
        }

        PriorityQueue<int, long>[] numericQueues = CreateCandidateQueues(3);
        PriorityQueue<int, long>[] alphanumericQueues = CreateCandidateQueues(2);
        PriorityQueue<int, long> byteQueue = new PriorityQueue<int, long>();
        PriorityQueue<int, long> kanjiQueue = new PriorityQueue<int, long>();
        int[] candidateBits = new int[4];
        int[] candidateSegmentCounts = new int[4];
        int[] candidateStarts = new int[4];
        int priorityFactor = boundaryCount + 1;
        int index = 0;
        while (index < text.Length)
        {
            int scalarLength = GetScalarLength(text, index);
            int end = index + scalarLength;
            int byteWidth = 0;
            if (byteEncodingSupported)
            {
                byteWidth = GetEncodedScalarByteWidth(
                    text,
                    index,
                    scalarLength,
                    effectiveByteAssignment);
            }

            bytePrefixCounts[end] = bytePrefixCounts[index] + byteWidth;
            if (scalarLength == 2)
            {
                bytePrefixCounts[index + 1] = bytePrefixCounts[index];
            }

            for (int modeIndex = 0; modeIndex < 4; modeIndex++)
            {
                candidateBits[modeIndex] = int.MaxValue;
                candidateSegmentCounts[modeIndex] = int.MaxValue;
                candidateStarts[modeIndex] = -1;
            }

            bool numeric = scalarLength == 1 && IsNumericCharacter(text[index]);
            if (!numeric)
            {
                ClearCandidateQueues(numericQueues);
            }
            else if (countWidths[0] > 0)
            {
                AddGroupedCandidate(
                    numericQueues,
                    index,
                    3,
                    10,
                    modeIndicatorWidth + countWidths[0],
                    bestBits,
                    bestSegmentCounts,
                    priorityFactor);
                RemoveExpiredCharacterCandidates(
                    numericQueues,
                    end - maximumCounts[0]);
                SelectNumericCandidate(
                    numericQueues,
                    end,
                    modeIndicatorWidth + countWidths[0],
                    bestBits,
                    bestSegmentCounts,
                    candidateBits,
                    candidateSegmentCounts,
                    candidateStarts);
            }

            bool alphanumeric = scalarLength == 1
                && QrCharacterSets.Alphanumeric.IndexOf(text[index]) >= 0;
            if (!alphanumeric)
            {
                ClearCandidateQueues(alphanumericQueues);
            }
            else if (countWidths[1] > 0)
            {
                AddGroupedCandidate(
                    alphanumericQueues,
                    index,
                    2,
                    11,
                    modeIndicatorWidth + countWidths[1],
                    bestBits,
                    bestSegmentCounts,
                    priorityFactor);
                RemoveExpiredCharacterCandidates(
                    alphanumericQueues,
                    end - maximumCounts[1]);
                SelectAlphanumericCandidate(
                    alphanumericQueues,
                    end,
                    modeIndicatorWidth + countWidths[1],
                    bestBits,
                    bestSegmentCounts,
                    candidateBits,
                    candidateSegmentCounts,
                    candidateStarts);
            }

            if (byteWidth == 0 || countWidths[2] == 0)
            {
                byteQueue.Clear();
            }
            else
            {
                AddLinearCandidate(
                    byteQueue,
                    index,
                    bytePrefixCounts[index],
                    8,
                    modeIndicatorWidth + countWidths[2],
                    bestBits,
                    bestSegmentCounts,
                    priorityFactor);
                RemoveExpiredByteCandidates(
                    byteQueue,
                    bytePrefixCounts,
                    end,
                    maximumCounts[2]);
                SelectByteCandidate(
                    byteQueue,
                    end,
                    bytePrefixCounts,
                    modeIndicatorWidth + countWidths[2],
                    bestBits,
                    bestSegmentCounts,
                    candidateBits,
                    candidateSegmentCounts,
                    candidateStarts);
            }

            bool kanji = scalarLength == 1 && KanjiEncoder.IsEncodableCharacter(text[index]);
            if (!kanji)
            {
                kanjiQueue.Clear();
            }
            else if (countWidths[3] > 0)
            {
                AddLinearCandidate(
                    kanjiQueue,
                    index,
                    index,
                    13,
                    modeIndicatorWidth + countWidths[3],
                    bestBits,
                    bestSegmentCounts,
                    priorityFactor);
                RemoveExpiredLinearCandidates(
                    kanjiQueue,
                    end - maximumCounts[3]);
                SelectKanjiCandidate(
                    kanjiQueue,
                    end,
                    modeIndicatorWidth + countWidths[3],
                    bestBits,
                    bestSegmentCounts,
                    candidateBits,
                    candidateSegmentCounts,
                    candidateStarts);
            }

            SelectBestModeCandidate(
                end,
                candidateBits,
                candidateSegmentCounts,
                candidateStarts,
                bestBits,
                bestSegmentCounts,
                previousPositions,
                selectedModes);
            index = end;
        }

        if (bestBits[text.Length] == int.MaxValue)
        {
            if (!byteEncodingSupported)
            {
                throw new QrUnsupportedFeatureException(
                    "The requested ECI assignment has no built-in text transcoder.",
                    QrFailureReason.UnsupportedTextEncoding,
                    "ECI",
                    effectiveByteAssignment);
            }

            throw new QrInputException(
                "The text cannot be represented by the selected QR profile and modes.",
                "text");
        }

        if (bestSegmentCounts[text.Length] > maximumSegments)
        {
            throw new QrResourceLimitException(
                "The optimized segment count exceeds the configured limit.",
                "MaximumSegments",
                bestSegmentCounts[text.Length],
                maximumSegments);
        }

        QrSegment[] segments = BuildOptimizedSegments(
            text,
            previousPositions,
            selectedModes,
            bestSegmentCounts[text.Length],
            effectiveByteAssignment,
            segmentByteAssignment);
        SegmentValidator.ValidateSegments(
            segments,
            symbolKind,
            version,
            maximumSegments,
            maximumInputBytes,
            false);
        return segments;
    }

    internal static QrSegment[] OptimizeTextSegmentsAcrossEci(
        string text,
        QrSymbolKind symbolKind,
        int version,
        int[] eciAssignmentPriorities,
        int maximumSegments,
        int maximumInputBytes)
    {
        return EciSegmentOptimizer.OptimizeText(
            text,
            symbolKind,
            version,
            eciAssignmentPriorities,
            maximumSegments,
            maximumInputBytes);
    }

    internal static QrSegment[] OptimizeTextSegmentsWithControls(
        string text,
        QrSymbolKind symbolKind,
        int version,
        int[] eciAssignmentPriorities,
        bool useFnc1First,
        int fnc1SecondApplicationIndicator,
        QrStructuredAppendInfo? structuredAppend,
        int maximumSegments,
        int maximumInputBytes)
    {
        return ControlSegmentOptimizer.OptimizeText(
            text,
            symbolKind,
            version,
            eciAssignmentPriorities,
            useFnc1First,
            fnc1SecondApplicationIndicator,
            structuredAppend,
            maximumSegments,
            maximumInputBytes);
    }

    internal static int CalculateOptimizedBitLength(
        QrSegment[] segments,
        QrSymbolKind symbolKind,
        int version,
        bool enableHanziCompatibility)
    {
        return ControlSegmentOptimizer.CalculateBitLength(
            segments,
            symbolKind,
            version,
            enableHanziCompatibility);
    }

    static PriorityQueue<int, long>[] CreateCandidateQueues(int count)
    {
        PriorityQueue<int, long>[] queues = new PriorityQueue<int, long>[count];
        for (int index = 0; index < count; index++)
        {
            queues[index] = new PriorityQueue<int, long>();
        }

        return queues;
    }

    static void ClearCandidateQueues(PriorityQueue<int, long>[] queues)
    {
        for (int index = 0; index < queues.Length; index++)
        {
            queues[index].Clear();
        }
    }

    static void AddGroupedCandidate(
        PriorityQueue<int, long>[] queues,
        int start,
        int groupSize,
        int groupBitCount,
        int headerBitCount,
        int[] bestBits,
        int[] bestSegmentCounts,
        int priorityFactor)
    {
        if (bestBits[start] == int.MaxValue)
        {
            return;
        }

        int residue = start % groupSize;
        int completeGroups = (start - residue) / groupSize;
        int key = bestBits[start] + headerBitCount - completeGroups * groupBitCount;
        long priority = CreateCandidatePriority(
            key,
            bestSegmentCounts[start] + 1,
            priorityFactor);
        queues[residue].Enqueue(start, priority);
    }

    static void AddLinearCandidate(
        PriorityQueue<int, long> queue,
        int start,
        int startCoordinate,
        int bitsPerUnit,
        int headerBitCount,
        int[] bestBits,
        int[] bestSegmentCounts,
        int priorityFactor)
    {
        if (bestBits[start] == int.MaxValue)
        {
            return;
        }

        int key = bestBits[start] + headerBitCount - startCoordinate * bitsPerUnit;
        long priority = CreateCandidatePriority(
            key,
            bestSegmentCounts[start] + 1,
            priorityFactor);
        queue.Enqueue(start, priority);
    }

    static long CreateCandidatePriority(int key, int segmentCount, int priorityFactor)
    {
        return (long)key * priorityFactor + segmentCount;
    }

    static void RemoveExpiredCharacterCandidates(
        PriorityQueue<int, long>[] queues,
        int minimumStart)
    {
        for (int index = 0; index < queues.Length; index++)
        {
            RemoveExpiredLinearCandidates(queues[index], minimumStart);
        }
    }

    static void RemoveExpiredLinearCandidates(
        PriorityQueue<int, long> queue,
        int minimumStart)
    {
        while (queue.Count > 0 && queue.Peek() < minimumStart)
        {
            queue.Dequeue();
        }
    }

    static void RemoveExpiredByteCandidates(
        PriorityQueue<int, long> queue,
        int[] bytePrefixCounts,
        int end,
        int maximumByteCount)
    {
        while (queue.Count > 0
            && bytePrefixCounts[end] - bytePrefixCounts[queue.Peek()] > maximumByteCount)
        {
            queue.Dequeue();
        }
    }

    static void SelectNumericCandidate(
        PriorityQueue<int, long>[] queues,
        int end,
        int headerBitCount,
        int[] bestBits,
        int[] bestSegmentCounts,
        int[] candidateBits,
        int[] candidateSegmentCounts,
        int[] candidateStarts)
    {
        for (int queueIndex = 0; queueIndex < queues.Length; queueIndex++)
        {
            if (queues[queueIndex].Count > 0)
            {
                int start = queues[queueIndex].Peek();
                int bits = bestBits[start]
                    + headerBitCount
                    + NumericEncoder.CalculateBitLength(end - start);
                int segmentCount = bestSegmentCounts[start] + 1;
                RecordModeCandidate(
                    bits,
                    segmentCount,
                    start,
                    0,
                    candidateBits,
                    candidateSegmentCounts,
                    candidateStarts);
            }
        }
    }

    static void SelectAlphanumericCandidate(
        PriorityQueue<int, long>[] queues,
        int end,
        int headerBitCount,
        int[] bestBits,
        int[] bestSegmentCounts,
        int[] candidateBits,
        int[] candidateSegmentCounts,
        int[] candidateStarts)
    {
        for (int queueIndex = 0; queueIndex < queues.Length; queueIndex++)
        {
            if (queues[queueIndex].Count > 0)
            {
                int start = queues[queueIndex].Peek();
                int bits = bestBits[start]
                    + headerBitCount
                    + AlphanumericEncoder.CalculateBitLength(end - start);
                int segmentCount = bestSegmentCounts[start] + 1;
                RecordModeCandidate(
                    bits,
                    segmentCount,
                    start,
                    1,
                    candidateBits,
                    candidateSegmentCounts,
                    candidateStarts);
            }
        }
    }

    static void SelectByteCandidate(
        PriorityQueue<int, long> queue,
        int end,
        int[] bytePrefixCounts,
        int headerBitCount,
        int[] bestBits,
        int[] bestSegmentCounts,
        int[] candidateBits,
        int[] candidateSegmentCounts,
        int[] candidateStarts)
    {
        if (queue.Count == 0)
        {
            return;
        }

        int start = queue.Peek();
        int byteCount = bytePrefixCounts[end] - bytePrefixCounts[start];
        int bits = bestBits[start] + headerBitCount + byteCount * 8;
        int segmentCount = bestSegmentCounts[start] + 1;
        RecordModeCandidate(
            bits,
            segmentCount,
            start,
            2,
            candidateBits,
            candidateSegmentCounts,
            candidateStarts);
    }

    static void SelectKanjiCandidate(
        PriorityQueue<int, long> queue,
        int end,
        int headerBitCount,
        int[] bestBits,
        int[] bestSegmentCounts,
        int[] candidateBits,
        int[] candidateSegmentCounts,
        int[] candidateStarts)
    {
        if (queue.Count == 0)
        {
            return;
        }

        int start = queue.Peek();
        int bits = bestBits[start] + headerBitCount + (end - start) * 13;
        int segmentCount = bestSegmentCounts[start] + 1;
        RecordModeCandidate(
            bits,
            segmentCount,
            start,
            3,
            candidateBits,
            candidateSegmentCounts,
            candidateStarts);
    }

    static void RecordModeCandidate(
        int bits,
        int segmentCount,
        int start,
        int modeIndex,
        int[] candidateBits,
        int[] candidateSegmentCounts,
        int[] candidateStarts)
    {
        if (bits < candidateBits[modeIndex]
            || bits == candidateBits[modeIndex]
                && segmentCount < candidateSegmentCounts[modeIndex]
            || bits == candidateBits[modeIndex]
                && segmentCount == candidateSegmentCounts[modeIndex]
                && (candidateStarts[modeIndex] < 0 || start < candidateStarts[modeIndex]))
        {
            candidateBits[modeIndex] = bits;
            candidateSegmentCounts[modeIndex] = segmentCount;
            candidateStarts[modeIndex] = start;
        }
    }

    static void SelectBestModeCandidate(
        int end,
        int[] candidateBits,
        int[] candidateSegmentCounts,
        int[] candidateStarts,
        int[] bestBits,
        int[] bestSegmentCounts,
        int[] previousPositions,
        int[] selectedModes)
    {
        for (int modeIndex = 0; modeIndex < 4; modeIndex++)
        {
            if (candidateBits[modeIndex] < bestBits[end]
                || candidateBits[modeIndex] == bestBits[end]
                    && candidateSegmentCounts[modeIndex] < bestSegmentCounts[end])
            {
                bestBits[end] = candidateBits[modeIndex];
                bestSegmentCounts[end] = candidateSegmentCounts[modeIndex];
                previousPositions[end] = candidateStarts[modeIndex];
                selectedModes[end] = modeIndex;
            }
        }
    }

    static int GetEffectiveByteAssignment(string text, int requestedAssignment)
    {
        if (requestedAssignment < -1)
        {
            throw new QrInputException("The ECI assignment sentinel is invalid.", "byteEciAssignment");
        }

        if (requestedAssignment >= 0)
        {
            EciEncoder.ValidateAssignment(requestedAssignment);
            return requestedAssignment;
        }

        if (CanUseDefaultInterpretation(text))
        {
            return QrEciAssignments.Iso8859_1;
        }

        return QrEciAssignments.Utf8;
    }

    static int GetSegmentByteAssignment(string text, int requestedAssignment)
    {
        if (requestedAssignment >= 0)
        {
            return requestedAssignment;
        }

        if (CanUseDefaultInterpretation(text))
        {
            return -1;
        }

        return QrEciAssignments.Utf8;
    }

    static bool IsByteEncodingSupported(int assignment)
    {
        return EciTextTranscoder.IsSupportedAssignment(assignment);
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

    internal static void ValidateTextCharacters(string text)
    {
        int index = 0;
        while (index < text.Length)
        {
            index += GetScalarLength(text, index);
        }
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

    static bool IsNumericCharacter(char character)
    {
        return character >= '0' && character <= '9';
    }

    static QrSegment[] BuildOptimizedSegments(
        string text,
        int[] previousPositions,
        int[] selectedModes,
        int segmentCount,
        int effectiveByteAssignment,
        int segmentByteAssignment)
    {
        QrSegment[] segments = new QrSegment[segmentCount];
        int position = text.Length;
        for (int segmentIndex = segmentCount - 1; segmentIndex >= 0; segmentIndex--)
        {
            int start = previousPositions[position];
            int modeIndex = selectedModes[position];
            string segmentText = text.Substring(start, position - start);
            segments[segmentIndex] = CreateOptimizedSegment(
                segmentText,
                modeIndex,
                effectiveByteAssignment,
                segmentByteAssignment);
            position = start;
        }

        return segments;
    }

    static QrSegment CreateOptimizedSegment(
        string text,
        int modeIndex,
        int effectiveByteAssignment,
        int segmentByteAssignment)
    {
        if (modeIndex == 0)
        {
            return CreateTextDataSegment(
                text,
                QrEncodingMode.Numeric,
                EncodeAsciiText(text),
                -1);
        }

        if (modeIndex == 1)
        {
            return CreateTextDataSegment(
                text,
                QrEncodingMode.Alphanumeric,
                EncodeAsciiText(text),
                -1);
        }

        if (modeIndex == 2)
        {
            return CreateTextDataSegment(
                text,
                QrEncodingMode.Byte,
                EciTextTranscoder.EncodeText(text, effectiveByteAssignment),
                segmentByteAssignment);
        }

        return CreateTextDataSegment(
            text,
            QrEncodingMode.Kanji,
            EciTextTranscoder.EncodeText(text, QrEciAssignments.ShiftJis),
            -1);
    }

    internal static bool RequiresSingleByteMode(string text, bool enableHanziCompatibility)
    {
        return !IsAlphanumericText(text) && !IsKanjiText(text)
            && !(enableHanziCompatibility && IsHanziText(text));
    }

    static bool IsNumericText(string text)
    {
        for (int index = 0; index < text.Length; index++)
        {
            char character = text[index];
            if (character < '0' || character > '9')
            {
                return false;
            }
        }

        return true;
    }

    static bool IsAlphanumericText(string text)
    {
        for (int index = 0; index < text.Length; index++)
        {
            if (QrCharacterSets.Alphanumeric.IndexOf(text[index]) < 0)
            {
                return false;
            }
        }

        return true;
    }

    static bool IsKanjiText(string text)
    {
        for (int index = 0; index < text.Length; index++)
        {
            if (!KanjiEncoder.IsEncodableCharacter(text[index]))
            {
                return false;
            }
        }

        return true;
    }

    static bool IsHanziText(string text)
    {
        for (int index = 0; index < text.Length; index++)
        {
            if (!HanziEncoder.IsEncodableCharacter(text[index]))
            {
                return false;
            }
        }

        return true;
    }

    static QrSegment CreateByteTextSegment(string text, int requestedAssignment)
    {
        int effectiveAssignment = requestedAssignment;
        int segmentAssignment = requestedAssignment;
        if (requestedAssignment == -1)
        {
            if (CanUseDefaultInterpretation(text))
            {
                effectiveAssignment = QrEciAssignments.Iso8859_1;
                segmentAssignment = -1;
            }
            else
            {
                effectiveAssignment = QrEciAssignments.Utf8;
                segmentAssignment = QrEciAssignments.Utf8;
            }
        }
        else if (requestedAssignment < -1)
        {
            throw new QrInputException("The ECI assignment sentinel is invalid.", "byteEciAssignment");
        }

        byte[] data = EciTextTranscoder.EncodeText(text, effectiveAssignment);
        return CreateTextDataSegment(text, QrEncodingMode.Byte, data, segmentAssignment);
    }

    static bool CanUseDefaultInterpretation(string text)
    {
        for (int index = 0; index < text.Length; index++)
        {
            if (text[index] > 0xFF)
            {
                return false;
            }
        }

        return true;
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

    static QrSegment CreateTextDataSegment(
        string text,
        QrEncodingMode mode,
        byte[] data,
        int eciAssignment)
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
            EciAssignment = eciAssignment
        };
    }

    static void ValidateOptionalEciAssignment(int assignment)
    {
        if (assignment < -1)
        {
            throw new QrInputException("The ECI assignment sentinel is invalid.", "eciAssignment");
        }

        if (assignment >= 0)
        {
            EciEncoder.ValidateAssignment(assignment);
        }
    }

    static void ValidateMaximumInputBytes(int maximumInputBytes)
    {
        if (maximumInputBytes < 0)
        {
            throw new QrInputException(
                "The maximum input byte count cannot be negative.",
                "maximumInputBytes");
        }
    }

    static void ValidateInputByteCount(int byteCount, int maximumInputBytes)
    {
        if (byteCount > maximumInputBytes)
        {
            throw new QrResourceLimitException(
                "The input exceeds the configured byte limit.",
                "MaximumInputBytes",
                byteCount,
                maximumInputBytes);
        }
    }
}
