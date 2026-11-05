namespace barcode;

internal class EanUpcGuardBar
{
    internal int ModuleOffset;
    internal int ModuleCount;
    internal double DescentModules;
}

internal class EanUpcLayoutResult
{
    internal BarcodeSymbol Symbol = new BarcodeSymbol();
    internal EanUpcGuardBar[] GuardBars = new EanUpcGuardBar[0];
    internal int MainModuleCount;
    internal int AddOnOffsetModules;
    internal int AddOnModuleCount;
    internal int AddOnSeparationModules;
    internal double GuardDescentModules;
}

internal static class EanUpcLayout
{
    internal const int Ean13LeftQuietZoneModules = 11;
    internal const int Ean13RightQuietZoneModules = 7;
    internal const int Ean8LeftQuietZoneModules = 7;
    internal const int Ean8RightQuietZoneModules = 7;
    internal const int UpcALeftQuietZoneModules = 9;
    internal const int UpcARightQuietZoneModules = 9;
    internal const int UpcELeftQuietZoneModules = 9;
    internal const int UpcERightQuietZoneModules = 7;
    internal const int AddOnLeftQuietZoneModules = 7;
    internal const int AddOnRightQuietZoneModules = 5;
    internal const int StandardAddOnSeparationModules = 7;
    internal const int UpcAAddOnSeparationModules = 9;
    internal const double NominalModuleSizeMillimetres = 0.330;
    internal const double MinimumModuleSizeMillimetres = 0.264;
    internal const double MaximumModuleSizeMillimetres = 0.660;
    internal const double NominalLongBarHeightMillimetres = 22.85;
    internal const double NominalEan8BarHeightMillimetres = 18.23;
    internal const double StandardGuardDescentModules = 5.0;
    internal const double HumanReadableHeightModules = 7.0;
    internal const double PhysicalComparisonTolerance = 0.000001;

    internal static BarcodeSymbol CreateSymbol(
        EanUpcAddonAssociation association,
        BarcodePayload payload,
        BarcodeEncodeOptions? options,
        EanUpcOptions? eanUpcOptions)
    {
        return CreateLayout(association, payload, options, eanUpcOptions).Symbol;
    }

    internal static BarcodeSymbol CreateAddOnSymbol(
        EanUpcEncodedData encoded,
        BarcodePayload payload,
        BarcodeEncodeOptions? options)
    {
        ValidateAddOnData(encoded);
        if (payload == null)
        {
            throw new BarcodeEncodingException(
                "EAN/UPC add-on payload must not be null.");
        }

        BarcodeEncodeOptions settings = options == null
            ? new BarcodeEncodeOptions()
            : options;
        BarcodeQuietZone requiredQuietZone = new BarcodeQuietZone();
        requiredQuietZone.LeftModules = AddOnLeftQuietZoneModules;
        requiredQuietZone.RightModules = AddOnRightQuietZoneModules;
        BarcodeQuietZone selectedQuietZone = ResolveQuietZone(
            settings,
            requiredQuietZone);
        BarcodeSize size = ResolveSize(
            encoded.Format,
            encoded.Modules.Length,
            settings.Sizing,
            selectedQuietZone);

        BarcodeTextLayout? textLayout = CreateAddOnTextLayout(
            encoded,
            payload,
            settings,
            size,
            selectedQuietZone);
        BarcodeSymbol symbol = new BarcodeSymbol();
        symbol.Format = encoded.Format;
        symbol.Dimension = BarcodeDimension.Linear;
        symbol.WidthModules = encoded.Modules.Length;
        symbol.HeightModules = 1;
        symbol.Modules = ResultCopier.CopyByteArray(encoded.Modules);
        symbol.Payload = ResultCopier.CopyPayload(payload);
        symbol.Size = size;
        symbol.RequiredQuietZone = requiredQuietZone;
        symbol.TextLayout = textLayout;
        symbol.SymbologyIdentifier = BarcodeSymbologyIdentifiers.EanUpc;
        symbol.Codewords = ResultCopier.CopyIntArray(encoded.Digits);
        symbol.HasChecksum = true;
        symbol.ChecksumValue = encoded.ChecksumValue;
        return symbol;
    }

