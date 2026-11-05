namespace barcode;

internal class EanUpcAddonDecodedData
{
    internal BarcodeDecodeResult Result = new BarcodeDecodeResult();
    internal BarcodeFormat Format;
    internal string Text = "";
    internal int[] Digits = new int[0];
    internal EanUpcDigitSet[] Parity = new EanUpcDigitSet[0];
    internal int ChecksumValue;
    internal bool IsReversed;
    internal int StartPosition;
    internal int EndPosition;
    internal double ModuleSize;
    internal double AverageVariance;
}

internal class EanUpcDecodedAssociation
{
    internal EanUpcDecodedData Main = new EanUpcDecodedData();
    internal EanUpcAddonDecodedData? AddOn;
    internal int SeparationModules;
}

internal class EanUpcAddonDecodeState
{
    internal int[] Digits = new int[EanUpcAddon.FiveDigitLength];
    internal EanUpcDigitSet[] Parity = new EanUpcDigitSet[EanUpcAddon.FiveDigitLength];
    internal int Position;
    internal double TotalVariance;
    internal int PatternCount;
}

internal static class EanUpcAddonScanlineDecoder
{
    internal const int StandaloneLeftQuietZoneModules =
        EanUpcLayout.AddOnLeftQuietZoneModules;
    internal const int StandardRightQuietZoneModules = 5;
    internal const int MaximumSeparationModules = 12;
    internal const double MaximumModuleDifferenceRatio = 0.25;

    internal static EanUpcAddonDecodedData DecodeRow(BitRow row, EanUpcOptions? options)
    {
        EanUpcAddonDecodedData? decoded = TryDecodeRow(row, options);
        if (decoded == null)
        {
            throw new BarcodeDecodingException("The scanline does not contain a valid EAN/UPC add-on symbol.");
        }

        return decoded;
    }

    internal static EanUpcAddonDecodedData? TryDecodeRow(BitRow row, EanUpcOptions? options)
    {
        if (row == null)
        {
            throw new BarcodeValidationException("EAN/UPC add-on scanline must not be null.");
        }

        int[]? allowedLengths = options == null ? null : options.AllowedAddOnLengths;
        ValidateAllowedLengths(allowedLengths);
        EanUpcAddonDecodedData? forward = TryDecodeStandaloneForward(row, allowedLengths);
        if (forward != null)
        {
            forward.IsReversed = false;
            return forward;
        }

        BitRow reversedRow = EanUpcScanlineDecoder.CreateReversedRow(row);
        EanUpcAddonDecodedData? reversed = TryDecodeStandaloneForward(reversedRow, allowedLengths);
        if (reversed == null)
        {
            return null;
        }

        MapReversedGeometry(reversed, row.Length);
        return reversed;
    }

    internal static EanUpcDecodedAssociation DecodeAssociation(
        BitRow row,
        EanUpcOptions? options)
    {
        EanUpcDecodedAssociation? association = TryDecodeAssociation(row, options);
        if (association == null)
        {
            throw new BarcodeDecodingException("The scanline does not contain a valid EAN/UPC main and add-on association.");
        }

        return association;
    }

