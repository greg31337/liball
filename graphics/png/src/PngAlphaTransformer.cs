namespace png;

/// <summary>
/// Provides alpha-channel addition, removal, inversion, compositing, association, and cleanup operations.
/// </summary>
internal static class PngAlphaTransformer
{
    /// <summary>
    /// Adds a fully opaque alpha sample to every pixel and returns a new buffer.
    /// </summary>
    /// <param name="buffer">The source pixel buffer.</param>
    /// <returns>A newly allocated pixel buffer containing the result.</returns>
    public static PngPixelBuffer AddOpaqueAlpha(PngPixelBuffer buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentNullException.ThrowIfNull(buffer.Data);

        if (PngReorderedPixelTransformer.IsReordered(buffer.Format))
        {
            PngPixelBuffer transformed = AddOpaqueAlpha(PngReorderedPixelTransformer.Normalize(buffer));
            return PngReorderedPixelTransformer.Restore(buffer.Format, transformed);
        }

        if (PngGrayscaleExpander.IsLowBitGrayscaleFormat(buffer.Format))
        {
            PngPixelBuffer grayscale8 = PngGrayscaleExpander.ExpandLowBitToGrayscale8(buffer);
            return AddOpaqueAlpha(grayscale8);
        }

        ValidateAddAlphaBuffer(buffer);

        if (buffer.Format == PngPixelFormat.Grayscale16 || buffer.Format == PngPixelFormat.Rgb16)
        {
            return AddOpaqueAlpha16(buffer);
        }

        return AddOpaqueAlpha8(buffer);
    }

    /// <summary>
    /// Removes every alpha sample and returns a new grayscale or RGB buffer.
    /// </summary>
    /// <param name="buffer">The source pixel buffer.</param>
    /// <returns>A newly allocated pixel buffer containing the result.</returns>
    public static PngPixelBuffer StripAlpha(PngPixelBuffer buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentNullException.ThrowIfNull(buffer.Data);

        if (PngReorderedPixelTransformer.IsReordered(buffer.Format))
        {
            PngPixelBuffer transformed = StripAlpha(PngReorderedPixelTransformer.Normalize(buffer));
            return PngReorderedPixelTransformer.Restore(buffer.Format, transformed);
        }

        ValidateStripAlphaBuffer(buffer);
        return StripAlphaCore(buffer);
    }

    /// <summary>
    /// Removes alpha only when every pixel is opaque; otherwise returns a copy with alpha intact.
    /// </summary>
    /// <param name="buffer">The source pixel buffer.</param>
    /// <returns>A newly allocated pixel buffer containing the result.</returns>
    public static PngPixelBuffer StripAlphaIfOpaque(PngPixelBuffer buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentNullException.ThrowIfNull(buffer.Data);

        if (PngReorderedPixelTransformer.IsReordered(buffer.Format))
        {
            PngPixelBuffer transformed = StripAlphaIfOpaque(PngReorderedPixelTransformer.Normalize(buffer));
            return PngReorderedPixelTransformer.Restore(buffer.Format, transformed);
        }

        int inputBytesPerPixel = GetAlphaInputBytesPerPixel(buffer.Format);
        ValidateBuffer(buffer, inputBytesPerPixel);
        if (IsOpaqueCore(buffer, inputBytesPerPixel))
        {
            return StripAlphaCore(buffer);
        }

        return CopyBufferCore(buffer, inputBytesPerPixel);
    }

    /// <summary>
    /// Determines whether every alpha sample is at its maximum value.
    /// </summary>
    /// <param name="buffer">The source pixel buffer.</param>
    /// <returns>True when the requested condition is met; otherwise false.</returns>
    public static bool IsOpaque(PngPixelBuffer buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentNullException.ThrowIfNull(buffer.Data);

        if (PngReorderedPixelTransformer.IsReordered(buffer.Format))
        {
            return IsOpaque(PngReorderedPixelTransformer.Normalize(buffer));
        }

        int inputBytesPerPixel = GetAlphaInputBytesPerPixel(buffer.Format);
        ValidateBuffer(buffer, inputBytesPerPixel);
        return IsOpaqueCore(buffer, inputBytesPerPixel);
    }

    /// <summary>
    /// Replaces each alpha sample with its complement and returns a new buffer.
    /// </summary>
    /// <param name="buffer">The source pixel buffer.</param>
    /// <returns>A newly allocated pixel buffer containing the result.</returns>
    public static PngPixelBuffer InvertAlpha(PngPixelBuffer buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentNullException.ThrowIfNull(buffer.Data);

        if (PngReorderedPixelTransformer.IsReordered(buffer.Format))
        {
            PngPixelBuffer transformed = InvertAlpha(PngReorderedPixelTransformer.Normalize(buffer));
            return PngReorderedPixelTransformer.Restore(buffer.Format, transformed);
        }

        ValidateInvertAlphaBuffer(buffer);
        return InvertAlphaCore(buffer);
    }

    /// <summary>
    /// Composites straight-alpha pixels over an opaque background and returns a buffer without alpha.
    /// </summary>
    /// <param name="buffer">The source pixel buffer.</param>
    /// <param name="background">The background samples used for compositing.</param>
    /// <returns>A newly allocated pixel buffer containing the result.</returns>
    public static PngPixelBuffer CompositeOverBackground(PngPixelBuffer buffer, PngBackgroundColor background)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentNullException.ThrowIfNull(buffer.Data);
        ArgumentNullException.ThrowIfNull(background);

        if (PngReorderedPixelTransformer.IsReordered(buffer.Format))
        {
            PngPixelBuffer transformed = CompositeOverBackground(PngReorderedPixelTransformer.Normalize(buffer), background);
            return PngReorderedPixelTransformer.Restore(buffer.Format, transformed);
        }

