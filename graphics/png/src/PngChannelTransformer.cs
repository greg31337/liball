namespace png;

/// <summary>
/// Provides byte-channel reorder operations for RGB and RGBA pixel buffers.
/// </summary>
internal static class PngChannelTransformer
{
    /// <summary>
    /// Exchanges red and blue samples in every RGB pixel and returns a new buffer.
    /// </summary>
    /// <param name="buffer">The source pixel buffer.</param>
    /// <returns>A newly allocated pixel buffer containing the result.</returns>
    public static PngPixelBuffer SwapRgbBgr(PngPixelBuffer buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentNullException.ThrowIfNull(buffer.Data);

        ValidateBuffer(buffer);

        if (buffer.Format == PngPixelFormat.Rgb16
            || buffer.Format == PngPixelFormat.Bgr16
            || buffer.Format == PngPixelFormat.Rgba16
            || buffer.Format == PngPixelFormat.Bgra16)
        {
            return SwapRgbBgr16(buffer);
        }

        return SwapRgbBgr8(buffer);
    }

    /// <summary>
    /// Moves alpha between the first and last channel positions and returns a new buffer.
    /// </summary>
    /// <param name="buffer">The source pixel buffer.</param>
    /// <returns>A newly allocated pixel buffer containing the result.</returns>
    public static PngPixelBuffer SwapRgbaArgb(PngPixelBuffer buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentNullException.ThrowIfNull(buffer.Data);

        ValidateRgbaBuffer(buffer);

        if (buffer.Format == PngPixelFormat.Rgba16 || buffer.Format == PngPixelFormat.Argb16)
        {
            return SwapRgbaArgb16(buffer);
        }

        return SwapRgbaArgb8(buffer);
    }

    internal static PngPixelBuffer SwapRgbBgr8(PngPixelBuffer buffer)
    {
        int bytesPerPixel = GetBytesPerPixel(buffer.Format);
        int outputStride = CalculateOutputStride(buffer.Width, bytesPerPixel);
        byte[] output = CreateOutputData(outputStride, buffer.Height);

        int rowIndex = 0;
        while (rowIndex < buffer.Height)
        {
            SwapRgbBgr8Row(buffer, rowIndex, bytesPerPixel, output, outputStride);
            rowIndex += 1;
        }

        return CreateBuffer(GetRgbBgrOutputFormat(buffer.Format), buffer.Width, buffer.Height, outputStride, output);
    }

    internal static PngPixelBuffer SwapRgbBgr16(PngPixelBuffer buffer)
    {
        int bytesPerPixel = GetBytesPerPixel(buffer.Format);
        int outputStride = CalculateOutputStride(buffer.Width, bytesPerPixel);
        byte[] output = CreateOutputData(outputStride, buffer.Height);

        int rowIndex = 0;
        while (rowIndex < buffer.Height)
        {
            SwapRgbBgr16Row(buffer, rowIndex, bytesPerPixel, output, outputStride);
            rowIndex += 1;
        }

        return CreateBuffer(GetRgbBgrOutputFormat(buffer.Format), buffer.Width, buffer.Height, outputStride, output);
    }

    internal static PngPixelBuffer SwapRgbaArgb8(PngPixelBuffer buffer)
    {
        int bytesPerPixel = GetRgbaBytesPerPixel(buffer.Format);
        int outputStride = CalculateOutputStride(buffer.Width, bytesPerPixel);
        byte[] output = CreateOutputData(outputStride, buffer.Height);

        int rowIndex = 0;
        while (rowIndex < buffer.Height)
        {
            SwapRgbaArgb8Row(buffer, rowIndex, output, outputStride);
            rowIndex += 1;
        }

        return CreateBuffer(GetRgbaArgbOutputFormat(buffer.Format), buffer.Width, buffer.Height, outputStride, output);
    }

    internal static PngPixelBuffer SwapRgbaArgb16(PngPixelBuffer buffer)
    {
        int bytesPerPixel = GetRgbaBytesPerPixel(buffer.Format);
        int outputStride = CalculateOutputStride(buffer.Width, bytesPerPixel);
        byte[] output = CreateOutputData(outputStride, buffer.Height);

        int rowIndex = 0;
        while (rowIndex < buffer.Height)
        {
            SwapRgbaArgb16Row(buffer, rowIndex, output, outputStride);
            rowIndex += 1;
        }

        return CreateBuffer(GetRgbaArgbOutputFormat(buffer.Format), buffer.Width, buffer.Height, outputStride, output);
    }

