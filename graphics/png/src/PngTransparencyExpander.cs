namespace png;

/// <summary>
/// Converts PNG transparency metadata into explicit alpha channels.
/// </summary>
internal static class PngTransparencyExpander
{
    /// <summary>
    /// Looks up each palette index, applies its optional transparency sample, and returns an 8-bit RGBA buffer.
    /// </summary>
    /// <param name="indexedBuffer">The source indexed-color pixel buffer.</param>
    /// <param name="palette">The palette whose entry order matches the pixel indices.</param>
    /// <param name="transparency">The PNG transparency samples to expand.</param>
    /// <returns>A newly allocated pixel buffer containing the result.</returns>
    public static PngPixelBuffer ExpandIndexedToRgba8(PngPixelBuffer indexedBuffer, PngPalette palette, PngIndexedTransparency transparency)
    {
        ArgumentNullException.ThrowIfNull(indexedBuffer);
        ArgumentNullException.ThrowIfNull(palette);
        ArgumentNullException.ThrowIfNull(transparency);
        ArgumentNullException.ThrowIfNull(transparency.Alpha);

        ValidatePalette(palette);
        PngPixelBuffer indices = PngPaletteIndexPixelConverter.ToIndexed8(indexedBuffer);
        int outputStride = CalculateOutputStride(indices.Width, 4);
        byte[] rgba = CreateOutputData(outputStride, indices.Height);

        int rowIndex = 0;
        while (rowIndex < indices.Height)
        {
            ExpandIndexedRow(indices, rowIndex, palette, transparency, rgba, outputStride);
            rowIndex += 1;
        }

        return CreateBuffer(PngPixelFormat.Rgba8, indices.Width, indices.Height, outputStride, rgba);
    }

    /// <summary>
    /// Looks up each palette index, applies its optional transparency sample, and returns an 8-bit RGBA buffer.
    /// </summary>
    /// <param name="indexedBuffer">The source indexed-color pixel buffer.</param>
    /// <param name="plteChunk">The plte chunk value.</param>
    /// <param name="trnsChunk">The trns chunk value.</param>
    /// <returns>A newly allocated pixel buffer containing the result.</returns>
    public static PngPixelBuffer ExpandIndexedToRgba8(PngPixelBuffer indexedBuffer, PngChunk plteChunk, PngChunk trnsChunk)
    {
        ArgumentNullException.ThrowIfNull(plteChunk);
        ArgumentNullException.ThrowIfNull(trnsChunk);

        PngPalette palette = PngPlteParser.Parse(plteChunk);
        PngIndexedTransparency transparency = PngTransparencyParser.ParseIndexed(trnsChunk);
        return ExpandIndexedToRgba8(indexedBuffer, palette, transparency);
    }

    /// <summary>
    /// Adds an alpha channel that is zero for the declared transparent grayscale sample and opaque otherwise.
    /// </summary>
    /// <param name="grayscaleBuffer">The source grayscale pixel buffer.</param>
    /// <param name="transparency">The PNG transparency samples to expand.</param>
    /// <returns>A newly allocated pixel buffer containing the result.</returns>
    public static PngPixelBuffer ExpandGrayscaleToAlpha(PngPixelBuffer grayscaleBuffer, PngGrayscaleTransparency transparency)
    {
        ArgumentNullException.ThrowIfNull(grayscaleBuffer);
        ArgumentNullException.ThrowIfNull(grayscaleBuffer.Data);
        ArgumentNullException.ThrowIfNull(transparency);

        PngBitDepth bitDepth = GetGrayscaleBitDepth(grayscaleBuffer.Format);
        ValidateBuffer(grayscaleBuffer, bitDepth);

        if (grayscaleBuffer.Format == PngPixelFormat.Grayscale16)
        {
            return ExpandGrayscale16ToAlpha(grayscaleBuffer, transparency);
        }

        return ExpandGrayscaleToAlpha8(grayscaleBuffer, transparency, bitDepth);
    }

    /// <summary>
    /// Adds an alpha channel that is zero for the declared transparent grayscale sample and opaque otherwise.
    /// </summary>
    /// <param name="grayscaleBuffer">The source grayscale pixel buffer.</param>
    /// <param name="trnsChunk">The trns chunk value.</param>
    /// <returns>A newly allocated pixel buffer containing the result.</returns>
    public static PngPixelBuffer ExpandGrayscaleToAlpha(PngPixelBuffer grayscaleBuffer, PngChunk trnsChunk)
    {
        ArgumentNullException.ThrowIfNull(trnsChunk);

        PngGrayscaleTransparency transparency = PngTransparencyParser.ParseGrayscale(trnsChunk);
        return ExpandGrayscaleToAlpha(grayscaleBuffer, transparency);
    }