    internal static EanUpcLayoutResult CreateLayout(
        EanUpcAddonAssociation association,
        BarcodePayload payload,
        BarcodeEncodeOptions? options,
        EanUpcOptions? eanUpcOptions)
    {
        ValidateAssociation(association);
        if (payload == null)
        {
            throw new BarcodeEncodingException("EAN/UPC payload must not be null.");
        }

        BarcodeEncodeOptions settings = options == null ? new BarcodeEncodeOptions() : options;
        EanUpcEncodedData main = association.Main;
        int separationModules = ResolveAddOnSeparation(main.Format, association.AddOn);
        int addOnOffset = association.AddOn == null
            ? 0
            : CheckedMath.Add(main.Modules.Length, separationModules);
        byte[] modules = CombineModules(main.Modules, association.AddOn, separationModules);
        BarcodeQuietZone requiredQuietZone = CreateRequiredQuietZone(main.Format, association.AddOn);
        BarcodeQuietZone selectedQuietZone = ResolveQuietZone(settings, requiredQuietZone);
        BarcodeSize size = ResolveSize(main.Format, modules.Length, settings.Sizing, selectedQuietZone);
        double guardDescent = eanUpcOptions != null && eanUpcOptions.ExtendGuardBars
            ? StandardGuardDescentModules
            : 0.0;
        EanUpcGuardBar[] guardBars = CreateGuardBars(main, guardDescent);
        BarcodeTextLayout? textLayout = CreateTextLayout(
            association,
            payload,
            settings,
            eanUpcOptions,
            size,
            selectedQuietZone,
            addOnOffset,
            guardBars);

        BarcodeSymbol symbol = new BarcodeSymbol();
        symbol.Format = main.Format;
        symbol.Dimension = BarcodeDimension.Linear;
        symbol.WidthModules = modules.Length;
        symbol.HeightModules = 1;
        symbol.Modules = modules;
        symbol.Payload = ResultCopier.CopyPayload(payload);
        symbol.Size = size;
        symbol.RequiredQuietZone = requiredQuietZone;
        symbol.TextLayout = textLayout;
        symbol.SymbologyIdentifier = BarcodeSymbologyIdentifiers.EanUpc;
        symbol.HasChecksum = true;
        symbol.ChecksumValue = main.ChecksumValue;

        EanUpcLayoutResult result = new EanUpcLayoutResult();
        result.Symbol = symbol;
        result.GuardBars = guardBars;
        result.MainModuleCount = main.Modules.Length;
        result.AddOnOffsetModules = addOnOffset;
        result.AddOnModuleCount = association.AddOn == null ? 0 : association.AddOn.Modules.Length;
        result.AddOnSeparationModules = separationModules;
        result.GuardDescentModules = guardDescent;
        return result;
    }

    static void ValidateAssociation(EanUpcAddonAssociation association)
    {
        if (association == null || association.Main == null)
        {
            throw new BarcodeValidationException("EAN/UPC layout requires a main symbol.");
        }

        ValidateMainData(association.Main);
        if (association.AddOn != null)
        {
            if (association.Main.Format == BarcodeFormat.Ean8)
            {
                throw new BarcodeValidationException("EAN-8 add-on association is not supported by the standard layout.");
            }

            ValidateAddOnData(association.AddOn);
        }
    }

