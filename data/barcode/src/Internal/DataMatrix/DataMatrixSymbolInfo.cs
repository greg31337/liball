namespace barcode;

internal class DataMatrixSymbolInfo
{
    internal int Version;
    internal bool IsRectangular;
    internal int SymbolRows;
    internal int SymbolColumns;
    internal int DataRegionRows;
    internal int DataRegionColumns;
    internal int VerticalDataRegionCount;
    internal int HorizontalDataRegionCount;
    internal int DataRegionCount;
    internal int MappingMatrixRows;
    internal int MappingMatrixColumns;
    internal int DataCodewordCapacity;
    internal int ErrorCodewordCount;
    internal int TotalCodewordCount;
    internal int InterleavedBlockCount;
    internal int[] DataCodewordsPerBlock = new int[0];
    internal int[] ErrorCodewordsPerBlock = new int[0];

    internal int GetDataCodewordCountForBlock(int blockIndex)
    {
        ValidateBlockIndex(blockIndex);
        return DataCodewordsPerBlock[blockIndex];
    }

    internal int GetErrorCodewordCountForBlock(int blockIndex)
    {
        ValidateBlockIndex(blockIndex);
        return ErrorCodewordsPerBlock[blockIndex];
    }

    internal int[] CopyDataCodewordsPerBlock()
    {
        return CopyArray(DataCodewordsPerBlock);
    }

    internal int[] CopyErrorCodewordsPerBlock()
    {
        return CopyArray(ErrorCodewordsPerBlock);
    }

    internal DataMatrixSymbolInfo CopySymbolInfo()
    {
        DataMatrixSymbolInfo copy = new DataMatrixSymbolInfo();
        copy.Version = Version;
        copy.IsRectangular = IsRectangular;
        copy.SymbolRows = SymbolRows;
        copy.SymbolColumns = SymbolColumns;
        copy.DataRegionRows = DataRegionRows;
        copy.DataRegionColumns = DataRegionColumns;
        copy.VerticalDataRegionCount = VerticalDataRegionCount;
        copy.HorizontalDataRegionCount = HorizontalDataRegionCount;
        copy.DataRegionCount = DataRegionCount;
        copy.MappingMatrixRows = MappingMatrixRows;
        copy.MappingMatrixColumns = MappingMatrixColumns;
        copy.DataCodewordCapacity = DataCodewordCapacity;
        copy.ErrorCodewordCount = ErrorCodewordCount;
        copy.TotalCodewordCount = TotalCodewordCount;
        copy.InterleavedBlockCount = InterleavedBlockCount;
        copy.DataCodewordsPerBlock = CopyArray(DataCodewordsPerBlock);
        copy.ErrorCodewordsPerBlock = CopyArray(ErrorCodewordsPerBlock);
        return copy;
    }

    static int[] CopyArray(int[] source)
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

    void ValidateBlockIndex(int blockIndex)
    {
        if (blockIndex < 0 || blockIndex >= InterleavedBlockCount)
        {
            throw new BarcodeValidationException("Data Matrix interleaved block index is outside the symbol's block range.");
        }
    }
}

internal static class DataMatrixSymbolInfoTable
{
    internal const int SquareSymbolCount = 24;
    internal const int StandardRectangularSymbolCount = 6;
    internal const int SymbolCount = 30;
    internal const int MaximumDataCodewordCapacity = 1558;

    static readonly DataMatrixSymbolInfo[] Symbols = CreateSymbols();

    internal static int GetSymbolCount()
    {
        return SymbolCount;
    }

    internal static DataMatrixSymbolInfo GetSymbolByVersion(int version)
    {
        if (version < 1 || version > SymbolCount)
        {
            throw new BarcodeValidationException("Data Matrix ECC 200 version must be one through 30.");
        }

        return Symbols[version - 1].CopySymbolInfo();
    }

    internal static DataMatrixSymbolInfo? FindSymbolByDimensions(int symbolRows, int symbolColumns)
    {
        if (symbolRows <= 0 || symbolColumns <= 0)
        {
            throw new BarcodeValidationException("Data Matrix symbol dimensions must be positive.");
        }

        int index = 0;
        while (index < Symbols.Length)
        {
            DataMatrixSymbolInfo symbol = Symbols[index];
            if (symbol.SymbolRows == symbolRows && symbol.SymbolColumns == symbolColumns)
            {
                return symbol.CopySymbolInfo();
            }

            index = index + 1;
        }

        return null;
    }

