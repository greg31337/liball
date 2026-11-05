namespace barcode;

internal class Itf14LayoutResult
{
    internal BarcodeSymbol Symbol = new BarcodeSymbol();
    internal ItfEncodedData Encoded = new ItfEncodedData();
    internal double[] ElementWidths = new double[0];
    internal bool[] ElementIsDark = new bool[0];
    internal BarcodeRectangle[] BearerBars = new BarcodeRectangle[0];
    internal bool UsesBearerBox;
    internal bool UsesCorrugatedProfile;
    internal double WideToNarrowRatio;
    internal double CarrierWidthModules;
    internal double CarrierXModules;
    internal double CarrierYModules;
    internal double BarHeightModules;
    internal double BearerThicknessModules;
    internal double CompleteWidthModules;
    internal double CompleteHeightModules;
    internal int ExpectedDigitCount;
    internal int ExpectedPairCount;
    internal double MinimumCompleteScanWidthModules;
}

internal static class Itf14Profile
{
    internal const int CompleteDigitCount = 14;
    internal const int DataDigitCount = 13;
    internal const int PairCount = 7;
    internal const double TargetWideToNarrowRatio = 2.5;
    internal const double MinimumWideToNarrowRatio = 2.25;
    internal const double MaximumWideToNarrowRatio = 3.0;
    internal const double NominalModuleSizeMillimetres = 1.016;
    internal const double MinimumModuleSizeMillimetres = 0.495;
    internal const double CorrugatedMinimumModuleSizeMillimetres = 0.635;
    internal const double MaximumModuleSizeMillimetres = 1.016;
    internal const double MinimumBarHeightMillimetres = 31.75;
    internal const double DefaultBearerThicknessMillimetres = 4.83;
    internal const double MinimumHorizontalBearerThicknessMillimetres = 1.0;
    internal const double HumanReadableHeightModules = 1.0;
    internal const double PhysicalComparisonTolerance = 0.000001;

    internal static Itf14LayoutResult CreateLayout(
        string text,
        BarcodePayload payload,
        ItfOptions? itfOptions,
        BarcodeEncodeOptions? encodeOptions)
    {
        if (payload == null)
        {
            throw new BarcodeEncodingException("ITF-14 payload must not be null.");
        }

        ItfOptions settings = itfOptions == null ? new ItfOptions() : itfOptions;
        BarcodeEncodeOptions common = encodeOptions == null ? new BarcodeEncodeOptions() : encodeOptions;
        ValidateAllowedLengths(settings.AllowedLengths);
        BarcodeChecksumMode requestedChecksumMode = itfOptions == null
            ? common.ChecksumMode
            : settings.ChecksumMode;
        ItfEncodedData encoded = EncodeGtin(text, requestedChecksumMode);
        double ratio = ResolveRatio(settings.WideToNarrowRatio);
        ItfLayoutResult carrier = CreateCarrierLayout(encoded.EncodedText, ratio);
        BarcodeQuietZone requiredQuietZone = CreateRequiredQuietZone();
        BarcodeQuietZone selectedQuietZone = ResolveQuietZone(common, requiredQuietZone);
        bool bearerBox = settings.UseBearerBox || settings.UseCorrugatedProfile;
        Itf14SizeData sizeData = ResolveSize(
            carrier.SymbolWidthModules,
            common.Sizing,
            selectedQuietZone,
            settings,
            bearerBox);
        double carrierX = bearerBox
            ? sizeData.BearerThicknessModules + selectedQuietZone.LeftModules
            : selectedQuietZone.LeftModules;
        double carrierY = sizeData.BearerThicknessModules + selectedQuietZone.TopModules;
        BarcodeRectangle[] bearerBars = CreateBearerBars(
            sizeData.CompleteWidthModules,
            sizeData.CompleteHeightModules,
            sizeData.BearerThicknessModules,
            bearerBox);
        BarcodePayload symbolPayload = CreateProfilePayload(payload, encoded.EncodedText);
        BarcodeTextLayout? textLayout = CreateTextLayout(
            encoded.EncodedText,
            payload,
            common,
            sizeData.Size,
            carrierX,
            sizeData.CompleteHeightModules,
            carrier.SymbolWidthModules);

        BarcodeSymbol symbol = new BarcodeSymbol();
        symbol.Format = BarcodeFormat.Itf14;
        symbol.Dimension = BarcodeDimension.Linear;
        symbol.WidthModules = carrier.Symbol.WidthModules;
        symbol.HeightModules = 1;
        symbol.Modules = ResultCopier.CopyByteArray(carrier.Symbol.Modules);
        symbol.Payload = symbolPayload;
        symbol.Size = sizeData.Size;
        symbol.RequiredQuietZone = requiredQuietZone;
        symbol.TextLayout = textLayout;
        symbol.BearerBars = ResultCopier.CopyRectangleValues(bearerBars);
        symbol.SymbologyIdentifier = BarcodeSymbologyIdentifiers.Itf14;
        symbol.Codewords = ResultCopier.CopyIntArray(encoded.Digits);
        symbol.HasChecksum = true;
        symbol.ChecksumValue = encoded.ChecksumValue;

        Itf14LayoutResult result = new Itf14LayoutResult();
        result.Symbol = symbol;
        result.Encoded = encoded;
        result.ElementWidths = carrier.ElementWidths;
        result.ElementIsDark = carrier.ElementIsDark;
        result.BearerBars = bearerBars;
        result.UsesBearerBox = bearerBox;
        result.UsesCorrugatedProfile = settings.UseCorrugatedProfile;
        result.WideToNarrowRatio = ratio;
        result.CarrierWidthModules = carrier.SymbolWidthModules;
        result.CarrierXModules = carrierX;
        result.CarrierYModules = carrierY;
        result.BarHeightModules = sizeData.BarHeightModules;
        result.BearerThicknessModules = sizeData.BearerThicknessModules;
        result.CompleteWidthModules = sizeData.CompleteWidthModules;
        result.CompleteHeightModules = sizeData.CompleteHeightModules;
        result.ExpectedDigitCount = CompleteDigitCount;
        result.ExpectedPairCount = PairCount;
        result.MinimumCompleteScanWidthModules = carrier.SymbolWidthModules +
            requiredQuietZone.LeftModules + requiredQuietZone.RightModules;
        return result;
    }

