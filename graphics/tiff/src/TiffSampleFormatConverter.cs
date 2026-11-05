using System;

namespace tiff;

internal class TiffSampleFormatConverter
{
    internal static double[] ConvertToDoubleSamples(TiffCodecContext context, ReadOnlySpan<byte> source, TiffDiagnostics diagnostics)
    {
        if (context == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "A codec context is required to convert sample formats.");
            return new double[0];
        }

        if (context.Info == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "Image information is required to convert sample formats.");
            return new double[0];
        }

        int rowCount = TiffSubByteSampleUnpacker.CalculateRowCount(context, diagnostics);
        int pixelCount = TiffSubByteSampleUnpacker.CalculatePixelCount(context, diagnostics);
        int samplesPerPixel = TiffSubByteSampleUnpacker.CalculateSamplesPerPixel(context, diagnostics);
        int separateSampleIndex = TiffSubByteSampleUnpacker.CalculateSeparateSampleIndex(context, diagnostics);
        if (diagnostics.HasErrors())
        {
            return new double[0];
        }

        int pixelByteCount = CalculatePixelByteCount(context.Info, samplesPerPixel, separateSampleIndex, diagnostics);
        ulong sampleCount = CalculateSampleCount(rowCount, pixelCount, samplesPerPixel, context.Limits, diagnostics);
        if (diagnostics.HasErrors())
        {
            return new double[0];
        }

        ulong expectedSourceByteCount = (ulong)rowCount * (ulong)pixelCount * (ulong)pixelByteCount;
        if ((ulong)source.Length != expectedSourceByteCount)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "Sample format source byte count does not match the expected sample layout.");
            return new double[0];
        }

        if (sampleCount > int.MaxValue)
        {
            diagnostics.AddError(TiffErrorCode.AllocationLimitExceeded, "Converted sample count exceeds the supported array range.");
            return new double[0];
        }

        double[] destination = new double[(int)sampleCount];
        ConvertRows(context.Info, source, destination, rowCount, pixelCount, samplesPerPixel, separateSampleIndex, pixelByteCount, diagnostics);
        if (diagnostics.HasErrors())
        {
            return new double[0];
        }

        return destination;
    }

    internal static void ConvertRows(TiffImageInfo info, ReadOnlySpan<byte> source, double[] destination, int rowCount, int pixelCount, int samplesPerPixel, int separateSampleIndex, int pixelByteCount, TiffDiagnostics diagnostics)
    {
        int destinationIndex = 0;
        int rowIndex = 0;
        while (rowIndex < rowCount)
        {
            int rowOffset = rowIndex * pixelCount * pixelByteCount;
            int pixelIndex = 0;
            while (pixelIndex < pixelCount)
            {
                int sampleOffset = rowOffset + pixelIndex * pixelByteCount;
                int sampleIndex = 0;
                while (sampleIndex < samplesPerPixel)
                {
                    int infoSampleIndex = TiffSubByteSampleUnpacker.CalculateInfoSampleIndex(info, sampleIndex, separateSampleIndex);
                    int bitsPerSample = TiffSubByteSampleUnpacker.GetBitsPerSample(info, infoSampleIndex, diagnostics);
                    if (diagnostics.HasErrors())
                    {
                        return;
                    }

                    TiffSampleFormat sampleFormat = TiffPredictor.GetSampleFormat(info, infoSampleIndex);
                    int sampleByteCount = CalculateSampleByteCount(bitsPerSample, diagnostics);
                    if (diagnostics.HasErrors())
                    {
                        return;
                    }

                    destination[destinationIndex] = ConvertOneSample(source, sampleOffset, bitsPerSample, sampleByteCount, sampleFormat, info, diagnostics);
                    if (diagnostics.HasErrors())
                    {
                        return;
                    }

                    destinationIndex = destinationIndex + 1;
                    sampleOffset = sampleOffset + sampleByteCount;
                    sampleIndex = sampleIndex + 1;
                }

                pixelIndex = pixelIndex + 1;
            }

            rowIndex = rowIndex + 1;
        }
    }

    internal static double ConvertOneSample(ReadOnlySpan<byte> source, int offset, int bitsPerSample, int sampleByteCount, TiffSampleFormat sampleFormat, TiffImageInfo info, TiffDiagnostics diagnostics)
    {
        if (sampleFormat == TiffSampleFormat.UnsignedInteger || sampleFormat == TiffSampleFormat.Undefined)
        {
            ulong value = ReadUnsignedInteger(source, offset, bitsPerSample, sampleByteCount, diagnostics);
            if (diagnostics.HasErrors())
            {
                return 0;
            }

            return (double)value;
        }

        if (sampleFormat == TiffSampleFormat.SignedInteger)
        {
            long value = ReadSignedInteger(source, offset, bitsPerSample, sampleByteCount, diagnostics);
            if (diagnostics.HasErrors())
            {
                return 0;
            }

            return (double)value;
        }

        if (sampleFormat == TiffSampleFormat.IeeeFloatingPoint)
        {
            return ReadFloatingPoint(source, offset, bitsPerSample, diagnostics);
        }

        diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "SampleFormat is not supported for sample conversion.", info.ImageIndex, TiffTagId.SampleFormat);
        return 0;
    }

    internal static ulong ReadUnsignedInteger(ReadOnlySpan<byte> source, int offset, int bitsPerSample, int sampleByteCount, TiffDiagnostics diagnostics)
    {
        ulong value = 0;
        int byteIndex = 0;
        while (byteIndex < sampleByteCount)
        {
            value = value | ((ulong)source[offset + byteIndex] << (8 * byteIndex));
            byteIndex = byteIndex + 1;
        }

        if (bitsPerSample < 64)
        {
            ulong maximumValue = (1UL << bitsPerSample) - 1UL;
            if (value > maximumValue)
            {
                diagnostics.AddError(TiffErrorCode.InvalidTagValue, "Unsigned sample value exceeds the range allowed by BitsPerSample.");
                return 0;
            }
        }

        return value;
    }

    internal static long ReadSignedInteger(ReadOnlySpan<byte> source, int offset, int bitsPerSample, int sampleByteCount, TiffDiagnostics diagnostics)
    {
        ulong value = ReadUnsignedInteger(source, offset, bitsPerSample, sampleByteCount, diagnostics);
        if (diagnostics.HasErrors())
        {
            return 0;
        }

        if (bitsPerSample == 64)
        {
            if (value <= long.MaxValue)
            {
                return (long)value;
            }

            return long.MinValue + (long)(value - ((ulong)long.MaxValue + 1UL));
        }

        ulong signBit = 1UL << (bitsPerSample - 1);
        if ((value & signBit) == 0)
        {
            return (long)value;
        }

        return (long)(value - signBit) - (long)signBit;
    }

    internal static double ReadFloatingPoint(ReadOnlySpan<byte> source, int offset, int bitsPerSample, TiffDiagnostics diagnostics)
    {
        if (bitsPerSample == 16)
        {
            ushort bits = (ushort)ReadUnsignedInteger(source, offset, 16, 2, diagnostics);
            if (diagnostics.HasErrors())
            {
                return 0;
            }

            return (double)BitConverter.UInt16BitsToHalf(bits);
        }

        if (bitsPerSample == 32)
        {
            uint bits = (uint)ReadUnsignedInteger(source, offset, 32, 4, diagnostics);
            if (diagnostics.HasErrors())
            {
                return 0;
            }

            return (double)BitConverter.UInt32BitsToSingle(bits);
        }

        if (bitsPerSample == 64)
        {
            ulong bits = ReadUnsignedInteger(source, offset, 64, 8, diagnostics);
            if (diagnostics.HasErrors())
            {
                return 0;
            }

            return BitConverter.UInt64BitsToDouble(bits);
        }

        diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "IEEE floating-point sample conversion requires 16, 32, or 64 BitsPerSample.", TiffDiagnostic.NoImageIndex, TiffTagId.BitsPerSample);
        return 0;
    }

    internal static int CalculatePixelByteCount(TiffImageInfo info, int samplesPerPixel, int separateSampleIndex, TiffDiagnostics diagnostics)
    {
        int pixelByteCount = 0;
        int sampleIndex = 0;
        while (sampleIndex < samplesPerPixel)
        {
            int infoSampleIndex = TiffSubByteSampleUnpacker.CalculateInfoSampleIndex(info, sampleIndex, separateSampleIndex);
            int bitsPerSample = TiffSubByteSampleUnpacker.GetBitsPerSample(info, infoSampleIndex, diagnostics);
            if (diagnostics.HasErrors())
            {
                return 0;
            }

            int sampleByteCount = CalculateSampleByteCount(bitsPerSample, diagnostics);
            if (diagnostics.HasErrors())
            {
                return 0;
            }

            if (pixelByteCount > int.MaxValue - sampleByteCount)
            {
                diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Sample format pixel byte count overflows the supported range.");
                return 0;
            }

            pixelByteCount = pixelByteCount + sampleByteCount;
            sampleIndex = sampleIndex + 1;
        }

        return pixelByteCount;
    }

    internal static int CalculateSampleByteCount(int bitsPerSample, TiffDiagnostics diagnostics)
    {
        if (bitsPerSample <= 0)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "BitsPerSample values must be positive for sample conversion.", TiffDiagnostic.NoImageIndex, TiffTagId.BitsPerSample);
            return 0;
        }

        if (bitsPerSample < 8)
        {
            return 1;
        }

        if (bitsPerSample % 8 != 0)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "Byte-aligned BitsPerSample values are required for sample conversion.", TiffDiagnostic.NoImageIndex, TiffTagId.BitsPerSample);
            return 0;
        }

        int byteCount = bitsPerSample / 8;
        if (byteCount > 8)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "Sample conversion supports samples up to 64 bits.", TiffDiagnostic.NoImageIndex, TiffTagId.BitsPerSample);
            return 0;
        }

        return byteCount;
    }

    internal static ulong CalculateSampleCount(int rowCount, int pixelCount, int samplesPerPixel, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        if (samplesPerPixel != 0 && (ulong)pixelCount > ulong.MaxValue / (ulong)samplesPerPixel)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Converted sample row count overflows the supported range.");
            return 0;
        }

        ulong rowSampleCount = (ulong)pixelCount * (ulong)samplesPerPixel;
        if (rowSampleCount != 0 && (ulong)rowCount > ulong.MaxValue / rowSampleCount)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Converted sample count overflows the supported range.");
            return 0;
        }

        ulong sampleCount = rowSampleCount * (ulong)rowCount;
        if (sampleCount > long.MaxValue / 8L)
        {
            diagnostics.AddError(TiffErrorCode.AllocationLimitExceeded, "Converted sample allocation exceeds the supported range.");
            return 0;
        }

        TiffLimits actualLimits = TiffTagValueLoader.NormalizeLimits(limits);
        long byteCount = (long)sampleCount * 8L;
        if (!actualLimits.AllowsAllocation(byteCount))
        {
            diagnostics.AddError(TiffErrorCode.AllocationLimitExceeded, "Converted sample allocation exceeds the configured limit.");
            return 0;
        }

        return sampleCount;
    }
}
