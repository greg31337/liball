using System;

namespace webp;

internal static class WebpVp8LColorTransformDecoder
{
    internal static WebpVp8LColorTransformParseResult Parse(WebpVp8LBitReader reader, int imageWidth, int imageHeight, Func<WebpVp8LBitReader, int, int, WebpPixelBuffer> colorTransformImageDecoder)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(colorTransformImageDecoder);
        ValidateDimensions(imageWidth, imageHeight);

        WebpVp8LColorTransformParseResult result = new WebpVp8LColorTransformParseResult();
        WebpVp8LColorTransform transform = result.Transform;
        transform.SizeBits = (int)reader.ReadBits(3) + 2;
        transform.Width = DivRoundUp(imageWidth, 1 << transform.SizeBits);
        transform.Height = DivRoundUp(imageHeight, 1 << transform.SizeBits);

        WebpPixelBuffer transformImage = colorTransformImageDecoder(reader, transform.Width, transform.Height);
        ArgumentNullException.ThrowIfNull(transformImage);
        if (transformImage.Width == 0 && transformImage.Height == 0 && transformImage.Rgba.Length == 0)
        {
            AddDiagnostic(result, "VP8L color transform image decode exceeded the recursion depth limit");
            Finish(result);
            return result;
        }

        WebpPixelBufferHelper.ValidateRgbaBuffer(transformImage);
        ValidateTransformImageDimensions(transformImage, transform.Width, transform.Height);
        ExtractElements(result, transformImage);
        Finish(result);
        return result;
    }

    internal static WebpVp8LColor[] ApplyInverse(WebpVp8LColor[] transformedPixels, int width, int height, WebpVp8LColorTransform transform)
    {
        ArgumentNullException.ThrowIfNull(transformedPixels);
        ArgumentNullException.ThrowIfNull(transform);
        ValidateDimensions(width, height);
        ValidatePixels(transformedPixels, width, height);
        ValidateTransform(transform, width, height);

        WebpVp8LColor[] output = new WebpVp8LColor[transformedPixels.Length];
        int index = 0;
        while (index < transformedPixels.Length)
        {
            int x = index % width;
            int y = index / width;
            WebpVp8LColorTransformElement element = GetElement(transform, x, y);
            output[index] = ApplyInverseColor(transformedPixels[index], element);
            index = index + 1;
        }

        return output;
    }

    internal static WebpVp8LColor ApplyInverseColor(WebpVp8LColor transformed, WebpVp8LColorTransformElement element)
    {
        ArgumentNullException.ThrowIfNull(transformed);
        ArgumentNullException.ThrowIfNull(element);

        int red = transformed.Red;
        int blue = transformed.Blue;
        red = red + ColorTransformDelta(element.GreenToRed, transformed.Green);
        blue = blue + ColorTransformDelta(element.GreenToBlue, transformed.Green);
        blue = blue + ColorTransformDelta(element.RedToBlue, (byte)(red & 255));

        WebpVp8LColor color = new WebpVp8LColor();
        color.Red = (byte)(red & 255);
        color.Green = transformed.Green;
        color.Blue = (byte)(blue & 255);
        color.Alpha = transformed.Alpha;
        return color;
    }

    internal static int ColorTransformDelta(byte transform, byte channel)
    {
        int signedTransform = ToSignedByte(transform);
        int signedChannel = ToSignedByte(channel);
        return (signedTransform * signedChannel) >> 5;
    }

    internal static WebpVp8LColorTransformElement GetElement(WebpVp8LColorTransform transform, int x, int y)
    {
        int blockX = x >> transform.SizeBits;
        int blockY = y >> transform.SizeBits;
        return transform.Elements[blockY * transform.Width + blockX];
    }

    internal static void ExtractElements(WebpVp8LColorTransformParseResult result, WebpPixelBuffer transformImage)
    {
        WebpVp8LColorTransform transform = result.Transform;
        transform.Elements = new WebpVp8LColorTransformElement[transformImage.Width * transformImage.Height];
        int row = 0;
        while (row < transformImage.Height)
        {
            int rowOffset = row * transformImage.Stride;
            int column = 0;
            while (column < transformImage.Width)
            {
                int pixel = rowOffset + column * 4;
                int index = row * transformImage.Width + column;
                WebpVp8LColorTransformElement element = new WebpVp8LColorTransformElement();
                element.RedToBlue = transformImage.Rgba[pixel];
                element.GreenToBlue = transformImage.Rgba[pixel + 1];
                element.GreenToRed = transformImage.Rgba[pixel + 2];
                if (transformImage.Rgba[pixel + 3] != 255)
                {
                    AddDiagnostic(result, "VP8L color transform image alpha must be 255");
                    return;
                }

                transform.Elements[index] = element;
                column = column + 1;
            }

            row = row + 1;
        }
    }

    internal static int ToSignedByte(byte value)
    {
        if (value >= 128)
        {
            return value - 256;
        }

        return value;
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

    internal static void ValidateTransformImageDimensions(WebpPixelBuffer image, int width, int height)
    {
        if (image.Width != width || image.Height != height)
        {
            throw new ArgumentException("color transform image dimensions must match derived transform dimensions", "colorTransformImage");
        }
    }

    internal static void ValidatePixels(WebpVp8LColor[] pixels, int width, int height)
    {
        if (pixels.Length != width * height)
        {
            throw new ArgumentException("pixel count must equal width * height", "transformedPixels");
        }
    }

    internal static void ValidateTransform(WebpVp8LColorTransform transform, int width, int height)
    {
        int expectedWidth = DivRoundUp(width, 1 << transform.SizeBits);
        int expectedHeight = DivRoundUp(height, 1 << transform.SizeBits);
        if (transform.Width != expectedWidth || transform.Height != expectedHeight)
        {
            throw new ArgumentException("color transform dimensions do not match the image dimensions", "transform");
        }

        if (transform.Elements.Length != transform.Width * transform.Height)
        {
            throw new ArgumentException("color transform element count does not match transform dimensions", "transform");
        }
    }

    internal static void AddDiagnostic(WebpVp8LColorTransformParseResult result, string message)
    {
        result.Diagnostics.Add(new WebpDiagnostic(WebpDiagnosticSeverity.Error, -1, WebpConstants.FourCcVp8LString, message));
    }

    internal static void Finish(WebpVp8LColorTransformParseResult result)
    {
        result.IsValid = !result.HasErrors();
    }
}