    static void ValidateMainData(EanUpcEncodedData encoded)
    {
        int expectedTextLength;
        int expectedModuleCount;
        int expectedParityLength;
        int centerOffset;
        int endOffset;
        EanUpcGuardPattern endPattern;
        switch (encoded.Format)
        {
            case BarcodeFormat.Ean13:
                expectedTextLength = EanUpcEncoder.Ean13CompleteLength;
                expectedModuleCount = EanUpcEncoder.Ean13ModuleCount;
                expectedParityLength = EanUpcPatterns.Ean13ParityLength;
                centerOffset = 45;
                endOffset = 92;
                endPattern = EanUpcGuardPattern.Normal;
                break;
            case BarcodeFormat.Ean8:
                expectedTextLength = EanUpcEncoder.Ean8CompleteLength;
                expectedModuleCount = EanUpcEncoder.Ean8ModuleCount;
                expectedParityLength = 4;
                centerOffset = 31;
                endOffset = 64;
                endPattern = EanUpcGuardPattern.Normal;
                break;
            case BarcodeFormat.UpcA:
                expectedTextLength = EanUpcEncoder.UpcACompleteLength;
                expectedModuleCount = EanUpcEncoder.UpcAModuleCount;
                expectedParityLength = EanUpcPatterns.Ean13ParityLength;
                centerOffset = 45;
                endOffset = 92;
                endPattern = EanUpcGuardPattern.Normal;
                break;
            case BarcodeFormat.UpcE:
                expectedTextLength = EanUpcEncoder.UpcECompleteLength;
                expectedModuleCount = EanUpcEncoder.UpcEModuleCount;
                expectedParityLength = EanUpcPatterns.UpcEParityLength;
                centerOffset = -1;
                endOffset = 45;
                endPattern = EanUpcGuardPattern.UpcEEnd;
                break;
            default:
                throw new BarcodeValidationException("EAN/UPC layout requires EAN-13, EAN-8, UPC-A, or UPC-E data.");
        }

        if (encoded.Text == null || encoded.Text.Length != expectedTextLength ||
            encoded.Digits == null || encoded.Digits.Length != expectedTextLength ||
            encoded.LeftParity == null || encoded.LeftParity.Length != expectedParityLength ||
            encoded.Modules == null || encoded.Modules.Length != expectedModuleCount)
        {
            throw new BarcodeValidationException("Encoded EAN/UPC data has invalid dimensions.");
        }

        string checksumText = encoded.Format == BarcodeFormat.UpcE
            ? encoded.ExpandedUpcAText
            : encoded.Text;
        if (!Gs1Modulo10.VerifyCompleteDigits(checksumText) ||
            encoded.ChecksumValue != encoded.Digits[encoded.Digits.Length - 1])
        {
            throw new BarcodeValidationException("Encoded EAN/UPC data has an invalid check digit.");
        }

        ValidateDigitText(encoded.Text, encoded.Digits);
        ValidateGuardPattern(encoded.Modules, 0, EanUpcGuardPattern.Normal);
        if (centerOffset >= 0)
        {
            ValidateGuardPattern(encoded.Modules, centerOffset, EanUpcGuardPattern.Center);
        }

        ValidateGuardPattern(encoded.Modules, endOffset, endPattern);
    }

    static void ValidateAddOnData(EanUpcEncodedData encoded)
    {
        int expectedLength;
        int expectedModuleCount;
        if (encoded.Format == BarcodeFormat.EanUpcAddon2)
        {
            expectedLength = EanUpcAddon.TwoDigitLength;
            expectedModuleCount = EanUpcAddon.TwoDigitModuleCount;
        }
        else if (encoded.Format == BarcodeFormat.EanUpcAddon5)
        {
            expectedLength = EanUpcAddon.FiveDigitLength;
            expectedModuleCount = EanUpcAddon.FiveDigitModuleCount;
        }
        else
        {
            throw new BarcodeValidationException("EAN/UPC layout add-on format is invalid.");
        }

        if (encoded.Text == null || encoded.Text.Length != expectedLength ||
            encoded.Digits == null || encoded.Digits.Length != expectedLength ||
            encoded.LeftParity == null || encoded.LeftParity.Length != expectedLength ||
            encoded.Modules == null || encoded.Modules.Length != expectedModuleCount)
        {
            throw new BarcodeValidationException("Encoded EAN/UPC add-on data has invalid dimensions.");
        }

        ValidateDigitText(encoded.Text, encoded.Digits);
        ValidateGuardPattern(encoded.Modules, 0, EanUpcGuardPattern.AddonStart);
        int separatorIndex = 1;
        while (separatorIndex < expectedLength)
        {
            int separatorOffset = 4 + (separatorIndex * EanUpcPatterns.DigitModuleCount) +
                ((separatorIndex - 1) * 2);
            ValidateGuardPattern(encoded.Modules, separatorOffset, EanUpcGuardPattern.AddonSeparator);
            separatorIndex = separatorIndex + 1;
        }
    }

    static void ValidateDigitText(string text, int[] digits)
    {
        int index = 0;
        while (index < text.Length)
        {
            char character = text[index];
            if (character < '0' || character > '9' || digits[index] != character - '0')
            {
                throw new BarcodeValidationException("Encoded EAN/UPC digits do not match their text.");
            }

            index = index + 1;
        }
    }

    static void ValidateGuardPattern(byte[] modules, int offset, EanUpcGuardPattern pattern)
    {
        byte[] expected = EanUpcPatterns.GetGuardModules(pattern);
        int index = 0;
        while (index < expected.Length)
        {
            if (modules[offset + index] != expected[index])
            {
                throw new BarcodeValidationException("Encoded EAN/UPC guard pattern is invalid.");
            }

            index = index + 1;
        }
    }

