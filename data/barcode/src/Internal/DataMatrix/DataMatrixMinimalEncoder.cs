namespace barcode;

internal class DataMatrixEncodingPlan
{
    internal byte[] Data = new byte[0];
    internal DataMatrixEncodingMode RequestedMode;
    internal int[] Codewords = new int[0];
    internal DataMatrixEncodingMode[] SegmentModes = new DataMatrixEncodingMode[0];
    internal int[] SegmentByteStarts = new int[0];
    internal int[] SegmentByteCounts = new int[0];
    internal int[] SegmentCodewordStarts = new int[0];
    internal int[] SegmentCodewordCounts = new int[0];
}

internal class DataMatrixPlanCandidate
{
    internal int Position;
    internal int Cost;
    internal int SegmentCount;
    internal DataMatrixEncodingMode SegmentMode;
    internal int SegmentStart;
    internal int SegmentByteCount;
    internal DataMatrixPlanCandidate? Previous;
}

internal static class DataMatrixMinimalEncoder
{
    internal const int MaximumInputByteCount = 3116;
    internal const int MaximumPlannedCodewordCount = 1558;

    internal static DataMatrixEncodingPlan Plan(
        byte[] data,
        DataMatrixEncodingMode requestedMode)
    {
        return PlanAtCodewordPosition(data, requestedMode, 1);
    }

    internal static DataMatrixEncodingPlan PlanAtCodewordPosition(
        byte[] data,
        DataMatrixEncodingMode requestedMode,
        int firstCodewordPosition)
    {
        ValidatePlanInput(data, requestedMode);
        if (firstCodewordPosition < 1
            || firstCodewordPosition > MaximumPlannedCodewordCount)
        {
            throw new BarcodeValidationException("Data Matrix first planned codeword position is outside the standard symbol range.");
        }

        if (requestedMode != DataMatrixEncodingMode.Automatic)
        {
            return CreateForcedPlan(data, requestedMode, firstCodewordPosition);
        }

        int[] asciiCosts = CreateAsciiCostTable(data);
        int width = data.Length + 1;
        DataMatrixPlanCandidate?[] candidates = new DataMatrixPlanCandidate?[width];
        DataMatrixPlanCandidate initial = new DataMatrixPlanCandidate();
        initial.Position = 0;
        candidates[0] = initial;

        int start = 0;
        while (start < data.Length)
        {
            DataMatrixPlanCandidate? previous = candidates[start];
            if (previous != null)
            {
                AddAutomaticTransitions(data, asciiCosts, width, start, previous, candidates);
            }

            start = start + 1;
        }

        DataMatrixPlanCandidate? best = candidates[data.Length];
        if (best == null)
        {
            throw new BarcodeEncodingException("Data Matrix data exceeds the maximum standard symbol capacity.");
        }

        return CreateAutomaticPlan(data, best, firstCodewordPosition);
    }

    internal static DataMatrixEncodingPlan PlanGs1ElementString(
        byte[] data,
        DataMatrixEncodingMode requestedMode,
        int firstCodewordPosition)
    {
        if (data == null || data.Length == 0)
        {
            throw new BarcodeEncodingException("GS1 DataMatrix element string must not be empty.");
        }

        if (data.Length > MaximumInputByteCount)
        {
            throw new BarcodeEncodingException("GS1 DataMatrix element string exceeds the maximum input length.");
        }

        int separatorCount = CountGs1Separators(data);
        if (separatorCount == 0)
        {
            return PlanAtCodewordPosition(data, requestedMode, firstCodewordPosition);
        }

        DataMatrixEncodingPlan[] parts = new DataMatrixEncodingPlan[separatorCount + 1];
        int totalCodewordCount = separatorCount;
        int totalSegmentCount = 0;
        int plannedCodewordCount = 0;
        int partIndex = 0;
        int partStart = 0;
        int index = 0;
        while (index <= data.Length)
        {
            if (index == data.Length || data[index] == 29)
            {
                byte[] partData = CopyByteRange(data, partStart, index - partStart);
                DataMatrixEncodingPlan part = PlanAtCodewordPosition(
                    partData,
                    requestedMode,
                    CheckedMath.Add(firstCodewordPosition, plannedCodewordCount));
                parts[partIndex] = part;
                totalCodewordCount = CheckedMath.Add(totalCodewordCount, part.Codewords.Length);
                totalSegmentCount = CheckedMath.Add(totalSegmentCount, part.SegmentModes.Length);
                plannedCodewordCount = CheckedMath.Add(plannedCodewordCount, part.Codewords.Length);
                if (index < data.Length)
                {
                    plannedCodewordCount = CheckedMath.Add(plannedCodewordCount, 1);
                }
                partIndex = partIndex + 1;
                partStart = index + 1;
            }

            index = index + 1;
        }

        if (totalCodewordCount > MaximumPlannedCodewordCount)
        {
            throw new BarcodeEncodingException("GS1 DataMatrix element data exceeds the maximum standard symbol capacity.");
        }

        return AssembleGs1Plan(
            data,
            requestedMode,
            parts,
            totalCodewordCount,
            totalSegmentCount);
    }

