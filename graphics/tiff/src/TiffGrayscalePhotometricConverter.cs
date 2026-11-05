using System;

namespace tiff;

internal class TiffGrayscalePhotometricConverter
{
    internal static ushort[] ConvertToRgb16(TiffImageInfo info, ReadOnlySpan<double> grayscaleSamples, TiffDiagnostics diagnostics)
    {
        return ConvertToRgb16(info, grayscaleSamples, new TiffLimits(), diagnostics);
    }

    internal static ushort[] ConvertToRgb16(TiffImageInfo info, ReadOnlySpan<double> grayscaleSamples, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        if (!ValidateGrayscaleRequest(info, grayscaleSamples.Length, 2, limits, diagnostics))
        {
            return new ushort[0];
        }

        ushort[] destination = new ushort[grayscaleSamples.Length * 3];
        int index = 0;
        while (index < grayscaleSamples.Length)
        {
            double intensity = ConvertOneSampleToIntensity(info, grayscaleSamples[index], diagnostics);
            if (diagnostics.HasErrors())
            {
                return new ushort[0];
            }

            ushort value = ScaleIntensityToUInt16(intensity);
            int destinationOffset = index * 3;
            destination[destinationOffset] = value;
            destination[destinationOffset + 1] = value;
            destination[destinationOffset + 2] = value;
            index = index + 1;
        }

        return destination;
    }

    internal static byte[] ConvertToRgb8(TiffImageInfo info, ReadOnlySpan<double> grayscaleSamples, TiffDiagnostics diagnostics)
    {
        return ConvertToRgb8(info, grayscaleSamples, new TiffLimits(), diagnostics);
    }

    internal static byte[] ConvertToRgb8(TiffImageInfo info, ReadOnlySpan<double> grayscaleSamples, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        if (!ValidateGrayscaleRequest(info, grayscaleSamples.Length, 1, limits, diagnostics))
        {
            return new byte[0];
        }

        byte[] destination = new byte[grayscaleSamples.Length * 3];
        int index = 0;
        while (index < grayscaleSamples.Length)
        {
            double intensity = ConvertOneSampleToIntensity(info, grayscaleSamples[index], diagnostics);
            if (diagnostics.HasErrors())
            {
                return new byte[0];
            }

            byte value = ScaleIntensityToByte(intensity);
            int destinationOffset = index * 3;
            destination[destinationOffset] = value;
            destination[destinationOffset + 1] = value;
            destination[destinationOffset + 2] = value;
            index = index + 1;
        }

        return destination;
    }

    internal static bool ValidateGrayscaleRequest(TiffImageInfo info, int sampleCount, int outputBytesPerChannel, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        if (info == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "Image information is required for grayscale conversion.");
            return false;
        }

        if (info.Photometric != TiffPhotometric.BlackIsZero && info.Photometric != TiffPhotometric.WhiteIsZero)
        {
            diagnostics.AddError(TiffErrorCode.UnsupportedPhotometric, "Grayscale conversion requires WhiteIsZero or BlackIsZero photometric interpretation.");
            return false;
        }

