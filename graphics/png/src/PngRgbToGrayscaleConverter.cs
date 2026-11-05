namespace png;

/// <summary>
/// Converts RGB-family buffers to grayscale using fixed integer luminance weights.
/// </summary>
internal static class PngRgbToGrayscaleConverter
{
    internal const int RedWeight = 6968;
    internal const int GreenWeight = 23434;
    internal const int BlueWeight = 2366;
    internal const int WeightScale = 32768;

    /// <summary>
    /// Converts every RGB pixel to grayscale in a newly allocated buffer.
    /// </summary>
    /// <param name="rgbBuffer">The source RGB-family pixel buffer.</param>
    /// <returns>A newly allocated pixel buffer containing the result.</returns>
    public static PngPixelBuffer Convert(PngPixelBuffer rgbBuffer)
    {
        ArgumentNullException.ThrowIfNull(rgbBuffer);
        ArgumentNullException.ThrowIfNull(rgbBuffer.Data);

        if (PngReorderedPixelTransformer.IsReordered(rgbBuffer.Format))
        {
            return Convert(PngReorderedPixelTransformer.Normalize(rgbBuffer));
        }

        ValidateBuffer(rgbBuffer);

        if (rgbBuffer.Format == PngPixelFormat.Rgb16 || rgbBuffer.Format == PngPixelFormat.Rgba16)
        {
            return Convert16(rgbBuffer);
        }

        return Convert8(rgbBuffer);
    }

    /// <summary>
    /// Returns a grayscale buffer when all RGB channels match; otherwise returns a copy in the original format.
    /// </summary>
    /// <param name="rgbBuffer">The source RGB-family pixel buffer.</param>
    /// <returns>A newly allocated pixel buffer containing the result.</returns>
    public static PngPixelBuffer ConvertIfGrayscale(PngPixelBuffer rgbBuffer)
    {
        ArgumentNullException.ThrowIfNull(rgbBuffer);
        ArgumentNullException.ThrowIfNull(rgbBuffer.Data);

        if (PngReorderedPixelTransformer.IsReordered(rgbBuffer.Format))
        {
            PngPixelBuffer transformed = ConvertIfGrayscale(PngReorderedPixelTransformer.Normalize(rgbBuffer));
            return PngReorderedPixelTransformer.Restore(rgbBuffer.Format, transformed);
        }

        int inputBytesPerPixel = GetInputBytesPerPixel(rgbBuffer.Format);
        ValidateBuffer(rgbBuffer);
        if (IsGrayscaleCore(rgbBuffer, inputBytesPerPixel))
        {
            if (rgbBuffer.Format == PngPixelFormat.Rgb16 || rgbBuffer.Format == PngPixelFormat.Rgba16)
            {
                return Convert16(rgbBuffer);
            }

            return Convert8(rgbBuffer);
        }

        return CopyBufferCore(rgbBuffer, inputBytesPerPixel);
    }

    /// <summary>
    /// Determines whether red, green, and blue are equal for every pixel.
    /// </summary>
    /// <param name="rgbBuffer">The source RGB-family pixel buffer.</param>
    /// <returns>True when the requested condition is met; otherwise false.</returns>
    public static bool IsGrayscale(PngPixelBuffer rgbBuffer)
    {
        ArgumentNullException.ThrowIfNull(rgbBuffer);
        ArgumentNullException.ThrowIfNull(rgbBuffer.Data);

        if (PngReorderedPixelTransformer.IsReordered(rgbBuffer.Format))
        {
            return IsGrayscale(PngReorderedPixelTransformer.Normalize(rgbBuffer));
        }

        int inputBytesPerPixel = GetInputBytesPerPixel(rgbBuffer.Format);
        ValidateBuffer(rgbBuffer);
        return IsGrayscaleCore(rgbBuffer, inputBytesPerPixel);
    }

    /// <summary>
    /// Calculates an 8-bit grayscale sample with the converter's fixed integer luminance weights.
    /// </summary>
    /// <param name="red">The red sample.</param>
    /// <param name="green">The green sample.</param>
    /// <param name="blue">The blue sample.</param>
    /// <returns>The calculated value.</returns>
    public static byte CalculateLuminance8(byte red, byte green, byte blue)
    {
        int luminance = CalculateWeightedLuminance(red, green, blue);
        return (byte)luminance;
    }

    /// <summary>
    /// Calculates a 16-bit grayscale sample with the converter's fixed integer luminance weights.
    /// </summary>
    /// <param name="red">The red sample.</param>
    /// <param name="green">The green sample.</param>
    /// <param name="blue">The blue sample.</param>
    /// <returns>The calculated value.</returns>
    public static ushort CalculateLuminance16(ushort red, ushort green, ushort blue)
    {
        int luminance = CalculateWeightedLuminance(red, green, blue);
        return (ushort)luminance;
    }

