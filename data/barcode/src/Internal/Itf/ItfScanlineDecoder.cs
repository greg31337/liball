namespace barcode;

internal class ItfScanlineDecodedData
{
    internal BarcodeDecodeResult Result = new BarcodeDecodeResult();
    internal BarcodeFormat Format;
    internal string Text = "";
    internal string DataText = "";
    internal string EncodedText = "";
    internal int[] Digits = new int[0];
    internal int[] BarDigitPatternBits = new int[0];
    internal int[] SpaceDigitPatternBits = new int[0];
    internal bool HasChecksum;
    internal int ChecksumValue;
    internal string SymbologyIdentifier = "";
    internal bool IsReversed;
    internal bool LeadingBearerBoundaryObserved;
    internal bool TrailingBearerBoundaryObserved;
    internal int StartPosition;
    internal int EndPosition;
    internal int PairCount;
    internal double NarrowWidth;
    internal double WideToNarrowRatio;
    internal double AverageVariance;
}

internal class ItfScanlinePairMatch
{
    internal int BarDigit;
    internal int SpaceDigit;
    internal int BarPatternBits;
    internal int SpacePatternBits;
    internal int EndPosition;
    internal double NarrowWidth;
    internal double WideWidth;
    internal double Variance;
}

internal class ItfScanlineStopMatch
{
    internal int EndPosition;
    internal double NarrowWidth;
    internal double WideWidth;
    internal double Variance;
    internal int TrailingQuietWidth;
    internal bool TrailingBearerBoundaryObserved;
}

internal class ItfScanlineState
{
    internal global::System.Text.StringBuilder Text = new global::System.Text.StringBuilder();
    internal global::System.Collections.Generic.List<int> Digits =
        new global::System.Collections.Generic.List<int>();
    internal global::System.Collections.Generic.List<int> BarPatterns =
        new global::System.Collections.Generic.List<int>();
    internal global::System.Collections.Generic.List<int> SpacePatterns =
        new global::System.Collections.Generic.List<int>();
    internal int StartPosition;
    internal int Position;
    internal int PairCount;
    internal double TotalNarrowWidth;
    internal double TotalWideWidth;
    internal double TotalVariance;
    internal int MeasurementCount;
    internal int WideMeasurementCount;
    internal bool LeadingBearerBoundaryObserved;
}

internal static class ItfScanlineDecoder
{
    internal const double MinimumObservedWideRatio = 1.5;
    internal const double MaximumObservedWideRatio = 3.5;
    internal const double MaximumElementRelativeVariance = 0.45;
    internal const double MaximumAverageVariance = 0.20;
    internal const double ScaleRelativeTolerance = 0.50;
    internal const int MaximumPairCount = 4096;

    internal static ItfScanlineDecodedData DecodeRow(
        BitRow row,
        BarcodeFormat format,
        ItfOptions? options)
    {
        ItfScanlineDecodedData? decoded = TryDecodeRow(row, format, options);
        if (decoded == null)
        {
            throw new BarcodeDecodingException("The scanline does not contain a valid ITF symbol.");
        }

        return decoded;
    }

    internal static ItfScanlineDecodedData? TryDecodeRow(
        BitRow row,
        BarcodeFormat format,
        ItfOptions? options)
    {
        if (row == null)
        {
            throw new BarcodeValidationException("ITF scanline must not be null.");
        }

        ItfOptions settings = options == null ? new ItfOptions() : options;
        ValidateOptions(format, settings);
        ItfScanlineDecodedData? forward = TryDecodeForward(row, format, settings);
        if (forward != null)
        {
            forward.IsReversed = false;
            return forward;
        }

        BitRow reversedRow = CreateReversedRow(row);
        ItfScanlineDecodedData? reversed = TryDecodeForward(reversedRow, format, settings);
        if (reversed == null)
        {
            return null;
        }

        int originalStart = row.Length - reversed.EndPosition;
        int originalEnd = row.Length - reversed.StartPosition;
        reversed.StartPosition = originalStart;
        reversed.EndPosition = originalEnd;
        reversed.IsReversed = true;
        reversed.Result.Rotation = BarcodeRotation.Clockwise180;
        reversed.Result.OrientationDegrees = 180.0;
        return reversed;
    }

