using System;

namespace webp;

internal static class WebpVp8InverseTransform
{
    internal const int CosPi8Sqrt2Minus1 = 20091;
    internal const int SinPi8Sqrt2 = 35468;

    internal static int[] InverseDct4X4(int[] coefficients)
    {
        ArgumentNullException.ThrowIfNull(coefficients);
        ValidateBlock(coefficients, "coefficients");

        int[] temp = new int[16];
        int column = 0;
        while (column < 4)
        {
            int a1 = coefficients[column] + coefficients[8 + column];
            int b1 = coefficients[column] - coefficients[8 + column];
            int temp1 = (coefficients[4 + column] * SinPi8Sqrt2) >> 16;
            int temp2 = coefficients[12 + column] + ((coefficients[12 + column] * CosPi8Sqrt2Minus1) >> 16);
            int c1 = temp1 - temp2;
            temp1 = coefficients[4 + column] + ((coefficients[4 + column] * CosPi8Sqrt2Minus1) >> 16);
            temp2 = (coefficients[12 + column] * SinPi8Sqrt2) >> 16;
            int d1 = temp1 + temp2;

            temp[column] = a1 + d1;
            temp[12 + column] = a1 - d1;
            temp[4 + column] = b1 + c1;
            temp[8 + column] = b1 - c1;
            column = column + 1;
        }

        int[] output = new int[16];
        int row = 0;
        while (row < 4)
        {
            int offset = row * 4;
            int a1 = temp[offset] + temp[offset + 2];
            int b1 = temp[offset] - temp[offset + 2];
            int temp1 = (temp[offset + 1] * SinPi8Sqrt2) >> 16;
            int temp2 = temp[offset + 3] + ((temp[offset + 3] * CosPi8Sqrt2Minus1) >> 16);
            int c1 = temp1 - temp2;
            temp1 = temp[offset + 1] + ((temp[offset + 1] * CosPi8Sqrt2Minus1) >> 16);
            temp2 = (temp[offset + 3] * SinPi8Sqrt2) >> 16;
            int d1 = temp1 + temp2;

            output[offset] = (a1 + d1 + 4) >> 3;
            output[offset + 3] = (a1 - d1 + 4) >> 3;
            output[offset + 1] = (b1 + c1 + 4) >> 3;
            output[offset + 2] = (b1 - c1 + 4) >> 3;
            row = row + 1;
        }

        return output;
    }

    internal static int[] InverseDct4X4DcOnly(int dcCoefficient)
    {
        int value = (dcCoefficient + 4) >> 3;
        int[] output = new int[16];
        int index = 0;
        while (index < output.Length)
        {
            output[index] = value;
            index = index + 1;
        }

        return output;
    }

    internal static int[] InverseWalsh4X4(int[] coefficients)
    {
        ArgumentNullException.ThrowIfNull(coefficients);
        ValidateBlock(coefficients, "coefficients");

        int[] temp = new int[16];
        int column = 0;
        while (column < 4)
        {
            int a1 = coefficients[column] + coefficients[12 + column];
            int b1 = coefficients[4 + column] + coefficients[8 + column];
            int c1 = coefficients[4 + column] - coefficients[8 + column];
            int d1 = coefficients[column] - coefficients[12 + column];

            temp[column] = a1 + b1;
            temp[4 + column] = c1 + d1;
            temp[8 + column] = a1 - b1;
            temp[12 + column] = d1 - c1;
            column = column + 1;
        }

        int[] output = new int[16];
        int row = 0;
        while (row < 4)
        {
            int offset = row * 4;
            int a1 = temp[offset] + temp[offset + 3];
            int b1 = temp[offset + 1] + temp[offset + 2];
            int c1 = temp[offset + 1] - temp[offset + 2];
            int d1 = temp[offset] - temp[offset + 3];

            int a2 = a1 + b1;
            int b2 = c1 + d1;
            int c2 = a1 - b1;
            int d2 = d1 - c1;

            output[offset] = (a2 + 3) >> 3;
            output[offset + 1] = (b2 + 3) >> 3;
            output[offset + 2] = (c2 + 3) >> 3;
            output[offset + 3] = (d2 + 3) >> 3;
            row = row + 1;
        }

        return output;
    }

    internal static int[] InverseWalsh4X4DcOnly(int dcCoefficient)
    {
        int value = (dcCoefficient + 3) >> 3;
        int[] output = new int[16];
        int index = 0;
        while (index < output.Length)
        {
            output[index] = value;
            index = index + 1;
        }

        return output;
    }

    internal static void AddResidualToBlock(byte[] destination, int destinationOffset, int destinationStride, byte[] prediction, int predictionOffset, int predictionStride, int[] residual)
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(prediction);
        ArgumentNullException.ThrowIfNull(residual);
        ValidateBlock(residual, "residual");
        ValidatePlaneAccess(destination, destinationOffset, destinationStride, "destination");
        ValidatePlaneAccess(prediction, predictionOffset, predictionStride, "prediction");

        int row = 0;
        while (row < 4)
        {
            int column = 0;
            while (column < 4)
            {
                int sourceIndex = predictionOffset + (row * predictionStride) + column;
                int destinationIndex = destinationOffset + (row * destinationStride) + column;
                int residualIndex = (row * 4) + column;
                destination[destinationIndex] = ClampToByte(prediction[sourceIndex] + residual[residualIndex]);
                column = column + 1;
            }

            row = row + 1;
        }
    }

    internal static byte ClampToByte(int value)
    {
        if (value < 0)
        {
            return 0;
        }

        if (value > 255)
        {
            return 255;
        }

        return (byte)value;
    }

    internal static void ValidateBlock(int[] block, string name)
    {
        if (block.Length != 16)
        {
            throw new ArgumentException("VP8 transform blocks must contain 16 values", name);
        }
    }

    internal static void ValidatePlaneAccess(byte[] plane, int offset, int stride, string name)
    {
        if (offset < 0)
        {
            throw new ArgumentOutOfRangeException(name + "Offset");
        }

        if (stride < 4)
        {
            throw new ArgumentOutOfRangeException(name + "Stride");
        }

        int lastIndex = offset + (3 * stride) + 3;
        if (lastIndex >= plane.Length)
        {
            throw new ArgumentException("VP8 4x4 block exceeds " + name + " plane bounds", name);
        }
    }
}