    static int CountGs1Separators(byte[] data)
    {
        int count = 0;
        bool previousWasSeparator = false;
        int index = 0;
        while (index < data.Length)
        {
            if (data[index] == 29)
            {
                if (index == 0 || index == data.Length - 1 || previousWasSeparator)
                {
                    throw new BarcodeEncodingException("GS1 DataMatrix separators must occur singly between element data.");
                }

                count = count + 1;
                previousWasSeparator = true;
            }
            else
            {
                previousWasSeparator = false;
            }

            index = index + 1;
        }

        return count;
    }

    static DataMatrixEncodingPlan AssembleGs1Plan(
        byte[] data,
        DataMatrixEncodingMode requestedMode,
        DataMatrixEncodingPlan[] parts,
        int totalCodewordCount,
        int totalSegmentCount)
    {
        int[] codewords = new int[totalCodewordCount];
        DataMatrixEncodingMode[] modes = new DataMatrixEncodingMode[totalSegmentCount];
        int[] byteStarts = new int[totalSegmentCount];
        int[] byteCounts = new int[totalSegmentCount];
        int[] codewordStarts = new int[totalSegmentCount];
        int[] codewordCounts = new int[totalSegmentCount];
        int outputOffset = 0;
        int byteOffset = 0;
        int segmentOffset = 0;
        int partIndex = 0;
        while (partIndex < parts.Length)
        {
            DataMatrixEncodingPlan part = parts[partIndex];
            CopyIntegers(part.Codewords, codewords, outputOffset);
            int segmentIndex = 0;
            while (segmentIndex < part.SegmentModes.Length)
            {
                modes[segmentOffset] = part.SegmentModes[segmentIndex];
                byteStarts[segmentOffset] = byteOffset + part.SegmentByteStarts[segmentIndex];
                byteCounts[segmentOffset] = part.SegmentByteCounts[segmentIndex];
                codewordStarts[segmentOffset] = outputOffset + part.SegmentCodewordStarts[segmentIndex];
                codewordCounts[segmentOffset] = part.SegmentCodewordCounts[segmentIndex];
                segmentOffset = segmentOffset + 1;
                segmentIndex = segmentIndex + 1;
            }

            outputOffset = outputOffset + part.Codewords.Length;
            byteOffset = byteOffset + part.Data.Length;
            if (partIndex < parts.Length - 1)
            {
                codewords[outputOffset] = DataMatrixHighLevelEncoder.Fnc1Codeword;
                outputOffset = outputOffset + 1;
                byteOffset = byteOffset + 1;
            }

            partIndex = partIndex + 1;
        }

        DataMatrixEncodingPlan plan = new DataMatrixEncodingPlan();
        plan.Data = CopyBytes(data);
        plan.RequestedMode = requestedMode;
        plan.Codewords = codewords;
        plan.SegmentModes = modes;
        plan.SegmentByteStarts = byteStarts;
        plan.SegmentByteCounts = byteCounts;
        plan.SegmentCodewordStarts = codewordStarts;
        plan.SegmentCodewordCounts = codewordCounts;
        return plan;
    }

