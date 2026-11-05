using System;

namespace tiff;

internal class TiffRgba8Writer
{
    internal static TiffSourceRasterWriteResult Write(TiffImageInfo info, ulong[] offsets, ulong[] byteCounts, Span<byte> destination, ReadOnlySpan<byte> rgba)
    {
        return Write(info, offsets, byteCounts, destination, rgba, new TiffLimits());
    }

    internal static TiffSourceRasterWriteResult Write(TiffImageInfo info, ulong[] offsets, ulong[] byteCounts, Span<byte> destination, ReadOnlySpan<byte> rgba, TiffLimits limits)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        return Write(info, offsets, byteCounts, destination, rgba, limits, diagnostics);
    }

    internal static TiffSourceRasterWriteResult Write(TiffImageInfo info, ulong[] offsets, ulong[] byteCounts, Span<byte> destination, ReadOnlySpan<byte> rgba, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        if (info == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "Image information is required to write an RGBA8 raster.");
            return TiffSourceRasterWriteResult.Create(0, 0, TiffCompression.Unknown, diagnostics);
        }

        byte[] source = ConvertRgba8ToSourceBytes(info, rgba, limits, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffSourceRasterWriteResult.Create(0, 0, info.Compression, diagnostics);
        }

        return TiffSourceRasterWriter.Write(info, offsets, byteCounts, destination, source, limits, diagnostics);
    }

    internal static byte[] ConvertRgba8ToSourceBytes(TiffImageInfo info, ReadOnlySpan<byte> rgba, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        if (!ValidateRgbaRequest(info, rgba.Length, diagnostics))
        {
            return new byte[0];
        }

        int[] sampleByteCounts = TiffSourceRasterWriter.CalculateByteAlignedSampleByteCounts(info, diagnostics);
        if (diagnostics.HasErrors())
        {
            return new byte[0];
        }

        int bytesPerPixel = TiffRgba8Reader.CalculateBytesPerPixel(sampleByteCounts, diagnostics);
        ulong pixelCount = TiffRgba8Reader.CalculatePixelCount(info.Width, info.Length, diagnostics);
        ulong totalByteCount = TiffRgba8Reader.CalculateTotalByteCount(pixelCount, bytesPerPixel, limits, diagnostics);
        if (diagnostics.HasErrors())
        {
            return new byte[0];
        }

        byte[] source = new byte[(int)totalByteCount];
        int pixelIndex = 0;
        while (pixelIndex < (int)pixelCount)
        {
            WritePixel(info, rgba, pixelIndex, source, sampleByteCounts, bytesPerPixel, diagnostics);
            if (diagnostics.HasErrors())
            {
                return new byte[0];
            }

            pixelIndex = pixelIndex + 1;
        }

        return source;
    }

    internal static bool ValidateRgbaRequest(TiffImageInfo info, int rgbaByteCount, TiffDiagnostics diagnostics)
    {
        if (info.Width == 0 || info.Length == 0)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, "RGBA8 writing requires positive image dimensions.");
            return false;
        }

        if (rgbaByteCount % 4 != 0)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "RGBA8 source buffer must contain complete four-channel pixels.");
            return false;
        }

        ulong pixelCount = TiffRgba8Reader.CalculatePixelCount(info.Width, info.Length, diagnostics);
        if (diagnostics.HasErrors())
        {
            return false;
        }

        if (pixelCount > int.MaxValue / 4)
        {
            diagnostics.AddError(TiffErrorCode.AllocationLimitExceeded, "RGBA8 source pixel count exceeds the supported array range.");
            return false;
        }

        if ((ulong)rgbaByteCount != pixelCount * 4UL)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "RGBA8 source byte count does not match image dimensions.");
            return false;
        }

        return true;
    }

    internal static void WritePixel(TiffImageInfo info, ReadOnlySpan<byte> rgba, int pixelIndex, byte[] source, int[] sampleByteCounts, int bytesPerPixel, TiffDiagnostics diagnostics)
    {
        int rgbaOffset = pixelIndex * 4;
        double alpha = (double)rgba[rgbaOffset + 3] / 255d;
        double red = (double)rgba[rgbaOffset] / 255d;
        double green = (double)rgba[rgbaOffset + 1] / 255d;
        double blue = (double)rgba[rgbaOffset + 2] / 255d;
        if (TiffRgba8Reader.HasAssociatedAlpha(info))
        {
            red = red * alpha;
            green = green * alpha;
            blue = blue * alpha;
        }

        if (info.Photometric == TiffPhotometric.BlackIsZero || info.Photometric == TiffPhotometric.WhiteIsZero)
        {
            WriteGrayscalePixel(info, red, green, blue, alpha, source, pixelIndex, sampleByteCounts, bytesPerPixel, diagnostics);
            return;
        }

        if (info.Photometric == TiffPhotometric.Palette)
        {
            WritePalettePixel(info, rgba[rgbaOffset], rgba[rgbaOffset + 1], rgba[rgbaOffset + 2], alpha, source, pixelIndex, sampleByteCounts, bytesPerPixel, diagnostics);
            return;
        }

        if (info.Photometric == TiffPhotometric.Rgb)
        {
            WriteRgbPixel(info, red, green, blue, alpha, source, pixelIndex, sampleByteCounts, bytesPerPixel, diagnostics);
            return;
        }

        if (info.Photometric == TiffPhotometric.Cmyk)
        {
            WriteCmykPixel(info, red, green, blue, alpha, source, pixelIndex, sampleByteCounts, bytesPerPixel, diagnostics);
            return;
        }

        if (info.Photometric == TiffPhotometric.YCbCr)
        {
            WriteYCbCrPixel(info, red, green, blue, alpha, source, pixelIndex, sampleByteCounts, bytesPerPixel, diagnostics);
            return;
        }

        diagnostics.AddError(TiffErrorCode.UnsupportedPhotometric, "RGBA8 writing does not support this photometric interpretation.");
    }

    internal static void WriteGrayscalePixel(TiffImageInfo info, double red, double green, double blue, double alpha, byte[] source, int pixelIndex, int[] sampleByteCounts, int bytesPerPixel, TiffDiagnostics diagnostics)
    {
        double luminance = 0.299d * red + 0.587d * green + 0.114d * blue;
        if (info.Photometric == TiffPhotometric.WhiteIsZero)
        {
            luminance = 1d - luminance;
        }

        WriteIntensitySample(info, 0, luminance, source, pixelIndex, sampleByteCounts, bytesPerPixel, diagnostics);
        WriteAlphaIfPresent(info, alpha, source, pixelIndex, sampleByteCounts, bytesPerPixel, diagnostics);
    }

    internal static void WritePalettePixel(TiffImageInfo info, byte red, byte green, byte blue, double alpha, byte[] source, int pixelIndex, int[] sampleByteCounts, int bytesPerPixel, TiffDiagnostics diagnostics)
    {
        int paletteIndex = FindNearestPaletteIndex(info, red, green, blue, diagnostics);
        if (diagnostics.HasErrors())
        {
            return;
        }

        WriteUnsignedSample(info, 0, (ulong)paletteIndex, source, pixelIndex, sampleByteCounts, bytesPerPixel, diagnostics);
        WriteAlphaIfPresent(info, alpha, source, pixelIndex, sampleByteCounts, bytesPerPixel, diagnostics);
    }

    internal static void WriteRgbPixel(TiffImageInfo info, double red, double green, double blue, double alpha, byte[] source, int pixelIndex, int[] sampleByteCounts, int bytesPerPixel, TiffDiagnostics diagnostics)
    {
        WriteIntensitySample(info, 0, red, source, pixelIndex, sampleByteCounts, bytesPerPixel, diagnostics);
        WriteIntensitySample(info, 1, green, source, pixelIndex, sampleByteCounts, bytesPerPixel, diagnostics);
        WriteIntensitySample(info, 2, blue, source, pixelIndex, sampleByteCounts, bytesPerPixel, diagnostics);
        WriteAlphaIfPresent(info, alpha, source, pixelIndex, sampleByteCounts, bytesPerPixel, diagnostics);
    }

    internal static void WriteCmykPixel(TiffImageInfo info, double red, double green, double blue, double alpha, byte[] source, int pixelIndex, int[] sampleByteCounts, int bytesPerPixel, TiffDiagnostics diagnostics)
    {
        double black = 1d - Maximum(red, green, blue);
        double cyan = 0d;
        double magenta = 0d;
        double yellow = 0d;
        if (black < 1d)
        {
            cyan = (1d - red - black) / (1d - black);
            magenta = (1d - green - black) / (1d - black);
            yellow = (1d - blue - black) / (1d - black);
        }

        WriteIntensitySample(info, 0, cyan, source, pixelIndex, sampleByteCounts, bytesPerPixel, diagnostics);
        WriteIntensitySample(info, 1, magenta, source, pixelIndex, sampleByteCounts, bytesPerPixel, diagnostics);
        WriteIntensitySample(info, 2, yellow, source, pixelIndex, sampleByteCounts, bytesPerPixel, diagnostics);
        WriteIntensitySample(info, 3, black, source, pixelIndex, sampleByteCounts, bytesPerPixel, diagnostics);
        WriteAlphaIfPresent(info, alpha, source, pixelIndex, sampleByteCounts, bytesPerPixel, diagnostics);
    }

    internal static void WriteYCbCrPixel(TiffImageInfo info, double red, double green, double blue, double alpha, byte[] source, int pixelIndex, int[] sampleByteCounts, int bytesPerPixel, TiffDiagnostics diagnostics)
    {
        if (!TiffYCbCrPhotometricConverter.ValidateCoefficients(info, diagnostics))
        {
            return;
        }

        if (!TiffYCbCrPhotometricConverter.ValidateReferenceBlackWhite(info, diagnostics))
        {
            return;
        }

        double lumaRed = info.YCbCrCoefficients[0];
        double lumaGreen = info.YCbCrCoefficients[1];
        double lumaBlue = info.YCbCrCoefficients[2];
        double y = lumaRed * red + lumaGreen * green + lumaBlue * blue;
        double cb = 0d;
        double cr = 0d;
        if (lumaBlue < 1d)
        {
            cb = (blue - y) / (2d - 2d * lumaBlue);
        }

        if (lumaRed < 1d)
        {
            cr = (red - y) / (2d - 2d * lumaRed);
        }

        double ySample = info.ReferenceBlackWhite[0] + y * (info.ReferenceBlackWhite[1] - info.ReferenceBlackWhite[0]);
        double cbSample = info.ReferenceBlackWhite[2] + cb * 2d * (info.ReferenceBlackWhite[3] - info.ReferenceBlackWhite[2]);
        double crSample = info.ReferenceBlackWhite[4] + cr * 2d * (info.ReferenceBlackWhite[5] - info.ReferenceBlackWhite[4]);
        WriteUnsignedRoundedSample(info, 0, ySample, source, pixelIndex, sampleByteCounts, bytesPerPixel, diagnostics);
        WriteUnsignedRoundedSample(info, 1, cbSample, source, pixelIndex, sampleByteCounts, bytesPerPixel, diagnostics);
        WriteUnsignedRoundedSample(info, 2, crSample, source, pixelIndex, sampleByteCounts, bytesPerPixel, diagnostics);
        WriteAlphaIfPresent(info, alpha, source, pixelIndex, sampleByteCounts, bytesPerPixel, diagnostics);
    }

    internal static void WriteAlphaIfPresent(TiffImageInfo info, double alpha, byte[] source, int pixelIndex, int[] sampleByteCounts, int bytesPerPixel, TiffDiagnostics diagnostics)
    {
        if (!TiffRgba8Reader.HasAlpha(info))
        {
            return;
        }

        int alphaChannelIndex = TiffRgba8Reader.CalculateBaseColorSampleCount(info);
        WriteIntensitySample(info, alphaChannelIndex, alpha, source, pixelIndex, sampleByteCounts, bytesPerPixel, diagnostics);
    }

    internal static void WriteIntensitySample(TiffImageInfo info, int sampleIndex, double intensity, byte[] source, int pixelIndex, int[] sampleByteCounts, int bytesPerPixel, TiffDiagnostics diagnostics)
    {
        int bitsPerSample = TiffSubByteSampleUnpacker.GetBitsPerSample(info, sampleIndex, diagnostics);
        if (diagnostics.HasErrors())
        {
            return;
        }

        ulong value = ScaleIntensityToUnsigned(intensity, bitsPerSample);
        WriteUnsignedSample(info, sampleIndex, value, source, pixelIndex, sampleByteCounts, bytesPerPixel, diagnostics);
    }

    internal static void WriteUnsignedRoundedSample(TiffImageInfo info, int sampleIndex, double value, byte[] source, int pixelIndex, int[] sampleByteCounts, int bytesPerPixel, TiffDiagnostics diagnostics)
    {
        int bitsPerSample = TiffSubByteSampleUnpacker.GetBitsPerSample(info, sampleIndex, diagnostics);
        if (diagnostics.HasErrors())
        {
            return;
        }

        double maximum = TiffGrayscalePhotometricConverter.CalculateUnsignedMaximum(bitsPerSample);
        if (value < 0d)
        {
            value = 0d;
        }

        if (value > maximum)
        {
            value = maximum;
        }

        WriteUnsignedSample(info, sampleIndex, (ulong)(value + 0.5d), source, pixelIndex, sampleByteCounts, bytesPerPixel, diagnostics);
    }

    internal static void WriteUnsignedSample(TiffImageInfo info, int sampleIndex, ulong value, byte[] source, int pixelIndex, int[] sampleByteCounts, int bytesPerPixel, TiffDiagnostics diagnostics)
    {
        TiffSampleFormat sampleFormat = TiffPredictor.GetSampleFormat(info, sampleIndex);
        if (sampleFormat != TiffSampleFormat.UnsignedInteger && sampleFormat != TiffSampleFormat.Undefined)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "RGBA8 writing supports unsigned or undefined integer target samples.", info.ImageIndex, TiffTagId.SampleFormat);
            return;
        }

        int channelOffset = TiffSourceRasterWriter.CalculateChannelOffset(sampleByteCounts, sampleIndex);
        int destinationOffset = pixelIndex * bytesPerPixel + channelOffset;
        WriteUnsignedLittleEndian(value, sampleByteCounts[sampleIndex], source, destinationOffset);
    }

    internal static ulong ScaleIntensityToUnsigned(double intensity, int bitsPerSample)
    {
        if (intensity < 0d)
        {
            intensity = 0d;
        }

        if (intensity > 1d)
        {
            intensity = 1d;
        }

        if (bitsPerSample == 64)
        {
            if (intensity >= 1d)
            {
                return ulong.MaxValue;
            }
        }

        double maximum = TiffGrayscalePhotometricConverter.CalculateUnsignedMaximum(bitsPerSample);
        return (ulong)(intensity * maximum + 0.5d);
    }

    internal static void WriteUnsignedLittleEndian(ulong value, int byteCount, byte[] destination, int offset)
    {
        int byteIndex = 0;
        while (byteIndex < byteCount)
        {
            destination[offset + byteIndex] = (byte)((value >> (8 * byteIndex)) & 0xffUL);
            byteIndex = byteIndex + 1;
        }
    }

    internal static int FindNearestPaletteIndex(TiffImageInfo info, byte red, byte green, byte blue, TiffDiagnostics diagnostics)
    {
        int bitsPerSample = TiffSubByteSampleUnpacker.GetBitsPerSample(info, 0, diagnostics);
        if (diagnostics.HasErrors())
        {
            return 0;
        }

        if (bitsPerSample <= 0 || bitsPerSample > 30)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "Palette target BitsPerSample is outside the supported range.", info.ImageIndex, TiffTagId.BitsPerSample);
            return 0;
        }

        int entryCount = 1 << bitsPerSample;
        if (info.ColorMap == null || info.ColorMap.Length != entryCount * 3)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagCount, "Palette target ColorMap count must match BitsPerSample.", info.ImageIndex, TiffTagId.ColorMap);
            return 0;
        }

        int bestIndex = 0;
        int bestDistance = int.MaxValue;
        int index = 0;
        while (index < entryCount)
        {
            int paletteRed = TiffPaletteColorMapExpander.ScaleColorMapValueToByte(info.ColorMap[index]);
            int paletteGreen = TiffPaletteColorMapExpander.ScaleColorMapValueToByte(info.ColorMap[entryCount + index]);
            int paletteBlue = TiffPaletteColorMapExpander.ScaleColorMapValueToByte(info.ColorMap[entryCount * 2 + index]);
            int redDelta = paletteRed - (int)red;
            int greenDelta = paletteGreen - (int)green;
            int blueDelta = paletteBlue - (int)blue;
            int distance = redDelta * redDelta + greenDelta * greenDelta + blueDelta * blueDelta;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestIndex = index;
            }

            index = index + 1;
        }

        return bestIndex;
    }

    internal static double Maximum(double first, double second, double third)
    {
        double maximum = first;
        if (second > maximum)
        {
            maximum = second;
        }

        if (third > maximum)
        {
            maximum = third;
        }

        return maximum;
    }
}
