using System;

namespace tiff;

internal class TiffCmykPhotometricConverter
{
    internal static byte[] ConvertToRgb8(TiffImageInfo info, ReadOnlySpan<double> samples, TiffDiagnostics diagnostics)
    {
        return ConvertToRgb8(info, samples, new TiffLimits(), diagnostics);
    }

    internal static byte[] ConvertToRgb8(TiffImageInfo info, ReadOnlySpan<double> samples, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        int pixelCount = ValidateCmykRequest(info, samples.Length, 1, limits, diagnostics);
        if (diagnostics.HasErrors())
        {
            return new byte[0];
        }

        byte[] destination = new byte[pixelCount * 3];
        int pixelIndex = 0;
        while (pixelIndex < pixelCount)
        {
            WriteRgb8Pixel(info, samples, destination, pixelIndex, diagnostics);
            if (diagnostics.HasErrors())
            {
                return new byte[0];
            }

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
        int pixelCount = ValidateCmykRequest(info, samples.Length, 2, limits, diagnostics);
        if (diagnostics.HasErrors())
        {
            return new ushort[0];
        }

        ushort[] destination = new ushort[pixelCount * 3];
        int pixelIndex = 0;
        while (pixelIndex < pixelCount)
        {
            WriteRgb16Pixel(info, samples, destination, pixelIndex, diagnostics);
            if (diagnostics.HasErrors())
            {
                return new ushort[0];
            }

            pixelIndex = pixelIndex + 1;
        }

        return destination;
    }

    internal static void WriteRgb8Pixel(TiffImageInfo info, ReadOnlySpan<double> samples, byte[] destination, int pixelIndex, TiffDiagnostics diagnostics)
    {
        double[] rgb = ConvertOnePixel(info, samples, pixelIndex, diagnostics);
        if (diagnostics.HasErrors())
        {
            return;
        }

        int destinationOffset = pixelIndex * 3;
        destination[destinationOffset] = TiffGrayscalePhotometricConverter.ScaleIntensityToByte(rgb[0]);
        destination[destinationOffset + 1] = TiffGrayscalePhotometricConverter.ScaleIntensityToByte(rgb[1]);
        destination[destinationOffset + 2] = TiffGrayscalePhotometricConverter.ScaleIntensityToByte(rgb[2]);
    }

    internal static void WriteRgb16Pixel(TiffImageInfo info, ReadOnlySpan<double> samples, ushort[] destination, int pixelIndex, TiffDiagnostics diagnostics)
    {
        double[] rgb = ConvertOnePixel(info, samples, pixelIndex, diagnostics);
        if (diagnostics.HasErrors())
        {
            return;
        }

        int destinationOffset = pixelIndex * 3;
        destination[destinationOffset] = TiffGrayscalePhotometricConverter.ScaleIntensityToUInt16(rgb[0]);
        destination[destinationOffset + 1] = TiffGrayscalePhotometricConverter.ScaleIntensityToUInt16(rgb[1]);
        destination[destinationOffset + 2] = TiffGrayscalePhotometricConverter.ScaleIntensityToUInt16(rgb[2]);
    }

    internal static double[] ConvertOnePixel(TiffImageInfo info, ReadOnlySpan<double> samples, int pixelIndex, TiffDiagnostics diagnostics)
    {
        double[] rgb = new double[3];
        int sourceOffset = pixelIndex * info.SamplesPerPixel;
        double cyan = TiffRgbPhotometricConverter.ConvertChannelToIntensity(info, 0, samples[sourceOffset], diagnostics);
        double magenta = TiffRgbPhotometricConverter.ConvertChannelToIntensity(info, 1, samples[sourceOffset + 1], diagnostics);
        double yellow = TiffRgbPhotometricConverter.ConvertChannelToIntensity(info, 2, samples[sourceOffset + 2], diagnostics);
        double black = TiffRgbPhotometricConverter.ConvertChannelToIntensity(info, 3, samples[sourceOffset + 3], diagnostics);
        if (diagnostics.HasErrors())
        {
            return rgb;
        }

        rgb[0] = (1d - cyan) * (1d - black);
        rgb[1] = (1d - magenta) * (1d - black);
        rgb[2] = (1d - yellow) * (1d - black);
        return rgb;
    }

    internal static int ValidateCmykRequest(TiffImageInfo info, int sampleCount, int outputBytesPerChannel, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        if (info == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "Image information is required for CMYK conversion.");
            return 0;
        }

        if (info.Photometric != TiffPhotometric.Cmyk)
        {
            diagnostics.AddError(TiffErrorCode.UnsupportedPhotometric, "CMYK conversion requires CMYK photometric interpretation.");
            return 0;
        }

        if (info.SamplesPerPixel < 4)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "CMYK conversion requires at least four samples per pixel.", info.ImageIndex, TiffTagId.SamplesPerPixel);
            return 0;
        }

        if (sampleCount % info.SamplesPerPixel != 0)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "CMYK sample count does not match SamplesPerPixel.");
            return 0;
        }

        int pixelCount = sampleCount / info.SamplesPerPixel;
        int outputBytesPerPixel = 3 * outputBytesPerChannel;
        if (pixelCount > int.MaxValue / outputBytesPerPixel)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "CMYK conversion output byte count overflows the supported range.");
            return 0;
        }

        int outputByteCount = pixelCount * outputBytesPerPixel;
        TiffLimits actualLimits = TiffTagValueLoader.NormalizeLimits(limits);
        if (!actualLimits.AllowsAllocation((long)outputByteCount))
        {
            diagnostics.AddError(TiffErrorCode.AllocationLimitExceeded, "CMYK conversion output exceeds the configured allocation limit.");
            return 0;
        }

        return pixelCount;
    }
}
