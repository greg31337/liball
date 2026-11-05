namespace barcode;

internal class Pdf417SymbolPlan
{
    internal int[] HighLevelCodewords = new int[0];
    internal int[] DataCodewords = new int[0];
    internal Pdf417Variant Variant;
    internal int Columns;
    internal int Rows;
    internal int ErrorCorrectionLevel;
    internal int ErrorCorrectionCodewordCount;
    internal int LengthDescriptor;
    internal int PaddingCodewordCount;
    internal int TotalCodewordCount;
    internal int RowWidthModules;
    internal double NominalAspectRatio;
    internal double TargetAspectRatio;
}

internal class Pdf417SymbolConstraints
{
    internal int Columns;
    internal int Rows;
    internal int RequestedErrorCorrectionLevel = -1;
    internal Pdf417Variant Variant;
    internal double TargetAspectRatio;
}

internal static class Pdf417SymbolPlanner
{
    internal const int MinimumColumns = 1;
    internal const int MaximumColumns = 30;
    internal const int MinimumRows = 3;
    internal const int MaximumRows = 90;
    internal const int MaximumTotalCodewordCount = 928;
    internal const int MaximumHighLevelCodewordCount = 925;
    internal const int PaddingCodeword = 900;
    internal const int CodewordWidthModules = 17;
    internal const int StandardRowOverheadModules = 69;
    internal const int CompactRowOverheadModules = 35;
    internal const double DefaultTargetAspectRatio = 3.0;
    internal const double DefaultModuleWidthMillimetres = 0.357;
    internal const double DefaultRowHeightMillimetres = 2.0;
    internal const double AspectComparisonTolerance = 0.000000001;

    internal static Pdf417SymbolPlan CreateSymbolPlan(
        int[] highLevelCodewords,
        Pdf417Options? options,
        BarcodeSizing? sizing)
    {
        ValidateHighLevelCodewords(highLevelCodewords);
        Pdf417SymbolConstraints constraints = CreateConstraints(options, sizing);
        int errorCorrectionLevel = SelectErrorCorrectionLevel(
            highLevelCodewords.Length,
            constraints.RequestedErrorCorrectionLevel);
        int errorCorrectionCodewordCount = Pdf417ReedSolomon.GetErrorCorrectionCodewordCount(
            errorCorrectionLevel);
        int requiredTotalCodewordCount = CheckedMath.Add(
            CheckedMath.Add(highLevelCodewords.Length, 1),
            errorCorrectionCodewordCount);
        if (requiredTotalCodewordCount > MaximumTotalCodewordCount)
        {
            throw new BarcodeEncodingException("PDF417 data and error correction exceed the 928-codeword symbol capacity.");
        }

        Pdf417SymbolPlan plan = SelectDimensions(
            highLevelCodewords,
            constraints,
            errorCorrectionLevel,
            errorCorrectionCodewordCount,
            requiredTotalCodewordCount);
        plan.DataCodewords = CreateDataCodewords(
            highLevelCodewords,
            plan.LengthDescriptor,
            plan.PaddingCodewordCount);
        return plan;
    }

    internal static int SelectErrorCorrectionLevel(
        int highLevelCodewordCount,
        int requestedErrorCorrectionLevel)
    {
        if (highLevelCodewordCount <= 0
            || highLevelCodewordCount > MaximumHighLevelCodewordCount)
        {
            throw new BarcodeEncodingException("PDF417 high-level codeword count is outside the standard symbol range.");
        }

        if (requestedErrorCorrectionLevel >= 0)
        {
            if (requestedErrorCorrectionLevel > Pdf417ReedSolomon.MaximumErrorCorrectionLevel)
            {
                throw new BarcodeEncodingException("PDF417 error-correction level must be from zero through eight or negative for automatic.");
            }

            return requestedErrorCorrectionLevel;
        }

        if (highLevelCodewordCount <= 40)
        {
            return 2;
        }

        if (highLevelCodewordCount <= 160)
        {
            return 3;
        }

        if (highLevelCodewordCount <= 320)
        {
            return 4;
        }

        if (highLevelCodewordCount <= 863)
        {
            return 5;
        }

        return 6;
    }

