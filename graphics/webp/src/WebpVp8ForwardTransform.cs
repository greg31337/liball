using System;

namespace webp;

internal static class WebpVp8ForwardTransform
{
    internal static int[] ForwardDct4X4(byte[] source, int sourceOffset, int sourceStride, byte[] prediction, int predictionOffset, int predictionStride)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(prediction);
        ValidatePlaneAccess(source, sourceOffset, sourceStride, "source");
        ValidatePlaneAccess(prediction, predictionOffset, predictionStride, "prediction");

        int[] temp = new int[16];
        int row = 0;
        while (row < 4)
        {
            int sourceIndex = sourceOffset + (row * sourceStride);
            int predictionIndex = predictionOffset + (row * predictionStride);
            int d0 = source[sourceIndex] - prediction[predictionIndex];
            int d1 = source[sourceIndex + 1] - prediction[predictionIndex + 1];
            int d2 = source[sourceIndex + 2] - prediction[predictionIndex + 2];
            int d3 = source[sourceIndex + 3] - prediction[predictionIndex + 3];
            int a0 = d0 + d3;
            int a1 = d1 + d2;
            int a2 = d1 - d2;
            int a3 = d0 - d3;

            temp[(row * 4)] = (a0 + a1) * 8;
            temp[(row * 4) + 1] = ((a2 * 2217) + (a3 * 5352) + 1812) >> 9;
            temp[(row * 4) + 2] = (a0 - a1) * 8;
            temp[(row * 4) + 3] = ((a3 * 2217) - (a2 * 5352) + 937) >> 9;
            row = row + 1;
        }

        int[] output = new int[16];
        int column = 0;
        while (column < 4)
        {
            int a0 = temp[column] + temp[12 + column];
            int a1 = temp[4 + column] + temp[8 + column];
            int a2 = temp[4 + column] - temp[8 + column];
            int a3 = temp[column] - temp[12 + column];

            output[column] = (a0 + a1 + 7) >> 4;
            output[4 + column] = ((a2 * 2217) + (a3 * 5352) + 12000) >> 16;
            if (a3 != 0)
            {
                output[4 + column] = output[4 + column] + 1;
            }

            output[8 + column] = (a0 - a1 + 7) >> 4;
            output[12 + column] = ((a3 * 2217) - (a2 * 5352) + 51000) >> 16;
            column = column + 1;
        }

        return output;
    }

    internal static int[] ForwardWalsh4X4(int[] coefficients)
    {
        ArgumentNullException.ThrowIfNull(coefficients);
        ValidateBlock(coefficients, "coefficients");

        int[] temp = new int[16];
        int row = 0;
        while (row < 4)
        {
            int offset = row * 4;
            int a0 = coefficients[offset] + coefficients[offset + 2];
            int a1 = coefficients[offset + 1] + coefficients[offset + 3];
            int a2 = coefficients[offset + 1] - coefficients[offset + 3];
            int a3 = coefficients[offset] - coefficients[offset + 2];

            temp[offset] = a0 + a1;
            temp[offset + 1] = a3 + a2;
            temp[offset + 2] = a3 - a2;
            temp[offset + 3] = a0 - a1;
            row = row + 1;
        }

        int[] output = new int[16];
        int column = 0;
        while (column < 4)
        {
            int a0 = temp[column] + temp[8 + column];
            int a1 = temp[4 + column] + temp[12 + column];
            int a2 = temp[4 + column] - temp[12 + column];
            int a3 = temp[column] - temp[8 + column];

            output[column] = (a0 + a1) >> 1;
            output[4 + column] = (a3 + a2) >> 1;
            output[8 + column] = (a3 - a2) >> 1;
            output[12 + column] = (a0 - a1) >> 1;
            column = column + 1;
        }

        return output;
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
