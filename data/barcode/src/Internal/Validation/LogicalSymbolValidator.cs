namespace barcode;

internal class LogicalSymbolValidationState
{
    internal BarcodeValidationMode Mode;
    internal BarcodeFormat Format;
    internal string Standard = "Barcode symbol validation";
    internal string? Profile;
    internal global::System.Collections.Generic.List<BarcodeValidationIssue> Issues;

    internal LogicalSymbolValidationState(BarcodeValidationMode mode)
    {
        Mode = mode;
        Issues = new global::System.Collections.Generic.List<BarcodeValidationIssue>();
    }
}

internal static class LogicalSymbolValidator
{
    internal static BarcodeValidationReport ValidateSymbolRequest(
        BarcodeValidationRequest request)
    {
        if (request == null)
        {
            throw new BarcodeValidationException(
                "A barcode symbol validation request is required.");
        }

        if (request.Mode != BarcodeValidationMode.Strict &&
            request.Mode != BarcodeValidationMode.Tolerant)
        {
            throw new BarcodeValidationException(
                "Barcode validation mode is invalid.");
        }

        LogicalSymbolValidationState state =
            new LogicalSymbolValidationState(request.Mode);
        BarcodeSymbol? symbol = request.Symbol;
        if (symbol == null)
        {
            AddSymbolIssue(
                state,
                "SYMBOL_MISSING",
                "A logical barcode symbol is required.",
                "Symbol",
                -1,
                "an encoded logical symbol",
                "null");
            return CreateSymbolReport(state, request.Mode);
        }

        BarcodeFormat format = request.Format == BarcodeFormat.Automatic
            ? symbol.Format
            : request.Format;
        state.Format = format;
        ResolveSymbolStandard(state, format);
        if (request.Format != BarcodeFormat.Automatic &&
            symbol.Format != request.Format)
        {
            AddSymbolIssue(
                state,
                "SYMBOL_FORMAT_CONFLICT",
                "The validation and logical-symbol formats conflict.",
                "Format",
                -1,
                request.Format.ToString(),
                symbol.Format.ToString());
        }

        if (!FormatDispatcher.IsSupportedFormat(format))
        {
            AddSymbolIssue(
                state,
                "SYMBOL_FORMAT_UNSUPPORTED",
                "The logical symbol format is unsupported.",
                "Format",
                -1,
                "a supported barcode format",
                ((int)format).ToString(
                    global::System.Globalization.CultureInfo.InvariantCulture));
            return CreateSymbolReport(state, request.Mode);
        }

        ValidateSymbolDimension(state, symbol, format);
        bool modulesValid = ValidateSymbolModuleEnvelope(
            state,
            symbol,
            format);
        ValidateSymbolQuietZone(state, symbol, format);
        ValidateSymbolIdentifier(state, symbol, format);
        ValidateSymbolPayloadMetadata(state, symbol, format, request.ValidateGs1Profile);
        ValidateSymbolBearerBars(state, symbol, format);
        if (modulesValid)
        {
            DispatchSymbolValidation(state, symbol, format);
        }

        return CreateSymbolReport(state, request.Mode);
    }

    static void ResolveSymbolStandard(
        LogicalSymbolValidationState state,
        BarcodeFormat format)
    {
        if (format == BarcodeFormat.Code128 || format == BarcodeFormat.Gs1128)
        {
            state.Standard = BarcodeStandards.Code128;
        }
        else if (format == BarcodeFormat.Ean8 || format == BarcodeFormat.Ean13 ||
            format == BarcodeFormat.UpcA || format == BarcodeFormat.UpcE ||
            format == BarcodeFormat.EanUpcAddon2 ||
            format == BarcodeFormat.EanUpcAddon5)
        {
            state.Standard = BarcodeStandards.EanUpc;
        }
        else if (format == BarcodeFormat.Code39)
        {
            state.Standard = BarcodeStandards.Code39;
        }
        else if (format == BarcodeFormat.Itf || format == BarcodeFormat.Itf14)
        {
            state.Standard = BarcodeStandards.Itf;
        }
        else if (format == BarcodeFormat.Codabar)
        {
            state.Standard = BarcodeStandards.Codabar;
        }
        else if (format == BarcodeFormat.DataMatrix ||
            format == BarcodeFormat.Gs1DataMatrix)
        {
            state.Standard = BarcodeStandards.DataMatrix;
        }
        else if (format == BarcodeFormat.Pdf417 ||
            format == BarcodeFormat.CompactPdf417 ||
            format == BarcodeFormat.MacroPdf417)
        {
            state.Standard = BarcodeStandards.Pdf417;
        }

        if (format == BarcodeFormat.Gs1128 ||
            format == BarcodeFormat.Gs1DataMatrix ||
            format == BarcodeFormat.Itf14)
        {
            state.Profile = BarcodeStandards.Gs1GeneralSpecifications;
        }
    }

    static void ValidateSymbolDimension(
        LogicalSymbolValidationState state,
        BarcodeSymbol symbol,
        BarcodeFormat format)
    {
        BarcodeDimension expected = FormatDispatcher.ResolveDimension(format);
        if (symbol.Dimension != expected)
        {
            AddSymbolIssue(
                state,
                "SYMBOL_DIMENSION_CLASS_INVALID",
                "The symbol dimension class does not match its format.",
                "Symbol.Dimension",
                -1,
                expected.ToString(),
                symbol.Dimension.ToString());
        }

        if (symbol.WidthModules <= 0 || symbol.HeightModules <= 0)
        {
            AddSymbolIssue(
                state,
                "SYMBOL_DIMENSIONS_INVALID",
                "Logical symbol dimensions must be positive.",
                "Symbol.WidthModulesHeightModules",
                -1,
                "positive module dimensions",
                $"{symbol.WidthModules}x{symbol.HeightModules}");
        }
        else if (expected == BarcodeDimension.Linear &&
            symbol.HeightModules != 1)
        {
            AddSymbolIssue(
                state,
                "LINEAR_SYMBOL_HEIGHT_INVALID",
                "A logical linear symbol must contain exactly one module row.",
                "Symbol.HeightModules",
                -1,
                "1",
                symbol.HeightModules.ToString(
                    global::System.Globalization.CultureInfo.InvariantCulture));
        }
    }

