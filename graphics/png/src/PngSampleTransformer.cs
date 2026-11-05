namespace png;

/// <summary>
/// Provides byte-order conversion, lossless depth reduction, and significant-bit scaling for samples.
/// </summary>
internal static class PngSampleTransformer
{
    /// <summary>
    /// Reverses the two bytes of every 16-bit sample and returns a new buffer.
    /// </summary>
    /// <param name="buffer">The source pixel buffer.</param>
    /// <returns>A newly allocated pixel buffer containing the result.</returns>
    public static PngPixelBuffer ByteSwap16BitSamples(PngPixelBuffer buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentNullException.ThrowIfNull(buffer.Data);

        if (PngReorderedPixelTransformer.IsReordered(buffer.Format))
        {
            PngPixelBuffer transformed = ByteSwap16BitSamples(PngReorderedPixelTransformer.Normalize(buffer));
            return PngReorderedPixelTransformer.Restore(buffer.Format, transformed);
        }

        ValidateBuffer(buffer);

        int bytesPerPixel = GetBytesPerPixel(buffer.Format);
        int outputStride = CalculateOutputStride(buffer.Width, bytesPerPixel);
        byte[] output = CreateOutputData(outputStride, buffer.Height);

        int rowIndex = 0;
        while (rowIndex < buffer.Height)
        {
            ByteSwap16BitRow(buffer, rowIndex, outputStride, output);
            rowIndex += 1;
        }

        return CreateBuffer(buffer.Format, buffer.Width, buffer.Height, outputStride, output);
    }

    /// <summary>
    /// Returns an 8-bit buffer when every 16-bit sample repeats the same byte; otherwise returns a 16-bit copy.
    /// </summary>
    /// <param name="buffer">The source pixel buffer.</param>
    /// <returns>A newly allocated pixel buffer containing the result.</returns>
    public static PngPixelBuffer Reduce16BitTo8BitIfLossless(PngPixelBuffer buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentNullException.ThrowIfNull(buffer.Data);

        if (PngReorderedPixelTransformer.IsReordered(buffer.Format))
        {
            PngPixelBuffer transformed = Reduce16BitTo8BitIfLossless(PngReorderedPixelTransformer.Normalize(buffer));
            return PngReorderedPixelTransformer.Restore(buffer.Format, transformed);
        }

        ValidateBuffer(buffer);
        if (CanReduce16BitTo8BitCore(buffer))
        {
            return Reduce16BitTo8BitCore(buffer);
        }

        return Copy16BitBufferCore(buffer);
    }

    /// <summary>
    /// Determines whether every 16-bit sample can be represented losslessly by its high byte.
    /// </summary>
    /// <param name="buffer">The source pixel buffer.</param>
    /// <returns>True when the requested condition is met; otherwise false.</returns>
    public static bool CanReduce16BitTo8Bit(PngPixelBuffer buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentNullException.ThrowIfNull(buffer.Data);

        if (PngReorderedPixelTransformer.IsReordered(buffer.Format))
        {
            return CanReduce16BitTo8Bit(PngReorderedPixelTransformer.Normalize(buffer));
        }

        ValidateBuffer(buffer);
        return CanReduce16BitTo8BitCore(buffer);
    }

    /// <summary>
    /// Discards insignificant low-order sample bits and returns a new buffer.
    /// </summary>
    /// <param name="buffer">The source pixel buffer.</param>
    /// <param name="bits">The significant-bit counts for the buffer channels.</param>
    /// <returns>A newly allocated pixel buffer containing the result.</returns>
    public static PngPixelBuffer DownshiftToSignificantBits(PngPixelBuffer buffer, PngSignificantBits bits)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentNullException.ThrowIfNull(buffer.Data);
        ArgumentNullException.ThrowIfNull(bits);

        if (PngReorderedPixelTransformer.IsReordered(buffer.Format))
        {
            PngPixelBuffer transformed = DownshiftToSignificantBits(PngReorderedPixelTransformer.Normalize(buffer), bits);
            return PngReorderedPixelTransformer.Restore(buffer.Format, transformed);
        }