    internal static EanUpcDecodedAssociation? TryDecodeAssociation(
        BitRow row,
        EanUpcOptions? options)
    {
        if (row == null)
        {
            throw new BarcodeValidationException("EAN/UPC association scanline must not be null.");
        }

        int[]? allowedLengths = options == null ? null : options.AllowedAddOnLengths;
        ValidateAllowedLengths(allowedLengths);
        EanUpcDecodedData? main = EanUpcScanlineDecoder.TryDecodeRow(row);
        if (main == null)
        {
            return null;
        }

        bool requireAddOn = options != null && options.RequireAddOn;
        if (main.Format == BarcodeFormat.Ean8)
        {
            if (requireAddOn)
            {
                return null;
            }

            return CreateAssociationResult(main, null, 0);
        }

        if (!main.IsReversed)
        {
            EanUpcAddonDecodedData? forwardAddOn = TryDecodeAfterMain(
                row,
                main,
                allowedLengths);
            if (forwardAddOn == null && requireAddOn)
            {
                return null;
            }

            int forwardSeparation = forwardAddOn == null
                ? 0
                : ResolveSeparationModules(main, forwardAddOn);
            return CreateAssociationResult(main, forwardAddOn, forwardSeparation);
        }

        BitRow reversedRow = EanUpcScanlineDecoder.CreateReversedRow(row);
        EanUpcDecodedData? forwardMain = EanUpcScanlineDecoder.TryDecodeRow(reversedRow);
        if (forwardMain == null || forwardMain.IsReversed)
        {
            return null;
        }

        EanUpcAddonDecodedData? reversedAddOn = TryDecodeAfterMain(
            reversedRow,
            forwardMain,
            allowedLengths);
        if (reversedAddOn == null)
        {
            if (requireAddOn)
            {
                return null;
            }

            return CreateAssociationResult(main, null, 0);
        }

        int separation = ResolveSeparationModules(forwardMain, reversedAddOn);
        MapReversedGeometry(reversedAddOn, row.Length);
        return CreateAssociationResult(main, reversedAddOn, separation);
    }

    static EanUpcAddonDecodedData? TryDecodeStandaloneForward(
        BitRow row,
        int[]? allowedLengths)
    {
        int position = 0;
        while (position < row.Length)
        {
            bool dark = row.ReadBitUnchecked(position);
            bool atRunStart = position == 0 || row.ReadBitUnchecked(position - 1) != dark;
            if (dark && atRunStart)
            {
                EanUpcAddonDecodedData? decoded = TryAllowedLengthsAt(
                    row,
                    position,
                    allowedLengths,
                    StandaloneLeftQuietZoneModules);
                if (decoded != null)
                {
                    return decoded;
                }
            }

            position = position + 1;
        }

        return null;
    }

    static EanUpcAddonDecodedData? TryDecodeAfterMain(
        BitRow row,
        EanUpcDecodedData main,
        int[]? allowedLengths)
    {
        int addOnStart = FindNextDark(row, main.EndPosition);
        if (addOnStart < 0)
        {
            return null;
        }

        int separationPixels = addOnStart - main.EndPosition;
        int minimumModules = main.Format == BarcodeFormat.UpcA
            ? EanUpcLayout.UpcAAddOnSeparationModules
            : EanUpcLayout.StandardAddOnSeparationModules;
        if ((double)separationPixels < minimumModules * main.ModuleSize ||
            (double)separationPixels > MaximumSeparationModules * main.ModuleSize)
        {
            return null;
        }

        EanUpcAddonDecodedData? addOn = TryAllowedLengthsAt(
            row,
            addOnStart,
            allowedLengths,
            0);
        if (addOn == null)
        {
            return null;
        }

        double difference = global::System.Math.Abs(addOn.ModuleSize - main.ModuleSize);
        if (difference > main.ModuleSize * MaximumModuleDifferenceRatio)
        {
            return null;
        }

        return addOn;
    }

    static EanUpcAddonDecodedData? TryAllowedLengthsAt(
        BitRow row,
        int position,
        int[]? allowedLengths,
        int leftQuietZoneModules)
    {
        if (IsLengthAllowed(EanUpcAddon.FiveDigitLength, allowedLengths))
        {
            EanUpcAddonDecodedData? fiveDigit = TryDecodeAt(
                row,
                position,
                EanUpcAddon.FiveDigitLength,
                leftQuietZoneModules);
            if (fiveDigit != null)
            {
                return fiveDigit;
            }
        }

        if (IsLengthAllowed(EanUpcAddon.TwoDigitLength, allowedLengths))
        {
            return TryDecodeAt(
                row,
                position,
                EanUpcAddon.TwoDigitLength,
                leftQuietZoneModules);
        }

        return null;
    }