    static void AddAutomaticTransitions(
        byte[] data,
        int[] asciiCosts,
        int width,
        int start,
        DataMatrixPlanCandidate previous,
        DataMatrixPlanCandidate?[] candidates)
    {
        int c40ValueCount = 0;
        int c40AlignedValueCount = 0;
        int c40AlignedEnd = start;
        bool hasC40Alignment = false;
        int textValueCount = 0;
        int textAlignedValueCount = 0;
        int textAlignedEnd = start;
        bool hasTextAlignment = false;
        bool x12Eligible = true;
        bool edifactEligible = true;
        int end = start + 1;
        while (end <= data.Length)
        {
            int byteValue = data[end - 1];
            c40ValueCount = c40ValueCount + GetC40TextValueCount(byteValue, false);
            if (c40ValueCount % 3 == 0)
            {
                c40AlignedValueCount = c40ValueCount;
                c40AlignedEnd = end;
                hasC40Alignment = true;
            }

            textValueCount = textValueCount + GetC40TextValueCount(byteValue, true);
            if (textValueCount % 3 == 0)
            {
                textAlignedValueCount = textValueCount;
                textAlignedEnd = end;
                hasTextAlignment = true;
            }

            if (!IsX12Byte(byteValue))
            {
                x12Eligible = false;
            }

            if (byteValue < 32 || byteValue > 94)
            {
                edifactEligible = false;
            }

            int asciiCost = GetAsciiCost(asciiCosts, width, start, end);
            AddCandidate(candidates, previous, end, DataMatrixEncodingMode.Ascii, asciiCost);

            if (hasC40Alignment)
            {
                int tailCost = GetAsciiCost(asciiCosts, width, c40AlignedEnd, end);
                int c40Cost = 2 + ((c40AlignedValueCount / 3) * 2) + tailCost;
                AddCandidate(candidates, previous, end, DataMatrixEncodingMode.C40, c40Cost);
            }

            if (hasTextAlignment)
            {
                int tailCost = GetAsciiCost(asciiCosts, width, textAlignedEnd, end);
                int textCost = 2 + ((textAlignedValueCount / 3) * 2) + tailCost;
                AddCandidate(candidates, previous, end, DataMatrixEncodingMode.Text, textCost);
            }

            int byteCount = end - start;
            if (x12Eligible && byteCount >= 3)
            {
                int modeByteCount = byteCount - (byteCount % 3);
                int tailStart = start + modeByteCount;
                int x12Cost = 2
                    + ((modeByteCount / 3) * 2)
                    + GetAsciiCost(asciiCosts, width, tailStart, end);
                AddCandidate(candidates, previous, end, DataMatrixEncodingMode.X12, x12Cost);
            }

            if (edifactEligible)
            {
                int edifactCost = 1 + (((byteCount + 1) * 6 + 7) / 8);
                AddCandidate(candidates, previous, end, DataMatrixEncodingMode.Edifact, edifactCost);
            }

            if (byteCount <= DataMatrixModeCodec.MaximumBase256DataCount)
            {
                int lengthFieldCount = byteCount <= 249 ? 1 : 2;
                int base256Cost = 1 + lengthFieldCount + byteCount;
                AddCandidate(candidates, previous, end, DataMatrixEncodingMode.Base256, base256Cost);
            }

            end = end + 1;
        }
    }

    static void AddCandidate(
        DataMatrixPlanCandidate?[] candidates,
        DataMatrixPlanCandidate previous,
        int targetPosition,
        DataMatrixEncodingMode segmentMode,
        int segmentCost)
    {
        int totalCost = previous.Cost + segmentCost;
        if (totalCost > MaximumPlannedCodewordCount)
        {
            return;
        }

        int segmentCount = previous.SegmentCount + 1;
        DataMatrixPlanCandidate? existing = candidates[targetPosition];
        if (existing != null
            && (existing.Cost < totalCost
                || (existing.Cost == totalCost && existing.SegmentCount <= segmentCount)))
        {
            return;
        }

        DataMatrixPlanCandidate candidate = new DataMatrixPlanCandidate();
        candidate.Position = targetPosition;
        candidate.Cost = totalCost;
        candidate.SegmentCount = segmentCount;
        candidate.SegmentMode = segmentMode;
        candidate.SegmentStart = previous.Position;
        candidate.SegmentByteCount = targetPosition - previous.Position;
        candidate.Previous = previous;
        candidates[targetPosition] = candidate;
    }

    static DataMatrixEncodingPlan CreateAutomaticPlan(
        byte[] data,
        DataMatrixPlanCandidate best,
        int firstCodewordPosition)
    {
        int segmentCount = best.SegmentCount;
        DataMatrixEncodingMode[] modes = new DataMatrixEncodingMode[segmentCount];
        int[] byteStarts = new int[segmentCount];
        int[] byteCounts = new int[segmentCount];
        DataMatrixPlanCandidate? candidate = best;
        int segmentIndex = segmentCount - 1;
        while (candidate != null && candidate.Previous != null)
        {
            modes[segmentIndex] = candidate.SegmentMode;
            byteStarts[segmentIndex] = candidate.SegmentStart;
            byteCounts[segmentIndex] = candidate.SegmentByteCount;
            candidate = candidate.Previous;
            segmentIndex = segmentIndex - 1;
        }

        return AssemblePlan(
            data,
            DataMatrixEncodingMode.Automatic,
            modes,
            byteStarts,
            byteCounts,
            best.Cost,
            firstCodewordPosition);
    }

