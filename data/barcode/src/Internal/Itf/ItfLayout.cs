namespace barcode;

internal class ItfLayoutResult
{
    internal BarcodeSymbol Symbol = new BarcodeSymbol();
    internal ItfEncodedData Encoded = new ItfEncodedData();
    internal double WideToNarrowRatio;
    internal double SymbolWidthModules;
    internal double[] ElementWidths = new double[0];
    internal bool[] ElementIsDark = new bool[0];
}

internal class ItfElementLayout
{
    internal double[] Widths = new double[0];
    internal bool[] IsDark = new bool[0];
    internal double WidthModules;
}

internal static class ItfLayout
{
    internal const int StandardQuietZoneModules = 10;
    internal const double DefaultWideToNarrowRatio = 3.0;
    internal const double HumanReadableHeight = 1.0;
    internal const double PhysicalComparisonTolerance = 0.000001;

    internal static ItfLayoutResult CreateLayout(
        string text,
        BarcodePayload payload,
        ItfOptions? itfOptions,
        BarcodeEncodeOptions? encodeOptions)
    {
        if (payload == null)
        {
            throw new BarcodeEncodingException("ITF payload must not be null.");
        }

        ItfOptions settings = itfOptions == null ? new ItfOptions() : itfOptions;
        BarcodeEncodeOptions common = encodeOptions == null ? new BarcodeEncodeOptions() : encodeOptions;
        ValidateGenericProfileOptions(settings);
        BarcodeChecksumMode checksumMode = itfOptions == null
            ? common.ChecksumMode
            : settings.ChecksumMode;
        ItfEncodedData encoded = ItfEncoder.Encode(text, checksumMode, settings.AllowedLengths);
        double ratio = ResolveRatio(settings.WideToNarrowRatio);
        ItfElementLayout elements = CreateElementLayout(encoded.Digits, ratio);
        BarcodeQuietZone requiredQuietZone = CreateRequiredQuietZone();
        BarcodeQuietZone selectedQuietZone = ResolveQuietZone(common, requiredQuietZone);
        BarcodeSize size = ResolveSize(elements.WidthModules, common.Sizing, selectedQuietZone);
        BarcodeTextLayout? textLayout = CreateTextLayout(
            encoded.TransmittedText,
            payload,
            common,
            size,
            selectedQuietZone,
            elements.WidthModules);

        BarcodeSymbol symbol = new BarcodeSymbol();
        symbol.Format = BarcodeFormat.Itf;
        symbol.Dimension = BarcodeDimension.Linear;
        symbol.WidthModules = RoundWidthToModules(elements.WidthModules);
        symbol.HeightModules = 1;
        symbol.Modules = CreateIntegralModuleRow(elements.Widths, elements.IsDark);
        symbol.Payload = ResultCopier.CopyPayload(payload);
        symbol.Size = size;
        symbol.RequiredQuietZone = requiredQuietZone;
        symbol.TextLayout = textLayout;
        symbol.SymbologyIdentifier = ResolveSymbologyIdentifier(encoded);
        symbol.Codewords = ResultCopier.CopyIntArray(encoded.Digits);
        symbol.HasChecksum = encoded.HasChecksum;
        symbol.ChecksumValue = encoded.ChecksumValue;

        ItfLayoutResult result = new ItfLayoutResult();
        result.Symbol = symbol;
        result.Encoded = encoded;
        result.WideToNarrowRatio = ratio;
        result.SymbolWidthModules = elements.WidthModules;
        result.ElementWidths = elements.Widths;
        result.ElementIsDark = elements.IsDark;
        return result;
    }

    static void ValidateGenericProfileOptions(ItfOptions options)
    {
        if (options.UseBearerBars || options.UseBearerBox || options.UseCorrugatedProfile ||
            options.BearerBarThicknessModules != 0)
        {
            throw new BarcodeEncodingException("Bearer bars and corrugated sizing require the ITF-14 profile.");
        }
    }

    static double ResolveRatio(double ratio)
    {
        if (ratio == 0.0)
        {
            return DefaultWideToNarrowRatio;
        }

        if (double.IsNaN(ratio) || double.IsInfinity(ratio) || ratio < 2.0 || ratio > 3.0)
        {
            throw new BarcodeEncodingException("ITF wide-to-narrow ratio must be from 2.0 through 3.0.");
        }

        return ratio;
    }

