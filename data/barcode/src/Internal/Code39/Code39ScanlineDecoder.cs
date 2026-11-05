namespace barcode;

internal class Code39ScanlineDecodedData
{
    internal BarcodeDecodeResult Result = new BarcodeDecodeResult();
    internal string Text = "";
    internal string DataText = "";
    internal string EncodedText = "";
    internal string DataEncodedText = "";
    internal int[] Values = new int[0];
    internal int[] PatternBits = new int[0];
    internal bool HasChecksum;
    internal char CheckCharacter;
    internal Code39AppendMetadata? AppendMetadata;
    internal string SymbologyIdentifier = "";
    internal bool IsReversed;
    internal int StartPosition;
    internal int EndPosition;
    internal double NarrowWidth;
    internal double WideToNarrowRatio;
    internal double IntercharacterGapModules;
    internal double AverageVariance;
}

internal class Code39ScanlineCharacterMatch
{
    internal char Character;
    internal int Value;
    internal int PatternBits;
    internal int StartPosition;
    internal int EndPosition;
    internal double NarrowWidth;
    internal double WideWidth;
    internal double Variance;
}

internal class Code39ScanlineState
{
    internal global::System.Text.StringBuilder EncodedText = new global::System.Text.StringBuilder();
    internal global::System.Collections.Generic.List<int> Values =
        new global::System.Collections.Generic.List<int>();
    internal global::System.Collections.Generic.List<int> Patterns =
        new global::System.Collections.Generic.List<int>();
    internal int StartPosition;
    internal int Position;
    internal int CharacterCount;
    internal int GapCount;
    internal double TotalNarrowWidth;
    internal double TotalWideWidth;
    internal double TotalGapModules;
    internal double TotalVariance;
}

internal static class Code39ScanlineDecoder
{
    internal const double MinimumObservedWideRatio = 1.5;
    internal const double MaximumObservedWideRatio = 3.5;
    internal const double MaximumElementRelativeVariance = 0.45;
    internal const double MaximumAverageVariance = 0.20;
    internal const double IntercharacterGapRelativeTolerance = 0.50;
    internal const int MaximumDataCharacterCount = 4096;

    internal static Code39ScanlineDecodedData DecodeRow(BitRow row, Code39Options? options)
    {
        Code39ScanlineDecodedData? decoded = TryDecodeRow(row, options);
        if (decoded == null)
        {
            throw new BarcodeDecodingException("The scanline does not contain a valid Code 39 symbol.");
        }

        return decoded;
    }