    static int ResolveAddOnSeparation(BarcodeFormat format, EanUpcEncodedData? addOn)
    {
        if (addOn == null)
        {
            return 0;
        }

        return format == BarcodeFormat.UpcA
            ? UpcAAddOnSeparationModules
            : StandardAddOnSeparationModules;
    }

    static byte[] CombineModules(
        byte[] mainModules,
        EanUpcEncodedData? addOn,
        int separationModules)
    {
        if (addOn == null)
        {
            byte[] mainCopy = new byte[mainModules.Length];
            int copyIndex = 0;
            while (copyIndex < mainModules.Length)
            {
                mainCopy[copyIndex] = mainModules[copyIndex];
                copyIndex = copyIndex + 1;
            }

            return mainCopy;
        }

        int mainAndGap = CheckedMath.Add(mainModules.Length, separationModules);
        int totalLength = CheckedMath.Add(mainAndGap, addOn.Modules.Length);
        byte[] combined = new byte[totalLength];
        int mainIndex = 0;
        while (mainIndex < mainModules.Length)
        {
            combined[mainIndex] = mainModules[mainIndex];
            mainIndex = mainIndex + 1;
        }

        int addOnIndex = 0;
        while (addOnIndex < addOn.Modules.Length)
        {
            combined[mainAndGap + addOnIndex] = addOn.Modules[addOnIndex];
            addOnIndex = addOnIndex + 1;
        }

        return combined;
    }