    static bool ValidateSymbolModuleEnvelope(
        LogicalSymbolValidationState state,
        BarcodeSymbol symbol,
        BarcodeFormat format)
    {
        if (symbol.WidthModules <= 0 || symbol.HeightModules <= 0)
        {
            return false;
        }

        long expected = (long)symbol.WidthModules * symbol.HeightModules;
        if (expected > DecodingContext.DefaultMaximumModules)
        {
            AddSymbolIssue(
                state,
                "SYMBOL_MODULE_CAPACITY_EXCEEDED",
                "The logical symbol exceeds the supported module capacity.",
                "Symbol.Modules",
                -1,
                $"at most {DecodingContext.DefaultMaximumModules} modules",
                expected.ToString(
                    global::System.Globalization.CultureInfo.InvariantCulture));
            return false;
        }

        if (symbol.Modules == null)
        {
            if (format == BarcodeFormat.Itf14 && symbol.Codewords != null)
            {
                return true;
            }

            AddSymbolIssue(
                state,
                "SYMBOL_MODULES_MISSING",
                "Logical symbol modules are required.",
                "Symbol.Modules",
                -1,
                expected.ToString(
                    global::System.Globalization.CultureInfo.InvariantCulture),
                "null");
            return false;
        }

        if (symbol.Modules.Length != expected)
        {
            AddSymbolIssue(
                state,
                "SYMBOL_MODULE_COUNT_INVALID",
                "The module buffer length does not match the logical dimensions.",
                "Symbol.Modules",
                -1,
                expected.ToString(
                    global::System.Globalization.CultureInfo.InvariantCulture),
                symbol.Modules.Length.ToString(
                    global::System.Globalization.CultureInfo.InvariantCulture));
            return false;
        }

        return true;
    }

    static void ValidateSymbolQuietZone(
        LogicalSymbolValidationState state,
        BarcodeSymbol symbol,
        BarcodeFormat format)
    {
        BarcodeQuietZone? quiet = symbol.RequiredQuietZone;
        if (quiet == null)
        {
            AddSymbolIssue(
                state,
                "SYMBOL_QUIET_ZONE_MISSING",
                "The required logical quiet-zone metadata is missing.",
                "Symbol.RequiredQuietZone",
                -1,
                "format-specific quiet-zone requirements",
                "null");
            return;
        }

        int left = 0;
        int top = 0;
        int right = 0;
        int bottom = 0;
        if (format == BarcodeFormat.Code128 || format == BarcodeFormat.Gs1128 ||
            format == BarcodeFormat.Code39 || format == BarcodeFormat.Itf ||
            format == BarcodeFormat.Itf14 || format == BarcodeFormat.Codabar)
        {
            left = 10;
            right = 10;
        }
        else if (format == BarcodeFormat.Ean13)
        {
            left = EanUpcLayout.Ean13LeftQuietZoneModules;
            right = EanUpcLayout.Ean13RightQuietZoneModules;
        }
        else if (format == BarcodeFormat.Ean8)
        {
            left = EanUpcLayout.Ean8LeftQuietZoneModules;
            right = EanUpcLayout.Ean8RightQuietZoneModules;
        }
        else if (format == BarcodeFormat.UpcA)
        {
            left = EanUpcLayout.UpcALeftQuietZoneModules;
            right = EanUpcLayout.UpcARightQuietZoneModules;
        }
        else if (format == BarcodeFormat.UpcE)
        {
            left = EanUpcLayout.UpcELeftQuietZoneModules;
            right = EanUpcLayout.UpcERightQuietZoneModules;
        }
        else if (format == BarcodeFormat.EanUpcAddon2 ||
            format == BarcodeFormat.EanUpcAddon5)
        {
            left = EanUpcLayout.AddOnLeftQuietZoneModules;
            right = EanUpcLayout.AddOnRightQuietZoneModules;
        }
        else if (format == BarcodeFormat.DataMatrix ||
            format == BarcodeFormat.Gs1DataMatrix)
        {
            left = DataMatrixLayout.StandardQuietZoneModules;
            top = DataMatrixLayout.StandardQuietZoneModules;
            right = DataMatrixLayout.StandardQuietZoneModules;
            bottom = DataMatrixLayout.StandardQuietZoneModules;
        }
        else
        {
            left = Pdf417Layout.StandardQuietZoneModules;
            top = Pdf417Layout.StandardQuietZoneModules;
            right = Pdf417Layout.StandardQuietZoneModules;
            bottom = Pdf417Layout.StandardQuietZoneModules;
        }

        if (quiet.LeftModules < left || quiet.TopModules < top ||
            quiet.RightModules < right || quiet.BottomModules < bottom)
        {
            AddSymbolIssue(
                state,
                "SYMBOL_QUIET_ZONE_INSUFFICIENT",
                "The logical quiet-zone requirement is below the format minimum.",
                "Symbol.RequiredQuietZone",
                -1,
                $"at least {left},{top},{right},{bottom} modules",
                $"{quiet.LeftModules},{quiet.TopModules},{quiet.RightModules},{quiet.BottomModules}");
        }
    }

    static void ValidateSymbolIdentifier(
        LogicalSymbolValidationState state,
        BarcodeSymbol symbol,
        BarcodeFormat format)
    {
        string expected = ResolveSymbolIdentifier(format);
        if (!IsSymbolIdentifierAccepted(
            symbol.SymbologyIdentifier,
            format,
            symbol.HasChecksum))
        {
            AddSymbolIssue(
                state,
                "SYMBOL_IDENTIFIER_INVALID",
                "The ISO/IEC 15424 symbology identifier does not match the symbol format.",
                "Symbol.SymbologyIdentifier",
                -1,
                expected,
                symbol.SymbologyIdentifier == null
                    ? "null"
                    : symbol.SymbologyIdentifier);
        }
    }

    static bool IsSymbolIdentifierAccepted(
        string? identifier,
        BarcodeFormat format,
        bool hasChecksum)
    {
        if (format == BarcodeFormat.Itf)
        {
            if (!hasChecksum)
            {
                return identifier == BarcodeSymbologyIdentifiers.Itf;
            }

            return identifier == BarcodeSymbologyIdentifiers.Itf14 ||
                identifier == "]I3";
        }

        if (format == BarcodeFormat.Code39)
        {
            return identifier == BarcodeSymbologyIdentifiers.Code39 ||
                identifier == "]A1" || identifier == "]A3" ||
                identifier == "]A4" || identifier == "]A5" ||
                identifier == "]A7";
        }

        return identifier == ResolveSymbolIdentifier(format);
    }

    static string ResolveSymbolIdentifier(BarcodeFormat format)
    {
        switch (format)
        {
            case BarcodeFormat.Code128:
                return BarcodeSymbologyIdentifiers.Code128;
            case BarcodeFormat.Gs1128:
                return BarcodeSymbologyIdentifiers.Gs1128;
            case BarcodeFormat.Ean8:
            case BarcodeFormat.Ean13:
            case BarcodeFormat.UpcA:
            case BarcodeFormat.UpcE:
            case BarcodeFormat.EanUpcAddon2:
            case BarcodeFormat.EanUpcAddon5:
                return BarcodeSymbologyIdentifiers.EanUpc;
            case BarcodeFormat.Code39:
                return BarcodeSymbologyIdentifiers.Code39;
            case BarcodeFormat.Itf:
                return BarcodeSymbologyIdentifiers.Itf;
            case BarcodeFormat.Itf14:
                return BarcodeSymbologyIdentifiers.Itf14;
            case BarcodeFormat.Codabar:
                return BarcodeSymbologyIdentifiers.Codabar;
            case BarcodeFormat.DataMatrix:
                return BarcodeSymbologyIdentifiers.DataMatrix;
            case BarcodeFormat.Gs1DataMatrix:
                return BarcodeSymbologyIdentifiers.Gs1DataMatrix;
            default:
                return BarcodeSymbologyIdentifiers.Pdf417;
        }
    }

