using System;

namespace tiff;

internal class TiffRgba16Writer
{
    internal static TiffSourceRasterWriteResult Write(TiffImageInfo info, ulong[] offsets, ulong[] byteCounts, Span<byte> destination, ReadOnlySpan<ushort> rgba)
    {
        return Write(info, offsets, byteCounts, destination, rgba, new TiffLimits());
    }

    internal static TiffSourceRasterWriteResult Write(TiffImageInfo info, ulong[] offsets, ulong[] byteCounts, Span<byte> destination, ReadOnlySpan<ushort> rgba, TiffLimits limits)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        return Write(info, offsets, byteCounts, destination, rgba, limits, diagnostics);
    }

    internal static TiffSourceRasterWriteResult Write(TiffImageInfo info, ulong[] offsets, ulong[] byteCounts, Span<byte> destination, ReadOnlySpan<ushort> rgba, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        if (info == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "Image information is required to write an RGBA16 raster.");
            return TiffSourceRasterWriteResult.Create(0, 0, TiffCompression.Unknown, diagnostics);
        }

        byte[] source = ConvertRgba16ToSourceBytes(info, rgba, limits, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffSourceRasterWriteResult.Create(0, 0, info.Compression, diagnostics);
        }

        return TiffSourceRasterWriter.Write(info, offsets, byteCounts, destination, source, limits, diagnostics);
    }

    internal static byte[] ConvertRgba16ToSourceBytes(TiffImageInfo info, ReadOnlySpan<ushort> rgba, TiffLimits limits, TiffDiagnostics diagnostics)
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

    internal static bool ValidateRgbaRequest(TiffImageInfo info, int rgbaSampleCount, TiffDiagnostics diagnostics)
    {
        if (info.Width == 0 || info.Length == 0)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, "RGBA16 writing requires positive image dimensions.");
            return false;
        }

        if (rgbaSampleCount % 4 != 0)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "RGBA16 source buffer must contain complete four-channel pixels.");
            return false;
        }

        ulong pixelCount = TiffRgba8Reader.CalculatePixelCount(info.Width, info.Length, diagnostics);
        if (diagnostics.HasErrors())
        {
            return false;
        }

        if (pixelCount > int.MaxValue / 4)
        {
            diagnostics.AddError(TiffErrorCode.AllocationLimitExceeded, "RGBA16 source pixel count exceeds the supported array range.");
            return false;
        }

        if ((ulong)rgbaSampleCount != pixelCount * 4UL)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "RGBA16 source sample count does not match image dimensions.");
            return false;
        }

        return true;
    }

    internal static void WritePixel(TiffImageInfo info, ReadOnlySpan<ushort> rgba, int pixelIndex, byte[] source, int[] sampleByteCounts, int bytesPerPixel, TiffDiagnostics diagnostics)
    {
        int rgbaOffset = pixelIndex * 4;
        double alpha = (double)rgba[rgbaOffset + 3] / 65535d;
        double red = (double)rgba[rgbaOffset] / 65535d;
        double green = (double)rgba[rgbaOffset + 1] / 65535d;
        double blue = (double)rgba[rgbaOffset + 2] / 65535d;
        if (TiffRgba8Reader.HasAssociatedAlpha(info))
        {
            red = red * alpha;
            green = green * alpha;
            blue = blue * alpha;
        }

        if (info.Photometric == TiffPhotometric.BlackIsZero || info.Photometric == TiffPhotometric.WhiteIsZero)
        {
            TiffRgba8Writer.WriteGrayscalePixel(info, red, green, blue, alpha, source, pixelIndex, sampleByteCounts, bytesPerPixel, diagnostics);
            return;
        }

        if (info.Photometric == TiffPhotometric.Palette)
        {
            TiffRgba8Writer.WritePalettePixel(info, ScaleIntensityToByte(red), ScaleIntensityToByte(green), ScaleIntensityToByte(blue), alpha, source, pixelIndex, sampleByteCounts, bytesPerPixel, diagnostics);
            return;
        }

        if (info.Photometric == TiffPhotometric.Rgb)
        {
            TiffRgba8Writer.WriteRgbPixel(info, red, green, blue, alpha, source, pixelIndex, sampleByteCounts, bytesPerPixel, diagnostics);
            return;
        }

        if (info.Photometric == TiffPhotometric.Cmyk)
        {
            TiffRgba8Writer.WriteCmykPixel(info, red, green, blue, alpha, source, pixelIndex, sampleByteCounts, bytesPerPixel, diagnostics);
            return;
        }

        if (info.Photometric == TiffPhotometric.YCbCr)
        {
            TiffRgba8Writer.WriteYCbCrPixel(info, red, green, blue, alpha, source, pixelIndex, sampleByteCounts, bytesPerPixel, diagnostics);
            return;
        }

        diagnostics.AddError(TiffErrorCode.UnsupportedPhotometric, "RGBA16 writing does not support this photometric interpretation.");
    }

    internal static byte ScaleIntensityToByte(double intensity)
    {
        return (byte)TiffRgba8Writer.ScaleIntensityToUnsigned(intensity, 8);
    }
}
