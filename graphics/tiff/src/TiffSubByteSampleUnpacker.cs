using System;

namespace tiff;

internal class TiffSubByteSampleUnpacker
{
    internal static byte[] Unpack(TiffCodecContext context, ReadOnlySpan<byte> source, TiffDiagnostics diagnostics)
    {
        if (context == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "A codec context is required to unpack sub-byte samples.");
            return new byte[0];
        }

        if (context.Info == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "Image information is required to unpack sub-byte samples.");
            return new byte[0];
        }

        if (!HasSubByteSamples(context, diagnostics))
        {
            if (diagnostics.HasErrors())
            {
                return new byte[0];
            }

            return TiffIfdEntry.CopyBytes(source);
        }

        int rowCount = CalculateRowCount(context, diagnostics);
        int pixelCount = CalculatePixelCount(context, diagnostics);
        int samplesPerPixel = CalculateSamplesPerPixel(context, diagnostics);
        int separateSampleIndex = CalculateSeparateSampleIndex(context, diagnostics);
        if (diagnostics.HasErrors())
        {
            return new byte[0];
        }

        ulong bitsPerRow = CalculateBitsPerRow(context.Info, pixelCount, samplesPerPixel, separateSampleIndex, diagnostics);
        ulong rowByteCountValue = TiffStrileLayout.DivideRoundUp(bitsPerRow, 8);
        ulong expectedSourceByteCount = rowByteCountValue * (ulong)rowCount;
        ulong destinationByteCount = CalculateDestinationByteCount(rowCount, pixelCount, samplesPerPixel, context.Limits, diagnostics);
        if (diagnostics.HasErrors())
        {
            return new byte[0];
        }

        if ((ulong)source.Length != expectedSourceByteCount)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "Sub-byte source byte count does not match the expected row layout.");
            return new byte[0];
        }

        if (rowByteCountValue > int.MaxValue || destinationByteCount > int.MaxValue)
        {
            diagnostics.AddError(TiffErrorCode.AllocationLimitExceeded, "Sub-byte sample unpacking exceeds the supported array range.");
            return new byte[0];
        }

        byte[] destination = new byte[(int)destinationByteCount];
        UnpackRows(context.Info, source, destination, rowCount, pixelCount, samplesPerPixel, separateSampleIndex, (int)rowByteCountValue, diagnostics);
        if (diagnostics.HasErrors())
        {
            return new byte[0];
        }

        return destination;
    }

    internal static bool HasSubByteSamples(TiffCodecContext context, TiffDiagnostics diagnostics)
    {
        int samplesPerPixel = CalculateSamplesPerPixel(context, diagnostics);
        int separateSampleIndex = CalculateSeparateSampleIndex(context, diagnostics);
        if (diagnostics.HasErrors())
        {
            return false;
        }

        int sampleIndex = 0;
        while (sampleIndex < samplesPerPixel)
        {
            int infoSampleIndex = CalculateInfoSampleIndex(context.Info, sampleIndex, separateSampleIndex);
            int bitsPerSample = GetBitsPerSample(context.Info, infoSampleIndex, diagnostics);
            if (diagnostics.HasErrors())
            {
                return false;
            }

            if (bitsPerSample < 8)
            {
                return true;
            }

            sampleIndex = sampleIndex + 1;
        }

        return false;
    }

    internal static void UnpackRows(TiffImageInfo info, ReadOnlySpan<byte> source, byte[] destination, int rowCount, int pixelCount, int samplesPerPixel, int separateSampleIndex, int rowByteCount, TiffDiagnostics diagnostics)
    {
        int destinationIndex = 0;
        int rowIndex = 0;
        while (rowIndex < rowCount)
        {
            int sourceRowOffset = rowIndex * rowByteCount;
            int rowBitOffset = 0;
            int pixelIndex = 0;
            while (pixelIndex < pixelCount)
            {
                int sampleIndex = 0;
                while (sampleIndex < samplesPerPixel)
                {
                    int infoSampleIndex = CalculateInfoSampleIndex(info, sampleIndex, separateSampleIndex);
                    int bitsPerSample = GetBitsPerSample(info, infoSampleIndex, diagnostics);
                    if (diagnostics.HasErrors())
                    {
                        return;
                    }

                    if (bitsPerSample > 8)
                    {
                        diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "Sub-byte unpacking requires all unpacked samples to be 8 bits or fewer.", info.ImageIndex, TiffTagId.BitsPerSample);
                        return;
                    }

                    destination[destinationIndex] = ReadPackedSample(source, sourceRowOffset, rowBitOffset, bitsPerSample);
                    destinationIndex = destinationIndex + 1;
                    rowBitOffset = rowBitOffset + bitsPerSample;
                    sampleIndex = sampleIndex + 1;
                }

                pixelIndex = pixelIndex + 1;
            }

            rowIndex = rowIndex + 1;
        }
    }

    internal static byte ReadPackedSample(ReadOnlySpan<byte> source, int rowByteOffset, int bitOffset, int bitsPerSample)
    {
        int value = 0;
        int bitIndex = 0;
        while (bitIndex < bitsPerSample)
        {
            int sourceBitOffset = bitOffset + bitIndex;
            int sourceByteOffset = rowByteOffset + sourceBitOffset / 8;
            int bitInByte = 7 - sourceBitOffset % 8;
            int bit = (source[sourceByteOffset] >> bitInByte) & 1;
            value = value * 2 + bit;
            bitIndex = bitIndex + 1;
        }

        return (byte)value;
    }

    internal static ulong CalculateBitsPerRow(TiffImageInfo info, int pixelCount, int samplesPerPixel, int separateSampleIndex, TiffDiagnostics diagnostics)
    {
        ulong bitsPerPixel = 0;
        int sampleIndex = 0;
        while (sampleIndex < samplesPerPixel)
        {
            int infoSampleIndex = CalculateInfoSampleIndex(info, sampleIndex, separateSampleIndex);
            int bitsPerSample = GetBitsPerSample(info, infoSampleIndex, diagnostics);
            if (diagnostics.HasErrors())
            {
                return 0;
            }

            if (bitsPerSample <= 0)
            {
                diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "BitsPerSample values must be positive for sub-byte unpacking.", info.ImageIndex, TiffTagId.BitsPerSample);
                return 0;
            }

            if (bitsPerSample > 8)
            {
                diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "Sub-byte unpacking requires all unpacked samples to be 8 bits or fewer.", info.ImageIndex, TiffTagId.BitsPerSample);
                return 0;
            }

            bitsPerPixel = bitsPerPixel + (ulong)bitsPerSample;
            sampleIndex = sampleIndex + 1;
        }

        if (pixelCount < 0)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, "Sub-byte row width must be positive.");
            return 0;
        }

        if (bitsPerPixel != 0 && (ulong)pixelCount > ulong.MaxValue / bitsPerPixel)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Sub-byte row bit count overflows the supported range.");
            return 0;
        }

        return bitsPerPixel * (ulong)pixelCount;
    }

    internal static ulong CalculateDestinationByteCount(int rowCount, int pixelCount, int samplesPerPixel, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        ulong rowSampleCount = 0;
        if (samplesPerPixel != 0 && (ulong)pixelCount > ulong.MaxValue / (ulong)samplesPerPixel)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Sub-byte unpacked row byte count overflows the supported range.");
            return 0;
        }

        rowSampleCount = (ulong)pixelCount * (ulong)samplesPerPixel;
        if (rowSampleCount != 0 && (ulong)rowCount > ulong.MaxValue / rowSampleCount)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Sub-byte unpacked byte count overflows the supported range.");
            return 0;
        }

        ulong byteCount = rowSampleCount * (ulong)rowCount;
        TiffLimits actualLimits = TiffTagValueLoader.NormalizeLimits(limits);
        if (byteCount > long.MaxValue || !actualLimits.AllowsDecodedBytes((long)byteCount) || !actualLimits.AllowsAllocation((long)byteCount))
        {
            diagnostics.AddError(TiffErrorCode.AllocationLimitExceeded, "Sub-byte unpacked byte count exceeds the configured limits.");
            return 0;
        }

        return byteCount;
    }

    internal static int CalculateRowCount(TiffCodecContext context, TiffDiagnostics diagnostics)
    {
        ulong rowCount = 0;
        if (context.IsTile)
        {
            rowCount = context.Info.TileLength;
        }
        else
        {
            rowCount = TiffStrileLayout.CalculateRowsInStrip(context.Info, context.SegmentIndex, diagnostics);
        }

        if (rowCount == 0)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, "Sub-byte row count must be positive.");
            return 0;
        }

        if (rowCount > int.MaxValue)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Sub-byte row count exceeds the supported range.");
            return 0;
        }

        return (int)rowCount;
    }

    internal static int CalculatePixelCount(TiffCodecContext context, TiffDiagnostics diagnostics)
    {
        ulong pixelCount = context.Info.Width;
        if (context.IsTile)
        {
            pixelCount = context.Info.TileWidth;
        }

        if (pixelCount == 0)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, "Sub-byte row width must be positive.");
            return 0;
        }

        if (pixelCount > int.MaxValue)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Sub-byte row width exceeds the supported range.");
            return 0;
        }

        return (int)pixelCount;
    }

    internal static int CalculateSamplesPerPixel(TiffCodecContext context, TiffDiagnostics diagnostics)
    {
        if (context.Info.PlanarConfiguration == TiffPlanarConfiguration.Separate)
        {
            return 1;
        }

        if (context.Info.SamplesPerPixel <= 0)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "SamplesPerPixel must be positive for sub-byte unpacking.", context.Info.ImageIndex, TiffTagId.SamplesPerPixel);
            return 0;
        }

        return context.Info.SamplesPerPixel;
    }

    internal static int CalculateSeparateSampleIndex(TiffCodecContext context, TiffDiagnostics diagnostics)
    {
        if (context.Info.PlanarConfiguration != TiffPlanarConfiguration.Separate)
        {
            return 0;
        }

        if (context.Info.BitsPerSample != null && context.Info.BitsPerSample.Length == 1)
        {
            return 0;
        }

        ulong segmentsPerPlane = 0;
        if (context.IsTile)
        {
            segmentsPerPlane = TiffStrileLayout.CalculateTilesPerPlane(context.Info, diagnostics);
        }
        else
        {
            segmentsPerPlane = TiffStrileLayout.CalculateStripsPerPlane(context.Info, diagnostics);
        }

        if (segmentsPerPlane == 0)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, "Sub-byte segment count per plane must be positive.");
            return 0;
        }

        ulong sampleIndex = context.SegmentIndex / segmentsPerPlane;
        if (sampleIndex > int.MaxValue)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Sub-byte sample index exceeds the supported range.");
            return 0;
        }

        return (int)sampleIndex;
    }

    internal static int CalculateInfoSampleIndex(TiffImageInfo info, int sampleIndex, int separateSampleIndex)
    {
        if (info.PlanarConfiguration == TiffPlanarConfiguration.Separate)
        {
            return separateSampleIndex;
        }

        return sampleIndex;
    }

    internal static int GetBitsPerSample(TiffImageInfo info, int sampleIndex, TiffDiagnostics diagnostics)
    {
        if (info.BitsPerSample == null || info.BitsPerSample.Length == 0)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagCount, "BitsPerSample is required for sub-byte unpacking.", info.ImageIndex, TiffTagId.BitsPerSample);
            return 0;
        }

        if (info.BitsPerSample.Length == 1)
        {
            return info.BitsPerSample[0];
        }

        if (sampleIndex < 0 || sampleIndex >= info.BitsPerSample.Length)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagCount, "BitsPerSample does not contain the sample required for sub-byte unpacking.", info.ImageIndex, TiffTagId.BitsPerSample);
            return 0;
        }

        return info.BitsPerSample[sampleIndex];
    }
}