    static void ValidateSymbolPayloadMetadata(
        LogicalSymbolValidationState state,
        BarcodeSymbol symbol,
        BarcodeFormat format,
        bool validateGs1Profile)
    {
        BarcodePayload? payload = symbol.Payload;
        if (payload == null)
        {
            AddSymbolIssue(
                state,
                "SYMBOL_PAYLOAD_MISSING",
                "The logical symbol payload metadata is missing.",
                "Symbol.Payload",
                -1,
                "decoded or encoded payload metadata",
                "null");
            return;
        }

        bool expectsGs1 = format == BarcodeFormat.Gs1128 ||
            format == BarcodeFormat.Gs1DataMatrix ||
            format == BarcodeFormat.Itf14;
        if (payload.IsGs1 != expectsGs1)
        {
            AddSymbolIssue(
                state,
                "SYMBOL_GS1_STATE_INVALID",
                "The payload GS1 state does not match the selected profile.",
                "Symbol.Payload.IsGs1",
                -1,
                expectsGs1.ToString(),
                payload.IsGs1.ToString());
        }

        if (validateGs1Profile && expectsGs1 &&
            format != BarcodeFormat.Itf14)
        {
            ValidateSymbolGs1Text(state, payload.Text);
        }
    }

    static void ValidateSymbolGs1Text(
        LogicalSymbolValidationState state,
        string? text)
    {
        if (text == null || text.Length == 0)
        {
            AddSymbolIssue(
                state,
                "SYMBOL_GS1_DATA_MISSING",
                "GS1 symbol payload metadata requires an element string.",
                "Symbol.Payload.Text",
                0,
                "an unbracketed GS1 element string",
                "missing");
            return;
        }

        Gs1ElementStringParseResult parsed =
            Gs1ElementStringParser.ParseElementString(text);
        int index = 0;
        while (index < parsed.Issues.Length)
        {
            state.Issues.Add(parsed.Issues[index]);
            index = index + 1;
        }
    }

    static void ValidateSymbolBearerBars(
        LogicalSymbolValidationState state,
        BarcodeSymbol symbol,
        BarcodeFormat format)
    {
        BarcodeRectangle[]? bars = symbol.BearerBars;
        if (format != BarcodeFormat.Itf14)
        {
            if (bars != null && bars.Length != 0)
            {
                AddSymbolIssue(
                    state,
                    "SYMBOL_BEARER_BARS_NOT_APPLICABLE",
                    "Bearer bars are applicable only to the ITF-14 profile.",
                    "Symbol.BearerBars",
                    -1,
                    "no bearer bars",
                    bars.Length.ToString(
                        global::System.Globalization.CultureInfo.InvariantCulture));
            }

            return;
        }

        if (bars == null || (bars.Length != 2 && bars.Length != 4))
        {
            AddSymbolIssue(
                state,
                "ITF14_BEARER_BARS_MISSING",
                "ITF-14 requires two horizontal bearer bars or a four-sided bearer box.",
                "Symbol.BearerBars",
                -1,
                "2 or 4 bearer rectangles",
                bars == null
                    ? "null"
                    : bars.Length.ToString(
                        global::System.Globalization.CultureInfo.InvariantCulture));
            return;
        }

        int index = 0;
        while (index < bars.Length)
        {
            BarcodeRectangle? rectangle = bars[index];
            if (rectangle == null || !IsFiniteRectangle(rectangle) ||
                rectangle.X < 0.0 || rectangle.Y < 0.0 ||
                rectangle.Width <= 0.0 || rectangle.Height <= 0.0)
            {
                AddSymbolIssue(
                    state,
                    "ITF14_BEARER_BAR_INVALID",
                    "An ITF-14 bearer rectangle has invalid bounds.",
                    "Symbol.BearerBars",
                    index,
                    "a finite positive rectangle",
                    rectangle == null ? "null" : "invalid bounds");
            }

            index = index + 1;
        }

        if (bars[0] != null && bars[1] != null &&
            (bars[0].X != 0.0 || bars[0].Y != 0.0 ||
             bars[1].X != 0.0 || bars[1].Y <= bars[0].Y ||
             bars[0].Width != bars[1].Width ||
             bars[0].Height != bars[1].Height))
        {
            AddSymbolIssue(
                state,
                "ITF14_HORIZONTAL_BEARER_BARS_INVALID",
                "ITF-14 horizontal bearer bars must cover matching top and bottom bounds.",
                "Symbol.BearerBars",
                -1,
                "matching full-width top and bottom bars",
                "inconsistent horizontal bars");
        }

        if (bars.Length == 4 && bars[2] != null && bars[3] != null &&
            (bars[2].X != 0.0 || bars[2].Y != 0.0 ||
             bars[3].X <= bars[2].X || bars[3].Y != 0.0 ||
             bars[2].Width != bars[3].Width ||
             bars[2].Height != bars[3].Height))
        {
            AddSymbolIssue(
                state,
                "ITF14_VERTICAL_BEARER_BARS_INVALID",
                "An ITF-14 bearer box requires matching left and right side bars.",
                "Symbol.BearerBars",
                -1,
                "matching full-height left and right bars",
                "inconsistent vertical bars");
        }
    }

    static bool IsFiniteRectangle(BarcodeRectangle rectangle)
    {
        return !double.IsNaN(rectangle.X) && !double.IsInfinity(rectangle.X) &&
            !double.IsNaN(rectangle.Y) && !double.IsInfinity(rectangle.Y) &&
            !double.IsNaN(rectangle.Width) &&
            !double.IsInfinity(rectangle.Width) &&
            !double.IsNaN(rectangle.Height) &&
            !double.IsInfinity(rectangle.Height);
    }

    static void DispatchSymbolValidation(
        LogicalSymbolValidationState state,
        BarcodeSymbol symbol,
        BarcodeFormat format)
    {
        switch (format)
        {
            case BarcodeFormat.Code128:
            case BarcodeFormat.Gs1128:
                ValidateCode128Symbol(state, symbol, format);
                break;
            case BarcodeFormat.Ean8:
            case BarcodeFormat.Ean13:
            case BarcodeFormat.UpcA:
            case BarcodeFormat.UpcE:
            case BarcodeFormat.EanUpcAddon2:
            case BarcodeFormat.EanUpcAddon5:
                ValidateEanUpcSymbol(state, symbol, format);
                break;
            case BarcodeFormat.Code39:
                ValidateCode39Symbol(state, symbol);
                break;
            case BarcodeFormat.Itf:
            case BarcodeFormat.Itf14:
                ValidateItfSymbol(state, symbol, format);
                break;
            case BarcodeFormat.Codabar:
                ValidateCodabarSymbol(state, symbol);
                break;
            case BarcodeFormat.DataMatrix:
            case BarcodeFormat.Gs1DataMatrix:
                ValidateDataMatrixSymbol(state, symbol);
                break;
            case BarcodeFormat.Pdf417:
            case BarcodeFormat.CompactPdf417:
            case BarcodeFormat.MacroPdf417:
                ValidatePdf417Symbol(state, symbol, format);
                break;
        }
    }

