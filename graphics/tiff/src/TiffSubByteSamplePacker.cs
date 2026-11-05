using System;

namespace tiff;

internal class TiffSubByteSamplePacker
{
    internal static byte[] Pack(TiffCodecContext context, ReadOnlySpan<byte> source, TiffDiagnostics diagnostics)
    {
        if (context == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "A codec context is required to pack sub-byte samples.");
            return new byte[0];
        }

        if (context.Info == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "Image information is required to pack sub-byte samples.");
            return new byte[0];
        }

        if (!TiffSubByteSampleUnpacker.HasSubByteSamples(context, diagnostics))
        {
            if (diagnostics.HasErrors())
            {
                return new byte[0];
            }

            return TiffIfdEntry.CopyBytes(source);
        }

        int rowCount = TiffSubByteSampleUnpacker.CalculateRowCount(context, diagnostics);
        int pixelCount = TiffSubByteSampleUnpacker.CalculatePixelCount(context, diagnostics);
        int samplesPerPixel = TiffSubByteSampleUnpacker.CalculateSamplesPerPixel(context, diagnostics);
        int separateSampleIndex = TiffSubByteSampleUnpacker.CalculateSeparateSampleIndex(context, diagnostics);
        if (diagnostics.HasErrors())
        {
            return new byte[0];
        }

        ulong bitsPerRow = TiffSubByteSampleUnpacker.CalculateBitsPerRow(context.Info, pixelCount, samplesPerPixel, separateSampleIndex, diagnostics);
        ulong rowByteCountValue = TiffStrileLayout.DivideRoundUp(bitsPerRow, 8);
        ulong destinationByteCount = rowByteCountValue * (ulong)rowCount;
        ulong expectedSourceByteCount = CalculateSourceByteCount(rowCount, pixelCount, samplesPerPixel, diagnostics);
        if (diagnostics.HasErrors())
        {
            return new byte[0];
        }

        if ((ulong)source.Length != expectedSourceByteCount)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "Sub-byte source byte count does not match the unpacked sample layout.");
            return new byte[0];
        }

        if (rowByteCountValue > int.MaxValue || destinationByteCount > int.MaxValue)
        {
            diagnostics.AddError(TiffErrorCode.AllocationLimitExceeded, "Sub-byte sample packing exceeds the supported array range.");
            return new byte[0];
        }

        TiffLimits actualLimits = TiffTagValueLoader.NormalizeLimits(context.Limits);
        if (destinationByteCount > long.MaxValue || !actualLimits.AllowsDecodedBytes((long)destinationByteCount) || !actualLimits.AllowsAllocation((long)destinationByteCount))
        {
            diagnostics.AddError(TiffErrorCode.AllocationLimitExceeded, "Sub-byte packed byte count exceeds the configured limits.");
            return new byte[0];
        }

        byte[] destination = new byte[(int)destinationByteCount];
        PackRows(context.Info, source, destination, rowCount, pixelCount, samplesPerPixel, separateSampleIndex, (int)rowByteCountValue, diagnostics);
        if (diagnostics.HasErrors())
        {
            return new byte[0];
        }

        return destination;
    }

    internal static void PackRows(TiffImageInfo info, ReadOnlySpan<byte> source, byte[] destination, int rowCount, int pixelCount, int samplesPerPixel, int separateSampleIndex, int rowByteCount, TiffDiagnostics diagnostics)
    {
        int sourceIndex = 0;
        int rowIndex = 0;
        while (rowIndex < rowCount)
        {
            int destinationRowOffset = rowIndex * rowByteCount;
            int rowBitOffset = 0;
            int pixelIndex = 0;
            while (pixelIndex < pixelCount)
            {
                int sampleIndex = 0;
                while (sampleIndex < samplesPerPixel)
                {
                    int infoSampleIndex = TiffSubByteSampleUnpacker.CalculateInfoSampleIndex(info, sampleIndex, separateSampleIndex);
                    int bitsPerSample = TiffSubByteSampleUnpacker.GetBitsPerSample(info, infoSampleIndex, diagnostics);
                    if (diagnostics.HasErrors())
                    {
                        return;
                    }

                    if (bitsPerSample > 8)
                    {
                        diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "Sub-byte packing requires all packed samples to be 8 bits or fewer.", info.ImageIndex, TiffTagId.BitsPerSample);
                        return;
                    }

                    byte sampleValue = source[sourceIndex];
                    if (!ValidateSampleValue(sampleValue, bitsPerSample, info, diagnostics))
                    {
                        return;
                    }

                    WritePackedSample(destination, destinationRowOffset, rowBitOffset, bitsPerSample, sampleValue);
                    sourceIndex = sourceIndex + 1;
                    rowBitOffset = rowBitOffset + bitsPerSample;
                    sampleIndex = sampleIndex + 1;
                }

                pixelIndex = pixelIndex + 1;
            }

            rowIndex = rowIndex + 1;
        }
    }

    internal static bool ValidateSampleValue(byte sampleValue, int bitsPerSample, TiffImageInfo info, TiffDiagnostics diagnostics)
    {
        int maximumValue = 0;
        if (bitsPerSample == 8)
        {
            maximumValue = 255;
        }
        else
        {
            maximumValue = (1 << bitsPerSample) - 1;
        }

        if ((int)sampleValue > maximumValue)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "Sub-byte sample value exceeds the range allowed by BitsPerSample.", info.ImageIndex, TiffTagId.BitsPerSample);
            return false;
        }

        return true;
    }

    internal static void WritePackedSample(byte[] destination, int rowByteOffset, int bitOffset, int bitsPerSample, byte sampleValue)
    {
        int bitIndex = 0;
        while (bitIndex < bitsPerSample)
        {
            int shift = bitsPerSample - bitIndex - 1;
            int bit = ((int)sampleValue >> shift) & 1;
            int destinationBitOffset = bitOffset + bitIndex;
            int destinationByteOffset = rowByteOffset + destinationBitOffset / 8;
            int bitInByte = 7 - destinationBitOffset % 8;
            if (bit == 1)
            {
                destination[destinationByteOffset] = (byte)(destination[destinationByteOffset] | (1 << bitInByte));
            }

            bitIndex = bitIndex + 1;
        }
    }

    internal static ulong CalculateSourceByteCount(int rowCount, int pixelCount, int samplesPerPixel, TiffDiagnostics diagnostics)
    {
        if (samplesPerPixel != 0 && (ulong)pixelCount > ulong.MaxValue / (ulong)samplesPerPixel)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Sub-byte unpacked row byte count overflows the supported range.");
            return 0;
        }

        ulong rowByteCount = (ulong)pixelCount * (ulong)samplesPerPixel;
        if (rowByteCount != 0 && (ulong)rowCount > ulong.MaxValue / rowByteCount)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Sub-byte unpacked source byte count overflows the supported range.");
            return 0;
        }

        return rowByteCount * (ulong)rowCount;
    }
}
