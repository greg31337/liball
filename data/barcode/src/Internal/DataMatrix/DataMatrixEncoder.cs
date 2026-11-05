namespace barcode;

internal class DataMatrixEncodingResult
{
    internal BarcodeSymbol Symbol = new BarcodeSymbol();
    internal DataMatrixSymbolInfo SymbolInfo = new DataMatrixSymbolInfo();
    internal DataMatrixMessagePlan MessagePlan = new DataMatrixMessagePlan();
    internal int[] DataCodewords = new int[0];
    internal int[] SymbolCodewords = new int[0];
    internal ModuleMatrix MappingMatrix = new ModuleMatrix(1, 1);
    internal ModuleMatrix SymbolMatrix = new ModuleMatrix(1, 1);
}

internal class DataMatrixSelectionConstraints
{
    internal DataMatrixShape Shape;
    internal int Rows;
    internal int Columns;
    internal double TargetAspectRatio;
}

internal static class DataMatrixEncoder
{
    internal const double AspectComparisonTolerance = 0.000001;

    internal static DataMatrixEncodingResult Encode(
        byte[] data,
        BarcodePayload payload,
        DataMatrixOptions? dataMatrixOptions,
        BarcodeEncodeOptions? encodeOptions,
        bool isGs1)
    {
        if (data == null)
        {
            throw new BarcodeEncodingException("Data Matrix encoder data must not be null.");
        }

        if (payload == null)
        {
            throw new BarcodeEncodingException("Data Matrix encoder payload must not be null.");
        }

        BarcodeEncodeOptions common = encodeOptions == null
            ? new BarcodeEncodeOptions()
            : encodeOptions;
        DataMatrixOptions settings = ResolveOptions(dataMatrixOptions, common);
        DataMatrixMessagePlan messagePlan = DataMatrixControlCodewordEncoder.CreateMessagePlan(
            data,
            settings,
            isGs1);
        DataMatrixSelectionConstraints constraints = CreateSelectionConstraints(settings, common.Sizing);
        DataMatrixSymbolInfo symbolInfo = SelectSymbol(
            messagePlan.Codewords.Length,
            constraints);
        int[] dataCodewords = DataMatrixHighLevelEncoder.PadCodewords(
            messagePlan.Codewords,
            symbolInfo.DataCodewordCapacity);
        int[] symbolCodewords = DataMatrixBlockInterleaver.EncodeSymbolCodewords(
            dataCodewords,
            symbolInfo);
        ModuleMatrix mappingMatrix = DataMatrixPlacement.PlaceCodewords(
            symbolCodewords,
            symbolInfo);
        ModuleMatrix symbolMatrix = CreateSymbolMatrix(mappingMatrix, symbolInfo);
        BarcodeSymbol symbol = DataMatrixLayout.CreateSymbol(
            symbolMatrix,
            symbolCodewords,
            data,
            payload,
            settings,
            common,
            symbolInfo,
            isGs1);

        DataMatrixEncodingResult result = new DataMatrixEncodingResult();
        result.Symbol = symbol;
        result.SymbolInfo = symbolInfo.CopySymbolInfo();
        result.MessagePlan = messagePlan;
        result.DataCodewords = CopyIntegers(dataCodewords);
        result.SymbolCodewords = CopyIntegers(symbolCodewords);
        result.MappingMatrix = CopyMatrix(mappingMatrix);
        result.SymbolMatrix = CopyMatrix(symbolMatrix);
        return result;
    }

    internal static DataMatrixSymbolInfo SelectSymbol(
        int requiredDataCodewords,
        DataMatrixSelectionConstraints constraints)
    {
        if (requiredDataCodewords <= 0
            || requiredDataCodewords > DataMatrixSymbolInfoTable.MaximumDataCodewordCapacity)
        {
            throw new BarcodeEncodingException("Data Matrix data codeword count is outside the standard symbol capacity.");
        }

        ValidateSelectionConstraints(constraints);
        DataMatrixSymbolInfo? selected = null;
        double selectedAspectDifference = double.MaxValue;
        int version = 1;
        while (version <= DataMatrixSymbolInfoTable.SymbolCount)
        {
            DataMatrixSymbolInfo candidate = DataMatrixSymbolInfoTable.GetSymbolByVersion(version);
            if (candidate.DataCodewordCapacity >= requiredDataCodewords
                && ShapeMatches(candidate, constraints.Shape)
                && (constraints.Rows == 0 || candidate.SymbolRows == constraints.Rows)
                && (constraints.Columns == 0 || candidate.SymbolColumns == constraints.Columns))
            {
                double aspectDifference = 0.0;
                if (constraints.TargetAspectRatio > 0.0)
                {
                    double aspect = (double)candidate.SymbolColumns / (double)candidate.SymbolRows;
                    aspectDifference = global::System.Math.Abs(
                        aspect - constraints.TargetAspectRatio);
                }

                if (IsBetterSymbolCandidate(
                    candidate,
                    aspectDifference,
                    selected,
                    selectedAspectDifference,
                    constraints.TargetAspectRatio > 0.0))
                {
                    selected = candidate;
                    selectedAspectDifference = aspectDifference;
                }
            }

            version = version + 1;
        }

        if (selected == null)
        {
            throw new BarcodeEncodingException("No standard Data Matrix symbol satisfies the data capacity and size constraints.");
        }

        return selected.CopySymbolInfo();
    }

