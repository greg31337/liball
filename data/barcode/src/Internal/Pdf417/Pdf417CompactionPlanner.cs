namespace barcode;

internal class Pdf417CompactionPlan
{
    internal byte[] Data = new byte[0];
    internal Pdf417CompactionMode RequestedMode;
    internal int EciAssignmentNumber = -1;
    internal int[] Codewords = new int[0];
    internal Pdf417CompactionMode[] SegmentModes = new Pdf417CompactionMode[0];
    internal int[] SegmentByteStarts = new int[0];
    internal int[] SegmentByteCounts = new int[0];
    internal int[] SegmentCodewordStarts = new int[0];
    internal int[] SegmentCodewordCounts = new int[0];
}

internal class Pdf417CompactionCandidate
{
    internal int Position;
    internal int Cost;
    internal int SegmentCount;
    internal Pdf417CompactionMode Mode;
    internal Pdf417TextSubmode TextSubmode;
    internal int PendingTextValue = -1;
    internal Pdf417CompactionMode ActionMode;
    internal int ActionStart;
    internal int ActionCount;
    internal int TextFlushCodeword = -1;
    internal int[] StepCodewords = new int[0];
    internal Pdf417CompactionCandidate? Previous;
}

internal class Pdf417TextStep
{
    internal int[] Values = new int[0];
    internal Pdf417TextSubmode FinalSubmode;
}

internal static class Pdf417CompactionPlanner
{
    internal const int NoEciAssignmentNumber = -1;
    internal const int CharacterSetEciCodeword = 927;
    internal const int GeneralPurposeEciCodeword = 926;
    internal const int UserDefinedEciCodeword = 925;
    internal const int MaximumEciAssignmentNumber = 811799;
    internal const int MaximumInputByteCount = 3000;
    internal const int TextPendingStateCount = 31;
    internal const int TextStateCount = 124;
    internal const int ByteStateIndex = 124;
    internal const int NumericStateIndex = 125;
    internal const int StateCount = 126;

    internal static Pdf417CompactionPlan Plan(
        byte[] data,
        Pdf417CompactionMode requestedMode,
        int eciAssignmentNumber)
    {
        ValidateInput(data, requestedMode, eciAssignmentNumber);
        int[] eciCodewords = eciAssignmentNumber == NoEciAssignmentNumber
            ? new int[0]
            : EncodeEciDesignator(eciAssignmentNumber);
        if (requestedMode != Pdf417CompactionMode.Automatic)
        {
            return CreateForcedPlan(data, requestedMode, eciAssignmentNumber, eciCodewords);
        }

        int stateArrayLength = CheckedMath.Multiply(data.Length + 1, StateCount);
        Pdf417CompactionCandidate?[] candidates = new Pdf417CompactionCandidate?[stateArrayLength];
        Pdf417CompactionCandidate initial = new Pdf417CompactionCandidate();
        initial.Mode = Pdf417CompactionMode.Text;
        initial.TextSubmode = Pdf417TextSubmode.Alpha;
        initial.ActionMode = Pdf417CompactionMode.Automatic;
        candidates[GetStateIndex(0, initial.Mode, initial.TextSubmode, -1)] = initial;

        int position = 0;
        while (position < data.Length)
        {
            int localState = 0;
            while (localState < StateCount)
            {
                Pdf417CompactionCandidate? candidate = candidates[(position * StateCount) + localState];
                if (candidate != null)
                {
                    AddTextTransition(data, candidate, candidates);
                    AddByteTransitions(data, candidate, candidates);
                    AddNumericTransitions(data, candidate, candidates);
                }

                localState = localState + 1;
            }

            position = position + 1;
        }

        Pdf417CompactionCandidate? best = FindBestCandidate(candidates, data.Length);
        if (best == null)
        {
            throw new BarcodeEncodingException("PDF417 data cannot be represented by the available compaction modes.");
        }

        int finalTextCodeword = best.Mode == Pdf417CompactionMode.Text
            && best.PendingTextValue >= 0
            ? (best.PendingTextValue * Pdf417TextCompaction.ValueBase)
                + Pdf417TextCompaction.PaddingValue
            : -1;
        return AssembleAutomaticPlan(
            data,
            eciAssignmentNumber,
            eciCodewords,
            best,
            finalTextCodeword);
    }

