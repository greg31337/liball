namespace barcode;

internal class Code128DecodedData
{
    internal byte[] Bytes = new byte[0];
    internal int[] Tokens = new int[0];
    internal int[] Codewords = new int[0];
    internal Code128CodeSet StartCodeSet;
    internal int ChecksumValue;
    internal bool IsReversed;
    internal int StartPosition;
    internal int EndPosition;
    internal double ModuleSize;
    internal double AverageVariance;
}

internal class Code128PatternMatch
{
    internal int Codeword;
    internal int StartPosition;
    internal int EndPosition;
    internal double ModuleSize;
    internal double Variance;
}

internal class Code128SemanticData
{
    internal byte[] Bytes = new byte[0];
    internal int[] Tokens = new int[0];
}

internal static class Code128ScanlineDecoder
{
    internal const double MaximumAverageVariance = 0.25;
    internal const double MaximumIndividualVariance = 0.7;
    internal const int MaximumCompleteCodewordCount = 104;
    internal const int MaximumDecodedValueCount = 202;

    internal static Code128DecodedData DecodeRow(BitRow row)
    {
        Code128DecodedData? decoded = TryDecodeRow(row);
        if (decoded == null)
        {
            throw new BarcodeDecodingException("The scanline does not contain a valid Code 128 symbol.");
        }

        return decoded;
    }

    internal static Code128DecodedData? TryDecodeRow(BitRow row)
    {
        if (row == null)
        {
            throw new BarcodeValidationException("Code 128 scanline must not be null.");
        }

        Code128DecodedData? forward = TryDecodeForward(row);
        if (forward != null)
        {
            forward.IsReversed = false;
            return forward;
        }

        BitRow reversedRow = CreateReversedRow(row);
        Code128DecodedData? reversed = TryDecodeForward(reversedRow);
        if (reversed == null)
        {
            return null;
        }

        int originalStart = row.Length - reversed.EndPosition;
        int originalEnd = row.Length - reversed.StartPosition;
        reversed.StartPosition = originalStart;
        reversed.EndPosition = originalEnd;
        reversed.IsReversed = true;
        return reversed;
    }

    static Code128DecodedData? TryDecodeForward(BitRow row)
    {
        int position = 0;
        while (position < row.Length)
        {
            bool dark = row.ReadBitUnchecked(position);
            bool atRunStart = position == 0 || row.ReadBitUnchecked(position - 1) != dark;
            if (dark && atRunStart)
            {
                Code128PatternMatch? start = MatchStartAt(row, position);
                if (start != null)
                {
                    Code128DecodedData? decoded = DecodeFromStart(row, start);
                    if (decoded != null)
                    {
                        return decoded;
                    }
                }
            }

            position = position + 1;
        }

        return null;
    }

    static Code128PatternMatch? MatchStartAt(BitRow row, int position)
    {
        int[] observed = RunLength.ExtractForward(row, position, Code128Patterns.OrdinaryElementCount);
        if (observed.Length != Code128Patterns.OrdinaryElementCount)
        {
            return null;
        }

        int bestCodeword = -1;
        double bestVariance = MaximumAverageVariance;
        int codeword = Code128Patterns.StartACodeword;
        while (codeword <= Code128Patterns.StartCCodeword)
        {
            int[] expected = Code128Patterns.GetPatternWidths(codeword);
            double variance = PatternMatcher.CalculateNormalizedVariance(observed, expected, MaximumIndividualVariance);
            if (variance < bestVariance)
            {
                bestVariance = variance;
                bestCodeword = codeword;
            }

            codeword = codeword + 1;
        }

        if (bestCodeword < 0)
        {
            return null;
        }

        int observedWidth = PatternMatcher.SumPositiveRuns(observed);
        double moduleSize = observedWidth / (double)Code128Patterns.OrdinaryModuleCount;
        int quietZone = CountLightBefore(row, position);
        if ((double)quietZone < Code128Layout.StandardQuietZoneModules * moduleSize)
        {
            return null;
        }

        Code128PatternMatch match = new Code128PatternMatch();
        match.Codeword = bestCodeword;
        match.StartPosition = position;
        match.EndPosition = CheckedMath.Add(position, observedWidth);
        match.ModuleSize = moduleSize;
        match.Variance = bestVariance;
        return match;
    }

