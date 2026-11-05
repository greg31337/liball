namespace raw;

internal static class RawOutputProcessor
{
    internal static RawRgbImage Create(RawRgbImage image, RawArea? crop, RawRenderOptions options)
    {
        int[] sourceChannels = Validate(image, crop, options);
        RawArea selected = crop == null ? new RawArea(RawAreaKind.StoredImage, 0, 0, image.Width, image.Height) : crop;
        RawOrientation transform = options.ApplyOrientation && !image.OrientationApplied ? image.Orientation : RawOrientation.Normal;
        int orientedWidth = SwapsDimensions(transform) ? selected.Height : selected.Width;
        int orientedHeight = SwapsDimensions(transform) ? selected.Width : selected.Height;
        int outputWidth = options.OutputWidth == 0 ? orientedWidth : options.OutputWidth;
        int outputHeight = options.OutputHeight == 0 ? orientedHeight : options.OutputHeight;
        int channelCount = image.ChannelCount;
        float[] oriented = OrientCrop(image, selected, transform, sourceChannels, orientedWidth, orientedHeight, options);
        float[] resized = orientedWidth == outputWidth && orientedHeight == outputHeight ? oriented : Resize(oriented, orientedWidth, orientedHeight, channelCount, outputWidth, outputHeight, options);
        RawChannelDescription[] channels = OutputChannels(channelCount);
        bool orientationApplied = options.ApplyOrientation ? true : image.OrientationApplied;
        return Convert(resized, outputWidth, outputHeight, channelCount, channels, image, orientationApplied, options);
    }

