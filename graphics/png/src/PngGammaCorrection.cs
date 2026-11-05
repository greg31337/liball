namespace png;

/// <summary>
/// Builds gamma lookup tables and applies them to 8-bit or 16-bit color samples while leaving alpha unchanged.
/// </summary>
internal static class PngGammaCorrection
{
    internal const int Table8BitLength = 256;
    internal const int Table16BitLength = 65536;
    internal const double PngGammaScale = 100000.0;

    /// <summary>
    /// Builds a 256-entry table using the image, display, and optional user gamma exponents.
    /// </summary>
    /// <param name="gamma">The PNG image-gamma value.</param>
    /// <param name="displayExponent">The positive finite display exponent.</param>
    /// <returns>The resulting encoded or transformed bytes.</returns>
    public static byte[] Build8BitTable(PngGamma gamma, double displayExponent)
    {
        ArgumentNullException.ThrowIfNull(gamma);

        return Build8BitTable(gamma.Value, displayExponent);
    }

    /// <summary>
    /// Builds a 256-entry table using the image, display, and optional user gamma exponents.
    /// </summary>
    /// <param name="pngGammaValue">The PNG gamma integer scaled by 100000.</param>
    /// <param name="displayExponent">The positive finite display exponent.</param>
    /// <returns>The resulting encoded or transformed bytes.</returns>
    public static byte[] Build8BitTable(uint pngGammaValue, double displayExponent)
    {
        if (pngGammaValue == 0)
        {
            throw new ArgumentOutOfRangeException("pngGammaValue");
        }

        double imageGamma = pngGammaValue / PngGammaScale;
        return Build8BitTable(imageGamma, displayExponent);
    }

    /// <summary>
    /// Builds a 256-entry table using the image, display, and optional user gamma exponents.
    /// </summary>
    /// <param name="imageGamma">The image gamma as a positive finite value.</param>
    /// <param name="displayExponent">The positive finite display exponent.</param>
    /// <returns>The resulting encoded or transformed bytes.</returns>
    public static byte[] Build8BitTable(double imageGamma, double displayExponent)
    {
        return Build8BitTable(imageGamma, displayExponent, 1.0);
    }

    /// <summary>
    /// Builds a 256-entry table using the image, display, and optional user gamma exponents.
    /// </summary>
    /// <param name="imageGamma">The image gamma as a positive finite value.</param>
    /// <param name="displayExponent">The positive finite display exponent.</param>
    /// <param name="userExponent">An additional positive finite correction exponent.</param>
    /// <returns>The resulting encoded or transformed bytes.</returns>
    public static byte[] Build8BitTable(double imageGamma, double displayExponent, double userExponent)
    {
        ValidatePositiveFinite(imageGamma, "imageGamma");
        ValidatePositiveFinite(displayExponent, "displayExponent");
        ValidatePositiveFinite(userExponent, "userExponent");

        double decodingExponent = userExponent / (imageGamma * displayExponent);
        byte[] table = new byte[Table8BitLength];
        int index = 0;
        while (index < table.Length)
        {
            table[index] = Calculate8BitOutput(index, decodingExponent);
            index += 1;
        }

        return table;
    }

    /// <summary>
    /// Builds a 256-entry table that maps every 8-bit sample to itself.
    /// </summary>
    /// <returns>The resulting encoded or transformed bytes.</returns>
    public static byte[] BuildIdentity8BitTable()
    {
        byte[] table = new byte[Table8BitLength];
        int index = 0;
        while (index < table.Length)
        {
            table[index] = (byte)index;
            index += 1;
        }

        return table;
    }

    /// <summary>
    /// Builds a 65536-entry table using the image, display, and optional user gamma exponents.
    /// </summary>
    /// <param name="gamma">The PNG image-gamma value.</param>
    /// <param name="displayExponent">The positive finite display exponent.</param>
    /// <returns>The resulting 16-bit lookup table.</returns>
    public static ushort[] Build16BitTable(PngGamma gamma, double displayExponent)
    {
        ArgumentNullException.ThrowIfNull(gamma);

        return Build16BitTable(gamma.Value, displayExponent);
    }