    internal static int CalculateWeightedLuminance(int red, int green, int blue)
    {
        long weighted = (long)red * RedWeight + (long)green * GreenWeight + (long)blue * BlueWeight;
        return (int)((weighted + WeightScale / 2) / WeightScale);
    }

    internal static PngPixelBuffer Convert8(PngPixelBuffer rgbBuffer)
    {
        PngPixelFormat outputFormat = GetOutputFormat(rgbBuffer.Format);
        int outputBytesPerPixel = GetOutputBytesPerPixel(outputFormat);
        int outputStride = CalculateOutputStride(rgbBuffer.Width, outputBytesPerPixel);
        byte[] output = CreateOutputData(outputStride, rgbBuffer.Height);

        int rowIndex = 0;
        while (rowIndex < rgbBuffer.Height)
        {
            Convert8Row(rgbBuffer, rowIndex, output, outputStride);
            rowIndex += 1;
        }

        return CreateBuffer(outputFormat, rgbBuffer.Width, rgbBuffer.Height, outputStride, output);
    }

    internal static PngPixelBuffer Convert16(PngPixelBuffer rgbBuffer)
    {
        PngPixelFormat outputFormat = GetOutputFormat(rgbBuffer.Format);
        int outputBytesPerPixel = GetOutputBytesPerPixel(outputFormat);
        int outputStride = CalculateOutputStride(rgbBuffer.Width, outputBytesPerPixel);
        byte[] output = CreateOutputData(outputStride, rgbBuffer.Height);

        int rowIndex = 0;
        while (rowIndex < rgbBuffer.Height)
        {
            Convert16Row(rgbBuffer, rowIndex, output, outputStride);
            rowIndex += 1;
        }

        return CreateBuffer(outputFormat, rgbBuffer.Width, rgbBuffer.Height, outputStride, output);
    }

    internal static PngPixelBuffer CopyBufferCore(PngPixelBuffer rgbBuffer, int bytesPerPixel)
    {
        int outputStride = CalculateOutputStride(rgbBuffer.Width, bytesPerPixel);
        byte[] output = CreateOutputData(outputStride, rgbBuffer.Height);

        int rowIndex = 0;
        while (rowIndex < rgbBuffer.Height)
        {
            CopyPixelRow(rgbBuffer, rowIndex, output, outputStride, outputStride);
            rowIndex += 1;
        }

        return CreateBuffer(rgbBuffer.Format, rgbBuffer.Width, rgbBuffer.Height, outputStride, output);
    }

    internal static void Convert8Row(PngPixelBuffer rgbBuffer, int rowIndex, byte[] output, int outputStride)
    {
        int inputBytesPerPixel = GetInputBytesPerPixel(rgbBuffer.Format);
        int sourceOffset = rgbBuffer.Stride * rowIndex;
        int destinationOffset = outputStride * rowIndex;
        int x = 0;
        while (x < rgbBuffer.Width)
        {
            byte gray = CalculateLuminance8(rgbBuffer.Data[sourceOffset], rgbBuffer.Data[sourceOffset + 1], rgbBuffer.Data[sourceOffset + 2]);
            output[destinationOffset] = gray;
            if (rgbBuffer.Format == PngPixelFormat.Rgba8)
            {
                output[destinationOffset + 1] = rgbBuffer.Data[sourceOffset + 3];
                destinationOffset += 2;
            }
            else
            {
                destinationOffset += 1;
            }

            sourceOffset += inputBytesPerPixel;
            x += 1;
        }
    }

    internal static void CopyPixelRow(PngPixelBuffer rgbBuffer, int rowIndex, byte[] output, int outputStride, int rowBytes)
    {
        int sourceOffset = rgbBuffer.Stride * rowIndex;
        int destinationOffset = outputStride * rowIndex;
        CopyBytes(rgbBuffer.Data, sourceOffset, output, destinationOffset, rowBytes);
    }

    internal static bool IsGrayscaleCore(PngPixelBuffer rgbBuffer, int inputBytesPerPixel)
    {
        int rowIndex = 0;
        while (rowIndex < rgbBuffer.Height)
        {
            if (!IsGrayscaleRow(rgbBuffer, rowIndex, inputBytesPerPixel))
            {
                return false;
            }

            rowIndex += 1;
        }

        return true;
    }