    static ItfEncodedData EncodeGtin(string text, BarcodeChecksumMode checksumMode)
    {
        if (checksumMode == BarcodeChecksumMode.VerifyStrip)
        {
            throw new BarcodeEncodingException("ITF-14 must retain its mandatory GS1 check digit.");
        }

        if (checksumMode != BarcodeChecksumMode.None &&
            checksumMode != BarcodeChecksumMode.Calculate &&
            checksumMode != BarcodeChecksumMode.Require &&
            checksumMode != BarcodeChecksumMode.VerifyKeep)
        {
            throw new BarcodeEncodingException("ITF-14 checksum mode is invalid.");
        }

        if (text == null)
        {
            throw new BarcodeEncodingException("ITF-14 text must not be null.");
        }

        BarcodeChecksumMode effectiveMode;
        if (checksumMode == BarcodeChecksumMode.None)
        {
            if (text.Length == DataDigitCount)
            {
                effectiveMode = BarcodeChecksumMode.Calculate;
            }
            else if (text.Length == CompleteDigitCount)
            {
                effectiveMode = BarcodeChecksumMode.Require;
            }
            else
            {
                throw new BarcodeEncodingException("ITF-14 requires 13 data digits or a complete 14-digit GTIN.");
            }
        }
        else
        {
            effectiveMode = checksumMode;
        }

        if (effectiveMode == BarcodeChecksumMode.Calculate && text.Length != DataDigitCount)
        {
            throw new BarcodeEncodingException("ITF-14 check calculation requires exactly 13 data digits.");
        }

        if ((effectiveMode == BarcodeChecksumMode.Require ||
            effectiveMode == BarcodeChecksumMode.VerifyKeep) &&
            text.Length != CompleteDigitCount)
        {
            throw new BarcodeEncodingException("ITF-14 check verification requires exactly 14 digits.");
        }

        return ItfEncoder.Encode(text, effectiveMode, new int[] { CompleteDigitCount });
    }

