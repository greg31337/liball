using System;

namespace tiff;

internal class TiffCfaPhotometricConverter
{
    internal static byte[] ConvertToRgb8(TiffImageInfo info, ReadOnlySpan<double> samples, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        double[] intensities = ConvertToRgbIntensities(info, samples, 1, limits, diagnostics);
        if (diagnostics.HasErrors())
        {
            return new byte[0];
        }

        byte[] destination = new byte[intensities.Length];
        int index = 0;
        while (index < intensities.Length)
        {
            destination[index] = TiffGrayscalePhotometricConverter.ScaleIntensityToByte(intensities[index]);
            index = index + 1;
        }

        return destination;
    }

    internal static ushort[] ConvertToRgb16(TiffImageInfo info, ReadOnlySpan<double> samples, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        double[] intensities = ConvertToRgbIntensities(info, samples, 2, limits, diagnostics);
        if (diagnostics.HasErrors())
        {
            return new ushort[0];
        }

        ushort[] destination = new ushort[intensities.Length];
        int index = 0;
        while (index < intensities.Length)
        {
            destination[index] = TiffGrayscalePhotometricConverter.ScaleIntensityToUInt16(intensities[index]);
            index = index + 1;
        }

        return destination;
    }

    internal static double[] ConvertToRgbIntensities(TiffImageInfo info, ReadOnlySpan<double> samples, int outputBytesPerChannel, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        int pixelCount = ValidateRequest(info, samples.Length, outputBytesPerChannel, limits, diagnostics);
        if (diagnostics.HasErrors())
        {
            return new double[0];
        }

        double[] sourceIntensities = new double[pixelCount];
        int index = 0;
        while (index < pixelCount)
        {
            sourceIntensities[index] = TiffRgbPhotometricConverter.ConvertChannelToIntensity(info, 0, samples[index], diagnostics);
            if (diagnostics.HasErrors())
            {
                return new double[0];
            }

            index = index + 1;
        }

        double[] destination = new double[pixelCount * 3];
        int y = 0;
        while (y < (int)info.Length)
        {
            int x = 0;
            while (x < (int)info.Width)
            {
                int pixelIndex = y * (int)info.Width + x;
                int siteColor = GetSiteColor(info, x, y, diagnostics);
                int channelIndex = 0;
                while (channelIndex < 3)
                {
                    if (siteColor == channelIndex)
                    {
                        destination[pixelIndex * 3 + channelIndex] = sourceIntensities[pixelIndex];
                    }
                    else
                    {
                        destination[pixelIndex * 3 + channelIndex] = InterpolateChannel(info, sourceIntensities, x, y, channelIndex, diagnostics);
                    }

                    if (diagnostics.HasErrors())
                    {
                        return new double[0];
                    }

                    channelIndex = channelIndex + 1;
                }

                x = x + 1;
            }

            y = y + 1;
        }

        return destination;
    }

    internal static double InterpolateChannel(TiffImageInfo info, double[] samples, int x, int y, int channelIndex, TiffDiagnostics diagnostics)
    {
        double sum = 0d;
        int count = 0;
        int neighborY = y - 1;
        while (neighborY <= y + 1)
        {
            int neighborX = x - 1;
            while (neighborX <= x + 1)
            {
                if (neighborX >= 0 && neighborY >= 0 && neighborX < (int)info.Width && neighborY < (int)info.Length && (neighborX != x || neighborY != y) && GetSiteColor(info, neighborX, neighborY, diagnostics) == channelIndex)
                {
                    sum = sum + samples[neighborY * (int)info.Width + neighborX];
                    count = count + 1;
                }

                if (diagnostics.HasErrors())
                {
                    return 0d;
                }

                neighborX = neighborX + 1;
            }

            neighborY = neighborY + 1;
        }

        if (count == 0)
        {
            return samples[y * (int)info.Width + x];
        }

        return sum / (double)count;
    }

    internal static int GetSiteColor(TiffImageInfo info, int x, int y, TiffDiagnostics diagnostics)
    {
        int repeatRows = info.CFARepeatPatternDimensions[0];
        int repeatColumns = info.CFARepeatPatternDimensions[1];
        int patternIndex = (y % repeatRows) * repeatColumns + (x % repeatColumns);
        int color = info.CFAPattern[patternIndex];
        if (color < 0 || color > 2)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.UnsupportedPhotometric, "CFA conversion supports red, green, and blue filter-plane identifiers only.", info.ImageIndex, TiffTagId.CFAPattern);
            return 0;
        }

        return color;
    }

    internal static int ValidateRequest(TiffImageInfo info, int sampleCount, int outputBytesPerChannel, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        if (info == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "Image information is required for CFA conversion.");
            return 0;
        }

        if (info.Photometric != TiffPhotometric.ColorFilterArray || info.SamplesPerPixel != 1)
        {
            diagnostics.AddError(TiffErrorCode.UnsupportedPhotometric, "CFA conversion requires one color-filter-array sample per pixel.");
            return 0;
        }

        if (info.Width == 0 || info.Length == 0 || info.Width > int.MaxValue || info.Length > int.MaxValue || info.Width > (ulong)int.MaxValue / info.Length)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "CFA dimensions exceed the supported conversion range.");
            return 0;
        }

        int pixelCount = (int)(info.Width * info.Length);
        if (sampleCount != pixelCount)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "CFA sample count does not match the image dimensions.");
            return 0;
        }

        if (info.CFARepeatPatternDimensions == null || info.CFARepeatPatternDimensions.Length != 2 || info.CFARepeatPatternDimensions[0] <= 0 || info.CFARepeatPatternDimensions[1] <= 0)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagCount, "CFA conversion requires two positive repeat-pattern dimensions.", info.ImageIndex, TiffTagId.CFARepeatPatternDim);
            return 0;
        }

        if (info.CFARepeatPatternDimensions[0] > int.MaxValue / info.CFARepeatPatternDimensions[1])
        {
            diagnostics.AddErrorForTag(TiffErrorCode.IntegerOverflow, "CFA repeat-pattern dimensions overflow the supported range.", info.ImageIndex, TiffTagId.CFARepeatPatternDim);
            return 0;
        }

        int patternCount = info.CFARepeatPatternDimensions[0] * info.CFARepeatPatternDimensions[1];
        if (info.CFAPattern == null || info.CFAPattern.Length != patternCount)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagCount, "CFA pattern count must match the repeat-pattern dimensions.", info.ImageIndex, TiffTagId.CFAPattern);
            return 0;
        }

        int outputBytesPerPixel = 3 * outputBytesPerChannel;
        if (pixelCount > int.MaxValue / outputBytesPerPixel)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "CFA conversion output exceeds the supported array range.");
            return 0;
        }

        int outputByteCount = pixelCount * outputBytesPerPixel;
        TiffLimits actualLimits = TiffTagValueLoader.NormalizeLimits(limits);
        if (!actualLimits.AllowsAllocation(outputByteCount))
        {
            diagnostics.AddError(TiffErrorCode.AllocationLimitExceeded, "CFA conversion output exceeds the configured allocation limit.");
            return 0;
        }

        return pixelCount;
    }
}