    internal static int[] EncodeEciDesignator(int assignmentNumber)
    {
        if (assignmentNumber < 0 || assignmentNumber > MaximumEciAssignmentNumber)
        {
            throw new BarcodeEncodingException("PDF417 ECI assignment number must be between 0 and 811799.");
        }

        if (assignmentNumber < 900)
        {
            return new int[] { CharacterSetEciCodeword, assignmentNumber };
        }

        if (assignmentNumber < 810900)
        {
            return new int[]
            {
                GeneralPurposeEciCodeword,
                (assignmentNumber / 900) - 1,
                assignmentNumber % 900
            };
        }

        return new int[]
        {
            UserDefinedEciCodeword,
            assignmentNumber - 810900
        };
    }

    static void AddTextTransition(
        byte[] data,
        Pdf417CompactionCandidate previous,
        Pdf417CompactionCandidate?[] candidates)
    {
        if (!IsTextByte(data[previous.Position]))
        {
            return;
        }

        Pdf417TextSubmode initialSubmode = previous.Mode == Pdf417CompactionMode.Text
            ? previous.TextSubmode
            : Pdf417TextSubmode.Alpha;
        int pendingValue = previous.Mode == Pdf417CompactionMode.Text
            ? previous.PendingTextValue
            : -1;
        Pdf417TextStep step = CreateTextStep(data, previous.Position, initialSubmode);
        int[] buffer = new int[5];
        int codewordCount = 0;
        if (previous.Mode != Pdf417CompactionMode.Text)
        {
            buffer[codewordCount] = Pdf417TextCompaction.TextLatchCodeword;
            codewordCount = codewordCount + 1;
        }

        int valueIndex = 0;
        while (valueIndex < step.Values.Length)
        {
            if (pendingValue < 0)
            {
                pendingValue = step.Values[valueIndex];
            }
            else
            {
                buffer[codewordCount] = (pendingValue * Pdf417TextCompaction.ValueBase)
                    + step.Values[valueIndex];
                codewordCount = codewordCount + 1;
                pendingValue = -1;
            }

            valueIndex = valueIndex + 1;
        }

        Pdf417CompactionCandidate candidate = new Pdf417CompactionCandidate();
        candidate.Position = previous.Position + 1;
        candidate.Cost = CheckedMath.Add(previous.Cost, codewordCount);
        candidate.SegmentCount = previous.SegmentCount;
        if (previous.ActionMode != Pdf417CompactionMode.Text)
        {
            candidate.SegmentCount = candidate.SegmentCount + 1;
        }
        candidate.Mode = Pdf417CompactionMode.Text;
        candidate.TextSubmode = step.FinalSubmode;
        candidate.PendingTextValue = pendingValue;
        candidate.ActionMode = Pdf417CompactionMode.Text;
        candidate.ActionStart = previous.Position;
        candidate.ActionCount = 1;
        candidate.StepCodewords = CopyIntegerPrefix(buffer, codewordCount);
        candidate.Previous = previous;
        UpdateCandidate(candidates, candidate);
    }

    static void AddByteTransitions(
        byte[] data,
        Pdf417CompactionCandidate previous,
        Pdf417CompactionCandidate?[] candidates)
    {
        if (previous.Mode == Pdf417CompactionMode.Byte)
        {
            return;
        }

        int flushCodeword = CreatePendingTextCodeword(previous);
        int flushCost = flushCodeword < 0 ? 0 : 1;
        int byteCount = 1;
        while (previous.Position + byteCount <= data.Length)
        {
            bool shiftsFromText = previous.Mode == Pdf417CompactionMode.Text
                && byteCount == 1;
            int byteCost = GetByteCompactionCost(byteCount);
            Pdf417CompactionCandidate candidate = new Pdf417CompactionCandidate();
            candidate.Position = previous.Position + byteCount;
            candidate.Cost = CheckedMath.Add(
                CheckedMath.Add(previous.Cost, flushCost),
                byteCost);
            candidate.SegmentCount = previous.SegmentCount + 1;
            candidate.Mode = shiftsFromText
                ? Pdf417CompactionMode.Text
                : Pdf417CompactionMode.Byte;
            candidate.TextSubmode = shiftsFromText
                ? previous.TextSubmode
                : Pdf417TextSubmode.Alpha;
            candidate.PendingTextValue = -1;
            candidate.ActionMode = Pdf417CompactionMode.Byte;
            candidate.ActionStart = previous.Position;
            candidate.ActionCount = byteCount;
            candidate.TextFlushCodeword = flushCodeword;
            candidate.Previous = previous;
            UpdateCandidate(candidates, candidate);
            byteCount = byteCount + 1;
        }
    }