    static void ValidateAllowedLengths(int[]? allowedLengths)
    {
        if (allowedLengths == null)
        {
            return;
        }

        if (allowedLengths.Length == 0)
        {
            throw new BarcodeEncodingException("ITF-14 allowed lengths must contain only the complete length 14.");
        }

        int index = 0;
        while (index < allowedLengths.Length)
        {
            if (allowedLengths[index] != CompleteDigitCount)
            {
                throw new BarcodeEncodingException("ITF-14 allowed lengths must contain only the complete length 14.");
            }

            index = index + 1;
        }
    }

    static double ResolveRatio(double ratio)
    {
        if (ratio == 0.0)
        {
            return TargetWideToNarrowRatio;
        }

        if (double.IsNaN(ratio) || double.IsInfinity(ratio) ||
            ratio < MinimumWideToNarrowRatio || ratio > MaximumWideToNarrowRatio)
        {
            throw new BarcodeEncodingException("ITF-14 wide-to-narrow ratio must be from 2.25 through 3.0.");
        }

        return ratio;
    }

    static ItfLayoutResult CreateCarrierLayout(string completeText, double ratio)
    {
        ItfOptions carrierOptions = new ItfOptions();
        carrierOptions.WideToNarrowRatio = ratio;
        carrierOptions.AllowedLengths = new int[] { CompleteDigitCount };
        carrierOptions.ChecksumMode = BarcodeChecksumMode.Require;
        BarcodePayload carrierPayload = new BarcodePayload();
        carrierPayload.Text = completeText;
        return ItfLayout.CreateLayout(completeText, carrierPayload, carrierOptions, null);
    }

    static BarcodeQuietZone CreateRequiredQuietZone()
    {
        BarcodeQuietZone quietZone = new BarcodeQuietZone();
        quietZone.LeftModules = ItfLayout.StandardQuietZoneModules;
        quietZone.RightModules = ItfLayout.StandardQuietZoneModules;
        return quietZone;
    }

