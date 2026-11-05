using System;
using System.Collections.Generic;

namespace webp;

internal static class WebpPixelUtilities
{
    internal static byte[] CopyRgbaToBgra(WebpPixelBuffer source)
    {
        ArgumentNullException.ThrowIfNull(source);
        WebpPixelBufferHelper.ValidateRgbaBuffer(source);
        ValidateDefaultOutput(source.Width, source.Height, 1);
        return WebpPixelBufferHelper.ToBgra(source.Rgba, source.Width, source.Height, source.Stride);
    }

    internal static WebpPremultipliedPixelBuffer ConvertStraightToPremultipliedAlpha(WebpPixelBuffer source)
    {
        ArgumentNullException.ThrowIfNull(source);
        WebpPixelBufferHelper.ValidateRgbaBuffer(source);
        ValidateDefaultOutput(source.Rgba.Length);

        WebpPremultipliedPixelBuffer destination = new WebpPremultipliedPixelBuffer();
        destination.Width = source.Width;
        destination.Height = source.Height;
        destination.Stride = source.Stride;
        destination.Rgba = new byte[source.Rgba.Length];
        Array.Copy(source.Rgba, destination.Rgba, source.Rgba.Length);

        int row = 0;
        while (row < source.Height)
        {
            int rowOffset = row * source.Stride;
            int column = 0;
            while (column < source.Width)
            {
                int pixel = rowOffset + column * 4;
                int alpha = source.Rgba[pixel + 3];
                destination.Rgba[pixel] = PremultiplyChannel(source.Rgba[pixel], alpha);
                destination.Rgba[pixel + 1] = PremultiplyChannel(source.Rgba[pixel + 1], alpha);
                destination.Rgba[pixel + 2] = PremultiplyChannel(source.Rgba[pixel + 2], alpha);
                column = column + 1;
            }

            row = row + 1;
        }

        return destination;
    }

    internal static WebpPixelBuffer ConvertPremultipliedToStraightAlpha(WebpPremultipliedPixelBuffer source)
    {
        ArgumentNullException.ThrowIfNull(source);
        WebpPixelBufferHelper.ValidateRgbaBuffer(source.Rgba, source.Width, source.Height, source.Stride);
        ValidateDefaultOutput(source.Rgba.Length);

        WebpPixelBuffer destination = new WebpPixelBuffer();
        destination.Width = source.Width;
        destination.Height = source.Height;
        destination.Stride = source.Stride;
        destination.Rgba = new byte[source.Rgba.Length];
        Array.Copy(source.Rgba, destination.Rgba, source.Rgba.Length);

        int row = 0;
        while (row < source.Height)
        {
            int rowOffset = row * source.Stride;
            int column = 0;
            while (column < source.Width)
            {
                int pixel = rowOffset + column * 4;
                int alpha = source.Rgba[pixel + 3];
                destination.Rgba[pixel] = UnpremultiplyChannel(source.Rgba[pixel], alpha);
                destination.Rgba[pixel + 1] = UnpremultiplyChannel(source.Rgba[pixel + 1], alpha);
                destination.Rgba[pixel + 2] = UnpremultiplyChannel(source.Rgba[pixel + 2], alpha);
                column = column + 1;
            }

            row = row + 1;
        }

        return destination;
    }

    internal static bool HasNonOpaqueAlpha(WebpPixelBuffer source)
    {
        return WebpAlphaHelper.HasNonOpaqueAlpha(source);
    }

    internal static WebpPixelBuffer BlendOverBackground(WebpPixelBuffer source, uint backgroundColorBgra)
    {
        ArgumentNullException.ThrowIfNull(source);
        WebpPixelBufferHelper.ValidateRgbaBuffer(source);
        ValidateDefaultOutput(source.Width, source.Height, 1);

        WebpPixelBuffer destination = WebpPixelBufferHelper.AllocateRgba(source.Width, source.Height, source.Stride);
        WebpAnimationComposer.FillRectangle(destination, 0, 0, destination.Width, destination.Height, backgroundColorBgra);

        int row = 0;
        while (row < source.Height)
        {
            int sourceRow = row * source.Stride;
            int destinationRow = row * destination.Stride;
            int column = 0;
            while (column < source.Width)
            {
                int sourcePixel = sourceRow + column * 4;
                int destinationPixel = destinationRow + column * 4;
                WebpAnimationComposer.BlendPixel(source.Rgba, sourcePixel, destination.Rgba, destinationPixel);
                column = column + 1;
            }

            row = row + 1;
        }

        return destination;
    }

    internal static List<WebpFrame> FlattenAnimationFrames(WebpImage image)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(image.Frames);
        ValidateDefaultOutput(image.CanvasWidth, image.CanvasHeight, image.Frames.Count);
        return WebpAnimationComposer.ComposeFrames(image);
    }

    internal static void ValidateDefaultOutput(int width, int height, int count)
    {
        long bytes = (long)width * (long)height * 4L * (long)count;
        ValidateDefaultOutput(bytes);
    }

    internal static void ValidateDefaultOutput(long bytes)
    {
        WebpSecurityLimits limits = new WebpSecurityLimits();
        WebpSecurityLimitValidator.ValidateOutputBytes(bytes, limits, "source");
    }

    internal static byte PremultiplyChannel(byte channel, int alpha)
    {
        return (byte)((channel * alpha + 127) / 255);
    }

    internal static byte UnpremultiplyChannel(byte channel, int alpha)
    {
        if (alpha == 0)
        {
            return 0;
        }

        int value = (channel * 255 + alpha / 2) / alpha;
        if (value > 255)
        {
            return 255;
        }

        return (byte)value;
    }
}
