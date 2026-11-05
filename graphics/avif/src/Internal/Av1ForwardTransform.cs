using System;

namespace avif;

internal class Av1ForwardTransformRequest
{
    internal Av1TransformSize TransformSize;
    internal Av1TransformType TransformType;
    internal int BitDepth;
    internal int[] Residual;
    internal bool Lossless;

    internal Av1ForwardTransformRequest()
    {
        TransformSize = Av1TransformSize.Transform4x4;
        TransformType = Av1TransformType.DctDct;
        BitDepth = 8;
        Residual = System.Array.Empty<int>();
    }
}

internal class Av1ForwardTransformResult
{
    internal AvifResultCode Code;
    internal int[] Coefficients;
    internal int Width;
    internal int Height;

    internal Av1ForwardTransformResult(
        AvifResultCode code,
        int width,
        int height,
        int coefficientCount)
    {
        Code = code;
        Width = width;
        Height = height;
        Coefficients = code == AvifResultCode.Success
            ? new int[coefficientCount]
            : System.Array.Empty<int>();
    }
}

// AV1 specification sections 7.12 and 7.13.
internal static class Av1ForwardTransformer
{
    static readonly int[] rowShifts = new int[]
    {
        0,1,2,2,2,0,0,1,1,1,1,1,1,1,1,2,2,2,2
    };

    internal static Av1ForwardTransformResult Transform(
        Av1ForwardTransformRequest request)
    {
        int width = request == null
            ? 0
            : Av1ConversionTables.GetTransformWidth(request.TransformSize);
        int height = request == null
            ? 0
            : Av1ConversionTables.GetTransformHeight(request.TransformSize);
        int coefficientCount = request == null
            ? 0
            : Av1ConversionTables.GetTransformCoefficientCount(
                request.TransformSize);
        AvifResultCode validation = Validate(
            request, width, height, coefficientCount);
        Av1ForwardTransformResult result = new Av1ForwardTransformResult(
            validation, width, height, coefficientCount);
        if (validation != AvifResultCode.Success)
        {
            return result;
        }

        if ((request ?? throw new InvalidOperationException("Invalid internal state.")).Lossless)
        {
            ForwardLossless(request.Residual, result.Coefficients);
            return result;
        }

        double[] horizontal = new double[width * height];
        Av1Transform1DType horizontalType =
            Av1ConversionTables.GetHorizontalTransformType(request.TransformType);
        Av1Transform1DType verticalType =
            Av1ConversionTables.GetVerticalTransformType(request.TransformType);
        int row = 0;
        while (row < height)
        {
            ForwardOneDimension(
                request.Residual,
                row * width,
                1,
                width,
                horizontal,
                row * width,
                1,
                horizontalType);
            row++;
        }

        double[] transformed = new double[width * height];
        int column = 0;
        while (column < width)
        {
            ForwardOneDimension(
                horizontal,
                column,
                width,
                height,
                transformed,
                column,
                width,
                verticalType);
            column++;
        }

        Av1TransformSize adjusted =
            Av1ConversionTables.GetAdjustedTransformSize(request.TransformSize);
        int coefficientWidth = Av1ConversionTables.GetTransformWidth(adjusted);
        int coefficientHeight = Av1ConversionTables.GetTransformHeight(adjusted);
        double scale = 1 << (rowShifts[(int)request.TransformSize] + 3);
        int widthLog2 = Log2(width);
        int heightLog2 = Log2(height);
        if (widthLog2 - heightLog2 == 1 || heightLog2 - widthLog2 == 1)
        {
            scale *= System.Math.Sqrt(2.0);
        }

        row = 0;
        while (row < coefficientHeight)
        {
            column = 0;
            while (column < coefficientWidth)
            {
                int position = column * coefficientHeight + row;
                double value = transformed[row * width + column] * scale;
                result.Coefficients[position] = Round(value);
                column++;
            }
            row++;
        }

        return result;
    }

    static void ForwardLossless(int[] residual, int[] coefficients)
    {
        int[] intermediate = new int[16];
        int column = 0;
        while (column < 4)
        {
            int[] values = new int[]
            {
                residual[column],
                residual[4 + column],
                residual[8 + column],
                residual[12 + column]
            };
            InvertWalshHadamard(values);
            int row = 0;
            while (row < 4)
            {
                intermediate[row * 4 + column] = values[row];
                row++;
            }
            column++;
        }

        int rowIndex = 0;
        while (rowIndex < 4)
        {
            int[] values = new int[]
            {
                intermediate[rowIndex * 4],
                intermediate[rowIndex * 4 + 1],
                intermediate[rowIndex * 4 + 2],
                intermediate[rowIndex * 4 + 3]
            };
            InvertWalshHadamard(values);
            column = 0;
            while (column < 4)
            {
                coefficients[column * 4 + rowIndex] = values[column] * 4;
                column++;
            }
            rowIndex++;
        }
    }

