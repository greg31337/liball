using System;

namespace webp;

internal static class WebpNearLosslessPreprocessor
{
    internal static WebpPixelBuffer Apply(WebpPixelBuffer source, int quality, bool exactTransparentRgb)
    {
        ArgumentNullException.ThrowIfNull(source);
        WebpPixelBufferHelper.ValidateRgbaBuffer(source);
        ValidateQuality(quality);

        WebpPixelBuffer output = WebpPixelBufferHelper.Copy(source);
        if (quality == 100)
        {
            return output;
        }

        int step = ComputeQuantizationStep(quality);
        int row = 0;
        while (row < output.Height)
        {
            int rowOffset = row * output.Stride;
            int column = 0;
            while (column < output.Width)
            {
                int pixel = rowOffset + column * 4;
                if (!exactTransparentRgb || output.Rgba[pixel + 3] != 0)
                {
                    output.Rgba[pixel] = QuantizeByte(output.Rgba[pixel], step);
                    output.Rgba[pixel + 1] = QuantizeByte(output.Rgba[pixel + 1], step);
                    output.Rgba[pixel + 2] = QuantizeByte(output.Rgba[pixel + 2], step);
                }

                column = column + 1;
            }

            row = row + 1;
        }

        return output;
    }

    internal static int ComputeQuantizationStep(int quality)
    {
        ValidateQuality(quality);
        if (quality == 100)
        {
            return 1;
        }

        return 1 + ((100 - quality) * 15) / 100;
    }

    internal static byte QuantizeByte(byte value, int step)
    {
        int quantized = ((value + step / 2) / step) * step;
        if (quantized > 255)
        {
            return 255;
        }

        return (byte)quantized;
    }

    internal static void ValidateQuality(int quality)
    {
        if (quality < 0 || quality > 100)
        {
            throw new ArgumentOutOfRangeException("quality");
        }
    }
}
