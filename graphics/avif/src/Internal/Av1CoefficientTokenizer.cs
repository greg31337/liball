using System.Collections.Generic;

namespace avif;

internal class Av1CoefficientTokenizationRequest
{
    internal Av1TransformSize TransformSize;
    internal Av1TransformType TransformType;
    internal int Plane;
    internal int TransformBlockSkipContext;
    internal int DcSignContext;
    internal int[] QuantizedCoefficients;
    internal int EndOfBlock;

    internal Av1CoefficientTokenizationRequest()
    {
        TransformSize = Av1TransformSize.Transform4x4;
        TransformType = Av1TransformType.DctDct;
        QuantizedCoefficients = System.Array.Empty<int>();
    }
}

internal class Av1CoefficientToken
{
    internal bool Symbol;
    internal string TableName;
    internal int Row;
    internal int Value;

    internal Av1CoefficientToken(bool symbol, string tableName, int row, int value)
    {
        Symbol = symbol;
        TableName = tableName;
        Row = row;
        Value = value;
    }
}

internal class Av1CoefficientTokenizationResult
{
    internal AvifResultCode Code;
    internal Av1CoefficientToken[] Tokens;
    internal int[] Symbols;
    internal int[] Bits;

    internal Av1CoefficientTokenizationResult(
        AvifResultCode code,
        Av1CoefficientToken[] tokens,
        int[] symbols,
        int[] bits)
    {
        Code = code;
        Tokens = tokens;
        Symbols = symbols;
        Bits = bits;
    }
}

// AV1 specification sections 5.11.39 and 7.11.3.14 through 7.11.3.19.
internal static class Av1CoefficientTokenizer
{
    const string TransformBlockSkipTable = "Default_Txb_Skip_Cdf";
    const string EndOfBlockExtraTable = "Default_Eob_Extra_Cdf";
    const string DcSignTable = "Default_Dc_Sign_Cdf";
    const string CoefficientBaseEobTable = "Default_Coeff_Base_Eob_Cdf";
    const string CoefficientBaseTable = "Default_Coeff_Base_Cdf";
    const string CoefficientBaseRangeTable = "Default_Coeff_Br_Cdf";

    static readonly int[] endOfBlockGroupStarts = new int[]
    {
        0,1,2,3,5,9,17,33,65,129,257,513
    };

    static readonly int[] endOfBlockOffsetBits = new int[]
    {
        0,0,0,1,2,3,4,5,6,7,8,9
    };

