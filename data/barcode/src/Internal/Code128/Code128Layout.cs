namespace barcode;

internal static class Code128Layout
{
    internal const int StandardQuietZoneModules = 10;
    internal const double HumanReadableHeight = 1.0;
    internal const double PhysicalComparisonTolerance = 0.000001;

    internal static BarcodeSymbol CreateSymbol(
        Code128EncodedData encoded,
        BarcodePayload payload,
        BarcodeEncodeOptions? options)
    {
        ValidateEncodedData(encoded);
        if (payload == null)
        {
            throw new BarcodeEncodingException("Code 128 payload must not be null.");
        }

        BarcodeEncodeOptions settings = options == null ? new BarcodeEncodeOptions() : options;
        BarcodeQuietZone requiredQuietZone = CreateRequiredQuietZone();
        BarcodeQuietZone selectedQuietZone = ResolveQuietZone(settings, requiredQuietZone);
        BarcodeSize size = ResolveSize(encoded.Modules.Length, settings.Sizing, selectedQuietZone);
        BarcodeTextLayout? textLayout = CreateTextLayout(payload, settings, size, selectedQuietZone);

        BarcodeSymbol symbol = new BarcodeSymbol();
        symbol.Format = BarcodeFormat.Code128;
        symbol.Dimension = BarcodeDimension.Linear;
        symbol.WidthModules = encoded.Modules.Length;
        symbol.HeightModules = 1;
        symbol.Modules = ResultCopier.CopyByteArray(encoded.Modules);
        symbol.Payload = ResultCopier.CopyPayload(payload);
        symbol.Size = size;
        symbol.RequiredQuietZone = requiredQuietZone;
        symbol.TextLayout = textLayout;
        symbol.SymbologyIdentifier = BarcodeSymbologyIdentifiers.Code128;
        symbol.Codewords = ResultCopier.CopyIntArray(encoded.Codewords);
        symbol.HasChecksum = true;
        symbol.ChecksumValue = encoded.ChecksumValue;
        return symbol;
    }

    static void ValidateEncodedData(Code128EncodedData encoded)
    {
        if (encoded == null)
        {
            throw new BarcodeValidationException("Encoded Code 128 data must not be null.");
        }

        if (encoded.Codewords == null || encoded.Codewords.Length < 4)
        {
            throw new BarcodeValidationException("Encoded Code 128 data must contain start, data, check, and stop codewords.");
        }

        int lastIndex = encoded.Codewords.Length - 1;
        if (encoded.Codewords[lastIndex] != Code128Patterns.StopCodeword)
        {
            throw new BarcodeValidationException("Encoded Code 128 data must end with the stop codeword.");
        }

        int checkIndex = lastIndex - 1;
        int expectedCheck = Code128Modulo103.CalculateCheckCodewordForPrefix(encoded.Codewords, checkIndex);
        if (encoded.Codewords[checkIndex] != expectedCheck || encoded.ChecksumValue != expectedCheck)
        {
            throw new BarcodeValidationException("Encoded Code 128 data has an invalid check codeword.");
        }

        int expectedStart = Code128Patterns.GetStartCodeword(encoded.StartCodeSet);
        if (encoded.Codewords[0] != expectedStart)
        {
            throw new BarcodeValidationException("Encoded Code 128 start codeword does not match its code set.");
        }

        if (encoded.Modules == null)
        {
            throw new BarcodeValidationException("Encoded Code 128 modules must not be null.");
        }

        int ordinaryModuleCount = CheckedMath.Multiply(encoded.Codewords.Length - 1, Code128Patterns.OrdinaryModuleCount);
        int expectedModuleCount = CheckedMath.Add(ordinaryModuleCount, Code128Patterns.StopModuleCount);
        if (encoded.Modules.Length != expectedModuleCount)
        {
            throw new BarcodeValidationException("Encoded Code 128 module count does not match its codewords.");
        }
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
                    throw new BarcodeEncodingException("Custom Code 128 quiet zones must be at least 10 modules on the left and right.");
                }

