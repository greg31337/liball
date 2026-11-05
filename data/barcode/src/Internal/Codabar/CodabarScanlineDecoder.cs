namespace barcode;

internal class CodabarScanlineDecodedData
{
    internal BarcodeDecodeResult Result = new BarcodeDecodeResult();
    internal string Text = "";
    internal string DataText = "";
    internal string CarrierDataText = "";
    internal string EncodedText = "";
    internal int[] Values = new int[0];
    internal int[] PatternBits = new int[0];
    internal char StartCharacter;
    internal char StopCharacter;
    internal char CheckCharacter;
    internal int ChecksumValue;
    internal bool HasChecksum;
    internal string SymbologyIdentifier = "";
    internal bool IsReversed;
    internal int StartPosition;
    internal int EndPosition;
    internal double NarrowWidth;
    internal double WideToNarrowRatio;
    internal double IntercharacterGapModules;
    internal double AverageVariance;
}

internal class CodabarScanlineCharacterMatch
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

internal class CodabarScanlinePatternMeasurement
{
    internal int Value;
    internal int PatternBits;
    internal double NarrowWidth;
    internal double WideWidth;
    internal double Variance;
}

internal class CodabarScanlineState
{
    internal global::System.Text.StringBuilder CarrierData = new global::System.Text.StringBuilder();
    internal global::System.Collections.Generic.List<int> Values =
        new global::System.Collections.Generic.List<int>();
    internal global::System.Collections.Generic.List<int> Patterns =
        new global::System.Collections.Generic.List<int>();
    internal char StartCharacter;
    internal char StopCharacter;
    internal int StartPosition;
    internal int Position;
    internal int CharacterCount;
    internal int GapCount;
    internal double TotalNarrowWidth;
    internal double TotalWideWidth;
    internal double TotalGapModules;
    internal double TotalVariance;
}

internal static class CodabarScanlineDecoder
{
    internal const double MinimumObservedWideRatio = 1.5;
    internal const double MaximumObservedWideRatio = 3.5;
    internal const double MaximumElementRelativeVariance = 0.45;
    internal const double MaximumAverageVariance = 0.20;
    internal const double IntercharacterGapRelativeTolerance = 0.50;
    internal const int MaximumDataCharacterCount = 4096;

    internal static CodabarScanlineDecodedData DecodeRow(BitRow row, CodabarOptions? options)
    {
        CodabarScanlineDecodedData? decoded = TryDecodeRow(row, options);
        if (decoded == null)
        {
            throw new BarcodeDecodingException("The scanline does not contain a valid Codabar symbol.");
        }

        return decoded;
    }

