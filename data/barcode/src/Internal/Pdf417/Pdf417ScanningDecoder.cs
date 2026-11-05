namespace barcode;

internal class Pdf417ScanlineCodeword
{
    internal int Value;
    internal int Pattern;
    internal int Bucket;
    internal int ClusterIndex;
    internal int StartPosition;
    internal int EndPosition;
    internal int[] ModuleBitCounts = new int[0];
    internal double ModuleSize;
    internal double Variance;
}

internal class Pdf417GuardMatch
{
    internal int StartPosition;
    internal int EndPosition;
    internal double ModuleSize;
    internal double Variance;
}

internal class Pdf417ScanlineRow
{
    internal Pdf417Variant Variant;
    internal int LeftIndicator;
    internal int RightIndicator = -1;
    internal int[] DataCodewords = new int[0];
    internal int[] Codewords = new int[0];
    internal int[] Patterns = new int[0];
    internal int[] Buckets = new int[0];
    internal int ClusterIndex;
    internal bool IsReversed;
    internal int StartPosition;
    internal int EndPosition;
    internal double ModuleSize;
    internal double AverageVariance;
}

internal static class Pdf417ScanningDecoder
{
    internal const int MaximumRowCodewordCount = 32;
    internal const double MaximumAverageVariance = 0.25;
    internal const double MaximumIndividualVariance = 0.8;
    internal const double MaximumModuleSizeRelativeDifference = 0.5;

    internal static Pdf417ScanlineRow DecodeRow(BitRow row)
    {
        Pdf417ScanlineRow? decoded = TryDecodeRow(row);
        if (decoded == null)
        {
            throw new BarcodeDecodingException("The scanline does not contain a valid PDF417 row.");
        }

        return decoded;
    }