                return CopyLogicalQuietZone(minimumCustom);
            case BarcodeQuietZonePolicy.ExactCustom:
                BarcodeQuietZone exactCustom = RequireCustomQuietZone(options);
                ValidateQuietZone(exactCustom);
                return CopyLogicalQuietZone(exactCustom);
            default:
                throw new BarcodeEncodingException("Code 128 quiet-zone policy is invalid.");
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
            throw new BarcodeEncodingException("A custom Code 128 quiet zone is required by the selected policy.");
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
            throw new BarcodeEncodingException("Code 128 quiet-zone dimensions must not be negative.");
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
        int symbolWidthModules,
        BarcodeSizing? sizing,
        BarcodeQuietZone quietZone)
    {
        BarcodeSizing settings = sizing == null ? new BarcodeSizing() : sizing;
        ValidateSizing(settings, symbolWidthModules);
        int horizontalQuietZone = CheckedMath.Add(quietZone.LeftModules, quietZone.RightModules);
        int physicalWidthModules = CheckedMath.Add(symbolWidthModules, horizontalQuietZone);
        int verticalQuietZone = CheckedMath.Add(quietZone.TopModules, quietZone.BottomModules);
        int physicalHeightModules = CheckedMath.Add(1, verticalQuietZone);

        double moduleSize = settings.ModuleSizeMillimetres;
        if (moduleSize == 0.0 && settings.RequestedWidthMillimetres > 0.0)
        {
            moduleSize = settings.RequestedWidthMillimetres / (double)physicalWidthModules;
        }

        double widthMillimetres = 0.0;
        if (moduleSize > 0.0)
        {
            widthMillimetres = (double)physicalWidthModules * moduleSize;
            if (double.IsInfinity(widthMillimetres))
            {
                throw new BarcodeEncodingException("Resolved Code 128 width exceeds the supported physical range.");
            }
        }

        if (settings.RequestedWidthMillimetres > 0.0 &&
            global::System.Math.Abs(widthMillimetres - settings.RequestedWidthMillimetres) > PhysicalComparisonTolerance)
        {
            throw new BarcodeEncodingException("Requested Code 128 width conflicts with the module size and quiet zones.");
        }

        double barHeight = settings.BarHeightMillimetres;
        double verticalQuietZoneMillimetres = (double)verticalQuietZone * moduleSize;
        if (settings.RequestedHeightMillimetres > 0.0 && verticalQuietZone > 0 && moduleSize == 0.0)
        {
            throw new BarcodeEncodingException("A Code 128 module size is required to resolve physical vertical quiet zones.");
        }

        if (barHeight == 0.0 && settings.RequestedHeightMillimetres > 0.0)
        {
            barHeight = settings.RequestedHeightMillimetres - verticalQuietZoneMillimetres;
            if (barHeight <= 0.0)
            {
                throw new BarcodeEncodingException("Requested Code 128 height does not leave a positive bar height.");
            }
        }

        double heightMillimetres = 0.0;
        if (barHeight > 0.0)
        {
            heightMillimetres = barHeight + verticalQuietZoneMillimetres;
            if (double.IsInfinity(heightMillimetres))
            {
                throw new BarcodeEncodingException("Resolved Code 128 height exceeds the supported physical range.");
            }
        }

        if (settings.RequestedHeightMillimetres > 0.0 &&
            global::System.Math.Abs(heightMillimetres - settings.RequestedHeightMillimetres) > PhysicalComparisonTolerance)
        {
            throw new BarcodeEncodingException("Requested Code 128 height conflicts with the bar height and quiet zones.");
        }

        BarcodeSize size = new BarcodeSize();
        size.WidthModules = physicalWidthModules;
        size.HeightModules = physicalHeightModules;
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
            throw new BarcodeEncodingException("Code 128 sizing dimensions must not be negative.");
        }

        if (sizing.RequestedWidthModules != 0 && sizing.RequestedWidthModules != symbolWidthModules)
        {
            throw new BarcodeEncodingException("Requested Code 128 module width does not match the encoded symbol.");
        }

        if (sizing.RequestedHeightModules != 0 && sizing.RequestedHeightModules != 1)
        {
            throw new BarcodeEncodingException("A logical Code 128 symbol must have one module row.");
        }

        ValidateOptionalMeasurement(sizing.RequestedWidthMillimetres, "Requested Code 128 width must be finite and non-negative.");
        ValidateOptionalMeasurement(sizing.RequestedHeightMillimetres, "Requested Code 128 height must be finite and non-negative.");
        ValidateOptionalMeasurement(sizing.DotsPerInch, "Code 128 resolution must be finite and non-negative.");
        ValidateOptionalMeasurement(sizing.ModuleSizeMillimetres, "Code 128 module size must be finite and non-negative.");
        ValidateOptionalMeasurement(sizing.BarHeightMillimetres, "Code 128 bar height must be finite and non-negative.");
        ValidateOptionalMeasurement(sizing.TargetAspectRatio, "Code 128 target aspect ratio must be finite and non-negative.");
    }

    static void ValidateOptionalMeasurement(double value, string message)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0.0)
        {
            throw new BarcodeEncodingException(message);
        }
    }

    static BarcodeTextLayout? CreateTextLayout(
        BarcodePayload payload,
        BarcodeEncodeOptions options,
        BarcodeSize size,
        BarcodeQuietZone quietZone)
    {
        string? text = SelectHumanReadableText(payload, options);
        if (text == null)
        {
            return null;
        }

        BarcodeTextRun run = new BarcodeTextRun();
        run.Text = text;
        run.X = quietZone.LeftModules;
        run.BaselineY = size.HeightModules + HumanReadableHeight;
        run.Width = size.WidthModules - quietZone.LeftModules - quietZone.RightModules;
        run.Height = HumanReadableHeight;

        BarcodeRectangle bounds = new BarcodeRectangle();
        bounds.X = quietZone.LeftModules;
        bounds.Y = size.HeightModules;
        bounds.Width = run.Width;
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

    static string? SelectHumanReadableText(BarcodePayload payload, BarcodeEncodeOptions options)
    {
        switch (options.TextMode)
        {
            case BarcodeTextMode.Hidden:
                return null;
            case BarcodeTextMode.Standard:
                if (payload.Text != null)
                {
                    return payload.Text;
                }

                return ConvertRawBytesToText(payload.RawBytes);
            case BarcodeTextMode.RawPayload:
                if (payload.RawBytes != null)
                {
                    return ConvertRawBytesToText(payload.RawBytes);
                }

                return payload.Text;
            case BarcodeTextMode.Custom:
                if (options.CustomText == null)
                {
                    throw new BarcodeEncodingException("Custom Code 128 human-readable text must not be null.");
                }

                return options.CustomText;
            default:
                throw new BarcodeEncodingException("Code 128 human-readable text mode is invalid.");
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
