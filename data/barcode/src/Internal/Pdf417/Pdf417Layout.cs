namespace barcode;

internal static class Pdf417Layout
{
    internal const int StandardQuietZoneModules = 2;
    internal const int DefaultRowHeightModules = 3;
    internal const double HumanReadableHeight = 1.0;
    internal const double PhysicalComparisonTolerance = 0.000001;
    internal const double MillimetresPerInch = 25.4;

    internal static BarcodeSymbol CreateSymbol(
        Pdf417SymbolPlan plan,
        int[] symbolCodewords,
        byte[] data,
        BarcodePayload payload,
        BarcodeEncodeOptions encodeOptions)
    {
        ValidateSymbolInputs(plan, symbolCodewords, data, payload, encodeOptions);
        int symbolHeightModules = CheckedMath.Multiply(
            plan.Rows,
            DefaultRowHeightModules);
        byte[] modules = CreateModules(plan, symbolCodewords);
        BarcodeQuietZone requiredQuietZone = CreateRequiredQuietZone();
        BarcodeQuietZone selectedQuietZone = ResolveQuietZone(
            encodeOptions,
            requiredQuietZone);
        BarcodeSize size = ResolveSize(
            plan.RowWidthModules,
            symbolHeightModules,
            encodeOptions.Sizing,
            selectedQuietZone);
        BarcodePayload symbolPayload = CreateSymbolPayload(payload, data, encodeOptions);
        BarcodeTextLayout? textLayout = CreateTextLayout(
            symbolPayload,
            encodeOptions,
            size,
            selectedQuietZone,
            plan.RowWidthModules);

        BarcodeSymbol symbol = new BarcodeSymbol();
        symbol.Format = plan.Variant == Pdf417Variant.Compact
            ? BarcodeFormat.CompactPdf417
            : BarcodeFormat.Pdf417;
        symbol.Dimension = BarcodeDimension.StackedLinear;
        symbol.WidthModules = plan.RowWidthModules;
        symbol.HeightModules = symbolHeightModules;
        symbol.Modules = modules;
        symbol.Payload = symbolPayload;
        symbol.Size = size;
        symbol.RequiredQuietZone = requiredQuietZone;
        symbol.TextLayout = textLayout;
        symbol.SymbologyIdentifier = BarcodeSymbologyIdentifiers.Pdf417;
        symbol.Codewords = CopyIntegers(symbolCodewords);
        symbol.HasChecksum = false;
        return symbol;
    }

    internal static int GetClusterIndex(int rowNumber)
    {
        if (rowNumber < 0 || rowNumber >= Pdf417SymbolPlanner.MaximumRows)
        {
            throw new BarcodeValidationException("PDF417 row number must be from zero through eighty-nine.");
        }

        return rowNumber % Pdf417CodewordTable.ClusterCount;
    }

    internal static int CalculateLeftRowIndicator(
        int rowNumber,
        int rowCount,
        int columnCount,
        int errorCorrectionLevel)
    {
        ValidateRowMetadata(rowNumber, rowCount, columnCount, errorCorrectionLevel);
        int rowGroup = rowNumber / Pdf417CodewordTable.ClusterCount;
        int clusterIndex = GetClusterIndex(rowNumber);
        if (clusterIndex == 0)
        {
            return (30 * rowGroup) + ((rowCount - 1) / 3);
        }

        if (clusterIndex == 1)
        {
            return (30 * rowGroup)
                + (errorCorrectionLevel * 3)
                + ((rowCount - 1) % 3);
        }

        return (30 * rowGroup) + columnCount - 1;
    }

    internal static int CalculateRightRowIndicator(
        int rowNumber,
        int rowCount,
        int columnCount,
        int errorCorrectionLevel)
    {
        ValidateRowMetadata(rowNumber, rowCount, columnCount, errorCorrectionLevel);
        int rowGroup = rowNumber / Pdf417CodewordTable.ClusterCount;
        int clusterIndex = GetClusterIndex(rowNumber);
        if (clusterIndex == 0)
        {
            return (30 * rowGroup) + columnCount - 1;
        }

        if (clusterIndex == 1)
        {
            return (30 * rowGroup) + ((rowCount - 1) / 3);
        }

        return (30 * rowGroup)
            + (errorCorrectionLevel * 3)
            + ((rowCount - 1) % 3);
    }