    internal static Code39ScanlineDecodedData? TryDecodeRow(BitRow row, Code39Options? options)
    {
        if (row == null)
        {
            throw new BarcodeValidationException("Code 39 scanline must not be null.");
        }

        Code39Options settings = options == null ? new Code39Options() : options;
        ValidateOptions(settings);
        Code39ScanlineDecodedData? forward = TryDecodeForward(row, settings);
        if (forward != null)
        {
            forward.IsReversed = false;
            return forward;
        }

        BitRow reversedRow = CreateReversedRow(row);
        Code39ScanlineDecodedData? reversed = TryDecodeForward(reversedRow, settings);
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

    static void ValidateOptions(Code39Options options)
    {
        if (options.CharacterMode != Code39CharacterMode.Standard &&
            options.CharacterMode != Code39CharacterMode.FullAscii)
        {
            throw new BarcodeValidationException("Code 39 character mode is invalid.");
        }

        if (options.ChecksumMode != BarcodeChecksumMode.None &&
            options.ChecksumMode != BarcodeChecksumMode.Calculate &&
            options.ChecksumMode != BarcodeChecksumMode.Require &&
            options.ChecksumMode != BarcodeChecksumMode.VerifyKeep &&
            options.ChecksumMode != BarcodeChecksumMode.VerifyStrip)
        {
            throw new BarcodeValidationException("Code 39 checksum mode is invalid.");
        }

        if (options.IntercharacterGapModules < 0)
        {
            throw new BarcodeValidationException("Code 39 intercharacter gap must not be negative.");
        }

        double ratio = options.WideToNarrowRatio;
        if (double.IsNaN(ratio) || double.IsInfinity(ratio) ||
            (ratio != 0.0 && (ratio < 2.0 || ratio > 3.0)))
        {
            throw new BarcodeValidationException("Code 39 wide-to-narrow ratio must be zero or from 2.0 through 3.0.");
        }
    }

    static Code39ScanlineDecodedData? TryDecodeForward(BitRow row, Code39Options options)
    {
        int position = 0;
        while (position < row.Length)
        {
            bool dark = row.ReadBitUnchecked(position);
            bool atRunStart = position == 0 || row.ReadBitUnchecked(position - 1) != dark;
            if (dark && atRunStart)
            {
                Code39ScanlineCharacterMatch? start = MatchCharacterAt(row, position);
                if (start != null && start.Character == Code39Patterns.StartStopCharacter)
                {
                    int leadingQuietZone = CountLightBefore(row, position);
                    if (leadingQuietZone >= Code39Layout.StandardQuietZoneModules * start.NarrowWidth)
                    {
                        Code39ScanlineDecodedData? decoded = DecodeFromStart(row, start, options);
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

    static Code39ScanlineDecodedData? DecodeFromStart(
        BitRow row,
        Code39ScanlineCharacterMatch start,
        Code39Options options)
    {
        Code39ScanlineState state = new Code39ScanlineState();
        state.StartPosition = start.StartPosition;
        state.Position = start.EndPosition;
        state.Patterns.Add(start.PatternBits);
        AddCharacterMeasurement(state, start);
        Code39ScanlineCharacterMatch previous = start;
        int expectedGapModules = options.IntercharacterGapModules == 0
            ? Code39Layout.DefaultIntercharacterGapModules
            : options.IntercharacterGapModules;

        while (state.Position < row.Length)
        {
            if (row.ReadBitUnchecked(state.Position))
            {
                return null;
            }

            int gapWidth = CountLightForward(row, state.Position);
            int nextPosition = CheckedMath.Add(state.Position, gapWidth);
            if (nextPosition >= row.Length)
            {
                return null;
            }

            Code39ScanlineCharacterMatch? current = MatchCharacterAt(row, nextPosition);
            if (current == null)
            {
                return null;
            }

            double localNarrowWidth = (previous.NarrowWidth + current.NarrowWidth) / 2.0;
            double gapModules = gapWidth / localNarrowWidth;
            if (!IsValidIntercharacterGap(gapModules, expectedGapModules))
            {
                return null;
            }

            state.TotalGapModules = state.TotalGapModules + gapModules;
            state.GapCount = state.GapCount + 1;
            state.Patterns.Add(current.PatternBits);
            AddCharacterMeasurement(state, current);
            state.Position = current.EndPosition;

            if (current.Character == Code39Patterns.StartStopCharacter)
            {
                if (state.EncodedText.Length == 0)
                {
                    return null;
                }

                double averageNarrowWidth = state.TotalNarrowWidth / state.CharacterCount;
                int trailingQuietZone = CountLightAfter(row, state.Position);
                if (trailingQuietZone < Code39Layout.StandardQuietZoneModules * averageNarrowWidth)
                {
                    return null;
                }

                double averageVariance = state.TotalVariance / state.CharacterCount;
                if (averageVariance > MaximumAverageVariance)
                {
                    return null;
                }

                return CreateDecodedData(state, options, averageVariance);
            }

            if (state.EncodedText.Length >= MaximumDataCharacterCount)
            {
                return null;
            }

            state.EncodedText.Append(current.Character);
            state.Values.Add(current.Value);
            previous = current;
        }

        return null;
    }

    static void AddCharacterMeasurement(
        Code39ScanlineState state,
        Code39ScanlineCharacterMatch match)
    {
        state.TotalNarrowWidth = state.TotalNarrowWidth + match.NarrowWidth;
        state.TotalWideWidth = state.TotalWideWidth + match.WideWidth;
        state.TotalVariance = state.TotalVariance + match.Variance;
        state.CharacterCount = state.CharacterCount + 1;
    }

    static Code39ScanlineCharacterMatch? MatchCharacterAt(BitRow row, int position)
    {
        if (position < 0 || position >= row.Length || !row.ReadBitUnchecked(position))
        {
            return null;
        }

        int[] observed = RunLength.ExtractForward(row, position, Code39Patterns.ElementCount);
        if (observed.Length != Code39Patterns.ElementCount)
        {
            return null;
        }

        int[] sorted = CopyWidths(observed);
        SortWidths(sorted);
        if (sorted[5] >= sorted[6])
        {
            return null;
        }

        double threshold = (sorted[5] + sorted[6]) / 2.0;
        int patternBits = 0;
        int wideCount = 0;
        double narrowTotal = 0.0;
        double wideTotal = 0.0;
        int index = 0;
        while (index < observed.Length)
        {
            if (observed[index] > threshold)
            {
                int bit = Code39Patterns.ElementCount - index - 1;
                patternBits = patternBits | (1 << bit);
                wideTotal = wideTotal + observed[index];
                wideCount = wideCount + 1;
            }
            else
            {
                narrowTotal = narrowTotal + observed[index];
            }

            index = index + 1;
        }

        if (wideCount != Code39Patterns.WideElementCount)
        {
            return null;
        }

        double narrowWidth = narrowTotal / 6.0;
        double wideWidth = wideTotal / 3.0;
        double ratio = wideWidth / narrowWidth;
        if (ratio < MinimumObservedWideRatio || ratio > MaximumObservedWideRatio)
        {
            return null;
        }

        double totalDeviation = 0.0;
        double totalWidth = 0.0;
        index = 0;
        while (index < observed.Length)
        {
            bool wide = observed[index] > threshold;
            double expected = wide ? wideWidth : narrowWidth;
            double deviation = global::System.Math.Abs(observed[index] - expected);
            if (deviation / expected > MaximumElementRelativeVariance)
            {
                return null;
            }

            totalDeviation = totalDeviation + deviation;
            totalWidth = totalWidth + observed[index];
            index = index + 1;
        }

        int value = FindDataValue(patternBits);
        char character;
        if (patternBits == Code39Patterns.StartStopPatternBits)
        {
            character = Code39Patterns.StartStopCharacter;
            value = -1;
        }
        else if (value >= 0)
        {
            character = Code39Patterns.GetCharacter(value);
        }
        else
        {
            return null;
        }

        Code39ScanlineCharacterMatch match = new Code39ScanlineCharacterMatch();
        match.Character = character;
        match.Value = value;
        match.PatternBits = patternBits;
        match.StartPosition = position;
        match.EndPosition = CheckedMath.Add(position, PatternMatcher.SumPositiveRuns(observed));
        match.NarrowWidth = narrowWidth;
        match.WideWidth = wideWidth;
        match.Variance = totalDeviation / totalWidth;
        return match;
    }

    static int FindDataValue(int patternBits)
    {
        int value = 0;
        while (value < Code39Patterns.DataCharacterCount)
        {
            if (Code39Patterns.GetDataPatternBits(value) == patternBits)
            {
                return value;
            }

            value = value + 1;
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

    static bool IsValidIntercharacterGap(double observedModules, int expectedModules)
    {
        double minimum = expectedModules * (1.0 - IntercharacterGapRelativeTolerance);
        double maximum = expectedModules * (1.0 + IntercharacterGapRelativeTolerance);
        return observedModules >= minimum && observedModules <= maximum;
    }

    static Code39ScanlineDecodedData? CreateDecodedData(
        Code39ScanlineState state,
        Code39Options options,
        double averageVariance)
    {
        string encodedText = state.EncodedText.ToString();
        bool hasChecksum = options.ChecksumMode != BarcodeChecksumMode.None;
        bool transmitCheck = false;
        char checkCharacter = '\0';
        string dataEncodedText = encodedText;
        if (hasChecksum)
        {
            if (encodedText.Length < 2 || !Code39Modulo43.VerifyCompleteText(encodedText))
            {
                return null;
            }

            int checkIndex = encodedText.Length - 1;
            checkCharacter = encodedText[checkIndex];
            dataEncodedText = encodedText.Substring(0, checkIndex);
            transmitCheck = IsCheckCharacterTransmitted(options);
        }

        Code39DecodedData? semantic = Code39Decoder.TryDecodeDataCharacters(
            dataEncodedText,
            options.CharacterMode);
        if (semantic == null)
        {
            return null;
        }

        string text = transmitCheck ? semantic.Text + checkCharacter : semantic.Text;
        string identifier = ResolveSymbologyIdentifier(options.CharacterMode, hasChecksum, transmitCheck);
        Code39AppendMetadata? append = CreateAppendMetadata(semantic.Text, options.EnableAppend);
        int[] values = state.Values.ToArray();
        int[] patterns = state.Patterns.ToArray();
        double narrowWidth = state.TotalNarrowWidth / state.CharacterCount;
        double wideWidth = state.TotalWideWidth / state.CharacterCount;
        double gapModules = state.TotalGapModules / state.GapCount;
        BarcodeDecodeResult publicResult = CreatePublicResult(
            text,
            values,
            hasChecksum,
            checkCharacter,
            identifier,
            averageVariance);

        Code39ScanlineDecodedData decoded = new Code39ScanlineDecodedData();
        decoded.Result = publicResult;
        decoded.Text = text;
        decoded.DataText = semantic.Text;
        decoded.EncodedText = encodedText;
        decoded.DataEncodedText = dataEncodedText;
        decoded.Values = values;
        decoded.PatternBits = patterns;
        decoded.HasChecksum = hasChecksum;
        decoded.CheckCharacter = checkCharacter;
        decoded.AppendMetadata = append;
        decoded.SymbologyIdentifier = identifier;
        decoded.StartPosition = state.StartPosition;
        decoded.EndPosition = state.Position;
        decoded.NarrowWidth = narrowWidth;
        decoded.WideToNarrowRatio = wideWidth / narrowWidth;
        decoded.IntercharacterGapModules = gapModules;
        decoded.AverageVariance = averageVariance;
        return decoded;
    }

    static bool IsCheckCharacterTransmitted(Code39Options options)
    {
        if (options.ChecksumMode == BarcodeChecksumMode.VerifyKeep)
        {
            return true;
        }

        if (options.ChecksumMode == BarcodeChecksumMode.VerifyStrip)
        {
            return false;
        }

        return options.TransmitCheckCharacter;
    }

    static string ResolveSymbologyIdentifier(
        Code39CharacterMode characterMode,
        bool hasChecksum,
        bool transmitCheck)
    {
        int modifier = characterMode == Code39CharacterMode.FullAscii ? 4 : 0;
        if (hasChecksum)
        {
            modifier = modifier + (transmitCheck ? 1 : 3);
        }

        return "]A" + (char)('0' + modifier);
    }

    static Code39AppendMetadata? CreateAppendMetadata(string dataText, bool enabled)
    {
        if (!enabled)
        {
            return null;
        }

        Code39AppendMetadata metadata = new Code39AppendMetadata();
        if (dataText.Length > 0 && dataText[0] == ' ')
        {
            metadata.IsContinuation = true;
            metadata.SegmentText = dataText.Substring(1);
        }
        else
        {
            metadata.ReleasesBufferedData = true;
            metadata.SegmentText = dataText;
        }

        return metadata;
    }

    static BarcodeDecodeResult CreatePublicResult(
        string text,
        int[] values,
        bool hasChecksum,
        char checkCharacter,
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

        BarcodeDecodeMetadata metadata = new BarcodeDecodeMetadata();
        metadata.HasChecksum = hasChecksum;
        metadata.ChecksumValid = hasChecksum;
        if (hasChecksum)
        {
            metadata.ChecksumValue = Code39Patterns.GetValue(checkCharacter);
        }

        metadata.SymbologyIdentifier = identifier;
        metadata.Codewords = CopyValues(values);
        metadata.Confidence = CalculateConfidence(averageVariance);

        BarcodeDecodeResult result = new BarcodeDecodeResult();
        result.Payload = payload;
        result.Format = BarcodeFormat.Code39;
        result.Dimension = BarcodeDimension.Linear;
        result.Rotation = BarcodeRotation.None;
        result.OrientationDegrees = 0.0;
        result.Polarity = BarcodePolarity.DarkOnLight;
        result.MirrorMode = BarcodeMirrorMode.Normal;
        result.Metadata = metadata;
        result.Diagnostics = new BarcodeDiagnostic[0];
        return result;
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

    static int[] CopyValues(int[] values)
    {
        int[] copy = new int[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            copy[index] = values[index];
            index = index + 1;
        }

        return copy;
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

    static int CountLightForward(BitRow row, int position)
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

    static int CountLightAfter(BitRow row, int position)
    {
        return CountLightForward(row, position);
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