    internal static DataMatrixSymbolInfo? FindSymbolForDataCodewords(
        int requiredDataCodewords,
        DataMatrixShape shape)
    {
        if (requiredDataCodewords <= 0)
        {
            throw new BarcodeValidationException("Required Data Matrix data codewords must be positive.");
        }

        ValidateShape(shape);
        DataMatrixSymbolInfo? selected = null;
        int index = 0;
        while (index < Symbols.Length)
        {
            DataMatrixSymbolInfo candidate = Symbols[index];
            if (ShapeMatches(candidate, shape)
                && candidate.DataCodewordCapacity >= requiredDataCodewords
                && (selected == null || candidate.DataCodewordCapacity < selected.DataCodewordCapacity))
            {
                selected = candidate;
            }

            index = index + 1;
        }

        if (selected == null)
        {
            return null;
        }

        return selected.CopySymbolInfo();
    }

    static DataMatrixSymbolInfo[] CreateSymbols()
    {
        return new DataMatrixSymbolInfo[]
        {
            CreateUniformSymbol(1, false, 10, 10, 8, 8, 1, 1, 3, 5, 1, 3, 5),
            CreateUniformSymbol(2, false, 12, 12, 10, 10, 1, 1, 5, 7, 1, 5, 7),
            CreateUniformSymbol(3, false, 14, 14, 12, 12, 1, 1, 8, 10, 1, 8, 10),
            CreateUniformSymbol(4, false, 16, 16, 14, 14, 1, 1, 12, 12, 1, 12, 12),
            CreateUniformSymbol(5, false, 18, 18, 16, 16, 1, 1, 18, 14, 1, 18, 14),
            CreateUniformSymbol(6, false, 20, 20, 18, 18, 1, 1, 22, 18, 1, 22, 18),
            CreateUniformSymbol(7, false, 22, 22, 20, 20, 1, 1, 30, 20, 1, 30, 20),
            CreateUniformSymbol(8, false, 24, 24, 22, 22, 1, 1, 36, 24, 1, 36, 24),
            CreateUniformSymbol(9, false, 26, 26, 24, 24, 1, 1, 44, 28, 1, 44, 28),
            CreateUniformSymbol(10, false, 32, 32, 14, 14, 2, 2, 62, 36, 1, 62, 36),
            CreateUniformSymbol(11, false, 36, 36, 16, 16, 2, 2, 86, 42, 1, 86, 42),
            CreateUniformSymbol(12, false, 40, 40, 18, 18, 2, 2, 114, 48, 1, 114, 48),
            CreateUniformSymbol(13, false, 44, 44, 20, 20, 2, 2, 144, 56, 1, 144, 56),
            CreateUniformSymbol(14, false, 48, 48, 22, 22, 2, 2, 174, 68, 1, 174, 68),
            CreateUniformSymbol(15, false, 52, 52, 24, 24, 2, 2, 204, 84, 2, 102, 42),
            CreateUniformSymbol(16, false, 64, 64, 14, 14, 4, 4, 280, 112, 2, 140, 56),
            CreateUniformSymbol(17, false, 72, 72, 16, 16, 4, 4, 368, 144, 4, 92, 36),
            CreateUniformSymbol(18, false, 80, 80, 18, 18, 4, 4, 456, 192, 4, 114, 48),
            CreateUniformSymbol(19, false, 88, 88, 20, 20, 4, 4, 576, 224, 4, 144, 56),
            CreateUniformSymbol(20, false, 96, 96, 22, 22, 4, 4, 696, 272, 4, 174, 68),
            CreateUniformSymbol(21, false, 104, 104, 24, 24, 4, 4, 816, 336, 6, 136, 56),
            CreateUniformSymbol(22, false, 120, 120, 18, 18, 6, 6, 1050, 408, 6, 175, 68),
            CreateUniformSymbol(23, false, 132, 132, 20, 20, 6, 6, 1304, 496, 8, 163, 62),
            CreateUnevenSymbol144(),
            CreateUniformSymbol(25, true, 8, 18, 6, 16, 1, 1, 5, 7, 1, 5, 7),
            CreateUniformSymbol(26, true, 8, 32, 6, 14, 1, 2, 10, 11, 1, 10, 11),
            CreateUniformSymbol(27, true, 12, 26, 10, 24, 1, 1, 16, 14, 1, 16, 14),
            CreateUniformSymbol(28, true, 12, 36, 10, 16, 1, 2, 22, 18, 1, 22, 18),
            CreateUniformSymbol(29, true, 16, 36, 14, 16, 1, 2, 32, 24, 1, 32, 24),
            CreateUniformSymbol(30, true, 16, 48, 14, 22, 1, 2, 49, 28, 1, 49, 28)
        };
    }

