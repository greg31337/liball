using System;

namespace avif;

internal class Av1CoefficientSymbolReader
{
    internal Av1SymbolDecoder? Decoder;
    internal Av1TileCdfContext? Context;
    internal int[] Symbols;
    internal int[] Bits;
    internal int SymbolPosition;
    internal int BitPosition;
    internal bool Scripted;
    internal AvifResultCode Code;

    internal Av1CoefficientSymbolReader(
        Av1SymbolDecoder? decoder,
        Av1TileCdfContext? context)
    {
        Decoder = decoder;
        Context = context;
        Symbols = System.Array.Empty<int>();
        Bits = System.Array.Empty<int>();
        Scripted = false;
        Code = decoder == null || context == null
            ? AvifResultCode.InvalidArgument
            : decoder.Code;
    }

    internal static Av1CoefficientSymbolReader CreateScripted(
        int[] symbols,
        int[] bits)
    {
        Av1CoefficientSymbolReader result =
            new Av1CoefficientSymbolReader(null, null);
        result.Symbols = symbols;
        result.Bits = bits;
        result.Scripted = true;
        result.Code = symbols == null || bits == null
            ? AvifResultCode.InvalidArgument
            : AvifResultCode.Success;
        return result;
    }

    internal static Av1CoefficientSymbolReader Create(
        Av1SymbolDecoder? decoder,
        Av1TileCdfContext? context)
    {
        return new Av1CoefficientSymbolReader(decoder, context);
    }

    internal static Av1CoefficientSymbolReader Create(
        int[] symbols,
        int[] bits)
    {
        return CreateScripted(symbols, bits);
    }

    internal int ReadSymbol(string tableName, int row)
    {
        if (Code != AvifResultCode.Success)
        {
            return 0;
        }
        if (Scripted)
        {
            if (SymbolPosition >= Symbols.Length || Symbols[SymbolPosition] < 0)
            {
                Code = AvifResultCode.InvalidAv1Bitstream;
                return 0;
            }
            return Symbols[SymbolPosition++];
        }

        Av1Cdf? cdf = (Context ?? throw new InvalidOperationException("Invalid internal state.")).GetCdf(tableName, row);
        if (cdf == null)
        {
            Code = AvifResultCode.InvalidArgument;
            return 0;
        }
        int symbol = (Decoder ?? throw new InvalidOperationException("Invalid internal state.")).ReadCdfSymbol(cdf);
        Code = Decoder.Code;
        return symbol;
    }

    internal int ReadBit()
    {
        if (Code != AvifResultCode.Success)
        {
            return 0;
        }
        if (Scripted)
        {
            if (BitPosition >= Bits.Length
                || Bits[BitPosition] < 0 || Bits[BitPosition] > 1)
            {
                Code = AvifResultCode.InvalidAv1Bitstream;
                return 0;
            }
            return Bits[BitPosition++];
        }

        int bit = (Decoder ?? throw new InvalidOperationException("Invalid internal state.")).ReadBoolean();
        Code = Decoder.Code;
        return bit;
    }
}

internal class Av1CoefficientDecodeRequest
{
    internal Av1CoefficientSymbolReader? Reader;
    internal Av1TransformSize TransformSize;
    internal Av1TransformType TransformType;
    internal int Plane;
    internal int BitDepth;
    internal int QIndex;
    internal int DcDelta;
    internal int AcDelta;
    internal bool Lossless;
    internal bool UseQuantizerMatrix;
    internal int MatrixLevel;
    internal int TransformBlockSkipContext;
    internal int DcSignContext;

    internal Av1CoefficientDecodeRequest()
    {
        TransformSize = Av1TransformSize.Transform4x4;
        TransformType = Av1TransformType.DctDct;
        BitDepth = 8;
        MatrixLevel = Av1SyntaxConstants.NumQuantizerMatrixLevels - 1;
    }
}

internal class Av1CoefficientDecodeResult
{
    internal AvifResultCode Code;
    internal int[] QuantizedCoefficients;
    internal int[] DequantizedCoefficients;
    internal int EndOfBlock;
    internal int MaximumScanLine;
    internal int CulLevel;
    internal bool Skipped;

    internal Av1CoefficientDecodeResult(AvifResultCode code, int coefficientCount)
    {
        Code = code;
        QuantizedCoefficients = coefficientCount > 0
            ? new int[coefficientCount]
            : System.Array.Empty<int>();
        DequantizedCoefficients = coefficientCount > 0
            ? new int[coefficientCount]
            : System.Array.Empty<int>();
    }
}

