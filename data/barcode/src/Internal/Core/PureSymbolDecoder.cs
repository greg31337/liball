namespace barcode;

internal static class PureSymbolDecoder
{
    internal const int LinearQuietZoneModules = 16;
    internal const int Pdf417QuietZoneModules = 4;
    internal const int Pdf417SampleScale = 2;

    internal static BarcodeDecodeResult? TryDecode(
        BarcodeSymbol symbol,
        BarcodeDecodeOptions? options)
    {
        if (symbol == null)
        {
            throw new BarcodeValidationException("A pure barcode symbol is required.");
        }

        FormatDispatcher.EnsureSupportedFormat(symbol.Format);
        BarcodeDecodeOptions settings = options == null
            ? CreateDefaultOptions()
            : options;
        if (!IsAllowedFormat(symbol.Format, settings.AllowedFormats))
        {
            return null;
        }

        ModuleMatrix modules = CreateModuleMatrix(symbol);
        BarcodeCodecFamily family = FormatDispatcher.ResolveCodecFamily(symbol.Format);
        BarcodeDecodeResult? decoded;
        if (family == BarcodeCodecFamily.DataMatrix)
        {
            decoded = DecodeDataMatrix(modules, settings);
        }
        else if (family == BarcodeCodecFamily.Pdf417)
        {
            decoded = DecodePdf417(modules, settings);
        }
        else
        {
            decoded = DecodeLinear(modules, symbol.Format, settings);
        }

        if (decoded == null ||
            !IsAllowedFormat(decoded.Format, settings.AllowedFormats) ||
            !IsAllowedLength(decoded, settings.AllowedLengths))
        {
            return null;
        }

        decoded.Location = CreateSymbolLocation(symbol.WidthModules, symbol.HeightModules);
        return decoded;
    }

    internal static ModuleMatrix CreateModuleMatrix(BarcodeSymbol symbol)
    {
        if (symbol.Format == BarcodeFormat.Itf14 && symbol.Modules == null)
        {
            return CreateItf14ModuleMatrix(symbol);
        }

        if (symbol.WidthModules <= 0 || symbol.HeightModules <= 0 ||
            symbol.Modules == null)
        {
            throw new BarcodeValidationException(
                "Pure symbol dimensions and modules must be present.");
        }

        int expectedLength = CheckedMath.Multiply(
            symbol.WidthModules,
            symbol.HeightModules);
        if (symbol.Modules.Length != expectedLength)
        {
            throw new BarcodeValidationException(
                "Pure symbol module length does not match its dimensions.");
        }

        ModuleMatrix matrix = new ModuleMatrix(
            symbol.WidthModules,
            symbol.HeightModules);
        int y = 0;
        while (y < symbol.HeightModules)
        {
            int x = 0;
            while (x < symbol.WidthModules)
            {
                matrix.SetModule(
                    x,
                    y,
                    symbol.Modules[(y * symbol.WidthModules) + x] != 0);
                x = x + 1;
            }

            y = y + 1;
        }

        return matrix;
    }

    static ModuleMatrix CreateItf14ModuleMatrix(BarcodeSymbol symbol)
    {
        if (symbol.Codewords == null ||
            symbol.Codewords.Length != Itf14Profile.CompleteDigitCount)
        {
            throw new BarcodeValidationException(
                "Pure ITF-14 symbols require fourteen digit codewords.");
        }

        char[] characters = new char[symbol.Codewords.Length];
        int index = 0;
        while (index < symbol.Codewords.Length)
        {
            int digit = symbol.Codewords[index];
            if (digit < 0 || digit > 9)
            {
                throw new BarcodeValidationException(
                    "Pure ITF-14 codewords must contain decimal digits.");
            }

            characters[index] = (char)('0' + digit);
            index = index + 1;
        }

        string text = new string(characters);
        if (!Gs1Modulo10.VerifyCompleteDigits(text))
        {
            throw new BarcodeValidationException(
                "Pure ITF-14 codewords have an invalid check digit.");
        }

        BarcodePayload payload = new BarcodePayload();
        payload.Text = text;
        ItfOptions options = new ItfOptions();
        options.AllowedLengths = new int[] { Itf14Profile.CompleteDigitCount };
        options.ChecksumMode = BarcodeChecksumMode.Require;
        options.WideToNarrowRatio = ItfLayout.DefaultWideToNarrowRatio;
        BarcodeSymbol carrier = ItfLayout.CreateLayout(
            text,
            payload,
            options,
            null).Symbol;
        return CreateModuleMatrix(carrier);
    }

    static BarcodeDecodeResult? DecodeDataMatrix(
        ModuleMatrix modules,
        BarcodeDecodeOptions options)
    {
        DataMatrixPureDecodeResult? decoded = DataMatrixPureDecoder.TryDecode(
            modules,
            options);
        return decoded == null ? null : decoded.Result;
    }

