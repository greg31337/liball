namespace barcode;

internal class ImageHistogramAnalysis
{
    internal int[] Histogram = new int[256];
    internal int Minimum;
    internal int Maximum;
    internal int Median;
    internal int GlobalThreshold;
    internal int OtsuThreshold;
    internal int Contrast;
    internal int DarkPixelCount;
    internal int LightPixelCount;
    internal double Mean;
    internal double BorderMean;
    internal BarcodePolarity EstimatedPolarity;
}

internal static class HistogramAnalyzer
{
    internal static ImageHistogramAnalysis Analyze(
        ManagedGrayImage? source)
    {
        ValidateGrayImage(source);
        ManagedGrayImage image = source == null
            ? new ManagedGrayImage(1, 1)
            : source;
        int[] histogram = new int[256];
        long sum = 0L;
        int minimum = 255;
        int maximum = 0;
        int index = 0;
        while (index < image.Samples.Length)
        {
            int value = image.Samples[index];
            histogram[value] = histogram[value] + 1;
            sum = sum + value;
            if (value < minimum)
            {
                minimum = value;
            }

            if (value > maximum)
            {
                maximum = value;
            }

            index = index + 1;
        }

        int otsu = CalculateOtsuThreshold(
            histogram,
            image.Samples.Length,
            sum,
            minimum);
        int darkCount = 0;
        index = 0;
        while (index <= otsu)
        {
            darkCount = darkCount + histogram[index];
            index = index + 1;
        }

        double mean = (double)sum / image.Samples.Length;
        double borderMean = CalculateBorderMean(image);
        ImageHistogramAnalysis result = new ImageHistogramAnalysis();
        result.Histogram = histogram;
        result.Minimum = minimum;
        result.Maximum = maximum;
        result.Median = CalculateMedian(
            histogram,
            image.Samples.Length);
        result.GlobalThreshold = (minimum + maximum) / 2;
        result.OtsuThreshold = otsu;
        result.Contrast = maximum - minimum;
        result.DarkPixelCount = darkCount;
        result.LightPixelCount = image.Samples.Length - darkCount;
        result.Mean = mean;
        result.BorderMean = borderMean;
        result.EstimatedPolarity = borderMean < mean
            ? BarcodePolarity.LightOnDark
            : BarcodePolarity.DarkOnLight;
        return result;
    }

    internal static void ValidateGrayImage(ManagedGrayImage? source)
    {
        if (source == null || source.Samples == null)
        {
            throw new BarcodeValidationException(
                "A managed grayscale image is required.");
        }

        CheckedMath.ValidateDimension(source.Width);
        CheckedMath.ValidateDimension(source.Height);
        int expected = CheckedMath.Multiply(
            source.Width,
            source.Height);
        if (source.Samples.Length != expected)
        {
            throw new BarcodeValidationException(
                "The managed grayscale sample buffer does not match its dimensions.");
        }

        int index = 0;
        while (index < source.Samples.Length)
        {
            int value = source.Samples[index];
            if (value < 0 || value > 255)
            {
                throw new BarcodeValidationException(
                    "Managed grayscale samples must be between zero and 255.");
            }

            index = index + 1;
        }
    }

    internal static int CalculateOtsuThreshold(
        int[] histogram,
        int sampleCount,
        long totalSum,
        int fallback)
    {
        if (histogram == null || histogram.Length != 256)
        {
            throw new BarcodeValidationException(
                "An Otsu histogram must contain 256 bins.");
        }

        if (sampleCount <= 0)
        {
            throw new BarcodeValidationException(
                "An Otsu histogram must contain samples.");
        }

        long backgroundSum = 0L;
        int backgroundCount = 0;
        double bestVariance = -1.0;
        int bestThreshold = fallback;
        int threshold = 0;
        while (threshold < 256)
        {
            int binCount = histogram[threshold];
            backgroundCount = backgroundCount + binCount;
            backgroundSum = backgroundSum
                + ((long)threshold * binCount);
            int foregroundCount = sampleCount - backgroundCount;
            if (backgroundCount > 0 && foregroundCount > 0)
            {
                double backgroundMean = (double)backgroundSum
                    / backgroundCount;
                double foregroundMean = (double)(totalSum - backgroundSum)
                    / foregroundCount;
                double difference = backgroundMean - foregroundMean;
                double variance = (double)backgroundCount
                    * foregroundCount
                    * difference
                    * difference;
                if (variance > bestVariance)
                {
                    bestVariance = variance;
                    bestThreshold = threshold;
                }
            }

            threshold = threshold + 1;
        }

        return bestThreshold;
    }

    static int CalculateMedian(int[] histogram, int sampleCount)
    {
        int target = (sampleCount - 1) / 2;
        int cumulative = 0;
        int value = 0;
        while (value < 256)
        {
            cumulative = cumulative + histogram[value];
            if (cumulative > target)
            {
                return value;
            }

            value = value + 1;
        }

        return 255;
    }

    static double CalculateBorderMean(ManagedGrayImage source)
    {
        long sum = 0L;
        int count = 0;
        int column = 0;
        while (column < source.Width)
        {
            sum = sum + source.ReadSample(column, 0);
            count = count + 1;
            if (source.Height > 1)
            {
                sum = sum + source.ReadSample(
                    column,
                    source.Height - 1);
                count = count + 1;
            }

            column = column + 1;
        }

        int row = 1;
        while (row < source.Height - 1)
        {
            sum = sum + source.ReadSample(0, row);
            count = count + 1;
            if (source.Width > 1)
            {
                sum = sum + source.ReadSample(
                    source.Width - 1,
                    row);
                count = count + 1;
            }

            row = row + 1;
        }

        return (double)sum / count;
    }
}
