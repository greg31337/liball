using System;

namespace tiff;

internal class TiffRgbPhotometricConverter
{
    internal static byte[] ConvertToRgb8(TiffImageInfo info, ReadOnlySpan<double> samples, TiffDiagnostics diagnostics)
    {
        return ConvertToRgb8(info, samples, new TiffLimits(), diagnostics);
    }

    internal static byte[] ConvertToRgb8(TiffImageInfo info, ReadOnlySpan<double> samples, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        int pixelCount = ValidateRgbRequest(info, samples.Length, 3, 1, limits, diagnostics);
        if (diagnostics.HasErrors())
        {
            return new byte[0];
        }

        byte[] destination = new byte[pixelCount * 3];
        ConvertToByteChannels(info, samples, destination, pixelCount, 3, diagnostics);
        if (diagnostics.HasErrors())
        {
            return new byte[0];
        }

        return destination;
    }

    internal static ushort[] ConvertToRgb16(TiffImageInfo info, ReadOnlySpan<double> samples, TiffDiagnostics diagnostics)
    {
        return ConvertToRgb16(info, samples, new TiffLimits(), diagnostics);
    }

    internal static ushort[] ConvertToRgb16(TiffImageInfo info, ReadOnlySpan<double> samples, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        int pixelCount = ValidateRgbRequest(info, samples.Length, 3, 2, limits, diagnostics);
        if (diagnostics.HasErrors())
        {
            return new ushort[0];
        }

        ushort[] destination = new ushort[pixelCount * 3];
        ConvertToUShortChannels(info, samples, destination, pixelCount, 3, diagnostics);
        if (diagnostics.HasErrors())
        {
            return new ushort[0];
        }

        return destination;
    }

    internal static byte[] ConvertToRgba8(TiffImageInfo info, ReadOnlySpan<double> samples, TiffDiagnostics diagnostics)
    {
        return ConvertToRgba8(info, samples, new TiffLimits(), diagnostics);
    }

    internal static byte[] ConvertToRgba8(TiffImageInfo info, ReadOnlySpan<double> samples, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        int pixelCount = ValidateRgbRequest(info, samples.Length, 4, 1, limits, diagnostics);
        if (diagnostics.HasErrors())
        {
            return new byte[0];
        }

        byte[] destination = new byte[pixelCount * 4];
        ConvertToByteChannels(info, samples, destination, pixelCount, 4, diagnostics);
        if (diagnostics.HasErrors())
        {
            return new byte[0];
        }

        return destination;
    }

    internal static ushort[] ConvertToRgba16(TiffImageInfo info, ReadOnlySpan<double> samples, TiffDiagnostics diagnostics)
    {
        return ConvertToRgba16(info, samples, new TiffLimits(), diagnostics);
    }

    internal static ushort[] ConvertToRgba16(TiffImageInfo info, ReadOnlySpan<double> samples, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        int pixelCount = ValidateRgbRequest(info, samples.Length, 4, 2, limits, diagnostics);
        if (diagnostics.HasErrors())
        {
            return new ushort[0];
        }

        ushort[] destination = new ushort[pixelCount * 4];
        ConvertToUShortChannels(info, samples, destination, pixelCount, 4, diagnostics);
        if (diagnostics.HasErrors())
        {
            return new ushort[0];
        }

        return destination;
    }

    internal static void ConvertToByteChannels(TiffImageInfo info, ReadOnlySpan<double> samples, byte[] destination, int pixelCount, int outputChannelCount, TiffDiagnostics diagnostics)
    {
        int pixelIndex = 0;
        while (pixelIndex < pixelCount)
        {
            int channelIndex = 0;
            while (channelIndex < outputChannelCount)
            {
                double intensity = ConvertChannelToIntensity(info, channelIndex, samples[pixelIndex * info.SamplesPerPixel + channelIndex], diagnostics);
                if (diagnostics.HasErrors())
                {
                    return;
                }

                destination[pixelIndex * outputChannelCount + channelIndex] = TiffGrayscalePhotometricConverter.ScaleIntensityToByte(intensity);
                channelIndex = channelIndex + 1;
            }

            pixelIndex = pixelIndex + 1;
        }
    }