    static Code128DecodedData? DecodeFromStart(BitRow row, Code128PatternMatch start)
    {
        int[] codewordBuffer = new int[MaximumCompleteCodewordCount];
        codewordBuffer[0] = start.Codeword;
        int codewordCount = 1;
        int position = start.EndPosition;
        double totalVariance = start.Variance;
        int matchedPatternCount = 1;

        while (position < row.Length)
        {
            Code128PatternMatch? stop = MatchStopAt(row, position);
            if (stop != null)
            {
                if (codewordCount < 3 || codewordCount >= MaximumCompleteCodewordCount)
                {
                    return null;
                }

                codewordBuffer[codewordCount] = Code128Patterns.StopCodeword;
                codewordCount = codewordCount + 1;
                if (!HasValidChecksum(codewordBuffer, codewordCount))
                {
                    return null;
                }

                Code128SemanticData? semantic = DecodeSemanticValues(codewordBuffer, codewordCount, start.Codeword);
                if (semantic == null)
                {
                    return null;
                }

                totalVariance = totalVariance + stop.Variance;
                matchedPatternCount = matchedPatternCount + 1;
                return CreateDecodedData(
                    codewordBuffer,
                    codewordCount,
                    semantic,
                    start,
                    stop,
                    totalVariance / matchedPatternCount);
            }

            if (codewordCount >= MaximumCompleteCodewordCount - 1)
            {
                return null;
            }

            Code128PatternMatch? ordinary = MatchOrdinaryAt(row, position);
            if (ordinary == null)
            {
                return null;
            }

            codewordBuffer[codewordCount] = ordinary.Codeword;
            codewordCount = codewordCount + 1;
            position = ordinary.EndPosition;
            totalVariance = totalVariance + ordinary.Variance;
            matchedPatternCount = matchedPatternCount + 1;
        }

        return null;
    }

    static Code128PatternMatch? MatchOrdinaryAt(BitRow row, int position)
    {
        if (position < 0 || position >= row.Length || !row.ReadBitUnchecked(position))
        {
            return null;
        }

        int[] observed = RunLength.ExtractForward(row, position, Code128Patterns.OrdinaryElementCount);
        if (observed.Length != Code128Patterns.OrdinaryElementCount)
        {
            return null;
        }

        int bestCodeword = -1;
        double bestVariance = MaximumAverageVariance;
        int codeword = 0;
        while (codeword <= Code128Patterns.Function1Codeword)
        {
            int[] expected = Code128Patterns.GetPatternWidths(codeword);
            double variance = PatternMatcher.CalculateNormalizedVariance(observed, expected, MaximumIndividualVariance);
            if (variance < bestVariance)
            {
                bestVariance = variance;
                bestCodeword = codeword;
            }

            codeword = codeword + 1;
        }

        if (bestCodeword < 0)
        {
            return null;
        }

        int width = PatternMatcher.SumPositiveRuns(observed);
        Code128PatternMatch match = new Code128PatternMatch();
        match.Codeword = bestCodeword;
        match.StartPosition = position;
        match.EndPosition = CheckedMath.Add(position, width);
        match.ModuleSize = width / (double)Code128Patterns.OrdinaryModuleCount;
        match.Variance = bestVariance;
        return match;
    }