    static DataMatrixEncodingPlan CreateForcedPlan(
        byte[] data,
        DataMatrixEncodingMode requestedMode,
        int firstCodewordPosition)
    {
        ValidateForcedModeData(data, requestedMode);
        DataMatrixEncodingMode[] modes = new DataMatrixEncodingMode[] { requestedMode };
        int[] byteStarts = new int[] { 0 };
        int[] byteCounts = new int[] { data.Length };
        int[] codewords = EncodePlanSegment(
            data,
            0,
            data.Length,
            requestedMode,
            firstCodewordPosition - 1);
        if (codewords.Length > MaximumPlannedCodewordCount)
        {
            throw new BarcodeEncodingException("Data Matrix data exceeds the maximum standard symbol capacity.");
        }

        return AssemblePlan(
            data,
            requestedMode,
            modes,
            byteStarts,
            byteCounts,
            codewords.Length,
            firstCodewordPosition);
    }

    static DataMatrixEncodingPlan AssemblePlan(
        byte[] data,
        DataMatrixEncodingMode requestedMode,
        DataMatrixEncodingMode[] modes,
        int[] byteStarts,
        int[] byteCounts,
        int totalCodewordCount,
        int firstCodewordPosition)
    {
        int[] codewordStarts = new int[modes.Length];
        int[] codewordCounts = new int[modes.Length];
        int[] codewords = new int[totalCodewordCount];
        int outputIndex = 0;
        int segmentIndex = 0;
        while (segmentIndex < modes.Length)
        {
            codewordStarts[segmentIndex] = outputIndex;
            int[] segment = EncodePlanSegment(
                data,
                byteStarts[segmentIndex],
                byteCounts[segmentIndex],
                modes[segmentIndex],
                outputIndex + firstCodewordPosition - 1);
            codewordCounts[segmentIndex] = segment.Length;
            int index = 0;
            while (index < segment.Length)
            {
                if (outputIndex >= codewords.Length)
                {
                    throw new BarcodeValidationException("Data Matrix plan cost does not match its assembled segments.");
                }

                codewords[outputIndex] = segment[index];
                outputIndex = outputIndex + 1;
                index = index + 1;
            }

            segmentIndex = segmentIndex + 1;
        }

        if (outputIndex != codewords.Length)
        {
            throw new BarcodeValidationException("Data Matrix plan cost does not match its assembled codeword count.");
        }

        DataMatrixEncodingPlan plan = new DataMatrixEncodingPlan();
        plan.Data = CopyBytes(data);
        plan.RequestedMode = requestedMode;
        plan.Codewords = codewords;
        plan.SegmentModes = modes;
        plan.SegmentByteStarts = byteStarts;
        plan.SegmentByteCounts = byteCounts;
        plan.SegmentCodewordStarts = codewordStarts;
        plan.SegmentCodewordCounts = codewordCounts;
        return plan;
    }

    static int[] EncodePlanSegment(
        byte[] data,
        int start,
        int count,
        DataMatrixEncodingMode mode,
        int codewordStart)
    {
        byte[] segmentData = CopyByteRange(data, start, count);
        int[] codewords;
        if (mode == DataMatrixEncodingMode.Ascii)
        {
            codewords = DataMatrixHighLevelEncoder.EncodeAscii(segmentData);
        }
        else if (mode == DataMatrixEncodingMode.C40)
        {
            codewords = DataMatrixModeCodec.EncodeC40(segmentData);
        }
        else if (mode == DataMatrixEncodingMode.Text)
        {
            codewords = DataMatrixModeCodec.EncodeText(segmentData);
        }
        else if (mode == DataMatrixEncodingMode.X12)
        {
            codewords = DataMatrixModeCodec.EncodeX12(segmentData);
        }
        else if (mode == DataMatrixEncodingMode.Edifact)
        {
            codewords = DataMatrixModeCodec.EncodeEdifact(segmentData);
        }
        else if (mode == DataMatrixEncodingMode.Base256)
        {
            codewords = DataMatrixModeCodec.EncodeBase256(segmentData, codewordStart + 1);
        }
        else
        {
            throw new BarcodeValidationException("Data Matrix plan contains an invalid segment mode.");
        }

        return codewords;
    }