    static void ValidateOptions(BarcodeFormat format, ItfOptions options)
    {
        if (format != BarcodeFormat.Itf && format != BarcodeFormat.Itf14)
        {
            throw new BarcodeValidationException("ITF scanline format must be ITF or ITF-14.");
        }

        if (options.ChecksumMode != BarcodeChecksumMode.None &&
            options.ChecksumMode != BarcodeChecksumMode.Calculate &&
            options.ChecksumMode != BarcodeChecksumMode.Require &&
            options.ChecksumMode != BarcodeChecksumMode.VerifyKeep &&
            options.ChecksumMode != BarcodeChecksumMode.VerifyStrip)
        {
            throw new BarcodeValidationException("ITF checksum mode is invalid.");
        }

        if (format == BarcodeFormat.Itf14 && options.ChecksumMode == BarcodeChecksumMode.VerifyStrip)
        {
            throw new BarcodeValidationException("ITF-14 must retain its mandatory GS1 check digit.");
        }

        double ratio = options.WideToNarrowRatio;
        double minimumRatio = format == BarcodeFormat.Itf14
            ? Itf14Profile.MinimumWideToNarrowRatio
            : 2.0;
        if (double.IsNaN(ratio) || double.IsInfinity(ratio) ||
            (ratio != 0.0 && (ratio < minimumRatio || ratio > 3.0)))
        {
            throw new BarcodeValidationException("ITF wide-to-narrow ratio is outside the selected profile range.");
        }

        if (options.BearerBarThicknessModules < 0)
        {
            throw new BarcodeValidationException("ITF bearer-bar thickness must not be negative.");
        }

        ValidateAllowedLengths(format, options.AllowedLengths);
    }

    static void ValidateAllowedLengths(BarcodeFormat format, int[]? allowedLengths)
    {
        if (allowedLengths == null)
        {
            return;
        }

        if (allowedLengths.Length == 0)
        {
            throw new BarcodeValidationException("ITF allowed lengths must not be empty when supplied.");
        }

        int index = 0;
        while (index < allowedLengths.Length)
        {
            int length = allowedLengths[index];
            if (format == BarcodeFormat.Itf14)
            {
                if (length != Itf14Profile.CompleteDigitCount)
                {
                    throw new BarcodeValidationException("ITF-14 allowed lengths must contain only the complete length 14.");
                }
            }
            else if (length <= 0 || length % 2 != 0)
            {
                throw new BarcodeValidationException("ITF allowed lengths must contain positive even values.");
            }

            index = index + 1;
        }
    }

    static ItfScanlineDecodedData? TryDecodeForward(
        BitRow row,
        BarcodeFormat format,
        ItfOptions options)
    {
        int position = 0;
        while (position < row.Length)
        {
            bool dark = row.ReadBitUnchecked(position);
            bool atRunStart = position == 0 || row.ReadBitUnchecked(position - 1) != dark;
            if (dark && atRunStart)
            {
                int[] startRuns = RunLength.ExtractForward(
                    row,
                    position,
                    ItfPatterns.StartElementCount);
                double startNarrowWidth = MatchStart(startRuns);
                if (startNarrowWidth > 0.0)
                {
                    int leadingQuietWidth = CountLightBefore(row, position);
                    if (leadingQuietWidth >= ItfLayout.StandardQuietZoneModules * startNarrowWidth)
                    {
                        int startEnd = CheckedMath.Add(position, PatternMatcher.SumPositiveRuns(startRuns));
                        int quietStart = position - leadingQuietWidth;
                        bool leadingBoundary = quietStart > 0 && row.ReadBitUnchecked(quietStart - 1);
                        ItfScanlineDecodedData? decoded = DecodeFromStart(
                            row,
                            position,
                            startEnd,
                            startNarrowWidth,
                            CalculateUniformVariance(startRuns, startNarrowWidth),
                            leadingBoundary,
                            format,
                            options);
                        if (decoded != null)
                        {
                            return decoded;
                        }
                    }
                }
            }

            position = position + 1;
        }

        return null;
    }