    internal static void ConvertToUShortChannels(TiffImageInfo info, ReadOnlySpan<double> samples, ushort[] destination, int pixelCount, int outputChannelCount, TiffDiagnostics diagnostics)
    {
        int pixelIndex = 0;
        while (pixelIndex < pixelCount)
        {
            int channelIndex = 0;
            while (channelIndex < outputChannelCount)
            {
                double intensity = ConvertChannelToIntensity(info, channelIndex, samples[pixelIndex * info.SamplesPerPixel + channelIndex], diagnostics);
                if (diagnostics.HasErrors())
                {
                    return;
                }

                destination[pixelIndex * outputChannelCount + channelIndex] = TiffGrayscalePhotometricConverter.ScaleIntensityToUInt16(intensity);
                channelIndex = channelIndex + 1;
            }

            pixelIndex = pixelIndex + 1;
        }
    }

    internal static double ConvertChannelToIntensity(TiffImageInfo info, int channelIndex, double sampleValue, TiffDiagnostics diagnostics)
    {
        int bitsPerSample = TiffSubByteSampleUnpacker.GetBitsPerSample(info, channelIndex, diagnostics);
        if (diagnostics.HasErrors())
        {
            return 0;
        }

        TiffSampleFormat sampleFormat = TiffPredictor.GetSampleFormat(info, channelIndex);
        if (sampleFormat == TiffSampleFormat.IeeeFloatingPoint)
        {
            return TiffGrayscalePhotometricConverter.ConvertFloatingPointSample(sampleValue, diagnostics);
        }

        if (sampleFormat == TiffSampleFormat.SignedInteger)
        {
            return TiffGrayscalePhotometricConverter.ConvertSignedIntegerSample(sampleValue, bitsPerSample, diagnostics);
        }

        if (sampleFormat == TiffSampleFormat.UnsignedInteger || sampleFormat == TiffSampleFormat.Undefined)
        {
            return TiffGrayscalePhotometricConverter.ConvertUnsignedIntegerSample(sampleValue, bitsPerSample, diagnostics);
        }

        diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "SampleFormat is not supported for RGB conversion.", info.ImageIndex, TiffTagId.SampleFormat);
        return 0;
    }

    internal static int ValidateRgbRequest(TiffImageInfo info, int sampleCount, int outputChannelCount, int outputBytesPerChannel, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        if (info == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "Image information is required for RGB conversion.");
            return 0;
        }

        if (info.Photometric != TiffPhotometric.Rgb)
        {
            diagnostics.AddError(TiffErrorCode.UnsupportedPhotometric, "RGB conversion requires RGB photometric interpretation.");
            return 0;
        }

        if (info.SamplesPerPixel < outputChannelCount)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "RGB conversion requires enough samples per pixel for the requested output.", info.ImageIndex, TiffTagId.SamplesPerPixel);
            return 0;
        }

        if (info.SamplesPerPixel <= 0 || sampleCount % info.SamplesPerPixel != 0)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "RGB sample count does not match SamplesPerPixel.");
            return 0;
        }

        int pixelCount = sampleCount / info.SamplesPerPixel;
        int outputBytesPerPixel = outputChannelCount * outputBytesPerChannel;
        if (pixelCount > int.MaxValue / outputBytesPerPixel)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "RGB conversion output byte count overflows the supported range.");
            return 0;
        }

        int outputByteCount = pixelCount * outputBytesPerPixel;
        TiffLimits actualLimits = TiffTagValueLoader.NormalizeLimits(limits);
        if (!actualLimits.AllowsAllocation((long)outputByteCount))
        {
            diagnostics.AddError(TiffErrorCode.AllocationLimitExceeded, "RGB conversion output exceeds the configured allocation limit.");
            return 0;
        }

        return pixelCount;
    }
}
