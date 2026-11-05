namespace qrcode;

internal class GlobalHistogramBinarizer
{
    internal const int MinimumContrastSpan = 24;

    internal static GlobalHistogramResult Binarize(
        QrImage luminance,
        QrDetectionOptions options)
    {
        int pixelCount = ValidateImage(luminance, options);
        int[] histogram = CreateHistogram(luminance);
        int[] range = FindRepresentativeRange(histogram, pixelCount);
        bool lowContrast = range[1] - range[0] < MinimumContrastSpan;
        int threshold;
        if (lowContrast)
        {
            threshold = (range[0] + range[1]) / 2;
        }
        else
        {
            threshold = FindOtsuThreshold(histogram, pixelCount);
        }

        CheckedDimensions.CalculateAllocationBytes(
            pixelCount,
            1,
            QrLimits.DefaultMaximumAllocationBytes);
        QrRaster darkOnLight = CreateRaster(luminance, pixelCount, threshold, false);
        QrRaster lightOnDark = CreateRaster(luminance, pixelCount, threshold, true);
        return new GlobalHistogramResult
        {
            Threshold = threshold,
            LowContrast = lowContrast,
            DarkOnLight = darkOnLight,
            LightOnDark = lightOnDark
        };
    }

    internal static int ValidateImage(QrImage luminance, QrDetectionOptions options)
    {
        if (luminance == null)
        {
            throw new QrInputException("The luminance image is required.", "luminance");
        }

        if (options == null)
        {
            throw new QrInputException("The detection options are required.", "options");
        }

        if (luminance.Pixels == null)
        {
            throw new QrInputException("The luminance pixel buffer is required.", "Pixels");
        }

        if (luminance.PixelFormat != QrPixelFormat.Gray8)
        {
            throw new QrInputException("Global histogram binarization requires Gray8 input.", "PixelFormat");
        }

        CheckedDimensions.ValidateDimensionBounds(
            luminance.Width,
            luminance.Height,
            options.MaximumImageWidth,
            options.MaximumImageHeight,
            options.MaximumImagePixels);
        int minimumStride = CheckedDimensions.CalculateMinimumStride(luminance.Width, 1);
        CheckedDimensions.ValidateStride(luminance.Stride, minimumStride);
        int requiredLength = CheckedDimensions.CalculateRequiredBufferLength(
            luminance.Height,
            luminance.Stride,
            minimumStride);
        CheckedDimensions.ValidateBufferLength(requiredLength, luminance.Pixels.Length);
        return CheckedDimensions.CalculateArea(
            luminance.Width,
            luminance.Height,
            options.MaximumImagePixels);
    }

    static int[] CreateHistogram(QrImage luminance)
    {
        int[] histogram = new int[256];
        for (int row = 0; row < luminance.Height; row++)
        {
            int rowOffset = row * luminance.Stride;
            for (int column = 0; column < luminance.Width; column++)
            {
                histogram[luminance.Pixels[rowOffset + column]]++;
            }
        }

        return histogram;
    }

    static int[] FindRepresentativeRange(int[] histogram, int pixelCount)
    {
        int ignoredTailCount = pixelCount / 100;
        int cumulative = 0;
        int minimum = 0;
        for (int value = 0; value < histogram.Length; value++)
        {
            cumulative += histogram[value];
            if (cumulative > ignoredTailCount)
            {
                minimum = value;
                break;
            }
        }

        cumulative = 0;
        int maximum = 255;
        for (int value = histogram.Length - 1; value >= 0; value--)
        {
            cumulative += histogram[value];
            if (cumulative > ignoredTailCount)
            {
                maximum = value;
                break;
            }
        }

        return new int[] { minimum, maximum };
    }

    static int FindOtsuThreshold(int[] histogram, int pixelCount)
    {
        long totalSum = 0;
        for (int value = 0; value < histogram.Length; value++)
        {
            totalSum += (long)value * histogram[value];
        }

        int backgroundCount = 0;
        long backgroundSum = 0;
        double bestScore = -1.0;
        int firstBestThreshold = 0;
        int lastBestThreshold = 0;
        for (int threshold = 0; threshold < histogram.Length - 1; threshold++)
        {
            backgroundCount += histogram[threshold];
            backgroundSum += (long)threshold * histogram[threshold];
            if (backgroundCount == 0)
            {
                continue;
            }

            int foregroundCount = pixelCount - backgroundCount;
            if (foregroundCount == 0)
            {
                break;
            }

            double backgroundMean = (double)backgroundSum / backgroundCount;
            double foregroundMean = (double)(totalSum - backgroundSum) / foregroundCount;
            double meanDifference = backgroundMean - foregroundMean;
            double score = (double)backgroundCount
                * foregroundCount
                * meanDifference
                * meanDifference;
            if (score > bestScore)
            {
                bestScore = score;
                firstBestThreshold = threshold;
                lastBestThreshold = threshold;
            }
            else if (score == bestScore)
            {
                lastBestThreshold = threshold;
            }
        }

        return (firstBestThreshold + lastBestThreshold + 1) / 2;
    }

    static QrRaster CreateRaster(
        QrImage luminance,
        int pixelCount,
        int threshold,
        bool inverted)
    {
        QrRaster raster = new QrRaster
        {
            Pixels = new byte[pixelCount],
            Width = luminance.Width,
            Height = luminance.Height,
            Stride = luminance.Width
        };
        for (int row = 0; row < luminance.Height; row++)
        {
            int sourceRow = row * luminance.Stride;
            int destinationRow = row * raster.Stride;
            for (int column = 0; column < luminance.Width; column++)
            {
                bool dark = luminance.Pixels[sourceRow + column] <= threshold;
                if (inverted)
                {
                    dark = !dark;
                }

                raster.Pixels[destinationRow + column] = dark ? (byte)0 : (byte)255;
            }
        }

        return raster;
    }
}
