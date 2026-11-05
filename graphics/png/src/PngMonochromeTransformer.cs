namespace png;

/// <summary>
/// Provides sample inversion for grayscale pixel buffers.
/// </summary>
internal static class PngMonochromeTransformer
{
    /// <summary>
    /// Complements every grayscale sample and returns a new buffer.
    /// </summary>
    /// <param name="buffer">The source pixel buffer.</param>
    /// <returns>A newly allocated pixel buffer containing the result.</returns>
    public static PngPixelBuffer Invert(PngPixelBuffer buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentNullException.ThrowIfNull(buffer.Data);

        PngBitDepth bitDepth = GetMonochromeBitDepth(buffer.Format);
        ValidateBuffer(buffer, bitDepth);

        if (IsPackedMonochromeFormat(buffer.Format))
        {
            return InvertPacked(buffer, bitDepth);
        }

        if (buffer.Format == PngPixelFormat.Grayscale16 || buffer.Format == PngPixelFormat.GrayscaleAlpha16)
        {
            return Invert16(buffer);
        }

        return Invert8(buffer);
    }

    internal static PngPixelBuffer InvertPacked(PngPixelBuffer buffer, PngBitDepth bitDepth)
    {
        int outputStride = CalculatePackedRowBytes(buffer.Width, bitDepth);
        byte[] output = CreateOutputData(outputStride, buffer.Height);

        int rowIndex = 0;
        while (rowIndex < buffer.Height)
        {
            InvertPackedRow(buffer, rowIndex, bitDepth, output, outputStride);
            rowIndex += 1;
        }

        return CreateBuffer(buffer.Format, buffer.Width, buffer.Height, outputStride, output);
    }

    internal static PngPixelBuffer Invert8(PngPixelBuffer buffer)
    {
        int bytesPerPixel = GetBytesPerPixel(buffer.Format);
        int outputStride = CalculateOutputStride(buffer.Width, bytesPerPixel);
        byte[] output = CreateOutputData(outputStride, buffer.Height);

        int rowIndex = 0;
        while (rowIndex < buffer.Height)
        {
            Invert8Row(buffer, rowIndex, bytesPerPixel, output, outputStride);
            rowIndex += 1;
        }

        return CreateBuffer(buffer.Format, buffer.Width, buffer.Height, outputStride, output);
    }

    internal static PngPixelBuffer Invert16(PngPixelBuffer buffer)
    {
        int bytesPerPixel = GetBytesPerPixel(buffer.Format);
        int outputStride = CalculateOutputStride(buffer.Width, bytesPerPixel);
        byte[] output = CreateOutputData(outputStride, buffer.Height);

        int rowIndex = 0;
        while (rowIndex < buffer.Height)
        {
            Invert16Row(buffer, rowIndex, bytesPerPixel, output, outputStride);
            rowIndex += 1;
        }

        return CreateBuffer(buffer.Format, buffer.Width, buffer.Height, outputStride, output);
    }

    internal static void InvertPackedRow(PngPixelBuffer buffer, int rowIndex, PngBitDepth bitDepth, byte[] output, int outputStride)
    {
        int sourceOffset = buffer.Stride * rowIndex;
        int destinationOffset = outputStride * rowIndex;
        byte[] samples = PngPackedSampleUnpacker.Unpack(buffer.Data, sourceOffset, buffer.Width, bitDepth);
        InvertPackedSamples(samples, bitDepth);
        PngPackedSampleWriter.PackInto(output, destinationOffset, samples, 0, samples.Length, bitDepth);
    }

    internal static void InvertPackedSamples(byte[] samples, PngBitDepth bitDepth)
    {
        int maximumSample = GetMaximumPackedSample(bitDepth);
        int index = 0;
        while (index < samples.Length)
        {
            samples[index] = (byte)(maximumSample - samples[index]);
            index += 1;
        }
    }

    internal static void Invert8Row(PngPixelBuffer buffer, int rowIndex, int bytesPerPixel, byte[] output, int outputStride)
    {
        int sourceOffset = buffer.Stride * rowIndex;
        int destinationOffset = outputStride * rowIndex;
        int x = 0;
        while (x < buffer.Width)
        {
            CopyBytes(buffer.Data, sourceOffset, output, destinationOffset, bytesPerPixel);
            output[destinationOffset] = (byte)(255 - output[destinationOffset]);
            sourceOffset += bytesPerPixel;
            destinationOffset += bytesPerPixel;
            x += 1;
        }
    }

