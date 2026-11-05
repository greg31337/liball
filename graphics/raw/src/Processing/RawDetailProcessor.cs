namespace raw;

internal static class RawDetailProcessor
{
    internal static RawRgbImage Apply(RawRgbImage image, RawChromaticAberration? chromaticAberration, RawRenderOptions options)
    {
        RawRgbChannelIndices channels = Validate(image, chromaticAberration, options);
        int channelCount = image.ChannelCount;
        long pixelCount = CheckedMath.PixelCount(image.Width, image.Height, options.Limits);
        long sampleCountLong = CheckedMath.Multiply(pixelCount, channelCount);
        CheckedMath.DecodedSize(sampleCountLong, 8, options.Limits);
        CheckedMath.AllocationSize(sampleCountLong, 4, options.Limits);
        int sampleCount = CheckedMath.ToInt32(sampleCountLong);
        float[] current = CopyCanonical(image, channels, options, sampleCount);
        if (options.DenoiseStrength > 0.0)
        {
            if (options.DenoiseMode == RawDenoiseMode.HotPixel)
            {
                current = HotPixels(current, image.Width, image.Height, channelCount, options);
            }
            else if (options.DenoiseMode == RawDenoiseMode.MedianChroma)
            {
                current = MedianChroma(current, image.Width, image.Height, channelCount, options.DenoiseStrength, options);
            }
            else if (options.DenoiseMode == RawDenoiseMode.WaveletLike)
            {
                current = WaveletLike(current, image.Width, image.Height, channelCount, options);
            }
        }
        if (options.FalseColorSuppression > 0.0)
        {
            current = MedianChroma(current, image.Width, image.Height, channelCount, options.FalseColorSuppression, options);
        }
        if (options.ApplyPostDemosaicChromaticAberration)
        {
            current = CorrectChromaticAberration(current, image.Width, image.Height, channelCount, chromaticAberration, options);
        }
        if (options.CaptureSharpening > 0.0)
        {
            current = Sharpen(current, image.Width, image.Height, channelCount, options);
        }
        return new RawRgbImage(image.Width, image.Height, channelCount, image.Width * channelCount, null, null, current, RawOutputColorProcessor.RgbChannels(channelCount), image.ColorSpace, RawTransferFunction.Linear, image.Orientation, image.OrientationApplied, true, image.Diagnostics);
    }