    internal static Av1CoefficientTokenizationResult Tokenize(
        Av1CoefficientTokenizationRequest request)
    {
        AvifResultCode validation = Validate(request);
        if (validation != AvifResultCode.Success)
        {
            return Fail(validation);
        }

        List<Av1CoefficientToken> tokens = new List<Av1CoefficientToken>();
        List<int> symbols = new List<int>();
        List<int> bits = new List<int>();
        int entropySize = GetEntropyTransformSize(request.TransformSize);
        int planeType = request.Plane == 0 ? 0 : 1;
        int skipRow = entropySize * Av1SyntaxConstants.TransformBlockSkipContexts
            + request.TransformBlockSkipContext;
        if (request.EndOfBlock == 0)
        {
            AddSymbol(tokens, symbols, TransformBlockSkipTable, skipRow, 1);
            return CreateResult(tokens, symbols, bits);
        }

        AddSymbol(tokens, symbols, TransformBlockSkipTable, skipRow, 0);
        int[] scan = Av1ScanTables.GetScan(
            request.TransformSize, request.TransformType);
        int[] levels = new int[request.QuantizedCoefficients.Length];
        int index = 0;
        while (index < levels.Length)
        {
            int value = request.QuantizedCoefficients[index];
            if (value == int.MinValue)
            {
                return Fail(AvifResultCode.InvalidArgument);
            }
            levels[index] = value < 0 ? -value : value;
            index++;
        }

        WriteEndOfBlock(
            request, tokens, symbols, bits, entropySize, planeType, levels.Length);
        Av1TransformClass transformClass =
            Av1ConversionTables.GetTransformClass(request.TransformType);
        int lastScanIndex = request.EndOfBlock - 1;
        int lastPosition = scan[lastScanIndex];
        int lastContext = GetEndOfBlockBaseContext(lastScanIndex, levels.Length);
        int lastRow = (entropySize * 2 + planeType)
            * Av1SyntaxConstants.EndOfBlockCoefficientContexts + lastContext;
        int lastLevel = levels[lastPosition];
        AddSymbol(
            tokens,
            symbols,
            CoefficientBaseEobTable,
            lastRow,
            Minimum(lastLevel, 3) - 1);
        if (lastLevel > Av1SyntaxConstants.QuantizerBaseLevelCount)
        {
            WriteBaseRange(
                request,
                tokens,
                symbols,
                levels,
                lastPosition,
                lastLevel,
                entropySize,
                planeType,
                transformClass,
                true);
        }

        int scanIndex = lastScanIndex - 1;
        while (scanIndex >= 0)
        {
            int position = scan[scanIndex];
            int context = GetCoefficientBaseContext(
                levels, position, request.TransformSize, transformClass);
            int row = (entropySize * 2 + planeType)
                * Av1SyntaxConstants.SignificantCoefficientContexts + context;
            int level = levels[position];
            AddSymbol(
                tokens,
                symbols,
                CoefficientBaseTable,
                row,
                Minimum(level, 3));
            if (level > Av1SyntaxConstants.QuantizerBaseLevelCount)
            {
                WriteBaseRange(
                    request,
                    tokens,
                    symbols,
                    levels,
                    position,
                    level,
                    entropySize,
                    planeType,
                    transformClass,
                    false);
            }
            scanIndex--;
        }

        scanIndex = 0;
        while (scanIndex < request.EndOfBlock)
        {
            int position = scan[scanIndex];
            int level = levels[position];
            if (level != 0)
            {
                int sign = request.QuantizedCoefficients[position] < 0 ? 1 : 0;
                if (scanIndex == 0)
                {
                    int signRow = planeType * 3 + request.DcSignContext;
                    AddSymbol(tokens, symbols, DcSignTable, signRow, sign);
                }
                else
                {
                    AddBit(tokens, bits, sign);
                }

                if (level >= Av1SyntaxConstants.CoefficientBaseRange
                    + Av1SyntaxConstants.QuantizerBaseLevelCount + 1)
                {
                    WriteGolomb(tokens, bits,
                        level - (Av1SyntaxConstants.CoefficientBaseRange
                            + Av1SyntaxConstants.QuantizerBaseLevelCount + 1));
                }
            }
            scanIndex++;
        }

        return CreateResult(tokens, symbols, bits);
    }

    internal static AvifResultCode WriteTokens(
        Av1CoefficientTokenizationResult? tokenization,
        Av1SymbolEncoder encoder,
        Av1TileCdfContext context)
    {
        if (tokenization == null || encoder == null || context == null
            || tokenization.Code != AvifResultCode.Success
            || encoder.Code != AvifResultCode.Success)
        {
            return AvifResultCode.InvalidArgument;
        }

        int index = 0;
        while (index < tokenization.Tokens.Length
            && encoder.Code == AvifResultCode.Success)
        {
            Av1CoefficientToken token = tokenization.Tokens[index];
            if (token.Symbol)
            {
                Av1Cdf? cdf = context.GetCdf(token.TableName, token.Row);
                if (cdf == null)
                {
                    return AvifResultCode.InvalidArgument;
                }
                encoder.WriteCdfSymbol(cdf, token.Value);
            }
            else
            {
                encoder.WriteBoolean(token.Value);
            }
            index++;
        }
        return encoder.Code;
    }

