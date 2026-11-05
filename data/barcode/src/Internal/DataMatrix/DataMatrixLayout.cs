namespace barcode;

internal static class DataMatrixLayout
{
    internal const int StandardQuietZoneModules = 1;
    internal const double HumanReadableHeight = 1.0;
    internal const double PhysicalComparisonTolerance = 0.000001;
    internal const double MillimetresPerInch = 25.4;

    internal static BarcodeSymbol CreateSymbol(
        ModuleMatrix symbolMatrix,
        int[] symbolCodewords,
        byte[] data,
        BarcodePayload payload,
        DataMatrixOptions dataMatrixOptions,
        BarcodeEncodeOptions encodeOptions,
        DataMatrixSymbolInfo symbolInfo,
        bool isGs1)
    {
        ValidateSymbolInputs(
            symbolMatrix,
            symbolCodewords,
            data,
            payload,
            dataMatrixOptions,
            encodeOptions,
            symbolInfo);
        BarcodeQuietZone requiredQuietZone = CreateRequiredQuietZone();
        BarcodeQuietZone selectedQuietZone = ResolveQuietZone(
            encodeOptions,
            requiredQuietZone);
        BarcodeSize size = ResolveSize(
            symbolInfo.SymbolColumns,
            symbolInfo.SymbolRows,
            encodeOptions.Sizing,
            selectedQuietZone);
        BarcodePayload symbolPayload = CreateSymbolPayload(
            payload,
            data,
            dataMatrixOptions,
            encodeOptions,
            isGs1);
        BarcodeTextLayout? textLayout = CreateTextLayout(
            symbolPayload,
            encodeOptions,
            size,
            selectedQuietZone,
            symbolInfo.SymbolColumns);

        BarcodeSymbol symbol = new BarcodeSymbol();
        symbol.Format = isGs1 ? BarcodeFormat.Gs1DataMatrix : BarcodeFormat.DataMatrix;
        symbol.Dimension = BarcodeDimension.Matrix;
        symbol.WidthModules = symbolInfo.SymbolColumns;
        symbol.HeightModules = symbolInfo.SymbolRows;
        symbol.Modules = ConvertModules(symbolMatrix);
        symbol.Payload = symbolPayload;
        symbol.Size = size;
        symbol.RequiredQuietZone = requiredQuietZone;
        symbol.TextLayout = textLayout;
        symbol.SymbologyIdentifier = isGs1
            ? BarcodeSymbologyIdentifiers.Gs1DataMatrix
            : BarcodeSymbologyIdentifiers.DataMatrix;
        symbol.Codewords = CopyIntegers(symbolCodewords);
        symbol.HasChecksum = false;
        return symbol;
    }

    static BarcodePayload CreateSymbolPayload(
        BarcodePayload payload,
        byte[] data,
        DataMatrixOptions options,
        BarcodeEncodeOptions common,
        bool isGs1)
    {
        BarcodePayload? copied = ResultCopier.CopyPayload(payload);
        if (copied == null)
        {
            throw new BarcodeValidationException("Data Matrix payload copying failed.");
        }

        copied.RawBytes = CopyBytes(data);
        copied.IsGs1 = isGs1;
        if (common.CharacterEncoding != null)
        {
            copied.CharacterEncoding = common.CharacterEncoding;
        }

        if (options.EciAssignmentNumber > 0)
        {
            EciSegment segment = new EciSegment();
            segment.AssignmentNumber = options.EciAssignmentNumber;
            segment.ByteOffset = 0;
            segment.ByteCount = data.Length;
            copied.EciSegments = new EciSegment[] { segment };
            if (copied.CharacterEncoding == null)
            {
                copied.CharacterEncoding = DataMatrixHighLevelDecoder.FindEciEncodingName(
                    options.EciAssignmentNumber);
            }
        }

        if (options.StructuredAppend != null)
        {
            copied.StructuredAppend = ResultCopier.CopyStructuredAppendInfo(
                options.StructuredAppend);
            if (copied.StructuredAppend != null)
            {
                copied.StructuredAppend.Format = isGs1
                    ? BarcodeFormat.Gs1DataMatrix
                    : BarcodeFormat.DataMatrix;
                copied.StructuredAppend.IsLastSegment = copied.StructuredAppend.Index
                    == copied.StructuredAppend.Count - 1;
            }
        }

        return copied;
    }

    static BarcodeQuietZone CreateRequiredQuietZone()
    {
        BarcodeQuietZone quietZone = new BarcodeQuietZone();
        quietZone.LeftModules = StandardQuietZoneModules;
        quietZone.TopModules = StandardQuietZoneModules;
        quietZone.RightModules = StandardQuietZoneModules;
        quietZone.BottomModules = StandardQuietZoneModules;
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
                if (minimumCustom.LeftModules < StandardQuietZoneModules
                    || minimumCustom.TopModules < StandardQuietZoneModules
                    || minimumCustom.RightModules < StandardQuietZoneModules
                    || minimumCustom.BottomModules < StandardQuietZoneModules)
                {
                    throw new BarcodeEncodingException("Custom Data Matrix quiet zones must be at least one module on every side.");
                }

                return CopyLogicalQuietZone(minimumCustom);
            case BarcodeQuietZonePolicy.ExactCustom:
                BarcodeQuietZone exactCustom = RequireCustomQuietZone(options);
                ValidateQuietZone(exactCustom);
                return CopyLogicalQuietZone(exactCustom);
            default:
                throw new BarcodeEncodingException("Data Matrix quiet-zone policy is invalid.");
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
            throw new BarcodeEncodingException("A custom Data Matrix quiet zone is required by the selected policy.");
        }

