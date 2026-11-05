using System;

namespace webp;

internal static class WebpVp8LColorIndexingTransformDecoder
{
    internal static WebpVp8LColorIndexingTransformParseResult Parse(WebpVp8LBitReader reader, int imageWidth, int imageHeight, Func<WebpVp8LBitReader, int, int, WebpPixelBuffer> colorTableDecoder)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(colorTableDecoder);
        ValidateDimensions(imageWidth, imageHeight);

        WebpVp8LColorIndexingTransformParseResult result = new WebpVp8LColorIndexingTransformParseResult();
        WebpVp8LColorIndexingTransform transform = result.Transform;
        transform.ColorTableSize = (int)reader.ReadBits(8) + 1;
        transform.WidthBits = ComputeWidthBits(transform.ColorTableSize);
        transform.PackedWidth = DivRoundUp(imageWidth, 1 << transform.WidthBits);
        transform.Height = imageHeight;

        WebpPixelBuffer colorTableImage = colorTableDecoder(reader, transform.ColorTableSize, 1);
        ArgumentNullException.ThrowIfNull(colorTableImage);
        if (colorTableImage.Width == 0 && colorTableImage.Height == 0 && colorTableImage.Rgba.Length == 0)
        {
            AddDiagnostic(result, "VP8L color table decode exceeded the recursion depth limit");
            Finish(result);
            return result;
        }

        WebpPixelBufferHelper.ValidateRgbaBuffer(colorTableImage);
        ValidateColorTableImageDimensions(colorTableImage, transform.ColorTableSize);
        transform.ColorTable = DecodePalette(ReadPaletteDeltas(colorTableImage, transform.ColorTableSize));
        Finish(result);
        return result;
    }

    internal static WebpVp8LColor[] ApplyInverse(WebpVp8LColor[] transformedPixels, int width, int height, WebpVp8LColorIndexingTransform transform)
    {
        ArgumentNullException.ThrowIfNull(transformedPixels);
        ArgumentNullException.ThrowIfNull(transform);
        ValidateDimensions(width, height);
        ValidateTransform(transform, width, height);
        ValidatePixels(transformedPixels, transform.PackedWidth, height);

        WebpVp8LColor[] output = new WebpVp8LColor[width * height];
        int y = 0;
        while (y < height)
        {
            int x = 0;
            while (x < width)
            {
                int packedX = x >> transform.WidthBits;
                int packedIndex = y * transform.PackedWidth + packedX;
                int index = ExtractPackedIndex(transformedPixels[packedIndex].Green, x, transform.WidthBits);
                output[y * width + x] = GetColorTableEntry(transform, index);
                x = x + 1;
            }

            y = y + 1;
        }

        return output;
    }

    internal static WebpVp8LColor[] DecodePalette(WebpVp8LColor[] deltas)
    {
        ArgumentNullException.ThrowIfNull(deltas);

        WebpVp8LColor[] palette = new WebpVp8LColor[deltas.Length];
        WebpVp8LColor previous = new WebpVp8LColor();
        int index = 0;
        while (index < deltas.Length)
        {
            WebpVp8LColor delta = deltas[index];
            ArgumentNullException.ThrowIfNull(delta);
            WebpVp8LColor color = new WebpVp8LColor();
            color.Red = (byte)((previous.Red + delta.Red) & 255);
            color.Green = (byte)((previous.Green + delta.Green) & 255);
            color.Blue = (byte)((previous.Blue + delta.Blue) & 255);
            color.Alpha = (byte)((previous.Alpha + delta.Alpha) & 255);
            palette[index] = color;
            previous = color;
            index = index + 1;
        }

        return palette;
    }

    internal static int ComputeWidthBits(int colorTableSize)
    {
        ValidateColorTableSize(colorTableSize);
        if (colorTableSize <= 2)
        {
            return 3;
        }

        if (colorTableSize <= 4)
        {
            return 2;
        }

        if (colorTableSize <= 16)
        {
            return 1;
        }

        return 0;
    }

    internal static int ExtractPackedIndex(byte packedGreen, int x, int widthBits)
    {
        ValidateWidthBits(widthBits);
        if (widthBits == 0)
        {
            return packedGreen;
        }

        int bitsPerPixel = 8 >> widthBits;
        int pixelsPerPackedPixel = 1 << widthBits;
        int position = x & (pixelsPerPackedPixel - 1);
        int shift = position * bitsPerPixel;
        int mask = (1 << bitsPerPixel) - 1;
        return (packedGreen >> shift) & mask;
    }

    internal static WebpVp8LColor[] ReadPaletteDeltas(WebpPixelBuffer colorTableImage, int colorTableSize)
    {
        WebpVp8LColor[] deltas = new WebpVp8LColor[colorTableSize];
        int index = 0;
        while (index < colorTableSize)
        {
            int pixel = index * 4;
            WebpVp8LColor color = new WebpVp8LColor();
            color.Red = colorTableImage.Rgba[pixel];
            color.Green = colorTableImage.Rgba[pixel + 1];
            color.Blue = colorTableImage.Rgba[pixel + 2];
            color.Alpha = colorTableImage.Rgba[pixel + 3];
            deltas[index] = color;
            index = index + 1;
        }

        return deltas;
    }

    internal static WebpVp8LColor GetColorTableEntry(WebpVp8LColorIndexingTransform transform, int index)
    {
        if (index >= transform.ColorTableSize)
        {
            return new WebpVp8LColor();
        }

        return transform.ColorTable[index].Copy();
    }

    internal static int DivRoundUp(int value, int divisor)
    {
        return (value + divisor - 1) / divisor;
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

    internal static void ValidateColorTableSize(int colorTableSize)
    {
        if (colorTableSize <= 0)
        {
            throw new ArgumentOutOfRangeException("colorTableSize");
        }

        if (colorTableSize > 256)
        {
            throw new ArgumentOutOfRangeException("colorTableSize");
        }
    }

    internal static void ValidateWidthBits(int widthBits)
    {
        if (widthBits < 0 || widthBits > 3)
        {
            throw new ArgumentOutOfRangeException("widthBits");
        }
    }

    internal static void ValidateColorTableImageDimensions(WebpPixelBuffer image, int colorTableSize)
    {
        if (image.Width != colorTableSize || image.Height != 1)
        {
            throw new ArgumentException("color indexing image dimensions must match the color table size and height 1", "colorTableImage");
        }
    }

    internal static void ValidatePixels(WebpVp8LColor[] pixels, int width, int height)
    {
        if (pixels.Length != width * height)
        {
            throw new ArgumentException("pixel count must equal packed width * height", "transformedPixels");
        }
    }

    internal static void ValidateTransform(WebpVp8LColorIndexingTransform transform, int width, int height)
    {
        ValidateColorTableSize(transform.ColorTableSize);
        ValidateWidthBits(transform.WidthBits);
        int packedWidth = DivRoundUp(width, 1 << transform.WidthBits);
        if (transform.PackedWidth != packedWidth || transform.Height != height)
        {
            throw new ArgumentException("color indexing transform dimensions do not match the image dimensions", "transform");
        }

        if (transform.ColorTable.Length != transform.ColorTableSize)
        {
            throw new ArgumentException("color table length does not match the color table size", "transform");
        }
    }

    internal static void Finish(WebpVp8LColorIndexingTransformParseResult result)
    {
        result.IsValid = !result.HasErrors();
    }

    internal static void AddDiagnostic(WebpVp8LColorIndexingTransformParseResult result, string message)
    {
        result.Diagnostics.Add(new WebpDiagnostic(WebpDiagnosticSeverity.Error, -1, WebpConstants.FourCcVp8LString, message));
    }
}