// AV1 specification sections 7.11.3.14 through 7.11.3.19.
internal static class Av1CoefficientDecoder
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

    internal static Av1CoefficientDecodeResult Decode(
        Av1CoefficientDecodeRequest request)
    {
        int coefficientCount = GetCoefficientCount(request);
        AvifResultCode validation = Validate(request, coefficientCount);
        Av1CoefficientDecodeResult result = new Av1CoefficientDecodeResult(
            validation, coefficientCount);
        if (validation != AvifResultCode.Success)
        {
            return result;
        }

        int entropySize = GetEntropyTransformSize(request.TransformSize);
        int planeType = request.Plane == 0 ? 0 : 1;
        int skipRow = entropySize * Av1SyntaxConstants.TransformBlockSkipContexts
            + request.TransformBlockSkipContext;
        int skipped = (request.Reader ?? throw new InvalidOperationException("Invalid internal state.")).ReadSymbol(TransformBlockSkipTable, skipRow);
        if (!ValidSymbol(request.Reader, skipped, 2, result))
        {
            return result;
        }
        if (skipped != 0)
        {
            result.Skipped = true;
            return result;
        }

        int[] scan = Av1ScanTables.GetScan(
            request.TransformSize, request.TransformType);
        int eob = DecodeEndOfBlock(
            request, result, entropySize, planeType, coefficientCount);
        if (result.Code != AvifResultCode.Success)
        {
            return result;
        }
        result.EndOfBlock = eob;

        int[] levels = new int[coefficientCount];
        Av1TransformClass transformClass =
            Av1ConversionTables.GetTransformClass(request.TransformType);
        int lastScanIndex = eob - 1;
        int lastPosition = scan[lastScanIndex];
        int lastContext = GetEndOfBlockBaseContext(lastScanIndex, coefficientCount);
        int lastRow = (entropySize * 2 + planeType)
            * Av1SyntaxConstants.EndOfBlockCoefficientContexts + lastContext;
        int lastBase = request.Reader.ReadSymbol(CoefficientBaseEobTable, lastRow);
        if (!ValidSymbol(request.Reader, lastBase, 3, result))
        {
            return result;
        }
        int lastLevel = lastBase + 1;
        if (lastLevel > Av1SyntaxConstants.QuantizerBaseLevelCount)
        {
            lastLevel = DecodeBaseRange(
                request, result, levels, lastPosition, lastLevel,
                entropySize, planeType, transformClass, true);
        }
        levels[lastPosition] = lastLevel;

        int scanIndex = lastScanIndex - 1;
        while (scanIndex >= 0 && result.Code == AvifResultCode.Success)
        {
            int position = scan[scanIndex];
            int context = GetCoefficientBaseContext(
                levels, position, request.TransformSize, transformClass);
            int row = (entropySize * 2 + planeType)
                * Av1SyntaxConstants.SignificantCoefficientContexts + context;
            int baseLevel = request.Reader.ReadSymbol(CoefficientBaseTable, row);
            if (!ValidSymbol(request.Reader, baseLevel, 4, result))
            {
                return result;
            }
            if (baseLevel > Av1SyntaxConstants.QuantizerBaseLevelCount)
            {
                baseLevel = DecodeBaseRange(
                    request, result, levels, position, baseLevel,
                    entropySize, planeType, transformClass, false);
            }
            levels[position] = baseLevel;
            scanIndex--;
        }

        if (result.Code != AvifResultCode.Success)
        {
            return result;
        }
        ApplySignsAndDequantize(request, result, levels, scan, eob);
        return result;
    }

    static int DecodeEndOfBlock(
        Av1CoefficientDecodeRequest request,
        Av1CoefficientDecodeResult result,
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

        string tableName = GetEndOfBlockTableName(multiSize);
        Av1TransformClass transformClass =
            Av1ConversionTables.GetTransformClass(request.TransformType);
        int row = multiSize < 5
            ? planeType * 2 + (transformClass == Av1TransformClass.TwoDimensional ? 0 : 1)
            : planeType;
        int symbolCount = multiSize + 5;
        int tokenSymbol = (request.Reader ?? throw new InvalidOperationException("Invalid internal state.")).ReadSymbol(tableName, row);
        if (!ValidSymbol(request.Reader, tokenSymbol, symbolCount, result))
        {
            return 0;
        }

        int token = tokenSymbol + 1;
        int extra = 0;
        int bitCount = endOfBlockOffsetBits[token];
        if (bitCount > 0)
        {
            int extraRow = (entropySize * 2 + planeType) * 9 + token - 3;
            int leading = request.Reader.ReadSymbol(EndOfBlockExtraTable, extraRow);
            if (!ValidSymbol(request.Reader, leading, 2, result))
            {
                return 0;
            }
            extra = leading << (bitCount - 1);
            int bit = 1;
            while (bit < bitCount)
            {
                extra += request.Reader.ReadBit() << (bitCount - 1 - bit);
                if (request.Reader.Code != AvifResultCode.Success)
                {
                    result.Code = request.Reader.Code;
                    return 0;
                }
                bit++;
            }
        }

        int eob = endOfBlockGroupStarts[token] + extra;
        if (eob <= 0 || eob > coefficientCount)
        {
            result.Code = AvifResultCode.InvalidAv1Bitstream;
            return 0;
        }
        return eob;
    }

    static int DecodeBaseRange(
        Av1CoefficientDecodeRequest request,
        Av1CoefficientDecodeResult result,
        int[] levels,
        int position,
        int level,
        int entropySize,
        int planeType,
        Av1TransformClass transformClass,
        bool endOfBlock)
    {
        int context = endOfBlock
            ? GetEndOfBlockRangeContext(position, request.TransformSize, transformClass)
            : GetBaseRangeContext(levels, position, request.TransformSize, transformClass);
        int rangeSize = entropySize > 3 ? 3 : entropySize;
        int row = (rangeSize * 2 + planeType)
            * Av1SyntaxConstants.LevelContexts + context;
        int index = 0;
        while (index < Av1SyntaxConstants.CoefficientBaseRange)
        {
            int value = (request.Reader ?? throw new InvalidOperationException("Invalid internal state.")).ReadSymbol(CoefficientBaseRangeTable, row);
            if (!ValidSymbol(request.Reader, value,
                Av1SyntaxConstants.CoefficientBaseRangeCdfSize, result))
            {
                return level;
            }
            level += value;
            if (value < Av1SyntaxConstants.CoefficientBaseRangeCdfSize - 1)
            {
                break;
            }
            index += Av1SyntaxConstants.CoefficientBaseRangeCdfSize - 1;
        }
        return level;
    }

    static void ApplySignsAndDequantize(
        Av1CoefficientDecodeRequest request,
        Av1CoefficientDecodeResult result,
        int[] levels,
        int[] scan,
        int eob)
    {
        int dcQuantizer = Av1QuantizationTables.GetDcQuantizer(
            request.QIndex, request.DcDelta, request.BitDepth);
        int acQuantizer = Av1QuantizationTables.GetAcQuantizer(
            request.QIndex, request.AcDelta, request.BitDepth);
        int shift = GetTransformScale(request.TransformSize);
        int maximum = (1 << (7 + request.BitDepth)) - 1;
        int minimum = -(1 << (7 + request.BitDepth));
        int totalLevel = 0;
        int dcValue = 0;
        int scanIndex = 0;
        while (scanIndex < eob)
        {
            int position = scan[scanIndex];
            int level = levels[position];
            if (level != 0)
            {
                int sign = scanIndex == 0
                    ? (request.Reader ?? throw new InvalidOperationException("Invalid internal state.")).ReadSymbol(
                        DcSignTable, (request.Plane == 0 ? 0 : 1) * 3
                            + request.DcSignContext)
                    : (request.Reader ?? throw new InvalidOperationException("Invalid internal state.")).ReadBit();
                if (!ValidSymbol(request.Reader, sign, 2, result))
                {
                    return;
                }
                if (level >= Av1SyntaxConstants.CoefficientBaseRange
                    + Av1SyntaxConstants.QuantizerBaseLevelCount + 1)
                {
                    int golomb = ReadGolomb(request.Reader, result);
                    if (result.Code != AvifResultCode.Success)
                    {
                        return;
                    }
                    level += golomb;
                }

                int signedLevel = sign == 0 ? level : -level;
                result.QuantizedCoefficients[position] = signedLevel;
                int quantizer = position == 0 ? dcQuantizer : acQuantizer;
                if (request.UseQuantizerMatrix)
                {
                    int[] coordinate = GetCoefficientCoordinate(
                        position, request.TransformSize);
                    int weight = Av1QuantizerMatrices.GetWeight(
                        request.MatrixLevel,
                        request.Plane == 0 ? 0 : 1,
                        request.TransformSize,
                        coordinate[0],
                        coordinate[1]);
                    quantizer = (weight * quantizer + 16) >> 5;
                }

                long dequantized = (long)signedLevel * quantizer;
                if (shift > 0)
                {
                    dequantized >>= shift;
                }
                result.DequantizedCoefficients[position] = ClipLong(
                    minimum, maximum, dequantized);
                totalLevel += level;
                if (totalLevel > 63)
                {
                    totalLevel = 63;
                }
                if (position > result.MaximumScanLine)
                {
                    result.MaximumScanLine = position;
                }
                if (scanIndex == 0)
                {
                    dcValue = signedLevel;
                }
            }
            scanIndex++;
        }

        result.CulLevel = totalLevel;
        if (dcValue < 0)
        {
            result.CulLevel |= 64;
        }
        else if (dcValue > 0)
        {
            result.CulLevel += 128;
        }
    }

    static int ReadGolomb(
        Av1CoefficientSymbolReader reader,
        Av1CoefficientDecodeResult result)
    {
        int length = 0;
        int bit = 0;
        while (bit == 0 && length <= 20)
        {
            bit = reader.ReadBit();
            length++;
            if (reader.Code != AvifResultCode.Success)
            {
                result.Code = reader.Code;
                return 0;
            }
        }
        if (bit == 0 || length > 20)
        {
            result.Code = AvifResultCode.InvalidAv1Bitstream;
            return 0;
        }

        int value = 1;
        int index = 0;
        while (index < length - 1)
        {
            value = (value << 1) + reader.ReadBit();
            if (reader.Code != AvifResultCode.Success)
            {
                result.Code = reader.Code;
                return 0;
            }
            index++;
        }
        return value - 1;
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
            : (transformClass == Av1TransformClass.Vertical ? row == 0 : column == 0);
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

    static int[] GetCoefficientCoordinate(int position, Av1TransformSize transformSize)
    {
        int[] dimensions = GetScanDimensions(transformSize);
        return new int[] { position % dimensions[0], position / dimensions[0] };
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

    static int GetTransformScale(Av1TransformSize transformSize)
    {
        int area = Av1ConversionTables.GetTransformWidth(transformSize)
            * Av1ConversionTables.GetTransformHeight(transformSize);
        if (area > 1024)
        {
            return 2;
        }
        return area > 256 ? 1 : 0;
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

    static int GetCoefficientCount(Av1CoefficientDecodeRequest request)
    {
        return request == null
            ? 0
            : Av1ConversionTables.GetTransformCoefficientCount(request.TransformSize);
    }

    static AvifResultCode Validate(
        Av1CoefficientDecodeRequest request,
        int coefficientCount)
    {
        if (request == null || request.Reader == null
            || request.Reader.Code != AvifResultCode.Success
            || coefficientCount <= 0 || coefficientCount > 1024
            || request.TransformType < Av1TransformType.DctDct
            || request.TransformType > Av1TransformType.HorizontalFlipAdst
            || request.Plane < 0 || request.Plane > 2
            || request.BitDepth != 8 && request.BitDepth != 10 && request.BitDepth != 12
            || request.QIndex < 0 || request.QIndex > 255
            || request.DcDelta < -255 || request.DcDelta > 255
            || request.AcDelta < -255 || request.AcDelta > 255
            || request.TransformBlockSkipContext < 0
            || request.TransformBlockSkipContext
                >= Av1SyntaxConstants.TransformBlockSkipContexts
            || request.DcSignContext < 0
            || request.DcSignContext >= Av1SyntaxConstants.DcSignContexts
            || request.UseQuantizerMatrix
                && (request.MatrixLevel < 0
                    || request.MatrixLevel
                        >= Av1SyntaxConstants.NumQuantizerMatrixLevels)
            || request.Lossless
                && (request.QIndex != 0 || request.DcDelta != 0
                    || request.AcDelta != 0 || request.UseQuantizerMatrix))
        {
            return AvifResultCode.InvalidArgument;
        }
        return AvifResultCode.Success;
    }

    static bool ValidSymbol(
        Av1CoefficientSymbolReader reader,
        int symbol,
        int symbolCount,
        Av1CoefficientDecodeResult result)
    {
        if (reader.Code != AvifResultCode.Success)
        {
            result.Code = reader.Code;
            return false;
        }
        if (symbol < 0 || symbol >= symbolCount)
        {
            result.Code = AvifResultCode.InvalidAv1Bitstream;
            return false;
        }
        return true;
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

    static int ClipLong(int minimum, int maximum, long value)
    {
        if (value < minimum)
        {
            return minimum;
        }
        return value > maximum ? maximum : (int)value;
    }
}