    static void AddNumericTransitions(
        byte[] data,
        Pdf417CompactionCandidate previous,
        Pdf417CompactionCandidate?[] candidates)
    {
        if (previous.Mode == Pdf417CompactionMode.Numeric
            || !IsDigitByte(data[previous.Position]))
        {
            return;
        }

        int flushCodeword = CreatePendingTextCodeword(previous);
        int flushCost = flushCodeword < 0 ? 0 : 1;
        int completedChunkCost = 0;
        int chunkDigitCount = 0;
        global::System.Numerics.BigInteger chunkValue = 0;
        int end = previous.Position;
        while (end < data.Length && IsDigitByte(data[end]))
        {
            if (chunkDigitCount == 0)
            {
                chunkValue = 1;
            }

            chunkValue = (chunkValue * 10) + (data[end] - '0');
            chunkDigitCount = chunkDigitCount + 1;
            int currentChunkCost = CountBase900Codewords(chunkValue);
            int numericDataCost = completedChunkCost + currentChunkCost;
            Pdf417CompactionCandidate candidate = new Pdf417CompactionCandidate();
            candidate.Position = end + 1;
            candidate.Cost = CheckedMath.Add(
                CheckedMath.Add(previous.Cost, flushCost),
                numericDataCost + 1);
            candidate.SegmentCount = previous.SegmentCount + 1;
            candidate.Mode = Pdf417CompactionMode.Numeric;
            candidate.TextSubmode = Pdf417TextSubmode.Alpha;
            candidate.PendingTextValue = -1;
            candidate.ActionMode = Pdf417CompactionMode.Numeric;
            candidate.ActionStart = previous.Position;
            candidate.ActionCount = candidate.Position - previous.Position;
            candidate.TextFlushCodeword = flushCodeword;
            candidate.Previous = previous;
            UpdateCandidate(candidates, candidate);
            if (chunkDigitCount == Pdf417NumericCompaction.MaximumDigitsPerSegment)
            {
                completedChunkCost = numericDataCost;
                chunkDigitCount = 0;
                chunkValue = 0;
            }

            end = end + 1;
        }
    }

    static Pdf417CompactionCandidate? FindBestCandidate(
        Pdf417CompactionCandidate?[] candidates,
        int position)
    {
        Pdf417CompactionCandidate? best = null;
        int bestCost = int.MaxValue;
        int bestSegmentCount = int.MaxValue;
        int bestModeRank = int.MaxValue;
        int localState = 0;
        while (localState < StateCount)
        {
            Pdf417CompactionCandidate? candidate = candidates[(position * StateCount) + localState];
            if (candidate != null)
            {
                int finalCost = candidate.Cost;
                if (candidate.Mode == Pdf417CompactionMode.Text
                    && candidate.PendingTextValue >= 0)
                {
                    finalCost = CheckedMath.Add(finalCost, 1);
                }

                int modeRank = GetModeRank(candidate.Mode);
                if (finalCost < bestCost
                    || (finalCost == bestCost
                        && candidate.SegmentCount < bestSegmentCount)
                    || (finalCost == bestCost
                        && candidate.SegmentCount == bestSegmentCount
                        && modeRank < bestModeRank))
                {
                    best = candidate;
                    bestCost = finalCost;
                    bestSegmentCount = candidate.SegmentCount;
                    bestModeRank = modeRank;
                }
            }

            localState = localState + 1;
        }

        return best;
    }

