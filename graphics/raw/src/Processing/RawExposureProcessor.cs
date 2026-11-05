namespace raw;

internal static class RawExposureProcessor
{
    internal static RawRgbImage Apply(RawRgbImage image, RawRenderOptions options)
    {
        RawRgbChannelIndices channels = Validate(image, options);
        int channelCount = image.ChannelCount;
        long pixelCountLong = CheckedMath.PixelCount(image.Width, image.Height, options.Limits);
        long sampleCountLong = CheckedMath.Multiply(pixelCountLong, channelCount);
        CheckedMath.DecodedSize(sampleCountLong, 4, options.Limits);
        CheckedMath.AllocationSize(sampleCountLong, 4, options.Limits);
        int pixelCount = CheckedMath.ToInt32(pixelCountLong);
        float[] output = new float[CheckedMath.ToInt32(sampleCountLong)];
        FillBase(image, options, channels, output);
        double scale = ExposureScale(options.ExposureCompensation);
        double[]? scratch = null;
        if (options.AutoBrightness || options.SaturationProtection > 0.0)
        {
            CheckedMath.AllocationSize(pixelCountLong, 8, options.Limits);
            scratch = new double[pixelCount];
        }
        if (options.AutoBrightness)
        {
            double brightness = BrightnessPercentile(output, channelCount, pixelCount, options.AutoBrightnessClippingThreshold, scratch);
            if (brightness > 0.000000000001)
            {
                scale *= 1.0 / brightness;
            }
        }
        if (options.SaturationProtection > 0.0)
        {
            double protectedScale = ProtectedScale(output, channelCount, pixelCount, options.AutoBrightnessClippingThreshold, scratch);
            if (scale > protectedScale)
            {
                scale *= System.Math.Pow(protectedScale / scale, options.SaturationProtection);
            }
        }
        if (!RawModelValidation.IsFinite(scale) || scale < 0.0)
        {
            throw new RawFormatException(RawDiagnosticCode.ArithmeticOverflow, "The selected exposure scale is outside the managed numeric range.", -1);
        }
        Scale(output, image.Width, image.Height, channelCount, scale, options);
        ApplyHighlights(output, image.Width, image.Height, channelCount, options);
        RawChannelDescription[] descriptions = RawOutputColorProcessor.RgbChannels(channelCount);
        return new RawRgbImage(image.Width, image.Height, channelCount, image.Width * channelCount, null, null, output, descriptions, image.ColorSpace, RawTransferFunction.Linear, image.Orientation, image.OrientationApplied, true, image.Diagnostics);
    }