    static ItfElementLayout CreateElementLayout(int[] digits, double ratio)
    {
        int pairCount = digits.Length / 2;
        int pairElements = CheckedMath.Multiply(pairCount, ItfPatterns.PairElementCount);
        int framedElements = CheckedMath.Add(ItfPatterns.StartElementCount, ItfPatterns.StopElementCount);
        int elementCount = CheckedMath.Add(pairElements, framedElements);
        double[] widths = new double[elementCount];
        bool[] isDark = new bool[elementCount];
        int target = 0;
        while (target < ItfPatterns.StartElementCount)
        {
            widths[target] = 1.0;
            isDark[target] = target % 2 == 0;
            target = target + 1;
        }

        int pairIndex = 0;
        while (pairIndex < pairCount)
        {
            bool[] barWide = ItfPatterns.GetDigitWideElements(digits[pairIndex * 2]);
            bool[] spaceWide = ItfPatterns.GetDigitWideElements(digits[(pairIndex * 2) + 1]);
            int digitElement = 0;
            while (digitElement < ItfPatterns.DigitElementCount)
            {
                widths[target] = barWide[digitElement] ? ratio : 1.0;
                isDark[target] = true;
                target = target + 1;
                widths[target] = spaceWide[digitElement] ? ratio : 1.0;
                isDark[target] = false;
                target = target + 1;
                digitElement = digitElement + 1;
            }

            pairIndex = pairIndex + 1;
        }

        widths[target] = ratio;
        isDark[target] = true;
        target = target + 1;
        widths[target] = 1.0;
        isDark[target] = false;
        target = target + 1;
        widths[target] = 1.0;
        isDark[target] = true;

        double totalWidth = 0.0;
        int index = 0;
        while (index < widths.Length)
        {
            totalWidth = totalWidth + widths[index];
            index = index + 1;
        }

        if (double.IsInfinity(totalWidth))
        {
            throw new BarcodeEncodingException("ITF layout width exceeds the supported range.");
        }

        ItfElementLayout layout = new ItfElementLayout();
        layout.Widths = widths;
        layout.IsDark = isDark;
        layout.WidthModules = totalWidth;
        return layout;
    }

    static byte[]? CreateIntegralModuleRow(double[] widths, bool[] isDark)
    {
        int totalWidth = 0;
        int index = 0;
        while (index < widths.Length)
        {
            double rounded = global::System.Math.Round(widths[index]);
            if (widths[index] != rounded)
            {
                return null;
            }

            totalWidth = CheckedMath.Add(totalWidth, (int)rounded);
            index = index + 1;
        }

        byte[] modules = new byte[totalWidth];
        int target = 0;
        index = 0;
        while (index < widths.Length)
        {
            int count = (int)widths[index];
            byte value = isDark[index] ? (byte)1 : (byte)0;
            int offset = 0;
            while (offset < count)
            {
                modules[target] = value;
                target = target + 1;
                offset = offset + 1;
            }

            index = index + 1;
        }

        return modules;
    }

    static int RoundWidthToModules(double width)
    {
        double rounded = global::System.Math.Ceiling(width);
        if (rounded > int.MaxValue)
        {
            throw new BarcodeEncodingException("ITF logical width exceeds the supported range.");
        }

        return (int)rounded;
    }

    static string ResolveSymbologyIdentifier(ItfEncodedData encoded)
    {
        if (!encoded.HasChecksum)
        {
            return BarcodeSymbologyIdentifiers.Itf;
        }

        return encoded.ChecksumMode == BarcodeChecksumMode.VerifyStrip ? "]I3" : "]I1";
    }

    static BarcodeQuietZone CreateRequiredQuietZone()
    {
        BarcodeQuietZone quietZone = new BarcodeQuietZone();
        quietZone.LeftModules = StandardQuietZoneModules;
        quietZone.RightModules = StandardQuietZoneModules;
        return quietZone;
    }

    static BarcodeQuietZone ResolveQuietZone(
        BarcodeEncodeOptions options,
        BarcodeQuietZone requiredQuietZone)
    {
        switch (options.QuietZonePolicy)
        {
            case BarcodeQuietZonePolicy.Standard:
                return CopyLogicalQuietZone(requiredQuietZone);
            case BarcodeQuietZonePolicy.CustomAtLeastStandard:
                BarcodeQuietZone minimumCustom = RequireCustomQuietZone(options);
                ValidateQuietZone(minimumCustom);
                if (minimumCustom.LeftModules < StandardQuietZoneModules ||
                    minimumCustom.RightModules < StandardQuietZoneModules)
                {
                    throw new BarcodeEncodingException("Custom ITF quiet zones must be at least 10 modules on the left and right.");
                }

                return CopyLogicalQuietZone(minimumCustom);
            case BarcodeQuietZonePolicy.ExactCustom:
                BarcodeQuietZone exactCustom = RequireCustomQuietZone(options);
                ValidateQuietZone(exactCustom);
                return CopyLogicalQuietZone(exactCustom);
            default:
                throw new BarcodeEncodingException("ITF quiet-zone policy is invalid.");
        }
    }