    static Pdf417CompactionPlan AssembleAutomaticPlan(
        byte[] data,
        int eciAssignmentNumber,
        int[] eciCodewords,
        Pdf417CompactionCandidate best,
        int finalTextCodeword)
    {
        int actionCount = 0;
        Pdf417CompactionCandidate? current = best;
        while (current != null && current.Previous != null)
        {
            actionCount = actionCount + 1;
            current = current.Previous;
        }

        Pdf417CompactionCandidate[] actions = new Pdf417CompactionCandidate[actionCount];
        current = best;
        int actionIndex = actionCount - 1;
        while (actionIndex >= 0 && current != null)
        {
            actions[actionIndex] = current;
            current = current.Previous;
            actionIndex = actionIndex - 1;
        }

        int finalCost = best.Cost + (finalTextCodeword < 0 ? 0 : 1);
        int[] codewords = new int[CheckedMath.Add(eciCodewords.Length, finalCost)];
        CopyIntegersToOffset(eciCodewords, codewords, 0);
        Pdf417CompactionMode[] modes = new Pdf417CompactionMode[actionCount];
        int[] byteStarts = new int[actionCount];
        int[] byteCounts = new int[actionCount];
        int[] codewordStarts = new int[actionCount];
        int[] codewordCounts = new int[actionCount];
        int outputOffset = eciCodewords.Length;
        int segmentCount = 0;
        Pdf417CompactionMode previousActionMode = Pdf417CompactionMode.Automatic;
        actionIndex = 0;
        while (actionIndex < actions.Length)
        {
            Pdf417CompactionCandidate action = actions[actionIndex];
            if (action.TextFlushCodeword >= 0)
            {
                codewords[outputOffset] = action.TextFlushCodeword;
                outputOffset = outputOffset + 1;
                codewordCounts[segmentCount - 1] = codewordCounts[segmentCount - 1] + 1;
            }

            bool startsSegment = action.ActionMode != previousActionMode;
            if (startsSegment)
            {
                modes[segmentCount] = action.ActionMode;
                byteStarts[segmentCount] = action.ActionStart;
                byteCounts[segmentCount] = action.ActionCount;
                codewordStarts[segmentCount] = outputOffset;
                segmentCount = segmentCount + 1;
            }
            else
            {
                byteCounts[segmentCount - 1] = byteCounts[segmentCount - 1]
                    + action.ActionCount;
            }

            int writtenCount;
            if (action.ActionMode == Pdf417CompactionMode.Text)
            {
                CopyIntegersToOffset(action.StepCodewords, codewords, outputOffset);
                writtenCount = action.StepCodewords.Length;
            }
            else if (action.ActionMode == Pdf417CompactionMode.Byte)
            {
                byte[] bytes = CopyByteRange(data, action.ActionStart, action.ActionCount);
                bool startsInTextMode = action.Previous != null
                    && action.Previous.Mode == Pdf417CompactionMode.Text;
                Pdf417ByteEncodingResult encoded = Pdf417ByteCompaction.EncodeBytes(
                    bytes,
                    startsInTextMode);
                CopyIntegersToOffset(encoded.Codewords, codewords, outputOffset);
                writtenCount = encoded.Codewords.Length;
            }
            else
            {
                string digits = CreateText(data, action.ActionStart, action.ActionCount);
                Pdf417NumericEncodingResult encoded = Pdf417NumericCompaction.EncodeDigits(digits);
                CopyIntegersToOffset(encoded.Codewords, codewords, outputOffset);
                writtenCount = encoded.Codewords.Length;
            }

            outputOffset = outputOffset + writtenCount;
            codewordCounts[segmentCount - 1] = codewordCounts[segmentCount - 1]
                + writtenCount;
            previousActionMode = action.ActionMode;
            actionIndex = actionIndex + 1;
        }

        if (finalTextCodeword >= 0)
        {
            codewords[outputOffset] = finalTextCodeword;
            outputOffset = outputOffset + 1;
            codewordCounts[segmentCount - 1] = codewordCounts[segmentCount - 1] + 1;
        }

        if (outputOffset != codewords.Length)
        {
            throw new BarcodeValidationException("PDF417 compaction plan cost does not match the emitted codeword count.");
        }

        Pdf417CompactionPlan plan = new Pdf417CompactionPlan();
        plan.Data = CopyBytes(data);
        plan.RequestedMode = Pdf417CompactionMode.Automatic;
        plan.EciAssignmentNumber = eciAssignmentNumber;
        plan.Codewords = codewords;
        plan.SegmentModes = CopyModePrefix(modes, segmentCount);
        plan.SegmentByteStarts = CopyIntegerPrefix(byteStarts, segmentCount);
        plan.SegmentByteCounts = CopyIntegerPrefix(byteCounts, segmentCount);
        plan.SegmentCodewordStarts = CopyIntegerPrefix(codewordStarts, segmentCount);
        plan.SegmentCodewordCounts = CopyIntegerPrefix(codewordCounts, segmentCount);
        return plan;
    }

