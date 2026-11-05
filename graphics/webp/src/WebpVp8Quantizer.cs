using System;

namespace webp;

internal static class WebpVp8Quantizer
{
    internal static int[] QuantizeBlock(int[] coefficients, int quantizationStep)
    {
        ArgumentNullException.ThrowIfNull(coefficients);
        ValidateBlock(coefficients, "coefficients");
        ValidateQuantizationStep(quantizationStep);

        int[] output = new int[16];
        int index = 0;
        while (index < 16)
        {
            output[index] = QuantizeValue(coefficients[index], quantizationStep);
            index = index + 1;
        }

        return output;
    }

    internal static int[] DequantizeBlock(int[] quantizedCoefficients, int quantizationStep)
    {
        ArgumentNullException.ThrowIfNull(quantizedCoefficients);
        ValidateBlock(quantizedCoefficients, "quantizedCoefficients");
        ValidateQuantizationStep(quantizationStep);

        int[] output = new int[16];
        int index = 0;
        while (index < 16)
        {
            output[index] = quantizedCoefficients[index] * quantizationStep;
            index = index + 1;
        }

        return output;
    }

    internal static int QuantizeValue(int value, int quantizationStep)
    {
        int sign = 1;
        int absoluteValue = value;
        if (value < 0)
        {
            sign = -1;
            absoluteValue = -value;
        }

        return sign * ((absoluteValue + (quantizationStep >> 1)) / quantizationStep);
    }

    internal static void ValidateBlock(int[] block, string name)
    {
        if (block.Length != 16)
        {
            throw new ArgumentException("VP8 quantizer blocks must contain 16 values", name);
        }
    }

    internal static void ValidateQuantizationStep(int quantizationStep)
    {
        if (quantizationStep <= 0)
        {
            throw new ArgumentOutOfRangeException("quantizationStep");
        }
    }
}