    /// <summary>
    /// Builds a 65536-entry table using the image, display, and optional user gamma exponents.
    /// </summary>
    /// <param name="pngGammaValue">The PNG gamma integer scaled by 100000.</param>
    /// <param name="displayExponent">The positive finite display exponent.</param>
    /// <returns>The resulting 16-bit lookup table.</returns>
    public static ushort[] Build16BitTable(uint pngGammaValue, double displayExponent)
    {
        if (pngGammaValue == 0)
        {
            throw new ArgumentOutOfRangeException("pngGammaValue");
        }

        double imageGamma = pngGammaValue / PngGammaScale;
        return Build16BitTable(imageGamma, displayExponent);
    }

    /// <summary>
    /// Builds a 65536-entry table using the image, display, and optional user gamma exponents.
    /// </summary>
    /// <param name="imageGamma">The image gamma as a positive finite value.</param>
    /// <param name="displayExponent">The positive finite display exponent.</param>
    /// <returns>The resulting 16-bit lookup table.</returns>
    public static ushort[] Build16BitTable(double imageGamma, double displayExponent)
    {
        return Build16BitTable(imageGamma, displayExponent, 1.0);
    }

    /// <summary>
    /// Builds a 65536-entry table using the image, display, and optional user gamma exponents.
    /// </summary>
    /// <param name="imageGamma">The image gamma as a positive finite value.</param>
    /// <param name="displayExponent">The positive finite display exponent.</param>
    /// <param name="userExponent">An additional positive finite correction exponent.</param>
    /// <returns>The resulting 16-bit lookup table.</returns>
    public static ushort[] Build16BitTable(double imageGamma, double displayExponent, double userExponent)
    {
        ValidatePositiveFinite(imageGamma, "imageGamma");
        ValidatePositiveFinite(displayExponent, "displayExponent");
        ValidatePositiveFinite(userExponent, "userExponent");

        double decodingExponent = userExponent / (imageGamma * displayExponent);
        ushort[] table = new ushort[Table16BitLength];
        int index = 0;
        while (index < table.Length)
        {
            table[index] = Calculate16BitOutput(index, decodingExponent);
            index += 1;
        }

        return table;
    }

    /// <summary>
    /// Builds a 65536-entry table that maps every 16-bit sample to itself.
    /// </summary>
    /// <returns>The resulting 16-bit lookup table.</returns>
    public static ushort[] BuildIdentity16BitTable()
    {
        ushort[] table = new ushort[Table16BitLength];
        int index = 0;
        while (index < table.Length)
        {
            table[index] = (ushort)index;
            index += 1;
        }

        return table;
    }

    /// <summary>
    /// Maps each 8-bit color sample through the supplied table, preserves alpha, and returns a new buffer.
    /// </summary>
    /// <param name="buffer">The source pixel buffer.</param>
    /// <param name="table">The lookup table to apply to color samples.</param>
    /// <returns>A newly allocated pixel buffer containing the result.</returns>
    public static PngPixelBuffer Apply8BitTable(PngPixelBuffer buffer, byte[] table)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentNullException.ThrowIfNull(buffer.Data);
        ArgumentNullException.ThrowIfNull(table);

        if (PngReorderedPixelTransformer.IsReordered(buffer.Format))
        {
            PngPixelBuffer transformed = Apply8BitTable(PngReorderedPixelTransformer.Normalize(buffer), table);
            return PngReorderedPixelTransformer.Restore(buffer.Format, transformed);
        }

        if (PngGrayscaleExpander.IsLowBitGrayscaleFormat(buffer.Format))
        {
            return Apply8BitTable(PngGrayscaleExpander.ExpandLowBitToGrayscale8(buffer), table);
        }

        ValidateTable(table);
        Validate8BitColorBuffer(buffer);

        int bytesPerPixel = Get8BitBytesPerPixel(buffer.Format);
        int outputStride = CalculateOutputStride(buffer.Width, bytesPerPixel);
        byte[] output = CreateOutputData(outputStride, buffer.Height);

        int rowIndex = 0;
        while (rowIndex < buffer.Height)
        {
            Apply8BitTableRow(buffer, rowIndex, table, output, outputStride);
            rowIndex += 1;
        }