    internal static Pdf417ScanlineRow? TryDecodeRow(BitRow row)
    {
        if (row == null)
        {
            throw new BarcodeValidationException("PDF417 scanline must not be null.");
        }

        Pdf417ScanlineRow? forward = TryDecodeForward(row);
        if (forward != null)
        {
            forward.IsReversed = false;
            return forward;
        }

        BitRow reversedRow = CreateReversedRow(row);
        Pdf417ScanlineRow? reversed = TryDecodeForward(reversedRow);
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

    internal static Pdf417ScanlineCodeword? RecoverCodeword(
        int[] observedRuns,
        int expectedClusterIndex)
    {
        if (observedRuns == null
            || observedRuns.Length != Pdf417CodewordTable.RunsPerCodeword)
        {
            throw new BarcodeValidationException("PDF417 codeword sampling requires eight observed runs.");
        }

        if (expectedClusterIndex < -1
            || expectedClusterIndex >= Pdf417CodewordTable.ClusterCount)
        {
            throw new BarcodeValidationException("Expected PDF417 cluster must be negative for unknown or from zero through two.");
        }

        int observedWidth = PatternMatcher.SumPositiveRuns(observedRuns);
        int[] sampled = SampleModuleBitCounts(observedRuns);
        int sampledIndex = 0;
        while (sampledIndex < sampled.Length)
        {
            if (sampled[sampledIndex] == 0)
            {
                return null;
            }

            sampledIndex = sampledIndex + 1;
        }

        int bucket = CalculateBucket(sampled);
        if (bucket != 0 && bucket != 3 && bucket != 6)
        {
            return null;
        }

        int clusterIndex = bucket / 3;
        if (expectedClusterIndex >= 0 && clusterIndex != expectedClusterIndex)
        {
            return null;
        }

        int sampledPattern = CreatePatternFromModuleBitCounts(sampled);
        int sampledValue = Pdf417CodewordTable.FindCodeword(
            clusterIndex,
            sampledPattern);
        if (sampledValue >= 0)
        {
            int[] expectedRuns = Pdf417CodewordTable.GetRunWidths(sampledPattern);
            double variance = PatternMatcher.CalculateNormalizedVariance(
                observedRuns,
                expectedRuns,
                MaximumIndividualVariance);
            if (variance <= MaximumAverageVariance)
            {
                return CreateCodewordMatch(
                    sampledValue,
                    sampledPattern,
                    bucket,
                    clusterIndex,
                    sampled,
                    observedWidth,
                    variance);
            }
        }

        int bestValue = -1;
        int bestPattern = 0;
        double bestVariance = MaximumAverageVariance;
        int codeword = 0;
        while (codeword < Pdf417CodewordTable.CodewordCount)
        {
            int pattern = Pdf417CodewordTable.GetPattern(clusterIndex, codeword);
            int[] expectedRuns = Pdf417CodewordTable.GetRunWidths(pattern);
            double variance = PatternMatcher.CalculateNormalizedVariance(
                observedRuns,
                expectedRuns,
                MaximumIndividualVariance);
            if (variance < bestVariance)
            {
                bestValue = codeword;
                bestPattern = pattern;
                bestVariance = variance;
            }

            codeword = codeword + 1;
        }

        if (bestValue < 0)
        {
            return null;
        }

        int[] recoveredCounts = Pdf417CodewordTable.GetRunWidths(bestPattern);
        return CreateCodewordMatch(
            bestValue,
            bestPattern,
            bucket,
            clusterIndex,
            recoveredCounts,
            observedWidth,
            bestVariance);
    }

    internal static int[] SampleModuleBitCounts(int[] observedRuns)
    {
        if (observedRuns == null
            || observedRuns.Length != Pdf417CodewordTable.RunsPerCodeword)
        {
            throw new BarcodeValidationException("PDF417 module sampling requires eight observed runs.");
        }

        int observedWidth = PatternMatcher.SumPositiveRuns(observedRuns);
        int[] sampled = new int[Pdf417CodewordTable.RunsPerCodeword];
        int runIndex = 0;
        int previousWidth = 0;
        int module = 0;
        while (module < Pdf417CodewordTable.ModulesPerCodeword)
        {
            double samplePosition = (observedWidth / 34.0)
                + ((module * observedWidth)
                    / (double)Pdf417CodewordTable.ModulesPerCodeword);
            while (runIndex < observedRuns.Length - 1
                && previousWidth + observedRuns[runIndex] <= samplePosition)
            {
                previousWidth = previousWidth + observedRuns[runIndex];
                runIndex = runIndex + 1;
            }

            sampled[runIndex] = sampled[runIndex] + 1;
            module = module + 1;
        }

        return sampled;
    }

    internal static int CalculateBucket(int[] moduleBitCounts)
    {
        if (moduleBitCounts == null
            || moduleBitCounts.Length != Pdf417CodewordTable.RunsPerCodeword)
        {
            throw new BarcodeValidationException("PDF417 bucket calculation requires eight module runs.");
        }

        int total = 0;
        int index = 0;
        while (index < moduleBitCounts.Length)
        {
            if (moduleBitCounts[index] <= 0)
            {
                throw new BarcodeValidationException("PDF417 bucket module runs must be positive.");
            }

            total = CheckedMath.Add(total, moduleBitCounts[index]);
            index = index + 1;
        }

        if (total != Pdf417CodewordTable.ModulesPerCodeword)
        {
            throw new BarcodeValidationException("PDF417 bucket module runs must total seventeen modules.");
        }

        return (moduleBitCounts[0]
            - moduleBitCounts[2]
            + moduleBitCounts[4]
            - moduleBitCounts[6]
            + 9) % 9;
    }

    static Pdf417ScanlineRow? TryDecodeForward(BitRow row)
    {
        int position = 0;
        while (position < row.Length)
        {
            if (row.ReadBitUnchecked(position)
                && (position == 0 || !row.ReadBitUnchecked(position - 1)))
            {
                Pdf417GuardMatch? start = MatchStartAt(row, position);
                if (start != null)
                {
                    Pdf417ScanlineRow? decoded = DecodeFromStart(row, start);
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

    static Pdf417GuardMatch? MatchStartAt(BitRow row, int position)
    {
        int[] expected = CreateRunWidths(
            Pdf417CodewordTable.StartPatternBits,
            Pdf417CodewordTable.StartPatternModuleCount,
            Pdf417CodewordTable.RunsPerCodeword);
        int[] observed = RunLength.ExtractForward(
            row,
            position,
            expected.Length);
        if (observed.Length != expected.Length)
        {
            return null;
        }

        double variance = PatternMatcher.CalculateNormalizedVariance(
            observed,
            expected,
            MaximumIndividualVariance);
        if (variance > MaximumAverageVariance)
        {
            return null;
        }

        int width = PatternMatcher.SumPositiveRuns(observed);
        double moduleSize = width
            / (double)Pdf417CodewordTable.StartPatternModuleCount;
        int quietZone = CountLightBefore(row, position);
        if (quietZone < Pdf417Layout.StandardQuietZoneModules * moduleSize)
        {
            return null;
        }

        return CreateGuardMatch(position, width, moduleSize, variance);
    }

    static Pdf417ScanlineRow? DecodeFromStart(
        BitRow row,
        Pdf417GuardMatch start)
    {
        int[] values = new int[MaximumRowCodewordCount];
        int[] patterns = new int[MaximumRowCodewordCount];
        int[] buckets = new int[MaximumRowCodewordCount];
        int count = 0;
        int clusterIndex = -1;
        int position = start.EndPosition;
        double moduleSizeTotal = start.ModuleSize;
        double varianceTotal = start.Variance;
        int measuredPatternCount = 1;
        while (position < row.Length)
        {
            Pdf417GuardMatch? stop = MatchStandardStopAt(
                row,
                position,
                moduleSizeTotal / measuredPatternCount);
            if (stop != null && count >= 3)
            {
                moduleSizeTotal = moduleSizeTotal + stop.ModuleSize;
                varianceTotal = varianceTotal + stop.Variance;
                measuredPatternCount = measuredPatternCount + 1;
                return CreateDecodedRow(
                    Pdf417Variant.Standard,
                    values,
                    patterns,
                    buckets,
                    count,
                    clusterIndex,
                    start.StartPosition,
                    stop.EndPosition,
                    moduleSizeTotal / measuredPatternCount,
                    varianceTotal / measuredPatternCount);
            }

            if (count >= MaximumRowCodewordCount)
            {
                return null;
            }

            int[] observed = RunLength.ExtractForward(
                row,
                position,
                Pdf417CodewordTable.RunsPerCodeword);
            Pdf417ScanlineCodeword? codeword = null;
            if (observed.Length == Pdf417CodewordTable.RunsPerCodeword)
            {
                codeword = RecoverCodeword(observed, clusterIndex);
            }

            if (codeword != null
                && IsCompatibleModuleSize(
                    moduleSizeTotal / measuredPatternCount,
                    codeword.ModuleSize))
            {
                codeword.StartPosition = position;
                codeword.EndPosition = CheckedMath.Add(
                    position,
                    PatternMatcher.SumPositiveRuns(observed));
                if (clusterIndex < 0)
                {
                    clusterIndex = codeword.ClusterIndex;
                }

                values[count] = codeword.Value;
                patterns[count] = codeword.Pattern;
                buckets[count] = codeword.Bucket;
                count = count + 1;
                position = codeword.EndPosition;
                moduleSizeTotal = moduleSizeTotal + codeword.ModuleSize;
                varianceTotal = varianceTotal + codeword.Variance;
                measuredPatternCount = measuredPatternCount + 1;
                continue;
            }

            Pdf417GuardMatch? compactStop = MatchCompactStopAt(
                row,
                position,
                moduleSizeTotal / measuredPatternCount);
            if (compactStop != null && count >= 2)
            {
                moduleSizeTotal = moduleSizeTotal + compactStop.ModuleSize;
                varianceTotal = varianceTotal + compactStop.Variance;
                measuredPatternCount = measuredPatternCount + 1;
                return CreateDecodedRow(
                    Pdf417Variant.Compact,
                    values,
                    patterns,
                    buckets,
                    count,
                    clusterIndex,
                    start.StartPosition,
                    compactStop.EndPosition,
                    moduleSizeTotal / measuredPatternCount,
                    varianceTotal / measuredPatternCount);
            }

            return null;
        }

        return null;
    }

    static Pdf417GuardMatch? MatchStandardStopAt(
        BitRow row,
        int position,
        double referenceModuleSize)
    {
        if (position < 0
            || position >= row.Length
            || !row.ReadBitUnchecked(position))
        {
            return null;
        }

        int[] expected = CreateRunWidths(
            Pdf417CodewordTable.StopPatternBits,
            Pdf417CodewordTable.StopPatternModuleCount,
            9);
        int[] observed = RunLength.ExtractForward(row, position, expected.Length);
        if (observed.Length != expected.Length)
        {
            return null;
        }

        double variance = PatternMatcher.CalculateNormalizedVariance(
            observed,
            expected,
            MaximumIndividualVariance);
        if (variance > MaximumAverageVariance)
        {
            return null;
        }

        int width = PatternMatcher.SumPositiveRuns(observed);
        double moduleSize = width
            / (double)Pdf417CodewordTable.StopPatternModuleCount;
        if (!IsCompatibleModuleSize(referenceModuleSize, moduleSize))
        {
            return null;
        }

        int end = CheckedMath.Add(position, width);
        int quietZone = CountLightAfter(row, end);
        if (quietZone < Pdf417Layout.StandardQuietZoneModules * moduleSize)
        {
            return null;
        }

        return CreateGuardMatch(position, width, moduleSize, variance);
    }

    static Pdf417GuardMatch? MatchCompactStopAt(
        BitRow row,
        int position,
        double referenceModuleSize)
    {
        if (position < 0
            || position >= row.Length
            || !row.ReadBitUnchecked(position))
        {
            return null;
        }

        int darkWidth = CountDarkAfter(row, position);
        double relativeDifference = global::System.Math.Abs(
            darkWidth - referenceModuleSize) / referenceModuleSize;
        if (relativeDifference > MaximumModuleSizeRelativeDifference)
        {
            return null;
        }

        int end = CheckedMath.Add(position, darkWidth);
        int quietZone = CountLightAfter(row, end);
        if (quietZone < Pdf417Layout.StandardQuietZoneModules * referenceModuleSize)
        {
            return null;
        }

        return CreateGuardMatch(
            position,
            darkWidth,
            darkWidth,
            relativeDifference);
    }

    static Pdf417ScanlineRow CreateDecodedRow(
        Pdf417Variant variant,
        int[] values,
        int[] patterns,
        int[] buckets,
        int count,
        int clusterIndex,
        int startPosition,
        int endPosition,
        double moduleSize,
        double averageVariance)
    {
        int dataCount = variant == Pdf417Variant.Standard
            ? count - 2
            : count - 1;
        int[] data = new int[dataCount];
        int index = 0;
        while (index < dataCount)
        {
            data[index] = values[index + 1];
            index = index + 1;
        }

        Pdf417ScanlineRow decoded = new Pdf417ScanlineRow();
        decoded.Variant = variant;
        decoded.LeftIndicator = values[0];
        if (variant == Pdf417Variant.Standard)
        {
            decoded.RightIndicator = values[count - 1];
        }

        decoded.DataCodewords = data;
        decoded.Codewords = CopyIntegerPrefix(values, count);
        decoded.Patterns = CopyIntegerPrefix(patterns, count);
        decoded.Buckets = CopyIntegerPrefix(buckets, count);
        decoded.ClusterIndex = clusterIndex;
        decoded.StartPosition = startPosition;
        decoded.EndPosition = endPosition;
        decoded.ModuleSize = moduleSize;
        decoded.AverageVariance = averageVariance;
        return decoded;
    }

    static Pdf417ScanlineCodeword CreateCodewordMatch(
        int value,
        int pattern,
        int bucket,
        int clusterIndex,
        int[] moduleBitCounts,
        int observedWidth,
        double variance)
    {
        Pdf417ScanlineCodeword match = new Pdf417ScanlineCodeword();
        match.Value = value;
        match.Pattern = pattern;
        match.Bucket = bucket;
        match.ClusterIndex = clusterIndex;
        match.ModuleBitCounts = CopyIntegerPrefix(
            moduleBitCounts,
            moduleBitCounts.Length);
        match.ModuleSize = observedWidth
            / (double)Pdf417CodewordTable.ModulesPerCodeword;
        match.Variance = variance;
        return match;
    }

    static int CreatePatternFromModuleBitCounts(int[] moduleBitCounts)
    {
        int pattern = 0;
        int runIndex = 0;
        while (runIndex < moduleBitCounts.Length)
        {
            int remaining = moduleBitCounts[runIndex];
            int bit = runIndex % 2 == 0 ? 1 : 0;
            while (remaining > 0)
            {
                pattern = (pattern << 1) | bit;
                remaining = remaining - 1;
            }

            runIndex = runIndex + 1;
        }

        return pattern;
    }

    static int[] CreateRunWidths(int pattern, int moduleCount, int runCount)
    {
        int[] widths = new int[runCount];
        int currentBit = 1;
        int runIndex = 0;
        int bitIndex = moduleCount - 1;
        while (bitIndex >= 0)
        {
            int bit = (pattern >> bitIndex) & 1;
            if (bit == currentBit)
            {
                widths[runIndex] = widths[runIndex] + 1;
            }
            else
            {
                runIndex = runIndex + 1;
                if (runIndex >= runCount)
                {
                    throw new BarcodeValidationException("PDF417 guard pattern contains too many runs.");
                }

                currentBit = bit;
                widths[runIndex] = 1;
            }

            bitIndex = bitIndex - 1;
        }

        if (runIndex != runCount - 1)
        {
            throw new BarcodeValidationException("PDF417 guard pattern contains an unexpected run count.");
        }

        return widths;
    }

    static Pdf417GuardMatch CreateGuardMatch(
        int position,
        int width,
        double moduleSize,
        double variance)
    {
        Pdf417GuardMatch match = new Pdf417GuardMatch();
        match.StartPosition = position;
        match.EndPosition = CheckedMath.Add(position, width);
        match.ModuleSize = moduleSize;
        match.Variance = variance;
        return match;
    }

    static bool IsCompatibleModuleSize(double reference, double candidate)
    {
        if (reference <= 0.0 || candidate <= 0.0)
        {
            return false;
        }

        return global::System.Math.Abs(reference - candidate) / reference
            <= MaximumModuleSizeRelativeDifference;
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

    static int CountDarkAfter(BitRow row, int position)
    {
        int count = 0;
        int index = position;
        while (index < row.Length && row.ReadBitUnchecked(index))
        {
            count = count + 1;
            index = index + 1;
        }

        return count;
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
}