    internal static RawRgbChannelIndices Validate(RawRgbImage image, RawRenderOptions options)
    {
        if (image == null || options == null || !options.Validate().IsValid() || !image.Validate(options == null ? null : options.Limits).IsValid())
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "A valid scene-linear image and render options are required for exposure processing.", -1);
        }
        if (image.FloatingSamples == null || image.FloatingSamples.Length == 0 || image.TransferFunction != RawTransferFunction.Linear || image.ChannelCount != 3 && image.ChannelCount != 4 || !RgbColorSpace(image.ColorSpace))
        {
            throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedFormat, "Exposure processing requires floating scene-linear RGB or RGBA samples.", -1);
        }
        RawRgbChannelIndices channels = RawWhiteBalanceProcessor.FindChannels(image);
        if (channels.Red < 0 || channels.Green < 0 || channels.Blue < 0 || image.ChannelCount == 4 && RawCameraColorProcessor.FindAlpha(image) < 0)
        {
            throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedFormat, "Exposure processing requires unique RGB channels and optional alpha.", -1);
        }
        (options ?? throw new System.InvalidOperationException("A required value is unavailable.")).CheckCancellation();
        return channels;
    }

    internal static bool RgbColorSpace(RawColorSpace colorSpace)
    {
        switch (colorSpace)
        {
            case RawColorSpace.RawCamera:
            case RawColorSpace.Srgb:
            case RawColorSpace.DisplayP3:
            case RawColorSpace.AdobeRgb:
            case RawColorSpace.ProPhotoRgb:
            case RawColorSpace.Rec2020:
            case RawColorSpace.AcesCg:
                return true;
            default:
                return false;
        }
    }

    internal static void FillBase(RawRgbImage image, RawRenderOptions options, RawRgbChannelIndices channels, float[] output)
    {
        int alpha = image.ChannelCount == 4 ? RawCameraColorProcessor.FindAlpha(image) : -1;
        int y = 0;
        while (y < image.Height)
        {
            options.CheckCancellation();
            int x = 0;
            while (x < image.Width)
            {
                int source = y * image.RowStrideSamples + x * image.ChannelCount;
                int destination = (y * image.Width + x) * image.ChannelCount;
                Store(output, destination, Shadow(image.FloatingSamples[source + channels.Red], options.ShadowScale));
                Store(output, destination + 1, Shadow(image.FloatingSamples[source + channels.Green], options.ShadowScale));
                Store(output, destination + 2, Shadow(image.FloatingSamples[source + channels.Blue], options.ShadowScale));
                if (image.ChannelCount == 4)
                {
                    Store(output, destination + 3, image.FloatingSamples[source + alpha]);
                }
                x++;
            }
            y++;
        }
    }

    internal static double Shadow(double value, double shadowScale)
    {
        if (!RawModelValidation.IsFinite(value))
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "Exposure processing requires finite floating-point samples.", -1);
        }
        if (value <= 0.0)
        {
            return value * shadowScale;
        }
        double bounded = value < 1.0 ? value : 1.0;
        double factor = 1.0 + (shadowScale - 1.0) * (1.0 - bounded);
        return value * factor;
    }

    internal static double ExposureScale(double stops)
    {
        double scale = System.Math.Pow(2.0, stops);
        if (!RawModelValidation.IsFinite(scale))
        {
            throw new RawFormatException(RawDiagnosticCode.ArithmeticOverflow, "Exposure compensation exceeds the managed numeric range.", -1);
        }
        return scale;
    }

    internal static double BrightnessPercentile(float[] samples, int channelCount, int pixelCount, double clippingThreshold, double[]? scratch)
    {
        int pixel = 0;
        while (pixel < pixelCount)
        {
            int index = pixel * channelCount;
            double maximum = samples[index];
            if (samples[index + 1] > maximum)
            {
                maximum = samples[index + 1];
            }
            if (samples[index + 2] > maximum)
            {
                maximum = samples[index + 2];
            }
            (scratch ?? throw new System.InvalidOperationException("A required value is unavailable."))[pixel] = maximum > 0.0 ? maximum : 0.0;
            pixel++;
        }
        return Percentile(scratch, pixelCount, clippingThreshold);
    }

    internal static double ProtectedScale(float[] samples, int channelCount, int pixelCount, double clippingThreshold, double[]? scratch)
    {
        double protectedScale = double.MaxValue;
        int channel = 0;
        while (channel < 3)
        {
            int pixel = 0;
            while (pixel < pixelCount)
            {
                double value = samples[pixel * channelCount + channel];
                (scratch ?? throw new System.InvalidOperationException("A required value is unavailable."))[pixel] = value > 0.0 ? value : 0.0;
                pixel++;
            }
            double percentile = Percentile(scratch, pixelCount, clippingThreshold);
            if (percentile > 0.000000000001)
            {
                double channelScale = 1.0 / percentile;
                if (channelScale < protectedScale)
                {
                    protectedScale = channelScale;
                }
            }
            channel++;
        }
        return protectedScale == double.MaxValue ? 1.0 : protectedScale;
    }

    internal static double Percentile(double[]? values, int count, double clippingThreshold)
    {
        if (values == null)
        {
            throw new System.InvalidOperationException("The percentile sample buffer is unavailable.");
        }
        System.Array.Sort(values, 0, count);
        int index = (int)System.Math.Floor((count - 1) * (1.0 - clippingThreshold));
        if (index < 0)
        {
            index = 0;
        }
        else if (index >= count)
        {
            index = count - 1;
        }
        return values[index];
    }

    internal static void Scale(float[] samples, int width, int height, int channelCount, double scale, RawRenderOptions options)
    {
        int y = 0;
        while (y < height)
        {
            options.CheckCancellation();
            int x = 0;
            while (x < width)
            {
                int index = (y * width + x) * channelCount;
                Store(samples, index, samples[index] * scale);
                Store(samples, index + 1, samples[index + 1] * scale);
                Store(samples, index + 2, samples[index + 2] * scale);
                x++;
            }
            y++;
        }
    }

    internal static void ApplyHighlights(float[] samples, int width, int height, int channelCount, RawRenderOptions options)
    {
        if (options.HighlightMode == RawHighlightMode.Reconstruct)
        {
            CheckedMath.AllocationSize(samples.LongLength, 4, options.Limits);
            float[] source = new float[samples.Length];
            System.Array.Copy(samples, source, samples.Length);
            Reconstruct(samples, source, width, height, channelCount, options);
        }
        int y = 0;
        while (y < height)
        {
            options.CheckCancellation();
            int x = 0;
            while (x < width)
            {
                int index = (y * width + x) * channelCount;
                if (options.HighlightMode == RawHighlightMode.Clip)
                {
                    Clip(samples, index);
                }
                else
                {
                    Blend(samples, index);
                }
                x++;
            }
            y++;
        }
    }

    internal static void Clip(float[] samples, int index)
    {
        int channel = 0;
        while (channel < 3)
        {
            if (samples[index + channel] > 1.0F)
            {
                samples[index + channel] = 1.0F;
            }
            channel++;
        }
    }

    internal static void Blend(float[] samples, int index)
    {
        double maximum = samples[index];
        if (samples[index + 1] > maximum)
        {
            maximum = samples[index + 1];
        }
        if (samples[index + 2] > maximum)
        {
            maximum = samples[index + 2];
        }
        if (maximum > 1.0)
        {
            samples[index] = (float)(samples[index] / maximum);
            samples[index + 1] = (float)(samples[index + 1] / maximum);
            samples[index + 2] = (float)(samples[index + 2] / maximum);
        }
    }

    internal static void Reconstruct(float[] output, float[] source, int width, int height, int channelCount, RawRenderOptions options)
    {
        int y = 0;
        while (y < height)
        {
            options.CheckCancellation();
            int x = 0;
            while (x < width)
            {
                int index = (y * width + x) * channelCount;
                int channel = 0;
                while (channel < 3)
                {
                    if (source[index + channel] > 1.0F)
                    {
                        double estimate = ReconstructedValue(source, width, height, channelCount, x, y, channel);
                        Store(output, index + channel, estimate);
                    }
                    channel++;
                }
                x++;
            }
            y++;
        }
    }

    internal static double ReconstructedValue(float[] samples, int width, int height, int channelCount, int x, int y, int targetChannel)
    {
        int pixel = (y * width + x) * channelCount;
        double estimates = 0.0;
        int estimateCount = 0;
        int referenceChannel = 0;
        while (referenceChannel < 3)
        {
            double currentReference = samples[pixel + referenceChannel];
            if (referenceChannel != targetChannel && currentReference > 0.000000000001 && currentReference <= 1.0)
            {
                double ratios = 0.0;
                int ratioCount = 0;
                int neighborY = y - 1;
                while (neighborY <= y + 1)
                {
                    int neighborX = x - 1;
                    while (neighborX <= x + 1)
                    {
                        if ((neighborX != x || neighborY != y) && neighborX >= 0 && neighborY >= 0 && neighborX < width && neighborY < height)
                        {
                            int neighbor = (neighborY * width + neighborX) * channelCount;
                            double target = samples[neighbor + targetChannel];
                            double reference = samples[neighbor + referenceChannel];
                            if (target >= 0.0 && target < 1.0 && reference > 0.000000000001 && reference < 1.0)
                            {
                                double ratio = target / reference;
                                if (RawModelValidation.IsFinite(ratio) && ratio <= 64.0)
                                {
                                    ratios += ratio;
                                    ratioCount++;
                                }
                            }
                        }
                        neighborX++;
                    }
                    neighborY++;
                }
                if (ratioCount > 0)
                {
                    estimates += currentReference * ratios / ratioCount;
                    estimateCount++;
                }
            }
            referenceChannel++;
        }
        return estimateCount == 0 ? samples[pixel + targetChannel] : estimates / estimateCount;
    }

    internal static void Store(float[] samples, int index, double value)
    {
        if (!RawModelValidation.IsFinite(value) || value < -float.MaxValue || value > float.MaxValue)
        {
            throw new RawFormatException(RawDiagnosticCode.ArithmeticOverflow, "An exposure-stage sample exceeds the floating-point range.", -1);
        }
        samples[index] = (float)value;
    }
}
