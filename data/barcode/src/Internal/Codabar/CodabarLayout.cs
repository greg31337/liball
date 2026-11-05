namespace barcode;

internal class CodabarLayoutResult
{
    internal BarcodeSymbol Symbol = new BarcodeSymbol();
    internal CodabarEncodedData Encoded = new CodabarEncodedData();
    internal double WideToNarrowRatio;
    internal int IntercharacterGapModules;
    internal double SymbolWidthModules;
    internal double[] ElementWidths = new double[0];
    internal bool[] ElementIsDark = new bool[0];
}

internal class CodabarElementLayout
{
    internal double[] Widths = new double[0];
    internal bool[] IsDark = new bool[0];
    internal double WidthModules;
}

internal static class CodabarLayout
{
    internal const int StandardQuietZoneModules = 10;
    internal const double DefaultWideToNarrowRatio = 2.0;
    internal const int DefaultIntercharacterGapModules = 1;
    internal const double HumanReadableHeight = 1.0;
    internal const double PhysicalComparisonTolerance = 0.000001;

    internal static CodabarLayoutResult CreateLayout(
        string text,
        BarcodePayload payload,
        CodabarOptions? codabarOptions,
        BarcodeEncodeOptions? encodeOptions)
    {
        if (payload == null)
        {
            throw new BarcodeEncodingException("Codabar payload must not be null.");
        }

        CodabarOptions settings = codabarOptions == null ? new CodabarOptions() : codabarOptions;
        BarcodeEncodeOptions common = encodeOptions == null ? new BarcodeEncodeOptions() : encodeOptions;
        double ratio = ResolveRatio(settings.WideToNarrowRatio);
        int gap = ResolveGap(settings.IntercharacterGapModules);
        BarcodeChecksumMode checksumMode = codabarOptions == null
            ? common.ChecksumMode
            : settings.ChecksumMode;
        CodabarEncodedData encoded = CodabarEncoder.Encode(text, settings, checksumMode);
        CodabarElementLayout elements = CreateElementLayout(encoded.PatternBits, ratio, gap);
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
        symbol.Format = BarcodeFormat.Codabar;
        symbol.Dimension = BarcodeDimension.Linear;
        symbol.WidthModules = RoundWidthToModules(elements.WidthModules);
        symbol.HeightModules = 1;
        symbol.Modules = CreateIntegralModuleRow(elements.Widths, elements.IsDark);
        symbol.Payload = ResultCopier.CopyPayload(payload);
        symbol.Size = size;
        symbol.RequiredQuietZone = requiredQuietZone;
        symbol.TextLayout = textLayout;
        symbol.SymbologyIdentifier = BarcodeSymbologyIdentifiers.Codabar;
        symbol.Codewords = ResultCopier.CopyIntArray(encoded.Values);
        symbol.HasChecksum = encoded.HasChecksum;
        symbol.ChecksumValue = encoded.ChecksumValue;

        CodabarLayoutResult result = new CodabarLayoutResult();
        result.Symbol = symbol;
        result.Encoded = encoded;
        result.WideToNarrowRatio = ratio;
        result.IntercharacterGapModules = gap;
        result.SymbolWidthModules = elements.WidthModules;
        result.ElementWidths = elements.Widths;
        result.ElementIsDark = elements.IsDark;
        return result;
    }

    static double ResolveRatio(double ratio)
    {
        if (ratio == 0.0)
        {
            return DefaultWideToNarrowRatio;
        }

        if (double.IsNaN(ratio) || double.IsInfinity(ratio) || ratio < 2.0 || ratio > 3.0)
        {
            throw new BarcodeEncodingException("Codabar wide-to-narrow ratio must be from 2.0 through 3.0.");
        }

        return ratio;
    }

    static int ResolveGap(int gap)
    {
        if (gap == 0)
        {
            return DefaultIntercharacterGapModules;
        }

        if (gap < 0)
        {
            throw new BarcodeEncodingException("Codabar intercharacter gap must be positive.");
        }

        return gap;
    }

    static CodabarElementLayout CreateElementLayout(int[] patterns, double ratio, int gap)
    {
        int gapCount = patterns.Length - 1;
        int characterElements = CheckedMath.Multiply(patterns.Length, CodabarPatterns.ElementCount);
        int elementCount = CheckedMath.Add(characterElements, gapCount);
        double[] widths = new double[elementCount];
        bool[] isDark = new bool[elementCount];
        double totalWidth = 0.0;
        int target = 0;
        int characterIndex = 0;
        while (characterIndex < patterns.Length)
        {
            int elementIndex = 0;
            while (elementIndex < CodabarPatterns.ElementCount)
            {
                int bit = CodabarPatterns.ElementCount - elementIndex - 1;
                bool wide = (patterns[characterIndex] & (1 << bit)) != 0;
                double width = wide ? ratio : 1.0;
                widths[target] = width;
                isDark[target] = elementIndex % 2 == 0;
                totalWidth = totalWidth + width;
                target = target + 1;
                elementIndex = elementIndex + 1;
            }

            if (characterIndex < patterns.Length - 1)
            {
                widths[target] = gap;
                isDark[target] = false;
                totalWidth = totalWidth + gap;
                target = target + 1;
            }

            characterIndex = characterIndex + 1;
        }

        if (double.IsInfinity(totalWidth))
        {
            throw new BarcodeEncodingException("Codabar layout width exceeds the supported range.");
        }

        CodabarElementLayout layout = new CodabarElementLayout();
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
            throw new BarcodeEncodingException("Codabar logical width exceeds the supported range.");
        }