    static void ValidateCode128Symbol(
        LogicalSymbolValidationState state,
        BarcodeSymbol symbol,
        BarcodeFormat format)
    {
        int[]? codewords = symbol.Codewords;
        if (codewords == null || codewords.Length < 4)
        {
            AddSymbolIssue(
                state,
                "CODE128_CODEWORDS_MISSING",
                "Code 128 requires start, data, check, and stop codewords.",
                "Symbol.Codewords",
                -1,
                "at least 4 codewords",
                codewords == null ? "null" : codewords.Length.ToString());
            ValidateLinearSymbolDecode(state, symbol, format);
            return;
        }

        bool valuesValid = codewords[0] >= 103 && codewords[0] <= 105 &&
            codewords[codewords.Length - 1] == Code128Patterns.StopCodeword;
        int index = 1;
        while (index < codewords.Length - 1)
        {
            if (codewords[index] < 0 || codewords[index] > 102)
            {
                valuesValid = false;
            }

            index = index + 1;
        }

        if (!valuesValid)
        {
            AddSymbolIssue(
                state,
                "CODE128_CODEWORD_SEQUENCE_INVALID",
                "The Code 128 codeword envelope contains an invalid start, data, check, or stop value.",
                "Symbol.Codewords",
                -1,
                "Start A/B/C, data/check 0 through 102, and Stop",
                "invalid codeword sequence");
            ValidateLinearSymbolDecode(state, symbol, format);
            return;
        }

        if (codewords.Length - 3 >
            Code128Encoder.MaximumPlannedCodewordCount)
        {
            AddSymbolIssue(
                state,
                "CODE128_CAPACITY_EXCEEDED",
                "The Code 128 codeword sequence exceeds symbol capacity.",
                "Symbol.Codewords",
                -1,
                $"at most {Code128Encoder.MaximumPlannedCodewordCount} planned codewords",
                (codewords.Length - 3).ToString());
            return;
        }

        int checkIndex = codewords.Length - 2;
        int expectedCheck = Code128Modulo103.CalculateCheckCodewordForPrefix(
            codewords,
            checkIndex);
        if (codewords[checkIndex] != expectedCheck ||
            !symbol.HasChecksum || symbol.ChecksumValue != expectedCheck)
        {
            AddSymbolIssue(
                state,
                "CODE128_CHECKSUM_INVALID",
                "The Code 128 modulo-103 check metadata or codeword is invalid.",
                "Symbol.ChecksumValue",
                checkIndex,
                expectedCheck.ToString(),
                symbol.HasChecksum
                    ? symbol.ChecksumValue.ToString()
                    : "missing");
        }

        if (format == BarcodeFormat.Gs1128 &&
            (codewords.Length < 5 || codewords[1] != Code128Patterns.Function1Codeword))
        {
            AddSymbolIssue(
                state,
                "GS1128_LEADING_FNC1_MISSING",
                "GS1-128 requires FNC1 in the first data position.",
                "Symbol.Codewords",
                1,
                Code128Patterns.Function1Codeword.ToString(),
                codewords.Length < 2 ? "missing" : codewords[1].ToString());
        }

        Code128Plan plan = new Code128Plan();
        plan.StartCodeSet = Code128Patterns.GetCodeSetForStart(codewords[0]);
        plan.Codewords = CopyIntegerRange(
            codewords,
            1,
            codewords.Length - 3);
        Code128EncodedData rebuilt = Code128Encoder.AssemblePlan(plan);
        CompareCode128Modules(state, symbol, rebuilt.Modules);
        ValidateLinearSymbolDecode(state, symbol, format);
    }

    static void CompareCode128Modules(
        LogicalSymbolValidationState state,
        BarcodeSymbol symbol,
        byte[] expected)
    {
        if (symbol.Modules == null ||
            symbol.WidthModules != expected.Length ||
            !ModuleArraysEqual(symbol.Modules, expected))
        {
            int offset = FindFirstModuleDifference(symbol.Modules, expected);
            string code = offset >= 0 && offset < 11
                ? "CODE128_START_PATTERN_INVALID"
                : offset >= expected.Length - Code128Patterns.StopModuleCount
                    ? "CODE128_STOP_PATTERN_INVALID"
                    : "CODE128_CODEWORD_PATTERN_INVALID";
            AddSymbolIssue(
                state,
                code,
                "Code 128 logical modules do not match the encoded codeword patterns.",
                "Symbol.Modules",
                offset,
                expected.Length.ToString(),
                symbol.WidthModules.ToString());
        }
    }

    static void ValidateEanUpcSymbol(
        LogicalSymbolValidationState state,
        BarcodeSymbol symbol,
        BarcodeFormat format)
    {
        BarcodePayload? payload = symbol.Payload;
        string? text = payload == null ? null : payload.Text;
        EanUpcEncodedData? encoded = null;
        if (text == null || !IsAsciiDigitText(text))
        {
            AddSymbolIssue(
                state,
                "EAN_UPC_PAYLOAD_INVALID",
                "EAN/UPC logical metadata requires its complete decimal text.",
                "Symbol.Payload.Text",
                -1,
                "complete decimal symbol text",
                text == null ? "null" : text);
        }
        else if (format == BarcodeFormat.EanUpcAddon2 ||
            format == BarcodeFormat.EanUpcAddon5)
        {
            int length = format == BarcodeFormat.EanUpcAddon2 ? 2 : 5;
            if (text.Length == length)
            {
                encoded = EanUpcAddon.Encode(text);
            }
            else
            {
                AddSymbolIssue(
                    state,
                    "EAN_UPC_ADDON_LENGTH_INVALID",
                    "The add-on logical payload length is invalid.",
                    "Symbol.Payload.Text",
                    -1,
                    length.ToString(),
                    text.Length.ToString());
            }
        }
        else
        {
            int length = GetEanCompleteLength(format);
            if (text.Length != length || !Gs1Modulo10.VerifyCompleteDigits(text))
            {
                AddSymbolIssue(
                    state,
                    "EAN_UPC_CHECK_OR_LENGTH_INVALID",
                    "The logical EAN/UPC text has an invalid length or check digit.",
                    "Symbol.Payload.Text",
                    -1,
                    $"{length} digits with a valid check digit",
                    text);
            }
            else if (format == BarcodeFormat.UpcE)
            {
                int numberSystem = text[0] - '0';
                if (numberSystem == 0 || numberSystem == 1)
                {
                    encoded = EanUpcEncoder.EncodeUpcE(
                        text,
                        numberSystem,
                        BarcodeChecksumMode.Require);
                }
                else
                {
                    AddSymbolIssue(
                        state,
                        "UPCE_NUMBER_SYSTEM_INVALID",
                        "UPC-E number system must be zero or one.",
                        "Symbol.Payload.Text",
                        0,
                        "0 or 1",
                        numberSystem.ToString());
                }
            }
            else
            {
                encoded = EanUpcEncoder.Encode(
                    text,
                    format,
                    BarcodeChecksumMode.Require);
            }
        }

        if (encoded != null)
        {
            if (!symbol.HasChecksum ||
                symbol.ChecksumValue != encoded.ChecksumValue)
            {
                AddSymbolIssue(
                    state,
                    "EAN_UPC_CHECKSUM_METADATA_INVALID",
                    "EAN/UPC checksum metadata does not match the encoded symbol.",
                    "Symbol.ChecksumValue",
                    -1,
                    encoded.ChecksumValue.ToString(),
                    symbol.HasChecksum
                        ? symbol.ChecksumValue.ToString()
                        : "missing");
            }

            if (symbol.Modules == null ||
                symbol.Modules.Length < encoded.Modules.Length ||
                !ModulePrefixEqual(symbol.Modules, encoded.Modules))
            {
                AddSymbolIssue(
                    state,
                    "EAN_UPC_GUARD_OR_PATTERN_INVALID",
                    "EAN/UPC digit parity, guard, or add-on patterns are invalid.",
                    "Symbol.Modules",
                    FindFirstModuleDifference(symbol.Modules, encoded.Modules),
                    "standard EAN/UPC modules",
                    "mismatched modules");
            }
        }

        ValidateLinearSymbolDecode(state, symbol, format);
    }