    static double MatchStart(int[] runs)
    {
        if (runs.Length != ItfPatterns.StartElementCount)
        {
            return 0.0;
        }

        double total = 0.0;
        int index = 0;
        while (index < runs.Length)
        {
            total = total + runs[index];
            index = index + 1;
        }

        double narrowWidth = total / runs.Length;
        index = 0;
        while (index < runs.Length)
        {
            double relative = global::System.Math.Abs(runs[index] - narrowWidth) / narrowWidth;
            if (relative > MaximumElementRelativeVariance)
            {
                return 0.0;
            }

            index = index + 1;
        }

        return narrowWidth;
    }

    static ItfScanlineDecodedData? DecodeFromStart(
        BitRow row,
        int startPosition,
        int startEnd,
        double startNarrowWidth,
        double startVariance,
        bool leadingBoundary,
        BarcodeFormat format,
        ItfOptions options)
    {
        ItfScanlineState state = new ItfScanlineState();
        state.StartPosition = startPosition;
        state.Position = startEnd;
        state.TotalNarrowWidth = startNarrowWidth;
        state.TotalVariance = startVariance;
        state.MeasurementCount = 1;
        state.LeadingBearerBoundaryObserved = leadingBoundary;

        while (state.Position < row.Length)
        {
            ItfScanlineStopMatch? stop = MatchStopAt(row, state.Position, CurrentNarrowWidth(state));
            if (stop != null && state.PairCount > 0)
            {
                double averageVariance = (state.TotalVariance + stop.Variance) /
                    (state.MeasurementCount + 1);
                if (averageVariance > MaximumAverageVariance)
                {
                    return null;
                }

                return CreateDecodedData(state, stop, format, options, averageVariance);
            }

            if (state.PairCount >= MaximumPairCount)
            {
                return null;
            }

            ItfScanlinePairMatch? pair = MatchPairAt(row, state.Position);
            if (pair == null || !ScalesMatch(CurrentNarrowWidth(state), pair.NarrowWidth))
            {
                return null;
            }

            state.Text.Append((char)('0' + pair.BarDigit));
            state.Text.Append((char)('0' + pair.SpaceDigit));
            state.Digits.Add(pair.BarDigit);
            state.Digits.Add(pair.SpaceDigit);
            state.BarPatterns.Add(pair.BarPatternBits);
            state.SpacePatterns.Add(pair.SpacePatternBits);
            state.Position = pair.EndPosition;
            state.PairCount = state.PairCount + 1;
            state.TotalNarrowWidth = state.TotalNarrowWidth + pair.NarrowWidth;
            state.TotalWideWidth = state.TotalWideWidth + pair.WideWidth;
            state.TotalVariance = state.TotalVariance + pair.Variance;
            state.MeasurementCount = state.MeasurementCount + 1;
            state.WideMeasurementCount = state.WideMeasurementCount + 1;
        }

        return null;
    }