    static void CopyIntegers(int[] source, int[] target, int targetStart)
    {
        int index = 0;
        while (index < source.Length)
        {
            target[targetStart + index] = source[index];
            index = index + 1;
        }
    }

    static int[] CreateAsciiCostTable(byte[] data)
    {
        int width = data.Length + 1;
        int[] costs = new int[CheckedMath.Multiply(width, width)];
        int start = 0;
        while (start < data.Length)
        {
            int cost = 0;
            int digitRunLength = 0;
            int end = start + 1;
            while (end <= data.Length)
            {
                int value = data[end - 1];
                if (value >= 48 && value <= 57)
                {
                    digitRunLength = digitRunLength + 1;
                    cost = cost + 1;
                    if (digitRunLength % 2 == 0)
                    {
                        cost = cost - 1;
                    }
                }
                else
                {
                    digitRunLength = 0;
                    cost = cost + (value >= 128 ? 2 : 1);
                }

                costs[(start * width) + end] = cost;
                end = end + 1;
            }

            start = start + 1;
        }

        return costs;
    }

    static int GetAsciiCost(int[] costs, int width, int start, int end)
    {
        if (start == end)
        {
            return 0;
        }

        return costs[(start * width) + end];
    }

    static int GetC40TextValueCount(int byteValue, bool textMode)
    {
        int extra = 0;
        int value = byteValue;
        if (value >= 128)
        {
            extra = 2;
            value = value - 128;
        }

        if (value <= 31)
        {
            return extra + 2;
        }

        if (value == 32 || (value >= 48 && value <= 57))
        {
            return extra + 1;
        }

        if (!textMode && value >= 65 && value <= 90)
        {
            return extra + 1;
        }

        if (textMode && value >= 97 && value <= 122)
        {
            return extra + 1;
        }

        return extra + 2;
    }

    static bool IsX12Byte(int value)
    {
        return value == 13
            || value == 42
            || value == 62
            || value == 32
            || (value >= 48 && value <= 57)
            || (value >= 65 && value <= 90);
    }

    static void ValidateForcedModeData(byte[] data, DataMatrixEncodingMode mode)
    {
        int index = 0;
        if (mode == DataMatrixEncodingMode.X12)
        {
            while (index < data.Length)
            {
                if (!IsX12Byte(data[index]))
                {
                    throw new BarcodeEncodingException("Forced Data Matrix ANSI X12 mode cannot encode the supplied byte data.");
                }

                index = index + 1;
            }
        }
        else if (mode == DataMatrixEncodingMode.Edifact)
        {
            while (index < data.Length)
            {
                if (data[index] < 32 || data[index] > 94)
                {
                    throw new BarcodeEncodingException("Forced Data Matrix EDIFACT mode cannot encode the supplied byte data.");
                }

                index = index + 1;
            }
        }
        else if (mode == DataMatrixEncodingMode.Base256
            && data.Length > DataMatrixModeCodec.MaximumBase256DataCount)
        {
            throw new BarcodeEncodingException("Forced Data Matrix Base 256 mode exceeds its maximum segment length.");
        }
    }

    static void ValidatePlanInput(byte[] data, DataMatrixEncodingMode requestedMode)
    {
        if (data == null)
        {
            throw new BarcodeEncodingException("Data Matrix planning data must not be null.");
        }

        if (data.Length == 0)
        {
            throw new BarcodeEncodingException("Data Matrix planning data must not be empty.");
        }

        if (data.Length > MaximumInputByteCount)
        {
            throw new BarcodeEncodingException("Data Matrix input exceeds the maximum numeric byte count.");
        }

        if (requestedMode != DataMatrixEncodingMode.Automatic
            && requestedMode != DataMatrixEncodingMode.Ascii
            && requestedMode != DataMatrixEncodingMode.C40
            && requestedMode != DataMatrixEncodingMode.Text
            && requestedMode != DataMatrixEncodingMode.X12
            && requestedMode != DataMatrixEncodingMode.Edifact
            && requestedMode != DataMatrixEncodingMode.Base256)
        {
            throw new BarcodeEncodingException("Data Matrix requested encoding mode is invalid.");
        }
    }

    static byte[] CopyBytes(byte[] source)
    {
        return CopyByteRange(source, 0, source.Length);
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

}
