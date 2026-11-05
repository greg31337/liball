namespace raw;

internal static class RawToneProcessor
{
    internal static RawRgbImage Apply(RawRgbImage image, RawRenderOptions options)
    {
        RawRgbChannelIndices channels = ValidateLinear(image, options);
        RawOutputColorProcessor.ValidateTransfer(image.ColorSpace, options.TransferFunction);
        int channelCount = image.ChannelCount;
        int alpha = channelCount == 4 ? RawCameraColorProcessor.FindAlpha(image) : -1;
        long pixelCount = CheckedMath.PixelCount(image.Width, image.Height, options.Limits);
        long sampleCountLong = CheckedMath.Multiply(pixelCount, channelCount);
        CheckedMath.DecodedSize(sampleCountLong, 4, options.Limits);
        CheckedMath.AllocationSize(sampleCountLong, 4, options.Limits);
        float[] output = new float[CheckedMath.ToInt32(sampleCountLong)];
        int y = 0;
        while (y < image.Height)
        {
            options.CheckCancellation();
            int x = 0;
            while (x < image.Width)
            {
                int source = y * image.RowStrideSamples + x * channelCount;
                int destination = (y * image.Width + x) * channelCount;
                double red = Shape(image.FloatingSamples[source + channels.Red], options);
                double green = Shape(image.FloatingSamples[source + channels.Green], options);
                double blue = Shape(image.FloatingSamples[source + channels.Blue], options);
                if (options.GamutMode == RawGamutMode.Clip)
                {
                    red = Bound(red);
                    green = Bound(green);
                    blue = Bound(blue);
                }
                else if (options.GamutMode == RawGamutMode.Compress)
                {
                    double luminance = 0.2126 * red + 0.7152 * green + 0.0722 * blue;
                    double pivot = Bound(luminance);
                    double scale = CompressionScale(red, green, blue, pivot);
                    red = pivot + (red - pivot) * scale;
                    green = pivot + (green - pivot) * scale;
                    blue = pivot + (blue - pivot) * scale;
                    red = Bound(red);
                    green = Bound(green);
                    blue = Bound(blue);
                }
                Store(output, destination, RawOutputColorProcessor.Encode(red, options.TransferFunction, options.Gamma));
                Store(output, destination + 1, RawOutputColorProcessor.Encode(green, options.TransferFunction, options.Gamma));
                Store(output, destination + 2, RawOutputColorProcessor.Encode(blue, options.TransferFunction, options.Gamma));
                if (channelCount == 4)
                {
                    Store(output, destination + 3, image.FloatingSamples[source + alpha]);
                }
                x++;
            }
            y++;
        }
        return new RawRgbImage(image.Width, image.Height, channelCount, image.Width * channelCount, null, null, output, RawOutputColorProcessor.RgbChannels(channelCount), image.ColorSpace, options.TransferFunction, image.Orientation, image.OrientationApplied, true, image.Diagnostics);
    }