        return CreateBuffer(buffer.Format, buffer.Width, buffer.Height, outputStride, output);
    }

    /// <summary>
    /// Maps each 16-bit color sample through the supplied table, preserves alpha, and returns a new buffer.
    /// </summary>
    /// <param name="buffer">The source pixel buffer.</param>
    /// <param name="table">The lookup table to apply to color samples.</param>
    /// <returns>A newly allocated pixel buffer containing the result.</returns>
    public static PngPixelBuffer Apply16BitTable(PngPixelBuffer buffer, ushort[] table)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentNullException.ThrowIfNull(buffer.Data);
        ArgumentNullException.ThrowIfNull(table);

        if (PngReorderedPixelTransformer.IsReordered(buffer.Format))
        {
            PngPixelBuffer transformed = Apply16BitTable(PngReorderedPixelTransformer.Normalize(buffer), table);
            return PngReorderedPixelTransformer.Restore(buffer.Format, transformed);
        }

        ValidateTable(table);
        Validate16BitColorBuffer(buffer);

        int bytesPerPixel = Get16BitBytesPerPixel(buffer.Format);
        int outputStride = CalculateOutputStride(buffer.Width, bytesPerPixel);
        byte[] output = CreateOutputData(outputStride, buffer.Height);

        int rowIndex = 0;
        while (rowIndex < buffer.Height)
        {
            Apply16BitTableRow(buffer, rowIndex, table, output, outputStride);
            rowIndex += 1;
        }

        return CreateBuffer(buffer.Format, buffer.Width, buffer.Height, outputStride, output);
    }

    internal static void Apply8BitTableRow(PngPixelBuffer buffer, int rowIndex, byte[] table, byte[] output, int outputStride)
    {
        int bytesPerPixel = Get8BitBytesPerPixel(buffer.Format);
        int sourceOffset = buffer.Stride * rowIndex;
        int destinationOffset = outputStride * rowIndex;
        int x = 0;
        while (x < buffer.Width)
        {
            Apply8BitTablePixel(buffer, sourceOffset, table, output, destinationOffset);
            sourceOffset += bytesPerPixel;
            destinationOffset += bytesPerPixel;
            x += 1;
        }
    }

    internal static void Apply8BitTablePixel(PngPixelBuffer buffer, int sourceOffset, byte[] table, byte[] output, int destinationOffset)
    {
        switch (buffer.Format)
        {
            case PngPixelFormat.Grayscale8:
                output[destinationOffset] = table[buffer.Data[sourceOffset]];
                break;
            case PngPixelFormat.GrayscaleAlpha8:
                output[destinationOffset] = table[buffer.Data[sourceOffset]];
                output[destinationOffset + 1] = buffer.Data[sourceOffset + 1];
                break;
            case PngPixelFormat.Rgb8:
                output[destinationOffset] = table[buffer.Data[sourceOffset]];
                output[destinationOffset + 1] = table[buffer.Data[sourceOffset + 1]];
                output[destinationOffset + 2] = table[buffer.Data[sourceOffset + 2]];
                break;
            case PngPixelFormat.Rgba8:
                output[destinationOffset] = table[buffer.Data[sourceOffset]];
                output[destinationOffset + 1] = table[buffer.Data[sourceOffset + 1]];
                output[destinationOffset + 2] = table[buffer.Data[sourceOffset + 2]];
                output[destinationOffset + 3] = buffer.Data[sourceOffset + 3];
                break;
            default:
                throw new ArgumentOutOfRangeException("Format");
        }
    }

    internal static void Apply16BitTableRow(PngPixelBuffer buffer, int rowIndex, ushort[] table, byte[] output, int outputStride)
    {
        int bytesPerPixel = Get16BitBytesPerPixel(buffer.Format);
        int sourceOffset = buffer.Stride * rowIndex;
        int destinationOffset = outputStride * rowIndex;
        int x = 0;
        while (x < buffer.Width)
        {
            Apply16BitTablePixel(buffer, sourceOffset, table, output, destinationOffset);
            sourceOffset += bytesPerPixel;
            destinationOffset += bytesPerPixel;
            x += 1;
        }
    }

    internal static void Apply16BitTablePixel(PngPixelBuffer buffer, int sourceOffset, ushort[] table, byte[] output, int destinationOffset)
    {
        switch (buffer.Format)
        {
            case PngPixelFormat.Grayscale16:
                WriteTableSample16(output, destinationOffset, table, PngBinaryReader.ReadUInt16(buffer.Data, sourceOffset));
                break;
            case PngPixelFormat.GrayscaleAlpha16:
                WriteTableSample16(output, destinationOffset, table, PngBinaryReader.ReadUInt16(buffer.Data, sourceOffset));
                PngBinaryWriter.WriteUInt16(output, destinationOffset + 2, PngBinaryReader.ReadUInt16(buffer.Data, sourceOffset + 2));
                break;
            case PngPixelFormat.Rgb16:
                WriteTableSample16(output, destinationOffset, table, PngBinaryReader.ReadUInt16(buffer.Data, sourceOffset));
                WriteTableSample16(output, destinationOffset + 2, table, PngBinaryReader.ReadUInt16(buffer.Data, sourceOffset + 2));
                WriteTableSample16(output, destinationOffset + 4, table, PngBinaryReader.ReadUInt16(buffer.Data, sourceOffset + 4));
                break;
            case PngPixelFormat.Rgba16:
                WriteTableSample16(output, destinationOffset, table, PngBinaryReader.ReadUInt16(buffer.Data, sourceOffset));
                WriteTableSample16(output, destinationOffset + 2, table, PngBinaryReader.ReadUInt16(buffer.Data, sourceOffset + 2));
                WriteTableSample16(output, destinationOffset + 4, table, PngBinaryReader.ReadUInt16(buffer.Data, sourceOffset + 4));
                PngBinaryWriter.WriteUInt16(output, destinationOffset + 6, PngBinaryReader.ReadUInt16(buffer.Data, sourceOffset + 6));
                break;
            default:
                throw new ArgumentOutOfRangeException("Format");
        }
    }

    internal static void WriteTableSample16(byte[] output, int destinationOffset, ushort[] table, ushort sample)
    {
        PngBinaryWriter.WriteUInt16(output, destinationOffset, table[sample]);
    }

    internal static byte Calculate8BitOutput(int sample, double decodingExponent)
    {
        if (sample == 0 || sample == 255)
        {
            return (byte)sample;
        }

        double normalized = sample / 255.0;
        double corrected = Math.Pow(normalized, decodingExponent);
        int rounded = (int)(corrected * 255.0 + 0.5);
        if (rounded < 0)
        {
            return 0;
        }

        if (rounded > 255)
        {
            return 255;
        }

        return (byte)rounded;
    }

    internal static ushort Calculate16BitOutput(int sample, double decodingExponent)
    {
        if (sample == 0 || sample == 65535)
        {
            return (ushort)sample;
        }

        double normalized = sample / 65535.0;
        double corrected = Math.Pow(normalized, decodingExponent);
        int rounded = (int)(corrected * 65535.0 + 0.5);
        if (rounded < 0)
        {
            return 0;
        }

        if (rounded > 65535)
        {
            return 65535;
        }

        return (ushort)rounded;
    }

    internal static void ValidatePositiveFinite(double value, string name)
    {
        if (value <= 0.0 || double.IsNaN(value) || double.IsInfinity(value))
        {
            throw new ArgumentOutOfRangeException(name);
        }
    }

    internal static void ValidateTable(byte[] table)
    {
        if (table.Length != Table8BitLength)
        {
            throw new ArgumentOutOfRangeException("table");
        }
    }

    internal static void ValidateTable(ushort[] table)
    {
        if (table.Length != Table16BitLength)
        {
            throw new ArgumentOutOfRangeException("table");
        }
    }

    internal static void Validate8BitColorBuffer(PngPixelBuffer buffer)
    {
        if (buffer.Width <= 0)
        {
            throw new ArgumentOutOfRangeException("Width");
        }

        if (buffer.Height <= 0)
        {
            throw new ArgumentOutOfRangeException("Height");
        }

        int bytesPerPixel = Get8BitBytesPerPixel(buffer.Format);
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

    internal static void Validate16BitColorBuffer(PngPixelBuffer buffer)
    {
        if (buffer.Width <= 0)
        {
            throw new ArgumentOutOfRangeException("Width");
        }

        if (buffer.Height <= 0)
        {
            throw new ArgumentOutOfRangeException("Height");
        }

        int bytesPerPixel = Get16BitBytesPerPixel(buffer.Format);
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

    internal static int Get8BitBytesPerPixel(PngPixelFormat format)
    {
        switch (format)
        {
            case PngPixelFormat.Grayscale8:
                return 1;
            case PngPixelFormat.GrayscaleAlpha8:
                return 2;
            case PngPixelFormat.Rgb8:
                return 3;
            case PngPixelFormat.Rgba8:
                return 4;
            default:
                throw new ArgumentOutOfRangeException("Format");
        }
    }

    internal static int Get16BitBytesPerPixel(PngPixelFormat format)
    {
        switch (format)
        {
            case PngPixelFormat.Grayscale16:
                return 2;
            case PngPixelFormat.GrayscaleAlpha16:
                return 4;
            case PngPixelFormat.Rgb16:
                return 6;
            case PngPixelFormat.Rgba16:
                return 8;
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