    static Pdf417CompactionPlan CreateForcedPlan(
        byte[] data,
        Pdf417CompactionMode requestedMode,
        int eciAssignmentNumber,
        int[] eciCodewords)
    {
        int[] segmentCodewords;
        if (requestedMode == Pdf417CompactionMode.Text)
        {
            string text = CreateForcedText(data);
            segmentCodewords = Pdf417TextCompaction.EncodeText(
                text,
                Pdf417TextSubmode.Alpha).Codewords;
        }
        else if (requestedMode == Pdf417CompactionMode.Byte)
        {
            segmentCodewords = Pdf417ByteCompaction.EncodeBytes(data, true).Codewords;
        }
        else
        {
            string digits = CreateForcedDigits(data);
            segmentCodewords = Pdf417NumericCompaction.EncodeDigits(digits).Codewords;
        }

        int[] codewords = new int[CheckedMath.Add(eciCodewords.Length, segmentCodewords.Length)];
        CopyIntegersToOffset(eciCodewords, codewords, 0);
        CopyIntegersToOffset(segmentCodewords, codewords, eciCodewords.Length);
        Pdf417CompactionPlan plan = new Pdf417CompactionPlan();
        plan.Data = CopyBytes(data);
        plan.RequestedMode = requestedMode;
        plan.EciAssignmentNumber = eciAssignmentNumber;
        plan.Codewords = codewords;
        plan.SegmentModes = new Pdf417CompactionMode[] { requestedMode };
        plan.SegmentByteStarts = new int[] { 0 };
        plan.SegmentByteCounts = new int[] { data.Length };
        plan.SegmentCodewordStarts = new int[] { eciCodewords.Length };
        plan.SegmentCodewordCounts = new int[] { segmentCodewords.Length };
        return plan;
    }

    static Pdf417TextStep CreateTextStep(
        byte[] data,
        int position,
        Pdf417TextSubmode initialSubmode)
    {
        char character = (char)data[position];
        int[] buffer = new int[4];
        int valueCount = 0;
        Pdf417TextSubmode submode = initialSubmode;
        bool consumed = false;
        while (!consumed)
        {
            switch (submode)
            {
                case Pdf417TextSubmode.Alpha:
                    if (IsAlpha(character))
                    {
                        buffer[valueCount] = character == ' ' ? 26 : character - 'A';
                        valueCount = valueCount + 1;
                        consumed = true;
                    }
                    else if (IsLower(character))
                    {
                        buffer[valueCount] = 27;
                        valueCount = valueCount + 1;
                        submode = Pdf417TextSubmode.Lower;
                    }
                    else if (FindCharacter(Pdf417TextCompaction.MixedCharacters, character) >= 0)
                    {
                        buffer[valueCount] = 28;
                        valueCount = valueCount + 1;
                        submode = Pdf417TextSubmode.Mixed;
                    }
                    else
                    {
                        buffer[valueCount] = 29;
                        buffer[valueCount + 1] = FindCharacter(
                            Pdf417TextCompaction.PunctuationCharacters,
                            character);
                        valueCount = valueCount + 2;
                        consumed = true;
                    }

                    break;
                case Pdf417TextSubmode.Lower:
                    if (IsLower(character))
                    {
                        buffer[valueCount] = character == ' ' ? 26 : character - 'a';
                        valueCount = valueCount + 1;
                        consumed = true;
                    }
                    else if (IsAlpha(character))
                    {
                        buffer[valueCount] = 27;
                        buffer[valueCount + 1] = character - 'A';
                        valueCount = valueCount + 2;
                        consumed = true;
                    }
                    else if (FindCharacter(Pdf417TextCompaction.MixedCharacters, character) >= 0)
                    {
                        buffer[valueCount] = 28;
                        valueCount = valueCount + 1;
                        submode = Pdf417TextSubmode.Mixed;
                    }
                    else
                    {
                        buffer[valueCount] = 29;
                        buffer[valueCount + 1] = FindCharacter(
                            Pdf417TextCompaction.PunctuationCharacters,
                            character);
                        valueCount = valueCount + 2;
                        consumed = true;
                    }

                    break;
                case Pdf417TextSubmode.Mixed:
                    int mixedValue = FindCharacter(
                        Pdf417TextCompaction.MixedCharacters,
                        character);
                    if (mixedValue >= 0 || character == ' ')
                    {
                        buffer[valueCount] = character == ' ' ? 26 : mixedValue;
                        valueCount = valueCount + 1;
                        consumed = true;
                    }
                    else if (IsAlpha(character))
                    {
                        buffer[valueCount] = 28;
                        valueCount = valueCount + 1;
                        submode = Pdf417TextSubmode.Alpha;
                    }
                    else if (IsLower(character))
                    {
                        buffer[valueCount] = 27;
                        valueCount = valueCount + 1;
                        submode = Pdf417TextSubmode.Lower;
                    }
                    else if (position + 1 < data.Length
                        && FindCharacter(
                            Pdf417TextCompaction.PunctuationCharacters,
                            (char)data[position + 1]) >= 0)
                    {
                        buffer[valueCount] = 25;
                        valueCount = valueCount + 1;
                        submode = Pdf417TextSubmode.Punctuation;
                    }
                    else
                    {
                        buffer[valueCount] = 29;
                        buffer[valueCount + 1] = FindCharacter(
                            Pdf417TextCompaction.PunctuationCharacters,
                            character);
                        valueCount = valueCount + 2;
                        consumed = true;
                    }

                    break;
                default:
                    int punctuationValue = FindCharacter(
                        Pdf417TextCompaction.PunctuationCharacters,
                        character);
                    if (punctuationValue >= 0)
                    {
                        buffer[valueCount] = punctuationValue;
                        valueCount = valueCount + 1;
                        consumed = true;
                    }
                    else
                    {
                        buffer[valueCount] = 29;
                        valueCount = valueCount + 1;
                        submode = Pdf417TextSubmode.Alpha;
                    }

                    break;
            }
        }

        Pdf417TextStep step = new Pdf417TextStep();
        step.Values = CopyIntegerPrefix(buffer, valueCount);
        step.FinalSubmode = submode;
        return step;
    }