    internal static CodabarScanlineDecodedData? TryDecodeRow(BitRow row, CodabarOptions? options)
    {
        if (row == null)
        {
            throw new BarcodeValidationException("Codabar scanline must not be null.");
        }

        CodabarOptions settings = options == null ? new CodabarOptions() : options;
        ValidateOptions(settings);
        CodabarScanlineDecodedData? forward = TryDecodeForward(row, settings);
        if (forward != null)
        {
            forward.IsReversed = false;
            return forward;
        }

        BitRow reversedRow = CreateReversedRow(row);
        CodabarScanlineDecodedData? reversed = TryDecodeForward(reversedRow, settings);
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

    static void ValidateOptions(CodabarOptions options)
    {
        if (options.ChecksumMode != BarcodeChecksumMode.None &&
            options.ChecksumMode != BarcodeChecksumMode.Calculate &&
            options.ChecksumMode != BarcodeChecksumMode.Require &&
            options.ChecksumMode != BarcodeChecksumMode.VerifyKeep &&
            options.ChecksumMode != BarcodeChecksumMode.VerifyStrip)
        {
            throw new BarcodeValidationException("Codabar checksum mode is invalid.");
        }

        if (options.IntercharacterGapModules < 0)
        {
            throw new BarcodeValidationException("Codabar intercharacter gap must not be negative.");
        }

        double ratio = options.WideToNarrowRatio;
        if (double.IsNaN(ratio) || double.IsInfinity(ratio) ||
            (ratio != 0.0 && (ratio < 2.0 || ratio > 3.0)))
        {
            throw new BarcodeValidationException("Codabar wide-to-narrow ratio must be zero or from 2.0 through 3.0.");
        }

        ValidateSelectedTerminal(options.StartCharacter, options.AcceptAliases, "start");
        ValidateSelectedTerminal(options.StopCharacter, options.AcceptAliases, "stop");
    }

    static void ValidateSelectedTerminal(
        string? selected,
        bool acceptAliases,
        string terminalName)
    {
        if (selected == null)
        {
            return;
        }

        if (selected.Length != 1 ||
            !CodabarPatterns.IsStartStopInput(selected[0], acceptAliases))
        {
            throw new BarcodeValidationException(
                "Codabar " + terminalName + " filter must contain one permitted terminal character.");
        }
    }

    static CodabarScanlineDecodedData? TryDecodeForward(BitRow row, CodabarOptions options)
    {
        int position = 0;
        while (position < row.Length)
        {
            bool dark = row.ReadBitUnchecked(position);
            bool atRunStart = position == 0 || row.ReadBitUnchecked(position - 1) != dark;
            if (dark && atRunStart)
            {
                CodabarScanlineCharacterMatch? start = MatchCharacterAt(row, position);
                if (start != null && CodabarPatterns.IsStartStopCharacter(start.Character) &&
                    MatchesSelectedTerminal(start.Character, options.StartCharacter, options.AcceptAliases))
                {
                    int leadingQuietZone = CountLightBefore(row, position);
                    if (leadingQuietZone >= CodabarLayout.StandardQuietZoneModules * start.NarrowWidth)
                    {
                        CodabarScanlineDecodedData? decoded = DecodeFromStart(row, start, options);
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

    static CodabarScanlineDecodedData? DecodeFromStart(
        BitRow row,
        CodabarScanlineCharacterMatch start,
        CodabarOptions options)
    {
        CodabarScanlineState state = new CodabarScanlineState();
        state.StartCharacter = start.Character;
        state.StartPosition = start.StartPosition;
        state.Position = start.EndPosition;
        state.Values.Add(start.Value);
        state.Patterns.Add(start.PatternBits);
        AddCharacterMeasurement(state, start);
        CodabarScanlineCharacterMatch previous = start;
        int expectedGapModules = options.IntercharacterGapModules == 0
            ? CodabarLayout.DefaultIntercharacterGapModules
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

            CodabarScanlineCharacterMatch? current = MatchCharacterAt(row, nextPosition);
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
            state.Values.Add(current.Value);
            state.Patterns.Add(current.PatternBits);
            AddCharacterMeasurement(state, current);
            state.Position = current.EndPosition;

            if (CodabarPatterns.IsStartStopCharacter(current.Character))
            {
                if (state.CarrierData.Length == 0 ||
                    !IsValidTerminalPair(state.StartCharacter, current.Character) ||
                    !MatchesSelectedTerminal(current.Character, options.StopCharacter, options.AcceptAliases))
                {
                    return null;
                }

                state.StopCharacter = current.Character;
                double averageNarrowWidth = state.TotalNarrowWidth / state.CharacterCount;
                int trailingQuietZone = CountLightAfter(row, state.Position);
                if (trailingQuietZone < CodabarLayout.StandardQuietZoneModules * averageNarrowWidth)
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

            if (!CodabarPatterns.IsDataCharacter(current.Character) ||
                state.CarrierData.Length >= MaximumDataCharacterCount)
            {
                return null;
            }

            state.CarrierData.Append(current.Character);
            previous = current;
        }

        return null;
    }

    static bool IsValidTerminalPair(char start, char stop)
    {
        return CodabarPatterns.IsStartStopCharacter(start) &&
            CodabarPatterns.IsStartStopCharacter(stop);
    }

    static bool MatchesSelectedTerminal(
        char observed,
        string? selected,
        bool acceptAliases)
    {
        if (selected == null)
        {
            return true;
        }

        char expected = CodabarPatterns.NormalizeStartStop(selected[0], acceptAliases);
        return observed == expected;
    }

    static void AddCharacterMeasurement(
        CodabarScanlineState state,
        CodabarScanlineCharacterMatch match)
    {
        state.TotalNarrowWidth = state.TotalNarrowWidth + match.NarrowWidth;
        state.TotalWideWidth = state.TotalWideWidth + match.WideWidth;
        state.TotalVariance = state.TotalVariance + match.Variance;
        state.CharacterCount = state.CharacterCount + 1;
    }

    static CodabarScanlineCharacterMatch? MatchCharacterAt(BitRow row, int position)
    {
        if (position < 0 || position >= row.Length || !row.ReadBitUnchecked(position))
        {
            return null;
        }

        int[] observed = RunLength.ExtractForward(row, position, CodabarPatterns.ElementCount);
        if (observed.Length != CodabarPatterns.ElementCount)
        {
            return null;
        }

        CodabarScanlinePatternMeasurement? measurement = FindBestPattern(observed);
        if (measurement == null)
        {
            return null;
        }

        char character = CodabarPatterns.GetCharacter(measurement.Value);
        CodabarScanlineCharacterMatch match = new CodabarScanlineCharacterMatch();
        match.Character = character;
        match.Value = measurement.Value;
        match.PatternBits = measurement.PatternBits;
        match.StartPosition = position;
        match.EndPosition = CheckedMath.Add(position, PatternMatcher.SumPositiveRuns(observed));
        match.NarrowWidth = measurement.NarrowWidth;
        match.WideWidth = measurement.WideWidth;
        match.Variance = measurement.Variance;
        return match;
    }

    static CodabarScanlinePatternMeasurement? FindBestPattern(int[] observed)
    {
        CodabarScanlinePatternMeasurement? best = null;
        int value = 0;
        while (value < CodabarPatterns.CharacterCount)
        {
            char character = CodabarPatterns.GetCharacter(value);
            int patternBits = CodabarPatterns.GetCharacterPatternBits(character);
            CodabarScanlinePatternMeasurement? measured = MeasurePattern(
                observed,
                value,
                patternBits);
            if (measured != null && (best == null || measured.Variance < best.Variance))
            {
                best = measured;
            }

            value = value + 1;
        }

        return best;
    }

    static CodabarScanlinePatternMeasurement? MeasurePattern(
        int[] observed,
        int value,
        int patternBits)
    {
        double narrowTotal = 0.0;
        double wideTotal = 0.0;
        int narrowCount = 0;
        int wideCount = 0;
        int index = 0;
        while (index < observed.Length)
        {
            int bit = CodabarPatterns.ElementCount - index - 1;
            bool wide = (patternBits & (1 << bit)) != 0;
            if (wide)
            {
                wideTotal = wideTotal + observed[index];
                wideCount = wideCount + 1;
            }
            else
            {
                narrowTotal = narrowTotal + observed[index];
                narrowCount = narrowCount + 1;
            }

            index = index + 1;
        }

        double narrowWidth = narrowTotal / narrowCount;
        double wideWidth = wideTotal / wideCount;
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
            int bit = CodabarPatterns.ElementCount - index - 1;
            bool wide = (patternBits & (1 << bit)) != 0;
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

        CodabarScanlinePatternMeasurement measurement = new CodabarScanlinePatternMeasurement();
        measurement.Value = value;
        measurement.PatternBits = patternBits;
        measurement.NarrowWidth = narrowWidth;
        measurement.WideWidth = wideWidth;
        measurement.Variance = totalDeviation / totalWidth;
        return measurement;
    }

    static bool IsValidIntercharacterGap(double observedModules, int expectedModules)
    {
        double minimum = expectedModules * (1.0 - IntercharacterGapRelativeTolerance);
        double maximum = expectedModules * (1.0 + IntercharacterGapRelativeTolerance);
        return observedModules >= minimum && observedModules <= maximum;
    }

    static CodabarScanlineDecodedData? CreateDecodedData(
        CodabarScanlineState state,
        CodabarOptions options,
        double averageVariance)
    {
        string carrierDataText = state.CarrierData.ToString();
        string encodedText = state.StartCharacter + carrierDataText + state.StopCharacter;
        bool hasChecksum = options.ChecksumMode != BarcodeChecksumMode.None;
        char checkCharacter = '\0';
        int checksumValue = 0;
        string dataText = carrierDataText;
        if (hasChecksum)
        {
            if (carrierDataText.Length < 2 || !CodabarModulo16.VerifyCompleteText(encodedText))
            {
                return null;
            }

            int checkIndex = carrierDataText.Length - 1;
            checkCharacter = carrierDataText[checkIndex];
            checksumValue = CodabarPatterns.GetValue(checkCharacter);
            dataText = carrierDataText.Substring(0, checkIndex);
        }

        bool transmitCheck = IsCheckCharacterTransmitted(options);
        string transmittedData = transmitCheck ? dataText + checkCharacter : dataText;
        string text = options.TransmitStartStop
            ? state.StartCharacter + transmittedData + state.StopCharacter
            : transmittedData;
        string identifier = BarcodeSymbologyIdentifiers.Codabar;
        int[] values = state.Values.ToArray();
        int[] patterns = state.Patterns.ToArray();
        double narrowWidth = state.TotalNarrowWidth / state.CharacterCount;
        double wideWidth = state.TotalWideWidth / state.CharacterCount;
        double gapModules = state.TotalGapModules / state.GapCount;
        BarcodeDecodeResult publicResult = CreatePublicResult(
            text,
            values,
            hasChecksum,
            checksumValue,
            identifier,
            averageVariance);

        CodabarScanlineDecodedData decoded = new CodabarScanlineDecodedData();
        decoded.Result = publicResult;
        decoded.Text = text;
        decoded.DataText = dataText;
        decoded.CarrierDataText = carrierDataText;
        decoded.EncodedText = encodedText;
        decoded.Values = values;
        decoded.PatternBits = patterns;
        decoded.StartCharacter = state.StartCharacter;
        decoded.StopCharacter = state.StopCharacter;
        decoded.CheckCharacter = checkCharacter;
        decoded.ChecksumValue = checksumValue;
        decoded.HasChecksum = hasChecksum;
        decoded.SymbologyIdentifier = identifier;
        decoded.StartPosition = state.StartPosition;
        decoded.EndPosition = state.Position;
        decoded.NarrowWidth = narrowWidth;
        decoded.WideToNarrowRatio = wideWidth / narrowWidth;
        decoded.IntercharacterGapModules = gapModules;
        decoded.AverageVariance = averageVariance;
        return decoded;
    }

    static bool IsCheckCharacterTransmitted(CodabarOptions options)
    {
        if (options.ChecksumMode == BarcodeChecksumMode.VerifyKeep)
        {
            return true;
        }

        if (options.ChecksumMode == BarcodeChecksumMode.VerifyStrip ||
            options.ChecksumMode == BarcodeChecksumMode.None)
        {
            return false;
        }

        return options.TransmitCheckCharacter;
    }

    static BarcodeDecodeResult CreatePublicResult(
        string text,
        int[] values,
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

        BarcodeDecodeMetadata metadata = new BarcodeDecodeMetadata();
        metadata.HasChecksum = hasChecksum;
        metadata.ChecksumValid = hasChecksum;
        metadata.ChecksumValue = checksumValue;
        metadata.SymbologyIdentifier = identifier;
        metadata.Codewords = CopyValues(values);
        metadata.Confidence = CalculateConfidence(averageVariance);

        BarcodeDecodeResult result = new BarcodeDecodeResult();
        result.Payload = payload;
        result.Format = BarcodeFormat.Codabar;
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
