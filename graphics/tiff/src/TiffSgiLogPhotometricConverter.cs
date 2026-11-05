using System;

namespace tiff;

internal class TiffSgiLogPhotometricConverter
{
    internal const double NaturalLogTwo = 0.69314718055994530942d;
    internal const double UvScale = 410d;

    internal static byte[] ConvertLogLToRgb8(TiffImageInfo info, ReadOnlySpan<double> samples, TiffDiagnostics diagnostics)
    {
        return ConvertLogLToRgb8(info, samples, new TiffLimits(), diagnostics);
    }

    internal static byte[] ConvertLogLToRgb8(TiffImageInfo info, ReadOnlySpan<double> samples, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        int pixelCount = ValidateLogLRequest(info, samples.Length, 1, limits, diagnostics);
        if (diagnostics.HasErrors())
        {
            return new byte[0];
        }

        byte[] destination = new byte[pixelCount * 3];
        int pixelIndex = 0;
        while (pixelIndex < pixelCount)
        {
            if (!ValidateWholeSampleInRange(samples[pixelIndex], 16, false, diagnostics))
            {
                return new byte[0];
            }

            double y = DecodeLogL16ToY((int)samples[pixelIndex]);
            double intensity = ToneMapLogIntensity(y);
            byte value = TiffGrayscalePhotometricConverter.ScaleIntensityToByte(intensity);
            int destinationOffset = pixelIndex * 3;
            destination[destinationOffset] = value;
            destination[destinationOffset + 1] = value;
            destination[destinationOffset + 2] = value;
            pixelIndex = pixelIndex + 1;
        }

        return destination;
    }

    internal static ushort[] ConvertLogLToRgb16(TiffImageInfo info, ReadOnlySpan<double> samples, TiffDiagnostics diagnostics)
    {
        return ConvertLogLToRgb16(info, samples, new TiffLimits(), diagnostics);
    }

    internal static ushort[] ConvertLogLToRgb16(TiffImageInfo info, ReadOnlySpan<double> samples, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        int pixelCount = ValidateLogLRequest(info, samples.Length, 2, limits, diagnostics);
        if (diagnostics.HasErrors())
        {
            return new ushort[0];
        }

        ushort[] destination = new ushort[pixelCount * 3];
        int pixelIndex = 0;
        while (pixelIndex < pixelCount)
        {
            if (!ValidateWholeSampleInRange(samples[pixelIndex], 16, false, diagnostics))
            {
                return new ushort[0];
            }

            double y = DecodeLogL16ToY((int)samples[pixelIndex]);
            double intensity = ToneMapLogIntensity(y);
            ushort value = TiffGrayscalePhotometricConverter.ScaleIntensityToUInt16(intensity);
            int destinationOffset = pixelIndex * 3;
            destination[destinationOffset] = value;
            destination[destinationOffset + 1] = value;
            destination[destinationOffset + 2] = value;
            pixelIndex = pixelIndex + 1;
        }

        return destination;
    }

    internal static byte[] ConvertLogLuvToRgb8(TiffImageInfo info, ReadOnlySpan<double> samples, TiffDiagnostics diagnostics)
    {
        return ConvertLogLuvToRgb8(info, samples, new TiffLimits(), diagnostics);
    }