    static BarcodeQuietZone RequireCustomQuietZone(BarcodeEncodeOptions options)
    {
        BarcodeQuietZone? custom = options.QuietZone;
        if (custom == null && options.Sizing != null)
        {
            custom = options.Sizing.QuietZone;
        }

        if (custom == null)
        {
            throw new BarcodeEncodingException("A custom ITF quiet zone is required by the selected policy.");
        }

        return custom;
    }

    static void ValidateQuietZone(BarcodeQuietZone quietZone)
    {
        if (quietZone.LeftModules < 0 || quietZone.TopModules < 0 ||
            quietZone.RightModules < 0 || quietZone.BottomModules < 0 ||
            quietZone.LeftPixels < 0 || quietZone.TopPixels < 0 ||
            quietZone.RightPixels < 0 || quietZone.BottomPixels < 0)
        {
            throw new BarcodeEncodingException("ITF quiet-zone dimensions must not be negative.");
        }
    }

    static BarcodeQuietZone CopyLogicalQuietZone(BarcodeQuietZone source)
    {
        BarcodeQuietZone copy = new BarcodeQuietZone();
        copy.LeftModules = source.LeftModules;
        copy.TopModules = source.TopModules;
        copy.RightModules = source.RightModules;
        copy.BottomModules = source.BottomModules;
        return copy;
    }

    static BarcodeSize ResolveSize(
        double symbolWidthModules,
        BarcodeSizing? sizing,
        BarcodeQuietZone quietZone)
    {
        BarcodeSizing settings = sizing == null ? new BarcodeSizing() : sizing;
        int roundedSymbolWidth = RoundWidthToModules(symbolWidthModules);
        ValidateSizing(settings, roundedSymbolWidth);
        int horizontalQuietZone = CheckedMath.Add(quietZone.LeftModules, quietZone.RightModules);
        double physicalWidthModules = symbolWidthModules + horizontalQuietZone;
        int selectedWidthModules = RoundWidthToModules(physicalWidthModules);
        int verticalQuietZone = CheckedMath.Add(quietZone.TopModules, quietZone.BottomModules);
        int physicalHeightModules = CheckedMath.Add(1, verticalQuietZone);

        double moduleSize = settings.ModuleSizeMillimetres;
        if (moduleSize == 0.0 && settings.RequestedWidthMillimetres > 0.0)
        {
            moduleSize = settings.RequestedWidthMillimetres / physicalWidthModules;
        }

        double widthMillimetres = 0.0;
        if (moduleSize > 0.0)
        {
            widthMillimetres = physicalWidthModules * moduleSize;
            if (double.IsInfinity(widthMillimetres))
            {
                throw new BarcodeEncodingException("Resolved ITF width exceeds the supported physical range.");
            }
        }

        if (settings.RequestedWidthMillimetres > 0.0 &&
            global::System.Math.Abs(widthMillimetres - settings.RequestedWidthMillimetres) > PhysicalComparisonTolerance)
        {
            throw new BarcodeEncodingException("Requested ITF width conflicts with the module size and quiet zones.");
        }

        double barHeight = settings.BarHeightMillimetres;
        double verticalQuietZoneMillimetres = verticalQuietZone * moduleSize;
        if (settings.RequestedHeightMillimetres > 0.0 && verticalQuietZone > 0 && moduleSize == 0.0)
        {
            throw new BarcodeEncodingException("An ITF module size is required to resolve physical vertical quiet zones.");
        }

        if (barHeight == 0.0 && settings.RequestedHeightMillimetres > 0.0)
        {
            barHeight = settings.RequestedHeightMillimetres - verticalQuietZoneMillimetres;
            if (barHeight <= 0.0)
            {
                throw new BarcodeEncodingException("Requested ITF height does not leave a positive bar height.");
            }
        }

        double heightMillimetres = 0.0;
        if (barHeight > 0.0)
        {
            heightMillimetres = barHeight + verticalQuietZoneMillimetres;
            if (double.IsInfinity(heightMillimetres))
            {
                throw new BarcodeEncodingException("Resolved ITF height exceeds the supported physical range.");
            }
        }

        if (settings.RequestedHeightMillimetres > 0.0 &&
            global::System.Math.Abs(heightMillimetres - settings.RequestedHeightMillimetres) > PhysicalComparisonTolerance)
        {
            throw new BarcodeEncodingException("Requested ITF height conflicts with the bar height and quiet zones.");
        }

        BarcodeSize size = new BarcodeSize();
        size.WidthModules = selectedWidthModules;
        size.HeightModules = physicalHeightModules;
        size.WidthPixels = settings.RequestedWidthPixels;
        size.HeightPixels = settings.RequestedHeightPixels;
        size.WidthMillimetres = widthMillimetres;
        size.HeightMillimetres = heightMillimetres;
        size.DotsPerInch = settings.DotsPerInch;
        size.ModuleSizeMillimetres = moduleSize;
        size.QuietZone = CopyLogicalQuietZone(quietZone);
        return size;
    }