    static BarcodeDecodeResult? DecodePdf417(
        ModuleMatrix modules,
        BarcodeDecodeOptions options)
    {
        BarcodePolarity[] polarities = CreatePolarityAttempts(options.Polarity);
        int polarityIndex = 0;
        while (polarityIndex < polarities.Length)
        {
            ModuleMatrix normalized = polarities[polarityIndex] ==
                BarcodePolarity.LightOnDark
                ? InvertMatrix(modules)
                : modules;
            BarcodeDecodeResult? decoded = DecodePdf417Matrix(normalized);
            if (decoded != null &&
                IsAllowedFormat(decoded.Format, options.AllowedFormats) &&
                IsAllowedLength(decoded, options.AllowedLengths))
            {
                decoded.Polarity = polarities[polarityIndex];
                return decoded;
            }

            polarityIndex = polarityIndex + 1;
        }

        return null;
    }

    static BarcodeDecodeResult? DecodePdf417Matrix(ModuleMatrix modules)
    {
        if (modules.Height < Pdf417Layout.DefaultRowHeightModules ||
            modules.Height % Pdf417Layout.DefaultRowHeightModules != 0)
        {
            return null;
        }

        int rowCount = modules.Height / Pdf417Layout.DefaultRowHeightModules;
        Pdf417ScanlineRow[] observations = new Pdf417ScanlineRow[rowCount];
        int observationCount = 0;
        int row = 0;
        while (row < rowCount)
        {
            BitRow sample = CreatePdf417SampleRow(
                modules,
                row * Pdf417Layout.DefaultRowHeightModules);
            Pdf417ScanlineRow? decoded = Pdf417ScanningDecoder.TryDecodeRow(sample);
            if (decoded != null)
            {
                observations[observationCount] = decoded;
                observationCount = observationCount + 1;
            }

            row = row + 1;
        }

        if (observationCount == 0)
        {
            return null;
        }

        Pdf417ScanlineRow[] selected = new Pdf417ScanlineRow[observationCount];
        int index = 0;
        while (index < observationCount)
        {
            selected[index] = observations[index];
            index = index + 1;
        }

        Pdf417CodewordDecodeResult result = Pdf417CodewordDecoder.DecodeRows(selected);
        return result.Result;
    }

    static BitRow CreatePdf417SampleRow(ModuleMatrix modules, int sourceRow)
    {
        int outputWidth = (modules.Width + (Pdf417QuietZoneModules * 2)) *
            Pdf417SampleScale;
        BitRow row = new BitRow(outputWidth);
        int x = 0;
        while (x < modules.Width)
        {
            if (modules.GetModule(x, sourceRow))
            {
                int repeat = 0;
                while (repeat < Pdf417SampleScale)
                {
                    int output = ((Pdf417QuietZoneModules + x) *
                        Pdf417SampleScale) + repeat;
                    row.SetBit(output, true);
                    repeat = repeat + 1;
                }
            }

            x = x + 1;
        }

        return row;
    }

    static BarcodeDecodeResult? DecodeLinear(
        ModuleMatrix modules,
        BarcodeFormat format,
        BarcodeDecodeOptions options)
    {
        BarcodePolarity[] polarities = CreatePolarityAttempts(options.Polarity);
        int polarityIndex = 0;
        while (polarityIndex < polarities.Length)
        {
            bool inverted = polarities[polarityIndex] == BarcodePolarity.LightOnDark;
            BitRow row = CreateLinearRow(modules, inverted);
            BarcodeDecodeResult? decoded = DecodeLinearRow(row, format, options);
            if (decoded != null)
            {
                decoded.Polarity = polarities[polarityIndex];
                return decoded;
            }

            polarityIndex = polarityIndex + 1;
        }

        return null;
    }

