namespace png;

/// <summary>
/// Expands grayscale pixels to RGB or expands packed grayscale samples to 8-bit values.
/// </summary>
internal static class PngGrayscaleExpander
{
    /// <summary>
    /// Replicates each grayscale sample into red, green, and blue channels in a new buffer.
    /// </summary>
    /// <param name="grayscaleBuffer">The source grayscale pixel buffer.</param>
    /// <returns>A newly allocated pixel buffer containing the result.</returns>
    public static PngPixelBuffer ExpandToRgb(PngPixelBuffer grayscaleBuffer)
    {
        ArgumentNullException.ThrowIfNull(grayscaleBuffer);
        ArgumentNullException.ThrowIfNull(grayscaleBuffer.Data);

        if (grayscaleBuffer.Format == PngPixelFormat.GrayscaleAlpha8)
        {
            return ConvertAlphaGrayscale(grayscaleBuffer, PngPixelFormat.Rgba8);
        }

        if (grayscaleBuffer.Format == PngPixelFormat.GrayscaleAlpha16)
        {
            return ConvertAlphaGrayscale(grayscaleBuffer, PngPixelFormat.Rgba16);
        }

        PngBitDepth bitDepth = GetGrayscaleBitDepth(grayscaleBuffer.Format);
        ValidateBuffer(grayscaleBuffer, bitDepth);

        if (grayscaleBuffer.Format == PngPixelFormat.Grayscale16)
        {
            return ExpandGrayscale16ToRgb16(grayscaleBuffer);
        }

        return ExpandToRgb8(grayscaleBuffer, bitDepth);
    }

    /// <summary>
    /// Looks up each palette index and returns a new 8-bit RGB buffer.
    /// </summary>
    /// <param name="grayscaleBuffer">The source grayscale pixel buffer.</param>
    /// <returns>A newly allocated pixel buffer containing the result.</returns>
    public static PngPixelBuffer ExpandToRgb8(PngPixelBuffer grayscaleBuffer)
    {
        ArgumentNullException.ThrowIfNull(grayscaleBuffer);
        ArgumentNullException.ThrowIfNull(grayscaleBuffer.Data);

        if (grayscaleBuffer.Format == PngPixelFormat.GrayscaleAlpha8 || grayscaleBuffer.Format == PngPixelFormat.GrayscaleAlpha16)
        {
            return ConvertAlphaGrayscale(grayscaleBuffer, PngPixelFormat.Rgba8);
        }

        PngBitDepth bitDepth = GetEightBitOutputGrayscaleBitDepth(grayscaleBuffer.Format);
        ValidateBuffer(grayscaleBuffer, bitDepth);
        return ExpandToRgb8(grayscaleBuffer, bitDepth);
    }

    /// <summary>
    /// Unpacks 1-bit, 2-bit, or 4-bit grayscale samples and scales them across the 8-bit range.
    /// </summary>
    /// <param name="grayscaleBuffer">The source grayscale pixel buffer.</param>
    /// <returns>A newly allocated pixel buffer containing the result.</returns>
    public static PngPixelBuffer ExpandLowBitToGrayscale8(PngPixelBuffer grayscaleBuffer)
    {
        ArgumentNullException.ThrowIfNull(grayscaleBuffer);
        ArgumentNullException.ThrowIfNull(grayscaleBuffer.Data);

        PngBitDepth bitDepth = GetLowBitGrayscaleBitDepth(grayscaleBuffer.Format);
        ValidateBuffer(grayscaleBuffer, bitDepth);

        int outputStride = grayscaleBuffer.Width;
        byte[] output = CreateOutputData(outputStride, grayscaleBuffer.Height);
        int rowIndex = 0;
        while (rowIndex < grayscaleBuffer.Height)
        {
            ExpandLowBitRow(grayscaleBuffer, rowIndex, bitDepth, output, outputStride);
            rowIndex += 1;
        }

        return CreateBuffer(PngPixelFormat.Grayscale8, grayscaleBuffer.Width, grayscaleBuffer.Height, outputStride, output);
    }

    /// <summary>
    /// Determines whether a format stores packed 1-bit, 2-bit, or 4-bit grayscale samples.
    /// </summary>
    /// <param name="pixelFormat">The in-memory pixel format.</param>
    /// <returns>True when the requested condition is met; otherwise false.</returns>
    public static bool IsLowBitGrayscaleFormat(PngPixelFormat pixelFormat)
    {
        return pixelFormat == PngPixelFormat.Grayscale1
            || pixelFormat == PngPixelFormat.Grayscale2
            || pixelFormat == PngPixelFormat.Grayscale4;
    }

    internal static PngPixelBuffer ExpandToRgb8(PngPixelBuffer grayscaleBuffer, PngBitDepth bitDepth)
    {
        int outputStride = CalculateOutputStride(grayscaleBuffer.Width, 3);
        byte[] output = CreateOutputData(outputStride, grayscaleBuffer.Height);
        int rowIndex = 0;
        while (rowIndex < grayscaleBuffer.Height)
        {
            ExpandGrayscale8ToRgb8Row(grayscaleBuffer, rowIndex, bitDepth, output, outputStride);
            rowIndex += 1;
        }

        return CreateBuffer(PngPixelFormat.Rgb8, grayscaleBuffer.Width, grayscaleBuffer.Height, outputStride, output);
    }

    internal static PngPixelBuffer ConvertAlphaGrayscale(PngPixelBuffer grayscaleBuffer, PngPixelFormat targetFormat)
    {
        PngPixelConversionOptions options = new PngPixelConversionOptions();
        options.TargetFormat = targetFormat;
        return PngPixelFormatConverter.Convert(grayscaleBuffer, options);
    }

