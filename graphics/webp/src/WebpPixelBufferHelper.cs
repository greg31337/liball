using System;

namespace webp;

/// <summary>Allocates, validates, copies, and converts row-major four-byte RGBA pixel buffers.</summary>
internal static class WebpPixelBufferHelper
{
    /// <summary>Calculates the compact byte stride for one RGBA row.</summary>
    /// <param name="width">The positive row width in pixels.</param>
    /// <returns><c>width * 4</c> when the result fits in a 32-bit array length.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>width</c> is not positive or the stride is too large.</exception>
    internal static int ComputeMinimumRgbaStride(int width)
    {
        ValidatePositiveDimension(width, "width");
        long stride = (long)width * 4L;
        if (stride > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException("width");
        }

        return (int)stride;
    }

    /// <summary>Calculates the byte-array length needed for a strided RGBA image.</summary>
    /// <param name="width">The positive image width in pixels.</param>
    /// <param name="height">The positive image height in pixels.</param>
    /// <param name="stride">The bytes allocated per row; it must be at least <c>width * 4</c>.</param>
    /// <returns><c>height * stride</c>.</returns>
    /// <exception cref="ArgumentException">Thrown when <c>stride</c> is too small.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a dimension is not positive or the required array is too large.</exception>
    internal static int ComputeRequiredRgbaLength(int width, int height, int stride)
    {
        ValidateRgbaShape(width, height, stride);
        long required = ComputeRequiredLength(width, height, stride);
        if (required > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException("height");
        }

        return (int)required;
    }

    /// <summary>Allocates a compact RGBA buffer with no padding between rows.</summary>
    /// <param name="width">The positive image width in pixels.</param>
    /// <param name="height">The positive image height in pixels.</param>
    /// <returns>A zero-filled buffer whose stride is <c>width * 4</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a dimension is not positive or the required array is too large.</exception>
    internal static WebpPixelBuffer AllocateRgba(int width, int height)
    {
        int stride = ComputeMinimumRgbaStride(width);
        return AllocateRgba(width, height, stride);
    }

    /// <summary>Allocates an RGBA buffer with a caller-selected row stride.</summary>
    /// <param name="width">The positive image width in pixels.</param>
    /// <param name="height">The positive image height in pixels.</param>
    /// <param name="stride">The bytes per row, including any desired trailing padding.</param>
    /// <returns>A zero-filled buffer with the requested shape.</returns>
    /// <exception cref="ArgumentException">Thrown when <c>stride</c> is smaller than one compact RGBA row.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a dimension is not positive or the required array is too large.</exception>
    internal static WebpPixelBuffer AllocateRgba(int width, int height, int stride)
    {
        int requiredLength = ComputeRequiredRgbaLength(width, height, stride);
        WebpPixelBuffer buffer = new WebpPixelBuffer();
        buffer.Width = width;
        buffer.Height = height;
        buffer.Stride = stride;
        buffer.Rgba = new byte[requiredLength];
        return buffer;
    }

    /// <summary>Checks that a pixel-buffer model has positive dimensions, a sufficient stride, and enough bytes.</summary>
    /// <param name="buffer">The buffer model to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>buffer</c> or <c>buffer.Rgba</c> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the stride is too small or the byte array is too short.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a dimension is not positive or the required size is too large.</exception>
    internal static void ValidateRgbaBuffer(WebpPixelBuffer buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentNullException.ThrowIfNull(buffer.Rgba);
        ValidateRgbaBuffer(buffer.Rgba, buffer.Width, buffer.Height, buffer.Stride);
    }

    /// <summary>Checks that an RGBA byte array can represent the supplied dimensions and stride.</summary>
    /// <param name="rgba">The row-major four-byte pixel array.</param>
    /// <param name="width">The image width in pixels.</param>
    /// <param name="height">The image height in pixels.</param>
    /// <param name="stride">The number of bytes allocated per row.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>rgba</c> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the stride is too small or the byte array is too short.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a dimension is not positive or the required size is too large.</exception>
    internal static void ValidateRgbaBuffer(byte[] rgba, int width, int height, int stride)
    {
        ArgumentNullException.ThrowIfNull(rgba);
        int requiredLength = ComputeRequiredRgbaLength(width, height, stride);
        if (rgba.Length < requiredLength)
        {
            throw new ArgumentException("RGBA buffer is too small for the dimensions and stride", "rgba");
        }
    }