    static BarcodeDecodeResult? DecodeLinearRow(
        BitRow row,
        BarcodeFormat format,
        BarcodeDecodeOptions options)
    {
        if (format == BarcodeFormat.Gs1128)
        {
            Gs1128DecodedData? gs1 = Gs1128Codec.TryDecodeScanline(row);
            return gs1 == null ? null : gs1.Result;
        }

        if (format == BarcodeFormat.Code128)
        {
            LinearScanlineObservation? observation = LinearScanlineDecoder.TryDecode(
                row,
                BarcodeFormat.Code128,
                false,
                0);
            return observation == null ? null : observation.Result;
        }

        if (format == BarcodeFormat.Ean8 || format == BarcodeFormat.Ean13 ||
            format == BarcodeFormat.UpcA || format == BarcodeFormat.UpcE)
        {
            EanUpcDecodedData? ean = EanUpcScanlineDecoder.TryDecodeRow(row);
            return ean == null || ean.Format != format ? null : ean.Result;
        }

        if (format == BarcodeFormat.EanUpcAddon2 ||
            format == BarcodeFormat.EanUpcAddon5)
        {
            EanUpcOptions eanOptions = new EanUpcOptions();
            eanOptions.AllowedAddOnLengths = ResultCopier.CopyIntArray(
                options.AllowedLengths);
            EanUpcAddonDecodedData? addOn =
                EanUpcAddonScanlineDecoder.TryDecodeRow(row, eanOptions);
            return addOn == null || addOn.Format != format
                ? null
                : addOn.Result;
        }

        if (format == BarcodeFormat.Code39)
        {
            Code39Options code39Options = new Code39Options();
            code39Options.CharacterMode = Code39CharacterMode.FullAscii;
            code39Options.ChecksumMode = options.ChecksumMode;
            Code39ScanlineDecodedData? code39 =
                Code39ScanlineDecoder.TryDecodeRow(row, code39Options);
            return code39 == null ? null : code39.Result;
        }

        if (format == BarcodeFormat.Itf || format == BarcodeFormat.Itf14)
        {
            ItfOptions itfOptions = new ItfOptions();
            itfOptions.ChecksumMode = options.ChecksumMode;
            ItfScanlineDecodedData? itf = ItfScanlineDecoder.TryDecodeRow(
                row,
                format,
                itfOptions);
            return itf == null ? null : itf.Result;
        }

        if (format == BarcodeFormat.Codabar)
        {
            CodabarOptions codabarOptions = new CodabarOptions();
            codabarOptions.AcceptAliases = true;
            codabarOptions.ChecksumMode = options.ChecksumMode;
            CodabarScanlineDecodedData? codabar =
                CodabarScanlineDecoder.TryDecodeRow(row, codabarOptions);
            return codabar == null ? null : codabar.Result;
        }

        return null;
    }

    static BitRow CreateLinearRow(ModuleMatrix modules, bool inverted)
    {
        int sourceRow = (modules.Height - 1) / 2;
        BitRow row = new BitRow(modules.Width + (LinearQuietZoneModules * 2));
        int x = 0;
        while (x < modules.Width)
        {
            bool value = modules.GetModule(x, sourceRow);
            if (inverted)
            {
                value = !value;
            }

            if (value)
            {
                row.SetBit(LinearQuietZoneModules + x, true);
            }

            x = x + 1;
        }

        return row;
    }

    static BarcodePolarity[] CreatePolarityAttempts(BarcodePolarity polarity)
    {
        if (polarity == BarcodePolarity.Automatic)
        {
            return new BarcodePolarity[]
            {
                BarcodePolarity.DarkOnLight,
                BarcodePolarity.LightOnDark
            };
        }

        if (polarity != BarcodePolarity.DarkOnLight &&
            polarity != BarcodePolarity.LightOnDark)
        {
            throw new BarcodeValidationException("Pure symbol polarity is invalid.");
        }

        return new BarcodePolarity[] { polarity };
    }

    static ModuleMatrix InvertMatrix(ModuleMatrix source)
    {
        ModuleMatrix inverted = new ModuleMatrix(source.Width, source.Height);
        int y = 0;
        while (y < source.Height)
        {
            int x = 0;
            while (x < source.Width)
            {
                inverted.SetModule(x, y, !source.GetModule(x, y));
                x = x + 1;
            }

            y = y + 1;
        }

        return inverted;
    }

    static bool IsAllowedFormat(
        BarcodeFormat format,
        BarcodeFormat[]? allowedFormats)
    {
        if (allowedFormats == null || allowedFormats.Length == 0)
        {
            return true;
        }

        int index = 0;
        while (index < allowedFormats.Length)
        {
            if (allowedFormats[index] == BarcodeFormat.Automatic ||
                allowedFormats[index] == format)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static bool IsAllowedLength(
        BarcodeDecodeResult result,
        int[]? allowedLengths)
    {
        if (allowedLengths == null || allowedLengths.Length == 0)
        {
            return true;
        }

        int length = 0;
        if (result.Payload != null && result.Payload.RawBytes != null)
        {
            length = result.Payload.RawBytes.Length;
        }
        else if (result.Payload != null && result.Payload.Text != null)
        {
            length = result.Payload.Text.Length;
        }

        int index = 0;
        while (index < allowedLengths.Length)
        {
            if (allowedLengths[index] < 0)
            {
                throw new BarcodeValidationException(
                    "Allowed decode lengths must not be negative.");
            }

            if (allowedLengths[index] == length)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static BarcodeQuadrilateral CreateSymbolLocation(int width, int height)
    {
        return GeometryMath.CreateQuadrilateral(
            GeometryMath.CreatePoint(0.0, 0.0),
            GeometryMath.CreatePoint(width, 0.0),
            GeometryMath.CreatePoint(width, height),
            GeometryMath.CreatePoint(0.0, height));
    }

    static BarcodeDecodeOptions CreateDefaultOptions()
    {
        BarcodeDecodeOptions options = new BarcodeDecodeOptions();
        options.PureSymbol = true;
        options.Polarity = BarcodePolarity.Automatic;
        options.MirrorMode = BarcodeMirrorMode.Both;
        options.Effort = BarcodeEffort.Balanced;
        options.BinarizationMode = BarcodeBinarizationMode.Automatic;
        options.LocalizationMode = BarcodeLocalizationMode.Automatic;
        return options;
    }
}