    static BarcodeQuietZone CreateRequiredQuietZone(
        BarcodeFormat format,
        EanUpcEncodedData? addOn)
    {
        BarcodeQuietZone quietZone = new BarcodeQuietZone();
        switch (format)
        {
            case BarcodeFormat.Ean13:
                quietZone.LeftModules = Ean13LeftQuietZoneModules;
                quietZone.RightModules = Ean13RightQuietZoneModules;
                break;
            case BarcodeFormat.Ean8:
                quietZone.LeftModules = Ean8LeftQuietZoneModules;
                quietZone.RightModules = Ean8RightQuietZoneModules;
                break;
            case BarcodeFormat.UpcA:
                quietZone.LeftModules = UpcALeftQuietZoneModules;
                quietZone.RightModules = UpcARightQuietZoneModules;
                break;
            case BarcodeFormat.UpcE:
                quietZone.LeftModules = UpcELeftQuietZoneModules;
                quietZone.RightModules = UpcERightQuietZoneModules;
                break;
            default:
                throw new BarcodeValidationException("EAN/UPC quiet-zone format is invalid.");
        }

        if (addOn != null)
        {
            quietZone.RightModules = AddOnRightQuietZoneModules;
        }

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
                if (minimumCustom.LeftModules < requiredQuietZone.LeftModules ||
                    minimumCustom.RightModules < requiredQuietZone.RightModules)
                {
                    throw new BarcodeEncodingException("Custom EAN/UPC quiet zones must meet the format minimums.");
                }

                return CopyLogicalQuietZone(minimumCustom);
            case BarcodeQuietZonePolicy.ExactCustom:
                BarcodeQuietZone exactCustom = RequireCustomQuietZone(options);
                ValidateQuietZone(exactCustom);
                return CopyLogicalQuietZone(exactCustom);
            default:
                throw new BarcodeEncodingException("EAN/UPC quiet-zone policy is invalid.");
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
            throw new BarcodeEncodingException("A custom EAN/UPC quiet zone is required by the selected policy.");
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
            throw new BarcodeEncodingException("EAN/UPC quiet-zone dimensions must not be negative.");
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
        BarcodeFormat format,
        int symbolWidthModules,
        BarcodeSizing? sizing,
        BarcodeQuietZone quietZone)
    {
        BarcodeSizing settings = sizing == null ? new BarcodeSizing() : sizing;
        ValidateSizing(settings, symbolWidthModules);
        int horizontalQuietZone = CheckedMath.Add(quietZone.LeftModules, quietZone.RightModules);
        int widthModules = CheckedMath.Add(symbolWidthModules, horizontalQuietZone);
        int verticalQuietZone = CheckedMath.Add(quietZone.TopModules, quietZone.BottomModules);
        int heightModules = CheckedMath.Add(1, verticalQuietZone);

        double moduleSize = settings.ModuleSizeMillimetres;
        if (moduleSize == 0.0 && settings.RequestedWidthMillimetres > 0.0)
        {
            moduleSize = settings.RequestedWidthMillimetres / (double)widthModules;
        }

        if (moduleSize == 0.0)
        {
            moduleSize = NominalModuleSizeMillimetres;
        }

        if (moduleSize < MinimumModuleSizeMillimetres || moduleSize > MaximumModuleSizeMillimetres)
        {
            throw new BarcodeEncodingException("EAN/UPC module size must be between 0.264 and 0.660 millimetres.");
        }

        double widthMillimetres = (double)widthModules * moduleSize;
        if (double.IsInfinity(widthMillimetres))
        {
            throw new BarcodeEncodingException("Resolved EAN/UPC width exceeds the supported physical range.");
        }

        if (settings.RequestedWidthMillimetres > 0.0 &&
            global::System.Math.Abs(widthMillimetres - settings.RequestedWidthMillimetres) > PhysicalComparisonTolerance)
        {
            throw new BarcodeEncodingException("Requested EAN/UPC width conflicts with the module size and quiet zones.");
        }

        double minimumBarHeight = ResolveMinimumBarHeight(format, moduleSize);
        double verticalQuietZoneMillimetres = (double)verticalQuietZone * moduleSize;
        double barHeight = settings.BarHeightMillimetres;
        if (barHeight == 0.0 && settings.RequestedHeightMillimetres > 0.0)
        {
            barHeight = settings.RequestedHeightMillimetres - verticalQuietZoneMillimetres;
        }

        if (barHeight == 0.0)
        {
            barHeight = minimumBarHeight;
        }

        if (barHeight + PhysicalComparisonTolerance < minimumBarHeight)
        {
            throw new BarcodeEncodingException("EAN/UPC bar height is below the minimum for the selected module size.");
        }

        double heightMillimetres = barHeight + verticalQuietZoneMillimetres;
        if (double.IsInfinity(heightMillimetres))
        {
            throw new BarcodeEncodingException("Resolved EAN/UPC height exceeds the supported physical range.");
        }

        if (settings.RequestedHeightMillimetres > 0.0 &&
            global::System.Math.Abs(heightMillimetres - settings.RequestedHeightMillimetres) > PhysicalComparisonTolerance)
        {
            throw new BarcodeEncodingException("Requested EAN/UPC height conflicts with the bar height and quiet zones.");
        }

        BarcodeSize size = new BarcodeSize();
        size.WidthModules = widthModules;
        size.HeightModules = heightModules;
        size.WidthPixels = settings.RequestedWidthPixels;
        size.HeightPixels = settings.RequestedHeightPixels;
        size.WidthMillimetres = widthMillimetres;
        size.HeightMillimetres = heightMillimetres;
        size.DotsPerInch = settings.DotsPerInch;
        size.ModuleSizeMillimetres = moduleSize;
        size.QuietZone = CopyLogicalQuietZone(quietZone);
        return size;
    }

    static double ResolveMinimumBarHeight(BarcodeFormat format, double moduleSize)
    {
        double nominalHeight = format == BarcodeFormat.Ean8
            ? NominalEan8BarHeightMillimetres
            : NominalLongBarHeightMillimetres;
        return nominalHeight * moduleSize / NominalModuleSizeMillimetres;
    }

    static void ValidateSizing(BarcodeSizing sizing, int symbolWidthModules)
    {
        if (sizing.RequestedWidthModules < 0 || sizing.RequestedHeightModules < 0 ||
            sizing.RequestedWidthPixels < 0 || sizing.RequestedHeightPixels < 0)
        {
            throw new BarcodeEncodingException("EAN/UPC sizing dimensions must not be negative.");
        }

        if (sizing.RequestedWidthModules != 0 && sizing.RequestedWidthModules != symbolWidthModules)
        {
            throw new BarcodeEncodingException("Requested EAN/UPC module width does not match the encoded symbol.");
        }

        if (sizing.RequestedHeightModules != 0 && sizing.RequestedHeightModules != 1)
        {
            throw new BarcodeEncodingException("A logical EAN/UPC symbol must have one module row.");
        }

        ValidateOptionalMeasurement(sizing.RequestedWidthMillimetres, "Requested EAN/UPC width must be finite and non-negative.");
        ValidateOptionalMeasurement(sizing.RequestedHeightMillimetres, "Requested EAN/UPC height must be finite and non-negative.");
        ValidateOptionalMeasurement(sizing.DotsPerInch, "EAN/UPC resolution must be finite and non-negative.");
        ValidateOptionalMeasurement(sizing.ModuleSizeMillimetres, "EAN/UPC module size must be finite and non-negative.");
        ValidateOptionalMeasurement(sizing.BarHeightMillimetres, "EAN/UPC bar height must be finite and non-negative.");
        ValidateOptionalMeasurement(sizing.TargetAspectRatio, "EAN/UPC target aspect ratio must be finite and non-negative.");
    }