    static int GetEanCompleteLength(BarcodeFormat format)
    {
        if (format == BarcodeFormat.Ean8)
        {
            return EanUpcEncoder.Ean8CompleteLength;
        }

        if (format == BarcodeFormat.Ean13)
        {
            return EanUpcEncoder.Ean13CompleteLength;
        }

        if (format == BarcodeFormat.UpcA)
        {
            return EanUpcEncoder.UpcACompleteLength;
        }

        return EanUpcEncoder.UpcECompleteLength;
    }

    static void ValidateCode39Symbol(
        LogicalSymbolValidationState state,
        BarcodeSymbol symbol)
    {
        int[]? values = symbol.Codewords;
        if (values == null || values.Length == 0)
        {
            AddSymbolIssue(
                state,
                "CODE39_CODEWORDS_MISSING",
                "Code 39 data values are required for logical validation.",
                "Symbol.Codewords",
                -1,
                "one or more values",
                values == null ? "null" : "empty");
        }
        else
        {
            bool valid = true;
            int index = 0;
            while (index < values.Length)
            {
                if (values[index] < 0 || values[index] >= 43)
                {
                    valid = false;
                }

                index = index + 1;
            }

            if (!valid)
            {
                AddSymbolIssue(
                    state,
                    "CODE39_CODEWORD_INVALID",
                    "A Code 39 value is outside its 43-character alphabet.",
                    "Symbol.Codewords",
                    -1,
                    "values from 0 through 42",
                    "invalid value");
            }
            else if (symbol.HasChecksum)
            {
                if (values.Length < 2)
                {
                    AddSymbolIssue(
                        state,
                        "CODE39_CHECKSUM_MISSING",
                        "Code 39 checksum metadata requires data and a check value.",
                        "Symbol.Codewords",
                        -1,
                        "data and check values",
                        values.Length.ToString());
                }
                else
                {
                    int expected = 0;
                    index = 0;
                    while (index < values.Length - 1)
                    {
                        expected = (expected + values[index]) % 43;
                        index = index + 1;
                    }

                    if (values[values.Length - 1] != expected ||
                        symbol.ChecksumValue != expected)
                    {
                        AddSymbolIssue(
                            state,
                            "CODE39_CHECKSUM_INVALID",
                            "The Code 39 modulo-43 check value is invalid.",
                            "Symbol.ChecksumValue",
                            values.Length - 1,
                            expected.ToString(),
                            symbol.ChecksumValue.ToString());
                    }
                }
            }
        }

        ValidateLinearSymbolDecode(state, symbol, BarcodeFormat.Code39);
    }

    static void ValidateItfSymbol(
        LogicalSymbolValidationState state,
        BarcodeSymbol symbol,
        BarcodeFormat format)
    {
        int[]? digits = symbol.Codewords;
        bool valuesValid = digits != null && digits.Length > 0 &&
            digits.Length % 2 == 0;
        if (valuesValid && digits != null)
        {
            int index = 0;
            while (index < digits.Length)
            {
                if (digits[index] < 0 || digits[index] > 9)
                {
                    valuesValid = false;
                }

                index = index + 1;
            }
        }

        if (!valuesValid || digits == null)
        {
            AddSymbolIssue(
                state,
                "ITF_CODEWORDS_INVALID",
                "ITF requires a positive even count of decimal digit values.",
                "Symbol.Codewords",
                -1,
                "an even decimal digit sequence",
                digits == null ? "null" : digits.Length.ToString());
        }
        else if (format == BarcodeFormat.Itf14 && digits.Length != 14)
        {
            AddSymbolIssue(
                state,
                "ITF14_DIGIT_COUNT_INVALID",
                "ITF-14 requires exactly fourteen encoded digits.",
                "Symbol.Codewords",
                -1,
                "14",
                digits.Length.ToString());
        }

        bool checkDigitValid = false;
        if (valuesValid && digits != null)
        {
            int expected = CalculateDigitArrayCheck(digits, digits.Length - 1);
            int observed = digits[digits.Length - 1];
            checkDigitValid = observed == expected;
            if (symbol.HasChecksum &&
                (observed != expected || symbol.ChecksumValue != expected))
            {
                AddSymbolIssue(
                    state,
                    "ITF_CHECKSUM_INVALID",
                    "The ITF GS1 modulo-10 check value is invalid.",
                    "Symbol.ChecksumValue",
                    digits.Length - 1,
                    expected.ToString(),
                    symbol.ChecksumValue.ToString());
            }
        }

        if (format == BarcodeFormat.Itf14 && !symbol.HasChecksum)
        {
            AddSymbolIssue(
                state,
                "ITF14_CHECKSUM_MISSING",
                "ITF-14 requires its mandatory GS1 check digit.",
                "Symbol.HasChecksum",
                -1,
                "true",
                "false");
        }

        if (format != BarcodeFormat.Itf14 ||
            (valuesValid && digits != null && digits.Length == 14 &&
             checkDigitValid))
        {
            ValidateLinearSymbolDecode(state, symbol, format);
        }
    }