    /// <summary>Creates a deep copy of a valid pixel buffer, including row padding.</summary>
    /// <param name="source">The buffer to copy.</param>
    /// <returns>An independent buffer with the same shape and byte contents.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>source</c> or its byte array is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the source shape is invalid.</exception>
    internal static WebpPixelBuffer Copy(WebpPixelBuffer source)
    {
        ValidateRgbaBuffer(source);
        WebpPixelBuffer copy = new WebpPixelBuffer();
        copy.Width = source.Width;
        copy.Height = source.Height;
        copy.Stride = source.Stride;
        copy.Rgba = new byte[source.Rgba.Length];
        Array.Copy(source.Rgba, copy.Rgba, source.Rgba.Length);
        return copy;
    }

    /// <summary>Copies a strided RGBA image into a compact byte array.</summary>
    /// <param name="source">The source RGBA bytes.</param>
    /// <param name="width">The image width in pixels.</param>
    /// <param name="height">The image height in pixels.</param>
    /// <param name="sourceStride">The source bytes per row.</param>
    /// <returns>Compact RGBA rows with any source padding removed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>source</c> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the source shape is invalid.</exception>
    internal static byte[] CopyRgba(byte[] source, int width, int height, int sourceStride)
    {
        ValidateRgbaBuffer(source, width, height, sourceStride);
        int destinationStride = ComputeMinimumRgbaStride(width);
        byte[] destination = new byte[ComputeRequiredRgbaLength(width, height, destinationStride)];
        CopyRgbaTo(source, width, height, sourceStride, destination, destinationStride);
        return destination;
    }

    /// <summary>Copies RGBA pixel bytes row by row between buffers that may have different strides.</summary>
    /// <remarks>Only the <c>width * 4</c> pixel bytes in each row are copied; destination padding is left unchanged.</remarks>
    /// <param name="source">The source RGBA bytes.</param>
    /// <param name="width">The image width in pixels.</param>
    /// <param name="height">The image height in pixels.</param>
    /// <param name="sourceStride">The source bytes per row.</param>
    /// <param name="destination">The destination RGBA bytes.</param>
    /// <param name="destinationStride">The destination bytes per row.</param>
    /// <exception cref="ArgumentNullException">Thrown when either byte array is null.</exception>
    /// <exception cref="ArgumentException">Thrown when either buffer shape is invalid.</exception>
    internal static void CopyRgbaTo(byte[] source, int width, int height, int sourceStride, byte[] destination, int destinationStride)
    {
        ValidateRgbaBuffer(source, width, height, sourceStride);
        ValidateRgbaBuffer(destination, width, height, destinationStride);

        int rowBytes = ComputeMinimumRgbaStride(width);
        int row = 0;
        while (row < height)
        {
            Array.Copy(source, row * sourceStride, destination, row * destinationStride, rowBytes);
            row = row + 1;
        }
    }

    /// <summary>Converts strided RGBA rows to a compact BGRA byte array by exchanging red and blue.</summary>
    /// <param name="rgba">The source RGBA bytes.</param>
    /// <param name="width">The image width in pixels.</param>
    /// <param name="height">The image height in pixels.</param>
    /// <param name="stride">The source bytes per row.</param>
    /// <returns>Compact blue, green, red, alpha pixel bytes.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>rgba</c> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the source shape is invalid.</exception>
    internal static byte[] ToBgra(byte[] rgba, int width, int height, int stride)
    {
        ValidateRgbaBuffer(rgba, width, height, stride);
        int destinationStride = ComputeMinimumRgbaStride(width);
        byte[] bgra = new byte[ComputeRequiredRgbaLength(width, height, destinationStride)];
        CopyRgbaToBgra(rgba, width, height, stride, bgra, destinationStride);
        return bgra;
    }