    internal static RawRgbChannelIndices Validate(RawRgbImage image, RawChromaticAberration? chromaticAberration, RawRenderOptions options)
    {
        if (image == null || options == null || !options.Validate().IsValid() || !image.Validate(options == null ? null : options.Limits).IsValid())
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "A valid scene-linear image and render options are required for detail processing.", -1);
        }
        if (image.FloatingSamples == null || image.FloatingSamples.Length == 0 || image.TransferFunction != RawTransferFunction.Linear || image.ChannelCount != 3 && image.ChannelCount != 4 || !RawExposureProcessor.RgbColorSpace(image.ColorSpace))
        {
            throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedFormat, "Detail processing requires floating scene-linear RGB or RGBA samples.", -1);
        }
        RawRgbChannelIndices channels = RawWhiteBalanceProcessor.FindChannels(image);
        if (channels.Red < 0 || channels.Green < 0 || channels.Blue < 0 || image.ChannelCount == 4 && RawCameraColorProcessor.FindAlpha(image) < 0)
        {
            throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedFormat, "Detail processing requires unique RGB channels and optional alpha.", -1);
        }
        if ((options ?? throw new System.InvalidOperationException("A required value is unavailable.")).ApplyPostDemosaicChromaticAberration)
        {
            if (chromaticAberration == null)
            {
                throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedFormat, "Post-demosaic chromatic-aberration correction requires calibration.", -1);
            }
            if (!RawModelValidation.IsFinite(chromaticAberration.CenterX) || !RawModelValidation.IsFinite(chromaticAberration.CenterY) || !RawModelValidation.IsFinite(chromaticAberration.RedScale) || !RawModelValidation.IsFinite(chromaticAberration.BlueScale) || chromaticAberration.CenterX < 0.0 || chromaticAberration.CenterY < 0.0 || chromaticAberration.CenterX > image.Width - 1 || chromaticAberration.CenterY > image.Height - 1 || chromaticAberration.RedScale <= 0.0 || chromaticAberration.BlueScale <= 0.0)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidDescriptor, "Post-demosaic chromatic-aberration calibration requires an in-image center and positive finite scales.", -1);
            }
        }
        options.CheckCancellation();
        return channels;
    }

    internal static float[] CopyCanonical(RawRgbImage image, RawRgbChannelIndices channels, RawRenderOptions options, int sampleCount)
    {
        float[] output = new float[sampleCount];
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
                Store(output, destination, image.FloatingSamples[source + channels.Red]);
                Store(output, destination + 1, image.FloatingSamples[source + channels.Green]);
                Store(output, destination + 2, image.FloatingSamples[source + channels.Blue]);
                if (image.ChannelCount == 4)
                {
                    Store(output, destination + 3, image.FloatingSamples[source + alpha]);
                }
                x++;
            }
            y++;
        }
        return output;
    }

    internal static float[] HotPixels(float[] source, int width, int height, int channelCount, RawRenderOptions options)
    {
        float[] output = Copy(source, options);
        double[] neighbors = new double[8];
        int y = 0;
        while (y < height)
        {
            options.CheckCancellation();
            int x = 0;
            while (x < width)
            {
                int channel = 0;
                while (channel < 3)
                {
                    int count = Neighborhood(source, width, height, channelCount, x, y, channel, neighbors, false);
                    if (count >= 3)
                    {
                        double median = Median(neighbors, count);
                        double deviation = 0.0;
                        double minimum = neighbors[0];
                        double maximum = neighbors[0];
                        int index = 0;
                        while (index < count)
                        {
                            deviation += System.Math.Abs(neighbors[index] - median);
                            if (neighbors[index] < minimum)
                            {
                                minimum = neighbors[index];
                            }
                            if (neighbors[index] > maximum)
                            {
                                maximum = neighbors[index];
                            }
                            index++;
                        }
                        double threshold = 0.02 + (1.0 - options.DenoiseStrength) * 0.25 + deviation * 4.0 / count;
                        int sample = (y * width + x) * channelCount + channel;
                        double value = source[sample];
                        if (value > maximum + threshold || value < minimum - threshold)
                        {
                            Store(output, sample, median);
                        }
                    }
                    channel++;
                }
                x++;
            }
            y++;
        }
        return output;
    }

    internal static int Neighborhood(float[] source, int width, int height, int channelCount, int x, int y, int channel, double[] values, bool includeCenter)
    {
        int count = 0;
        int neighborY = y - 1;
        while (neighborY <= y + 1)
        {
            int boundedY = Bound(neighborY, height);
            int neighborX = x - 1;
            while (neighborX <= x + 1)
            {
                int boundedX = Bound(neighborX, width);
                if (includeCenter || neighborX != x || neighborY != y)
                {
                    values[count] = source[(boundedY * width + boundedX) * channelCount + channel];
                    count++;
                }
                neighborX++;
            }
            neighborY++;
        }
        return count;
    }

    internal static double Median(double[] values, int count)
    {
        int index = 1;
        while (index < count)
        {
            double value = values[index];
            int position = index;
            while (position > 0 && values[position - 1] > value)
            {
                values[position] = values[position - 1];
                position--;
            }
            values[position] = value;
            index++;
        }
        if ((count & 1) == 0)
        {
            return (values[count / 2 - 1] + values[count / 2]) * 0.5;
        }
        return values[count / 2];
    }

    internal static float[] MedianChroma(float[] source, int width, int height, int channelCount, double strength, RawRenderOptions options)
    {
        float[] output = Copy(source, options);
        double[] redChroma = new double[9];
        double[] blueChroma = new double[9];
        int y = 0;
        while (y < height)
        {
            options.CheckCancellation();
            int x = 0;
            while (x < width)
            {
                int count = 0;
                int neighborY = y - 1;
                while (neighborY <= y + 1)
                {
                    int boundedY = Bound(neighborY, height);
                    int neighborX = x - 1;
                    while (neighborX <= x + 1)
                    {
                        int boundedX = Bound(neighborX, width);
                        int neighbor = (boundedY * width + boundedX) * channelCount;
                        double luminance = Luminance(source, neighbor);
                        redChroma[count] = source[neighbor] - luminance;
                        blueChroma[count] = source[neighbor + 2] - luminance;
                        count++;
                        neighborX++;
                    }
                    neighborY++;
                }
                int pixel = (y * width + x) * channelCount;
                double currentLuminance = Luminance(source, pixel);
                double currentRed = source[pixel] - currentLuminance;
                double currentBlue = source[pixel + 2] - currentLuminance;
                double filteredRed = currentRed * (1.0 - strength) + Median(redChroma, count) * strength;
                double filteredBlue = currentBlue * (1.0 - strength) + Median(blueChroma, count) * strength;
                SetLuminanceChroma(output, pixel, currentLuminance, filteredRed, filteredBlue);
                x++;
            }
            y++;
        }
        return output;
    }

    internal static float[] WaveletLike(float[] source, int width, int height, int channelCount, RawRenderOptions options)
    {
        float[] output = Copy(source, options);
        int y = 0;
        while (y < height)
        {
            options.CheckCancellation();
            int x = 0;
            while (x < width)
            {
                double luminanceSum = 0.0;
                double redSum = 0.0;
                double blueSum = 0.0;
                int count = 0;
                int neighborY = y - 1;
                while (neighborY <= y + 1)
                {
                    int boundedY = Bound(neighborY, height);
                    int neighborX = x - 1;
                    while (neighborX <= x + 1)
                    {
                        int boundedX = Bound(neighborX, width);
                        int neighbor = (boundedY * width + boundedX) * channelCount;
                        double neighborLuminance = Luminance(source, neighbor);
                        luminanceSum += neighborLuminance;
                        redSum += source[neighbor] - neighborLuminance;
                        blueSum += source[neighbor + 2] - neighborLuminance;
                        count++;
                        neighborX++;
                    }
                    neighborY++;
                }
                int pixel = (y * width + x) * channelCount;
                double currentLuminance = Luminance(source, pixel);
                double currentRed = source[pixel] - currentLuminance;
                double currentBlue = source[pixel + 2] - currentLuminance;
                double luminanceAverage = luminanceSum / count;
                double redAverage = redSum / count;
                double blueAverage = blueSum / count;
                double luminance = luminanceAverage + SoftThreshold(currentLuminance - luminanceAverage, 0.03 * options.DenoiseStrength);
                double red = redAverage + SoftThreshold(currentRed - redAverage, 0.06 * options.DenoiseStrength);
                double blue = blueAverage + SoftThreshold(currentBlue - blueAverage, 0.06 * options.DenoiseStrength);
                SetLuminanceChroma(output, pixel, luminance, red, blue);
                x++;
            }
            y++;
        }
        return output;
    }

    internal static double SoftThreshold(double value, double threshold)
    {
        if (value > threshold)
        {
            return value - threshold;
        }
        if (value < -threshold)
        {
            return value + threshold;
        }
        return 0.0;
    }

    internal static float[] CorrectChromaticAberration(float[] source, int width, int height, int channelCount, RawChromaticAberration? calibration, RawRenderOptions options)
    {
        float[] output = Copy(source, options);
        int y = 0;
        while (y < height)
        {
            options.CheckCancellation();
            int x = 0;
            while (x < width)
            {
                int pixel = (y * width + x) * channelCount;
                double redX = (calibration ?? throw new System.InvalidOperationException("A required value is unavailable.")).CenterX + (x - calibration.CenterX) * calibration.RedScale;
                double redY = calibration.CenterY + (y - calibration.CenterY) * calibration.RedScale;
                double blueX = calibration.CenterX + (x - calibration.CenterX) * calibration.BlueScale;
                double blueY = calibration.CenterY + (y - calibration.CenterY) * calibration.BlueScale;
                Store(output, pixel, Bilinear(source, width, height, channelCount, redX, redY, 0));
                Store(output, pixel + 2, Bilinear(source, width, height, channelCount, blueX, blueY, 2));
                x++;
            }
            y++;
        }
        return output;
    }

    internal static double Bilinear(float[] source, int width, int height, int channelCount, double x, double y, int channel)
    {
        double boundedX = x < 0.0 ? 0.0 : x > width - 1 ? width - 1 : x;
        double boundedY = y < 0.0 ? 0.0 : y > height - 1 ? height - 1 : y;
        int x0 = (int)System.Math.Floor(boundedX);
        int y0 = (int)System.Math.Floor(boundedY);
        int x1 = x0 + 1 < width ? x0 + 1 : x0;
        int y1 = y0 + 1 < height ? y0 + 1 : y0;
        double fractionX = boundedX - x0;
        double fractionY = boundedY - y0;
        double top = source[(y0 * width + x0) * channelCount + channel] * (1.0 - fractionX) + source[(y0 * width + x1) * channelCount + channel] * fractionX;
        double bottom = source[(y1 * width + x0) * channelCount + channel] * (1.0 - fractionX) + source[(y1 * width + x1) * channelCount + channel] * fractionX;
        return top * (1.0 - fractionY) + bottom * fractionY;
    }

    internal static float[] Sharpen(float[] source, int width, int height, int channelCount, RawRenderOptions options)
    {
        float[] output = Copy(source, options);
        int y = 0;
        while (y < height)
        {
            options.CheckCancellation();
            int x = 0;
            while (x < width)
            {
                double blurred = 0.0;
                int offsetY = -1;
                while (offsetY <= 1)
                {
                    int weightY = offsetY == 0 ? 2 : 1;
                    int boundedY = Bound(y + offsetY, height);
                    int offsetX = -1;
                    while (offsetX <= 1)
                    {
                        int weightX = offsetX == 0 ? 2 : 1;
                        int boundedX = Bound(x + offsetX, width);
                        blurred += Luminance(source, (boundedY * width + boundedX) * channelCount) * weightX * weightY;
                        offsetX++;
                    }
                    offsetY++;
                }
                int pixel = (y * width + x) * channelCount;
                double detail = (Luminance(source, pixel) - blurred / 16.0) * options.CaptureSharpening;
                Store(output, pixel, source[pixel] + detail);
                Store(output, pixel + 1, source[pixel + 1] + detail);
                Store(output, pixel + 2, source[pixel + 2] + detail);
                x++;
            }
            y++;
        }
        return output;
    }

    internal static double Luminance(float[] samples, int index)
    {
        return 0.2126 * samples[index] + 0.7152 * samples[index + 1] + 0.0722 * samples[index + 2];
    }

    internal static void SetLuminanceChroma(float[] output, int index, double luminance, double redChroma, double blueChroma)
    {
        double red = luminance + redChroma;
        double blue = luminance + blueChroma;
        double green = (luminance - 0.2126 * red - 0.0722 * blue) / 0.7152;
        Store(output, index, red);
        Store(output, index + 1, green);
        Store(output, index + 2, blue);
    }

    internal static int Bound(int value, int length)
    {
        if (value < 0)
        {
            return 0;
        }
        if (value >= length)
        {
            return length - 1;
        }
        return value;
    }

    internal static float[] Copy(float[] source, RawRenderOptions options)
    {
        CheckedMath.AllocationSize(source.LongLength, 4, options.Limits);
        float[] output = new float[source.Length];
        System.Array.Copy(source, output, source.Length);
        return output;
    }

    internal static void Store(float[] output, int index, double value)
    {
        if (!RawModelValidation.IsFinite(value) || value < -float.MaxValue || value > float.MaxValue)
        {
            throw new RawFormatException(RawDiagnosticCode.ArithmeticOverflow, "A detail-processing sample exceeds the floating-point range.", -1);
        }
        output[index] = (float)value;
    }
}