    static int CalculateDigitArrayCheck(int[] digits, int count)
    {
        int sum = 0;
        bool weightThree = true;
        int index = count - 1;
        while (index >= 0)
        {
            sum = sum + (digits[index] * (weightThree ? 3 : 1));
            weightThree = !weightThree;
            index = index - 1;
        }

        return (10 - (sum % 10)) % 10;
    }

    static void ValidateCodabarSymbol(
        LogicalSymbolValidationState state,
        BarcodeSymbol symbol)
    {
        int[]? values = symbol.Codewords;
        bool valid = values != null && values.Length >= 3;
        if (valid && values != null)
        {
            valid = values[0] >= 16 && values[0] <= 19 &&
                values[values.Length - 1] >= 16 &&
                values[values.Length - 1] <= 19;
            int index = 1;
            while (index < values.Length - 1)
            {
                if (values[index] < 0 || values[index] > 15)
                {
                    valid = false;
                }

                index = index + 1;
            }
        }

        if (!valid || values == null)
        {
            AddSymbolIssue(
                state,
                "CODABAR_CODEWORDS_INVALID",
                "Codabar requires valid terminals and data values.",
                "Symbol.Codewords",
                -1,
                "terminal, data, and terminal values",
                values == null ? "null" : "invalid values");
        }
        else if (symbol.HasChecksum)
        {
            if (values.Length < 4)
            {
                AddSymbolIssue(
                    state,
                    "CODABAR_CHECKSUM_MISSING",
                    "Codabar checksum metadata requires a carrier check value.",
                    "Symbol.Codewords",
                    -1,
                    "start, data, check, and stop values",
                    values.Length.ToString());
            }
            else
            {
                int checkIndex = values.Length - 2;
                int sum = 0;
                int index = 0;
                while (index < values.Length)
                {
                    if (index != checkIndex)
                    {
                        sum = (sum + values[index]) % 16;
                    }

                    index = index + 1;
                }

                int expected = (16 - sum) % 16;
                if (values[checkIndex] != expected ||
                    symbol.ChecksumValue != expected)
                {
                    AddSymbolIssue(
                        state,
                        "CODABAR_CHECKSUM_INVALID",
                        "The Codabar modulo-16 check value is invalid.",
                        "Symbol.ChecksumValue",
                        checkIndex,
                        expected.ToString(),
                        symbol.ChecksumValue.ToString());
                }
            }
        }

        ValidateLinearSymbolDecode(state, symbol, BarcodeFormat.Codabar);
    }

    static void ValidateLinearSymbolDecode(
        LogicalSymbolValidationState state,
        BarcodeSymbol symbol,
        BarcodeFormat format)
    {
        BarcodeDecodeOptions options = new BarcodeDecodeOptions();
        options.PureSymbol = true;
        options.AllowedFormats = new BarcodeFormat[] { format };
        options.Polarity = BarcodePolarity.DarkOnLight;
        options.MirrorMode = BarcodeMirrorMode.Normal;
        options.Effort = BarcodeEffort.Balanced;
        options.BinarizationMode = BarcodeBinarizationMode.Automatic;
        options.LocalizationMode = BarcodeLocalizationMode.Direct;
        options.ChecksumMode = symbol.HasChecksum
            ? BarcodeChecksumMode.Require
            : BarcodeChecksumMode.None;
        BarcodeDecodeResult? decoded = PureSymbolDecoder.TryDecode(
            symbol,
            options);
        if (decoded == null || decoded.Format != format)
        {
            AddSymbolIssue(
                state,
                ResolveLinearPatternIssueCode(format),
                "The logical symbol has an invalid start, stop, guard, separator, or data pattern.",
                "Symbol.Modules",
                -1,
                "a decodable standard module pattern",
                "pattern validation failed");
        }
    }

    static string ResolveLinearPatternIssueCode(BarcodeFormat format)
    {
        if (format == BarcodeFormat.Code128 || format == BarcodeFormat.Gs1128)
        {
            return "CODE128_PATTERN_INVALID";
        }

        if (format == BarcodeFormat.Code39)
        {
            return "CODE39_START_STOP_OR_PATTERN_INVALID";
        }

        if (format == BarcodeFormat.Itf || format == BarcodeFormat.Itf14)
        {
            return "ITF_START_STOP_OR_PATTERN_INVALID";
        }

        if (format == BarcodeFormat.Codabar)
        {
            return "CODABAR_START_STOP_OR_PATTERN_INVALID";
        }

        return "EAN_UPC_GUARD_OR_PATTERN_INVALID";
    }

    static void ValidateDataMatrixSymbol(
        LogicalSymbolValidationState state,
        BarcodeSymbol symbol)
    {
        DataMatrixSymbolInfo? info = DataMatrixSymbolInfoTable.FindSymbolByDimensions(
            symbol.HeightModules,
            symbol.WidthModules);
        if (info == null)
        {
            AddSymbolIssue(
                state,
                "DATAMATRIX_DIMENSIONS_UNSUPPORTED",
                "The Data Matrix dimensions are not a standard ECC 200 size.",
                "Symbol.WidthModulesHeightModules",
                -1,
                "a standard ECC 200 size",
                $"{symbol.WidthModules}x{symbol.HeightModules}");
            return;
        }

        int[]? codewords = symbol.Codewords;
        if (codewords == null || codewords.Length != info.TotalCodewordCount)
        {
            AddSymbolIssue(
                state,
                "DATAMATRIX_CODEWORD_COUNT_INVALID",
                "Data Matrix codeword count does not match the selected size.",
                "Symbol.Codewords",
                -1,
                info.TotalCodewordCount.ToString(),
                codewords == null ? "null" : codewords.Length.ToString());
            return;
        }

        int index = 0;
        bool valuesValid = true;
        while (index < codewords.Length)
        {
            if (codewords[index] < 0 || codewords[index] > 255)
            {
                valuesValid = false;
            }

            index = index + 1;
        }

        if (!valuesValid)
        {
            AddSymbolIssue(
                state,
                "DATAMATRIX_CODEWORD_VALUE_INVALID",
                "A Data Matrix codeword is outside the byte range.",
                "Symbol.Codewords",
                -1,
                "0 through 255",
                "invalid codeword value");
            return;
        }

        DataMatrixInterleavedBlock[] blocks =
            DataMatrixBlockInterleaver.DeinterleaveSymbolCodewords(
                codewords,
                info);
        index = 0;
        while (index < blocks.Length)
        {
            DataMatrixInterleavedBlock block = blocks[index];
            if (!DataMatrixReedSolomon.VerifyCodewords(
                block.Codewords,
                block.ErrorCodewordCount))
            {
                AddSymbolIssue(
                    state,
                    "DATAMATRIX_ECC_INVALID",
                    "A Data Matrix interleaved block fails ECC 200 Reed-Solomon verification.",
                    "Symbol.Codewords",
                    index,
                    "zero Reed-Solomon syndromes",
                    "nonzero syndromes");
            }

            index = index + 1;
        }

        ModuleMatrix mapping = DataMatrixPlacement.PlaceCodewords(
            codewords,
            info);
        ModuleMatrix expected = DataMatrixEncoder.CreateSymbolMatrix(
            mapping,
            info);
        int difference = FindMatrixDifference(symbol, expected);
        if (difference >= 0)
        {
            int x = difference % symbol.WidthModules;
            int y = difference / symbol.WidthModules;
            bool finder = IsDataMatrixBorderModule(info, x, y);
            AddSymbolIssue(
                state,
                finder
                    ? "DATAMATRIX_FINDER_PATTERN_INVALID"
                    : "DATAMATRIX_MODULE_PLACEMENT_INVALID",
                finder
                    ? "A Data Matrix finder or timing border is invalid."
                    : "A Data Matrix placed data or fixed module is invalid.",
                "Symbol.Modules",
                difference,
                expected.GetModule(x, y).ToString(),
                GetSymbolModule(symbol, difference).ToString());
        }

        if (symbol.HasChecksum)
        {
            AddSymbolIssue(
                state,
                "DATAMATRIX_CHECKSUM_METADATA_INVALID",
                "Data Matrix uses ECC metadata rather than a scalar checksum field.",
                "Symbol.HasChecksum",
                -1,
                "false",
                "true");
        }
    }