    static void WriteEndOfBlock(
        Av1CoefficientTokenizationRequest request,
        List<Av1CoefficientToken> tokens,
        List<int> symbols,
        List<int> bits,
        int entropySize,
        int planeType,
        int coefficientCount)
    {
        int multiSize = FloorLog2(coefficientCount) - 4;
        if (multiSize < 0)
        {
            multiSize = 0;
        }
        if (multiSize > 6)
        {
            multiSize = 6;
        }
        int token = 1;
        while (token < endOfBlockGroupStarts.Length - 1
            && request.EndOfBlock >= endOfBlockGroupStarts[token + 1])
        {
            token++;
        }
        Av1TransformClass transformClass =
            Av1ConversionTables.GetTransformClass(request.TransformType);
        int row = multiSize < 5
            ? planeType * 2
                + (transformClass == Av1TransformClass.TwoDimensional ? 0 : 1)
            : planeType;
        AddSymbol(
            tokens,
            symbols,
            GetEndOfBlockTableName(multiSize),
            row,
            token - 1);
        int bitCount = endOfBlockOffsetBits[token];
        if (bitCount > 0)
        {
            int extra = request.EndOfBlock - endOfBlockGroupStarts[token];
            int extraRow = (entropySize * 2 + planeType) * 9 + token - 3;
            AddSymbol(
                tokens,
                symbols,
                EndOfBlockExtraTable,
                extraRow,
                extra >> (bitCount - 1));
            int bit = bitCount - 2;
            while (bit >= 0)
            {
                AddBit(tokens, bits, extra >> bit & 1);
                bit--;
            }
        }
    }

    static void WriteBaseRange(
        Av1CoefficientTokenizationRequest request,
        List<Av1CoefficientToken> tokens,
        List<int> symbols,
        int[] levels,
        int position,
        int level,
        int entropySize,
        int planeType,
        Av1TransformClass transformClass,
        bool endOfBlock)
    {
        int context = endOfBlock
            ? GetEndOfBlockRangeContext(
                position, request.TransformSize, transformClass)
            : GetBaseRangeContext(
                levels, position, request.TransformSize, transformClass);
        int rangeSize = entropySize > 3 ? 3 : entropySize;
        int row = (rangeSize * 2 + planeType)
            * Av1SyntaxConstants.LevelContexts + context;
        int remaining = level - 3;
        int index = 0;
        while (index < Av1SyntaxConstants.CoefficientBaseRange)
        {
            int value = remaining > 3 ? 3 : remaining;
            AddSymbol(
                tokens,
                symbols,
                CoefficientBaseRangeTable,
                row,
                value);
            remaining -= value;
            if (value < 3)
            {
                break;
            }
            index += 3;
        }
    }

    static void WriteGolomb(
        List<Av1CoefficientToken> tokens,
        List<int> bits,
        int value)
    {
        int codeNumber = value + 1;
        int leadingZeros = FloorLog2(codeNumber);
        int index = 0;
        while (index < leadingZeros)
        {
            AddBit(tokens, bits, 0);
            index++;
        }
        AddBit(tokens, bits, 1);
        int bit = leadingZeros - 1;
        while (bit >= 0)
        {
            AddBit(tokens, bits, codeNumber >> bit & 1);
            bit--;
        }
    }

    static int GetCoefficientBaseContext(
        int[] levels,
        int position,
        Av1TransformSize transformSize,
        Av1TransformClass transformClass)
    {
        int[] dimensions = GetScanDimensions(transformSize);
        int row = position % dimensions[0];
        int column = position / dimensions[0];
        int stats = GetForwardLevel(levels, dimensions, row + 1, column, 3)
            + GetForwardLevel(levels, dimensions, row, column + 1, 3);
        if (transformClass == Av1TransformClass.TwoDimensional)
        {
            stats += GetForwardLevel(levels, dimensions, row + 1, column + 1, 3)
                + GetForwardLevel(levels, dimensions, row + 2, column, 3)
                + GetForwardLevel(levels, dimensions, row, column + 2, 3);
        }
        else if (transformClass == Av1TransformClass.Vertical)
        {
            stats += GetForwardLevel(levels, dimensions, row + 2, column, 3)
                + GetForwardLevel(levels, dimensions, row + 3, column, 3)
                + GetForwardLevel(levels, dimensions, row + 4, column, 3);
        }
        else
        {
            stats += GetForwardLevel(levels, dimensions, row, column + 2, 3)
                + GetForwardLevel(levels, dimensions, row, column + 3, 3)
                + GetForwardLevel(levels, dimensions, row, column + 4, 3);
        }
        if (position == 0 && transformClass == Av1TransformClass.TwoDimensional)
        {
            return 0;
        }
        int context = (stats + 1) >> 1;
        if (context > 4)
        {
            context = 4;
        }
        if (transformClass == Av1TransformClass.Horizontal)
        {
            return context + 26 + (column == 0 ? 0 : (column == 1 ? 5 : 10));
        }
        if (transformClass == Av1TransformClass.Vertical)
        {
            return context + 26 + (row == 0 ? 0 : (row == 1 ? 5 : 10));
        }
        int width = Av1ConversionTables.GetTransformWidth(
            Av1ConversionTables.GetAdjustedTransformSize(transformSize));
        int height = Av1ConversionTables.GetTransformHeight(
            Av1ConversionTables.GetAdjustedTransformSize(transformSize));
        if (width < height && row < 2)
        {
            return 11 + context;
        }
        if (width > height && column < 2)
        {
            return 16 + context;
        }
        if (row + column < 2)
        {
            return 1 + context;
        }
        return row + column < 4 ? 6 + context : 21 + context;
    }

