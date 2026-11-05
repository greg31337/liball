using System;

namespace webp;

internal static class WebpAlphaHelper
{
    internal static bool HasNonOpaqueAlpha(WebpPixelBuffer buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        WebpPixelBufferHelper.ValidateRgbaBuffer(buffer);
        return HasNonOpaqueAlpha(buffer.Rgba, buffer.Width, buffer.Height, buffer.Stride);
    }

    internal static bool HasNonOpaqueAlpha(byte[] rgba, int width, int height, int stride)
    {
        WebpPixelBufferHelper.ValidateRgbaBuffer(rgba, width, height, stride);

        int row = 0;
        while (row < height)
        {
            int rowOffset = row * stride;
            int column = 0;
            while (column < width)
            {
                int pixel = rowOffset + column * 4;
                if (rgba[pixel + 3] != 255)
                {
                    return true;
                }

                column = column + 1;
            }

            row = row + 1;
        }

        return false;
    }

    internal static bool HasTransparentPixels(WebpPixelBuffer buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        WebpPixelBufferHelper.ValidateRgbaBuffer(buffer);
        return HasTransparentPixels(buffer.Rgba, buffer.Width, buffer.Height, buffer.Stride);
    }

    internal static bool HasTransparentPixels(byte[] rgba, int width, int height, int stride)
    {
        WebpPixelBufferHelper.ValidateRgbaBuffer(rgba, width, height, stride);

        int row = 0;
        while (row < height)
        {
            int rowOffset = row * stride;
            int column = 0;
            while (column < width)
            {
                int pixel = rowOffset + column * 4;
                if (rgba[pixel + 3] == 0)
                {
                    return true;
                }

                column = column + 1;
            }

            row = row + 1;
        }

        return false;
    }

    internal static bool HasTransparentRgb(WebpPixelBuffer buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        WebpPixelBufferHelper.ValidateRgbaBuffer(buffer);
        return HasTransparentRgb(buffer.Rgba, buffer.Width, buffer.Height, buffer.Stride);
    }

    internal static bool HasTransparentRgb(byte[] rgba, int width, int height, int stride)
    {
        WebpPixelBufferHelper.ValidateRgbaBuffer(rgba, width, height, stride);

        int row = 0;
        while (row < height)
        {
            int rowOffset = row * stride;
            int column = 0;
            while (column < width)
            {
                int pixel = rowOffset + column * 4;
                if (rgba[pixel + 3] == 0 && HasRgb(rgba, pixel))
                {
                    return true;
                }

                column = column + 1;
            }

            row = row + 1;
        }

        return false;
    }

    internal static void ClearTransparentRgb(WebpPixelBuffer buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        WebpPixelBufferHelper.ValidateRgbaBuffer(buffer);
        ClearTransparentRgb(buffer.Rgba, buffer.Width, buffer.Height, buffer.Stride);
    }

    internal static void ClearTransparentRgb(byte[] rgba, int width, int height, int stride)
    {
        WebpPixelBufferHelper.ValidateRgbaBuffer(rgba, width, height, stride);

        int row = 0;
        while (row < height)
        {
            int rowOffset = row * stride;
            int column = 0;
            while (column < width)
            {
                int pixel = rowOffset + column * 4;
                if (rgba[pixel + 3] == 0)
                {
                    rgba[pixel] = 0;
                    rgba[pixel + 1] = 0;
                    rgba[pixel + 2] = 0;
                }

                column = column + 1;
            }

            row = row + 1;
        }
    }

    internal static void PreserveTransparentRgb(WebpPixelBuffer source, WebpPixelBuffer destination)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        WebpPixelBufferHelper.ValidateRgbaBuffer(source);
        WebpPixelBufferHelper.ValidateRgbaBuffer(destination);
        ValidateSameDimensions(source.Width, source.Height, destination.Width, destination.Height);
        PreserveTransparentRgb(source.Rgba, source.Width, source.Height, source.Stride, destination.Rgba, destination.Stride);
    }

    internal static void PreserveTransparentRgb(byte[] sourceRgba, int width, int height, int sourceStride, byte[] destinationRgba, int destinationStride)
    {
        WebpPixelBufferHelper.ValidateRgbaBuffer(sourceRgba, width, height, sourceStride);
        WebpPixelBufferHelper.ValidateRgbaBuffer(destinationRgba, width, height, destinationStride);

        int row = 0;
        while (row < height)
        {
            int sourceRow = row * sourceStride;
            int destinationRow = row * destinationStride;
            int column = 0;
            while (column < width)
            {
                int sourcePixel = sourceRow + column * 4;
                int destinationPixel = destinationRow + column * 4;
                if (sourceRgba[sourcePixel + 3] == 0 && destinationRgba[destinationPixel + 3] == 0)
                {
                    destinationRgba[destinationPixel] = sourceRgba[sourcePixel];
                    destinationRgba[destinationPixel + 1] = sourceRgba[sourcePixel + 1];
                    destinationRgba[destinationPixel + 2] = sourceRgba[sourcePixel + 2];
                }

                column = column + 1;
            }

            row = row + 1;
        }
    }

    internal static bool HasRgb(byte[] rgba, int pixel)
    {
        return rgba[pixel] != 0 || rgba[pixel + 1] != 0 || rgba[pixel + 2] != 0;
    }

    internal static void ValidateSameDimensions(int sourceWidth, int sourceHeight, int destinationWidth, int destinationHeight)
    {
        if (sourceWidth != destinationWidth || sourceHeight != destinationHeight)
        {
            throw new ArgumentException("source and destination pixel buffers must have the same dimensions", "destination");
        }
    }
}