    internal static PngPixelBuffer ExpandGrayscale16ToRgb16(PngPixelBuffer grayscaleBuffer)
    {
        int outputStride = CalculateOutputStride(grayscaleBuffer.Width, 6);
        byte[] output = CreateOutputData(outputStride, grayscaleBuffer.Height);
        int rowIndex = 0;
        while (rowIndex < grayscaleBuffer.Height)
        {
            ExpandGrayscale16ToRgb16Row(grayscaleBuffer, rowIndex, output, outputStride);
            rowIndex += 1;
        }

        return CreateBuffer(PngPixelFormat.Rgb16, grayscaleBuffer.Width, grayscaleBuffer.Height, outputStride, output);
    }

    internal static void ExpandLowBitRow(PngPixelBuffer grayscaleBuffer, int rowIndex, PngBitDepth bitDepth, byte[] output, int outputStride)
    {
        int sourceOffset = grayscaleBuffer.Stride * rowIndex;
        int destinationOffset = outputStride * rowIndex;
        byte[] samples = PngPackedSampleUnpacker.Unpack(grayscaleBuffer.Data, sourceOffset, grayscaleBuffer.Width, bitDepth);
        int x = 0;
        while (x < grayscaleBuffer.Width)
        {
            output[destinationOffset + x] = ScaleSample(samples[x], bitDepth);
            x += 1;
        }
    }

    internal static void ExpandGrayscale8ToRgb8Row(PngPixelBuffer grayscaleBuffer, int rowIndex, PngBitDepth bitDepth, byte[] output, int outputStride)
    {
        int sourceOffset = grayscaleBuffer.Stride * rowIndex;
        int destinationOffset = outputStride * rowIndex;
        byte[] samples = GetGrayscale8Samples(grayscaleBuffer, sourceOffset, bitDepth);
        int x = 0;
        while (x < grayscaleBuffer.Width)
        {
            byte sample = samples[x];
            output[destinationOffset] = sample;
            output[destinationOffset + 1] = sample;
            output[destinationOffset + 2] = sample;
            destinationOffset += 3;
            x += 1;
        }
    }

    internal static void ExpandGrayscale16ToRgb16Row(PngPixelBuffer grayscaleBuffer, int rowIndex, byte[] output, int outputStride)
    {
        int sourceOffset = grayscaleBuffer.Stride * rowIndex;
        int destinationOffset = outputStride * rowIndex;
        int x = 0;
        while (x < grayscaleBuffer.Width)
        {
            output[destinationOffset] = grayscaleBuffer.Data[sourceOffset];
            output[destinationOffset + 1] = grayscaleBuffer.Data[sourceOffset + 1];
            output[destinationOffset + 2] = grayscaleBuffer.Data[sourceOffset];
            output[destinationOffset + 3] = grayscaleBuffer.Data[sourceOffset + 1];
            output[destinationOffset + 4] = grayscaleBuffer.Data[sourceOffset];
            output[destinationOffset + 5] = grayscaleBuffer.Data[sourceOffset + 1];
            sourceOffset += 2;
            destinationOffset += 6;
            x += 1;
        }
    }

    internal static byte[] GetGrayscale8Samples(PngPixelBuffer grayscaleBuffer, int sourceOffset, PngBitDepth bitDepth)
    {
        if (grayscaleBuffer.Format == PngPixelFormat.Grayscale8)
        {
            return CopyGrayscale8Samples(grayscaleBuffer, sourceOffset);
        }

        byte[] samples = PngPackedSampleUnpacker.Unpack(grayscaleBuffer.Data, sourceOffset, grayscaleBuffer.Width, bitDepth);
        int index = 0;
        while (index < samples.Length)
        {
            samples[index] = ScaleSample(samples[index], bitDepth);
            index += 1;
        }

        return samples;
    }

    internal static byte[] CopyGrayscale8Samples(PngPixelBuffer grayscaleBuffer, int sourceOffset)
    {
        byte[] samples = new byte[grayscaleBuffer.Width];
        int index = 0;
        while (index < samples.Length)
        {
            samples[index] = grayscaleBuffer.Data[sourceOffset + index];
            index += 1;
        }

        return samples;
    }

    internal static byte ScaleSample(byte sample, PngBitDepth bitDepth)
    {
        switch (bitDepth)
        {
            case PngBitDepth.One:
                return (byte)(sample * 255);
            case PngBitDepth.Two:
                return (byte)(sample * 85);
            case PngBitDepth.Four:
                return (byte)(sample * 17);
            default:
                throw new ArgumentOutOfRangeException("bitDepth");
        }
    }

    internal static PngBitDepth GetLowBitGrayscaleBitDepth(PngPixelFormat pixelFormat)
    {
        if (pixelFormat == PngPixelFormat.Grayscale1)
        {
            return PngBitDepth.One;
        }

        if (pixelFormat == PngPixelFormat.Grayscale2)
        {
            return PngBitDepth.Two;
        }

        if (pixelFormat == PngPixelFormat.Grayscale4)
        {
            return PngBitDepth.Four;
        }

        throw new ArgumentOutOfRangeException("Format");
    }

    internal static PngBitDepth GetEightBitOutputGrayscaleBitDepth(PngPixelFormat pixelFormat)
    {
        if (pixelFormat == PngPixelFormat.Grayscale8)
        {
            return PngBitDepth.Eight;
        }

        return GetLowBitGrayscaleBitDepth(pixelFormat);
    }

    internal static PngBitDepth GetGrayscaleBitDepth(PngPixelFormat pixelFormat)
    {
        if (pixelFormat == PngPixelFormat.Grayscale16)
        {
            return PngBitDepth.Sixteen;
        }

        return GetEightBitOutputGrayscaleBitDepth(pixelFormat);
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
}