    internal static int CalculateRowWidthModules(
        int columns,
        Pdf417Variant variant)
    {
        if (columns < MinimumColumns || columns > MaximumColumns)
        {
            throw new BarcodeValidationException("PDF417 data-column count must be from one through thirty.");
        }

        ValidateVariant(variant);
        int overhead = variant == Pdf417Variant.Compact
            ? CompactRowOverheadModules
            : StandardRowOverheadModules;
        return CheckedMath.Add(
            CheckedMath.Multiply(columns, CodewordWidthModules),
            overhead);
    }

    static Pdf417SymbolPlan SelectDimensions(
        int[] highLevelCodewords,
        Pdf417SymbolConstraints constraints,
        int errorCorrectionLevel,
        int errorCorrectionCodewordCount,
        int requiredTotalCodewordCount)
    {
        int firstColumn = MinimumColumns;
        int lastColumn = MaximumColumns;
        if (constraints.Columns != 0)
        {
            firstColumn = constraints.Columns;
            lastColumn = constraints.Columns;
        }
        else if (constraints.Rows != 0)
        {
            firstColumn = (requiredTotalCodewordCount + constraints.Rows - 1)
                / constraints.Rows;
            if (firstColumn < MinimumColumns)
            {
                firstColumn = MinimumColumns;
            }

            lastColumn = firstColumn;
        }
        bool selected = false;
        int selectedColumns = 0;
        int selectedRows = 0;
        int selectedPadding = 0;
        int selectedTotal = 0;
        int selectedRowWidth = 0;
        double selectedAspect = 0.0;
        double selectedDifference = double.MaxValue;
        int columns = firstColumn;
        while (columns <= lastColumn)
        {
            int rows = constraints.Rows;
            if (rows == 0)
            {
                rows = (requiredTotalCodewordCount + columns - 1) / columns;
                if (rows < MinimumRows)
                {
                    rows = MinimumRows;
                }
            }

            if (rows >= MinimumRows && rows <= MaximumRows)
            {
                int total = CheckedMath.Multiply(columns, rows);
                if (total >= requiredTotalCodewordCount
                    && total <= MaximumTotalCodewordCount)
                {
                    int rowWidth = CalculateRowWidthModules(columns, constraints.Variant);
                    double aspect = (rowWidth * DefaultModuleWidthMillimetres)
                        / (rows * DefaultRowHeightMillimetres);
                    double difference = global::System.Math.Abs(
                        aspect - constraints.TargetAspectRatio);
                    int padding = total - requiredTotalCodewordCount;
                    if (IsBetterCandidate(
                        selected,
                        difference,
                        selectedDifference,
                        padding,
                        selectedPadding,
                        total,
                        selectedTotal,
                        columns,
                        selectedColumns))
                    {
                        selected = true;
                        selectedColumns = columns;
                        selectedRows = rows;
                        selectedPadding = padding;
                        selectedTotal = total;
                        selectedRowWidth = rowWidth;
                        selectedAspect = aspect;
                        selectedDifference = difference;
                    }
                }
            }

            columns = columns + 1;
        }

        if (!selected)
        {
            throw new BarcodeEncodingException("No PDF417 row and column dimensions satisfy the data capacity and requested constraints.");
        }

        int lengthDescriptor = selectedTotal - errorCorrectionCodewordCount;
        Pdf417SymbolPlan plan = new Pdf417SymbolPlan();
        plan.HighLevelCodewords = CopyIntegers(highLevelCodewords);
        plan.Variant = constraints.Variant;
        plan.Columns = selectedColumns;
        plan.Rows = selectedRows;
        plan.ErrorCorrectionLevel = errorCorrectionLevel;
        plan.ErrorCorrectionCodewordCount = errorCorrectionCodewordCount;
        plan.LengthDescriptor = lengthDescriptor;
        plan.PaddingCodewordCount = selectedPadding;
        plan.TotalCodewordCount = selectedTotal;
        plan.RowWidthModules = selectedRowWidth;
        plan.NominalAspectRatio = selectedAspect;
        plan.TargetAspectRatio = constraints.TargetAspectRatio;
        return plan;
    }

    static bool IsBetterCandidate(
        bool selected,
        double difference,
        double selectedDifference,
        int padding,
        int selectedPadding,
        int total,
        int selectedTotal,
        int columns,
        int selectedColumns)
    {
        if (!selected)
        {
            return true;
        }

        if (difference + AspectComparisonTolerance < selectedDifference)
        {
            return true;
        }

        if (global::System.Math.Abs(difference - selectedDifference)
            > AspectComparisonTolerance)
        {
            return false;
        }

        if (padding != selectedPadding)
        {
            return padding < selectedPadding;
        }

        if (total != selectedTotal)
        {
            return total < selectedTotal;
        }

        return columns < selectedColumns;
    }

