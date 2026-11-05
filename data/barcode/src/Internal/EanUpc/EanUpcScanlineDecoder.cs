namespace barcode;

internal class EanUpcDecodedData
{
    internal BarcodeDecodeResult Result = new BarcodeDecodeResult();
    internal BarcodeFormat Format;
    internal string Text = "";
    internal string Ean13CompatibleText = "";
    internal string ExpandedUpcAText = "";
    internal int[] Digits = new int[0];
    internal EanUpcDigitSet[] LeftParity = new EanUpcDigitSet[0];
    internal int ChecksumValue;
    internal bool IsReversed;
    internal int StartPosition;
    internal int EndPosition;
    internal double ModuleSize;
    internal double AverageVariance;
}

internal class EanUpcScanlinePatternMatch
{
    internal int Digit;
    internal EanUpcDigitSet DigitSet;
    internal int EndPosition;
    internal double Variance;
}

internal class EanUpcScanlineState
{
    internal int[] Digits = new int[12];
    internal EanUpcDigitSet[] LeftParity = new EanUpcDigitSet[6];
    internal int DigitCount;
    internal int LeftParityCount;
    internal int Position;
    internal double TotalVariance;
    internal int PatternCount;
}

internal static class EanUpcScanlineDecoder
{
    internal const double MaximumAverageVariance = 0.25;
    internal const double MaximumIndividualVariance = 0.7;

    internal static EanUpcDecodedData DecodeRow(BitRow row)
    {
        EanUpcDecodedData? decoded = TryDecodeRow(row);
        if (decoded == null)
        {
            throw new BarcodeDecodingException("The scanline does not contain a valid EAN/UPC symbol.");
        }

        return decoded;
    }