    static BarcodeQuietZone ResolveQuietZone(
        BarcodeEncodeOptions options,
        BarcodeQuietZone requiredQuietZone)
    {
        BarcodeQuietZone selected;
        switch (options.QuietZonePolicy)
        {
            case BarcodeQuietZonePolicy.Standard:
                selected = CopyLogicalQuietZone(requiredQuietZone);
                break;
            case BarcodeQuietZonePolicy.CustomAtLeastStandard:
            case BarcodeQuietZonePolicy.ExactCustom:
                selected = RequireCustomQuietZone(options);
                ValidateQuietZone(selected);
                selected = CopyLogicalQuietZone(selected);
                break;
            default:
                throw new BarcodeEncodingException("ITF-14 quiet-zone policy is invalid.");
        }

        if (selected.LeftModules < requiredQuietZone.LeftModules ||
            selected.RightModules < requiredQuietZone.RightModules)
        {
            throw new BarcodeEncodingException("ITF-14 quiet zones must be at least 10 modules on both sides.");
        }

        return selected;
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
            throw new BarcodeEncodingException("A custom ITF-14 quiet zone is required by the selected policy.");
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
            throw new BarcodeEncodingException("ITF-14 quiet-zone dimensions must not be negative.");
        }
    }

    static Itf14SizeData ResolveSize(
        double carrierWidthModules,
        BarcodeSizing? sizing,
        BarcodeQuietZone quietZone,
        ItfOptions options,
        bool bearerBox)
    {
        BarcodeSizing settings = sizing == null ? new BarcodeSizing() : sizing;
        ValidateSizing(settings, RoundWidthToModules(carrierWidthModules));
        double baseWidthModules = carrierWidthModules + quietZone.LeftModules + quietZone.RightModules;
        bool explicitBearerThickness = options.BearerBarThicknessModules > 0;
        if (options.BearerBarThicknessModules < 0)
        {
            throw new BarcodeEncodingException("ITF-14 bearer-bar thickness must not be negative.");
        }

        double moduleSize = ResolveModuleSize(
            settings,
            baseWidthModules,
            options.BearerBarThicknessModules,
            explicitBearerThickness,
            bearerBox);
        double minimumModuleSize = options.UseCorrugatedProfile
            ? CorrugatedMinimumModuleSizeMillimetres
            : MinimumModuleSizeMillimetres;
        if (moduleSize < minimumModuleSize || moduleSize > MaximumModuleSizeMillimetres)
        {
            throw new BarcodeEncodingException("ITF-14 module size is outside the selected GS1 profile range.");
        }

        double bearerThicknessMillimetres = explicitBearerThickness
            ? options.BearerBarThicknessModules * moduleSize
            : DefaultBearerThicknessMillimetres;
        double minimumBearerThickness = bearerBox
            ? DefaultBearerThicknessMillimetres
            : MinimumHorizontalBearerThicknessMillimetres;
        if (bearerThicknessMillimetres + PhysicalComparisonTolerance < minimumBearerThickness)
        {
            throw new BarcodeEncodingException("ITF-14 bearer-bar thickness is below the selected profile minimum.");
        }

        double bearerThicknessModules = bearerThicknessMillimetres / moduleSize;
        double completeWidthModules = bearerBox
            ? baseWidthModules + (2.0 * bearerThicknessModules)
            : baseWidthModules;
        double widthMillimetres = completeWidthModules * moduleSize;
        if (double.IsInfinity(widthMillimetres))
        {
            throw new BarcodeEncodingException("Resolved ITF-14 width exceeds the supported physical range.");
        }

        if (settings.RequestedWidthMillimetres > 0.0 &&
            global::System.Math.Abs(widthMillimetres - settings.RequestedWidthMillimetres) > PhysicalComparisonTolerance)
        {
            throw new BarcodeEncodingException("Requested ITF-14 width conflicts with the GS1 profile dimensions.");
        }

        double verticalQuietModules = quietZone.TopModules + quietZone.BottomModules;
        double verticalFixedMillimetres = (verticalQuietModules * moduleSize) +
            (2.0 * bearerThicknessMillimetres);
        double barHeight = settings.BarHeightMillimetres;
        if (barHeight == 0.0 && settings.RequestedHeightMillimetres > 0.0)
        {
            barHeight = settings.RequestedHeightMillimetres - verticalFixedMillimetres;
        }

        if (barHeight == 0.0)
        {
            barHeight = MinimumBarHeightMillimetres;
        }

        if (barHeight + PhysicalComparisonTolerance < MinimumBarHeightMillimetres)
        {
            throw new BarcodeEncodingException("ITF-14 bar height must be at least 31.75 millimetres.");
        }

        double heightMillimetres = barHeight + verticalFixedMillimetres;
        if (double.IsInfinity(heightMillimetres))
        {
            throw new BarcodeEncodingException("Resolved ITF-14 height exceeds the supported physical range.");
        }

        if (settings.RequestedHeightMillimetres > 0.0 &&
            global::System.Math.Abs(heightMillimetres - settings.RequestedHeightMillimetres) > PhysicalComparisonTolerance)
        {
            throw new BarcodeEncodingException("Requested ITF-14 height conflicts with the GS1 profile dimensions.");
        }

        double barHeightModules = barHeight / moduleSize;
        double completeHeightModules = barHeightModules + verticalQuietModules +
            (2.0 * bearerThicknessModules);
        BarcodeSize size = new BarcodeSize();
        size.WidthModules = RoundWidthToModules(completeWidthModules);
        size.HeightModules = RoundWidthToModules(completeHeightModules);
        size.WidthPixels = settings.RequestedWidthPixels;
        size.HeightPixels = settings.RequestedHeightPixels;
        size.WidthMillimetres = widthMillimetres;
        size.HeightMillimetres = heightMillimetres;
        size.DotsPerInch = settings.DotsPerInch;
        size.ModuleSizeMillimetres = moduleSize;
        size.QuietZone = CopyLogicalQuietZone(quietZone);

        Itf14SizeData result = new Itf14SizeData();
        result.Size = size;
        result.BarHeightModules = barHeightModules;
        result.BearerThicknessModules = bearerThicknessModules;
        result.CompleteWidthModules = completeWidthModules;
        result.CompleteHeightModules = completeHeightModules;
        return result;
    }

    static double ResolveModuleSize(
        BarcodeSizing sizing,
        double baseWidthModules,
        int bearerThicknessModules,
        bool explicitBearerThickness,
        bool bearerBox)
    {
        double moduleSize = sizing.ModuleSizeMillimetres;
        if (moduleSize == 0.0 && sizing.RequestedWidthMillimetres > 0.0)
        {
            double scalableWidthModules = baseWidthModules;
            double fixedWidthMillimetres = 0.0;
            if (bearerBox)
            {
                if (explicitBearerThickness)
                {
                    scalableWidthModules = scalableWidthModules + (2.0 * bearerThicknessModules);
                }
                else
                {
                    fixedWidthMillimetres = 2.0 * DefaultBearerThicknessMillimetres;
                }
            }

            double scalableWidthMillimetres = sizing.RequestedWidthMillimetres - fixedWidthMillimetres;
            if (scalableWidthMillimetres <= 0.0)
            {
                throw new BarcodeEncodingException("Requested ITF-14 width is too small for the bearer bars.");
            }

            moduleSize = scalableWidthMillimetres / scalableWidthModules;
        }

        if (moduleSize == 0.0)
        {
            moduleSize = NominalModuleSizeMillimetres;
        }

        return moduleSize;
    }

    static void ValidateSizing(BarcodeSizing sizing, int carrierWidthModules)
    {
        if (sizing.RequestedWidthModules < 0 || sizing.RequestedHeightModules < 0 ||
            sizing.RequestedWidthPixels < 0 || sizing.RequestedHeightPixels < 0)
        {
            throw new BarcodeEncodingException("ITF-14 sizing dimensions must not be negative.");
        }

        if (sizing.RequestedWidthModules != 0 && sizing.RequestedWidthModules != carrierWidthModules)
        {
            throw new BarcodeEncodingException("Requested ITF-14 module width does not match the encoded carrier.");
        }

        if (sizing.RequestedHeightModules != 0 && sizing.RequestedHeightModules != 1)
        {
            throw new BarcodeEncodingException("A logical ITF-14 carrier must have one module row.");
        }

        ValidateOptionalMeasurement(sizing.RequestedWidthMillimetres, "Requested ITF-14 width must be finite and non-negative.");
        ValidateOptionalMeasurement(sizing.RequestedHeightMillimetres, "Requested ITF-14 height must be finite and non-negative.");
        ValidateOptionalMeasurement(sizing.DotsPerInch, "ITF-14 resolution must be finite and non-negative.");
        ValidateOptionalMeasurement(sizing.ModuleSizeMillimetres, "ITF-14 module size must be finite and non-negative.");
        ValidateOptionalMeasurement(sizing.BarHeightMillimetres, "ITF-14 bar height must be finite and non-negative.");
        ValidateOptionalMeasurement(sizing.TargetAspectRatio, "ITF-14 target aspect ratio must be finite and non-negative.");
    }

    static void ValidateOptionalMeasurement(double value, string message)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0.0)
        {
            throw new BarcodeEncodingException(message);
        }
    }

    static BarcodeRectangle[] CreateBearerBars(
        double completeWidthModules,
        double completeHeightModules,
        double thicknessModules,
        bool bearerBox)
    {
        int count = bearerBox ? 4 : 2;
        BarcodeRectangle[] bars = new BarcodeRectangle[count];
        bars[0] = CreateRectangle(0.0, 0.0, completeWidthModules, thicknessModules);
        bars[1] = CreateRectangle(
            0.0,
            completeHeightModules - thicknessModules,
            completeWidthModules,
            thicknessModules);
        if (bearerBox)
        {
            bars[2] = CreateRectangle(0.0, 0.0, thicknessModules, completeHeightModules);
            bars[3] = CreateRectangle(
                completeWidthModules - thicknessModules,
                0.0,
                thicknessModules,
                completeHeightModules);
        }

        return bars;
    }

    static BarcodeRectangle CreateRectangle(
        double x,
        double y,
        double width,
        double height)
    {
        BarcodeRectangle rectangle = new BarcodeRectangle();
        rectangle.X = x;
        rectangle.Y = y;
        rectangle.Width = width;
        rectangle.Height = height;
        return rectangle;
    }

    static BarcodePayload CreateProfilePayload(BarcodePayload source, string completeText)
    {
        BarcodePayload? copiedPayload = ResultCopier.CopyPayload(source);
        if (copiedPayload == null)
        {
            throw new BarcodeValidationException("ITF-14 payload copying failed.");
        }

        BarcodePayload payload = copiedPayload;
        byte[] rawBytes = new byte[completeText.Length];
        int index = 0;
        while (index < completeText.Length)
        {
            rawBytes[index] = (byte)completeText[index];
            index = index + 1;
        }

        payload.Text = completeText;
        payload.RawBytes = rawBytes;
        payload.CharacterEncoding = "ISO/IEC 646";
        payload.IsGs1 = true;
        return payload;
    }

    static BarcodeTextLayout? CreateTextLayout(
        string completeText,
        BarcodePayload payload,
        BarcodeEncodeOptions options,
        BarcodeSize size,
        double carrierX,
        double completeHeightModules,
        double carrierWidthModules)
    {
        string? text = SelectHumanReadableText(completeText, payload, options);
        if (text == null)
        {
            return null;
        }

        BarcodeTextRun run = new BarcodeTextRun();
        run.Text = text;
        run.X = carrierX;
        run.BaselineY = completeHeightModules + HumanReadableHeightModules;
        run.Width = carrierWidthModules;
        run.Height = HumanReadableHeightModules;

        BarcodeRectangle bounds = new BarcodeRectangle();
        bounds.X = carrierX;
        bounds.Y = completeHeightModules;
        bounds.Width = carrierWidthModules;
        bounds.Height = HumanReadableHeightModules;

        BarcodeSize textSize = new BarcodeSize();
        textSize.WidthModules = size.WidthModules;
        textSize.HeightModules = CheckedMath.Add(size.HeightModules, 1);
        textSize.WidthPixels = size.WidthPixels;
        textSize.HeightPixels = size.HeightPixels;
        textSize.WidthMillimetres = size.WidthMillimetres;
        textSize.HeightMillimetres = size.HeightMillimetres;
        textSize.DotsPerInch = size.DotsPerInch;
        textSize.ModuleSizeMillimetres = size.ModuleSizeMillimetres;
        textSize.QuietZone = CopyLogicalQuietZone(size.QuietZone);

        BarcodeTextLayout layout = new BarcodeTextLayout();
        layout.Runs = new BarcodeTextRun[] { run };
        layout.ReservedHeight = HumanReadableHeightModules;
        layout.Bounds = bounds;
        layout.Size = textSize;
        return layout;
    }

    static string? SelectHumanReadableText(
        string completeText,
        BarcodePayload payload,
        BarcodeEncodeOptions options)
    {
        switch (options.TextMode)
        {
            case BarcodeTextMode.Hidden:
                return null;
            case BarcodeTextMode.Standard:
                return GroupHumanReadableText(completeText);
            case BarcodeTextMode.RawPayload:
                if (payload.RawBytes != null)
                {
                    return ConvertRawBytesToText(payload.RawBytes);
                }

                return payload.Text;
            case BarcodeTextMode.Custom:
                if (options.CustomText == null)
                {
                    throw new BarcodeEncodingException("Custom ITF-14 human-readable text must not be null.");
                }

                return options.CustomText;
            default:
                throw new BarcodeEncodingException("ITF-14 human-readable text mode is invalid.");
        }
    }

    static string GroupHumanReadableText(string text)
    {
        return text.Substring(0, 1) + " " +
            text.Substring(1, 2) + " " +
            text.Substring(3, 5) + " " +
            text.Substring(8, 5) + " " +
            text.Substring(13, 1);
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

    static int RoundWidthToModules(double width)
    {
        double rounded = global::System.Math.Ceiling(width);
        if (rounded > int.MaxValue)
        {
            throw new BarcodeEncodingException("ITF-14 layout dimensions exceed the supported range.");
        }

        return (int)rounded;
    }

    static BarcodeQuietZone CopyLogicalQuietZone(BarcodeQuietZone? source)
    {
        BarcodeQuietZone copy = new BarcodeQuietZone();
        if (source != null)
        {
            copy.LeftModules = source.LeftModules;
            copy.TopModules = source.TopModules;
            copy.RightModules = source.RightModules;
            copy.BottomModules = source.BottomModules;
        }

        return copy;
    }
}

internal class Itf14SizeData
{
    internal BarcodeSize Size = new BarcodeSize();
    internal double BarHeightModules;
    internal double BearerThicknessModules;
    internal double CompleteWidthModules;
    internal double CompleteHeightModules;
}