    static EanUpcAddonDecodedData? TryDecodeAt(
        BitRow row,
        int startPosition,
        int digitCount,
        int leftQuietZoneModules)
    {
        EanUpcScanlinePatternMatch? start = MatchAddOnStartAt(row, startPosition);
        if (start == null)
        {
            return null;
        }

        EanUpcAddonDecodeState state = new EanUpcAddonDecodeState();
        state.Position = start.EndPosition;
        state.TotalVariance = start.Variance;
        state.PatternCount = 1;
        int digitIndex = 0;
        while (digitIndex < digitCount)
        {
            if (digitIndex > 0)
            {
                EanUpcScanlinePatternMatch? separator = MatchAddOnSeparatorAt(row, state.Position);
                if (separator == null)
                {
                    return null;
                }

                AddPatternToState(state, separator);
            }

            EanUpcScanlinePatternMatch? digit = EanUpcScanlineDecoder.MatchLeftDigitAt(
                row,
                state.Position,
                true);
            if (digit == null)
            {
                return null;
            }

            state.Digits[digitIndex] = digit.Digit;
            state.Parity[digitIndex] = digit.DigitSet;
            AddPatternToState(state, digit);
            digitIndex = digitIndex + 1;
        }

        int moduleCount = digitCount == EanUpcAddon.TwoDigitLength
            ? EanUpcAddon.TwoDigitModuleCount
            : EanUpcAddon.FiveDigitModuleCount;
        double moduleSize = (state.Position - startPosition) / (double)moduleCount;
        if (!HasQuietZones(
                row,
                startPosition,
                state.Position,
                moduleSize,
                leftQuietZoneModules,
                StandardRightQuietZoneModules) ||
            !HasValidParity(state.Digits, state.Parity, digitCount))
        {
            return null;
        }

        double averageVariance = state.TotalVariance / state.PatternCount;
        if (averageVariance >= EanUpcScanlineDecoder.MaximumAverageVariance)
        {
            return null;
        }

        return CreateDecodedData(
            state,
            digitCount,
            startPosition,
            moduleSize,
            averageVariance);
    }

    static EanUpcScanlinePatternMatch? MatchAddOnStartAt(BitRow row, int position)
    {
        return MatchAddOnGuardAt(row, position, new int[] { 1, 1, 2 }, true);
    }

    static EanUpcScanlinePatternMatch? MatchAddOnSeparatorAt(BitRow row, int position)
    {
        return MatchAddOnGuardAt(row, position, new int[] { 1, 1 }, false);
    }

    static EanUpcScanlinePatternMatch? MatchAddOnGuardAt(
        BitRow row,
        int position,
        int[] expected,
        bool startsDark)
    {
        if (position < 0 || position >= row.Length ||
            row.ReadBitUnchecked(position) != startsDark ||
            (position > 0 && row.ReadBitUnchecked(position - 1) == startsDark))
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
            EanUpcScanlineDecoder.MaximumIndividualVariance);
        if (variance >= EanUpcScanlineDecoder.MaximumAverageVariance)
        {
            return null;
        }