    static bool IsDataMatrixBorderModule(
        DataMatrixSymbolInfo info,
        int x,
        int y)
    {
        int regionWidth = info.DataRegionColumns + 2;
        int regionHeight = info.DataRegionRows + 2;
        int localX = x % regionWidth;
        int localY = y % regionHeight;
        return localX == 0 || localX == regionWidth - 1 ||
            localY == 0 || localY == regionHeight - 1;
    }

    static void ValidatePdf417Symbol(
        LogicalSymbolValidationState state,
        BarcodeSymbol symbol,
        BarcodeFormat format)
    {
        if (symbol.HeightModules % Pdf417Layout.DefaultRowHeightModules != 0)
        {
            AddSymbolIssue(
                state,
                "PDF417_ROW_HEIGHT_INVALID",
                "PDF417 logical height must contain integral three-module rows.",
                "Symbol.HeightModules",
                -1,
                "a multiple of 3",
                symbol.HeightModules.ToString());
            return;
        }

        int rows = symbol.HeightModules /
            Pdf417Layout.DefaultRowHeightModules;
        Pdf417Variant variant;
        int overhead;
        bool standardWidth =
            (symbol.WidthModules - Pdf417SymbolPlanner.StandardRowOverheadModules) >= 0 &&
            (symbol.WidthModules - Pdf417SymbolPlanner.StandardRowOverheadModules) %
                Pdf417SymbolPlanner.CodewordWidthModules == 0;
        bool compactWidth =
            (symbol.WidthModules - Pdf417SymbolPlanner.CompactRowOverheadModules) >= 0 &&
            (symbol.WidthModules - Pdf417SymbolPlanner.CompactRowOverheadModules) %
                Pdf417SymbolPlanner.CodewordWidthModules == 0;
        bool chooseCompact = format == BarcodeFormat.CompactPdf417;
        if (format == BarcodeFormat.MacroPdf417 && compactWidth &&
            symbol.Codewords != null)
        {
            int compactColumns =
                (symbol.WidthModules - Pdf417SymbolPlanner.CompactRowOverheadModules) /
                Pdf417SymbolPlanner.CodewordWidthModules;
            chooseCompact = symbol.Codewords.Length == rows * compactColumns;
        }

        if (standardWidth && !chooseCompact)
        {
            variant = Pdf417Variant.Standard;
            overhead = Pdf417SymbolPlanner.StandardRowOverheadModules;
        }
        else if (compactWidth)
        {
            variant = Pdf417Variant.Compact;
            overhead = Pdf417SymbolPlanner.CompactRowOverheadModules;
        }
        else
        {
            AddSymbolIssue(
                state,
                "PDF417_ROW_WIDTH_INVALID",
                "PDF417 row width does not match standard or compact overhead.",
                "Symbol.WidthModules",
                -1,
                "17 modules per data column plus row overhead",
                symbol.WidthModules.ToString());
            return;
        }

        int columns = (symbol.WidthModules - overhead) /
            Pdf417SymbolPlanner.CodewordWidthModules;
        if (rows < Pdf417SymbolPlanner.MinimumRows ||
            rows > Pdf417SymbolPlanner.MaximumRows ||
            columns < Pdf417SymbolPlanner.MinimumColumns ||
            columns > Pdf417SymbolPlanner.MaximumColumns)
        {
            AddSymbolIssue(
                state,
                "PDF417_DIMENSIONS_INVALID",
                "PDF417 row or column count is outside the standard range.",
                "Symbol.WidthModulesHeightModules",
                -1,
                "1-30 columns and 3-90 rows",
                $"{columns} columns and {rows} rows");
            return;
        }

        if ((format == BarcodeFormat.Pdf417 && variant != Pdf417Variant.Standard) ||
            (format == BarcodeFormat.CompactPdf417 && variant != Pdf417Variant.Compact))
        {
            AddSymbolIssue(
                state,
                "PDF417_VARIANT_FORMAT_CONFLICT",
                "PDF417 row layout does not match the selected format variant.",
                "Symbol.Format",
                -1,
                format.ToString(),
                variant.ToString());
        }

        int total = rows * columns;
        if (total > Pdf417SymbolPlanner.MaximumTotalCodewordCount)
        {
            AddSymbolIssue(
                state,
                "PDF417_SYMBOL_CAPACITY_EXCEEDED",
                "The PDF417 row and column grid exceeds symbol capacity.",
                "Symbol.WidthModulesHeightModules",
                -1,
                $"at most {Pdf417SymbolPlanner.MaximumTotalCodewordCount} codewords",
                total.ToString());
            return;
        }

        int[]? codewords = symbol.Codewords;
        if (codewords == null || codewords.Length != total)
        {
            AddSymbolIssue(
                state,
                "PDF417_CODEWORD_COUNT_INVALID",
                "PDF417 codeword count does not match its row and column grid.",
                "Symbol.Codewords",
                -1,
                total.ToString(),
                codewords == null ? "null" : codewords.Length.ToString());
            return;
        }

        int index = 0;
        bool valuesValid = true;
        while (index < codewords.Length)
        {
            if (codewords[index] < 0 ||
                codewords[index] >= Pdf417CodewordTable.CodewordCount)
            {
                valuesValid = false;
            }

            index = index + 1;
        }

        if (!valuesValid)
        {
            AddSymbolIssue(
                state,
                "PDF417_CODEWORD_VALUE_INVALID",
                "A PDF417 codeword is outside the modulo-929 range.",
                "Symbol.Codewords",
                -1,
                "0 through 928",
                "invalid codeword value");
            return;
        }

        int descriptor = codewords[0];
        int errorCount = total - descriptor;
        int level = ResolvePdf417ErrorLevel(errorCount);
        if (descriptor <= 0 || descriptor > total || level < 0)
        {
            AddSymbolIssue(
                state,
                "PDF417_LENGTH_OR_ECC_COUNT_INVALID",
                "PDF417 length descriptor and error-correction count are inconsistent.",
                "Symbol.Codewords",
                0,
                "a valid length descriptor and power-of-two ECC count",
                $"descriptor {descriptor}, ECC {errorCount}");
            return;
        }

        if (!Pdf417ReedSolomon.VerifyCodewords(codewords, level))
        {
            AddSymbolIssue(
                state,
                "PDF417_ECC_INVALID",
                "PDF417 codewords fail modulo-929 Reed-Solomon verification.",
                "Symbol.Codewords",
                -1,
                "zero error-correction syndromes",
                "nonzero syndromes");
        }

        Pdf417SymbolPlan plan = new Pdf417SymbolPlan();
        plan.Rows = rows;
        plan.Columns = columns;
        plan.Variant = variant;
        plan.ErrorCorrectionLevel = level;
        plan.RowWidthModules = symbol.WidthModules;
        byte[] expected = Pdf417Layout.CreateModules(plan, codewords);
        int difference = FindFirstModuleDifference(symbol.Modules, expected);
        if (difference >= 0)
        {
            int x = difference % symbol.WidthModules;
            string code = ResolvePdf417PatternCode(x, columns, variant);
            AddSymbolIssue(
                state,
                code,
                "A PDF417 start/stop, row-indicator, or codeword pattern is invalid.",
                "Symbol.Modules",
                difference,
                expected[difference].ToString(),
                GetSymbolModule(symbol, difference).ToString());
        }

        if (symbol.HasChecksum)
        {
            AddSymbolIssue(
                state,
                "PDF417_CHECKSUM_METADATA_INVALID",
                "PDF417 uses Reed-Solomon ECC rather than a scalar checksum field.",
                "Symbol.HasChecksum",
                -1,
                "false",
                "true");
        }

        if (format == BarcodeFormat.MacroPdf417 &&
            (symbol.Payload == null || symbol.Payload.StructuredAppend == null))
        {
            AddSymbolIssue(
                state,
                "MACRO_PDF417_METADATA_MISSING",
                "Macro PDF417 logical metadata is missing.",
                "Symbol.Payload.StructuredAppend",
                -1,
                "Macro PDF417 segment metadata",
                "null");
        }
    }