    internal static int[] Validate(RawRgbImage image, RawArea? crop, RawRenderOptions options)
    {
        if (image == null || options == null || !options.Validate().IsValid() || !image.Validate(options == null ? null : options.Limits).IsValid())
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "A valid floating rendered image and render options are required for output creation.", -1);
        }
        if (image.FloatingSamples == null || image.FloatingSamples.Length == 0)
        {
            throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedFormat, "Output creation requires floating gray, RGB, or RGBA samples.", -1);
        }
        if (crop != null && !crop.Validate(image.Width, image.Height).IsValid())
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidLength, "The output crop must be nonempty and contained in the current image.", -1);
        }
        if ((options ?? throw new System.InvalidOperationException("A required value is unavailable.")).ApplyOrientation && !image.OrientationApplied && image.Orientation == RawOrientation.Unknown)
        {
            throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedFormat, "An unknown orientation cannot be baked into output pixels.", -1);
        }
        int[] channels = new int[image.ChannelCount];
        if (image.ChannelCount == 1)
        {
            if (image.Channels[0].Kind != RawChannelKind.Gray)
            {
                throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedFormat, "Single-channel output requires a gray channel.", -1);
            }
            channels[0] = 0;
        }
        else
        {
            RawRgbChannelIndices rgb = RawWhiteBalanceProcessor.FindChannels(image);
            int alpha = image.ChannelCount == 4 ? RawCameraColorProcessor.FindAlpha(image) : -1;
            if (rgb.Red < 0 || rgb.Green < 0 || rgb.Blue < 0 || image.ChannelCount == 4 && alpha < 0)
            {
                throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedFormat, "Color output requires unique RGB channels and optional alpha.", -1);
            }
            channels[0] = rgb.Red;
            channels[1] = rgb.Green;
            channels[2] = rgb.Blue;
            if (image.ChannelCount == 4)
            {
                channels[3] = alpha;
            }
        }
        options.CheckCancellation();
        return channels;
    }

    internal static float[] OrientCrop(RawRgbImage image, RawArea crop, RawOrientation orientation, int[] sourceChannels, int width, int height, RawRenderOptions options)
    {
        int channelCount = image.ChannelCount;
        long pixelCount = CheckedMath.PixelCount(width, height, options.Limits);
        long sampleCountLong = CheckedMath.Multiply(pixelCount, channelCount);
        CheckedMath.DecodedSize(sampleCountLong, 4, options.Limits);
        CheckedMath.AllocationSize(sampleCountLong, 4, options.Limits);
        float[] output = new float[CheckedMath.ToInt32(sampleCountLong)];
        int y = 0;
        while (y < height)
        {
            options.CheckCancellation();
            int x = 0;
            while (x < width)
            {
                int sourceX = SourceX(x, y, crop.Width, crop.Height, orientation) + crop.X;
                int sourceY = SourceY(x, y, crop.Width, crop.Height, orientation) + crop.Y;
                int source = sourceY * image.RowStrideSamples + sourceX * channelCount;
                int destination = (y * width + x) * channelCount;
                int channel = 0;
                while (channel < channelCount)
                {
                    double value = image.FloatingSamples[source + sourceChannels[channel]];
                    if (!RawModelValidation.IsFinite(value))
                    {
                        throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "Output creation requires finite floating-point samples.", -1);
                    }
                    output[destination + channel] = (float)value;
                    channel++;
                }
                x++;
            }
            y++;
        }
        return output;
    }

    internal static bool SwapsDimensions(RawOrientation orientation)
    {
        return orientation == RawOrientation.Transpose || orientation == RawOrientation.Rotate90Clockwise || orientation == RawOrientation.Transverse || orientation == RawOrientation.Rotate270Clockwise;
    }

    internal static int SourceX(int x, int y, int width, int height, RawOrientation orientation)
    {
        switch (orientation)
        {
            case RawOrientation.MirrorHorizontal:
            case RawOrientation.Rotate180:
                return width - 1 - x;
            case RawOrientation.Transpose:
            case RawOrientation.Rotate90Clockwise:
                return y;
            case RawOrientation.Transverse:
            case RawOrientation.Rotate270Clockwise:
                return width - 1 - y;
            default:
                return x;
        }
    }

    internal static int SourceY(int x, int y, int width, int height, RawOrientation orientation)
    {
        switch (orientation)
        {
            case RawOrientation.Rotate180:
            case RawOrientation.MirrorVertical:
                return height - 1 - y;
            case RawOrientation.Transpose:
            case RawOrientation.Rotate270Clockwise:
                return x;
            case RawOrientation.Rotate90Clockwise:
            case RawOrientation.Transverse:
                return height - 1 - x;
            default:
                return y;
        }
    }

    internal static float[] Resize(float[] source, int sourceWidth, int sourceHeight, int channelCount, int outputWidth, int outputHeight, RawRenderOptions options)
    {
        long pixelCount = CheckedMath.PixelCount(outputWidth, outputHeight, options.Limits);
        long sampleCountLong = CheckedMath.Multiply(pixelCount, channelCount);
        CheckedMath.DecodedSize(sampleCountLong, 4, options.Limits);
        CheckedMath.AllocationSize(sampleCountLong, 4, options.Limits);
        float[] output = new float[CheckedMath.ToInt32(sampleCountLong)];
        int y = 0;
        while (y < outputHeight)
        {
            options.CheckCancellation();
            double sourceY = (y + 0.5) * sourceHeight / outputHeight - 0.5;
            int x = 0;
            while (x < outputWidth)
            {
                double sourceX = (x + 0.5) * sourceWidth / outputWidth - 0.5;
                int channel = 0;
                while (channel < channelCount)
                {
                    double value = Sample(source, sourceWidth, sourceHeight, channelCount, sourceX, sourceY, channel, options.Resampler);
                    if (!RawModelValidation.IsFinite(value) || value < -float.MaxValue || value > float.MaxValue)
                    {
                        throw new RawFormatException(RawDiagnosticCode.ArithmeticOverflow, "A resampled output value exceeds the floating-point range.", -1);
                    }
                    output[(y * outputWidth + x) * channelCount + channel] = (float)value;
                    channel++;
                }
                x++;
            }
            y++;
        }
        return output;
    }

    internal static double Sample(float[] source, int width, int height, int channelCount, double x, double y, int channel, RawResampler resampler)
    {
        if (resampler == RawResampler.NearestNeighbor)
        {
            int nearestX = Clamp((int)System.Math.Floor(x + 0.5), width);
            int nearestY = Clamp((int)System.Math.Floor(y + 0.5), height);
            return source[(nearestY * width + nearestX) * channelCount + channel];
        }
        int radius = resampler == RawResampler.Bilinear ? 1 : resampler == RawResampler.Bicubic ? 2 : 3;
        int firstX = resampler == RawResampler.Bilinear ? (int)System.Math.Floor(x) : (int)System.Math.Floor(x) - radius + 1;
        int firstY = resampler == RawResampler.Bilinear ? (int)System.Math.Floor(y) : (int)System.Math.Floor(y) - radius + 1;
        int count = radius * 2;
        double sum = 0.0;
        double weightSum = 0.0;
        int offsetY = 0;
        while (offsetY < count)
        {
            int sampleY = firstY + offsetY;
            double weightY = Kernel(y - sampleY, resampler);
            int boundedY = Clamp(sampleY, height);
            int offsetX = 0;
            while (offsetX < count)
            {
                int sampleX = firstX + offsetX;
                double weight = weightY * Kernel(x - sampleX, resampler);
                int boundedX = Clamp(sampleX, width);
                sum += source[(boundedY * width + boundedX) * channelCount + channel] * weight;
                weightSum += weight;
                offsetX++;
            }
            offsetY++;
        }
        if (System.Math.Abs(weightSum) < 0.000000000001)
        {
            int fallbackX = Clamp((int)System.Math.Floor(x + 0.5), width);
            int fallbackY = Clamp((int)System.Math.Floor(y + 0.5), height);
            return source[(fallbackY * width + fallbackX) * channelCount + channel];
        }
        return sum / weightSum;
    }

    internal static double Kernel(double distance, RawResampler resampler)
    {
        double value = System.Math.Abs(distance);
        if (resampler == RawResampler.Bilinear)
        {
            return value < 1.0 ? 1.0 - value : 0.0;
        }
        if (resampler == RawResampler.Bicubic)
        {
            if (value <= 1.0)
            {
                return 1.5 * value * value * value - 2.5 * value * value + 1.0;
            }
            if (value < 2.0)
            {
                return -0.5 * value * value * value + 2.5 * value * value - 4.0 * value + 2.0;
            }
            return 0.0;
        }
        if (value >= 3.0)
        {
            return 0.0;
        }
        if (value < 0.000000000001)
        {
            return 1.0;
        }
        double radians = System.Math.PI * value;
        return System.Math.Sin(radians) / radians * (System.Math.Sin(radians / 3.0) / (radians / 3.0));
    }

    internal static int Clamp(int value, int limit)
    {
        if (value < 0)
        {
            return 0;
        }
        if (value >= limit)
        {
            return limit - 1;
        }
        return value;
    }

    internal static RawRgbImage Convert(float[] samples, int width, int height, int channelCount, RawChannelDescription[] channels, RawRgbImage source, bool orientationApplied, RawRenderOptions options)
    {
        long sampleCountLong = CheckedMath.Multiply(CheckedMath.PixelCount(width, height, options.Limits), channelCount);
        int sampleCount = CheckedMath.ToInt32(sampleCountLong);
        byte[]? bytes = null;
        ushort[]? words = null;
        float[]? floating = null;
        if (options.OutputBitDepth == 8)
        {
            CheckedMath.DecodedSize(sampleCountLong, 1, options.Limits);
            CheckedMath.AllocationSize(sampleCountLong, 1, options.Limits);
            bytes = new byte[sampleCount];
        }
        else if (options.OutputBitDepth == 16)
        {
            CheckedMath.DecodedSize(sampleCountLong, 2, options.Limits);
            CheckedMath.AllocationSize(sampleCountLong, 2, options.Limits);
            words = new ushort[sampleCount];
        }
        else
        {
            CheckedMath.DecodedSize(sampleCountLong, 4, options.Limits);
            CheckedMath.AllocationSize(sampleCountLong, 4, options.Limits);
            floating = new float[sampleCount];
        }
        int y = 0;
        while (y < height)
        {
            options.CheckCancellation();
            int x = 0;
            while (x < width)
            {
                int channel = 0;
                while (channel < channelCount)
                {
                    int index = (y * width + x) * channelCount + channel;
                    double value = samples[index];
                    if (bytes != null)
                    {
                        bool dither = options.OrderedDithering && (channelCount == 1 || channel < 3);
                        bytes[index] = RawToneProcessor.Quantize(value, x, y, dither);
                    }
                    else if (words != null)
                    {
                        double bounded = RawToneProcessor.Bound(value);
                        words[index] = (ushort)System.Math.Floor(bounded * 65535.0 + 0.5);
                    }
                    else
                    {
                        (floating ?? throw new System.InvalidOperationException("A required value is unavailable."))[index] = (float)value;
                    }
                    channel++;
                }
                x++;
            }
            y++;
        }
        return new RawRgbImage(width, height, channelCount, width * channelCount, bytes, words, floating, channels, source.ColorSpace, source.TransferFunction, source.Orientation, orientationApplied, true, source.Diagnostics);
    }

    internal static RawChannelDescription[] OutputChannels(int channelCount)
    {
        if (channelCount == 1)
        {
            return new RawChannelDescription[] { new RawChannelDescription("Gray", RawChannelKind.Gray) };
        }
        return RawOutputColorProcessor.RgbChannels(channelCount);
    }
}