        int width = PatternMatcher.SumPositiveRuns(observed);
        EanUpcScanlinePatternMatch match = new EanUpcScanlinePatternMatch();
        match.Digit = -1;
        match.DigitSet = EanUpcDigitSet.L;
        match.EndPosition = CheckedMath.Add(position, width);
        match.Variance = variance;
        return match;
    }

    static void AddPatternToState(
        EanUpcAddonDecodeState state,
        EanUpcScanlinePatternMatch match)
    {
        state.Position = match.EndPosition;
        state.TotalVariance = state.TotalVariance + match.Variance;
        state.PatternCount = state.PatternCount + 1;
    }

    static bool HasValidParity(
        int[] digits,
        EanUpcDigitSet[] observedParity,
        int digitCount)
    {
        EanUpcDigitSet[] expectedParity;
        if (digitCount == EanUpcAddon.TwoDigitLength)
        {
            int value = (digits[0] * 10) + digits[1];
            expectedParity = EanUpcPatterns.GetAddon2Parity(value % 4);
        }
        else
        {
            int checksum = CalculateFiveDigitChecksum(digits);
            expectedParity = EanUpcPatterns.GetAddon5Parity(checksum);
        }

        int index = 0;
        while (index < digitCount)
        {
            if (observedParity[index] != expectedParity[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static int CalculateFiveDigitChecksum(int[] digits)
    {
        int oddPositionSum = digits[0] + digits[2] + digits[4];
        int evenPositionSum = digits[1] + digits[3];
        return ((oddPositionSum * 3) + (evenPositionSum * 9)) % 10;
    }

    static EanUpcAddonDecodedData CreateDecodedData(
        EanUpcAddonDecodeState state,
        int digitCount,
        int startPosition,
        double moduleSize,
        double averageVariance)
    {
        int[] digits = CopyDigits(state.Digits, digitCount);
        EanUpcDigitSet[] parity = CopyParity(state.Parity, digitCount);
        string text = ConvertDigitsToText(digits);
        BarcodeFormat format = digitCount == EanUpcAddon.TwoDigitLength
            ? BarcodeFormat.EanUpcAddon2
            : BarcodeFormat.EanUpcAddon5;
        int checksumValue = digitCount == EanUpcAddon.TwoDigitLength
            ? ((digits[0] * 10) + digits[1]) % 4
            : CalculateFiveDigitChecksum(digits);

        EanUpcAddonDecodedData decoded = new EanUpcAddonDecodedData();
        decoded.Format = format;
        decoded.Text = text;
        decoded.Digits = digits;
        decoded.Parity = parity;
        decoded.ChecksumValue = checksumValue;
        decoded.StartPosition = startPosition;
        decoded.EndPosition = state.Position;
        decoded.ModuleSize = moduleSize;
        decoded.AverageVariance = averageVariance;
        decoded.Result = CreatePublicResult(
            format,
            text,
            digits,
            checksumValue,
            averageVariance);
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
        metadata.Codewords = CopyDigits(digits, digits.Length);
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

    static EanUpcDecodedAssociation CreateAssociationResult(
        EanUpcDecodedData main,
        EanUpcAddonDecodedData? addOn,
        int separationModules)
    {
        EanUpcDecodedAssociation association = new EanUpcDecodedAssociation();
        association.Main = main;
        association.AddOn = addOn;
        association.SeparationModules = separationModules;
        return association;
    }

    static void MapReversedGeometry(EanUpcAddonDecodedData decoded, int rowLength)
    {
        int originalStart = rowLength - decoded.EndPosition;
        int originalEnd = rowLength - decoded.StartPosition;
        decoded.StartPosition = originalStart;
        decoded.EndPosition = originalEnd;
        decoded.IsReversed = true;
        decoded.Result.Rotation = BarcodeRotation.Clockwise180;
        decoded.Result.OrientationDegrees = 180.0;
    }

    static int ResolveSeparationModules(
        EanUpcDecodedData main,
        EanUpcAddonDecodedData addOn)
    {
        double separation = (addOn.StartPosition - main.EndPosition) / main.ModuleSize;
        return (int)(separation + 0.5);
    }

    static int FindNextDark(BitRow row, int position)
    {
        int index = position;
        while (index < row.Length)
        {
            if (row.ReadBitUnchecked(index))
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    static bool HasQuietZones(
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

    static void ValidateAllowedLengths(int[]? allowedLengths)
    {
        if (allowedLengths == null)
        {
            return;
        }

        int index = 0;
        while (index < allowedLengths.Length)
        {
            int length = allowedLengths[index];
            if (length != EanUpcAddon.TwoDigitLength &&
                length != EanUpcAddon.FiveDigitLength)
            {
                throw new BarcodeValidationException("Allowed EAN/UPC add-on lengths may contain only two and five.");
            }

            index = index + 1;
        }
    }

    static bool IsLengthAllowed(int length, int[]? allowedLengths)
    {
        if (allowedLengths == null)
        {
            return true;
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

    static double CalculateConfidence(double averageVariance)
    {
        double confidence = 1.0 -
            (averageVariance / EanUpcScanlineDecoder.MaximumAverageVariance);
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

    static int[] CopyDigits(int[] source, int count)
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
}
