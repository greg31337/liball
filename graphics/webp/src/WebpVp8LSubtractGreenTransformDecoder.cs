using System;

namespace webp;

internal static class WebpVp8LSubtractGreenTransformDecoder
{
    internal static WebpVp8LColor[] ApplyInverse(WebpVp8LColor[] transformedPixels, int width, int height)
    {
        ArgumentNullException.ThrowIfNull(transformedPixels);
        ValidateDimensions(width, height);
        ValidatePixels(transformedPixels, width, height);

        WebpVp8LColor[] output = new WebpVp8LColor[transformedPixels.Length];
        int index = 0;
        while (index < transformedPixels.Length)
        {
            output[index] = ApplyInverseColor(transformedPixels[index]);
            index = index + 1;
        }

        return output;
    }

    internal static WebpVp8LColor ApplyInverseColor(WebpVp8LColor transformed)
    {
        ArgumentNullException.ThrowIfNull(transformed);

        WebpVp8LColor color = new WebpVp8LColor();
        color.Red = (byte)((transformed.Red + transformed.Green) & 255);
        color.Green = transformed.Green;
        color.Blue = (byte)((transformed.Blue + transformed.Green) & 255);
        color.Alpha = transformed.Alpha;
        return color;
    }

    internal static void ValidateDimensions(int width, int height)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException("width");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException("height");
        }
    }

    internal static void ValidatePixels(WebpVp8LColor[] pixels, int width, int height)
    {
        if (pixels.Length != width * height)
        {
            throw new ArgumentException("pixel count must equal width * height", "transformedPixels");
        }
    }
}