    static void ValidateOptionalMeasurement(double value, string message)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0.0)
        {
            throw new BarcodeEncodingException(message);
        }
    }

    static EanUpcGuardBar[] CreateGuardBars(EanUpcEncodedData main, double descent)
    {
        global::System.Collections.Generic.List<EanUpcGuardBar> bars =
            new global::System.Collections.Generic.List<EanUpcGuardBar>();
        AddDarkRuns(bars, main.Modules, 0, 3, descent);
        if (main.Format == BarcodeFormat.Ean13 || main.Format == BarcodeFormat.UpcA)
        {
            AddDarkRuns(bars, main.Modules, 45, 5, descent);
            AddDarkRuns(bars, main.Modules, 92, 3, descent);
        }
        else if (main.Format == BarcodeFormat.Ean8)
        {
            AddDarkRuns(bars, main.Modules, 31, 5, descent);
            AddDarkRuns(bars, main.Modules, 64, 3, descent);
        }
        else
        {
            AddDarkRuns(bars, main.Modules, 45, 6, descent);
        }

        if (main.Format == BarcodeFormat.UpcA)
        {
            AddDarkRuns(bars, main.Modules, 3, 7, descent);
            AddDarkRuns(bars, main.Modules, 85, 7, descent);
        }

        return bars.ToArray();
    }

    static void AddDarkRuns(
        global::System.Collections.Generic.List<EanUpcGuardBar> bars,
        byte[] modules,
        int offset,
        int count,
        double descent)
    {
        int end = offset + count;
        int index = offset;
        while (index < end)
        {
            if (modules[index] == 0)
            {
                index = index + 1;
                continue;
            }

            int start = index;
            while (index < end && modules[index] != 0)
            {
                index = index + 1;
            }

            EanUpcGuardBar bar = new EanUpcGuardBar();
            bar.ModuleOffset = start;
            bar.ModuleCount = index - start;
            bar.DescentModules = descent;
            bars.Add(bar);
        }
    }

    static BarcodeTextLayout? CreateTextLayout(
        EanUpcAddonAssociation association,
        BarcodePayload payload,
        BarcodeEncodeOptions options,
        EanUpcOptions? eanUpcOptions,
        BarcodeSize size,
        BarcodeQuietZone quietZone,
        int addOnOffset,
        EanUpcGuardBar[] guardBars)
    {
        if (options.TextMode == BarcodeTextMode.Hidden)
        {
            return null;
        }

        global::System.Collections.Generic.List<BarcodeTextRun> runs =
            new global::System.Collections.Generic.List<BarcodeTextRun>();
        if (options.TextMode == BarcodeTextMode.Standard)
        {
            AddStandardMainText(runs, association.Main, quietZone.LeftModules, size.HeightModules);
            if (association.AddOn != null)
            {
                AddStandardAddOnText(runs, association.AddOn, quietZone.LeftModules + addOnOffset);
            }

            if (eanUpcOptions != null && eanUpcOptions.ShowQuietZoneIndicators)
            {
                AddQuietZoneIndicators(runs, association, quietZone, size);
            }
        }
        else
        {
            string text = SelectNonStandardText(payload, options);
            BarcodeTextRun run = CreateTextRun(
                text,
                quietZone.LeftModules,
                size.HeightModules + HumanReadableHeightModules,
                association.Main.Modules.Length,
                HumanReadableHeightModules);
            runs.Add(run);
        }

        double top = association.AddOn == null ? (double)size.HeightModules : -HumanReadableHeightModules;
        double bottom = (double)size.HeightModules + HumanReadableHeightModules;
        BarcodeRectangle bounds = new BarcodeRectangle();
        bounds.X = 0.0;
        bounds.Y = top;
        bounds.Width = size.WidthModules;
        bounds.Height = bottom - top;

        BarcodeSize textSize = CopySize(size);
        int reservedRows = association.AddOn == null ? 7 : 14;
        textSize.HeightModules = CheckedMath.Add(size.HeightModules, reservedRows);

        BarcodeTextLayout layout = new BarcodeTextLayout();
        layout.Runs = runs.ToArray();
        layout.BarExtensions = CreateBarExtensions(
            guardBars,
            quietZone);
        layout.ReservedHeight = (double)reservedRows;
        layout.Bounds = bounds;
        layout.Size = textSize;
        return layout;
    }

    static BarcodeTextLayout? CreateAddOnTextLayout(
        EanUpcEncodedData encoded,
        BarcodePayload payload,
        BarcodeEncodeOptions options,
        BarcodeSize size,
        BarcodeQuietZone quietZone)
    {
        if (options.TextMode == BarcodeTextMode.Hidden)
        {
            return null;
        }

        global::System.Collections.Generic.List<BarcodeTextRun> runs =
            new global::System.Collections.Generic.List<BarcodeTextRun>();
        if (options.TextMode == BarcodeTextMode.Standard)
        {
            AddStandardAddOnText(runs, encoded, quietZone.LeftModules);
        }
        else
        {
            string text = SelectNonStandardText(payload, options);
            runs.Add(CreateTextRun(
                text,
                quietZone.LeftModules,
                0.0,
                encoded.Modules.Length,
                HumanReadableHeightModules));
        }

        BarcodeRectangle bounds = new BarcodeRectangle();
        bounds.X = 0.0;
        bounds.Y = -HumanReadableHeightModules;
        bounds.Width = size.WidthModules;
        bounds.Height = size.HeightModules + HumanReadableHeightModules;
        BarcodeSize textSize = CopySize(size);
        textSize.HeightModules = CheckedMath.Add(size.HeightModules, 7);
        BarcodeTextLayout layout = new BarcodeTextLayout();
        layout.Runs = runs.ToArray();
        layout.BarExtensions = new BarcodeRectangle[0];
        layout.ReservedHeight = HumanReadableHeightModules;
        layout.Bounds = bounds;
        layout.Size = textSize;
        return layout;
    }

    static BarcodeRectangle[] CreateBarExtensions(
        EanUpcGuardBar[] guardBars,
        BarcodeQuietZone quietZone)
    {
        global::System.Collections.Generic.List<BarcodeRectangle> rectangles =
            new global::System.Collections.Generic.List<BarcodeRectangle>();
        int index = 0;
        while (index < guardBars.Length)
        {
            EanUpcGuardBar guardBar = guardBars[index];
            if (guardBar.DescentModules > 0.0)
            {
                BarcodeRectangle rectangle = new BarcodeRectangle();
                rectangle.X = quietZone.LeftModules
                    + guardBar.ModuleOffset;
                rectangle.Y = quietZone.TopModules + 1;
                rectangle.Width = guardBar.ModuleCount;
                rectangle.Height = guardBar.DescentModules;
                rectangles.Add(rectangle);
            }

            index = index + 1;
        }

        return rectangles.ToArray();
    }

    static void AddStandardMainText(
        global::System.Collections.Generic.List<BarcodeTextRun> runs,
        EanUpcEncodedData main,
        int leftQuietZone,
        int symbolHeight)
    {
        double baseline = (double)symbolHeight + HumanReadableHeightModules;
        if (main.Format == BarcodeFormat.Ean13)
        {
            runs.Add(CreateTextRun(main.Text.Substring(0, 1), leftQuietZone - 7, baseline, 7.0, HumanReadableHeightModules));
            runs.Add(CreateTextRun(main.Text.Substring(1, 6), leftQuietZone + 3, baseline, 42.0, HumanReadableHeightModules));
            runs.Add(CreateTextRun(main.Text.Substring(7, 6), leftQuietZone + 50, baseline, 42.0, HumanReadableHeightModules));
        }
        else if (main.Format == BarcodeFormat.Ean8)
        {
            runs.Add(CreateTextRun(main.Text.Substring(0, 4), leftQuietZone + 3, baseline, 28.0, HumanReadableHeightModules));
            runs.Add(CreateTextRun(main.Text.Substring(4, 4), leftQuietZone + 36, baseline, 28.0, HumanReadableHeightModules));
        }
        else if (main.Format == BarcodeFormat.UpcA)
        {
            runs.Add(CreateTextRun(main.Text.Substring(0, 1), leftQuietZone - 7, baseline, 7.0, HumanReadableHeightModules));
            runs.Add(CreateTextRun(main.Text.Substring(1, 5), leftQuietZone + 10, baseline, 35.0, HumanReadableHeightModules));
            runs.Add(CreateTextRun(main.Text.Substring(6, 5), leftQuietZone + 50, baseline, 35.0, HumanReadableHeightModules));
            runs.Add(CreateTextRun(main.Text.Substring(11, 1), leftQuietZone + 95, baseline, 7.0, HumanReadableHeightModules));
        }
        else
        {
            runs.Add(CreateTextRun(main.Text.Substring(0, 1), leftQuietZone - 7, baseline, 7.0, HumanReadableHeightModules));
            runs.Add(CreateTextRun(main.Text.Substring(1, 6), leftQuietZone + 3, baseline, 42.0, HumanReadableHeightModules));
            runs.Add(CreateTextRun(main.Text.Substring(7, 1), leftQuietZone + 51, baseline, 7.0, HumanReadableHeightModules));
        }
    }

    static void AddStandardAddOnText(
        global::System.Collections.Generic.List<BarcodeTextRun> runs,
        EanUpcEncodedData addOn,
        int offset)
    {
        BarcodeTextRun run = CreateTextRun(
            addOn.Text,
            offset + 4,
            0.0,
            addOn.Modules.Length - 4,
            HumanReadableHeightModules);
        runs.Add(run);
    }

    static void AddQuietZoneIndicators(
        global::System.Collections.Generic.List<BarcodeTextRun> runs,
        EanUpcAddonAssociation association,
        BarcodeQuietZone quietZone,
        BarcodeSize size)
    {
        double baseline = (double)size.HeightModules + HumanReadableHeightModules;
        if (association.Main.Format == BarcodeFormat.Ean8)
        {
            runs.Add(CreateTextRun("<", 0.0, baseline, quietZone.LeftModules, HumanReadableHeightModules));
        }

        bool showRight = association.Main.Format == BarcodeFormat.Ean13 ||
            association.Main.Format == BarcodeFormat.Ean8 || association.AddOn != null;
        if (showRight)
        {
            runs.Add(CreateTextRun(">", size.WidthModules - quietZone.RightModules, baseline,
                quietZone.RightModules, HumanReadableHeightModules));
        }
    }

    static BarcodeTextRun CreateTextRun(
        string text,
        double x,
        double baseline,
        double width,
        double height)
    {
        BarcodeTextRun run = new BarcodeTextRun();
        run.Text = text;
        run.X = x;
        run.BaselineY = baseline;
        run.Width = width;
        run.Height = height;
        return run;
    }

    static string SelectNonStandardText(BarcodePayload payload, BarcodeEncodeOptions options)
    {
        if (options.TextMode == BarcodeTextMode.RawPayload)
        {
            if (payload.RawBytes != null)
            {
                return ConvertRawBytesToText(payload.RawBytes);
            }

            return payload.Text == null ? "" : payload.Text;
        }

        if (options.TextMode == BarcodeTextMode.Custom)
        {
            if (options.CustomText == null)
            {
                throw new BarcodeEncodingException("Custom EAN/UPC human-readable text must not be null.");
            }

            return options.CustomText;
        }

        throw new BarcodeEncodingException("EAN/UPC human-readable text mode is invalid.");
    }

    static string ConvertRawBytesToText(byte[] bytes)
    {
        char[] characters = new char[bytes.Length];
        int index = 0;
        while (index < bytes.Length)
        {
            characters[index] = (char)bytes[index];
            index = index + 1;
        }

        return new string(characters);
    }

    static BarcodeSize CopySize(BarcodeSize source)
    {
        BarcodeSize copy = new BarcodeSize();
        copy.WidthModules = source.WidthModules;
        copy.HeightModules = source.HeightModules;
        copy.WidthPixels = source.WidthPixels;
        copy.HeightPixels = source.HeightPixels;
        copy.WidthMillimetres = source.WidthMillimetres;
        copy.HeightMillimetres = source.HeightMillimetres;
        copy.DotsPerInch = source.DotsPerInch;
        copy.ModuleSizeMillimetres = source.ModuleSizeMillimetres;
        if (source.QuietZone != null)
        {
            copy.QuietZone = CopyLogicalQuietZone(source.QuietZone);
        }

        return copy;
    }
}