    /// <summary>
    /// Adds an alpha channel that is zero for the declared transparent RGB sample and opaque otherwise.
    /// </summary>
    /// <param name="rgbBuffer">The source RGB-family pixel buffer.</param>
    /// <param name="transparency">The PNG transparency samples to expand.</param>
    /// <returns>A newly allocated pixel buffer containing the result.</returns>
    public static PngPixelBuffer ExpandTruecolorToAlpha(PngPixelBuffer rgbBuffer, PngTruecolorTransparency transparency)
    {
        ArgumentNullException.ThrowIfNull(rgbBuffer);
        ArgumentNullException.ThrowIfNull(rgbBuffer.Data);
        ArgumentNullException.ThrowIfNull(transparency);

        if (PngReorderedPixelTransformer.IsReordered(rgbBuffer.Format))
        {
            PngPixelBuffer transformed = ExpandTruecolorToAlpha(PngReorderedPixelTransformer.Normalize(rgbBuffer), transparency);
            return PngReorderedPixelTransformer.Restore(rgbBuffer.Format, transformed);
        }

        ValidateTruecolorBuffer(rgbBuffer);

        if (rgbBuffer.Format == PngPixelFormat.Rgb16)
        {
            return ExpandRgb16ToRgba16(rgbBuffer, transparency);
        }

        return ExpandRgb8ToRgba8(rgbBuffer, transparency);
    }

    /// <summary>
    /// Adds an alpha channel that is zero for the declared transparent RGB sample and opaque otherwise.
    /// </summary>
    /// <param name="rgbBuffer">The source RGB-family pixel buffer.</param>
    /// <param name="trnsChunk">The trns chunk value.</param>
    /// <returns>A newly allocated pixel buffer containing the result.</returns>
    public static PngPixelBuffer ExpandTruecolorToAlpha(PngPixelBuffer rgbBuffer, PngChunk trnsChunk)
    {
        ArgumentNullException.ThrowIfNull(trnsChunk);

        PngTruecolorTransparency transparency = PngTransparencyParser.ParseTruecolor(trnsChunk);
        return ExpandTruecolorToAlpha(rgbBuffer, transparency);
    }

    internal static void ExpandIndexedRow(PngPixelBuffer indices, int rowIndex, PngPalette palette, PngIndexedTransparency transparency, byte[] rgba, int outputStride)
    {
        int sourceOffset = indices.Stride * rowIndex;
        int destinationOffset = outputStride * rowIndex;
        int x = 0;
        while (x < indices.Width)
        {
            int paletteIndex = indices.Data[sourceOffset + x];
            if (paletteIndex >= palette.Entries.Length)
            {
                throw new ArgumentOutOfRangeException("palette");
            }

            PngPaletteEntry entry = palette.Entries[paletteIndex];
            rgba[destinationOffset] = entry.Red;
            rgba[destinationOffset + 1] = entry.Green;
            rgba[destinationOffset + 2] = entry.Blue;
            rgba[destinationOffset + 3] = GetIndexedAlpha(transparency, paletteIndex);
            destinationOffset += 4;
            x += 1;
        }
    }

    internal static PngPixelBuffer ExpandGrayscaleToAlpha8(PngPixelBuffer grayscaleBuffer, PngGrayscaleTransparency transparency, PngBitDepth bitDepth)
    {
        int outputStride = CalculateOutputStride(grayscaleBuffer.Width, 2);
        byte[] output = CreateOutputData(outputStride, grayscaleBuffer.Height);
        int rowIndex = 0;
        while (rowIndex < grayscaleBuffer.Height)
        {
            ExpandGrayscale8Row(grayscaleBuffer, rowIndex, transparency, bitDepth, output, outputStride);
            rowIndex += 1;
        }

        return CreateBuffer(PngPixelFormat.GrayscaleAlpha8, grayscaleBuffer.Width, grayscaleBuffer.Height, outputStride, output);
    }

    internal static PngPixelBuffer ExpandGrayscale16ToAlpha(PngPixelBuffer grayscaleBuffer, PngGrayscaleTransparency transparency)
    {
        int outputStride = CalculateOutputStride(grayscaleBuffer.Width, 4);
        byte[] output = CreateOutputData(outputStride, grayscaleBuffer.Height);
        int rowIndex = 0;
        while (rowIndex < grayscaleBuffer.Height)
        {
            ExpandGrayscale16Row(grayscaleBuffer, rowIndex, transparency, output, outputStride);
            rowIndex += 1;
        }

        return CreateBuffer(PngPixelFormat.GrayscaleAlpha16, grayscaleBuffer.Width, grayscaleBuffer.Height, outputStride, output);
    }