    static int ResolvePdf417ErrorLevel(int errorCount)
    {
        int level = 0;
        int expected = 2;
        while (level <= Pdf417ReedSolomon.MaximumErrorCorrectionLevel)
        {
            if (errorCount == expected)
            {
                return level;
            }

            expected = expected * 2;
            level = level + 1;
        }

        return -1;
    }

    static string ResolvePdf417PatternCode(
        int x,
        int columns,
        Pdf417Variant variant)
    {
        int dataStart = Pdf417CodewordTable.StartPatternModuleCount +
            Pdf417CodewordTable.ModulesPerCodeword;
        int dataEnd = dataStart +
            (columns * Pdf417CodewordTable.ModulesPerCodeword);
        if (x < Pdf417CodewordTable.StartPatternModuleCount ||
            (variant == Pdf417Variant.Standard &&
             x >= dataEnd + Pdf417CodewordTable.ModulesPerCodeword))
        {
            return "PDF417_START_STOP_PATTERN_INVALID";
        }

        if (x < dataStart ||
            (variant == Pdf417Variant.Standard && x >= dataEnd))
        {
            return "PDF417_ROW_INDICATOR_INVALID";
        }

        return "PDF417_CODEWORD_PATTERN_INVALID";
    }

    static int FindMatrixDifference(
        BarcodeSymbol symbol,
        ModuleMatrix expected)
    {
        if (symbol.Modules == null ||
            expected.Width != symbol.WidthModules ||
            expected.Height != symbol.HeightModules)
        {
            return 0;
        }

        int y = 0;
        while (y < expected.Height)
        {
            int x = 0;
            while (x < expected.Width)
            {
                int offset = (y * expected.Width) + x;
                bool actual = symbol.Modules[offset] != 0;
                if (actual != expected.GetModule(x, y))
                {
                    return offset;
                }

                x = x + 1;
            }

            y = y + 1;
        }

        return -1;
    }

    static bool ModuleArraysEqual(byte[] actual, byte[] expected)
    {
        return FindFirstModuleDifference(actual, expected) < 0;
    }

    static bool ModulePrefixEqual(byte[] actual, byte[] expected)
    {
        if (actual.Length < expected.Length)
        {
            return false;
        }

        int index = 0;
        while (index < expected.Length)
        {
            if ((actual[index] != 0) != (expected[index] != 0))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static int FindFirstModuleDifference(byte[]? actual, byte[] expected)
    {
        if (actual == null)
        {
            return 0;
        }

        int count = actual.Length < expected.Length
            ? actual.Length
            : expected.Length;
        int index = 0;
        while (index < count)
        {
            if ((actual[index] != 0) != (expected[index] != 0))
            {
                return index;
            }

            index = index + 1;
        }

        return actual.Length == expected.Length ? -1 : count;
    }

    static bool GetSymbolModule(BarcodeSymbol symbol, int offset)
    {
        return symbol.Modules != null && offset >= 0 &&
            offset < symbol.Modules.Length && symbol.Modules[offset] != 0;
    }

    static int[] CopyIntegerRange(int[] source, int start, int count)
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

    static bool IsAsciiDigitText(string text)
    {
        if (text.Length == 0)
        {
            return false;
        }

        int index = 0;
        while (index < text.Length)
        {
            if (text[index] < '0' || text[index] > '9')
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static void AddSymbolIssue(
        LogicalSymbolValidationState state,
        string code,
        string message,
        string field,
        int offset,
        string? expected,
        string? observed)
    {
        string standard = state.Profile == null
            ? state.Standard
            : state.Profile;
        state.Issues.Add(ValidationIssueFactory.Create(
            ValidationModePolicy.ResolveSeverity(state.Mode, code),
            code,
            message,
            standard,
            field,
            offset,
            expected,
            observed));
    }

    static BarcodeValidationReport CreateSymbolReport(
        LogicalSymbolValidationState state,
        BarcodeValidationMode mode)
    {
        BarcodeValidationIssue[] issues =
            new BarcodeValidationIssue[state.Issues.Count];
        int index = 0;
        while (index < state.Issues.Count)
        {
            issues[index] = ValidationIssueFactory.CopyWithOffset(
                state.Issues[index],
                state.Issues[index].Offset);
            index = index + 1;
        }

        BarcodeValidationReport report = new BarcodeValidationReport();
        report.IsConformant = ValidationModePolicy.IsConformant(issues);
        report.Mode = mode;
        report.Standard = state.Standard;
        report.Profile = state.Profile;
        report.Issues = issues;
        report.Metrics = new BarcodeQualityMetric[0];
        report.Diagnostics = new BarcodeDiagnostic[0];
        return report;
    }
}