    static int[] CreateDataCodewords(
        int[] highLevelCodewords,
        int lengthDescriptor,
        int paddingCodewordCount)
    {
        int[] dataCodewords = new int[lengthDescriptor];
        dataCodewords[0] = lengthDescriptor;
        int index = 0;
        while (index < highLevelCodewords.Length)
        {
            dataCodewords[index + 1] = highLevelCodewords[index];
            index = index + 1;
        }

        int paddingIndex = 0;
        while (paddingIndex < paddingCodewordCount)
        {
            dataCodewords[highLevelCodewords.Length + 1 + paddingIndex] = PaddingCodeword;
            paddingIndex = paddingIndex + 1;
        }

        return dataCodewords;
    }

    static Pdf417SymbolConstraints CreateConstraints(
        Pdf417Options? options,
        BarcodeSizing? sizing)
    {
        Pdf417SymbolConstraints constraints = new Pdf417SymbolConstraints();
        if (options != null)
        {
            constraints.Columns = options.Columns;
            constraints.Rows = options.Rows;
            constraints.RequestedErrorCorrectionLevel = options.ErrorCorrectionLevel;
            constraints.Variant = options.Variant;
        }

        if (sizing != null)
        {
            constraints.TargetAspectRatio = sizing.TargetAspectRatio;
        }

        if (constraints.TargetAspectRatio == 0.0)
        {
            constraints.TargetAspectRatio = DefaultTargetAspectRatio;
        }

        ValidateConstraints(constraints);
        return constraints;
    }

    static void ValidateConstraints(Pdf417SymbolConstraints constraints)
    {
        if (constraints.Columns < 0
            || constraints.Columns > MaximumColumns)
        {
            throw new BarcodeEncodingException("PDF417 requested columns must be zero or from one through thirty.");
        }

        if (constraints.Rows != 0
            && (constraints.Rows < MinimumRows || constraints.Rows > MaximumRows))
        {
            throw new BarcodeEncodingException("PDF417 requested rows must be zero or from three through ninety.");
        }

        if (constraints.Columns != 0
            && constraints.Rows != 0
            && CheckedMath.Multiply(constraints.Columns, constraints.Rows)
                > MaximumTotalCodewordCount)
        {
            throw new BarcodeEncodingException("PDF417 requested rows and columns exceed the 928-codeword symbol limit.");
        }

        if (constraints.RequestedErrorCorrectionLevel
            > Pdf417ReedSolomon.MaximumErrorCorrectionLevel)
        {
            throw new BarcodeEncodingException("PDF417 error-correction level must be from zero through eight or negative for automatic.");
        }

        if (double.IsNaN(constraints.TargetAspectRatio)
            || double.IsInfinity(constraints.TargetAspectRatio)
            || constraints.TargetAspectRatio <= 0.0)
        {
            throw new BarcodeEncodingException("PDF417 target aspect ratio must be finite and positive when specified.");
        }

        ValidateVariant(constraints.Variant);
    }

    static void ValidateVariant(Pdf417Variant variant)
    {
        if (variant != Pdf417Variant.Standard
            && variant != Pdf417Variant.Compact)
        {
            throw new BarcodeEncodingException("PDF417 row-layout variant is invalid.");
        }
    }

    static void ValidateHighLevelCodewords(int[] highLevelCodewords)
    {
        if (highLevelCodewords == null)
        {
            throw new BarcodeEncodingException("PDF417 high-level codewords must not be null.");
        }

        if (highLevelCodewords.Length == 0)
        {
            throw new BarcodeEncodingException("PDF417 requires at least one high-level codeword.");
        }

        if (highLevelCodewords.Length > MaximumHighLevelCodewordCount)
        {
            throw new BarcodeEncodingException("PDF417 high-level data exceed the 925-codeword capacity at error-correction level zero.");
        }

        int index = 0;
        while (index < highLevelCodewords.Length)
        {
            if (highLevelCodewords[index] < 0 || highLevelCodewords[index] > 928)
            {
                throw new BarcodeEncodingException("PDF417 high-level codewords must be between zero and 928.");
            }

            index = index + 1;
        }
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