    internal static void SwapRgbBgr8Row(PngPixelBuffer buffer, int rowIndex, int bytesPerPixel, byte[] output, int outputStride)
    {
        int sourceOffset = buffer.Stride * rowIndex;
        int destinationOffset = outputStride * rowIndex;
        int x = 0;
        while (x < buffer.Width)
        {
            output[destinationOffset] = buffer.Data[sourceOffset + 2];
            output[destinationOffset + 1] = buffer.Data[sourceOffset + 1];
            output[destinationOffset + 2] = buffer.Data[sourceOffset];
            CopyAlpha8(buffer, sourceOffset, output, destinationOffset);
            sourceOffset += bytesPerPixel;
            destinationOffset += bytesPerPixel;
            x += 1;
        }
    }

    internal static void SwapRgbBgr16Row(PngPixelBuffer buffer, int rowIndex, int bytesPerPixel, byte[] output, int outputStride)
    {
        int sourceOffset = buffer.Stride * rowIndex;
        int destinationOffset = outputStride * rowIndex;
        int x = 0;
        while (x < buffer.Width)
        {
            output[destinationOffset] = buffer.Data[sourceOffset + 4];
            output[destinationOffset + 1] = buffer.Data[sourceOffset + 5];
            output[destinationOffset + 2] = buffer.Data[sourceOffset + 2];
            output[destinationOffset + 3] = buffer.Data[sourceOffset + 3];
            output[destinationOffset + 4] = buffer.Data[sourceOffset];
            output[destinationOffset + 5] = buffer.Data[sourceOffset + 1];
            CopyAlpha16(buffer, sourceOffset, output, destinationOffset);
            sourceOffset += bytesPerPixel;
            destinationOffset += bytesPerPixel;
            x += 1;
        }
    }

    internal static void SwapRgbaArgb8Row(PngPixelBuffer buffer, int rowIndex, byte[] output, int outputStride)
    {
        int sourceOffset = buffer.Stride * rowIndex;
        int destinationOffset = outputStride * rowIndex;
        int x = 0;
        while (x < buffer.Width)
        {
            if (buffer.Format == PngPixelFormat.Argb8)
            {
                output[destinationOffset] = buffer.Data[sourceOffset + 1];
                output[destinationOffset + 1] = buffer.Data[sourceOffset + 2];
                output[destinationOffset + 2] = buffer.Data[sourceOffset + 3];
                output[destinationOffset + 3] = buffer.Data[sourceOffset];
            }
            else
            {
                output[destinationOffset] = buffer.Data[sourceOffset + 3];
                output[destinationOffset + 1] = buffer.Data[sourceOffset];
                output[destinationOffset + 2] = buffer.Data[sourceOffset + 1];
                output[destinationOffset + 3] = buffer.Data[sourceOffset + 2];
            }

            sourceOffset += 4;
            destinationOffset += 4;
            x += 1;
        }
    }

    internal static void SwapRgbaArgb16Row(PngPixelBuffer buffer, int rowIndex, byte[] output, int outputStride)
    {
        int sourceOffset = buffer.Stride * rowIndex;
        int destinationOffset = outputStride * rowIndex;
        int x = 0;
        while (x < buffer.Width)
        {
            if (buffer.Format == PngPixelFormat.Argb16)
            {
                output[destinationOffset] = buffer.Data[sourceOffset + 2];
                output[destinationOffset + 1] = buffer.Data[sourceOffset + 3];
                output[destinationOffset + 2] = buffer.Data[sourceOffset + 4];
                output[destinationOffset + 3] = buffer.Data[sourceOffset + 5];
                output[destinationOffset + 4] = buffer.Data[sourceOffset + 6];
                output[destinationOffset + 5] = buffer.Data[sourceOffset + 7];
                output[destinationOffset + 6] = buffer.Data[sourceOffset];
                output[destinationOffset + 7] = buffer.Data[sourceOffset + 1];
            }
            else
            {
                output[destinationOffset] = buffer.Data[sourceOffset + 6];
                output[destinationOffset + 1] = buffer.Data[sourceOffset + 7];
                output[destinationOffset + 2] = buffer.Data[sourceOffset];
                output[destinationOffset + 3] = buffer.Data[sourceOffset + 1];
                output[destinationOffset + 4] = buffer.Data[sourceOffset + 2];
                output[destinationOffset + 5] = buffer.Data[sourceOffset + 3];
                output[destinationOffset + 6] = buffer.Data[sourceOffset + 4];
                output[destinationOffset + 7] = buffer.Data[sourceOffset + 5];
            }

            sourceOffset += 8;
            destinationOffset += 8;
            x += 1;
        }
    }