    internal static ModuleMatrix CreateSymbolMatrix(
        ModuleMatrix mappingMatrix,
        DataMatrixSymbolInfo symbol)
    {
        ValidateMappingGeometry(mappingMatrix, symbol);
        ModuleMatrix matrix = new ModuleMatrix(symbol.SymbolColumns, symbol.SymbolRows);
        int mappingRow = 0;
        int symbolRow = 0;
        while (mappingRow < symbol.MappingMatrixRows)
        {
            if (mappingRow % symbol.DataRegionRows == 0)
            {
                int topColumn = 0;
                while (topColumn < matrix.Width)
                {
                    matrix.SetModule(topColumn, symbolRow, topColumn % 2 == 0);
                    topColumn = topColumn + 1;
                }

                symbolRow = symbolRow + 1;
            }

            int mappingColumn = 0;
            int symbolColumn = 0;
            while (mappingColumn < symbol.MappingMatrixColumns)
            {
                if (mappingColumn % symbol.DataRegionColumns == 0)
                {
                    matrix.SetModule(symbolColumn, symbolRow, true);
                    symbolColumn = symbolColumn + 1;
                }

                matrix.SetModule(
                    symbolColumn,
                    symbolRow,
                    mappingMatrix.GetModule(mappingColumn, mappingRow));
                symbolColumn = symbolColumn + 1;
                if (mappingColumn % symbol.DataRegionColumns
                    == symbol.DataRegionColumns - 1)
                {
                    matrix.SetModule(symbolColumn, symbolRow, mappingRow % 2 == 0);
                    symbolColumn = symbolColumn + 1;
                }

                mappingColumn = mappingColumn + 1;
            }

            if (symbolColumn != matrix.Width)
            {
                throw new BarcodeValidationException("Data Matrix finder row does not fill the symbol width.");
            }

            symbolRow = symbolRow + 1;
            if (mappingRow % symbol.DataRegionRows == symbol.DataRegionRows - 1)
            {
                int bottomColumn = 0;
                while (bottomColumn < matrix.Width)
                {
                    matrix.SetModule(bottomColumn, symbolRow, true);
                    bottomColumn = bottomColumn + 1;
                }

                symbolRow = symbolRow + 1;
            }

            mappingRow = mappingRow + 1;
        }

        if (symbolRow != matrix.Height)
        {
            throw new BarcodeValidationException("Data Matrix finder regions do not fill the symbol height.");
        }

        return matrix;
    }

    static DataMatrixOptions ResolveOptions(
        DataMatrixOptions? options,
        BarcodeEncodeOptions common)
    {
        DataMatrixOptions settings = new DataMatrixOptions();
        if (options != null)
        {
            settings.Shape = options.Shape;
            settings.EncodingMode = options.EncodingMode;
            settings.Rows = options.Rows;
            settings.Columns = options.Columns;
            settings.EciAssignmentNumber = options.EciAssignmentNumber;
            settings.MacroType = options.MacroType;
            settings.ReaderProgramming = options.ReaderProgramming;
            settings.StructuredAppend = ResultCopier.CopyStructuredAppendInfo(options.StructuredAppend);
        }

        if (settings.EciAssignmentNumber == 0)
        {
            settings.EciAssignmentNumber = common.EciAssignmentNumber;
        }

        if (settings.StructuredAppend == null)
        {
            settings.StructuredAppend = ResultCopier.CopyStructuredAppendInfo(common.StructuredAppend);
        }

        return settings;
    }