    static int GetBaseRangeContext(
        int[] levels,
        int position,
        Av1TransformSize transformSize,
        Av1TransformClass transformClass)
    {
        int[] dimensions = GetScanDimensions(transformSize);
        int row = position % dimensions[0];
        int column = position / dimensions[0];
        int magnitude = GetForwardLevel(levels, dimensions, row + 1, column, 12)
            + GetForwardLevel(levels, dimensions, row, column + 1, 12);
        if (transformClass == Av1TransformClass.TwoDimensional)
        {
            magnitude += GetForwardLevel(
                levels, dimensions, row + 1, column + 1, 12);
        }
        else if (transformClass == Av1TransformClass.Vertical)
        {
            magnitude += GetForwardLevel(levels, dimensions, row + 2, column, 12);
        }
        else
        {
            magnitude += GetForwardLevel(levels, dimensions, row, column + 2, 12);
        }
        magnitude = (magnitude + 1) >> 1;
        if (magnitude > 6)
        {
            magnitude = 6;
        }
        if (position == 0)
        {
            return magnitude;
        }
        bool nearOrigin = transformClass == Av1TransformClass.TwoDimensional
            ? row < 2 && column < 2
            : (transformClass == Av1TransformClass.Vertical
                ? row == 0
                : column == 0);
        return magnitude + (nearOrigin ? 7 : 14);
    }

    static int GetEndOfBlockRangeContext(
        int position,
        Av1TransformSize transformSize,
        Av1TransformClass transformClass)
    {
        int[] dimensions = GetScanDimensions(transformSize);
        int row = position % dimensions[0];
        int column = position / dimensions[0];
        if (position == 0)
        {
            return 0;
        }
        if (transformClass == Av1TransformClass.TwoDimensional
            && row < 2 && column < 2
            || transformClass == Av1TransformClass.Horizontal && column == 0
            || transformClass == Av1TransformClass.Vertical && row == 0)
        {
            return 7;
        }
        return 14;
    }

    static int GetForwardLevel(
        int[] levels,
        int[] dimensions,
        int row,
        int column,
        int maximum)
    {
        if (row < 0 || row >= dimensions[0]
            || column < 0 || column >= dimensions[1])
        {
            return 0;
        }
        int level = levels[column * dimensions[0] + row];
        return level > maximum ? maximum : level;
    }

    static int[] GetScanDimensions(Av1TransformSize transformSize)
    {
        Av1TransformSize adjusted =
            Av1ConversionTables.GetAdjustedTransformSize(transformSize);
        return new int[]
        {
            Av1ConversionTables.GetTransformHeight(adjusted),
            Av1ConversionTables.GetTransformWidth(adjusted)
        };
    }

    static int GetEndOfBlockBaseContext(int scanIndex, int coefficientCount)
    {
        if (scanIndex == 0)
        {
            return 0;
        }
        if (scanIndex <= coefficientCount >> 3)
        {
            return 1;
        }
        return scanIndex <= coefficientCount >> 2 ? 2 : 3;
    }

