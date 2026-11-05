using System;

namespace tiff;

internal class TiffPaletteColorMapExpander
{
    internal static ushort[] ExpandToRgb16(TiffImageInfo info, ReadOnlySpan<double> paletteIndices, TiffDiagnostics diagnostics)
    {
        return ExpandToRgb16(info, paletteIndices, new TiffLimits(), diagnostics);
    }

    internal static ushort[] ExpandToRgb16(TiffImageInfo info, ReadOnlySpan<double> paletteIndices, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        int entryCount = ValidatePaletteRequest(info, paletteIndices.Length, 2, limits, diagnostics);
        if (diagnostics.HasErrors())
        {
            return new ushort[0];
        }

        ushort[] destination = new ushort[paletteIndices.Length * 3];
        int index = 0;
        while (index < paletteIndices.Length)
        {
            int paletteIndex = ConvertPaletteIndex(paletteIndices[index], entryCount, diagnostics);
            if (diagnostics.HasErrors())
            {
                return new ushort[0];
            }

            int destinationOffset = index * 3;
            destination[destinationOffset] = info.ColorMap[paletteIndex];
            destination[destinationOffset + 1] = info.ColorMap[entryCount + paletteIndex];
            destination[destinationOffset + 2] = info.ColorMap[entryCount * 2 + paletteIndex];
            index = index + 1;
        }

        return destination;
    }

    internal static byte[] ExpandToRgb8(TiffImageInfo info, ReadOnlySpan<double> paletteIndices, TiffDiagnostics diagnostics)
    {
        return ExpandToRgb8(info, paletteIndices, new TiffLimits(), diagnostics);
    }

    internal static byte[] ExpandToRgb8(TiffImageInfo info, ReadOnlySpan<double> paletteIndices, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        ushort[] rgb16 = ExpandToRgb16(info, paletteIndices, limits, diagnostics);
        if (diagnostics.HasErrors())
        {
            return new byte[0];
        }

        byte[] destination = new byte[rgb16.Length];
        int index = 0;
        while (index < rgb16.Length)
        {
            destination[index] = ScaleColorMapValueToByte(rgb16[index]);
            index = index + 1;
        }

        return destination;
    }

    internal static int ValidatePaletteRequest(TiffImageInfo info, int paletteIndexCount, int outputBytesPerChannel, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        if (info == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "Image information is required to expand a palette ColorMap.");
            return 0;
        }

        if (info.Photometric != TiffPhotometric.Palette)
        {
            diagnostics.AddError(TiffErrorCode.UnsupportedPhotometric, "Palette ColorMap expansion requires palette photometric interpretation.");
            return 0;
        }

        if (info.SamplesPerPixel != 1)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "Palette ColorMap expansion requires one sample per pixel.", info.ImageIndex, TiffTagId.SamplesPerPixel);
            return 0;
        }

        if (info.BitsPerSample == null || info.BitsPerSample.Length == 0)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagCount, "BitsPerSample is required for palette ColorMap expansion.", info.ImageIndex, TiffTagId.BitsPerSample);
            return 0;
        }

        int bitsPerSample = info.BitsPerSample[0];
        if (bitsPerSample <= 0 || bitsPerSample > 30)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "Palette BitsPerSample must be positive and within the supported ColorMap range.", info.ImageIndex, TiffTagId.BitsPerSample);
            return 0;
        }

        int entryCount = 1 << bitsPerSample;
        if (info.ColorMap == null || info.ColorMap.Length != entryCount * 3)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagCount, "Palette ColorMap count must be three times two to the BitsPerSample value.", info.ImageIndex, TiffTagId.ColorMap);
            return 0;
        }

        int outputBytesPerPixel = 3 * outputBytesPerChannel;
        if (paletteIndexCount > int.MaxValue / outputBytesPerPixel)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Palette expansion output sample count overflows the supported range.");
            return 0;
        }

        int outputByteCount = paletteIndexCount * outputBytesPerPixel;
        TiffLimits actualLimits = TiffTagValueLoader.NormalizeLimits(limits);
        if (!actualLimits.AllowsAllocation((long)outputByteCount))
        {
            diagnostics.AddError(TiffErrorCode.AllocationLimitExceeded, "Palette expansion output exceeds the configured allocation limit.");
            return 0;
        }

        return entryCount;
    }

    internal static int ConvertPaletteIndex(double value, int entryCount, TiffDiagnostics diagnostics)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, "Palette index must be a finite value.");
            return 0;
        }

        if (value < 0 || value > (double)(entryCount - 1))
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, "Palette index is outside the ColorMap range.");
            return 0;
        }

        int index = (int)value;
        if ((double)index != value)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, "Palette index must be an integer value.");
            return 0;
        }

        return index;
    }

    internal static byte ScaleColorMapValueToByte(ushort value)
    {
        int scaled = ((int)value * 255 + 32767) / 65535;
        return (byte)scaled;
    }
}