    static void UpdateCandidate(
        Pdf417CompactionCandidate?[] candidates,
        Pdf417CompactionCandidate candidate)
    {
        int stateIndex = GetStateIndex(
            candidate.Position,
            candidate.Mode,
            candidate.TextSubmode,
            candidate.PendingTextValue);
        Pdf417CompactionCandidate? existing = candidates[stateIndex];
        if (existing == null
            || candidate.Cost < existing.Cost
            || (candidate.Cost == existing.Cost
                && candidate.SegmentCount < existing.SegmentCount))
        {
            candidates[stateIndex] = candidate;
        }
    }

    static int GetStateIndex(
        int position,
        Pdf417CompactionMode mode,
        Pdf417TextSubmode textSubmode,
        int pendingTextValue)
    {
        int localState;
        if (mode == Pdf417CompactionMode.Text)
        {
            localState = ((int)textSubmode * TextPendingStateCount)
                + pendingTextValue + 1;
        }
        else if (mode == Pdf417CompactionMode.Byte)
        {
            localState = ByteStateIndex;
        }
        else
        {
            localState = NumericStateIndex;
        }

        return (position * StateCount) + localState;
    }

    static int GetByteCompactionCost(int byteCount)
    {
        int packedGroupCount = byteCount / Pdf417ByteCompaction.BytesPerPackedGroup;
        int remainderCount = byteCount
            - (packedGroupCount * Pdf417ByteCompaction.BytesPerPackedGroup);
        return 1
            + (packedGroupCount * Pdf417ByteCompaction.CodewordsPerPackedGroup)
            + remainderCount;
    }

    static int CountBase900Codewords(global::System.Numerics.BigInteger value)
    {
        int count = 0;
        while (value > 0)
        {
            value = value / Pdf417NumericCompaction.CodewordBase;
            count = count + 1;
        }

        return count;
    }

    static int CreatePendingTextCodeword(Pdf417CompactionCandidate candidate)
    {
        if (candidate.Mode != Pdf417CompactionMode.Text
            || candidate.PendingTextValue < 0)
        {
            return -1;
        }

        return (candidate.PendingTextValue * Pdf417TextCompaction.ValueBase)
            + Pdf417TextCompaction.PaddingValue;
    }