        return (int)rounded;
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
                    throw new BarcodeEncodingException("Custom Codabar quiet zones must be at least 10 modules on the left and right.");
                }

                return CopyLogicalQuietZone(minimumCustom);
            case BarcodeQuietZonePolicy.ExactCustom:
                BarcodeQuietZone exactCustom = RequireCustomQuietZone(options);
                ValidateQuietZone(exactCustom);
                return CopyLogicalQuietZone(exactCustom);
            default:
                throw new BarcodeEncodingException("Codabar quiet-zone policy is invalid.");
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
            throw new BarcodeEncodingException("A custom Codabar quiet zone is required by the selected policy.");
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
            throw new BarcodeEncodingException("Codabar quiet-zone dimensions must not be negative.");
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
                throw new BarcodeEncodingException("Resolved Codabar width exceeds the supported physical range.");
            }
        }

        if (settings.RequestedWidthMillimetres > 0.0 &&
            global::System.Math.Abs(widthMillimetres - settings.RequestedWidthMillimetres) > PhysicalComparisonTolerance)
        {
            throw new BarcodeEncodingException("Requested Codabar width conflicts with the module size and quiet zones.");
        }

        double barHeight = settings.BarHeightMillimetres;
        double verticalQuietMillimetres = verticalQuietZone * moduleSize;
        if (settings.RequestedHeightMillimetres > 0.0 && verticalQuietZone > 0 && moduleSize == 0.0)
        {
            throw new BarcodeEncodingException("A Codabar module size is required to resolve physical vertical quiet zones.");
        }

        if (barHeight == 0.0 && settings.RequestedHeightMillimetres > 0.0)
        {
            barHeight = settings.RequestedHeightMillimetres - verticalQuietMillimetres;
            if (barHeight <= 0.0)
            {
                throw new BarcodeEncodingException("Requested Codabar height does not leave a positive bar height.");
            }
        }

        double heightMillimetres = 0.0;
        if (barHeight > 0.0)
        {
            heightMillimetres = barHeight + verticalQuietMillimetres;
            if (double.IsInfinity(heightMillimetres))
            {
                throw new BarcodeEncodingException("Resolved Codabar height exceeds the supported physical range.");
            }
        }

        if (settings.RequestedHeightMillimetres > 0.0 &&
            global::System.Math.Abs(heightMillimetres - settings.RequestedHeightMillimetres) > PhysicalComparisonTolerance)
        {
            throw new BarcodeEncodingException("Requested Codabar height conflicts with the bar height and quiet zones.");
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
            throw new BarcodeEncodingException("Codabar sizing dimensions must not be negative.");
        }

        if (sizing.RequestedWidthModules != 0 && sizing.RequestedWidthModules != symbolWidthModules)
        {
            throw new BarcodeEncodingException("Requested Codabar module width does not match the encoded symbol.");
        }

        if (sizing.RequestedHeightModules != 0 && sizing.RequestedHeightModules != 1)
        {
            throw new BarcodeEncodingException("A logical Codabar symbol must have one module row.");
        }

        ValidateOptionalMeasurement(sizing.RequestedWidthMillimetres, "Requested Codabar width must be finite and non-negative.");
        ValidateOptionalMeasurement(sizing.RequestedHeightMillimetres, "Requested Codabar height must be finite and non-negative.");
        ValidateOptionalMeasurement(sizing.DotsPerInch, "Codabar resolution must be finite and non-negative.");
        ValidateOptionalMeasurement(sizing.ModuleSizeMillimetres, "Codabar module size must be finite and non-negative.");
        ValidateOptionalMeasurement(sizing.BarHeightMillimetres, "Codabar bar height must be finite and non-negative.");
        ValidateOptionalMeasurement(sizing.TargetAspectRatio, "Codabar target aspect ratio must be finite and non-negative.");
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
                    throw new BarcodeEncodingException("Custom Codabar human-readable text must not be null.");
                }

                return options.CustomText;
            default:
                throw new BarcodeEncodingException("Codabar human-readable text mode is invalid.");
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