    internal static EanUpcDecodedData? TryDecodeRow(BitRow row)
    {
        if (row == null)
        {
            throw new BarcodeValidationException("EAN/UPC scanline must not be null.");
        }

        EanUpcDecodedData? forward = TryDecodeForward(row);
        if (forward != null)
        {
            forward.IsReversed = false;
            return forward;
        }

        BitRow reversedRow = CreateReversedRow(row);
        EanUpcDecodedData? reversed = TryDecodeForward(reversedRow);
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

    static EanUpcDecodedData? TryDecodeForward(BitRow row)
    {
        int position = 0;
        while (position < row.Length)
        {
            bool dark = row.ReadBitUnchecked(position);
            bool atRunStart = position == 0 || row.ReadBitUnchecked(position - 1) != dark;
            if (dark && atRunStart)
            {
                EanUpcScanlinePatternMatch? start = MatchStartAt(row, position);
                if (start != null)
                {
                    EanUpcDecodedData? upcE = TryDecodeUpcEAt(row, position, start);
                    if (upcE != null)
                    {
                        return upcE;
                    }

                    EanUpcDecodedData? ean8 = TryDecodeEan8At(row, position, start);
                    if (ean8 != null)
                    {
                        return ean8;
                    }

                    EanUpcDecodedData? ean13 = TryDecodeEan13At(row, position, start);
                    if (ean13 != null)
                    {
                        return ean13;
                    }
                }
            }

            position = position + 1;
        }

        return null;
    }

    static EanUpcScanlinePatternMatch? MatchStartAt(BitRow row, int position)
    {
        return MatchGuardAt(row, position, EanUpcGuardPattern.Normal);
    }

    static EanUpcDecodedData? TryDecodeEan13At(
        BitRow row,
        int startPosition,
        EanUpcScanlinePatternMatch start)
    {
        EanUpcScanlineState state = CreateStateFromStart(start);
        if (!ReadLeftDigits(row, state, 6, true) ||
            !ReadGuardPattern(row, state, EanUpcGuardPattern.Center) ||
            !ReadRightDigits(row, state, 6) ||
            !ReadGuardPattern(row, state, EanUpcGuardPattern.Normal))
        {
            return null;
        }

        int leadingDigit = FindEan13LeadingDigit(state.LeftParity, state.LeftParityCount);
        if (leadingDigit < 0)
        {
            return null;
        }

        int[] fullDigits = new int[EanUpcEncoder.Ean13CompleteLength];
        fullDigits[0] = leadingDigit;
        CopyDigitsInto(state.Digits, state.DigitCount, fullDigits, 1);
        string fullText = ConvertDigitsToText(fullDigits);
        if (!Gs1Modulo10.VerifyCompleteDigits(fullText))
        {
            return null;
        }

        int endPosition = state.Position;
        double moduleSize = (endPosition - startPosition) / (double)EanUpcEncoder.Ean13ModuleCount;
        bool hasUpcAMargins = leadingDigit == 0 && HasMinimumQuietZones(
            row,
            startPosition,
            endPosition,
            moduleSize,
            EanUpcLayout.UpcALeftQuietZoneModules,
            EanUpcLayout.UpcARightQuietZoneModules);
        bool hasEan13Margins = HasMinimumQuietZones(
            row,
            startPosition,
            endPosition,
            moduleSize,
            EanUpcLayout.Ean13LeftQuietZoneModules,
            EanUpcLayout.Ean13RightQuietZoneModules);
        if (!hasUpcAMargins && !hasEan13Margins)
        {
            return null;
        }

        double averageVariance = state.TotalVariance / state.PatternCount;
        if (averageVariance >= MaximumAverageVariance)
        {
            return null;
        }

        BarcodeFormat format = hasUpcAMargins ? BarcodeFormat.UpcA : BarcodeFormat.Ean13;
        string text = format == BarcodeFormat.UpcA ? fullText.Substring(1, 12) : fullText;
        int[] reportedDigits = format == BarcodeFormat.UpcA ? CopyDigitRange(fullDigits, 1, 12) : fullDigits;
        EanUpcDecodedData decoded = CreateDecodedData(
            format,
            text,
            reportedDigits,
            state.LeftParity,
            state.LeftParityCount,
            startPosition,
            endPosition,
            moduleSize,
            averageVariance);
        decoded.Ean13CompatibleText = fullText;
        if (format == BarcodeFormat.UpcA)
        {
            decoded.ExpandedUpcAText = text;
        }

        return decoded;
    }

    static EanUpcDecodedData? TryDecodeEan8At(
        BitRow row,
        int startPosition,
        EanUpcScanlinePatternMatch start)
    {
        EanUpcScanlineState state = CreateStateFromStart(start);
        if (!ReadLeftDigits(row, state, 4, false) ||
            !ReadGuardPattern(row, state, EanUpcGuardPattern.Center) ||
            !ReadRightDigits(row, state, 4) ||
            !ReadGuardPattern(row, state, EanUpcGuardPattern.Normal))
        {
            return null;
        }

        int[] digits = CopyDigitRange(state.Digits, 0, state.DigitCount);
        string text = ConvertDigitsToText(digits);
        int endPosition = state.Position;
        double moduleSize = (endPosition - startPosition) / (double)EanUpcEncoder.Ean8ModuleCount;
        if (!Gs1Modulo10.VerifyCompleteDigits(text) ||
            !HasMinimumQuietZones(
                row,
                startPosition,
                endPosition,
                moduleSize,
                EanUpcLayout.Ean8LeftQuietZoneModules,
                EanUpcLayout.Ean8RightQuietZoneModules))
        {
            return null;
        }

        double averageVariance = state.TotalVariance / state.PatternCount;
        if (averageVariance >= MaximumAverageVariance)
        {
            return null;
        }

        return CreateDecodedData(
            BarcodeFormat.Ean8,
            text,
            digits,
            state.LeftParity,
            state.LeftParityCount,
            startPosition,
            endPosition,
            moduleSize,
            averageVariance);
    }

    static EanUpcDecodedData? TryDecodeUpcEAt(
        BitRow row,
        int startPosition,
        EanUpcScanlinePatternMatch start)
    {
        EanUpcScanlineState state = CreateStateFromStart(start);
        if (!ReadLeftDigits(row, state, 6, true) ||
            !ReadGuardPattern(row, state, EanUpcGuardPattern.UpcEEnd))
        {
            return null;
        }

        int[] numberSystemAndCheck = FindUpcENumberSystemAndCheck(
            state.LeftParity,
            state.LeftParityCount);
        if (numberSystemAndCheck.Length == 0)
        {
            return null;
        }

        int[] digits = new int[EanUpcEncoder.UpcECompleteLength];
        digits[0] = numberSystemAndCheck[0];
        CopyDigitsInto(state.Digits, state.DigitCount, digits, 1);
        digits[digits.Length - 1] = numberSystemAndCheck[1];
        string text = ConvertDigitsToText(digits);
        string expandedData = ExpandUpcEData(text);
        string expandedUpcA = expandedData + text[text.Length - 1];
        int endPosition = state.Position;
        double moduleSize = (endPosition - startPosition) / (double)EanUpcEncoder.UpcEModuleCount;
        if (!Gs1Modulo10.VerifyCompleteDigits(expandedUpcA) ||
            !HasMinimumQuietZones(
                row,
                startPosition,
                endPosition,
                moduleSize,
                EanUpcLayout.UpcELeftQuietZoneModules,
                EanUpcLayout.UpcERightQuietZoneModules))
        {
            return null;
        }

        double averageVariance = state.TotalVariance / state.PatternCount;
        if (averageVariance >= MaximumAverageVariance)
        {
            return null;
        }

        EanUpcDecodedData decoded = CreateDecodedData(
            BarcodeFormat.UpcE,
            text,
            digits,
            state.LeftParity,
            state.LeftParityCount,
            startPosition,
            endPosition,
            moduleSize,
            averageVariance);
        decoded.Ean13CompatibleText = "0" + expandedUpcA;
        decoded.ExpandedUpcAText = expandedUpcA;
        return decoded;
    }

    static EanUpcScanlineState CreateStateFromStart(EanUpcScanlinePatternMatch start)
    {
        EanUpcScanlineState state = new EanUpcScanlineState();
        state.Position = start.EndPosition;
        state.TotalVariance = start.Variance;
        state.PatternCount = 1;
        return state;
    }

    static bool ReadLeftDigits(
        BitRow row,
        EanUpcScanlineState state,
        int count,
        bool allowG)
    {
        int index = 0;
        while (index < count)
        {
            EanUpcScanlinePatternMatch? match = MatchLeftDigitAt(row, state.Position, allowG);
            if (match == null)
            {
                return false;
            }

            state.Digits[state.DigitCount] = match.Digit;
            state.DigitCount = state.DigitCount + 1;
            state.LeftParity[state.LeftParityCount] = match.DigitSet;
            state.LeftParityCount = state.LeftParityCount + 1;
            AddMatchToState(state, match);
            index = index + 1;
        }

        return true;
    }

    static bool ReadRightDigits(BitRow row, EanUpcScanlineState state, int count)
    {
        int index = 0;
        while (index < count)
        {
            EanUpcScanlinePatternMatch? match = MatchRightDigitAt(row, state.Position);
            if (match == null)
            {
                return false;
            }

            state.Digits[state.DigitCount] = match.Digit;
            state.DigitCount = state.DigitCount + 1;
            AddMatchToState(state, match);
            index = index + 1;
        }

        return true;
    }

    static bool ReadGuardPattern(
        BitRow row,
        EanUpcScanlineState state,
        EanUpcGuardPattern pattern)
    {
        EanUpcScanlinePatternMatch? match = MatchGuardAt(row, state.Position, pattern);
        if (match == null)
        {
            return false;
        }

        AddMatchToState(state, match);
        return true;
    }

    static void AddMatchToState(EanUpcScanlineState state, EanUpcScanlinePatternMatch match)
    {
        state.Position = match.EndPosition;
        state.TotalVariance = state.TotalVariance + match.Variance;
        state.PatternCount = state.PatternCount + 1;
    }

    internal static EanUpcScanlinePatternMatch? MatchLeftDigitAt(
        BitRow row,
        int position,
        bool allowG)
    {
        if (!CanStartPattern(row, position, false))
        {
            return null;
        }

        int[] observed = RunLength.ExtractForward(row, position, EanUpcPatterns.DigitElementCount);
        if (observed.Length != EanUpcPatterns.DigitElementCount)
        {
            return null;
        }

        int bestDigit = -1;
        EanUpcDigitSet bestSet = EanUpcDigitSet.L;
        double bestVariance = MaximumAverageVariance;
        int maximumSet = allowG ? 1 : 0;
        int setValue = 0;
        while (setValue <= maximumSet)
        {
            EanUpcDigitSet digitSet = (EanUpcDigitSet)setValue;
            int digit = 0;
            while (digit < EanUpcPatterns.DigitCount)
            {
                int[] expected = EanUpcPatterns.GetDigitWidths(digit, digitSet);
                double variance = PatternMatcher.CalculateNormalizedVariance(
                    observed,
                    expected,
                    MaximumIndividualVariance);
                if (variance < bestVariance)
                {
                    bestDigit = digit;
                    bestSet = digitSet;
                    bestVariance = variance;
                }

                digit = digit + 1;
            }

            setValue = setValue + 1;
        }

        if (bestDigit < 0)
        {
            return null;
        }

        return CreatePatternMatch(bestDigit, bestSet, position, observed, bestVariance);
    }

    static EanUpcScanlinePatternMatch? MatchRightDigitAt(BitRow row, int position)
    {
        if (!CanStartPattern(row, position, true))
        {
            return null;
        }

        int[] observed = RunLength.ExtractForward(row, position, EanUpcPatterns.DigitElementCount);
        if (observed.Length != EanUpcPatterns.DigitElementCount)
        {
            return null;
        }

        int bestDigit = -1;
        double bestVariance = MaximumAverageVariance;
        int digit = 0;
        while (digit < EanUpcPatterns.DigitCount)
        {
            int[] expected = EanUpcPatterns.GetDigitWidths(digit, EanUpcDigitSet.R);
            double variance = PatternMatcher.CalculateNormalizedVariance(
                observed,
                expected,
                MaximumIndividualVariance);
            if (variance < bestVariance)
            {
                bestDigit = digit;
                bestVariance = variance;
            }

            digit = digit + 1;
        }

        if (bestDigit < 0)
        {
            return null;
        }

        return CreatePatternMatch(
            bestDigit,
            EanUpcDigitSet.R,
            position,
            observed,
            bestVariance);
    }

    static EanUpcScanlinePatternMatch? MatchGuardAt(
        BitRow row,
        int position,
        EanUpcGuardPattern pattern)
    {
        int[] expected = GetGuardWidths(pattern);
        bool startsDark = pattern == EanUpcGuardPattern.Normal;
        if (!CanStartPattern(row, position, startsDark))
        {
            return null;
        }

        int[] observed = RunLength.ExtractForward(row, position, expected.Length);
        if (observed.Length != expected.Length)
        {
            return null;
        }

        double variance = PatternMatcher.CalculateNormalizedVariance(
            observed,
            expected,
            MaximumIndividualVariance);
        if (variance >= MaximumAverageVariance)
        {
            return null;
        }

        return CreatePatternMatch(-1, EanUpcDigitSet.L, position, observed, variance);
    }

    static int[] GetGuardWidths(EanUpcGuardPattern pattern)
    {
        switch (pattern)
        {
            case EanUpcGuardPattern.Normal:
                return new int[] { 1, 1, 1 };
            case EanUpcGuardPattern.Center:
                return new int[] { 1, 1, 1, 1, 1 };
            case EanUpcGuardPattern.UpcEEnd:
                return new int[] { 1, 1, 1, 1, 1, 1 };
            default:
                throw new BarcodeValidationException("EAN/UPC scanline guard pattern is invalid.");
        }
    }

    static bool CanStartPattern(BitRow row, int position, bool startsDark)
    {
        if (position < 0 || position >= row.Length || row.ReadBitUnchecked(position) != startsDark)
        {
            return false;
        }

        return position == 0 || row.ReadBitUnchecked(position - 1) != startsDark;
    }

    static EanUpcScanlinePatternMatch CreatePatternMatch(
        int digit,
        EanUpcDigitSet digitSet,
        int position,
        int[] observed,
        double variance)
    {
        int width = PatternMatcher.SumPositiveRuns(observed);
        EanUpcScanlinePatternMatch match = new EanUpcScanlinePatternMatch();
        match.Digit = digit;
        match.DigitSet = digitSet;
        match.EndPosition = CheckedMath.Add(position, width);
        match.Variance = variance;
        return match;
    }

    static int FindEan13LeadingDigit(EanUpcDigitSet[] parity, int count)
    {
        if (count != EanUpcPatterns.Ean13ParityLength)
        {
            return -1;
        }

        int leadingDigit = 0;
        while (leadingDigit < EanUpcPatterns.DigitCount)
        {
            EanUpcDigitSet[] expected = EanUpcPatterns.GetEan13Parity(leadingDigit);
            if (ParityEquals(parity, count, expected))
            {
                return leadingDigit;
            }

            leadingDigit = leadingDigit + 1;
        }

        return -1;
    }

    static int[] FindUpcENumberSystemAndCheck(EanUpcDigitSet[] parity, int count)
    {
        if (count != EanUpcPatterns.UpcEParityLength)
        {
            return new int[0];
        }

        int numberSystem = 0;
        while (numberSystem <= 1)
        {
            int checkDigit = 0;
            while (checkDigit < EanUpcPatterns.DigitCount)
            {
                EanUpcDigitSet[] expected = EanUpcPatterns.GetUpcEParity(numberSystem, checkDigit);
                if (ParityEquals(parity, count, expected))
                {
                    return new int[] { numberSystem, checkDigit };
                }

                checkDigit = checkDigit + 1;
            }

            numberSystem = numberSystem + 1;
        }

        return new int[0];
    }

    static bool ParityEquals(
        EanUpcDigitSet[] observed,
        int observedCount,
        EanUpcDigitSet[] expected)
    {
        if (observedCount != expected.Length)
        {
            return false;
        }

        int index = 0;
        while (index < observedCount)
        {
            if (observed[index] != expected[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static bool HasMinimumQuietZones(
        BitRow row,
        int startPosition,
        int endPosition,
        double moduleSize,
        int leftModules,
        int rightModules)
    {
        int leftQuietZone = CountLightBefore(row, startPosition);
        int rightQuietZone = CountLightAfter(row, endPosition);
        return (double)leftQuietZone >= leftModules * moduleSize &&
            (double)rightQuietZone >= rightModules * moduleSize;
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

    static EanUpcDecodedData CreateDecodedData(
        BarcodeFormat format,
        string text,
        int[] digits,
        EanUpcDigitSet[] parity,
        int parityCount,
        int startPosition,
        int endPosition,
        double moduleSize,
        double averageVariance)
    {
        EanUpcDecodedData decoded = new EanUpcDecodedData();
        decoded.Format = format;
        decoded.Text = text;
        decoded.Digits = CopyDigitRange(digits, 0, digits.Length);
        decoded.LeftParity = CopyParity(parity, parityCount);
        decoded.ChecksumValue = digits[digits.Length - 1];
        decoded.StartPosition = startPosition;
        decoded.EndPosition = endPosition;
        decoded.ModuleSize = moduleSize;
        decoded.AverageVariance = averageVariance;
        decoded.Result = CreatePublicResult(format, text, decoded.Digits, decoded.ChecksumValue, averageVariance);
        return decoded;
    }

    static BarcodeDecodeResult CreatePublicResult(
        BarcodeFormat format,
        string text,
        int[] digits,
        int checksumValue,
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
        payload.IsGs1 = true;

        BarcodeDecodeMetadata metadata = new BarcodeDecodeMetadata();
        metadata.HasChecksum = true;
        metadata.ChecksumValid = true;
        metadata.ChecksumValue = checksumValue;
        metadata.SymbologyIdentifier = BarcodeSymbologyIdentifiers.EanUpc;
        metadata.Codewords = CopyDigitRange(digits, 0, digits.Length);
        metadata.Confidence = CalculateConfidence(averageVariance);
        metadata.IsGs1 = true;

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

    static string ConvertDigitsToText(int[] digits)
    {
        char[] characters = new char[digits.Length];
        int index = 0;
        while (index < digits.Length)
        {
            characters[index] = (char)('0' + digits[index]);
            index = index + 1;
        }

        return new string(characters);
    }

    static string ExpandUpcEData(string upcE)
    {
        char lastCompressedDigit = upcE[6];
        switch (lastCompressedDigit)
        {
            case '0':
            case '1':
            case '2':
                return upcE[0] + upcE.Substring(1, 2) + lastCompressedDigit + "0000" + upcE.Substring(3, 3);
            case '3':
                return upcE[0] + upcE.Substring(1, 3) + "00000" + upcE.Substring(4, 2);
            case '4':
                return upcE[0] + upcE.Substring(1, 4) + "00000" + upcE[5];
            default:
                return upcE[0] + upcE.Substring(1, 5) + "0000" + lastCompressedDigit;
        }
    }

    static void CopyDigitsInto(int[] source, int count, int[] destination, int offset)
    {
        int index = 0;
        while (index < count)
        {
            destination[offset + index] = source[index];
            index = index + 1;
        }
    }

    static int[] CopyDigitRange(int[] source, int start, int count)
    {
        int[] copy = new int[count];
        int index = 0;
        while (index < count)
        {
            copy[index] = source[start + index];
            index = index + 1;
        }

        return copy;
    }

    static EanUpcDigitSet[] CopyParity(EanUpcDigitSet[] source, int count)
    {
        EanUpcDigitSet[] copy = new EanUpcDigitSet[count];
        int index = 0;
        while (index < count)
        {
            copy[index] = source[index];
            index = index + 1;
        }

        return copy;
    }

    internal static BitRow CreateReversedRow(BitRow row)
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