    internal static RawRgbImage ConvertToEightBit(RawRgbImage image, RawRenderOptions options)
    {
        RawRgbChannelIndices channels = ValidateFloatingRgb(image, options);
        if (options.OutputBitDepth != 8)
        {
            throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedFormat, "Ordered eight-bit conversion requires an eight-bit output selection.", -1);
        }
        int channelCount = image.ChannelCount;
        int alpha = channelCount == 4 ? RawCameraColorProcessor.FindAlpha(image) : -1;
        long pixelCount = CheckedMath.PixelCount(image.Width, image.Height, options.Limits);
        long sampleCountLong = CheckedMath.Multiply(pixelCount, channelCount);
        CheckedMath.DecodedSize(sampleCountLong, 1, options.Limits);
        CheckedMath.AllocationSize(sampleCountLong, 1, options.Limits);
        byte[] output = new byte[CheckedMath.ToInt32(sampleCountLong)];
        int y = 0;
        while (y < image.Height)
        {
            options.CheckCancellation();
            int x = 0;
            while (x < image.Width)
            {
                int source = y * image.RowStrideSamples + x * channelCount;
                int destination = (y * image.Width + x) * channelCount;
                output[destination] = Quantize(image.FloatingSamples[source + channels.Red], x, y, options.OrderedDithering);
                output[destination + 1] = Quantize(image.FloatingSamples[source + channels.Green], x, y, options.OrderedDithering);
                output[destination + 2] = Quantize(image.FloatingSamples[source + channels.Blue], x, y, options.OrderedDithering);
                if (channelCount == 4)
                {
                    output[destination + 3] = Quantize(image.FloatingSamples[source + alpha], x, y, false);
                }
                x++;
            }
            y++;
        }
        return new RawRgbImage(image.Width, image.Height, channelCount, image.Width * channelCount, output, null, null, RawOutputColorProcessor.RgbChannels(channelCount), image.ColorSpace, image.TransferFunction, image.Orientation, image.OrientationApplied, true, image.Diagnostics);
    }

    internal static RawRgbChannelIndices ValidateLinear(RawRgbImage image, RawRenderOptions options)
    {
        RawRgbChannelIndices channels = ValidateFloatingRgb(image, options);
        if (image.TransferFunction != RawTransferFunction.Linear)
        {
            throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedFormat, "Tone processing requires floating scene-linear RGB or RGBA samples.", -1);
        }
        return channels;
    }

    internal static RawRgbChannelIndices ValidateFloatingRgb(RawRgbImage image, RawRenderOptions options)
    {
        if (image == null || options == null || !options.Validate().IsValid() || !image.Validate(options == null ? null : options.Limits).IsValid())
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "A valid floating RGB image and render options are required for tone processing.", -1);
        }
        if (image.FloatingSamples == null || image.FloatingSamples.Length == 0 || image.ChannelCount != 3 && image.ChannelCount != 4 || !RawExposureProcessor.RgbColorSpace(image.ColorSpace))
        {
            throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedFormat, "Tone processing requires floating RGB or RGBA samples in a managed RGB color space.", -1);
        }
        RawRgbChannelIndices channels = RawWhiteBalanceProcessor.FindChannels(image);
        if (channels.Red < 0 || channels.Green < 0 || channels.Blue < 0 || image.ChannelCount == 4 && RawCameraColorProcessor.FindAlpha(image) < 0)
        {
            throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedFormat, "Tone processing requires unique RGB channels and optional alpha.", -1);
        }
        (options ?? throw new System.InvalidOperationException("A required value is unavailable.")).CheckCancellation();
        return channels;
    }

    internal static double Shape(double value, RawRenderOptions options)
    {
        if (!RawModelValidation.IsFinite(value))
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "Tone processing requires finite floating-point samples.", -1);
        }
        double contrasted = 0.5 + (value - 0.5) * options.Contrast;
        return EvaluateCurve(contrasted, options.ToneCurve);
    }

    internal static double EvaluateCurve(double value, double[] samples)
    {
        if (samples.Length == 0)
        {
            return value;
        }
        int last = samples.Length - 1;
        double position = value * last;
        int lower;
        if (position <= 0.0)
        {
            lower = 0;
        }
        else if (position >= last)
        {
            lower = last - 1;
        }
        else
        {
            lower = (int)System.Math.Floor(position);
        }
        double fraction = position - lower;
        return samples[lower] + (samples[lower + 1] - samples[lower]) * fraction;
    }

    internal static double CompressionScale(double red, double green, double blue, double pivot)
    {
        double scale = 1.0;
        scale = ChannelScale(red, pivot, scale);
        scale = ChannelScale(green, pivot, scale);
        scale = ChannelScale(blue, pivot, scale);
        return scale;
    }

    internal static double ChannelScale(double value, double pivot, double current)
    {
        double candidate = current;
        if (value < 0.0 && value < pivot)
        {
            candidate = pivot / (pivot - value);
        }
        else if (value > 1.0 && value > pivot)
        {
            candidate = (1.0 - pivot) / (value - pivot);
        }
        return candidate < current ? candidate : current;
    }

    internal static double Bound(double value)
    {
        if (value < 0.0)
        {
            return 0.0;
        }
        if (value > 1.0)
        {
            return 1.0;
        }
        return value;
    }

    internal static byte Quantize(double value, int x, int y, bool dither)
    {
        if (!RawModelValidation.IsFinite(value))
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "Eight-bit conversion requires finite floating-point samples.", -1);
        }
        double bounded = Bound(value);
        double offset = dither ? (BayerValue(x, y) + 0.5) / 16.0 - 0.5 : 0.0;
        int quantized = (int)System.Math.Floor(bounded * 255.0 + 0.5 + offset);
        if (quantized < 0)
        {
            quantized = 0;
        }
        else if (quantized > 255)
        {
            quantized = 255;
        }
        return (byte)quantized;
    }

    internal static int BayerValue(int x, int y)
    {
        int column = x & 3;
        switch (y & 3)
        {
            case 0:
                return column == 0 ? 0 : column == 1 ? 8 : column == 2 ? 2 : 10;
            case 1:
                return column == 0 ? 12 : column == 1 ? 4 : column == 2 ? 14 : 6;
            case 2:
                return column == 0 ? 3 : column == 1 ? 11 : column == 2 ? 1 : 9;
            default:
                return column == 0 ? 15 : column == 1 ? 7 : column == 2 ? 13 : 5;
        }
    }

    internal static void Store(float[] output, int index, double value)
    {
        if (!RawModelValidation.IsFinite(value) || value < -float.MaxValue || value > float.MaxValue)
        {
            throw new RawFormatException(RawDiagnosticCode.ArithmeticOverflow, "A tone-processed sample exceeds the floating-point output range.", -1);
        }
        output[index] = (float)value;
    }
}