    static DataMatrixSymbolInfo CreateUniformSymbol(
        int version,
        bool isRectangular,
        int symbolRows,
        int symbolColumns,
        int dataRegionRows,
        int dataRegionColumns,
        int verticalDataRegionCount,
        int horizontalDataRegionCount,
        int dataCodewordCapacity,
        int errorCodewordCount,
        int interleavedBlockCount,
        int dataCodewordsPerBlock,
        int errorCodewordsPerBlock)
    {
        int[] dataBlockSizes = CreateRepeatedArray(interleavedBlockCount, dataCodewordsPerBlock);
        int[] errorBlockSizes = CreateRepeatedArray(interleavedBlockCount, errorCodewordsPerBlock);
        return CreateSymbol(
            version,
            isRectangular,
            symbolRows,
            symbolColumns,
            dataRegionRows,
            dataRegionColumns,
            verticalDataRegionCount,
            horizontalDataRegionCount,
            dataCodewordCapacity,
            errorCodewordCount,
            dataBlockSizes,
            errorBlockSizes);
    }

    static DataMatrixSymbolInfo CreateUnevenSymbol144()
    {
        return CreateSymbol(
            24,
            false,
            144,
            144,
            22,
            22,
            6,
            6,
            1558,
            620,
            new int[] { 156, 156, 156, 156, 156, 156, 156, 156, 155, 155 },
            new int[] { 62, 62, 62, 62, 62, 62, 62, 62, 62, 62 });
    }

    static DataMatrixSymbolInfo CreateSymbol(
        int version,
        bool isRectangular,
        int symbolRows,
        int symbolColumns,
        int dataRegionRows,
        int dataRegionColumns,
        int verticalDataRegionCount,
        int horizontalDataRegionCount,
        int dataCodewordCapacity,
        int errorCodewordCount,
        int[] dataCodewordsPerBlock,
        int[] errorCodewordsPerBlock)
    {
        DataMatrixSymbolInfo symbol = new DataMatrixSymbolInfo();
        symbol.Version = version;
        symbol.IsRectangular = isRectangular;
        symbol.SymbolRows = symbolRows;
        symbol.SymbolColumns = symbolColumns;
        symbol.DataRegionRows = dataRegionRows;
        symbol.DataRegionColumns = dataRegionColumns;
        symbol.VerticalDataRegionCount = verticalDataRegionCount;
        symbol.HorizontalDataRegionCount = horizontalDataRegionCount;
        symbol.DataRegionCount = verticalDataRegionCount * horizontalDataRegionCount;
        symbol.MappingMatrixRows = dataRegionRows * verticalDataRegionCount;
        symbol.MappingMatrixColumns = dataRegionColumns * horizontalDataRegionCount;
        symbol.DataCodewordCapacity = dataCodewordCapacity;
        symbol.ErrorCodewordCount = errorCodewordCount;
        symbol.TotalCodewordCount = dataCodewordCapacity + errorCodewordCount;
        symbol.InterleavedBlockCount = dataCodewordsPerBlock.Length;
        symbol.DataCodewordsPerBlock = dataCodewordsPerBlock;
        symbol.ErrorCodewordsPerBlock = errorCodewordsPerBlock;
        return symbol;
    }

    static int[] CreateRepeatedArray(int count, int value)
    {
        int[] values = new int[count];
        int index = 0;
        while (index < values.Length)
        {
            values[index] = value;
            index = index + 1;
        }

        return values;
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

    static void ValidateShape(DataMatrixShape shape)
    {
        if (shape != DataMatrixShape.Automatic
            && shape != DataMatrixShape.Square
            && shape != DataMatrixShape.StandardRectangular)
        {
            throw new BarcodeValidationException("Data Matrix shape is not supported.");
        }
    }
}