    static int GetModeRank(Pdf417CompactionMode mode)
    {
        if (mode == Pdf417CompactionMode.Text)
        {
            return 0;
        }

        if (mode == Pdf417CompactionMode.Numeric)
        {
            return 1;
        }

        return 2;
    }

    static bool IsTextByte(byte value)
    {
        return value == 9
            || value == 10
            || value == 13
            || (value >= 32 && value <= 126);
    }

    static bool IsDigitByte(byte value)
    {
        return value >= '0' && value <= '9';
    }

    static bool IsAlpha(char character)
    {
        return character == ' ' || (character >= 'A' && character <= 'Z');
    }

    static bool IsLower(char character)
    {
        return character == ' ' || (character >= 'a' && character <= 'z');
    }

    static int FindCharacter(string characters, char character)
    {
        int index = 0;
        while (index < characters.Length)
        {
            if (characters[index] == character)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    static string CreateForcedText(byte[] data)
    {
        int index = 0;
        while (index < data.Length)
        {
            if (!IsTextByte(data[index]))
            {
                throw new BarcodeEncodingException("Forced PDF417 text compaction accepts printable ASCII, tab, line feed, and carriage return only.");
            }

            index = index + 1;
        }

        return CreateText(data, 0, data.Length);
    }

    static string CreateForcedDigits(byte[] data)
    {
        int index = 0;
        while (index < data.Length)
        {
            if (!IsDigitByte(data[index]))
            {
                throw new BarcodeEncodingException("Forced PDF417 numeric compaction accepts decimal digits only.");
            }

            index = index + 1;
        }

        return CreateText(data, 0, data.Length);
    }

    static string CreateText(byte[] data, int start, int count)
    {
        char[] characters = new char[count];
        int index = 0;
        while (index < count)
        {
            characters[index] = (char)data[start + index];
            index = index + 1;
        }

        return new string(characters);
    }

    static byte[] CopyByteRange(byte[] source, int start, int count)
    {
        byte[] copy = new byte[count];
        int index = 0;
        while (index < count)
        {
            copy[index] = source[start + index];
            index = index + 1;
        }

        return copy;
    }

    static byte[] CopyBytes(byte[] source)
    {
        return CopyByteRange(source, 0, source.Length);
    }

    static int[] CopyIntegerPrefix(int[] source, int count)
    {
        int[] copy = new int[count];
        int index = 0;
        while (index < count)
        {
            copy[index] = source[index];
            index = index + 1;
        }

        return copy;
    }

    static Pdf417CompactionMode[] CopyModePrefix(
        Pdf417CompactionMode[] source,
        int count)
    {
        Pdf417CompactionMode[] copy = new Pdf417CompactionMode[count];
        int index = 0;
        while (index < count)
        {
            copy[index] = source[index];
            index = index + 1;
        }

        return copy;
    }

    static void CopyIntegersToOffset(int[] source, int[] target, int targetOffset)
    {
        int index = 0;
        while (index < source.Length)
        {
            target[targetOffset + index] = source[index];
            index = index + 1;
        }
    }

    static void ValidateInput(
        byte[] data,
        Pdf417CompactionMode requestedMode,
        int eciAssignmentNumber)
    {
        if (data == null)
        {
            throw new BarcodeEncodingException("PDF417 compaction-planning data must not be null.");
        }

        if (data.Length == 0)
        {
            throw new BarcodeEncodingException("PDF417 compaction planning requires at least one byte.");
        }

        if (data.Length > MaximumInputByteCount)
        {
            throw new BarcodeEncodingException("PDF417 compaction-planning input exceeds the supported symbol range.");
        }

        if (requestedMode != Pdf417CompactionMode.Automatic
            && requestedMode != Pdf417CompactionMode.Text
            && requestedMode != Pdf417CompactionMode.Byte
            && requestedMode != Pdf417CompactionMode.Numeric)
        {
            throw new BarcodeEncodingException("PDF417 requested compaction mode is invalid.");
        }

        if (eciAssignmentNumber < NoEciAssignmentNumber
            || eciAssignmentNumber > MaximumEciAssignmentNumber)
        {
            throw new BarcodeEncodingException("PDF417 ECI assignment number must be -1 or between 0 and 811799.");
        }
    }
}