    static ItfScanlinePairMatch? MatchPairAt(BitRow row, int position)
    {
        if (position < 0 || position >= row.Length || !row.ReadBitUnchecked(position))
        {
            return null;
        }

        int[] observed = RunLength.ExtractForward(row, position, ItfPatterns.PairElementCount);
        if (observed.Length != ItfPatterns.PairElementCount)
        {
            return null;
        }

        int[] barWidths = new int[ItfPatterns.DigitElementCount];
        int[] spaceWidths = new int[ItfPatterns.DigitElementCount];
        int index = 0;
        while (index < ItfPatterns.DigitElementCount)
        {
            barWidths[index] = observed[index * 2];
            spaceWidths[index] = observed[(index * 2) + 1];
            index = index + 1;
        }

        int[] sortedBars = CopyWidths(barWidths);
        int[] sortedSpaces = CopyWidths(spaceWidths);
        SortWidths(sortedBars);
        SortWidths(sortedSpaces);
        if (sortedBars[2] >= sortedBars[3] || sortedSpaces[2] >= sortedSpaces[3])
        {
            return null;
        }

        double barThreshold = (sortedBars[2] + sortedBars[3]) / 2.0;
        double spaceThreshold = (sortedSpaces[2] + sortedSpaces[3]) / 2.0;
        int barPattern = 0;
        int spacePattern = 0;
        double narrowTotal = 0.0;
        double wideTotal = 0.0;
        int narrowCount = 0;
        int wideCount = 0;
        index = 0;
        while (index < ItfPatterns.DigitElementCount)
        {
            if (barWidths[index] > barThreshold)
            {
                barPattern = barPattern | (1 << (ItfPatterns.DigitElementCount - index - 1));
                wideTotal = wideTotal + barWidths[index];
                wideCount = wideCount + 1;
            }
            else
            {
                narrowTotal = narrowTotal + barWidths[index];
                narrowCount = narrowCount + 1;
            }

            if (spaceWidths[index] > spaceThreshold)
            {
                spacePattern = spacePattern | (1 << (ItfPatterns.DigitElementCount - index - 1));
                wideTotal = wideTotal + spaceWidths[index];
                wideCount = wideCount + 1;
            }
            else
            {
                narrowTotal = narrowTotal + spaceWidths[index];
                narrowCount = narrowCount + 1;
            }

            index = index + 1;
        }

        if (narrowCount != 6 || wideCount != 4)
        {
            return null;
        }

        int barDigit = FindDigit(barPattern);
        int spaceDigit = FindDigit(spacePattern);
        if (barDigit < 0 || spaceDigit < 0)
        {
            return null;
        }

        double narrowWidth = narrowTotal / narrowCount;
        double wideWidth = wideTotal / wideCount;
        double ratio = wideWidth / narrowWidth;
        if (ratio < MinimumObservedWideRatio || ratio > MaximumObservedWideRatio)
        {
            return null;
        }

        double variance = CalculateClassifiedVariance(
            observed,
            barThreshold,
            spaceThreshold,
            narrowWidth,
            wideWidth);
        if (variance < 0.0)
        {
            return null;
        }

        ItfScanlinePairMatch match = new ItfScanlinePairMatch();
        match.BarDigit = barDigit;
        match.SpaceDigit = spaceDigit;
        match.BarPatternBits = barPattern;
        match.SpacePatternBits = spacePattern;
        match.EndPosition = CheckedMath.Add(position, PatternMatcher.SumPositiveRuns(observed));
        match.NarrowWidth = narrowWidth;
        match.WideWidth = wideWidth;
        match.Variance = variance;
        return match;
    }

    static double CalculateClassifiedVariance(
        int[] observed,
        double barThreshold,
        double spaceThreshold,
        double narrowWidth,
        double wideWidth)
    {
        double totalDeviation = 0.0;
        double totalWidth = 0.0;
        int index = 0;
        while (index < observed.Length)
        {
            bool wide = index % 2 == 0
                ? observed[index] > barThreshold
                : observed[index] > spaceThreshold;
            double expected = wide ? wideWidth : narrowWidth;
            double deviation = global::System.Math.Abs(observed[index] - expected);
            if (deviation / expected > MaximumElementRelativeVariance)
            {
                return -1.0;
            }

            totalDeviation = totalDeviation + deviation;
            totalWidth = totalWidth + observed[index];
            index = index + 1;
        }

        return totalDeviation / totalWidth;
    }