    static void InvertWalshHadamard(int[] values)
    {
        int output0 = values[0];
        int output1 = values[1];
        int output2 = values[2];
        int output3 = values[3];
        int firstSum = output0 + output1;
        int secondDifference = output3 - output2;
        int middle = (firstSum - secondDifference) >> 1;
        int b = middle - output1;
        int c = middle - output2;
        int a = firstSum - c;
        int d = secondDifference + b;
        values[0] = a;
        values[1] = c;
        values[2] = d;
        values[3] = b;
    }

    static void ForwardOneDimension(
        int[] source,
        int sourceOffset,
        int sourceStride,
        int length,
        double[] destination,
        int destinationOffset,
        int destinationStride,
        Av1Transform1DType type)
    {
        int output = 0;
        while (output < length)
        {
            double sum = 0;
            int input = 0;
            while (input < length)
            {
                sum += source[sourceOffset + input * sourceStride]
                    * GetBasis(type, length, input, output);
                input++;
            }

            destination[destinationOffset + output * destinationStride] = sum;
            output++;
        }
    }

    static void ForwardOneDimension(
        double[] source,
        int sourceOffset,
        int sourceStride,
        int length,
        double[] destination,
        int destinationOffset,
        int destinationStride,
        Av1Transform1DType type)
    {
        int output = 0;
        while (output < length)
        {
            double sum = 0;
            int input = 0;
            while (input < length)
            {
                sum += source[sourceOffset + input * sourceStride]
                    * GetBasis(type, length, input, output);
                input++;
            }

            destination[destinationOffset + output * destinationStride] = sum;
            output++;
        }
    }

    static double GetBasis(
        Av1Transform1DType type,
        int length,
        int input,
        int output)
    {
        if (type == Av1Transform1DType.Identity)
        {
            return input == output ? 1.0 : 0.0;
        }

        if (type == Av1Transform1DType.Dct)
        {
            double normalization = output == 0
                ? System.Math.Sqrt(1.0 / length)
                : System.Math.Sqrt(2.0 / length);
            return normalization * System.Math.Cos(
                System.Math.PI * (2 * input + 1) * output / (2 * length));
        }

        double adstNormalization = System.Math.Sqrt(4.0 / (2 * length + 1));
        return adstNormalization * System.Math.Sin(
            System.Math.PI * (2 * input + 1) * (output + 1)
                / (2 * length + 1));
    }

    static AvifResultCode Validate(
        Av1ForwardTransformRequest? request,
        int width,
        int height,
        int coefficientCount)
    {
        if (request == null || width == 0 || height == 0 || coefficientCount == 0
            || request.Residual == null
            || request.Residual.Length != width * height
            || request.BitDepth != 8 && request.BitDepth != 10
                && request.BitDepth != 12
            || request.TransformType < Av1TransformType.DctDct
            || request.TransformType > Av1TransformType.HorizontalFlipAdst)
        {
            return AvifResultCode.InvalidArgument;
        }

        if (request.Lossless
            && (request.TransformSize != Av1TransformSize.Transform4x4
                || request.TransformType != Av1TransformType.DctDct))
        {
            return AvifResultCode.InvalidArgument;
        }

        Av1InverseTransformRequest inverse = new Av1InverseTransformRequest();
        inverse.TransformSize = request.TransformSize;
        inverse.TransformType = request.TransformType;
        inverse.BitDepth = request.BitDepth;
        inverse.Lossless = request.Lossless;
        inverse.Coefficients = new int[coefficientCount];
        return Av1InverseTransformer.Transform(inverse).Code;
    }

    static int Round(double value)
    {
        if (value <= int.MinValue)
        {
            return int.MinValue;
        }
        if (value >= int.MaxValue)
        {
            return int.MaxValue;
        }
        return value < 0
            ? (int)System.Math.Ceiling(value - 0.5)
            : (int)System.Math.Floor(value + 0.5);
    }

    static int Log2(int value)
    {
        int result = 0;
        while (value > 1)
        {
            value >>= 1;
            result++;
        }
        return result;
    }
}