    internal static PngPixelBuffer ExpandRgb8ToRgba8(PngPixelBuffer rgbBuffer, PngTruecolorTransparency transparency)
    {
        int outputStride = CalculateOutputStride(rgbBuffer.Width, 4);
        byte[] output = CreateOutputData(outputStride, rgbBuffer.Height);
        int rowIndex = 0;
        while (rowIndex < rgbBuffer.Height)
        {
            ExpandRgb8Row(rgbBuffer, rowIndex, transparency, output, outputStride);
            rowIndex += 1;
        }

        return CreateBuffer(PngPixelFormat.Rgba8, rgbBuffer.Width, rgbBuffer.Height, outputStride, output);
    }

    internal static PngPixelBuffer ExpandRgb16ToRgba16(PngPixelBuffer rgbBuffer, PngTruecolorTransparency transparency)
    {
        int outputStride = CalculateOutputStride(rgbBuffer.Width, 8);
        byte[] output = CreateOutputData(outputStride, rgbBuffer.Height);
        int rowIndex = 0;
        while (rowIndex < rgbBuffer.Height)
        {
            ExpandRgb16Row(rgbBuffer, rowIndex, transparency, output, outputStride);
            rowIndex += 1;
        }

        return CreateBuffer(PngPixelFormat.Rgba16, rgbBuffer.Width, rgbBuffer.Height, outputStride, output);
    }

    internal static void ExpandGrayscale8Row(PngPixelBuffer grayscaleBuffer, int rowIndex, PngGrayscaleTransparency transparency, PngBitDepth bitDepth, byte[] output, int outputStride)
    {
        int sourceOffset = grayscaleBuffer.Stride * rowIndex;
        int destinationOffset = outputStride * rowIndex;
        byte[] samples = GetGrayscale8Samples(grayscaleBuffer, sourceOffset, bitDepth);
        int x = 0;
        while (x < grayscaleBuffer.Width)
        {
            int sample = samples[x];
            output[destinationOffset] = ScaleGrayscaleSample(sample, bitDepth);
            output[destinationOffset + 1] = GetSampleAlpha(sample, transparency.Gray);
            destinationOffset += 2;
            x += 1;
        }
    }

    internal static void ExpandGrayscale16Row(PngPixelBuffer grayscaleBuffer, int rowIndex, PngGrayscaleTransparency transparency, byte[] output, int outputStride)
    {
        int sourceOffset = grayscaleBuffer.Stride * rowIndex;
        int destinationOffset = outputStride * rowIndex;
        int x = 0;
        while (x < grayscaleBuffer.Width)
        {
            ushort sample = PngBinaryReader.ReadUInt16(grayscaleBuffer.Data, sourceOffset);
            output[destinationOffset] = grayscaleBuffer.Data[sourceOffset];
            output[destinationOffset + 1] = grayscaleBuffer.Data[sourceOffset + 1];
            WriteAlpha16(output, destinationOffset + 2, sample == transparency.Gray);
            sourceOffset += 2;
            destinationOffset += 4;
            x += 1;
        }
    }

    internal static void ExpandRgb8Row(PngPixelBuffer rgbBuffer, int rowIndex, PngTruecolorTransparency transparency, byte[] output, int outputStride)
    {
        int sourceOffset = rgbBuffer.Stride * rowIndex;
        int destinationOffset = outputStride * rowIndex;
        int x = 0;
        while (x < rgbBuffer.Width)
        {
            byte red = rgbBuffer.Data[sourceOffset];
            byte green = rgbBuffer.Data[sourceOffset + 1];
            byte blue = rgbBuffer.Data[sourceOffset + 2];
            output[destinationOffset] = red;
            output[destinationOffset + 1] = green;
            output[destinationOffset + 2] = blue;
            output[destinationOffset + 3] = GetRgb8Alpha(red, green, blue, transparency);
            sourceOffset += 3;
            destinationOffset += 4;
            x += 1;
        }
    }

    internal static void ExpandRgb16Row(PngPixelBuffer rgbBuffer, int rowIndex, PngTruecolorTransparency transparency, byte[] output, int outputStride)
    {
        int sourceOffset = rgbBuffer.Stride * rowIndex;
        int destinationOffset = outputStride * rowIndex;
        int x = 0;
        while (x < rgbBuffer.Width)
        {
            ushort red = PngBinaryReader.ReadUInt16(rgbBuffer.Data, sourceOffset);
            ushort green = PngBinaryReader.ReadUInt16(rgbBuffer.Data, sourceOffset + 2);
            ushort blue = PngBinaryReader.ReadUInt16(rgbBuffer.Data, sourceOffset + 4);
            CopyBytes(rgbBuffer.Data, sourceOffset, output, destinationOffset, 6);
            WriteAlpha16(output, destinationOffset + 6, red == transparency.Red && green == transparency.Green && blue == transparency.Blue);
            sourceOffset += 6;
            destinationOffset += 8;
            x += 1;
        }
    }