    internal static void Invert16Row(PngPixelBuffer buffer, int rowIndex, int bytesPerPixel, byte[] output, int outputStride)
    {
        int sourceOffset = buffer.Stride * rowIndex;
        int destinationOffset = outputStride * rowIndex;
        int x = 0;
        while (x < buffer.Width)
        {
            CopyBytes(buffer.Data, sourceOffset, output, destinationOffset, bytesPerPixel);
            output[destinationOffset] = (byte)(255 - output[destinationOffset]);
            output[destinationOffset + 1] = (byte)(255 - output[destinationOffset + 1]);
            sourceOffset += bytesPerPixel;
            destinationOffset += bytesPerPixel;
            x += 1;
        }
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

        int rowBytes = CalculateInputRowBytes(buffer.Width, buffer.Format, bitDepth);
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

    internal static int CalculateInputRowBytes(int width, PngPixelFormat format, PngBitDepth bitDepth)
    {
        if (IsPackedMonochromeFormat(format))
        {
            return CalculatePackedRowBytes(width, bitDepth);
        }

        int bytesPerPixel = GetBytesPerPixel(format);
        return CalculateOutputStride(width, bytesPerPixel);
    }

    internal static int CalculatePackedRowBytes(int width, PngBitDepth bitDepth)
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

    internal static PngBitDepth GetMonochromeBitDepth(PngPixelFormat format)
    {
        switch (format)
        {
            case PngPixelFormat.Grayscale1:
                return PngBitDepth.One;
            case PngPixelFormat.Grayscale2:
                return PngBitDepth.Two;
            case PngPixelFormat.Grayscale4:
                return PngBitDepth.Four;
            case PngPixelFormat.Grayscale8:
                return PngBitDepth.Eight;
            case PngPixelFormat.Grayscale16:
                return PngBitDepth.Sixteen;
            case PngPixelFormat.GrayscaleAlpha8:
                return PngBitDepth.Eight;
            case PngPixelFormat.GrayscaleAlpha16:
                return PngBitDepth.Sixteen;
            default:
                throw new ArgumentOutOfRangeException("Format");
        }
    }

    internal static bool IsPackedMonochromeFormat(PngPixelFormat format)
    {
        return format == PngPixelFormat.Grayscale1
            || format == PngPixelFormat.Grayscale2
            || format == PngPixelFormat.Grayscale4;
    }

    internal static int GetBytesPerPixel(PngPixelFormat format)
    {
        switch (format)
        {
            case PngPixelFormat.Grayscale8:
                return 1;
            case PngPixelFormat.Grayscale16:
                return 2;
            case PngPixelFormat.GrayscaleAlpha8:
                return 2;
            case PngPixelFormat.GrayscaleAlpha16:
                return 4;
            default:
                throw new ArgumentOutOfRangeException("Format");
        }
    }

    internal static int GetMaximumPackedSample(PngBitDepth bitDepth)
    {
        switch (bitDepth)
        {
            case PngBitDepth.One:
                return 1;
            case PngBitDepth.Two:
                return 3;
            case PngBitDepth.Four:
                return 15;
            default:
                throw new ArgumentOutOfRangeException("bitDepth");
        }
    }

    internal static int CalculateOutputStride(int width, int bytesPerPixel)
    {
        long stride = (long)width * bytesPerPixel;
        if (stride > int.MaxValue)
        {
            throw new OverflowException();
        }

        return (int)stride;
    }

    internal static byte[] CreateOutputData(int stride, int height)
    {
        long byteCount = (long)stride * height;
        if (byteCount > int.MaxValue)
        {
            throw new OverflowException();
        }

        return new byte[(int)byteCount];
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

    internal static PngPixelBuffer CreateBuffer(PngPixelFormat format, int width, int height, int stride, byte[] data)
    {
        PngPixelBuffer buffer = new PngPixelBuffer();
        buffer.Format = format;
        buffer.Width = width;
        buffer.Height = height;
        buffer.Stride = stride;
        buffer.Data = data;
        return buffer;
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