    static int GetEntropyTransformSize(Av1TransformSize transformSize)
    {
        Av1TransformSize adjusted =
            Av1ConversionTables.GetAdjustedTransformSize(transformSize);
        int width = Av1ConversionTables.GetTransformWidth(adjusted);
        int height = Av1ConversionTables.GetTransformHeight(adjusted);
        int largest = width > height ? width : height;
        if (largest <= 4)
        {
            return 0;
        }
        if (largest <= 8)
        {
            return 1;
        }
        if (largest <= 16)
        {
            return 2;
        }
        return largest <= 32 ? 3 : 4;
    }

    static string GetEndOfBlockTableName(int multiSize)
    {
        if (multiSize == 0)
        {
            return "Default_Eob_Pt_16_Cdf";
        }
        if (multiSize == 1)
        {
            return "Default_Eob_Pt_32_Cdf";
        }
        if (multiSize == 2)
        {
            return "Default_Eob_Pt_64_Cdf";
        }
        if (multiSize == 3)
        {
            return "Default_Eob_Pt_128_Cdf";
        }
        if (multiSize == 4)
        {
            return "Default_Eob_Pt_256_Cdf";
        }
        return multiSize == 5
            ? "Default_Eob_Pt_512_Cdf"
            : "Default_Eob_Pt_1024_Cdf";
    }

    static AvifResultCode Validate(Av1CoefficientTokenizationRequest request)
    {
        int count = request == null || request.QuantizedCoefficients == null
            ? 0
            : request.QuantizedCoefficients.Length;
        if (request == null || request.QuantizedCoefficients == null
            || count == 0 || count > 1024
            || count != Av1ConversionTables.GetTransformCoefficientCount(
                request.TransformSize)
            || request.TransformType < Av1TransformType.DctDct
            || request.TransformType > Av1TransformType.HorizontalFlipAdst
            || request.Plane < 0 || request.Plane > 2
            || request.TransformBlockSkipContext < 0
            || request.TransformBlockSkipContext
                >= Av1SyntaxConstants.TransformBlockSkipContexts
            || request.DcSignContext < 0
            || request.DcSignContext >= Av1SyntaxConstants.DcSignContexts
            || request.EndOfBlock < 0 || request.EndOfBlock > count)
        {
            return AvifResultCode.InvalidArgument;
        }
        if (request.EndOfBlock > 0)
        {
            int[] scan = Av1ScanTables.GetScan(
                request.TransformSize, request.TransformType);
            if (request.QuantizedCoefficients[
                scan[request.EndOfBlock - 1]] == 0)
            {
                return AvifResultCode.InvalidArgument;
            }
            int index = request.EndOfBlock;
            while (index < count)
            {
                if (request.QuantizedCoefficients[scan[index]] != 0)
                {
                    return AvifResultCode.InvalidArgument;
                }
                index++;
            }
        }
        else
        {
            int index = 0;
            while (index < count)
            {
                if (request.QuantizedCoefficients[index] != 0)
                {
                    return AvifResultCode.InvalidArgument;
                }
                index++;
            }
        }
        return AvifResultCode.Success;
    }

    static void AddSymbol(
        List<Av1CoefficientToken> tokens,
        List<int> symbols,
        string tableName,
        int row,
        int value)
    {
        tokens.Add(new Av1CoefficientToken(true, tableName, row, value));
        symbols.Add(value);
    }

    static void AddBit(
        List<Av1CoefficientToken> tokens,
        List<int> bits,
        int value)
    {
        tokens.Add(new Av1CoefficientToken(false, string.Empty, 0, value));
        bits.Add(value);
    }

    static Av1CoefficientTokenizationResult CreateResult(
        List<Av1CoefficientToken> tokens,
        List<int> symbols,
        List<int> bits)
    {
        return new Av1CoefficientTokenizationResult(
            AvifResultCode.Success,
            tokens.ToArray(),
            symbols.ToArray(),
            bits.ToArray());
    }

    static Av1CoefficientTokenizationResult Fail(AvifResultCode code)
    {
        return new Av1CoefficientTokenizationResult(
            code,
            System.Array.Empty<Av1CoefficientToken>(),
            System.Array.Empty<int>(),
            System.Array.Empty<int>());
    }

    static int FloorLog2(int value)
    {
        int result = -1;
        while (value != 0)
        {
            value >>= 1;
            result++;
        }
        return result;
    }

    static int Minimum(int first, int second)
    {
        return first < second ? first : second;
    }
}
