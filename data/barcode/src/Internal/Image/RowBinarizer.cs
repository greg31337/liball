namespace barcode;

internal class RowBinarizationData
{
    internal ModuleMatrix Modules = new ModuleMatrix(1, 1);
    internal int[] Thresholds = new int[0];
}

internal static class RowBinarizer
{
    internal static RowBinarizationData BinarizeRows(
        ManagedGrayImage? source,
        int fallbackThreshold,
        BarcodePolarity polarity)
    {
        HistogramAnalyzer.ValidateGrayImage(source);
        GlobalBinarizer.ValidatePolarity(polarity);
        if (fallbackThreshold < 0 || fallbackThreshold > 255)
        {
            throw new BarcodeValidationException(
                "A row fallback threshold must be between zero and 255.");
        }

        ManagedGrayImage image = source == null
            ? new ManagedGrayImage(1, 1)
            : source;
        ModuleMatrix modules = new ModuleMatrix(
            image.Width,
            image.Height);
        int[] thresholds = new int[image.Height];
        int row = 0;
        while (row < image.Height)
        {
            int[] histogram = new int[256];
            long sum = 0L;
            int minimum = 255;
            int maximum = 0;
            int column = 0;
            while (column < image.Width)
            {
                int value = image.ReadSample(column, row);
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

                column = column + 1;
            }

            int threshold = minimum == maximum
                ? fallbackThreshold
                : HistogramAnalyzer.CalculateOtsuThreshold(
                    histogram,
                    image.Width,
                    sum,
                    fallbackThreshold);
            thresholds[row] = threshold;
            column = 0;
            while (column < image.Width)
            {
                int sample = image.ReadSample(column, row);
                bool foreground = polarity == BarcodePolarity.DarkOnLight
                    ? sample <= threshold
                    : sample > threshold;
                modules.SetModule(column, row, foreground);
                column = column + 1;
            }

            row = row + 1;
        }

        RowBinarizationData result = new RowBinarizationData();
        result.Modules = modules;
        result.Thresholds = thresholds;
        return result;
    }
}
