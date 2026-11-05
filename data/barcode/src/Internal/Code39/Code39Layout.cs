namespace barcode;

internal class Code39AppendMetadata
{
    internal bool IsContinuation;
    internal bool ReleasesBufferedData;
    internal string SegmentText = "";
}

internal class Code39LayoutResult
{
    internal BarcodeSymbol Symbol = new BarcodeSymbol();
    internal Code39EncodedData Encoded = new Code39EncodedData();
    internal string DataText = "";
    internal string TransmittedText = "";
    internal char CheckCharacter;
    internal Code39AppendMetadata? AppendMetadata;
    internal double WideToNarrowRatio;
    internal int IntercharacterGapModules;
    internal double SymbolWidthModules;
    internal double[] ElementWidths = new double[0];
    internal bool[] ElementIsDark = new bool[0];
}

internal static class Code39Layout
{
    internal const int StandardQuietZoneModules = 10;
    internal const double DefaultWideToNarrowRatio = 2.0;
    internal const int DefaultIntercharacterGapModules = 1;
    internal const double HumanReadableHeight = 1.0;
    internal const double PhysicalComparisonTolerance = 0.000001;

    internal static Code39LayoutResult CreateLayout(
        string text,
        BarcodePayload payload,
        Code39Options? code39Options,
        BarcodeEncodeOptions? encodeOptions)
    {
        if (payload == null)
        {
            throw new BarcodeEncodingException("Code 39 payload must not be null.");
        }

        Code39Options settings = code39Options == null ? new Code39Options() : code39Options;
        BarcodeEncodeOptions common = encodeOptions == null ? new BarcodeEncodeOptions() : encodeOptions;
        double ratio = ResolveRatio(settings.WideToNarrowRatio);
        int gap = ResolveGap(settings.IntercharacterGapModules);
        BarcodeChecksumMode checksumMode = code39Options == null
            ? common.ChecksumMode
            : settings.ChecksumMode;
        ValidateChecksumMode(checksumMode);

        Code39EncodedData encoded;
        string dataText;
        string transmittedText;
        char checkCharacter = '\0';
        bool hasChecksum = checksumMode != BarcodeChecksumMode.None;
        if (checksumMode == BarcodeChecksumMode.None)
        {
            encoded = Code39Encoder.Encode(text, settings.CharacterMode);
            dataText = text;
            transmittedText = text;
        }
        else if (checksumMode == BarcodeChecksumMode.Calculate)
        {
            Code39EncodedData data = Code39Encoder.Encode(text, settings.CharacterMode);
            checkCharacter = Code39Modulo43.CalculateCheckCharacter(data.EncodedText);
            encoded = AppendCarrierCharacter(data, checkCharacter, text);
            dataText = text;
            transmittedText = settings.TransmitCheckCharacter ? text + checkCharacter : text;
        }
        else
        {
            Code39SuppliedCheckData supplied = VerifySuppliedCheck(text, settings.CharacterMode);
            encoded = supplied.Encoded;
            dataText = supplied.DataText;
            checkCharacter = supplied.CheckCharacter;
            if (checksumMode == BarcodeChecksumMode.VerifyKeep ||
                (checksumMode == BarcodeChecksumMode.Require && settings.TransmitCheckCharacter))
            {
                transmittedText = dataText + checkCharacter;
            }
            else
            {
                transmittedText = dataText;
            }
        }

        Code39AppendMetadata? appendMetadata = CreateAppendMetadata(dataText, settings.EnableAppend);
        Code39ElementLayout elements = CreateElementLayout(encoded.PatternBits, ratio, gap);
        BarcodeQuietZone requiredQuietZone = CreateRequiredQuietZone();
        BarcodeQuietZone selectedQuietZone = ResolveQuietZone(common, requiredQuietZone);
        BarcodeSize size = ResolveSize(elements.WidthModules, common.Sizing, selectedQuietZone);
        BarcodeTextLayout? textLayout = CreateTextLayout(
            transmittedText,
            payload,
            common,
            size,
            selectedQuietZone,
            elements.WidthModules);

        BarcodeSymbol symbol = new BarcodeSymbol();
        symbol.Format = BarcodeFormat.Code39;
        symbol.Dimension = BarcodeDimension.Linear;
        symbol.WidthModules = RoundWidthToModules(elements.WidthModules);
        symbol.HeightModules = 1;
        symbol.Modules = CreateIntegralModuleRow(elements.Widths, elements.IsDark);
        symbol.Payload = ResultCopier.CopyPayload(payload);
        symbol.Size = size;
        symbol.RequiredQuietZone = requiredQuietZone;
        symbol.TextLayout = textLayout;
        symbol.SymbologyIdentifier = BarcodeSymbologyIdentifiers.Code39;
        symbol.Codewords = ResultCopier.CopyIntArray(encoded.Values);
        symbol.HasChecksum = hasChecksum;
        if (hasChecksum)
        {
            symbol.ChecksumValue = Code39Patterns.GetValue(checkCharacter);
        }

        Code39LayoutResult result = new Code39LayoutResult();
        result.Symbol = symbol;
        result.Encoded = encoded;
        result.DataText = dataText;
        result.TransmittedText = transmittedText;
        result.CheckCharacter = checkCharacter;
        result.AppendMetadata = appendMetadata;
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
            throw new BarcodeEncodingException("Code 39 wide-to-narrow ratio must be from 2.0 through 3.0.");
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
            throw new BarcodeEncodingException("Code 39 intercharacter gap must be positive.");
        }