        ValidateCompositeBuffer(buffer, background);

        if (buffer.Format == PngPixelFormat.GrayscaleAlpha16 || buffer.Format == PngPixelFormat.Rgba16)
        {
            return CompositeOverBackground16(buffer, background);
        }

        return CompositeOverBackground8(buffer, background);
    }

    /// <summary>
    /// Multiplies color samples by alpha using integer rounding and returns a new associated-alpha buffer.
    /// </summary>
    /// <param name="buffer">The source pixel buffer.</param>
    /// <returns>A newly allocated pixel buffer containing the result.</returns>
    public static PngPixelBuffer PremultiplyAlpha(PngPixelBuffer buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentNullException.ThrowIfNull(buffer.Data);

        if (PngReorderedPixelTransformer.IsReordered(buffer.Format))
        {
            PngPixelBuffer transformed = PremultiplyAlpha(PngReorderedPixelTransformer.Normalize(buffer));
            return PngReorderedPixelTransformer.Restore(buffer.Format, transformed);
        }

        ValidatePremultiplyAlphaBuffer(buffer);

        if (buffer.Format == PngPixelFormat.GrayscaleAlpha16 || buffer.Format == PngPixelFormat.Rgba16)
        {
            return PremultiplyAlpha16(buffer);
        }

        return PremultiplyAlpha8(buffer);
    }

    /// <summary>
    /// Recovers straight color samples from associated alpha and returns a new buffer.
    /// </summary>
    /// <param name="buffer">The source pixel buffer.</param>
    /// <returns>A newly allocated pixel buffer containing the result.</returns>
    public static PngPixelBuffer UnpremultiplyAlpha(PngPixelBuffer buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentNullException.ThrowIfNull(buffer.Data);

        if (PngReorderedPixelTransformer.IsReordered(buffer.Format))
        {
            PngPixelBuffer transformed = UnpremultiplyAlpha(PngReorderedPixelTransformer.Normalize(buffer));
            return PngReorderedPixelTransformer.Restore(buffer.Format, transformed);
        }

        ValidateUnpremultiplyAlphaBuffer(buffer);

        if (buffer.Format == PngPixelFormat.GrayscaleAlpha16 || buffer.Format == PngPixelFormat.Rgba16)
        {
            return UnpremultiplyAlpha16(buffer);
        }

        return UnpremultiplyAlpha8(buffer);
    }

    /// <summary>
    /// Sets color samples to zero wherever alpha is zero and returns a new buffer.
    /// </summary>
    /// <param name="buffer">The source pixel buffer.</param>
    /// <returns>A newly allocated pixel buffer containing the result.</returns>
    public static PngPixelBuffer ClearTransparentPixelColors(PngPixelBuffer buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentNullException.ThrowIfNull(buffer.Data);

        if (PngReorderedPixelTransformer.IsReordered(buffer.Format))
        {
            PngPixelBuffer transformed = ClearTransparentPixelColors(PngReorderedPixelTransformer.Normalize(buffer));
            return PngReorderedPixelTransformer.Restore(buffer.Format, transformed);
        }

        ValidateClearTransparentPixelColorsBuffer(buffer);

        if (buffer.Format == PngPixelFormat.GrayscaleAlpha16 || buffer.Format == PngPixelFormat.Rgba16)
        {
            return ClearTransparentPixelColors16(buffer);
        }

        return ClearTransparentPixelColors8(buffer);
    }

    internal static PngPixelBuffer AddOpaqueAlpha8(PngPixelBuffer buffer)
    {
        PngPixelFormat outputFormat = GetAddAlphaOutputFormat(buffer.Format);
        int inputBytesPerPixel = GetAddAlphaInputBytesPerPixel(buffer.Format);
        int outputBytesPerPixel = GetAddAlphaOutputBytesPerPixel(outputFormat);
        int outputStride = CalculateOutputStride(buffer.Width, outputBytesPerPixel);
        byte[] output = CreateOutputData(outputStride, buffer.Height);

        int rowIndex = 0;
        while (rowIndex < buffer.Height)
        {
            AddOpaqueAlpha8Row(buffer, rowIndex, inputBytesPerPixel, output, outputStride);
            rowIndex += 1;
        }

        return CreateBuffer(outputFormat, buffer.Width, buffer.Height, outputStride, output);
    }

    internal static PngPixelBuffer AddOpaqueAlpha16(PngPixelBuffer buffer)
    {
        PngPixelFormat outputFormat = GetAddAlphaOutputFormat(buffer.Format);
        int inputBytesPerPixel = GetAddAlphaInputBytesPerPixel(buffer.Format);
        int outputBytesPerPixel = GetAddAlphaOutputBytesPerPixel(outputFormat);
        int outputStride = CalculateOutputStride(buffer.Width, outputBytesPerPixel);
        byte[] output = CreateOutputData(outputStride, buffer.Height);

        int rowIndex = 0;
        while (rowIndex < buffer.Height)
        {
            AddOpaqueAlpha16Row(buffer, rowIndex, inputBytesPerPixel, output, outputStride);
            rowIndex += 1;
        }

        return CreateBuffer(outputFormat, buffer.Width, buffer.Height, outputStride, output);
    }

    internal static PngPixelBuffer StripAlphaCore(PngPixelBuffer buffer)
    {
        PngPixelFormat outputFormat = GetStripAlphaOutputFormat(buffer.Format);
        int inputBytesPerPixel = GetAlphaInputBytesPerPixel(buffer.Format);
        int outputBytesPerPixel = GetStripAlphaOutputBytesPerPixel(outputFormat);
        int outputStride = CalculateOutputStride(buffer.Width, outputBytesPerPixel);
        byte[] output = CreateOutputData(outputStride, buffer.Height);

        int rowIndex = 0;
        while (rowIndex < buffer.Height)
        {
            StripAlphaRow(buffer, rowIndex, inputBytesPerPixel, outputBytesPerPixel, output, outputStride);
            rowIndex += 1;
        }

        return CreateBuffer(outputFormat, buffer.Width, buffer.Height, outputStride, output);
    }

    internal static PngPixelBuffer CopyBufferCore(PngPixelBuffer buffer, int bytesPerPixel)
    {
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

    internal static PngPixelBuffer InvertAlphaCore(PngPixelBuffer buffer)
    {
        int bytesPerPixel = GetAlphaInputBytesPerPixel(buffer.Format);
        int alphaOffset = GetAlphaOffset(buffer.Format);
        int alphaBytes = GetAlphaBytes(buffer.Format);
        int outputStride = CalculateOutputStride(buffer.Width, bytesPerPixel);
        byte[] output = CreateOutputData(outputStride, buffer.Height);

        int rowIndex = 0;
        while (rowIndex < buffer.Height)
        {
            InvertAlphaRow(buffer, rowIndex, bytesPerPixel, alphaOffset, alphaBytes, output, outputStride);
            rowIndex += 1;
        }

        return CreateBuffer(buffer.Format, buffer.Width, buffer.Height, outputStride, output);
    }

    internal static PngPixelBuffer CompositeOverBackground8(PngPixelBuffer buffer, PngBackgroundColor background)
    {
        PngPixelFormat outputFormat = GetStripAlphaOutputFormat(buffer.Format);
        int outputBytesPerPixel = GetStripAlphaOutputBytesPerPixel(outputFormat);
        int outputStride = CalculateOutputStride(buffer.Width, outputBytesPerPixel);
        byte[] output = CreateOutputData(outputStride, buffer.Height);

        int rowIndex = 0;
        while (rowIndex < buffer.Height)
        {
            CompositeOverBackground8Row(buffer, rowIndex, background, output, outputStride);
            rowIndex += 1;
        }

        return CreateBuffer(outputFormat, buffer.Width, buffer.Height, outputStride, output);
    }

    internal static PngPixelBuffer CompositeOverBackground16(PngPixelBuffer buffer, PngBackgroundColor background)
    {
        PngPixelFormat outputFormat = GetStripAlphaOutputFormat(buffer.Format);
        int outputBytesPerPixel = GetStripAlphaOutputBytesPerPixel(outputFormat);
        int outputStride = CalculateOutputStride(buffer.Width, outputBytesPerPixel);
        byte[] output = CreateOutputData(outputStride, buffer.Height);

        int rowIndex = 0;
        while (rowIndex < buffer.Height)
        {
            CompositeOverBackground16Row(buffer, rowIndex, background, output, outputStride);
            rowIndex += 1;
        }

        return CreateBuffer(outputFormat, buffer.Width, buffer.Height, outputStride, output);
    }

    internal static PngPixelBuffer PremultiplyAlpha8(PngPixelBuffer buffer)
    {
        int bytesPerPixel = GetAlphaInputBytesPerPixel(buffer.Format);
        int outputStride = CalculateOutputStride(buffer.Width, bytesPerPixel);
        byte[] output = CreateOutputData(outputStride, buffer.Height);

        int rowIndex = 0;
        while (rowIndex < buffer.Height)
        {
            PremultiplyAlpha8Row(buffer, rowIndex, output, outputStride);
            rowIndex += 1;
        }

        return CreateBuffer(buffer.Format, buffer.Width, buffer.Height, outputStride, output);
    }

    internal static PngPixelBuffer PremultiplyAlpha16(PngPixelBuffer buffer)
    {
        int bytesPerPixel = GetAlphaInputBytesPerPixel(buffer.Format);
        int outputStride = CalculateOutputStride(buffer.Width, bytesPerPixel);
        byte[] output = CreateOutputData(outputStride, buffer.Height);

        int rowIndex = 0;
        while (rowIndex < buffer.Height)
        {
            PremultiplyAlpha16Row(buffer, rowIndex, output, outputStride);
            rowIndex += 1;
        }

        return CreateBuffer(buffer.Format, buffer.Width, buffer.Height, outputStride, output);
    }

    internal static PngPixelBuffer UnpremultiplyAlpha8(PngPixelBuffer buffer)
    {
        int bytesPerPixel = GetAlphaInputBytesPerPixel(buffer.Format);
        int outputStride = CalculateOutputStride(buffer.Width, bytesPerPixel);
        byte[] output = CreateOutputData(outputStride, buffer.Height);

        int rowIndex = 0;
        while (rowIndex < buffer.Height)
        {
            UnpremultiplyAlpha8Row(buffer, rowIndex, output, outputStride);
            rowIndex += 1;
        }

        return CreateBuffer(buffer.Format, buffer.Width, buffer.Height, outputStride, output);
    }

    internal static PngPixelBuffer UnpremultiplyAlpha16(PngPixelBuffer buffer)
    {
        int bytesPerPixel = GetAlphaInputBytesPerPixel(buffer.Format);
        int outputStride = CalculateOutputStride(buffer.Width, bytesPerPixel);
        byte[] output = CreateOutputData(outputStride, buffer.Height);

        int rowIndex = 0;
        while (rowIndex < buffer.Height)
        {
            UnpremultiplyAlpha16Row(buffer, rowIndex, output, outputStride);
            rowIndex += 1;
        }

        return CreateBuffer(buffer.Format, buffer.Width, buffer.Height, outputStride, output);
    }

    internal static PngPixelBuffer ClearTransparentPixelColors8(PngPixelBuffer buffer)
    {
        int bytesPerPixel = GetAlphaInputBytesPerPixel(buffer.Format);
        int outputStride = CalculateOutputStride(buffer.Width, bytesPerPixel);
        byte[] output = CreateOutputData(outputStride, buffer.Height);

        int rowIndex = 0;
        while (rowIndex < buffer.Height)
        {
            ClearTransparentPixelColors8Row(buffer, rowIndex, output, outputStride);
            rowIndex += 1;
        }

        return CreateBuffer(buffer.Format, buffer.Width, buffer.Height, outputStride, output);
    }

    internal static PngPixelBuffer ClearTransparentPixelColors16(PngPixelBuffer buffer)
    {
        int bytesPerPixel = GetAlphaInputBytesPerPixel(buffer.Format);
        int outputStride = CalculateOutputStride(buffer.Width, bytesPerPixel);
        byte[] output = CreateOutputData(outputStride, buffer.Height);

        int rowIndex = 0;
        while (rowIndex < buffer.Height)
        {
            ClearTransparentPixelColors16Row(buffer, rowIndex, output, outputStride);
            rowIndex += 1;
        }

        return CreateBuffer(buffer.Format, buffer.Width, buffer.Height, outputStride, output);
    }

    internal static void AddOpaqueAlpha8Row(PngPixelBuffer buffer, int rowIndex, int inputBytesPerPixel, byte[] output, int outputStride)
    {
        int sourceOffset = buffer.Stride * rowIndex;
        int destinationOffset = outputStride * rowIndex;
        int x = 0;
        while (x < buffer.Width)
        {
            CopyBytes(buffer.Data, sourceOffset, output, destinationOffset, inputBytesPerPixel);
            output[destinationOffset + inputBytesPerPixel] = 255;
            sourceOffset += inputBytesPerPixel;
            destinationOffset += inputBytesPerPixel + 1;
            x += 1;
        }
    }

    internal static void AddOpaqueAlpha16Row(PngPixelBuffer buffer, int rowIndex, int inputBytesPerPixel, byte[] output, int outputStride)
    {
        int sourceOffset = buffer.Stride * rowIndex;
        int destinationOffset = outputStride * rowIndex;
        int x = 0;
        while (x < buffer.Width)
        {
            CopyBytes(buffer.Data, sourceOffset, output, destinationOffset, inputBytesPerPixel);
            output[destinationOffset + inputBytesPerPixel] = 255;
            output[destinationOffset + inputBytesPerPixel + 1] = 255;
            sourceOffset += inputBytesPerPixel;
            destinationOffset += inputBytesPerPixel + 2;
            x += 1;
        }
    }

    internal static void StripAlphaRow(PngPixelBuffer buffer, int rowIndex, int inputBytesPerPixel, int outputBytesPerPixel, byte[] output, int outputStride)
    {
        int sourceOffset = buffer.Stride * rowIndex;
        int destinationOffset = outputStride * rowIndex;
        int x = 0;
        while (x < buffer.Width)
        {
            CopyBytes(buffer.Data, sourceOffset, output, destinationOffset, outputBytesPerPixel);
            sourceOffset += inputBytesPerPixel;
            destinationOffset += outputBytesPerPixel;
            x += 1;
        }
    }

    internal static void CopyPixelRow(PngPixelBuffer buffer, int rowIndex, byte[] output, int outputStride, int rowBytes)
    {
        int sourceOffset = buffer.Stride * rowIndex;
        int destinationOffset = outputStride * rowIndex;
        CopyBytes(buffer.Data, sourceOffset, output, destinationOffset, rowBytes);
    }

    internal static bool IsOpaqueCore(PngPixelBuffer buffer, int inputBytesPerPixel)
    {
        int alphaOffset = GetAlphaOffset(buffer.Format);
        int alphaBytes = GetAlphaBytes(buffer.Format);
        int rowIndex = 0;
        while (rowIndex < buffer.Height)
        {
            if (!IsOpaqueRow(buffer, rowIndex, inputBytesPerPixel, alphaOffset, alphaBytes))
            {
                return false;
            }

            rowIndex += 1;
        }

        return true;
    }

    internal static bool IsOpaqueRow(PngPixelBuffer buffer, int rowIndex, int inputBytesPerPixel, int alphaOffset, int alphaBytes)
    {
        int sourceOffset = buffer.Stride * rowIndex;
        int x = 0;
        while (x < buffer.Width)
        {
            if (!IsOpaqueSample(buffer.Data, sourceOffset + alphaOffset, alphaBytes))
            {
                return false;
            }

            sourceOffset += inputBytesPerPixel;
            x += 1;
        }

        return true;
    }

    internal static bool IsOpaqueSample(byte[] data, int offset, int alphaBytes)
    {
        if (data[offset] != 255)
        {
            return false;
        }

        if (alphaBytes == 2 && data[offset + 1] != 255)
        {
            return false;
        }

        return true;
    }

    internal static void InvertAlphaRow(PngPixelBuffer buffer, int rowIndex, int bytesPerPixel, int alphaOffset, int alphaBytes, byte[] output, int outputStride)
    {
        int sourceOffset = buffer.Stride * rowIndex;
        int destinationOffset = outputStride * rowIndex;
        int x = 0;
        while (x < buffer.Width)
        {
            CopyBytes(buffer.Data, sourceOffset, output, destinationOffset, bytesPerPixel);
            InvertAlphaSample(output, destinationOffset + alphaOffset, alphaBytes);
            sourceOffset += bytesPerPixel;
            destinationOffset += bytesPerPixel;
            x += 1;
        }
    }

    internal static void InvertAlphaSample(byte[] data, int offset, int alphaBytes)
    {
        data[offset] = (byte)(255 - data[offset]);
        if (alphaBytes == 2)
        {
            data[offset + 1] = (byte)(255 - data[offset + 1]);
        }
    }

    internal static void CompositeOverBackground8Row(PngPixelBuffer buffer, int rowIndex, PngBackgroundColor background, byte[] output, int outputStride)
    {
        int inputBytesPerPixel = GetAlphaInputBytesPerPixel(buffer.Format);
        int outputBytesPerPixel = GetStripAlphaOutputBytesPerPixel(GetStripAlphaOutputFormat(buffer.Format));
        int sourceOffset = buffer.Stride * rowIndex;
        int destinationOffset = outputStride * rowIndex;
        int x = 0;
        while (x < buffer.Width)
        {
            if (buffer.Format == PngPixelFormat.GrayscaleAlpha8)
            {
                output[destinationOffset] = CompositeSample8(buffer.Data[sourceOffset], (byte)background.Gray, buffer.Data[sourceOffset + 1]);
            }
            else
            {
                output[destinationOffset] = CompositeSample8(buffer.Data[sourceOffset], (byte)background.Red, buffer.Data[sourceOffset + 3]);
                output[destinationOffset + 1] = CompositeSample8(buffer.Data[sourceOffset + 1], (byte)background.Green, buffer.Data[sourceOffset + 3]);
                output[destinationOffset + 2] = CompositeSample8(buffer.Data[sourceOffset + 2], (byte)background.Blue, buffer.Data[sourceOffset + 3]);
            }

            sourceOffset += inputBytesPerPixel;
            destinationOffset += outputBytesPerPixel;
            x += 1;
        }
    }

    internal static void CompositeOverBackground16Row(PngPixelBuffer buffer, int rowIndex, PngBackgroundColor background, byte[] output, int outputStride)
    {
        int inputBytesPerPixel = GetAlphaInputBytesPerPixel(buffer.Format);
        int outputBytesPerPixel = GetStripAlphaOutputBytesPerPixel(GetStripAlphaOutputFormat(buffer.Format));
        int sourceOffset = buffer.Stride * rowIndex;
        int destinationOffset = outputStride * rowIndex;
        int x = 0;
        while (x < buffer.Width)
        {
            if (buffer.Format == PngPixelFormat.GrayscaleAlpha16)
            {
                WriteCompositeSample16(output, destinationOffset, PngBinaryReader.ReadUInt16(buffer.Data, sourceOffset), background.Gray, PngBinaryReader.ReadUInt16(buffer.Data, sourceOffset + 2));
            }
            else
            {
                ushort alpha = PngBinaryReader.ReadUInt16(buffer.Data, sourceOffset + 6);
                WriteCompositeSample16(output, destinationOffset, PngBinaryReader.ReadUInt16(buffer.Data, sourceOffset), background.Red, alpha);
                WriteCompositeSample16(output, destinationOffset + 2, PngBinaryReader.ReadUInt16(buffer.Data, sourceOffset + 2), background.Green, alpha);
                WriteCompositeSample16(output, destinationOffset + 4, PngBinaryReader.ReadUInt16(buffer.Data, sourceOffset + 4), background.Blue, alpha);
            }

            sourceOffset += inputBytesPerPixel;
            destinationOffset += outputBytesPerPixel;
            x += 1;
        }
    }

    internal static void PremultiplyAlpha8Row(PngPixelBuffer buffer, int rowIndex, byte[] output, int outputStride)
    {
        int bytesPerPixel = GetAlphaInputBytesPerPixel(buffer.Format);
        int sourceOffset = buffer.Stride * rowIndex;
        int destinationOffset = outputStride * rowIndex;
        int x = 0;
        while (x < buffer.Width)
        {
            if (buffer.Format == PngPixelFormat.GrayscaleAlpha8)
            {
                output[destinationOffset] = PremultiplySample8(buffer.Data[sourceOffset], buffer.Data[sourceOffset + 1]);
                output[destinationOffset + 1] = buffer.Data[sourceOffset + 1];
            }
            else
            {
                byte alpha = buffer.Data[sourceOffset + 3];
                output[destinationOffset] = PremultiplySample8(buffer.Data[sourceOffset], alpha);
                output[destinationOffset + 1] = PremultiplySample8(buffer.Data[sourceOffset + 1], alpha);
                output[destinationOffset + 2] = PremultiplySample8(buffer.Data[sourceOffset + 2], alpha);
                output[destinationOffset + 3] = alpha;
            }

            sourceOffset += bytesPerPixel;
            destinationOffset += bytesPerPixel;
            x += 1;
        }
    }

    internal static void PremultiplyAlpha16Row(PngPixelBuffer buffer, int rowIndex, byte[] output, int outputStride)
    {
        int bytesPerPixel = GetAlphaInputBytesPerPixel(buffer.Format);
        int sourceOffset = buffer.Stride * rowIndex;
        int destinationOffset = outputStride * rowIndex;
        int x = 0;
        while (x < buffer.Width)
        {
            if (buffer.Format == PngPixelFormat.GrayscaleAlpha16)
            {
                ushort alpha = PngBinaryReader.ReadUInt16(buffer.Data, sourceOffset + 2);
                WritePremultipliedSample16(output, destinationOffset, PngBinaryReader.ReadUInt16(buffer.Data, sourceOffset), alpha);
                PngBinaryWriter.WriteUInt16(output, destinationOffset + 2, alpha);
            }
            else
            {
                ushort alpha = PngBinaryReader.ReadUInt16(buffer.Data, sourceOffset + 6);
                WritePremultipliedSample16(output, destinationOffset, PngBinaryReader.ReadUInt16(buffer.Data, sourceOffset), alpha);
                WritePremultipliedSample16(output, destinationOffset + 2, PngBinaryReader.ReadUInt16(buffer.Data, sourceOffset + 2), alpha);
                WritePremultipliedSample16(output, destinationOffset + 4, PngBinaryReader.ReadUInt16(buffer.Data, sourceOffset + 4), alpha);
                PngBinaryWriter.WriteUInt16(output, destinationOffset + 6, alpha);
            }

            sourceOffset += bytesPerPixel;
            destinationOffset += bytesPerPixel;
            x += 1;
        }
    }

    internal static void UnpremultiplyAlpha8Row(PngPixelBuffer buffer, int rowIndex, byte[] output, int outputStride)
    {
        int bytesPerPixel = GetAlphaInputBytesPerPixel(buffer.Format);
        int sourceOffset = buffer.Stride * rowIndex;
        int destinationOffset = outputStride * rowIndex;
        int x = 0;
        while (x < buffer.Width)
        {
            if (buffer.Format == PngPixelFormat.GrayscaleAlpha8)
            {
                output[destinationOffset] = UnpremultiplySample8(buffer.Data[sourceOffset], buffer.Data[sourceOffset + 1]);
                output[destinationOffset + 1] = buffer.Data[sourceOffset + 1];
            }
            else
            {
                byte alpha = buffer.Data[sourceOffset + 3];
                output[destinationOffset] = UnpremultiplySample8(buffer.Data[sourceOffset], alpha);
                output[destinationOffset + 1] = UnpremultiplySample8(buffer.Data[sourceOffset + 1], alpha);
                output[destinationOffset + 2] = UnpremultiplySample8(buffer.Data[sourceOffset + 2], alpha);
                output[destinationOffset + 3] = alpha;
            }

            sourceOffset += bytesPerPixel;
            destinationOffset += bytesPerPixel;
            x += 1;
        }
    }

    internal static void UnpremultiplyAlpha16Row(PngPixelBuffer buffer, int rowIndex, byte[] output, int outputStride)
    {
        int bytesPerPixel = GetAlphaInputBytesPerPixel(buffer.Format);
        int sourceOffset = buffer.Stride * rowIndex;
        int destinationOffset = outputStride * rowIndex;
        int x = 0;
        while (x < buffer.Width)
        {
            if (buffer.Format == PngPixelFormat.GrayscaleAlpha16)
            {
                ushort alpha = PngBinaryReader.ReadUInt16(buffer.Data, sourceOffset + 2);
                WriteUnpremultipliedSample16(output, destinationOffset, PngBinaryReader.ReadUInt16(buffer.Data, sourceOffset), alpha);
                PngBinaryWriter.WriteUInt16(output, destinationOffset + 2, alpha);
            }
            else
            {
                ushort alpha = PngBinaryReader.ReadUInt16(buffer.Data, sourceOffset + 6);
                WriteUnpremultipliedSample16(output, destinationOffset, PngBinaryReader.ReadUInt16(buffer.Data, sourceOffset), alpha);
                WriteUnpremultipliedSample16(output, destinationOffset + 2, PngBinaryReader.ReadUInt16(buffer.Data, sourceOffset + 2), alpha);
                WriteUnpremultipliedSample16(output, destinationOffset + 4, PngBinaryReader.ReadUInt16(buffer.Data, sourceOffset + 4), alpha);
                PngBinaryWriter.WriteUInt16(output, destinationOffset + 6, alpha);
            }

            sourceOffset += bytesPerPixel;
            destinationOffset += bytesPerPixel;
            x += 1;
        }
    }

    internal static void ClearTransparentPixelColors8Row(PngPixelBuffer buffer, int rowIndex, byte[] output, int outputStride)
    {
        int bytesPerPixel = GetAlphaInputBytesPerPixel(buffer.Format);
        int sourceOffset = buffer.Stride * rowIndex;
        int destinationOffset = outputStride * rowIndex;
        int x = 0;
        while (x < buffer.Width)
        {
            if (buffer.Format == PngPixelFormat.GrayscaleAlpha8)
            {
                ClearTransparentGrayscaleAlpha8Pixel(buffer, sourceOffset, output, destinationOffset);
            }
            else
            {
                ClearTransparentRgba8Pixel(buffer, sourceOffset, output, destinationOffset);
            }

            sourceOffset += bytesPerPixel;
            destinationOffset += bytesPerPixel;
            x += 1;
        }
    }

    internal static void ClearTransparentPixelColors16Row(PngPixelBuffer buffer, int rowIndex, byte[] output, int outputStride)
    {
        int bytesPerPixel = GetAlphaInputBytesPerPixel(buffer.Format);
        int sourceOffset = buffer.Stride * rowIndex;
        int destinationOffset = outputStride * rowIndex;
        int x = 0;
        while (x < buffer.Width)
        {
            if (buffer.Format == PngPixelFormat.GrayscaleAlpha16)
            {
                ClearTransparentGrayscaleAlpha16Pixel(buffer, sourceOffset, output, destinationOffset);
            }
            else
            {
                ClearTransparentRgba16Pixel(buffer, sourceOffset, output, destinationOffset);
            }

            sourceOffset += bytesPerPixel;
            destinationOffset += bytesPerPixel;
            x += 1;
        }
    }

    internal static void ClearTransparentGrayscaleAlpha8Pixel(PngPixelBuffer buffer, int sourceOffset, byte[] output, int destinationOffset)
    {
        if (buffer.Data[sourceOffset + 1] == 0)
        {
            output[destinationOffset] = 0;
            output[destinationOffset + 1] = 0;
            return;
        }

        CopyBytes(buffer.Data, sourceOffset, output, destinationOffset, 2);
    }

    internal static void ClearTransparentRgba8Pixel(PngPixelBuffer buffer, int sourceOffset, byte[] output, int destinationOffset)
    {
        if (buffer.Data[sourceOffset + 3] == 0)
        {
            output[destinationOffset] = 0;
            output[destinationOffset + 1] = 0;
            output[destinationOffset + 2] = 0;
            output[destinationOffset + 3] = 0;
            return;
        }

        CopyBytes(buffer.Data, sourceOffset, output, destinationOffset, 4);
    }

    internal static void ClearTransparentGrayscaleAlpha16Pixel(PngPixelBuffer buffer, int sourceOffset, byte[] output, int destinationOffset)
    {
        if (PngBinaryReader.ReadUInt16(buffer.Data, sourceOffset + 2) == 0)
        {
            output[destinationOffset] = 0;
            output[destinationOffset + 1] = 0;
            output[destinationOffset + 2] = 0;
            output[destinationOffset + 3] = 0;
            return;
        }

        CopyBytes(buffer.Data, sourceOffset, output, destinationOffset, 4);
    }

    internal static void ClearTransparentRgba16Pixel(PngPixelBuffer buffer, int sourceOffset, byte[] output, int destinationOffset)
    {
        if (PngBinaryReader.ReadUInt16(buffer.Data, sourceOffset + 6) == 0)
        {
            output[destinationOffset] = 0;
            output[destinationOffset + 1] = 0;
            output[destinationOffset + 2] = 0;
            output[destinationOffset + 3] = 0;
            output[destinationOffset + 4] = 0;
            output[destinationOffset + 5] = 0;
            output[destinationOffset + 6] = 0;
            output[destinationOffset + 7] = 0;
            return;
        }

        CopyBytes(buffer.Data, sourceOffset, output, destinationOffset, 8);
    }

    internal static byte CompositeSample8(byte foreground, byte background, byte alpha)
    {
        int sample = CompositeSample(foreground, background, alpha, 255);
        return (byte)sample;
    }

    internal static byte PremultiplySample8(byte sample, byte alpha)
    {
        int premultiplied = CompositeSample(sample, 0, alpha, 255);
        return (byte)premultiplied;
    }

    internal static byte UnpremultiplySample8(byte sample, byte alpha)
    {
        int unpremultiplied = UnpremultiplySample(sample, alpha, 255);
        return (byte)unpremultiplied;
    }

    internal static void WriteCompositeSample16(byte[] output, int offset, ushort foreground, ushort background, ushort alpha)
    {
        int sample = CompositeSample(foreground, background, alpha, 65535);
        PngBinaryWriter.WriteUInt16(output, offset, (ushort)sample);
    }

    internal static void WritePremultipliedSample16(byte[] output, int offset, ushort sample, ushort alpha)
    {
        int premultiplied = CompositeSample(sample, 0, alpha, 65535);
        PngBinaryWriter.WriteUInt16(output, offset, (ushort)premultiplied);
    }

    internal static void WriteUnpremultipliedSample16(byte[] output, int offset, ushort sample, ushort alpha)
    {
        int unpremultiplied = UnpremultiplySample(sample, alpha, 65535);
        PngBinaryWriter.WriteUInt16(output, offset, (ushort)unpremultiplied);
    }

    internal static int CompositeSample(int foreground, int background, int alpha, int maximum)
    {
        long inverseAlpha = maximum - alpha;
        long sample = ((long)foreground * alpha + (long)background * inverseAlpha + maximum / 2) / maximum;
        return (int)sample;
    }

    internal static int UnpremultiplySample(int sample, int alpha, int maximum)
    {
        if (alpha == 0)
        {
            return 0;
        }

        long unpremultiplied = ((long)sample * maximum + alpha / 2) / alpha;
        if (unpremultiplied > maximum)
        {
            return maximum;
        }

        return (int)unpremultiplied;
    }

    internal static void ValidateAddAlphaBuffer(PngPixelBuffer buffer)
    {
        int inputBytesPerPixel = GetAddAlphaInputBytesPerPixel(buffer.Format);
        ValidateBuffer(buffer, inputBytesPerPixel);
    }

    internal static void ValidateStripAlphaBuffer(PngPixelBuffer buffer)
    {
        int inputBytesPerPixel = GetAlphaInputBytesPerPixel(buffer.Format);
        ValidateBuffer(buffer, inputBytesPerPixel);
    }

    internal static void ValidateInvertAlphaBuffer(PngPixelBuffer buffer)
    {
        int inputBytesPerPixel = GetAlphaInputBytesPerPixel(buffer.Format);
        ValidateBuffer(buffer, inputBytesPerPixel);
    }

    internal static void ValidateCompositeBuffer(PngPixelBuffer buffer, PngBackgroundColor background)
    {
        int inputBytesPerPixel = GetAlphaInputBytesPerPixel(buffer.Format);
        ValidateBuffer(buffer, inputBytesPerPixel);
        ValidateCompositeBackground(buffer.Format, background);
    }

    internal static void ValidatePremultiplyAlphaBuffer(PngPixelBuffer buffer)
    {
        int inputBytesPerPixel = GetAlphaInputBytesPerPixel(buffer.Format);
        ValidateBuffer(buffer, inputBytesPerPixel);
    }

    internal static void ValidateUnpremultiplyAlphaBuffer(PngPixelBuffer buffer)
    {
        int inputBytesPerPixel = GetAlphaInputBytesPerPixel(buffer.Format);
        ValidateBuffer(buffer, inputBytesPerPixel);
    }

    internal static void ValidateClearTransparentPixelColorsBuffer(PngPixelBuffer buffer)
    {
        int inputBytesPerPixel = GetAlphaInputBytesPerPixel(buffer.Format);
        ValidateBuffer(buffer, inputBytesPerPixel);
    }

    internal static void ValidateBuffer(PngPixelBuffer buffer, int inputBytesPerPixel)
    {
        if (buffer.Width <= 0)
        {
            throw new ArgumentOutOfRangeException("Width");
        }

        if (buffer.Height <= 0)
        {
            throw new ArgumentOutOfRangeException("Height");
        }

        int rowBytes = CalculateOutputStride(buffer.Width, inputBytesPerPixel);
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

    internal static void ValidateCompositeBackground(PngPixelFormat format, PngBackgroundColor background)
    {
        if (format == PngPixelFormat.GrayscaleAlpha8)
        {
            ValidateBackgroundSample(background.Gray, 255, "Gray");
            return;
        }

        if (format == PngPixelFormat.Rgba8)
        {
            ValidateBackgroundSample(background.Red, 255, "Red");
            ValidateBackgroundSample(background.Green, 255, "Green");
            ValidateBackgroundSample(background.Blue, 255, "Blue");
            return;
        }

        if (format == PngPixelFormat.GrayscaleAlpha16)
        {
            ValidateBackgroundSample(background.Gray, 65535, "Gray");
            return;
        }

        if (format == PngPixelFormat.Rgba16)
        {
            ValidateBackgroundSample(background.Red, 65535, "Red");
            ValidateBackgroundSample(background.Green, 65535, "Green");
            ValidateBackgroundSample(background.Blue, 65535, "Blue");
            return;
        }

        throw new ArgumentOutOfRangeException("Format");
    }

    internal static void ValidateBackgroundSample(ushort sample, int maximum, string name)
    {
        if (sample > maximum)
        {
            throw new ArgumentOutOfRangeException(name);
        }
    }

    internal static int GetAddAlphaInputBytesPerPixel(PngPixelFormat pixelFormat)
    {
        switch (pixelFormat)
        {
            case PngPixelFormat.Grayscale8:
                return 1;
            case PngPixelFormat.Grayscale16:
                return 2;
            case PngPixelFormat.Rgb8:
                return 3;
            case PngPixelFormat.Rgb16:
                return 6;
            default:
                throw new ArgumentOutOfRangeException("Format");
        }
    }

    internal static PngPixelFormat GetAddAlphaOutputFormat(PngPixelFormat inputFormat)
    {
        switch (inputFormat)
        {
            case PngPixelFormat.Grayscale8:
                return PngPixelFormat.GrayscaleAlpha8;
            case PngPixelFormat.Grayscale16:
                return PngPixelFormat.GrayscaleAlpha16;
            case PngPixelFormat.Rgb8:
                return PngPixelFormat.Rgba8;
            case PngPixelFormat.Rgb16:
                return PngPixelFormat.Rgba16;
            default:
                throw new ArgumentOutOfRangeException("Format");
        }
    }

    internal static int GetAddAlphaOutputBytesPerPixel(PngPixelFormat pixelFormat)
    {
        switch (pixelFormat)
        {
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

    internal static int GetAlphaInputBytesPerPixel(PngPixelFormat pixelFormat)
    {
        switch (pixelFormat)
        {
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

    internal static int GetAlphaOffset(PngPixelFormat pixelFormat)
    {
        switch (pixelFormat)
        {
            case PngPixelFormat.GrayscaleAlpha8:
                return 1;
            case PngPixelFormat.GrayscaleAlpha16:
                return 2;
            case PngPixelFormat.Rgba8:
                return 3;
            case PngPixelFormat.Rgba16:
                return 6;
            default:
                throw new ArgumentOutOfRangeException("Format");
        }
    }

    internal static int GetAlphaBytes(PngPixelFormat pixelFormat)
    {
        switch (pixelFormat)
        {
            case PngPixelFormat.GrayscaleAlpha8:
                return 1;
            case PngPixelFormat.GrayscaleAlpha16:
                return 2;
            case PngPixelFormat.Rgba8:
                return 1;
            case PngPixelFormat.Rgba16:
                return 2;
            default:
                throw new ArgumentOutOfRangeException("Format");
        }
    }

    internal static PngPixelFormat GetStripAlphaOutputFormat(PngPixelFormat inputFormat)
    {
        switch (inputFormat)
        {
            case PngPixelFormat.GrayscaleAlpha8:
                return PngPixelFormat.Grayscale8;
            case PngPixelFormat.GrayscaleAlpha16:
                return PngPixelFormat.Grayscale16;
            case PngPixelFormat.Rgba8:
                return PngPixelFormat.Rgb8;
            case PngPixelFormat.Rgba16:
                return PngPixelFormat.Rgb16;
            default:
                throw new ArgumentOutOfRangeException("Format");
        }
    }

    internal static int GetStripAlphaOutputBytesPerPixel(PngPixelFormat pixelFormat)
    {
        switch (pixelFormat)
        {
            case PngPixelFormat.Grayscale8:
                return 1;
            case PngPixelFormat.Grayscale16:
                return 2;
            case PngPixelFormat.Rgb8:
                return 3;
            case PngPixelFormat.Rgb16:
                return 6;
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