    internal static byte[] ConvertLogLuvToRgb8(TiffImageInfo info, ReadOnlySpan<double> samples, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        int pixelCount = ValidateLogLuvRequest(info, samples.Length, 1, limits, diagnostics);
        if (diagnostics.HasErrors())
        {
            return new byte[0];
        }

        byte[] destination = new byte[pixelCount * 3];
        int pixelIndex = 0;
        while (pixelIndex < pixelCount)
        {
            double[] rgb = ConvertLogLuvPixelToRgbIntensity(info, samples, pixelIndex, diagnostics);
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

    internal static ushort[] ConvertLogLuvToRgb16(TiffImageInfo info, ReadOnlySpan<double> samples, TiffDiagnostics diagnostics)
    {
        return ConvertLogLuvToRgb16(info, samples, new TiffLimits(), diagnostics);
    }

    internal static ushort[] ConvertLogLuvToRgb16(TiffImageInfo info, ReadOnlySpan<double> samples, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        int pixelCount = ValidateLogLuvRequest(info, samples.Length, 2, limits, diagnostics);
        if (diagnostics.HasErrors())
        {
            return new ushort[0];
        }

        ushort[] destination = new ushort[pixelCount * 3];
        int pixelIndex = 0;
        while (pixelIndex < pixelCount)
        {
            double[] rgb = ConvertLogLuvPixelToRgbIntensity(info, samples, pixelIndex, diagnostics);
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

    internal static int ValidateLogLRequest(TiffImageInfo info, int sampleCount, int outputBytesPerChannel, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        if (info == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "Image information is required for LogL conversion.");
            return 0;
        }

        if (info.Photometric != TiffPhotometric.LogL)
        {
            diagnostics.AddError(TiffErrorCode.UnsupportedPhotometric, "LogL conversion requires LogL photometric interpretation.");
            return 0;
        }

        if (info.SamplesPerPixel != 1)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "LogL conversion requires one sample per pixel.", info.ImageIndex, TiffTagId.SamplesPerPixel);
            return 0;
        }

        if (sampleCount % info.SamplesPerPixel != 0)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "LogL sample count does not match SamplesPerPixel.");
            return 0;
        }

        if (!ValidateUnsignedIntegerSample(info, 0, 16, diagnostics))
        {
            return 0;
        }