        return gap;
    }

    static void ValidateChecksumMode(BarcodeChecksumMode checksumMode)
    {
        if (checksumMode != BarcodeChecksumMode.None &&
            checksumMode != BarcodeChecksumMode.Calculate &&
            checksumMode != BarcodeChecksumMode.Require &&
            checksumMode != BarcodeChecksumMode.VerifyKeep &&
            checksumMode != BarcodeChecksumMode.VerifyStrip)
        {
            throw new BarcodeEncodingException("Code 39 checksum mode is invalid.");
        }
    }

    static Code39SuppliedCheckData VerifySuppliedCheck(string text, Code39CharacterMode characterMode)
    {
        if (text == null || text.Length < 2)
        {
            throw new BarcodeEncodingException("Code 39 verification requires data and a supplied check character.");
        }

        string dataText = text.Substring(0, text.Length - 1);
        char checkCharacter = text[text.Length - 1];
        if (!Code39Patterns.IsStandardDataCharacter(checkCharacter))
        {
            throw new BarcodeEncodingException("The supplied Code 39 check character is outside the standard alphabet.");
        }

        Code39EncodedData data = Code39Encoder.Encode(dataText, characterMode);
        if (!Code39Modulo43.VerifyCheckCharacter(data.EncodedText, checkCharacter))
        {
            throw new BarcodeEncodingException("The supplied Code 39 check character is invalid.");
        }

        Code39SuppliedCheckData supplied = new Code39SuppliedCheckData();
        supplied.DataText = dataText;
        supplied.CheckCharacter = checkCharacter;
        supplied.Encoded = AppendCarrierCharacter(data, checkCharacter, text);
        return supplied;
    }

    static Code39EncodedData AppendCarrierCharacter(
        Code39EncodedData data,
        char checkCharacter,
        string sourceText)
    {
        int[] values = new int[data.Values.Length + 1];
        int valueIndex = 0;
        while (valueIndex < data.Values.Length)
        {
            values[valueIndex] = data.Values[valueIndex];
            valueIndex = valueIndex + 1;
        }

        values[values.Length - 1] = Code39Patterns.GetValue(checkCharacter);
        int[] patterns = new int[data.PatternBits.Length + 1];
        int patternIndex = 0;
        while (patternIndex < data.PatternBits.Length - 1)
        {
            patterns[patternIndex] = data.PatternBits[patternIndex];
            patternIndex = patternIndex + 1;
        }

        patterns[patterns.Length - 2] = Code39Patterns.GetCharacterPatternBits(checkCharacter);
        patterns[patterns.Length - 1] = Code39Patterns.StartStopPatternBits;

        Code39EncodedData result = new Code39EncodedData();
        result.Text = sourceText;
        result.EncodedText = data.EncodedText + checkCharacter;
        result.Values = values;
        result.PatternBits = patterns;
        result.CharacterMode = data.CharacterMode;
        return result;
    }

    static Code39AppendMetadata? CreateAppendMetadata(string dataText, bool enabled)
    {
        if (!enabled)
        {
            return null;
        }

        Code39AppendMetadata metadata = new Code39AppendMetadata();
        if (dataText.Length > 0 && dataText[0] == ' ')
        {
            metadata.IsContinuation = true;
            metadata.SegmentText = dataText.Substring(1);
        }
        else
        {
            metadata.ReleasesBufferedData = true;
            metadata.SegmentText = dataText;
        }

        return metadata;
    }

    static Code39ElementLayout CreateElementLayout(int[] patterns, double ratio, int gap)
    {
        int gaps = patterns.Length - 1;
        int elementsForCharacters = CheckedMath.Multiply(patterns.Length, Code39Patterns.ElementCount);
        int elementCount = CheckedMath.Add(elementsForCharacters, gaps);
        double[] widths = new double[elementCount];
        bool[] isDark = new bool[elementCount];
        double totalWidth = 0.0;
        int target = 0;
        int characterIndex = 0;
        while (characterIndex < patterns.Length)
        {
            int elementIndex = 0;
            while (elementIndex < Code39Patterns.ElementCount)
            {
                int bit = Code39Patterns.ElementCount - elementIndex - 1;
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
            throw new BarcodeEncodingException("Code 39 layout width exceeds the supported range.");
        }

        Code39ElementLayout layout = new Code39ElementLayout();
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
            throw new BarcodeEncodingException("Code 39 logical width exceeds the supported range.");
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
                    throw new BarcodeEncodingException("Custom Code 39 quiet zones must be at least 10 modules on the left and right.");
                }

                return CopyLogicalQuietZone(minimumCustom);
            case BarcodeQuietZonePolicy.ExactCustom:
                BarcodeQuietZone exactCustom = RequireCustomQuietZone(options);
                ValidateQuietZone(exactCustom);
                return CopyLogicalQuietZone(exactCustom);
            default:
                throw new BarcodeEncodingException("Code 39 quiet-zone policy is invalid.");
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
            throw new BarcodeEncodingException("A custom Code 39 quiet zone is required by the selected policy.");
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
            throw new BarcodeEncodingException("Code 39 quiet-zone dimensions must not be negative.");
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
                throw new BarcodeEncodingException("Resolved Code 39 width exceeds the supported physical range.");
            }
        }

        if (settings.RequestedWidthMillimetres > 0.0 &&
            global::System.Math.Abs(widthMillimetres - settings.RequestedWidthMillimetres) > PhysicalComparisonTolerance)
        {
            throw new BarcodeEncodingException("Requested Code 39 width conflicts with the module size and quiet zones.");
        }

        double barHeight = settings.BarHeightMillimetres;
        double verticalQuietZoneMillimetres = verticalQuietZone * moduleSize;
        if (settings.RequestedHeightMillimetres > 0.0 && verticalQuietZone > 0 && moduleSize == 0.0)
        {
            throw new BarcodeEncodingException("A Code 39 module size is required to resolve physical vertical quiet zones.");
        }

        if (barHeight == 0.0 && settings.RequestedHeightMillimetres > 0.0)
        {
            barHeight = settings.RequestedHeightMillimetres - verticalQuietZoneMillimetres;
            if (barHeight <= 0.0)
            {
                throw new BarcodeEncodingException("Requested Code 39 height does not leave a positive bar height.");
            }
        }

        double heightMillimetres = 0.0;
        if (barHeight > 0.0)
        {
            heightMillimetres = barHeight + verticalQuietZoneMillimetres;
            if (double.IsInfinity(heightMillimetres))
            {
                throw new BarcodeEncodingException("Resolved Code 39 height exceeds the supported physical range.");
            }
        }

        if (settings.RequestedHeightMillimetres > 0.0 &&
            global::System.Math.Abs(heightMillimetres - settings.RequestedHeightMillimetres) > PhysicalComparisonTolerance)
        {
            throw new BarcodeEncodingException("Requested Code 39 height conflicts with the bar height and quiet zones.");
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
            throw new BarcodeEncodingException("Code 39 sizing dimensions must not be negative.");
        }

        if (sizing.RequestedWidthModules != 0 && sizing.RequestedWidthModules != symbolWidthModules)
        {
            throw new BarcodeEncodingException("Requested Code 39 module width does not match the encoded symbol.");
        }

        if (sizing.RequestedHeightModules != 0 && sizing.RequestedHeightModules != 1)
        {
            throw new BarcodeEncodingException("A logical Code 39 symbol must have one module row.");
        }

        ValidateOptionalMeasurement(sizing.RequestedWidthMillimetres, "Requested Code 39 width must be finite and non-negative.");
        ValidateOptionalMeasurement(sizing.RequestedHeightMillimetres, "Requested Code 39 height must be finite and non-negative.");
        ValidateOptionalMeasurement(sizing.DotsPerInch, "Code 39 resolution must be finite and non-negative.");
        ValidateOptionalMeasurement(sizing.ModuleSizeMillimetres, "Code 39 module size must be finite and non-negative.");
        ValidateOptionalMeasurement(sizing.BarHeightMillimetres, "Code 39 bar height must be finite and non-negative.");
        ValidateOptionalMeasurement(sizing.TargetAspectRatio, "Code 39 target aspect ratio must be finite and non-negative.");
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
                    throw new BarcodeEncodingException("Custom Code 39 human-readable text must not be null.");
                }

                return options.CustomText;
            default:
                throw new BarcodeEncodingException("Code 39 human-readable text mode is invalid.");
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

internal class Code39SuppliedCheckData
{
    internal string DataText = "";
    internal char CheckCharacter;
    internal Code39EncodedData Encoded = new Code39EncodedData();
}

internal class Code39ElementLayout
{
    internal double[] Widths = new double[0];
    internal bool[] IsDark = new bool[0];
    internal double WidthModules;
}