    static DataMatrixSelectionConstraints CreateSelectionConstraints(
        DataMatrixOptions options,
        BarcodeSizing? sizing)
    {
        DataMatrixSelectionConstraints constraints = new DataMatrixSelectionConstraints();
        constraints.Shape = options.Shape;
        constraints.Rows = options.Rows;
        constraints.Columns = options.Columns;
        if (sizing != null)
        {
            constraints.Rows = ResolveRequestedDimension(
                constraints.Rows,
                sizing.RequestedHeightModules,
                "Data Matrix row constraints conflict.");
            constraints.Columns = ResolveRequestedDimension(
                constraints.Columns,
                sizing.RequestedWidthModules,
                "Data Matrix column constraints conflict.");
            constraints.TargetAspectRatio = sizing.TargetAspectRatio;
        }

        return constraints;
    }

    static int ResolveRequestedDimension(int first, int second, string message)
    {
        if (first < 0 || second < 0)
        {
            throw new BarcodeEncodingException("Data Matrix requested module dimensions must not be negative.");
        }

        if (first != 0 && second != 0 && first != second)
        {
            throw new BarcodeEncodingException(message);
        }

        return first != 0 ? first : second;
    }

    static void ValidateSelectionConstraints(DataMatrixSelectionConstraints constraints)
    {
        if (constraints == null)
        {
            throw new BarcodeValidationException("Data Matrix selection constraints must not be null.");
        }

        if (constraints.Shape != DataMatrixShape.Automatic
            && constraints.Shape != DataMatrixShape.Square
            && constraints.Shape != DataMatrixShape.StandardRectangular)
        {
            throw new BarcodeEncodingException("Data Matrix shape constraint is invalid.");
        }

        if (constraints.Rows < 0 || constraints.Columns < 0)
        {
            throw new BarcodeEncodingException("Data Matrix requested dimensions must not be negative.");
        }

        if (double.IsNaN(constraints.TargetAspectRatio)
            || double.IsInfinity(constraints.TargetAspectRatio)
            || constraints.TargetAspectRatio < 0.0)
        {
            throw new BarcodeEncodingException("Data Matrix target aspect ratio must be finite and non-negative.");
        }
    }

    static bool ShapeMatches(DataMatrixSymbolInfo symbol, DataMatrixShape shape)
    {
        if (shape == DataMatrixShape.Automatic)
        {
            return true;
        }

        if (shape == DataMatrixShape.Square)
        {
            return !symbol.IsRectangular;
        }

        return symbol.IsRectangular;
    }

    static bool IsBetterSymbolCandidate(
        DataMatrixSymbolInfo candidate,
        double aspectDifference,
        DataMatrixSymbolInfo? selected,
        double selectedAspectDifference,
        bool preferAspect)
    {
        if (selected == null)
        {
            return true;
        }

        if (preferAspect)
        {
            if (aspectDifference + AspectComparisonTolerance < selectedAspectDifference)
            {
                return true;
            }

            if (global::System.Math.Abs(aspectDifference - selectedAspectDifference)
                > AspectComparisonTolerance)
            {
                return false;
            }
        }

        if (candidate.DataCodewordCapacity != selected.DataCodewordCapacity)
        {
            return candidate.DataCodewordCapacity < selected.DataCodewordCapacity;
        }

        if (candidate.IsRectangular != selected.IsRectangular)
        {
            return !candidate.IsRectangular;
        }

        return candidate.Version < selected.Version;
    }

    static void ValidateMappingGeometry(
        ModuleMatrix mappingMatrix,
        DataMatrixSymbolInfo symbol)
    {
        if (mappingMatrix == null || symbol == null)
        {
            throw new BarcodeValidationException("Data Matrix mapping matrix and symbol information must not be null.");
        }

        if (mappingMatrix.Width != symbol.MappingMatrixColumns
            || mappingMatrix.Height != symbol.MappingMatrixRows
            || symbol.SymbolColumns != symbol.MappingMatrixColumns
                + (2 * symbol.HorizontalDataRegionCount)
            || symbol.SymbolRows != symbol.MappingMatrixRows
                + (2 * symbol.VerticalDataRegionCount))
        {
            throw new BarcodeValidationException("Data Matrix mapping and finder-region dimensions are inconsistent.");
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

    static ModuleMatrix CopyMatrix(ModuleMatrix source)
    {
        ModuleMatrix copy = new ModuleMatrix(source.Width, source.Height);
        copy.LoadModules(source.CopyModules());
        return copy;
    }
}