    /// <summary>Converts strided BGRA rows to a compact RGBA byte array by exchanging blue and red.</summary>
    /// <param name="bgra">The source BGRA bytes.</param>
    /// <param name="width">The image width in pixels.</param>
    /// <param name="height">The image height in pixels.</param>
    /// <param name="stride">The source bytes per row.</param>
    /// <returns>Compact red, green, blue, alpha pixel bytes.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>bgra</c> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the source shape is invalid.</exception>
    internal static byte[] FromBgra(byte[] bgra, int width, int height, int stride)
    {
        ValidateRgbaBuffer(bgra, width, height, stride);
        int destinationStride = ComputeMinimumRgbaStride(width);
        byte[] rgba = new byte[ComputeRequiredRgbaLength(width, height, destinationStride)];
        CopyBgraToRgba(bgra, width, height, stride, rgba, destinationStride);
        return rgba;
    }

    /// <summary>Copies RGBA pixels into BGRA storage, exchanging red and blue while respecting both strides.</summary>
    /// <param name="rgba">The source RGBA bytes.</param>
    /// <param name="width">The image width in pixels.</param>
    /// <param name="height">The image height in pixels.</param>
    /// <param name="rgbaStride">The source bytes per row.</param>
    /// <param name="bgra">The destination BGRA bytes.</param>
    /// <param name="bgraStride">The destination bytes per row.</param>
    /// <exception cref="ArgumentNullException">Thrown when either byte array is null.</exception>
    /// <exception cref="ArgumentException">Thrown when either buffer shape is invalid.</exception>
    internal static void CopyRgbaToBgra(byte[] rgba, int width, int height, int rgbaStride, byte[] bgra, int bgraStride)
    {
        ValidateRgbaBuffer(rgba, width, height, rgbaStride);
        ValidateRgbaBuffer(bgra, width, height, bgraStride);
        CopyWithRedBlueSwap(rgba, width, height, rgbaStride, bgra, bgraStride);
    }

    /// <summary>Copies BGRA pixels into RGBA storage, exchanging blue and red while respecting both strides.</summary>
    /// <param name="bgra">The source BGRA bytes.</param>
    /// <param name="width">The image width in pixels.</param>
    /// <param name="height">The image height in pixels.</param>
    /// <param name="bgraStride">The source bytes per row.</param>
    /// <param name="rgba">The destination RGBA bytes.</param>
    /// <param name="rgbaStride">The destination bytes per row.</param>
    /// <exception cref="ArgumentNullException">Thrown when either byte array is null.</exception>
    /// <exception cref="ArgumentException">Thrown when either buffer shape is invalid.</exception>
    internal static void CopyBgraToRgba(byte[] bgra, int width, int height, int bgraStride, byte[] rgba, int rgbaStride)
    {
        ValidateRgbaBuffer(bgra, width, height, bgraStride);
        ValidateRgbaBuffer(rgba, width, height, rgbaStride);
        CopyWithRedBlueSwap(bgra, width, height, bgraStride, rgba, rgbaStride);
    }

    internal static void CopyWithRedBlueSwap(byte[] source, int width, int height, int sourceStride, byte[] destination, int destinationStride)
    {
        int row = 0;
        while (row < height)
        {
            int sourceOffset = row * sourceStride;
            int destinationOffset = row * destinationStride;
            int column = 0;
            while (column < width)
            {
                int sourcePixel = sourceOffset + column * 4;
                int destinationPixel = destinationOffset + column * 4;
                destination[destinationPixel] = source[sourcePixel + 2];
                destination[destinationPixel + 1] = source[sourcePixel + 1];
                destination[destinationPixel + 2] = source[sourcePixel];
                destination[destinationPixel + 3] = source[sourcePixel + 3];
                column = column + 1;
            }

            row = row + 1;
        }
    }

    internal static void ValidateRgbaShape(int width, int height, int stride)
    {
        ValidatePositiveDimension(width, "width");
        ValidatePositiveDimension(height, "height");

        int minimumStride = ComputeMinimumRgbaStride(width);
        if (stride < minimumStride)
        {
            throw new ArgumentException("RGBA stride is smaller than width * 4", "stride");
        }
    }

    internal static void ValidatePositiveDimension(int value, string parameterName)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }

    internal static long ComputeRequiredLength(int width, int height, int stride)
    {
        return (long)height * (long)stride;
    }
}