    static ItfScanlineStopMatch? MatchStopAt(
        BitRow row,
        int position,
        double referenceNarrowWidth)
    {
        if (position < 0 || position >= row.Length || !row.ReadBitUnchecked(position))
        {
            return null;
        }

        int[] observed = RunLength.ExtractForward(row, position, ItfPatterns.StopElementCount);
        if (observed.Length != ItfPatterns.StopElementCount)
        {
            return null;
        }

        double narrowWidth = (observed[1] + observed[2]) / 2.0;
        double wideWidth = observed[0];
        double ratio = wideWidth / narrowWidth;
        if (ratio < MinimumObservedWideRatio || ratio > MaximumObservedWideRatio ||
            !ScalesMatch(referenceNarrowWidth, narrowWidth))
        {
            return null;
        }

        double narrowDeviationOne = global::System.Math.Abs(observed[1] - narrowWidth);
        double narrowDeviationTwo = global::System.Math.Abs(observed[2] - narrowWidth);
        if (narrowDeviationOne / narrowWidth > MaximumElementRelativeVariance ||
            narrowDeviationTwo / narrowWidth > MaximumElementRelativeVariance)
        {
            return null;
        }

        int endPosition = CheckedMath.Add(position, PatternMatcher.SumPositiveRuns(observed));
        int trailingQuietWidth = CountLightAfter(row, endPosition);
        if (trailingQuietWidth < ItfLayout.StandardQuietZoneModules * narrowWidth)
        {
            return null;
        }

        int quietEnd = CheckedMath.Add(endPosition, trailingQuietWidth);
        ItfScanlineStopMatch match = new ItfScanlineStopMatch();
        match.EndPosition = endPosition;
        match.NarrowWidth = narrowWidth;
        match.WideWidth = wideWidth;
        match.Variance = (narrowDeviationOne + narrowDeviationTwo) /
            (observed[0] + observed[1] + observed[2]);
        match.TrailingQuietWidth = trailingQuietWidth;
        match.TrailingBearerBoundaryObserved = quietEnd < row.Length &&
            row.ReadBitUnchecked(quietEnd);
        return match;
    }

    static ItfScanlineDecodedData? CreateDecodedData(
        ItfScanlineState state,
        ItfScanlineStopMatch stop,
        BarcodeFormat format,
        ItfOptions options,
        double averageVariance)
    {
        string encodedText = state.Text.ToString();
        if (!IsAllowedLength(encodedText.Length, format, options.AllowedLengths))
        {
            return null;
        }

        bool hasChecksum = format == BarcodeFormat.Itf14 ||
            options.ChecksumMode != BarcodeChecksumMode.None;
        int checksumValue = 0;
        string dataText = encodedText;
        if (hasChecksum)
        {
            if (encodedText.Length < 2 || !Gs1Modulo10.VerifyCompleteDigits(encodedText))
            {
                return null;
            }

            int checkIndex = encodedText.Length - 1;
            checksumValue = encodedText[checkIndex] - '0';
            dataText = encodedText.Substring(0, checkIndex);
        }

        bool stripChecksum = format == BarcodeFormat.Itf &&
            options.ChecksumMode == BarcodeChecksumMode.VerifyStrip;
        string text = stripChecksum ? dataText : encodedText;
        string identifier = hasChecksum
            ? (stripChecksum ? "]I3" : BarcodeSymbologyIdentifiers.Itf14)
            : BarcodeSymbologyIdentifiers.Itf;
        int[] digits = state.Digits.ToArray();
        int[] barPatterns = state.BarPatterns.ToArray();
        int[] spacePatterns = state.SpacePatterns.ToArray();
        BarcodeDecodeResult publicResult = CreatePublicResult(
            text,
            digits,
            format,
            hasChecksum,
            checksumValue,
            identifier,
            averageVariance);

        double narrowWidth = (state.TotalNarrowWidth + stop.NarrowWidth) /
            (state.MeasurementCount + 1);
        double wideWidth = (state.TotalWideWidth + stop.WideWidth) /
            (state.WideMeasurementCount + 1);
        ItfScanlineDecodedData decoded = new ItfScanlineDecodedData();
        decoded.Result = publicResult;
        decoded.Format = format;
        decoded.Text = text;
        decoded.DataText = dataText;
        decoded.EncodedText = encodedText;
        decoded.Digits = digits;
        decoded.BarDigitPatternBits = barPatterns;
        decoded.SpaceDigitPatternBits = spacePatterns;
        decoded.HasChecksum = hasChecksum;
        decoded.ChecksumValue = checksumValue;
        decoded.SymbologyIdentifier = identifier;
        decoded.LeadingBearerBoundaryObserved = state.LeadingBearerBoundaryObserved;
        decoded.TrailingBearerBoundaryObserved = stop.TrailingBearerBoundaryObserved;
        decoded.StartPosition = state.StartPosition;
        decoded.EndPosition = stop.EndPosition;
        decoded.PairCount = state.PairCount;
        decoded.NarrowWidth = narrowWidth;
        decoded.WideToNarrowRatio = wideWidth / narrowWidth;
        decoded.AverageVariance = averageVariance;
        return decoded;
    }