        return custom;
    }

    static void ValidateQuietZone(BarcodeQuietZone quietZone)
    {
        if (quietZone.LeftModules < 0
            || quietZone.TopModules < 0
            || quietZone.RightModules < 0
            || quietZone.BottomModules < 0
            || quietZone.LeftPixels < 0
            || quietZone.TopPixels < 0
            || quietZone.RightPixels < 0
            || quietZone.BottomPixels < 0)
        {
            throw new BarcodeEncodingException("Data Matrix quiet-zone dimensions must not be negative.");
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
        int symbolHeightModules,
        BarcodeSizing? sizing,
        BarcodeQuietZone quietZone)
    {
        BarcodeSizing settings = sizing == null ? new BarcodeSizing() : sizing;
        ValidateSizing(settings, symbolWidthModules, symbolHeightModules);
        int totalWidthModules = CheckedMath.Add(
            CheckedMath.Add(symbolWidthModules, quietZone.LeftModules),
            quietZone.RightModules);
        int totalHeightModules = CheckedMath.Add(
            CheckedMath.Add(symbolHeightModules, quietZone.TopModules),
            quietZone.BottomModules);
        int pixelScale = ResolvePixelScale(settings, totalWidthModules, totalHeightModules);
        int widthPixels = pixelScale == 0
            ? 0
            : CheckedMath.Multiply(totalWidthModules, pixelScale);
        int heightPixels = pixelScale == 0
            ? 0
            : CheckedMath.Multiply(totalHeightModules, pixelScale);

        double moduleSize = settings.ModuleSizeMillimetres;
        if (settings.RequestedWidthMillimetres > 0.0)
        {
            moduleSize = MergeModuleSize(
                moduleSize,
                settings.RequestedWidthMillimetres / (double)totalWidthModules,
                "Requested Data Matrix width conflicts with another physical size constraint.");
        }

        if (settings.RequestedHeightMillimetres > 0.0)
        {
            moduleSize = MergeModuleSize(
                moduleSize,
                settings.RequestedHeightMillimetres / (double)totalHeightModules,
                "Requested Data Matrix height conflicts with another physical size constraint.");
        }

        if (pixelScale > 0 && settings.DotsPerInch > 0.0)
        {
            moduleSize = MergeModuleSize(
                moduleSize,
                ((double)pixelScale * MillimetresPerInch) / settings.DotsPerInch,
                "Data Matrix pixel scale, resolution, and physical size conflict.");
        }

        double widthMillimetres = 0.0;
        double heightMillimetres = 0.0;
        if (moduleSize > 0.0)
        {
            widthMillimetres = (double)totalWidthModules * moduleSize;
            heightMillimetres = (double)totalHeightModules * moduleSize;
            if (double.IsInfinity(widthMillimetres)
                || double.IsInfinity(heightMillimetres))
            {
                throw new BarcodeEncodingException("Resolved Data Matrix physical dimensions exceed the supported range.");
            }
        }

        BarcodeQuietZone sizedQuietZone = CopyLogicalQuietZone(quietZone);
        if (pixelScale > 0)
        {
            sizedQuietZone.LeftPixels = CheckedMath.Multiply(
                sizedQuietZone.LeftModules,
                pixelScale);
            sizedQuietZone.TopPixels = CheckedMath.Multiply(
                sizedQuietZone.TopModules,
                pixelScale);
            sizedQuietZone.RightPixels = CheckedMath.Multiply(
                sizedQuietZone.RightModules,
                pixelScale);
            sizedQuietZone.BottomPixels = CheckedMath.Multiply(
                sizedQuietZone.BottomModules,
                pixelScale);
        }

        BarcodeSize size = new BarcodeSize();
        size.WidthModules = totalWidthModules;
        size.HeightModules = totalHeightModules;
        size.WidthPixels = widthPixels;
        size.HeightPixels = heightPixels;
        size.WidthMillimetres = widthMillimetres;
        size.HeightMillimetres = heightMillimetres;
        size.DotsPerInch = settings.DotsPerInch;
        size.ModuleSizeMillimetres = moduleSize;
        size.QuietZone = sizedQuietZone;
        return size;
    }

    static int ResolvePixelScale(
        BarcodeSizing sizing,
        int totalWidthModules,
        int totalHeightModules)
    {
        int scale = 0;
        if (sizing.RequestedWidthPixels > 0)
        {
            if (sizing.RequestedWidthPixels % totalWidthModules != 0)
            {
                throw new BarcodeEncodingException("Requested Data Matrix pixel width is not an integral module scale.");
            }

            scale = sizing.RequestedWidthPixels / totalWidthModules;
        }

        if (sizing.RequestedHeightPixels > 0)
        {
            if (sizing.RequestedHeightPixels % totalHeightModules != 0)
            {
                throw new BarcodeEncodingException("Requested Data Matrix pixel height is not an integral module scale.");
            }

            int heightScale = sizing.RequestedHeightPixels / totalHeightModules;
            if (scale != 0 && heightScale != scale)
            {
                throw new BarcodeEncodingException("Requested Data Matrix pixel dimensions would distort square modules.");
            }

            scale = heightScale;
        }

        return scale;
    }

    static double MergeModuleSize(
        double current,
        double candidate,
        string conflictMessage)
    {
        if (current == 0.0)
        {
            return candidate;
        }

        if (global::System.Math.Abs(current - candidate) > PhysicalComparisonTolerance)
        {
            throw new BarcodeEncodingException(conflictMessage);
        }

        return current;
    }

    static void ValidateSizing(
        BarcodeSizing sizing,
        int symbolWidthModules,
        int symbolHeightModules)
    {
        if (sizing.RequestedWidthModules < 0
            || sizing.RequestedHeightModules < 0
            || sizing.RequestedWidthPixels < 0
            || sizing.RequestedHeightPixels < 0)
        {
            throw new BarcodeEncodingException("Data Matrix sizing dimensions must not be negative.");
        }

        if (sizing.RequestedWidthModules != 0
            && sizing.RequestedWidthModules != symbolWidthModules)
        {
            throw new BarcodeEncodingException("Requested Data Matrix module width does not match the selected symbol.");
        }

        if (sizing.RequestedHeightModules != 0
            && sizing.RequestedHeightModules != symbolHeightModules)
        {
            throw new BarcodeEncodingException("Requested Data Matrix module height does not match the selected symbol.");
        }

        if (sizing.BarHeightMillimetres != 0.0)
        {
            throw new BarcodeEncodingException("Data Matrix does not use a linear bar-height setting.");
        }

        ValidateMeasurement(
            sizing.RequestedWidthMillimetres,
            "Requested Data Matrix width must be finite and non-negative.");
        ValidateMeasurement(
            sizing.RequestedHeightMillimetres,
            "Requested Data Matrix height must be finite and non-negative.");
        ValidateMeasurement(
            sizing.DotsPerInch,
            "Data Matrix resolution must be finite and non-negative.");
        ValidateMeasurement(
            sizing.ModuleSizeMillimetres,
            "Data Matrix module size must be finite and non-negative.");
        ValidateMeasurement(
            sizing.TargetAspectRatio,
            "Data Matrix target aspect ratio must be finite and non-negative.");
    }

    static void ValidateMeasurement(double value, string message)
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
        BarcodeQuietZone quietZone,
        int symbolWidthModules)
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
        BarcodePayload payload,
        BarcodeEncodeOptions options)
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
                    throw new BarcodeEncodingException("Custom Data Matrix human-readable text must not be null.");
                }

                return options.CustomText;
            default:
                throw new BarcodeEncodingException("Data Matrix human-readable text mode is invalid.");
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

    static byte[] ConvertModules(ModuleMatrix matrix)
    {
        byte[] modules = new byte[CheckedMath.Multiply(matrix.Width, matrix.Height)];
        int row = 0;
        while (row < matrix.Height)
        {
            int column = 0;
            while (column < matrix.Width)
            {
                if (matrix.GetModule(column, row))
                {
                    modules[(row * matrix.Width) + column] = 1;
                }

                column = column + 1;
            }

            row = row + 1;
        }

        return modules;
    }

    static void ValidateSymbolInputs(
        ModuleMatrix matrix,
        int[] codewords,
        byte[] data,
        BarcodePayload payload,
        DataMatrixOptions dataMatrixOptions,
        BarcodeEncodeOptions encodeOptions,
        DataMatrixSymbolInfo symbolInfo)
    {
        if (matrix == null
            || codewords == null
            || data == null
            || payload == null
            || dataMatrixOptions == null
            || encodeOptions == null
            || symbolInfo == null)
        {
            throw new BarcodeValidationException("Data Matrix layout inputs must not be null.");
        }

        if (matrix.Width != symbolInfo.SymbolColumns
            || matrix.Height != symbolInfo.SymbolRows
            || codewords.Length != symbolInfo.TotalCodewordCount)
        {
            throw new BarcodeValidationException("Data Matrix layout dimensions or codeword count do not match the symbol.");
        }
    }

    static byte[] CopyBytes(byte[] source)
    {
        byte[] copy = new byte[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            copy[index] = source[index];
            index = index + 1;
        }

        return copy;
    }

    static int[] CopyIntegers(int[] source)
    {
        int[] copy = new int[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            copy[index] = source[index];
            index = index + 1;
        }

        return copy;
    }
}