    static void ValidateSizing(BarcodeSizing sizing, int symbolWidthModules)
    {
        if (sizing.RequestedWidthModules < 0 || sizing.RequestedHeightModules < 0 ||
            sizing.RequestedWidthPixels < 0 || sizing.RequestedHeightPixels < 0)
        {
            throw new BarcodeEncodingException("ITF sizing dimensions must not be negative.");
        }

        if (sizing.RequestedWidthModules != 0 && sizing.RequestedWidthModules != symbolWidthModules)
        {
            throw new BarcodeEncodingException("Requested ITF module width does not match the encoded symbol.");
        }

        if (sizing.RequestedHeightModules != 0 && sizing.RequestedHeightModules != 1)
        {
            throw new BarcodeEncodingException("A logical ITF symbol must have one module row.");
        }

        ValidateOptionalMeasurement(sizing.RequestedWidthMillimetres, "Requested ITF width must be finite and non-negative.");
        ValidateOptionalMeasurement(sizing.RequestedHeightMillimetres, "Requested ITF height must be finite and non-negative.");
        ValidateOptionalMeasurement(sizing.DotsPerInch, "ITF resolution must be finite and non-negative.");
        ValidateOptionalMeasurement(sizing.ModuleSizeMillimetres, "ITF module size must be finite and non-negative.");
        ValidateOptionalMeasurement(sizing.BarHeightMillimetres, "ITF bar height must be finite and non-negative.");
        ValidateOptionalMeasurement(sizing.TargetAspectRatio, "ITF target aspect ratio must be finite and non-negative.");
    }

    static void ValidateOptionalMeasurement(double value, string message)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0.0)
        {
            throw new BarcodeEncodingException(message);
        }
    }

    static BarcodeTextLayout? CreateTextLayout(
        string transmittedText,
        BarcodePayload payload,
        BarcodeEncodeOptions options,
        BarcodeSize size,
        BarcodeQuietZone quietZone,
        double symbolWidthModules)
    {
        string? text = SelectHumanReadableText(transmittedText, payload, options);
        if (text == null)
        {
            return null;
        }

        BarcodeTextRun run = new BarcodeTextRun();
        run.Text = text;
        run.X = quietZone.LeftModules;
        run.BaselineY = size.HeightModules + HumanReadableHeight;
        run.Width = symbolWidthModules;
        run.Height = HumanReadableHeight;

        BarcodeRectangle bounds = new BarcodeRectangle();
        bounds.X = quietZone.LeftModules;
        bounds.Y = size.HeightModules;
        bounds.Width = symbolWidthModules;
        bounds.Height = HumanReadableHeight;

        BarcodeSize textSize = new BarcodeSize();
        textSize.WidthModules = size.WidthModules;
        textSize.HeightModules = CheckedMath.Add(size.HeightModules, 1);
        textSize.WidthPixels = size.WidthPixels;
        textSize.HeightPixels = size.HeightPixels;
        textSize.WidthMillimetres = size.WidthMillimetres;
        textSize.HeightMillimetres = size.HeightMillimetres;
        textSize.DotsPerInch = size.DotsPerInch;
        textSize.ModuleSizeMillimetres = size.ModuleSizeMillimetres;
        textSize.QuietZone = CopyLogicalQuietZone(quietZone);

        BarcodeTextLayout layout = new BarcodeTextLayout();
        layout.Runs = new BarcodeTextRun[] { run };
        layout.ReservedHeight = HumanReadableHeight;
        layout.Bounds = bounds;
        layout.Size = textSize;
        return layout;
    }

    static string? SelectHumanReadableText(
        string transmittedText,
        BarcodePayload payload,
        BarcodeEncodeOptions options)
    {
        switch (options.TextMode)
        {
            case BarcodeTextMode.Hidden:
                return null;
            case BarcodeTextMode.Standard:
                return transmittedText;
            case BarcodeTextMode.RawPayload:
                if (payload.RawBytes != null)
                {
                    return ConvertRawBytesToText(payload.RawBytes);
                }

                return payload.Text;
            case BarcodeTextMode.Custom:
                if (options.CustomText == null)
                {
                    throw new BarcodeEncodingException("Custom ITF human-readable text must not be null.");
                }

                return options.CustomText;
            default:
                throw new BarcodeEncodingException("ITF human-readable text mode is invalid.");
        }
    }

    static string? ConvertRawBytesToText(byte[]? bytes)
    {
        if (bytes == null)
        {
            return null;
        }

        char[] characters = new char[bytes.Length];
        int index = 0;
        while (index < bytes.Length)
        {
            characters[index] = (char)bytes[index];
            index = index + 1;
        }

        return new string(characters);
    }
}