    internal static byte[] CreateModules(Pdf417SymbolPlan plan, int[] symbolCodewords)
    {
        int height = CheckedMath.Multiply(plan.Rows, DefaultRowHeightModules);
        byte[] modules = new byte[CheckedMath.Multiply(plan.RowWidthModules, height)];
        byte[] rowModules = new byte[plan.RowWidthModules];
        int rowNumber = 0;
        while (rowNumber < plan.Rows)
        {
            ClearRow(rowModules);
            int clusterIndex = GetClusterIndex(rowNumber);
            int moduleOffset = AppendPattern(
                rowModules,
                0,
                Pdf417CodewordTable.StartPatternBits,
                Pdf417CodewordTable.StartPatternModuleCount);
            int leftIndicator = CalculateLeftRowIndicator(
                rowNumber,
                plan.Rows,
                plan.Columns,
                plan.ErrorCorrectionLevel);
            moduleOffset = AppendPattern(
                rowModules,
                moduleOffset,
                Pdf417CodewordTable.GetPattern(clusterIndex, leftIndicator),
                Pdf417CodewordTable.ModulesPerCodeword);

            int column = 0;
            while (column < plan.Columns)
            {
                int codewordIndex = (rowNumber * plan.Columns) + column;
                moduleOffset = AppendPattern(
                    rowModules,
                    moduleOffset,
                    Pdf417CodewordTable.GetPattern(
                        clusterIndex,
                        symbolCodewords[codewordIndex]),
                    Pdf417CodewordTable.ModulesPerCodeword);
                column = column + 1;
            }

            if (plan.Variant == Pdf417Variant.Standard)
            {
                int rightIndicator = CalculateRightRowIndicator(
                    rowNumber,
                    plan.Rows,
                    plan.Columns,
                    plan.ErrorCorrectionLevel);
                moduleOffset = AppendPattern(
                    rowModules,
                    moduleOffset,
                    Pdf417CodewordTable.GetPattern(clusterIndex, rightIndicator),
                    Pdf417CodewordTable.ModulesPerCodeword);
                moduleOffset = AppendPattern(
                    rowModules,
                    moduleOffset,
                    Pdf417CodewordTable.StopPatternBits,
                    Pdf417CodewordTable.StopPatternModuleCount);
            }
            else
            {
                rowModules[moduleOffset] = 1;
                moduleOffset = moduleOffset + 1;
            }

            if (moduleOffset != plan.RowWidthModules)
            {
                throw new BarcodeEncodingException("PDF417 row construction did not match the planned width.");
            }

            CopyRepeatedRow(modules, rowModules, rowNumber, plan.RowWidthModules);
            rowNumber = rowNumber + 1;
        }

        return modules;
    }

    static int AppendPattern(
        byte[] rowModules,
        int moduleOffset,
        int pattern,
        int moduleCount)
    {
        int bitIndex = moduleCount - 1;
        while (bitIndex >= 0)
        {
            rowModules[moduleOffset] = (byte)((pattern >> bitIndex) & 1);
            moduleOffset = moduleOffset + 1;
            bitIndex = bitIndex - 1;
        }

        return moduleOffset;
    }

    static void CopyRepeatedRow(
        byte[] modules,
        byte[] rowModules,
        int rowNumber,
        int rowWidthModules)
    {
        int repeat = 0;
        while (repeat < DefaultRowHeightModules)
        {
            int targetRow = (rowNumber * DefaultRowHeightModules) + repeat;
            int targetOffset = targetRow * rowWidthModules;
            int moduleIndex = 0;
            while (moduleIndex < rowWidthModules)
            {
                modules[targetOffset + moduleIndex] = rowModules[moduleIndex];
                moduleIndex = moduleIndex + 1;
            }

            repeat = repeat + 1;
        }
    }

