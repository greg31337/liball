using System;

namespace tiff;

internal class TiffLabPhotometricConverter
{
    internal const double D50X = 0.96422d;
    internal const double D50Y = 1d;
    internal const double D50Z = 0.82521d;

    internal static byte[] ConvertToRgb8(TiffImageInfo info, ReadOnlySpan<double> samples, TiffDiagnostics diagnostics)
    {
        return ConvertToRgb8(info, samples, new TiffLimits(), diagnostics);
    }

    internal static byte[] ConvertToRgb8(TiffImageInfo info, ReadOnlySpan<double> samples, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        int pixelCount = ValidateLabRequest(info, samples.Length, 1, limits, diagnostics);
        if (diagnostics.HasErrors())
        {
            return new byte[0];
        }

        byte[] destination = new byte[pixelCount * 3];
        int pixelIndex = 0;
        while (pixelIndex < pixelCount)
        {
            double[] rgb = ConvertOnePixel(info, samples, pixelIndex, diagnostics);
            if (diagnostics.HasErrors())
            {
                return new byte[0];
            }

            int destinationOffset = pixelIndex * 3;
            destination[destinationOffset] = TiffGrayscalePhotometricConverter.ScaleIntensityToByte(rgb[0]);
            destination[destinationOffset + 1] = TiffGrayscalePhotometricConverter.ScaleIntensityToByte(rgb[1]);
            destination[destinationOffset + 2] = TiffGrayscalePhotometricConverter.ScaleIntensityToByte(rgb[2]);
            pixelIndex = pixelIndex + 1;
        }

        return destination;
    }

    internal static ushort[] ConvertToRgb16(TiffImageInfo info, ReadOnlySpan<double> samples, TiffDiagnostics diagnostics)
    {
        return ConvertToRgb16(info, samples, new TiffLimits(), diagnostics);
    }

    internal static ushort[] ConvertToRgb16(TiffImageInfo info, ReadOnlySpan<double> samples, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        int pixelCount = ValidateLabRequest(info, samples.Length, 2, limits, diagnostics);
        if (diagnostics.HasErrors())
        {
            return new ushort[0];
        }

        ushort[] destination = new ushort[pixelCount * 3];
        int pixelIndex = 0;
        while (pixelIndex < pixelCount)
        {
            double[] rgb = ConvertOnePixel(info, samples, pixelIndex, diagnostics);
            if (diagnostics.HasErrors())
            {
                return new ushort[0];
            }

            int destinationOffset = pixelIndex * 3;
            destination[destinationOffset] = TiffGrayscalePhotometricConverter.ScaleIntensityToUInt16(rgb[0]);
            destination[destinationOffset + 1] = TiffGrayscalePhotometricConverter.ScaleIntensityToUInt16(rgb[1]);
            destination[destinationOffset + 2] = TiffGrayscalePhotometricConverter.ScaleIntensityToUInt16(rgb[2]);
            pixelIndex = pixelIndex + 1;
        }

        return destination;
    }

    internal static double[] ConvertOnePixel(TiffImageInfo info, ReadOnlySpan<double> samples, int pixelIndex, TiffDiagnostics diagnostics)
    {
        double[] rgb = new double[3];
        int sourceOffset = pixelIndex * info.SamplesPerPixel;
        double lightness = DecodeLightness(info, samples[sourceOffset], diagnostics);
        double a = 0d;
        double b = 0d;
        if (info.SamplesPerPixel >= 3)
        {
            a = DecodeChroma(info, 1, samples[sourceOffset + 1], diagnostics);
            b = DecodeChroma(info, 2, samples[sourceOffset + 2], diagnostics);
        }

        if (diagnostics.HasErrors())
        {
            return rgb;
        }

        return ConvertLabToRgbIntensity(lightness, a, b);
    }

    internal static int ValidateLabRequest(TiffImageInfo info, int sampleCount, int outputBytesPerChannel, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        if (info == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "Image information is required for Lab conversion.");
            return 0;
        }

        if (!IsLabPhotometric(info.Photometric))
        {
            diagnostics.AddError(TiffErrorCode.UnsupportedPhotometric, "Lab conversion requires CIELab, ICCLab, or ITULab photometric interpretation.");
            return 0;
        }