        if (PngGrayscaleExpander.IsLowBitGrayscaleFormat(buffer.Format))
        {
            return ScalePackedSignificantBits(buffer, bits, true);
        }

        ValidateSignificantBitsBuffer(buffer, bits);
        return ScaleSignificantBits(buffer, bits, true);
    }

    /// <summary>
    /// Replicates significant bits across each full sample and returns a new buffer.
    /// </summary>
    /// <param name="buffer">The source pixel buffer.</param>
    /// <param name="bits">The significant-bit counts for the buffer channels.</param>
    /// <returns>A newly allocated pixel buffer containing the result.</returns>
    public static PngPixelBuffer UpscaleFromSignificantBits(PngPixelBuffer buffer, PngSignificantBits bits)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentNullException.ThrowIfNull(buffer.Data);
        ArgumentNullException.ThrowIfNull(bits);

        if (PngReorderedPixelTransformer.IsReordered(buffer.Format))
        {
            PngPixelBuffer transformed = UpscaleFromSignificantBits(PngReorderedPixelTransformer.Normalize(buffer), bits);
            return PngReorderedPixelTransformer.Restore(buffer.Format, transformed);
        }

        if (PngGrayscaleExpander.IsLowBitGrayscaleFormat(buffer.Format))
        {
            return ScalePackedSignificantBits(buffer, bits, false);
        }

        ValidateSignificantBitsBuffer(buffer, bits);
        return ScaleSignificantBits(buffer, bits, false);
    }

    internal static PngPixelBuffer Reduce16BitTo8BitCore(PngPixelBuffer buffer)
    {
        PngPixelFormat outputFormat = Get8BitOutputFormat(buffer.Format);
        int outputBytesPerPixel = Get8BitBytesPerPixel(outputFormat);
        int outputStride = CalculateOutputStride(buffer.Width, outputBytesPerPixel);
        byte[] output = CreateOutputData(outputStride, buffer.Height);

        int rowIndex = 0;
        while (rowIndex < buffer.Height)
        {
            Reduce16BitTo8BitRow(buffer, rowIndex, output, outputStride);
            rowIndex += 1;
        }

        return CreateBuffer(outputFormat, buffer.Width, buffer.Height, outputStride, output);
    }

    internal static PngPixelBuffer Copy16BitBufferCore(PngPixelBuffer buffer)
    {
        int bytesPerPixel = GetBytesPerPixel(buffer.Format);
        int outputStride = CalculateOutputStride(buffer.Width, bytesPerPixel);
        byte[] output = CreateOutputData(outputStride, buffer.Height);

        int rowIndex = 0;
        while (rowIndex < buffer.Height)
        {
            CopyPixelRow(buffer, rowIndex, output, outputStride, outputStride);
            rowIndex += 1;
        }

        return CreateBuffer(buffer.Format, buffer.Width, buffer.Height, outputStride, output);
    }

    internal static PngPixelBuffer ScaleSignificantBits(PngPixelBuffer buffer, PngSignificantBits bits, bool downshift)
    {
        int bytesPerPixel = GetSignificantBitsBytesPerPixel(buffer.Format);
        int outputStride = CalculateOutputStride(buffer.Width, bytesPerPixel);
        byte[] output = CreateOutputData(outputStride, buffer.Height);

        int rowIndex = 0;
        while (rowIndex < buffer.Height)
        {
            if (Is16BitFormat(buffer.Format))
            {
                ScaleSignificantBits16Row(buffer, rowIndex, bits, downshift, output, outputStride);
            }
            else
            {
                ScaleSignificantBits8Row(buffer, rowIndex, bits, downshift, output, outputStride);
            }

            rowIndex += 1;
        }

        return CreateBuffer(buffer.Format, buffer.Width, buffer.Height, outputStride, output);
    }

    internal static PngPixelBuffer ScalePackedSignificantBits(PngPixelBuffer buffer, PngSignificantBits bits, bool downshift)
    {
        PngBitDepth bitDepth = PngMonochromeTransformer.GetMonochromeBitDepth(buffer.Format);
        PngMonochromeTransformer.ValidateBuffer(buffer, bitDepth);
        int sampleBits = PngSizeCalculator.GetBitDepthValue(bitDepth);
        ValidateSignificantBitValue(bits.Gray, sampleBits, "Gray");
        int outputStride = (int)PngSizeCalculator.CalculateFrameRowBytes(buffer.Width, buffer.Format);
        byte[] output = CreateOutputData(outputStride, buffer.Height);
        int rowIndex = 0;
        while (rowIndex < buffer.Height)
        {
            int sourceOffset = buffer.Stride * rowIndex;
            int destinationOffset = outputStride * rowIndex;
            byte[] samples = PngPackedSampleUnpacker.Unpack(buffer.Data, sourceOffset, buffer.Width, bitDepth);
            int sampleIndex = 0;
            while (sampleIndex < samples.Length)
            {
                if (downshift)
                {
                    samples[sampleIndex] = (byte)(samples[sampleIndex] >> (sampleBits - bits.Gray));
                }
                else
                {
                    samples[sampleIndex] = (byte)ScaleSampleToFullRange(samples[sampleIndex], bits.Gray, sampleBits);
                }

                sampleIndex += 1;
            }

            PngPackedSampleWriter.PackInto(output, destinationOffset, samples, 0, samples.Length, bitDepth);
            rowIndex += 1;
        }

        return CreateBuffer(buffer.Format, buffer.Width, buffer.Height, outputStride, output);
    }

    internal static void ByteSwap16BitRow(PngPixelBuffer buffer, int rowIndex, int outputStride, byte[] output)
    {
        int bytesPerPixel = GetBytesPerPixel(buffer.Format);
        int rowBytes = CalculateOutputStride(buffer.Width, bytesPerPixel);
        int sourceOffset = buffer.Stride * rowIndex;
        int destinationOffset = outputStride * rowIndex;
        int offset = 0;
        while (offset < rowBytes)
        {
            output[destinationOffset + offset] = buffer.Data[sourceOffset + offset + 1];
            output[destinationOffset + offset + 1] = buffer.Data[sourceOffset + offset];
            offset += 2;
        }
    }

    internal static void Reduce16BitTo8BitRow(PngPixelBuffer buffer, int rowIndex, byte[] output, int outputStride)
    {
        int inputBytesPerPixel = GetBytesPerPixel(buffer.Format);
        int outputBytesPerPixel = Get8BitBytesPerPixel(Get8BitOutputFormat(buffer.Format));
        int sourceOffset = buffer.Stride * rowIndex;
        int destinationOffset = outputStride * rowIndex;
        int x = 0;
        while (x < buffer.Width)
        {
            Reduce16BitTo8BitPixel(buffer, sourceOffset, inputBytesPerPixel, output, destinationOffset);
            sourceOffset += inputBytesPerPixel;
            destinationOffset += outputBytesPerPixel;
            x += 1;
        }
    }

    internal static void Reduce16BitTo8BitPixel(PngPixelBuffer buffer, int sourceOffset, int inputBytesPerPixel, byte[] output, int destinationOffset)
    {
        int sampleOffset = 0;
        int outputOffset = 0;
        while (sampleOffset < inputBytesPerPixel)
        {
            output[destinationOffset + outputOffset] = buffer.Data[sourceOffset + sampleOffset];
            sampleOffset += 2;
            outputOffset += 1;
        }
    }

    internal static void CopyPixelRow(PngPixelBuffer buffer, int rowIndex, byte[] output, int outputStride, int rowBytes)
    {
        int sourceOffset = buffer.Stride * rowIndex;
        int destinationOffset = outputStride * rowIndex;
        CopyBytes(buffer.Data, sourceOffset, output, destinationOffset, rowBytes);
    }

    internal static bool CanReduce16BitTo8BitCore(PngPixelBuffer buffer)
    {
        int rowIndex = 0;
        while (rowIndex < buffer.Height)
        {
            if (!CanReduce16BitTo8BitRow(buffer, rowIndex))
            {
                return false;
            }

            rowIndex += 1;
        }

        return true;
    }

    internal static bool CanReduce16BitTo8BitRow(PngPixelBuffer buffer, int rowIndex)
    {
        int bytesPerPixel = GetBytesPerPixel(buffer.Format);
        int rowBytes = CalculateOutputStride(buffer.Width, bytesPerPixel);
        int sourceOffset = buffer.Stride * rowIndex;
        int offset = 0;
        while (offset < rowBytes)
        {
            if (buffer.Data[sourceOffset + offset] != buffer.Data[sourceOffset + offset + 1])
            {
                return false;
            }

            offset += 2;
        }

        return true;
    }

    internal static void ScaleSignificantBits8Row(PngPixelBuffer buffer, int rowIndex, PngSignificantBits bits, bool downshift, byte[] output, int outputStride)
    {
        int bytesPerPixel = GetSignificantBitsBytesPerPixel(buffer.Format);
        int sourceOffset = buffer.Stride * rowIndex;
        int destinationOffset = outputStride * rowIndex;
        int x = 0;
        while (x < buffer.Width)
        {
            ScaleSignificantBits8Pixel(buffer, sourceOffset, bits, downshift, output, destinationOffset);
            sourceOffset += bytesPerPixel;
            destinationOffset += bytesPerPixel;
            x += 1;
        }
    }

    internal static void ScaleSignificantBits16Row(PngPixelBuffer buffer, int rowIndex, PngSignificantBits bits, bool downshift, byte[] output, int outputStride)
    {
        int bytesPerPixel = GetSignificantBitsBytesPerPixel(buffer.Format);
        int sourceOffset = buffer.Stride * rowIndex;
        int destinationOffset = outputStride * rowIndex;
        int x = 0;
        while (x < buffer.Width)
        {
            ScaleSignificantBits16Pixel(buffer, sourceOffset, bits, downshift, output, destinationOffset);
            sourceOffset += bytesPerPixel;
            destinationOffset += bytesPerPixel;
            x += 1;
        }
    }

    internal static void ScaleSignificantBits8Pixel(PngPixelBuffer buffer, int sourceOffset, PngSignificantBits bits, bool downshift, byte[] output, int destinationOffset)
    {
        switch (buffer.Format)
        {
            case PngPixelFormat.Grayscale8:
                output[destinationOffset] = ScaleSample8(buffer.Data[sourceOffset], bits.Gray, downshift);
                break;
            case PngPixelFormat.GrayscaleAlpha8:
                output[destinationOffset] = ScaleSample8(buffer.Data[sourceOffset], bits.Gray, downshift);
                output[destinationOffset + 1] = ScaleSample8(buffer.Data[sourceOffset + 1], bits.Alpha, downshift);
                break;
            case PngPixelFormat.Rgb8:
                output[destinationOffset] = ScaleSample8(buffer.Data[sourceOffset], bits.Red, downshift);
                output[destinationOffset + 1] = ScaleSample8(buffer.Data[sourceOffset + 1], bits.Green, downshift);
                output[destinationOffset + 2] = ScaleSample8(buffer.Data[sourceOffset + 2], bits.Blue, downshift);
                break;
            case PngPixelFormat.Rgba8:
                output[destinationOffset] = ScaleSample8(buffer.Data[sourceOffset], bits.Red, downshift);
                output[destinationOffset + 1] = ScaleSample8(buffer.Data[sourceOffset + 1], bits.Green, downshift);
                output[destinationOffset + 2] = ScaleSample8(buffer.Data[sourceOffset + 2], bits.Blue, downshift);
                output[destinationOffset + 3] = ScaleSample8(buffer.Data[sourceOffset + 3], bits.Alpha, downshift);
                break;
            default:
                throw new ArgumentOutOfRangeException("Format");
        }
    }

    internal static void ScaleSignificantBits16Pixel(PngPixelBuffer buffer, int sourceOffset, PngSignificantBits bits, bool downshift, byte[] output, int destinationOffset)
    {
        switch (buffer.Format)
        {
            case PngPixelFormat.Grayscale16:
                WriteScaledSample16(output, destinationOffset, PngBinaryReader.ReadUInt16(buffer.Data, sourceOffset), bits.Gray, downshift);
                break;
            case PngPixelFormat.GrayscaleAlpha16:
                WriteScaledSample16(output, destinationOffset, PngBinaryReader.ReadUInt16(buffer.Data, sourceOffset), bits.Gray, downshift);
                WriteScaledSample16(output, destinationOffset + 2, PngBinaryReader.ReadUInt16(buffer.Data, sourceOffset + 2), bits.Alpha, downshift);
                break;
            case PngPixelFormat.Rgb16:
                WriteScaledSample16(output, destinationOffset, PngBinaryReader.ReadUInt16(buffer.Data, sourceOffset), bits.Red, downshift);
                WriteScaledSample16(output, destinationOffset + 2, PngBinaryReader.ReadUInt16(buffer.Data, sourceOffset + 2), bits.Green, downshift);
                WriteScaledSample16(output, destinationOffset + 4, PngBinaryReader.ReadUInt16(buffer.Data, sourceOffset + 4), bits.Blue, downshift);
                break;
            case PngPixelFormat.Rgba16:
                WriteScaledSample16(output, destinationOffset, PngBinaryReader.ReadUInt16(buffer.Data, sourceOffset), bits.Red, downshift);
                WriteScaledSample16(output, destinationOffset + 2, PngBinaryReader.ReadUInt16(buffer.Data, sourceOffset + 2), bits.Green, downshift);
                WriteScaledSample16(output, destinationOffset + 4, PngBinaryReader.ReadUInt16(buffer.Data, sourceOffset + 4), bits.Blue, downshift);
                WriteScaledSample16(output, destinationOffset + 6, PngBinaryReader.ReadUInt16(buffer.Data, sourceOffset + 6), bits.Alpha, downshift);
                break;
            default:
                throw new ArgumentOutOfRangeException("Format");
        }
    }

    internal static byte ScaleSample8(byte sample, int significantBits, bool downshift)
    {
        if (downshift)
        {
            int shift = 8 - significantBits;
            return (byte)(sample >> shift);
        }

        return (byte)ScaleSampleToFullRange(sample, significantBits, 8);
    }

    internal static void WriteScaledSample16(byte[] output, int offset, ushort sample, int significantBits, bool downshift)
    {
        ushort scaled = ScaleSample16(sample, significantBits, downshift);
        PngBinaryWriter.WriteUInt16(output, offset, scaled);
    }

    internal static ushort ScaleSample16(ushort sample, int significantBits, bool downshift)
    {
        if (downshift)
        {
            int shift = 16 - significantBits;
            return (ushort)(sample >> shift);
        }

        return (ushort)ScaleSampleToFullRange(sample, significantBits, 16);
    }

    internal static int ScaleSampleToFullRange(int sample, int significantBits, int outputBits)
    {
        int maximumInput = GetMaximumValue(significantBits);
        int maximumOutput = GetMaximumValue(outputBits);
        int clamped = sample;
        if (clamped > maximumInput)
        {
            clamped = maximumInput;
        }

        long scaled = ((long)clamped * maximumOutput + maximumInput / 2) / maximumInput;
        return (int)scaled;
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

    internal static void ValidateSignificantBitsBuffer(PngPixelBuffer buffer, PngSignificantBits bits)
    {
        if (buffer.Width <= 0)
        {
            throw new ArgumentOutOfRangeException("Width");
        }

        if (buffer.Height <= 0)
        {
            throw new ArgumentOutOfRangeException("Height");
        }

        ValidateSignificantBits(buffer.Format, bits);

        int bytesPerPixel = GetSignificantBitsBytesPerPixel(buffer.Format);
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

    internal static void ValidateSignificantBits(PngPixelFormat format, PngSignificantBits bits)
    {
        int sampleBits = GetSampleBits(format);
        switch (format)
        {
            case PngPixelFormat.Grayscale8:
            case PngPixelFormat.Grayscale16:
                ValidateSignificantBitValue(bits.Gray, sampleBits, "Gray");
                break;
            case PngPixelFormat.GrayscaleAlpha8:
            case PngPixelFormat.GrayscaleAlpha16:
                ValidateSignificantBitValue(bits.Gray, sampleBits, "Gray");
                ValidateSignificantBitValue(bits.Alpha, sampleBits, "Alpha");
                break;
            case PngPixelFormat.Rgb8:
            case PngPixelFormat.Rgb16:
                ValidateSignificantBitValue(bits.Red, sampleBits, "Red");
                ValidateSignificantBitValue(bits.Green, sampleBits, "Green");
                ValidateSignificantBitValue(bits.Blue, sampleBits, "Blue");
                break;
            case PngPixelFormat.Rgba8:
            case PngPixelFormat.Rgba16:
                ValidateSignificantBitValue(bits.Red, sampleBits, "Red");
                ValidateSignificantBitValue(bits.Green, sampleBits, "Green");
                ValidateSignificantBitValue(bits.Blue, sampleBits, "Blue");
                ValidateSignificantBitValue(bits.Alpha, sampleBits, "Alpha");
                break;
            default:
                throw new ArgumentOutOfRangeException("Format");
        }
    }

    internal static void ValidateSignificantBitValue(int value, int sampleBits, string name)
    {
        if (value <= 0 || value > sampleBits)
        {
            throw new ArgumentOutOfRangeException(name);
        }
    }

    internal static int GetBytesPerPixel(PngPixelFormat format)
    {
        switch (format)
        {
            case PngPixelFormat.Grayscale16:
                return 2;
            case PngPixelFormat.Rgb16:
                return 6;
            case PngPixelFormat.GrayscaleAlpha16:
                return 4;
            case PngPixelFormat.Rgba16:
                return 8;
            default:
                throw new ArgumentOutOfRangeException("Format");
        }
    }

    internal static int GetSignificantBitsBytesPerPixel(PngPixelFormat format)
    {
        switch (format)
        {
            case PngPixelFormat.Grayscale8:
                return 1;
            case PngPixelFormat.Grayscale16:
                return 2;
            case PngPixelFormat.Rgb8:
                return 3;
            case PngPixelFormat.Rgb16:
                return 6;
            case PngPixelFormat.GrayscaleAlpha8:
                return 2;
            case PngPixelFormat.GrayscaleAlpha16:
                return 4;
            case PngPixelFormat.Rgba8:
                return 4;
            case PngPixelFormat.Rgba16:
                return 8;
            default:
                throw new ArgumentOutOfRangeException("Format");
        }
    }

    internal static PngPixelFormat Get8BitOutputFormat(PngPixelFormat format)
    {
        switch (format)
        {
            case PngPixelFormat.Grayscale16:
                return PngPixelFormat.Grayscale8;
            case PngPixelFormat.Rgb16:
                return PngPixelFormat.Rgb8;
            case PngPixelFormat.GrayscaleAlpha16:
                return PngPixelFormat.GrayscaleAlpha8;
            case PngPixelFormat.Rgba16:
                return PngPixelFormat.Rgba8;
            default:
                throw new ArgumentOutOfRangeException("Format");
        }
    }

    internal static int Get8BitBytesPerPixel(PngPixelFormat format)
    {
        switch (format)
        {
            case PngPixelFormat.Grayscale8:
                return 1;
            case PngPixelFormat.Rgb8:
                return 3;
            case PngPixelFormat.GrayscaleAlpha8:
                return 2;
            case PngPixelFormat.Rgba8:
                return 4;
            default:
                throw new ArgumentOutOfRangeException("Format");
        }
    }

    internal static bool Is16BitFormat(PngPixelFormat format)
    {
        return format == PngPixelFormat.Grayscale16
            || format == PngPixelFormat.Rgb16
            || format == PngPixelFormat.GrayscaleAlpha16
            || format == PngPixelFormat.Rgba16;
    }

    internal static int GetSampleBits(PngPixelFormat format)
    {
        if (Is16BitFormat(format))
        {
            return 16;
        }

        GetSignificantBitsBytesPerPixel(format);
        return 8;
    }

    internal static int GetMaximumValue(int bits)
    {
        if (bits == 16)
        {
            return 65535;
        }

        return (1 << bits) - 1;
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