    static Code128PatternMatch? MatchStopAt(BitRow row, int position)
    {
        if (position < 0 || position >= row.Length || !row.ReadBitUnchecked(position))
        {
            return null;
        }

        int[] observed = RunLength.ExtractForward(row, position, Code128Patterns.StopElementCount);
        if (observed.Length != Code128Patterns.StopElementCount)
        {
            return null;
        }

        int[] expected = Code128Patterns.GetPatternWidths(Code128Patterns.StopCodeword);
        double variance = PatternMatcher.CalculateNormalizedVariance(observed, expected, MaximumIndividualVariance);
        if (variance >= MaximumAverageVariance)
        {
            return null;
        }

        int width = PatternMatcher.SumPositiveRuns(observed);
        int end = CheckedMath.Add(position, width);
        double moduleSize = width / (double)Code128Patterns.StopModuleCount;
        int quietZone = CountLightAfter(row, end);
        if ((double)quietZone < Code128Layout.StandardQuietZoneModules * moduleSize)
        {
            return null;
        }

        Code128PatternMatch match = new Code128PatternMatch();
        match.Codeword = Code128Patterns.StopCodeword;
        match.StartPosition = position;
        match.EndPosition = end;
        match.ModuleSize = moduleSize;
        match.Variance = variance;
        return match;
    }

    static int CountLightBefore(BitRow row, int position)
    {
        int count = 0;
        int index = position - 1;
        while (index >= 0 && !row.ReadBitUnchecked(index))
        {
            count = count + 1;
            index = index - 1;
        }

        return count;
    }

    static int CountLightAfter(BitRow row, int position)
    {
        int count = 0;
        int index = position;
        while (index < row.Length && !row.ReadBitUnchecked(index))
        {
            count = count + 1;
            index = index + 1;
        }

        return count;
    }

    static bool HasValidChecksum(int[] codewords, int codewordCount)
    {
        int checkIndex = codewordCount - 2;
        if (checkIndex < 2)
        {
            return false;
        }

        int sumModulo103 = codewords[0] % 103;
        int index = 1;
        while (index < checkIndex)
        {
            sumModulo103 = (sumModulo103 + ((codewords[index] * index) % 103)) % 103;
            index = index + 1;
        }

        return sumModulo103 == codewords[checkIndex];
    }