    internal static void CopyAlpha8(PngPixelBuffer buffer, int sourceOffset, byte[] output, int destinationOffset)
    {
        if (buffer.Format == PngPixelFormat.Rgba8 || buffer.Format == PngPixelFormat.Bgra8)
        {
            output[destinationOffset + 3] = buffer.Data[sourceOffset + 3];
        }
    }

    internal static void CopyAlpha16(PngPixelBuffer buffer, int sourceOffset, byte[] output, int destinationOffset)
    {
        if (buffer.Format == PngPixelFormat.Rgba16 || buffer.Format == PngPixelFormat.Bgra16)
        {
            output[destinationOffset + 6] = buffer.Data[sourceOffset + 6];
            output[destinationOffset + 7] = buffer.Data[sourceOffset + 7];
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

        int bytesPerPixel = GetBytesPerPixel(buffer.Format);
        int rowBytes = CalculateOutputStride(buffer.Width, bytesPerPixel);
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

    internal static void ValidateRgbaBuffer(PngPixelBuffer buffer)
    {
        if (buffer.Width <= 0)
        {
            throw new ArgumentOutOfRangeException("Width");
        }

        if (buffer.Height <= 0)
        {
            throw new ArgumentOutOfRangeException("Height");
        }

        int bytesPerPixel = GetRgbaBytesPerPixel(buffer.Format);
        int rowBytes = CalculateOutputStride(buffer.Width, bytesPerPixel);
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

    internal static int GetBytesPerPixel(PngPixelFormat format)
    {
        switch (format)
        {
            case PngPixelFormat.Rgb8:
            case PngPixelFormat.Bgr8:
                return 3;
            case PngPixelFormat.Rgb16:
            case PngPixelFormat.Bgr16:
                return 6;
            case PngPixelFormat.Rgba8:
            case PngPixelFormat.Bgra8:
                return 4;
            case PngPixelFormat.Rgba16:
            case PngPixelFormat.Bgra16:
                return 8;
            default:
                throw new ArgumentOutOfRangeException("Format");
        }
    }

    internal static int GetRgbaBytesPerPixel(PngPixelFormat format)
    {
        switch (format)
        {
            case PngPixelFormat.Rgba8:
            case PngPixelFormat.Argb8:
                return 4;
            case PngPixelFormat.Rgba16:
            case PngPixelFormat.Argb16:
                return 8;
            default:
                throw new ArgumentOutOfRangeException("Format");
        }
    }

    internal static PngPixelFormat GetRgbBgrOutputFormat(PngPixelFormat format)
    {
        switch (format)
        {
            case PngPixelFormat.Rgb8:
                return PngPixelFormat.Bgr8;
            case PngPixelFormat.Bgr8:
                return PngPixelFormat.Rgb8;
            case PngPixelFormat.Rgb16:
                return PngPixelFormat.Bgr16;
            case PngPixelFormat.Bgr16:
                return PngPixelFormat.Rgb16;
            case PngPixelFormat.Rgba8:
                return PngPixelFormat.Bgra8;
            case PngPixelFormat.Bgra8:
                return PngPixelFormat.Rgba8;
            case PngPixelFormat.Rgba16:
                return PngPixelFormat.Bgra16;
            case PngPixelFormat.Bgra16:
                return PngPixelFormat.Rgba16;
            default:
                throw new ArgumentOutOfRangeException("Format");
        }
    }

    internal static PngPixelFormat GetRgbaArgbOutputFormat(PngPixelFormat format)
    {
        switch (format)
        {
            case PngPixelFormat.Rgba8:
                return PngPixelFormat.Argb8;
            case PngPixelFormat.Argb8:
                return PngPixelFormat.Rgba8;
            case PngPixelFormat.Rgba16:
                return PngPixelFormat.Argb16;
            case PngPixelFormat.Argb16:
                return PngPixelFormat.Rgba16;
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
}