        if (info.SamplesPerPixel != 1)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "Grayscale conversion requires one sample per pixel.", info.ImageIndex, TiffTagId.SamplesPerPixel);
            return false;
        }

        if (info.BitsPerSample == null || info.BitsPerSample.Length == 0)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagCount, "BitsPerSample is required for grayscale conversion.", info.ImageIndex, TiffTagId.BitsPerSample);
            return false;
        }

        int bitsPerSample = info.BitsPerSample[0];
        if (bitsPerSample <= 0 || bitsPerSample > 64)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "Grayscale conversion supports positive samples up to 64 bits.", info.ImageIndex, TiffTagId.BitsPerSample);
            return false;
        }

        int outputBytesPerPixel = 3 * outputBytesPerChannel;
        if (sampleCount > int.MaxValue / outputBytesPerPixel)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Grayscale conversion output byte count overflows the supported range.");
            return false;
        }

        int outputByteCount = sampleCount * outputBytesPerPixel;
        TiffLimits actualLimits = TiffTagValueLoader.NormalizeLimits(limits);
        if (!actualLimits.AllowsAllocation((long)outputByteCount))
        {
            diagnostics.AddError(TiffErrorCode.AllocationLimitExceeded, "Grayscale conversion output exceeds the configured allocation limit.");
            return false;
        }

        return true;
    }

    internal static double ConvertOneSampleToIntensity(TiffImageInfo info, double sampleValue, TiffDiagnostics diagnostics)
    {
        if (double.IsNaN(sampleValue) || double.IsInfinity(sampleValue))
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, "Grayscale sample value must be finite.");
            return 0;
        }

        int bitsPerSample = info.BitsPerSample[0];
        TiffSampleFormat sampleFormat = TiffPredictor.GetSampleFormat(info, 0);
        double intensity = 0;
        if (sampleFormat == TiffSampleFormat.IeeeFloatingPoint)
        {
            intensity = ConvertFloatingPointSample(sampleValue, diagnostics);
        }
        else
        {
            if (sampleFormat == TiffSampleFormat.SignedInteger)
            {
                intensity = ConvertSignedIntegerSample(sampleValue, bitsPerSample, diagnostics);
            }
            else
            {
                if (sampleFormat == TiffSampleFormat.UnsignedInteger || sampleFormat == TiffSampleFormat.Undefined)
                {
                    intensity = ConvertUnsignedIntegerSample(sampleValue, bitsPerSample, diagnostics);
                }
                else
                {
                    diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "SampleFormat is not supported for grayscale conversion.", info.ImageIndex, TiffTagId.SampleFormat);
                    return 0;
                }
            }
        }

        if (diagnostics.HasErrors())
        {
            return 0;
        }

        if (info.Photometric == TiffPhotometric.WhiteIsZero)
        {
            intensity = 1d - intensity;
        }

        return intensity;
    }

    internal static double ConvertFloatingPointSample(double sampleValue, TiffDiagnostics diagnostics)
    {
        if (sampleValue < 0d || sampleValue > 1d)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, "Floating-point grayscale samples must be in the 0 to 1 range.");
            return 0;
        }

        return sampleValue;
    }

    internal static double ConvertUnsignedIntegerSample(double sampleValue, int bitsPerSample, TiffDiagnostics diagnostics)
    {
        if (Math.Truncate(sampleValue) != sampleValue)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, "Integer grayscale samples must be whole values.");
            return 0;
        }

        double maximumValue = CalculateUnsignedMaximum(bitsPerSample);
        if (sampleValue < 0d || sampleValue > maximumValue)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, "Unsigned grayscale sample is outside the BitsPerSample range.");
            return 0;
        }

        if (maximumValue == 0d)
        {
            return 0d;
        }

        return sampleValue / maximumValue;
    }

    internal static double ConvertSignedIntegerSample(double sampleValue, int bitsPerSample, TiffDiagnostics diagnostics)
    {
        if (Math.Truncate(sampleValue) != sampleValue)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, "Integer grayscale samples must be whole values.");
            return 0;
        }

        double minimumValue = 0d - CalculatePowerOfTwo(bitsPerSample - 1);
        double maximumValue = CalculatePowerOfTwo(bitsPerSample - 1) - 1d;
        if (sampleValue < minimumValue || sampleValue > maximumValue)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, "Signed grayscale sample is outside the BitsPerSample range.");
            return 0;
        }

        return (sampleValue - minimumValue) / (maximumValue - minimumValue);
    }

    internal static double CalculateUnsignedMaximum(int bitsPerSample)
    {
        return CalculatePowerOfTwo(bitsPerSample) - 1d;
    }

    internal static double CalculatePowerOfTwo(int exponent)
    {
        double value = 1d;
        int index = 0;
        while (index < exponent)
        {
            value = value * 2d;
            index = index + 1;
        }

        return value;
    }

    internal static ushort ScaleIntensityToUInt16(double intensity)
    {
        int value = (int)(intensity * 65535d + 0.5d);
        if (value < 0)
        {
            value = 0;
        }

        if (value > 65535)
        {
            value = 65535;
        }

        return (ushort)value;
    }

    internal static byte ScaleIntensityToByte(double intensity)
    {
        int value = (int)(intensity * 255d + 0.5d);
        if (value < 0)
        {
            value = 0;
        }

        if (value > 255)
        {
            value = 255;
        }

        return (byte)value;
    }
}