        if (info.SamplesPerPixel != 1 && info.SamplesPerPixel < 3)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "Lab conversion requires one lightness sample or at least three samples per pixel.", info.ImageIndex, TiffTagId.SamplesPerPixel);
            return 0;
        }

        if (sampleCount % info.SamplesPerPixel != 0)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "Lab sample count does not match SamplesPerPixel.");
            return 0;
        }

        int pixelCount = sampleCount / info.SamplesPerPixel;
        int outputBytesPerPixel = 3 * outputBytesPerChannel;
        if (pixelCount > int.MaxValue / outputBytesPerPixel)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Lab conversion output byte count overflows the supported range.");
            return 0;
        }

        int outputByteCount = pixelCount * outputBytesPerPixel;
        TiffLimits actualLimits = TiffTagValueLoader.NormalizeLimits(limits);
        if (!actualLimits.AllowsAllocation((long)outputByteCount))
        {
            diagnostics.AddError(TiffErrorCode.AllocationLimitExceeded, "Lab conversion output exceeds the configured allocation limit.");
            return 0;
        }

        return pixelCount;
    }

    internal static bool IsLabPhotometric(TiffPhotometric photometric)
    {
        if (photometric == TiffPhotometric.CieLab)
        {
            return true;
        }

        if (photometric == TiffPhotometric.IccLab)
        {
            return true;
        }

        if (photometric == TiffPhotometric.ItuLab)
        {
            return true;
        }

        return false;
    }

    internal static double DecodeLightness(TiffImageInfo info, double sampleValue, TiffDiagnostics diagnostics)
    {
        if (!ValidateFiniteSample(sampleValue, diagnostics))
        {
            return 0d;
        }

        TiffSampleFormat sampleFormat = TiffPredictor.GetSampleFormat(info, 0);
        if (sampleFormat == TiffSampleFormat.IeeeFloatingPoint)
        {
            if (sampleValue < 0d || sampleValue > 100d)
            {
                diagnostics.AddError(TiffErrorCode.InvalidTagValue, "Floating-point Lab lightness must be in the 0 to 100 range.");
                return 0d;
            }

            return sampleValue;
        }

        if (sampleFormat == TiffSampleFormat.SignedInteger)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "Lab lightness requires unsigned, undefined, or floating-point samples.", info.ImageIndex, TiffTagId.SampleFormat);
            return 0d;
        }

        if (sampleFormat != TiffSampleFormat.UnsignedInteger && sampleFormat != TiffSampleFormat.Undefined)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "SampleFormat is not supported for Lab conversion.", info.ImageIndex, TiffTagId.SampleFormat);
            return 0d;
        }

        int bitsPerSample = ValidateBitsPerSample(info, 0, diagnostics);
        if (diagnostics.HasErrors())
        {
            return 0d;
        }

        if (!ValidateUnsignedIntegerSample(info, 0, sampleValue, bitsPerSample, diagnostics))
        {
            return 0d;
        }

        double codingMaximum = CalculateLabLightnessCodingMaximum(info, bitsPerSample);
        return Clamp(sampleValue / codingMaximum * 100d, 0d, 100d);
    }

    internal static double DecodeChroma(TiffImageInfo info, int channelIndex, double sampleValue, TiffDiagnostics diagnostics)
    {
        if (!ValidateFiniteSample(sampleValue, diagnostics))
        {
            return 0d;
        }

        TiffSampleFormat sampleFormat = TiffPredictor.GetSampleFormat(info, channelIndex);
        if (sampleFormat == TiffSampleFormat.IeeeFloatingPoint)
        {
            return sampleValue;
        }

        int bitsPerSample = ValidateBitsPerSample(info, channelIndex, diagnostics);
        if (diagnostics.HasErrors())
        {
            return 0d;
        }

        if (sampleFormat == TiffSampleFormat.SignedInteger)
        {
            if (!ValidateSignedIntegerSample(info, channelIndex, sampleValue, bitsPerSample, diagnostics))
            {
                return 0d;
            }

            return sampleValue;
        }

        if (sampleFormat == TiffSampleFormat.UnsignedInteger || sampleFormat == TiffSampleFormat.Undefined)
        {
            if (!ValidateUnsignedIntegerSample(info, channelIndex, sampleValue, bitsPerSample, diagnostics))
            {
                return 0d;
            }

            if (info.Photometric == TiffPhotometric.ItuLab)
            {
                return DecodeItuLabChroma(channelIndex, sampleValue, bitsPerSample);
            }

            return DecodeOffsetLabChroma(sampleValue, bitsPerSample);
        }

        diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "SampleFormat is not supported for Lab conversion.", info.ImageIndex, TiffTagId.SampleFormat);
        return 0d;
    }

    internal static int ValidateBitsPerSample(TiffImageInfo info, int channelIndex, TiffDiagnostics diagnostics)
    {
        int bitsPerSample = TiffSubByteSampleUnpacker.GetBitsPerSample(info, channelIndex, diagnostics);
        if (diagnostics.HasErrors())
        {
            return 0;
        }

        if (bitsPerSample <= 0 || bitsPerSample > 64)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "Lab conversion supports positive samples up to 64 bits.", info.ImageIndex, TiffTagId.BitsPerSample);
            return 0;
        }

        return bitsPerSample;
    }

    internal static bool ValidateFiniteSample(double sampleValue, TiffDiagnostics diagnostics)
    {
        if (double.IsNaN(sampleValue) || double.IsInfinity(sampleValue))
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, "Lab sample value must be finite.");
            return false;
        }

        return true;
    }

    internal static bool ValidateUnsignedIntegerSample(TiffImageInfo info, int channelIndex, double sampleValue, int bitsPerSample, TiffDiagnostics diagnostics)
    {
        if (Math.Truncate(sampleValue) != sampleValue)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, "Lab integer sample values must be whole values.");
            return false;
        }

        double maximumValue = TiffGrayscalePhotometricConverter.CalculateUnsignedMaximum(bitsPerSample);
        if (sampleValue < 0d || sampleValue > maximumValue)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "Lab unsigned sample is outside the BitsPerSample range.", info.ImageIndex, TiffTagId.BitsPerSample);
            return false;
        }

        return true;
    }

    internal static bool ValidateSignedIntegerSample(TiffImageInfo info, int channelIndex, double sampleValue, int bitsPerSample, TiffDiagnostics diagnostics)
    {
        if (Math.Truncate(sampleValue) != sampleValue)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, "Lab integer sample values must be whole values.");
            return false;
        }

        double minimumValue = 0d - TiffGrayscalePhotometricConverter.CalculatePowerOfTwo(bitsPerSample - 1);
        double maximumValue = TiffGrayscalePhotometricConverter.CalculatePowerOfTwo(bitsPerSample - 1) - 1d;
        if (sampleValue < minimumValue || sampleValue > maximumValue)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "Lab signed sample is outside the BitsPerSample range.", info.ImageIndex, TiffTagId.BitsPerSample);
            return false;
        }

        return true;
    }

    internal static double CalculateLabLightnessCodingMaximum(TiffImageInfo info, int bitsPerSample)
    {
        if (info.Photometric == TiffPhotometric.IccLab && bitsPerSample == 16)
        {
            return 65280d;
        }

        return TiffGrayscalePhotometricConverter.CalculateUnsignedMaximum(bitsPerSample);
    }

    internal static double DecodeOffsetLabChroma(double sampleValue, int bitsPerSample)
    {
        if (bitsPerSample == 8)
        {
            return sampleValue - 128d;
        }

        if (bitsPerSample == 16)
        {
            return (sampleValue - 32768d) / 256d;
        }

        double maximumValue = TiffGrayscalePhotometricConverter.CalculateUnsignedMaximum(bitsPerSample);
        return sampleValue / maximumValue * 255d - 128d;
    }

    internal static double DecodeItuLabChroma(int channelIndex, double sampleValue, int bitsPerSample)
    {
        double maximumValue = TiffGrayscalePhotometricConverter.CalculateUnsignedMaximum(bitsPerSample);
        double range = 170d;
        double offsetSample = TiffGrayscalePhotometricConverter.CalculatePowerOfTwo(bitsPerSample - 1);
        if (channelIndex == 2)
        {
            range = 200d;
            offsetSample = 3d * TiffGrayscalePhotometricConverter.CalculatePowerOfTwo(bitsPerSample - 3);
        }

        double minimumValue = 0d - offsetSample * range / maximumValue;
        return minimumValue + sampleValue * range / maximumValue;
    }

    internal static double[] ConvertLabToRgbIntensity(double lightness, double a, double b)
    {
        double[] rgb = new double[3];
        double fy = (lightness + 16d) / 116d;
        double fx = fy + a / 500d;
        double fz = fy - b / 200d;

        double x = D50X * ConvertLabFunctionToXyz(fx);
        double y = D50Y * ConvertLabFunctionToXyz(fy);
        double z = D50Z * ConvertLabFunctionToXyz(fz);

        double d65X = 0.9554734d * x - 0.0230985d * y + 0.0632593d * z;
        double d65Y = -0.0283697d * x + 1.0099956d * y + 0.0210414d * z;
        double d65Z = 0.012314d * x - 0.0205077d * y + 1.3303659d * z;

        double linearRed = 3.2404542d * d65X - 1.5371385d * d65Y - 0.4985314d * d65Z;
        double linearGreen = -0.969266d * d65X + 1.8760108d * d65Y + 0.041556d * d65Z;
        double linearBlue = 0.0556434d * d65X - 0.2040259d * d65Y + 1.0572252d * d65Z;

        rgb[0] = ConvertLinearSrgbToEncodedIntensity(linearRed);
        rgb[1] = ConvertLinearSrgbToEncodedIntensity(linearGreen);
        rgb[2] = ConvertLinearSrgbToEncodedIntensity(linearBlue);
        return rgb;
    }

    internal static double ConvertLabFunctionToXyz(double value)
    {
        double cube = value * value * value;
        if (cube > 216d / 24389d)
        {
            return cube;
        }

        return (116d * value - 16d) / 903.3d;
    }

    internal static double ConvertLinearSrgbToEncodedIntensity(double value)
    {
        if (value <= 0.0031308d)
        {
            return 12.92d * value;
        }

        return 1.055d * Math.Pow(value, 1d / 2.4d) - 0.055d;
    }

    internal static double Clamp(double value, double minimumValue, double maximumValue)
    {
        if (value < minimumValue)
        {
            return minimumValue;
        }

        if (value > maximumValue)
        {
            return maximumValue;
        }

        return value;
    }
}