    internal static byte[] GetGrayscale8Samples(PngPixelBuffer grayscaleBuffer, int sourceOffset, PngBitDepth bitDepth)
    {
        if (grayscaleBuffer.Format == PngPixelFormat.Grayscale8)
        {
            return CopyGrayscale8Samples(grayscaleBuffer, sourceOffset);
        }

        return PngPackedSampleUnpacker.Unpack(grayscaleBuffer.Data, sourceOffset, grayscaleBuffer.Width, bitDepth);
    }

    internal static byte[] CopyGrayscale8Samples(PngPixelBuffer grayscaleBuffer, int sourceOffset)
    {
        byte[] samples = new byte[grayscaleBuffer.Width];
        CopyBytes(grayscaleBuffer.Data, sourceOffset, samples, 0, samples.Length);
        return samples;
    }

    internal static byte GetIndexedAlpha(PngIndexedTransparency transparency, int paletteIndex)
    {
        if (paletteIndex >= transparency.Alpha.Length)
        {
            return 255;
        }

        return transparency.Alpha[paletteIndex];
    }

    internal static byte GetSampleAlpha(int sample, ushort transparentSample)
    {
        if (sample == transparentSample)
        {
            return 0;
        }

        return 255;
    }

    internal static byte GetRgb8Alpha(byte red, byte green, byte blue, PngTruecolorTransparency transparency)
    {
        if (transparency.Red <= 255 && transparency.Green <= 255 && transparency.Blue <= 255 && red == transparency.Red && green == transparency.Green && blue == transparency.Blue)
        {
            return 0;
        }

        return 255;
    }

    internal static void WriteAlpha16(byte[] output, int offset, bool isTransparent)
    {
        if (isTransparent)
        {
            output[offset] = 0;
            output[offset + 1] = 0;
            return;
        }

        output[offset] = 255;
        output[offset + 1] = 255;
    }

    internal static byte ScaleGrayscaleSample(int sample, PngBitDepth bitDepth)
    {
        switch (bitDepth)
        {
            case PngBitDepth.One:
                return (byte)(sample * 255);
            case PngBitDepth.Two:
                return (byte)(sample * 85);
            case PngBitDepth.Four:
                return (byte)(sample * 17);
            case PngBitDepth.Eight:
                return (byte)sample;
            default:
                throw new ArgumentOutOfRangeException("bitDepth");
        }
    }

    internal static PngBitDepth GetGrayscaleBitDepth(PngPixelFormat pixelFormat)
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

        if (pixelFormat == PngPixelFormat.Grayscale8)
        {
            return PngBitDepth.Eight;
        }

        if (pixelFormat == PngPixelFormat.Grayscale16)
        {
            return PngBitDepth.Sixteen;
        }

        throw new ArgumentOutOfRangeException("Format");
    }

    internal static void ValidateTruecolorBuffer(PngPixelBuffer buffer)
    {
        if (buffer.Format != PngPixelFormat.Rgb8 && buffer.Format != PngPixelFormat.Rgb16)
        {
            throw new ArgumentOutOfRangeException("Format");
        }

        ValidateBuffer(buffer, GetTruecolorBitDepth(buffer.Format));
    }

    internal static PngBitDepth GetTruecolorBitDepth(PngPixelFormat pixelFormat)
    {
        if (pixelFormat == PngPixelFormat.Rgb8)
        {
            return PngBitDepth.Eight;
        }

        if (pixelFormat == PngPixelFormat.Rgb16)
        {
            return PngBitDepth.Sixteen;
        }

        throw new ArgumentOutOfRangeException("Format");
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

    internal static int CalculateInputRowBytes(int width, PngPixelFormat pixelFormat, PngBitDepth bitDepth)
    {
        if (pixelFormat == PngPixelFormat.Grayscale1 || pixelFormat == PngPixelFormat.Grayscale2 || pixelFormat == PngPixelFormat.Grayscale4)
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

        long calculatedRowBytes = PngSizeCalculator.CalculateFrameRowBytes(width, pixelFormat);
        if (calculatedRowBytes > int.MaxValue)
        {
            throw new OverflowException();
        }

        return (int)calculatedRowBytes;
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

    internal static void ValidatePalette(PngPalette palette)
    {
        ArgumentNullException.ThrowIfNull(palette.Entries);

        if (palette.Entries.Length < 1 || palette.Entries.Length > 256)
        {
            throw new ArgumentOutOfRangeException("palette");
        }

        int index = 0;
        while (index < palette.Entries.Length)
        {
            if (palette.Entries[index] == null)
            {
                throw new ArgumentNullException("Entries");
            }

            index += 1;
        }
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