    static void ClearRow(byte[] rowModules)
    {
        int index = 0;
        while (index < rowModules.Length)
        {
            rowModules[index] = 0;
            index = index + 1;
        }
    }

    static BarcodePayload CreateSymbolPayload(
        BarcodePayload payload,
        byte[] data,
        BarcodeEncodeOptions options)
    {
        BarcodePayload? copied = ResultCopier.CopyPayload(payload);
        if (copied == null)
        {
            throw new BarcodeValidationException("PDF417 payload copying failed.");
        }

        copied.RawBytes = CopyBytes(data);
        if (options.CharacterEncoding != null)
        {
            copied.CharacterEncoding = options.CharacterEncoding;
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
                    throw new BarcodeEncodingException("Custom PDF417 quiet zones must be at least two modules on every side.");
                }

                return CopyLogicalQuietZone(minimumCustom);
            case BarcodeQuietZonePolicy.ExactCustom:
                BarcodeQuietZone exactCustom = RequireCustomQuietZone(options);
                ValidateQuietZone(exactCustom);
                return CopyLogicalQuietZone(exactCustom);
            default:
                throw new BarcodeEncodingException("PDF417 quiet-zone policy is invalid.");
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
            throw new BarcodeEncodingException("A custom PDF417 quiet zone is required by the selected policy.");
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
            throw new BarcodeEncodingException("PDF417 quiet-zone dimensions must not be negative.");
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
        if (settings.BarHeightMillimetres > 0.0)
        {
            moduleSize = MergeModuleSize(
                moduleSize,
                settings.BarHeightMillimetres / (double)DefaultRowHeightModules,
                "PDF417 row height conflicts with the requested module size.");
        }

        if (settings.RequestedWidthMillimetres > 0.0)
        {
            moduleSize = MergeModuleSize(
                moduleSize,
                settings.RequestedWidthMillimetres / (double)totalWidthModules,
                "Requested PDF417 width conflicts with another physical size constraint.");
        }

        if (settings.RequestedHeightMillimetres > 0.0)
        {
            moduleSize = MergeModuleSize(
                moduleSize,
                settings.RequestedHeightMillimetres / (double)totalHeightModules,
                "Requested PDF417 height conflicts with another physical size constraint.");
        }

        if (pixelScale > 0 && settings.DotsPerInch > 0.0)
        {
            moduleSize = MergeModuleSize(
                moduleSize,
                ((double)pixelScale * MillimetresPerInch) / settings.DotsPerInch,
                "PDF417 pixel scale, resolution, and physical size conflict.");
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
                throw new BarcodeEncodingException("Resolved PDF417 physical dimensions exceed the supported range.");
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
                throw new BarcodeEncodingException("Requested PDF417 pixel width is not an integral module scale.");
            }

            scale = sizing.RequestedWidthPixels / totalWidthModules;
        }

        if (sizing.RequestedHeightPixels > 0)
        {
            if (sizing.RequestedHeightPixels % totalHeightModules != 0)
            {
                throw new BarcodeEncodingException("Requested PDF417 pixel height is not an integral module scale.");
            }

            int heightScale = sizing.RequestedHeightPixels / totalHeightModules;
            if (scale != 0 && heightScale != scale)
            {
                throw new BarcodeEncodingException("Requested PDF417 pixel dimensions would distort modules.");
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
            throw new BarcodeEncodingException("PDF417 sizing dimensions must not be negative.");
        }

        if (sizing.RequestedWidthModules != 0
            && sizing.RequestedWidthModules != symbolWidthModules)
        {
            throw new BarcodeEncodingException("Requested PDF417 module width does not match the selected symbol.");
        }

        if (sizing.RequestedHeightModules != 0
            && sizing.RequestedHeightModules != symbolHeightModules)
        {
            throw new BarcodeEncodingException("Requested PDF417 module height does not match the selected symbol.");
        }

        ValidateMeasurement(
            sizing.RequestedWidthMillimetres,
            "Requested PDF417 width must be finite and non-negative.");
        ValidateMeasurement(
            sizing.RequestedHeightMillimetres,
            "Requested PDF417 height must be finite and non-negative.");
        ValidateMeasurement(
            sizing.DotsPerInch,
            "PDF417 resolution must be finite and non-negative.");
        ValidateMeasurement(
            sizing.ModuleSizeMillimetres,
            "PDF417 module size must be finite and non-negative.");
        ValidateMeasurement(
            sizing.BarHeightMillimetres,
            "PDF417 row height must be finite and non-negative.");
        ValidateMeasurement(
            sizing.TargetAspectRatio,
            "PDF417 target aspect ratio must be finite and non-negative.");
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
                    throw new BarcodeEncodingException("Custom PDF417 human-readable text must not be null.");
                }

