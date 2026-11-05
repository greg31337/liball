namespace png;

/// <summary>
/// Normalizes packed or byte-sized palette indices to one byte per pixel.
/// </summary>
internal static class PngPaletteIndexPixelConverter
{
    /// <summary>
    /// Unpacks palette indices to one byte per pixel in a new indexed buffer.
    /// </summary>
    /// <param name="buffer">The source pixel buffer.</param>
    /// <returns>A newly allocated pixel buffer containing the result.</returns>
    public static PngPixelBuffer ToIndexed8(PngPixelBuffer buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentNullException.ThrowIfNull(buffer.Data);

        PngBitDepth bitDepth = GetIndexedBitDepth(buffer.Format);
        ValidateBuffer(buffer, bitDepth);

        int outputStride = buffer.Width;
        long outputBytes = (long)outputStride * buffer.Height;
        if (outputBytes > int.MaxValue)
        {
            throw new OverflowException();
        }

        byte[] output = new byte[(int)outputBytes];
        int rowIndex = 0;
        while (rowIndex < buffer.Height)
        {
            CopyRowToIndexed8(buffer, rowIndex, bitDepth, output, outputStride);
            rowIndex += 1;
        }

        PngPixelBuffer result = new PngPixelBuffer();
        result.Format = PngPixelFormat.Indexed8;
        result.Width = buffer.Width;
        result.Height = buffer.Height;
        result.Stride = outputStride;
        result.Data = output;
        return result;
    }

    /// <summary>
    /// Determines whether a format stores packed or byte-sized palette indices.
    /// </summary>
    /// <param name="pixelFormat">The in-memory pixel format.</param>
    /// <returns>True when the requested condition is met; otherwise false.</returns>
    public static bool IsIndexedFormat(PngPixelFormat pixelFormat)
    {
        return pixelFormat == PngPixelFormat.Indexed1
            || pixelFormat == PngPixelFormat.Indexed2
            || pixelFormat == PngPixelFormat.Indexed4
            || pixelFormat == PngPixelFormat.Indexed8;
    }

    internal static void CopyRowToIndexed8(PngPixelBuffer buffer, int rowIndex, PngBitDepth bitDepth, byte[] output, int outputStride)
    {
        int sourceOffset = (int)((long)buffer.Stride * rowIndex);
        int destinationOffset = outputStride * rowIndex;

        if (buffer.Format == PngPixelFormat.Indexed8)
        {
            CopyBytes(buffer.Data, sourceOffset, output, destinationOffset, buffer.Width);
            return;
        }

        byte[] samples = PngPackedSampleUnpacker.Unpack(buffer.Data, sourceOffset, buffer.Width, bitDepth);
        CopyBytes(samples, 0, output, destinationOffset, samples.Length);
    }

    internal static void ValidateBuffer(PngPixelBuffer buffer, PngBitDepth bitDepth)
    {
        if (buffer.Width <= 0)
        {
            throw new ArgumentOutOfRangeException("Width");
        }

        if (buffer.Height <= 0)
        {
            throw new ArgumentOutOfRangeException("Height");
        }

        int rowBytes = CalculateRowBytes(buffer.Width, bitDepth);
        if (buffer.Stride < rowBytes)
        {
            throw new ArgumentOutOfRangeException("Stride");
        }

        long requiredBytes = CalculateRequiredBytes(buffer.Stride, buffer.Height, rowBytes);
        if (requiredBytes > buffer.Data.Length)
        {
            throw new ArgumentOutOfRangeException("Data");
        }
    }

    internal static PngBitDepth GetIndexedBitDepth(PngPixelFormat pixelFormat)
    {
        if (pixelFormat == PngPixelFormat.Indexed1)
        {
            return PngBitDepth.One;
        }

        if (pixelFormat == PngPixelFormat.Indexed2)
        {
            return PngBitDepth.Two;
        }

        if (pixelFormat == PngPixelFormat.Indexed4)
        {
            return PngBitDepth.Four;
        }

        if (pixelFormat == PngPixelFormat.Indexed8)
        {
            return PngBitDepth.Eight;
        }

        throw new ArgumentOutOfRangeException("Format");
    }

    internal static int CalculateRowBytes(int width, PngBitDepth bitDepth)
    {
        int bitsPerSample = PngSizeCalculator.GetBitDepthValue(bitDepth);
        long bitsPerRow = (long)width * bitsPerSample;
        long rowBytes = (bitsPerRow + 7) / 8;
        if (rowBytes > int.MaxValue)
        {
            throw new OverflowException();
        }

        return (int)rowBytes;
    }

    internal static long CalculateRequiredBytes(int stride, int height, int rowBytes)
    {
        if (height == 1)
        {
            return rowBytes;
        }

        long leadingRows = (long)stride * (height - 1);
        return leadingRows + rowBytes;
    }

    internal static void CopyBytes(byte[] source, int sourceOffset, byte[] destination, int destinationOffset, int length)
    {
        int index = 0;
        while (index < length)
        {
            destination[destinationOffset + index] = source[sourceOffset + index];
            index += 1;
        }
    }
}