    static bool IsAllowedLength(int length, BarcodeFormat format, int[]? allowedLengths)
    {
        if (format == BarcodeFormat.Itf14)
        {
            return length == Itf14Profile.CompleteDigitCount;
        }

        if (allowedLengths == null)
        {
            return length > 0 && length % 2 == 0;
        }

        int index = 0;
        while (index < allowedLengths.Length)
        {
            if (allowedLengths[index] == length)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static BarcodeDecodeResult CreatePublicResult(
        string text,
        int[] digits,
        BarcodeFormat format,
        bool hasChecksum,
        int checksumValue,
        string identifier,
        double averageVariance)
    {
        byte[] rawBytes = new byte[text.Length];
        int index = 0;
        while (index < text.Length)
        {
            rawBytes[index] = (byte)text[index];
            index = index + 1;
        }

        BarcodePayload payload = new BarcodePayload();
        payload.Text = text;
        payload.RawBytes = rawBytes;
        payload.CharacterEncoding = "ISO/IEC 646";
        payload.IsGs1 = format == BarcodeFormat.Itf14;

        BarcodeDecodeMetadata metadata = new BarcodeDecodeMetadata();
        metadata.HasChecksum = hasChecksum;
        metadata.ChecksumValid = hasChecksum;
        metadata.ChecksumValue = checksumValue;
        metadata.SymbologyIdentifier = identifier;
        metadata.Codewords = CopyWidths(digits);
        metadata.Confidence = CalculateConfidence(averageVariance);
        metadata.IsGs1 = format == BarcodeFormat.Itf14;

        BarcodeDecodeResult result = new BarcodeDecodeResult();
        result.Payload = payload;
        result.Format = format;
        result.Dimension = BarcodeDimension.Linear;
        result.Rotation = BarcodeRotation.None;
        result.OrientationDegrees = 0.0;
        result.Polarity = BarcodePolarity.DarkOnLight;
        result.MirrorMode = BarcodeMirrorMode.Normal;
        result.Metadata = metadata;
        result.Diagnostics = new BarcodeDiagnostic[0];
        return result;
    }

    static double CurrentNarrowWidth(ItfScanlineState state)
    {
        return state.TotalNarrowWidth / state.MeasurementCount;
    }

    static bool ScalesMatch(double first, double second)
    {
        double maximum = first > second ? first : second;
        return global::System.Math.Abs(first - second) / maximum <= ScaleRelativeTolerance;
    }

    static double CalculateUniformVariance(int[] runs, double expected)
    {
        double totalDeviation = 0.0;
        double totalWidth = 0.0;
        int index = 0;
        while (index < runs.Length)
        {
            totalDeviation = totalDeviation + global::System.Math.Abs(runs[index] - expected);
            totalWidth = totalWidth + runs[index];
            index = index + 1;
        }

        return totalDeviation / totalWidth;
    }

    static int FindDigit(int patternBits)
    {
        int digit = 0;
        while (digit < ItfPatterns.DigitCount)
        {
            if (ItfPatterns.GetDigitPatternBits(digit) == patternBits)
            {
                return digit;
            }

            digit = digit + 1;
        }

        return -1;
    }

    static int[] CopyWidths(int[] source)
    {
        int[] copy = new int[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            copy[index] = source[index];
            index = index + 1;
        }

        return copy;
    }

    static void SortWidths(int[] widths)
    {
        int index = 1;
        while (index < widths.Length)
        {
            int value = widths[index];
            int target = index;
            while (target > 0 && widths[target - 1] > value)
            {
                widths[target] = widths[target - 1];
                target = target - 1;
            }

            widths[target] = value;
            index = index + 1;
        }
    }

    static double CalculateConfidence(double averageVariance)
    {
        double confidence = 1.0 - (averageVariance / MaximumAverageVariance);
        if (confidence < 0.0)
        {
            return 0.0;
        }

        if (confidence > 1.0)
        {
            return 1.0;
        }

        return confidence;
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
