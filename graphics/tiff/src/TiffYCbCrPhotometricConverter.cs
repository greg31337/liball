using System;

namespace tiff;

internal class TiffYCbCrPhotometricConverter
{
    internal static byte[] ConvertToRgb8(TiffImageInfo info, ReadOnlySpan<double> samples, TiffDiagnostics diagnostics)
    {
        return ConvertToRgb8(info, samples, new TiffLimits(), diagnostics);
    }

    internal static byte[] ConvertToRgb8(TiffImageInfo info, ReadOnlySpan<double> samples, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        int pixelCount = ValidateYCbCrRequest(info, samples.Length, 1, limits, diagnostics);
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
        int pixelCount = ValidateYCbCrRequest(info, samples.Length, 2, limits, diagnostics);
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
        double ySample = samples[sourceOffset];
        double cbSample = samples[sourceOffset + 1];
        double crSample = samples[sourceOffset + 2];
        ValidateSampleValue(info, 0, ySample, diagnostics);
        ValidateSampleValue(info, 1, cbSample, diagnostics);
        ValidateSampleValue(info, 2, crSample, diagnostics);
        if (diagnostics.HasErrors())
        {
            return rgb;
        }

        double y = (ySample - info.ReferenceBlackWhite[0]) / (info.ReferenceBlackWhite[1] - info.ReferenceBlackWhite[0]);
        double cb = (cbSample - info.ReferenceBlackWhite[2]) / (2d * (info.ReferenceBlackWhite[3] - info.ReferenceBlackWhite[2]));
        double cr = (crSample - info.ReferenceBlackWhite[4]) / (2d * (info.ReferenceBlackWhite[5] - info.ReferenceBlackWhite[4]));
        double lumaRed = info.YCbCrCoefficients[0];
        double lumaGreen = info.YCbCrCoefficients[1];
        double lumaBlue = info.YCbCrCoefficients[2];

        rgb[0] = y + cr * (2d - 2d * lumaRed);
        rgb[2] = y + cb * (2d - 2d * lumaBlue);
        rgb[1] = (y - lumaRed * rgb[0] - lumaBlue * rgb[2]) / lumaGreen;
        return rgb;
    }

    internal static bool ValidateSampleValue(TiffImageInfo info, int channelIndex, double sampleValue, TiffDiagnostics diagnostics)
    {
        if (double.IsNaN(sampleValue) || double.IsInfinity(sampleValue))
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, "YCbCr sample value must be finite.");
            return false;
        }

        TiffSampleFormat sampleFormat = TiffPredictor.GetSampleFormat(info, channelIndex);
        if (sampleFormat != TiffSampleFormat.UnsignedInteger && sampleFormat != TiffSampleFormat.Undefined)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "YCbCr conversion requires unsigned or undefined integer samples.", info.ImageIndex, TiffTagId.SampleFormat);
            return false;
        }

        if (Math.Truncate(sampleValue) != sampleValue)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, "YCbCr sample values must be whole values.");
            return false;
        }

        int bitsPerSample = TiffSubByteSampleUnpacker.GetBitsPerSample(info, channelIndex, diagnostics);
        if (diagnostics.HasErrors())
        {
            return false;
        }

        double maximumValue = TiffGrayscalePhotometricConverter.CalculateUnsignedMaximum(bitsPerSample);
        if (sampleValue < 0d || sampleValue > maximumValue)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, "YCbCr sample value is outside the BitsPerSample range.");
            return false;
        }

        return true;
    }

    internal static int ValidateYCbCrRequest(TiffImageInfo info, int sampleCount, int outputBytesPerChannel, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        if (info == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "Image information is required for YCbCr conversion.");
            return 0;
        }

        if (info.Photometric != TiffPhotometric.YCbCr)
        {
            diagnostics.AddError(TiffErrorCode.UnsupportedPhotometric, "YCbCr conversion requires YCbCr photometric interpretation.");
            return 0;
        }

        if (info.SamplesPerPixel < 3)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "YCbCr conversion requires at least three samples per pixel.", info.ImageIndex, TiffTagId.SamplesPerPixel);
            return 0;
        }

        if (sampleCount % info.SamplesPerPixel != 0)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "YCbCr sample count does not match SamplesPerPixel.");
            return 0;
        }

        if (!ValidateCoefficients(info, diagnostics))
        {
            return 0;
        }

        if (!ValidateReferenceBlackWhite(info, diagnostics))
        {
            return 0;
        }

        int pixelCount = sampleCount / info.SamplesPerPixel;
        int outputBytesPerPixel = 3 * outputBytesPerChannel;
        if (pixelCount > int.MaxValue / outputBytesPerPixel)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "YCbCr conversion output byte count overflows the supported range.");
            return 0;
        }

        int outputByteCount = pixelCount * outputBytesPerPixel;
        TiffLimits actualLimits = TiffTagValueLoader.NormalizeLimits(limits);
        if (!actualLimits.AllowsAllocation((long)outputByteCount))
        {
            diagnostics.AddError(TiffErrorCode.AllocationLimitExceeded, "YCbCr conversion output exceeds the configured allocation limit.");
            return 0;
        }

        return pixelCount;
    }

    internal static bool ValidateCoefficients(TiffImageInfo info, TiffDiagnostics diagnostics)
    {
        if (info.YCbCrCoefficients == null || info.YCbCrCoefficients.Length != 3)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagCount, "YCbCrCoefficients must contain three values.", info.ImageIndex, TiffTagId.YCbCrCoefficients);
            return false;
        }

        int index = 0;
        while (index < 3)
        {
            double value = info.YCbCrCoefficients[index];
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0d)
            {
                diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "YCbCrCoefficients values must be positive finite values.", info.ImageIndex, TiffTagId.YCbCrCoefficients);
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool ValidateReferenceBlackWhite(TiffImageInfo info, TiffDiagnostics diagnostics)
    {
        if (info.ReferenceBlackWhite == null || info.ReferenceBlackWhite.Length != 6)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagCount, "ReferenceBlackWhite must contain six values.", info.ImageIndex, TiffTagId.ReferenceBlackWhite);
            return false;
        }

        int index = 0;
        while (index < 6)
        {
            double value = info.ReferenceBlackWhite[index];
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "ReferenceBlackWhite values must be finite.", info.ImageIndex, TiffTagId.ReferenceBlackWhite);
                return false;
            }

            index = index + 1;
        }

        if (info.ReferenceBlackWhite[1] <= info.ReferenceBlackWhite[0] || info.ReferenceBlackWhite[3] <= info.ReferenceBlackWhite[2] || info.ReferenceBlackWhite[5] <= info.ReferenceBlackWhite[4])
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "ReferenceBlackWhite white values must be greater than black values.", info.ImageIndex, TiffTagId.ReferenceBlackWhite);
            return false;
        }

        return true;
    }
}