                return options.CustomText;
            default:
                throw new BarcodeEncodingException("PDF417 human-readable text mode is invalid.");
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

    static void ValidateSymbolInputs(
        Pdf417SymbolPlan plan,
        int[] symbolCodewords,
        byte[] data,
        BarcodePayload payload,
        BarcodeEncodeOptions encodeOptions)
    {
        if (plan == null
            || symbolCodewords == null
            || data == null
            || payload == null
            || encodeOptions == null)
        {
            throw new BarcodeValidationException("PDF417 layout inputs must not be null.");
        }

        if (plan.Columns < Pdf417SymbolPlanner.MinimumColumns
            || plan.Columns > Pdf417SymbolPlanner.MaximumColumns
            || plan.Rows < Pdf417SymbolPlanner.MinimumRows
            || plan.Rows > Pdf417SymbolPlanner.MaximumRows
            || plan.ErrorCorrectionLevel < 0
            || plan.ErrorCorrectionLevel > Pdf417ReedSolomon.MaximumErrorCorrectionLevel)
        {
            throw new BarcodeValidationException("PDF417 layout metadata is outside the supported range.");
        }

        int expectedTotal = CheckedMath.Multiply(plan.Columns, plan.Rows);
        int expectedWidth = Pdf417SymbolPlanner.CalculateRowWidthModules(
            plan.Columns,
            plan.Variant);
        if (plan.TotalCodewordCount != expectedTotal
            || symbolCodewords.Length != expectedTotal
            || plan.RowWidthModules != expectedWidth)
        {
            throw new BarcodeValidationException("PDF417 layout dimensions do not match the supplied codewords.");
        }

        if (plan.DataCodewords == null
            || plan.DataCodewords.Length != plan.LengthDescriptor
            || plan.LengthDescriptor + plan.ErrorCorrectionCodewordCount != expectedTotal)
        {
            throw new BarcodeValidationException("PDF417 data and error-correction counts are inconsistent.");
        }

        int index = 0;
        while (index < symbolCodewords.Length)
        {
            int codeword = symbolCodewords[index];
            if (codeword < 0 || codeword >= Pdf417CodewordTable.CodewordCount)
            {
                throw new BarcodeValidationException("PDF417 symbol codewords must be between zero and 928.");
            }

            if (index < plan.DataCodewords.Length
                && codeword != plan.DataCodewords[index])
            {
                throw new BarcodeValidationException("PDF417 symbol codewords do not begin with the planned data codewords.");
            }

            index = index + 1;
        }
    }

    static void ValidateRowMetadata(
        int rowNumber,
        int rowCount,
        int columnCount,
        int errorCorrectionLevel)
    {
        if (rowCount < Pdf417SymbolPlanner.MinimumRows
            || rowCount > Pdf417SymbolPlanner.MaximumRows
            || rowNumber < 0
            || rowNumber >= rowCount)
        {
            throw new BarcodeValidationException("PDF417 row metadata contains an invalid row number or row count.");
        }

        if (columnCount < Pdf417SymbolPlanner.MinimumColumns
            || columnCount > Pdf417SymbolPlanner.MaximumColumns
            || errorCorrectionLevel < 0
            || errorCorrectionLevel > Pdf417ReedSolomon.MaximumErrorCorrectionLevel)
        {
            throw new BarcodeValidationException("PDF417 row metadata contains an invalid column count or error-correction level.");
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