    internal static bool IsGrayscaleRow(PngPixelBuffer rgbBuffer, int rowIndex, int inputBytesPerPixel)
    {
        int sourceOffset = rgbBuffer.Stride * rowIndex;
        int x = 0;
        while (x < rgbBuffer.Width)
        {
            if (!IsGrayscalePixel(rgbBuffer, sourceOffset))
            {
                return false;
            }

            sourceOffset += inputBytesPerPixel;
            x += 1;
        }

        return true;
    }

    internal static bool IsGrayscalePixel(PngPixelBuffer rgbBuffer, int offset)
    {
        if (rgbBuffer.Format == PngPixelFormat.Rgb16 || rgbBuffer.Format == PngPixelFormat.Rgba16)
        {
            ushort red = PngBinaryReader.ReadUInt16(rgbBuffer.Data, offset);
            ushort green = PngBinaryReader.ReadUInt16(rgbBuffer.Data, offset + 2);
            ushort blue = PngBinaryReader.ReadUInt16(rgbBuffer.Data, offset + 4);
            return red == green && red == blue;
        }

        byte red8 = rgbBuffer.Data[offset];
        byte green8 = rgbBuffer.Data[offset + 1];
        byte blue8 = rgbBuffer.Data[offset + 2];
        return red8 == green8 && red8 == blue8;
    }

    internal static void Convert16Row(PngPixelBuffer rgbBuffer, int rowIndex, byte[] output, int outputStride)
    {
        int inputBytesPerPixel = GetInputBytesPerPixel(rgbBuffer.Format);
        int sourceOffset = rgbBuffer.Stride * rowIndex;
        int destinationOffset = outputStride * rowIndex;
        int x = 0;
        while (x < rgbBuffer.Width)
        {
            ushort red = PngBinaryReader.ReadUInt16(rgbBuffer.Data, sourceOffset);
            ushort green = PngBinaryReader.ReadUInt16(rgbBuffer.Data, sourceOffset + 2);
            ushort blue = PngBinaryReader.ReadUInt16(rgbBuffer.Data, sourceOffset + 4);
            ushort gray = CalculateLuminance16(red, green, blue);
            PngBinaryWriter.WriteUInt16(output, destinationOffset, gray);
            if (rgbBuffer.Format == PngPixelFormat.Rgba16)
            {
                output[destinationOffset + 2] = rgbBuffer.Data[sourceOffset + 6];
                output[destinationOffset + 3] = rgbBuffer.Data[sourceOffset + 7];
                destinationOffset += 4;
            }
            else
            {
                destinationOffset += 2;
            }

            sourceOffset += inputBytesPerPixel;
            x += 1;
        }
    }

    internal static void ValidateBuffer(PngPixelBuffer buffer)
    {
        if (buffer.Width <= 0)
        {
            throw new ArgumentOutOfRangeException("Width");
        }

        if (buffer.Height <= 0)
        {
            throw new ArgumentOutOfRangeException("Height");
        }

        int rowBytes = CalculateInputRowBytes(buffer.Width, buffer.Format);
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

    internal static int CalculateInputRowBytes(int width, PngPixelFormat pixelFormat)
    {
        long rowBytes = PngSizeCalculator.CalculateFrameRowBytes(width, pixelFormat);
        GetInputBytesPerPixel(pixelFormat);
        if (rowBytes > int.MaxValue)
        {
            throw new OverflowException();
        }

        return (int)rowBytes;
    }

    internal static int GetInputBytesPerPixel(PngPixelFormat pixelFormat)
    {
        switch (pixelFormat)
        {
            case PngPixelFormat.Rgb8:
                return 3;
            case PngPixelFormat.Rgb16:
                return 6;
            case PngPixelFormat.Rgba8:
                return 4;
            case PngPixelFormat.Rgba16:
                return 8;
            default:
                throw new ArgumentOutOfRangeException("Format");
        }
    }

    internal static PngPixelFormat GetOutputFormat(PngPixelFormat inputFormat)
    {
        switch (inputFormat)
        {
            case PngPixelFormat.Rgb8:
                return PngPixelFormat.Grayscale8;
            case PngPixelFormat.Rgb16:
                return PngPixelFormat.Grayscale16;
            case PngPixelFormat.Rgba8:
                return PngPixelFormat.GrayscaleAlpha8;
            case PngPixelFormat.Rgba16:
                return PngPixelFormat.GrayscaleAlpha16;
            default:
                throw new ArgumentOutOfRangeException("Format");
        }
    }

    internal static int GetOutputBytesPerPixel(PngPixelFormat pixelFormat)
    {
        switch (pixelFormat)
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