    static Code128SemanticData? DecodeSemanticValues(
        int[] codewords,
        int codewordCount,
        int startCodeword)
    {
        Code128CodeSet activeCodeSet = Code128Patterns.GetCodeSetForStart(startCodeword);
        bool shiftPending = false;
        bool upperShiftLatched = false;
        bool function4ImmediatelyPending = false;
        bool singleUpperShiftPending = false;
        byte[] byteBuffer = new byte[MaximumDecodedValueCount];
        int[] tokenBuffer = new int[MaximumDecodedValueCount];
        int byteCount = 0;
        int tokenCount = 0;
        int checkIndex = codewordCount - 2;
        int index = 1;
        while (index < checkIndex)
        {
            int codeword = codewords[index];
            Code128CodeSet interpretationCodeSet = activeCodeSet;
            if (shiftPending)
            {
                interpretationCodeSet = Code128CodeSets.GetShiftedCodeSet(activeCodeSet);
            }

            Code128CodewordRole role = Code128Patterns.GetCodewordRole(codeword, interpretationCodeSet);
            if (shiftPending && role != Code128CodewordRole.Data)
            {
                return null;
            }

            if (role == Code128CodewordRole.Data)
            {
                if (interpretationCodeSet == Code128CodeSet.C)
                {
                    if (function4ImmediatelyPending || singleUpperShiftPending)
                    {
                        return null;
                    }

                    int[] pair = Code128CodeSets.DecodeDigitPair(codeword);
                    byteBuffer[byteCount] = (byte)pair[0];
                    tokenBuffer[tokenCount] = pair[0];
                    byteCount = byteCount + 1;
                    tokenCount = tokenCount + 1;
                    byteBuffer[byteCount] = (byte)pair[1];
                    tokenBuffer[tokenCount] = pair[1];
                    byteCount = byteCount + 1;
                    tokenCount = tokenCount + 1;
                }
                else
                {
                    int byteValue = Code128CodeSets.DecodeByte(codeword, interpretationCodeSet, false);
                    bool invertUpperShift = function4ImmediatelyPending || singleUpperShiftPending;
                    bool useUpperShift = upperShiftLatched;
                    if (invertUpperShift)
                    {
                        useUpperShift = !useUpperShift;
                    }

                    if (useUpperShift)
                    {
                        byteValue = byteValue + 128;
                    }

                    function4ImmediatelyPending = false;
                    singleUpperShiftPending = false;
                    byteBuffer[byteCount] = (byte)byteValue;
                    tokenBuffer[tokenCount] = byteValue;
                    byteCount = byteCount + 1;
                    tokenCount = tokenCount + 1;
                }

                shiftPending = false;
                index = index + 1;
                continue;
            }

            if (shiftPending)
            {
                return null;
            }

            if (role == Code128CodewordRole.Function4)
            {
                if (singleUpperShiftPending)
                {
                    return null;
                }

                if (function4ImmediatelyPending)
                {
                    upperShiftLatched = !upperShiftLatched;
                    function4ImmediatelyPending = false;
                }
                else
                {
                    function4ImmediatelyPending = true;
                }

                tokenBuffer[tokenCount] = Code128Planner.Function4Token;
                tokenCount = tokenCount + 1;
                index = index + 1;
                continue;
            }

            if (function4ImmediatelyPending)
            {
                function4ImmediatelyPending = false;
                singleUpperShiftPending = true;
            }

            switch (role)
            {
                case Code128CodewordRole.Function1:
                    tokenBuffer[tokenCount] = Code128Planner.Function1Token;
                    tokenCount = tokenCount + 1;
                    break;
                case Code128CodewordRole.Function2:
                    tokenBuffer[tokenCount] = Code128Planner.Function2Token;
                    tokenCount = tokenCount + 1;
                    break;
                case Code128CodewordRole.Function3:
                    tokenBuffer[tokenCount] = Code128Planner.Function3Token;
                    tokenCount = tokenCount + 1;
                    break;
                case Code128CodewordRole.Shift:
                    if (activeCodeSet == Code128CodeSet.C)
                    {
                        return null;
                    }

                    shiftPending = true;
                    break;
                case Code128CodewordRole.CodeA:
                    activeCodeSet = Code128CodeSet.A;
                    break;
                case Code128CodewordRole.CodeB:
                    activeCodeSet = Code128CodeSet.B;
                    break;
                case Code128CodewordRole.CodeC:
                    activeCodeSet = Code128CodeSet.C;
                    break;
                default:
                    return null;
            }

            index = index + 1;
        }

        if (shiftPending || function4ImmediatelyPending || singleUpperShiftPending)
        {
            return null;
        }

        Code128SemanticData semantic = new Code128SemanticData();
        semantic.Bytes = CopyBytes(byteBuffer, byteCount);
        semantic.Tokens = CopyIntegers(tokenBuffer, tokenCount);
        return semantic;
    }

    static Code128DecodedData CreateDecodedData(
        int[] codewordBuffer,
        int codewordCount,
        Code128SemanticData semantic,
        Code128PatternMatch start,
        Code128PatternMatch stop,
        double averageVariance)
    {
        int[] codewords = CopyIntegers(codewordBuffer, codewordCount);
        Code128DecodedData decoded = new Code128DecodedData();
        decoded.Bytes = semantic.Bytes;
        decoded.Tokens = semantic.Tokens;
        decoded.Codewords = codewords;
        decoded.StartCodeSet = Code128Patterns.GetCodeSetForStart(start.Codeword);
        decoded.ChecksumValue = codewords[codewords.Length - 2];
        decoded.StartPosition = start.StartPosition;
        decoded.EndPosition = stop.EndPosition;
        decoded.ModuleSize = (start.ModuleSize + stop.ModuleSize) / 2.0;
        decoded.AverageVariance = averageVariance;
        return decoded;
    }

    static byte[] CopyBytes(byte[] source, int count)
    {
        byte[] copy = new byte[count];
        int index = 0;
        while (index < count)
        {
            copy[index] = source[index];
            index = index + 1;
        }

        return copy;
    }

    static int[] CopyIntegers(int[] source, int count)
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

    static BitRow CreateReversedRow(BitRow row)
    {
        BitRow reversed = new BitRow(row.Length);
        int index = 0;
        while (index < row.Length)
        {
            if (row.ReadBitUnchecked(row.Length - index - 1))
            {
                reversed.WriteBitUnchecked(index, true);
            }

            index = index + 1;
        }

        return reversed;
    }
}
