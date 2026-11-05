using System;

namespace avif;

internal class Av1CoefficientQuantizationRequest
{
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
    internal int[] Coefficients;

    internal Av1CoefficientQuantizationRequest()
    {
        TransformSize = Av1TransformSize.Transform4x4;
        TransformType = Av1TransformType.DctDct;
        BitDepth = 8;
        MatrixLevel = Av1SyntaxConstants.NumQuantizerMatrixLevels - 1;
        Coefficients = System.Array.Empty<int>();
    }
}

internal class Av1CoefficientQuantizationResult
{
    internal AvifResultCode Code;
    internal int[] QuantizedCoefficients;
    internal int[] DequantizedCoefficients;
    internal int EndOfBlock;

    internal Av1CoefficientQuantizationResult(
        AvifResultCode code,
        int coefficientCount)
    {
        Code = code;
        QuantizedCoefficients = code == AvifResultCode.Success
            ? new int[coefficientCount]
            : System.Array.Empty<int>();
        DequantizedCoefficients = code == AvifResultCode.Success
            ? new int[coefficientCount]
            : System.Array.Empty<int>();
    }
}

internal static class Av1CoefficientQuantizer
{
    internal static Av1CoefficientQuantizationResult Quantize(
        Av1CoefficientQuantizationRequest request)
    {
        Av1TransformType transformType = request == null
            ? Av1TransformType.DctDct
            : request.TransformType;
        return Quantize(request, transformType);
    }

    internal static Av1CoefficientQuantizationResult Quantize(
        Av1CoefficientQuantizationRequest? request,
        Av1TransformType transformType)
    {
        int count = request == null ? 0 : request.Coefficients.Length;
        AvifResultCode validation = Validate(request, count, transformType);
        Av1CoefficientQuantizationResult result =
            new Av1CoefficientQuantizationResult(validation, count);
        if (validation != AvifResultCode.Success)
        {
            return result;
        }

        int dcQuantizer = Av1QuantizationTables.GetDcQuantizer(
            (request ?? throw new InvalidOperationException("Invalid internal state.")).QIndex, request.DcDelta, request.BitDepth);
        int acQuantizer = Av1QuantizationTables.GetAcQuantizer(
            request.QIndex, request.AcDelta, request.BitDepth);
        int shift = GetTransformScale(request.TransformSize);
        int minimum = -(1 << (7 + request.BitDepth));
        int maximum = (1 << (7 + request.BitDepth)) - 1;
        int index = 0;
        while (index < count)
        {
            int quantizer = index == 0 ? dcQuantizer : acQuantizer;
            if (request.UseQuantizerMatrix)
            {
                int[] coordinate = GetCoordinate(index, request.TransformSize);
                int weight = Av1QuantizerMatrices.GetWeight(
                    request.MatrixLevel,
                    request.Plane == 0 ? 0 : 1,
                    request.TransformSize,
                    coordinate[0],
                    coordinate[1]);
                quantizer = (weight * quantizer + 16) >> 5;
            }

            long scaled = (long)request.Coefficients[index] << shift;
            int level = DivideRounded(scaled, quantizer);
            result.QuantizedCoefficients[index] = level;
            long dequantized = (long)level * quantizer;
            if (shift > 0)
            {
                dequantized >>= shift;
            }
            result.DequantizedCoefficients[index] = Clip(
                minimum, maximum, dequantized);
            index++;
        }

        int[] scan = Av1ScanTables.GetScan(request.TransformSize, transformType);
        index = scan.Length - 1;
        while (index >= 0)
        {
            if (result.QuantizedCoefficients[scan[index]] != 0)
            {
                result.EndOfBlock = index + 1;
                break;
            }
            index--;
        }
        return result;
    }

    static AvifResultCode Validate(
        Av1CoefficientQuantizationRequest? request,
        int count,
        Av1TransformType transformType)
    {
        if (request == null || request.Coefficients == null
            || count == 0
            || count != Av1ConversionTables.GetTransformCoefficientCount(
                request.TransformSize)
            || transformType < Av1TransformType.DctDct
            || transformType > Av1TransformType.HorizontalFlipAdst
            || request.Plane < 0 || request.Plane > 2
            || request.BitDepth != 8 && request.BitDepth != 10
                && request.BitDepth != 12
            || request.QIndex < 0 || request.QIndex > 255
            || request.DcDelta < -255 || request.DcDelta > 255
            || request.AcDelta < -255 || request.AcDelta > 255
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

    static int DivideRounded(long value, int divisor)
    {
        if (divisor <= 0)
        {
            return 0;
        }
        long magnitude = value < 0 ? -value : value;
        long quotient = (magnitude + divisor / 2) / divisor;
        if (quotient > int.MaxValue)
        {
            quotient = int.MaxValue;
        }
        return value < 0 ? -(int)quotient : (int)quotient;
    }

    static int[] GetCoordinate(int position, Av1TransformSize transformSize)
    {
        Av1TransformSize adjusted =
            Av1ConversionTables.GetAdjustedTransformSize(transformSize);
        int height = Av1ConversionTables.GetTransformHeight(adjusted);
        return new int[] { position % height, position / height };
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

    static int Clip(int minimum, int maximum, long value)
    {
        if (value < minimum)
        {
            return minimum;
        }
        return value > maximum ? maximum : (int)value;
    }
}