        return ValidateOutputAllocation(sampleCount, outputBytesPerChannel, limits, "LogL", diagnostics);
    }

    internal static int ValidateLogLuvRequest(TiffImageInfo info, int sampleCount, int outputBytesPerChannel, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        if (info == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "Image information is required for LogLuv conversion.");
            return 0;
        }

        if (info.Photometric != TiffPhotometric.LogLuv)
        {
            diagnostics.AddError(TiffErrorCode.UnsupportedPhotometric, "LogLuv conversion requires LogLuv photometric interpretation.");
            return 0;
        }

        if (info.SamplesPerPixel != 1 && info.SamplesPerPixel < 3)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "LogLuv conversion requires one packed sample or at least three Luv samples per pixel.", info.ImageIndex, TiffTagId.SamplesPerPixel);
            return 0;
        }

        if (sampleCount % info.SamplesPerPixel != 0)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "LogLuv sample count does not match SamplesPerPixel.");
            return 0;
        }

        if (info.SamplesPerPixel == 1)
        {
            if (!ValidatePackedLogLuvShape(info, diagnostics))
            {
                return 0;
            }
        }
        else
        {
            if (!ValidateLuvTripletShape(info, diagnostics))
            {
                return 0;
            }
        }

        int pixelCount = sampleCount / info.SamplesPerPixel;
        return ValidateOutputAllocation(pixelCount, outputBytesPerChannel, limits, "LogLuv", diagnostics);
    }

    internal static int ValidateOutputAllocation(int pixelCount, int outputBytesPerChannel, TiffLimits limits, string name, TiffDiagnostics diagnostics)
    {
        int outputBytesPerPixel = 3 * outputBytesPerChannel;
        if (pixelCount > int.MaxValue / outputBytesPerPixel)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, name + " conversion output byte count overflows the supported range.");
            return 0;
        }

        int outputByteCount = pixelCount * outputBytesPerPixel;
        TiffLimits actualLimits = TiffTagValueLoader.NormalizeLimits(limits);
        if (!actualLimits.AllowsAllocation((long)outputByteCount))
        {
            diagnostics.AddError(TiffErrorCode.AllocationLimitExceeded, name + " conversion output exceeds the configured allocation limit.");
            return 0;
        }

        return pixelCount;
    }

    internal static bool ValidatePackedLogLuvShape(TiffImageInfo info, TiffDiagnostics diagnostics)
    {
        int bitsPerSample = TiffSubByteSampleUnpacker.GetBitsPerSample(info, 0, diagnostics);
        if (diagnostics.HasErrors())
        {
            return false;
        }

        if (bitsPerSample == 24)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.UnsupportedPhotometric, "Packed 24-bit LogLuv requires the SGILog uvcode lookup table and is not implemented.", info.ImageIndex, TiffTagId.PhotometricInterpretation);
            return false;
        }

        if (bitsPerSample != 32)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "Packed LogLuv conversion requires 32 BitsPerSample.", info.ImageIndex, TiffTagId.BitsPerSample);
            return false;
        }

        if (!IsUnsignedOrUndefined(info, 0))
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "Packed LogLuv conversion requires unsigned or undefined integer samples.", info.ImageIndex, TiffTagId.SampleFormat);
            return false;
        }

        return true;
    }

    internal static bool ValidateLuvTripletShape(TiffImageInfo info, TiffDiagnostics diagnostics)
    {
        if (!ValidateUnsignedIntegerSample(info, 0, 16, diagnostics))
        {
            return false;
        }

        if (!ValidateSignedIntegerSample(info, 1, 16, diagnostics))
        {
            return false;
        }

        if (!ValidateSignedIntegerSample(info, 2, 16, diagnostics))
        {
            return false;
        }

        return true;
    }

    internal static bool ValidateUnsignedIntegerSample(TiffImageInfo info, int channelIndex, int requiredBitsPerSample, TiffDiagnostics diagnostics)
    {
        int bitsPerSample = TiffSubByteSampleUnpacker.GetBitsPerSample(info, channelIndex, diagnostics);
        if (diagnostics.HasErrors())
        {
            return false;
        }

        if (bitsPerSample != requiredBitsPerSample)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "SGILog conversion requires the expected BitsPerSample value.", info.ImageIndex, TiffTagId.BitsPerSample);
            return false;
        }

        if (!IsUnsignedOrUndefined(info, channelIndex))
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "SGILog conversion requires unsigned or undefined integer samples for this channel.", info.ImageIndex, TiffTagId.SampleFormat);
            return false;
        }

        return true;
    }

    internal static bool ValidateSignedIntegerSample(TiffImageInfo info, int channelIndex, int requiredBitsPerSample, TiffDiagnostics diagnostics)
    {
        int bitsPerSample = TiffSubByteSampleUnpacker.GetBitsPerSample(info, channelIndex, diagnostics);
        if (diagnostics.HasErrors())
        {
            return false;
        }

        if (bitsPerSample != requiredBitsPerSample)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "SGILog Luv triplet conversion requires 16-bit samples.", info.ImageIndex, TiffTagId.BitsPerSample);
            return false;
        }

        if (TiffPredictor.GetSampleFormat(info, channelIndex) != TiffSampleFormat.SignedInteger)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "SGILog Luv triplet chroma channels require signed integer samples.", info.ImageIndex, TiffTagId.SampleFormat);
            return false;
        }

        return true;
    }

    internal static bool IsUnsignedOrUndefined(TiffImageInfo info, int channelIndex)
    {
        TiffSampleFormat sampleFormat = TiffPredictor.GetSampleFormat(info, channelIndex);
        if (sampleFormat == TiffSampleFormat.UnsignedInteger)
        {
            return true;
        }

        if (sampleFormat == TiffSampleFormat.Undefined)
        {
            return true;
        }

        return false;
    }

    internal static double[] ConvertLogLuvPixelToRgbIntensity(TiffImageInfo info, ReadOnlySpan<double> samples, int pixelIndex, TiffDiagnostics diagnostics)
    {
        double[] xyz = new double[3];
        if (info.SamplesPerPixel == 1)
        {
            double sampleValue = samples[pixelIndex];
            if (!ValidateWholeSampleInRange(sampleValue, 32, false, diagnostics))
            {
                return xyz;
            }

            xyz = DecodeLogLuv32ToXyz((uint)sampleValue);
        }
        else
        {
            int sourceOffset = pixelIndex * info.SamplesPerPixel;
            if (!ValidateWholeSampleInRange(samples[sourceOffset], 16, false, diagnostics))
            {
                return xyz;
            }

            if (!ValidateWholeSampleInRange(samples[sourceOffset + 1], 16, true, diagnostics))
            {
                return xyz;
            }

            if (!ValidateWholeSampleInRange(samples[sourceOffset + 2], 16, true, diagnostics))
            {
                return xyz;
            }

            xyz = DecodeLuv48ToXyz((int)samples[sourceOffset], (int)samples[sourceOffset + 1], (int)samples[sourceOffset + 2]);
        }

        return ConvertXyzToRgbIntensity(xyz);
    }

    internal static bool ValidateWholeSampleInRange(double sampleValue, int bitsPerSample, bool signedSample, TiffDiagnostics diagnostics)
    {
        if (double.IsNaN(sampleValue) || double.IsInfinity(sampleValue))
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, "SGILog sample value must be finite.");
            return false;
        }

        if (Math.Truncate(sampleValue) != sampleValue)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, "SGILog sample values must be whole values.");
            return false;
        }

        double minimumValue = 0d;
        double maximumValue = TiffGrayscalePhotometricConverter.CalculateUnsignedMaximum(bitsPerSample);
        if (signedSample)
        {
            minimumValue = 0d - TiffGrayscalePhotometricConverter.CalculatePowerOfTwo(bitsPerSample - 1);
            maximumValue = TiffGrayscalePhotometricConverter.CalculatePowerOfTwo(bitsPerSample - 1) - 1d;
        }

        if (sampleValue < minimumValue || sampleValue > maximumValue)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, "SGILog sample value is outside the BitsPerSample range.");
            return false;
        }

        return true;
    }

    internal static double DecodeLogL16ToY(int encoded)
    {
        int lightness = encoded & 0x7fff;
        if (lightness == 0)
        {
            return 0d;
        }

        double y = Math.Exp(NaturalLogTwo / 256d * ((double)lightness + 0.5d) - NaturalLogTwo * 64d);
        if ((encoded & 0x8000) != 0)
        {
            return 0d - y;
        }

        return y;
    }

    internal static double[] DecodeLogLuv32ToXyz(uint encoded)
    {
        int lightness = (int)(encoded >> 16);
        double yValue = DecodeLogL16ToY(lightness);
        double u = ((double)((encoded >> 8) & 0xffU) + 0.5d) / UvScale;
        double v = ((double)(encoded & 0xffU) + 0.5d) / UvScale;
        return ConvertYuvToXyz(yValue, u, v);
    }

    internal static double[] DecodeLuv48ToXyz(int encodedLightness, int encodedU, int encodedV)
    {
        double yValue = DecodeLogL16ToY(encodedLightness);
        double u = ((double)encodedU + 0.5d) / 32768d;
        double v = ((double)encodedV + 0.5d) / 32768d;
        return ConvertYuvToXyz(yValue, u, v);
    }

    internal static double[] ConvertYuvToXyz(double yValue, double u, double v)
    {
        double[] xyz = new double[3];
        if (yValue <= 0d)
        {
            return xyz;
        }

        double denominator = 6d * u - 16d * v + 12d;
        if (denominator == 0d || v == 0d)
        {
            return xyz;
        }

        double scale = 1d / denominator;
        double xChromaticity = 9d * u * scale;
        double yChromaticity = 4d * v * scale;
        if (yChromaticity <= 0d)
        {
            return xyz;
        }

        xyz[0] = xChromaticity / yChromaticity * yValue;
        xyz[1] = yValue;
        xyz[2] = (1d - xChromaticity - yChromaticity) / yChromaticity * yValue;
        return xyz;
    }

    internal static double[] ConvertXyzToRgbIntensity(double[] xyz)
    {
        double[] rgb = new double[3];
        double red = 2.690d * xyz[0] - 1.276d * xyz[1] - 0.414d * xyz[2];
        double green = -1.022d * xyz[0] + 1.978d * xyz[1] + 0.044d * xyz[2];
        double blue = 0.061d * xyz[0] - 0.224d * xyz[1] + 1.163d * xyz[2];
        rgb[0] = ToneMapLogIntensity(red);
        rgb[1] = ToneMapLogIntensity(green);
        rgb[2] = ToneMapLogIntensity(blue);
        return rgb;
    }

    internal static double ToneMapLogIntensity(double value)
    {
        if (value <= 0d)
        {
            return 0d;
        }

        if (value >= 1d)
        {
            return 1d;
        }

        return Math.Sqrt(value);
    }
}
